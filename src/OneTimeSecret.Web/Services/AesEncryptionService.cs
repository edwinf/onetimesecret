using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace OneTimeSecret.Web.Services
{
    public class AesEncryptionService : IAesEncryptionService
    {
        private const int AesKeyLengthBytes = 16; // aes-gcm-128 => 16 byte key
        private const int Version1Iterations = 10000;

        private const int VersionLengthBytes = 1;
        private const int NonceLengthBytes = 12;
        private const int TagLengthBytes = 16;
        private const int TokenPrefixLengthBytes = VersionLengthBytes + NonceLengthBytes + TagLengthBytes;

        private readonly string masterKey;
        private readonly byte encryptVersion;

        public AesEncryptionService(string masterKey, byte encryptVersion)
        {
            if (masterKey == null)
            {
                throw new ArgumentNullException(nameof(masterKey));
            }
            else if (encryptVersion == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(encryptVersion), "version should be greater than zero");
            }

            this.masterKey = masterKey;
            this.encryptVersion = encryptVersion;
        }

        public byte[] Encrypt(byte[] data, string? passPhrase = null)
        {
            byte[] nonce = new byte[NonceLengthBytes];

            RandomNumberGenerator.Fill(nonce);

            return this.EncryptRaw(data, nonce, this.encryptVersion, passPhrase);
        }

        public byte[] Decrypt(byte[] token, string? passPhrase = null)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return this.DecryptRaw(token, passPhrase);
        }

        private static int GetIterationsForVersion(byte version)
        {
            return version switch
            {
                1 => Version1Iterations,
                _ => throw new Exception($"Unknown encryption token version {version}"),
            };
        }

        private static byte[] CopySlice(byte[] buffer, int offset, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(buffer, offset, result, 0, length);

            return result;
        }

        // NOTE: The format of the result is as follows
        // format: | version | nonce | tag | ciphertext |
        // bytes:  |    1    |  12   | 16 |    16*n    |
        // NOTE: n is the number of blocks needed in the range [1, inf)
        private byte[] EncryptRaw(byte[] data, byte[] nonce, byte version, string? passPhrase = null)
        {
            int iterations = GetIterationsForVersion(version);

            string encryptKeyPassword = this.DetermineEncryptionKeyPassword(passPhrase);

            byte[] derivedKey = KeyDerivation.Pbkdf2(
                                    password: encryptKeyPassword,
                                    salt: nonce,
                                    prf: KeyDerivationPrf.HMACSHA1,
                                    iterationCount: iterations,
                                    numBytesRequested: AesKeyLengthBytes);

            byte[] tag = new byte[TagLengthBytes];
            byte[] cipherText = new byte[data.Length];

            using (var aesGcm = new AesGcm(derivedKey))
            {
                aesGcm.Encrypt(nonce, data, cipherText, tag);
            }

            using (var memoryStream = new MemoryStream())
            {
                byte[] versionBuffer = { version };

                // write the version, salt, and iv first
                memoryStream.Write(versionBuffer, 0, VersionLengthBytes);
                memoryStream.Write(nonce, 0, NonceLengthBytes);
                memoryStream.Write(tag, 0, TagLengthBytes);
                memoryStream.Write(cipherText, 0, cipherText.Length);

                byte[] result = memoryStream.ToArray();

                return result;
            }
        }

        private byte[] DecryptRaw(byte[] token, string? passPhrase)
        {
            int tokenCipherTextLength = token.Length - TokenPrefixLengthBytes;

            byte version = token[0];
            byte[] nonce = CopySlice(token, VersionLengthBytes, NonceLengthBytes);
            byte[] tag = CopySlice(token, VersionLengthBytes + NonceLengthBytes, TagLengthBytes);
            byte[] cipherText = CopySlice(token, VersionLengthBytes + NonceLengthBytes + TagLengthBytes, tokenCipherTextLength);

            int iterations = GetIterationsForVersion(version);
            string encryptKeyPassword = this.DetermineEncryptionKeyPassword(passPhrase);

            byte[] derivedKey = KeyDerivation.Pbkdf2(
                    password: encryptKeyPassword,
                    salt: nonce,
                    prf: KeyDerivationPrf.HMACSHA1,
                    iterationCount: iterations,
                    numBytesRequested: AesKeyLengthBytes);

            byte[] decryptedData = new byte[cipherText.Length];
            using (var aesGcm = new AesGcm(derivedKey))
            {
                aesGcm.Decrypt(nonce, cipherText, tag, decryptedData);
            }

            return decryptedData;
        }

        private string DetermineEncryptionKeyPassword(string? passPhrase)
        {
            string encryptKey;
            if (passPhrase != null)
            {
                return this.masterKey + passPhrase;
            }
            else
            {
                encryptKey = this.masterKey;
            }

            return encryptKey;
        }
    }
}