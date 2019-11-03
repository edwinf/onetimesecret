namespace OneTimeSecret.Web.Services
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public class AesEncryptionService : IAesEncryptionService
    {
        private const int AesKeyLengthBytes = 16; // aes-gcm-128 => 16 byte key
        private const int Version1Iterations = 10000;

        private const int VersionLengthBytes = 1;
        private const int NonceLengthBytes = 12;
        private const int TagLengthBytes = 16;
        private const int TokenPrefixLengthBytes = VersionLengthBytes + NonceLengthBytes + TagLengthBytes;

        private readonly byte[] masterKey;
        private readonly byte encryptVersion;

        public AesEncryptionService(byte[] masterKey, byte encryptVersion)
        {
            if (masterKey == null)
            {
                throw new ArgumentNullException("key");
            }
            else if (encryptVersion == 0)
            {
                throw new ArgumentOutOfRangeException("encryptVersion", "version should be greater than zero");
            }

            this.masterKey = masterKey;
            this.encryptVersion = encryptVersion;
        }

        public byte[] Encrypt(byte[] data, string passPhrase = null)
        {
            byte[] nonce = new byte[NonceLengthBytes];

            RandomNumberGenerator.Fill(nonce);

            byte[] hashPassPhrase = this.Hash(passPhrase);

            return this.EncryptRaw(data, nonce, this.encryptVersion, hashPassPhrase);
        }

        public byte[] Decrypt(byte[] token, string passPhrase = null)
        {
            if (token == null)
            {
                throw new ArgumentNullException("token");
            }

            byte[] hashPassPhrase = this.Hash(passPhrase);

            return DecryptRaw(token, hashPassPhrase);
        }

        // NOTE: The format of the result is as follows
        // format: | version | nonce | tag | ciphertext |
        // bytes:  |    1    |  12   | 16 |    16*n    |
        // NOTE: n is the number of blocks needed in the range [1, inf)
        private byte[] EncryptRaw(byte[] data, byte[] nonce, byte version, byte[] passPhrase = null)
        {
            int iterations = GetIterationsForVersion(version);

            byte[] encryptKeyPassword = DetermineEncryptionKeyPassword(passPhrase);

            // rfc 2898 is sha1, so we have to do pbkdf2 this way in native c#
            using (var pbkdf2 = new Rfc2898DeriveBytes(encryptKeyPassword, nonce, iterations))
            {
                byte[] derivedKey = pbkdf2.GetBytes(AesKeyLengthBytes);
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
        }

        private byte[] DecryptRaw(byte[] token, byte[] passPhrase)
        {
            int tokenCipherTextLength = token.Length - TokenPrefixLengthBytes;

            byte version = token[0];
            byte[] nonce = CopySlice(token, VersionLengthBytes, NonceLengthBytes);
            byte[] tag = CopySlice(token, VersionLengthBytes + NonceLengthBytes, TagLengthBytes);
            byte[] cipherText = CopySlice(token, VersionLengthBytes + NonceLengthBytes + TagLengthBytes, tokenCipherTextLength);

            int iterations = GetIterationsForVersion(version);
            byte[] encryptKeyPassword = DetermineEncryptionKeyPassword(passPhrase);

            using (var pbkdf2 = new Rfc2898DeriveBytes(encryptKeyPassword, nonce, iterations))
            {
                byte[] derivedKey = pbkdf2.GetBytes(AesKeyLengthBytes);
                byte[] decryptedData = new byte[cipherText.Length];
                using (AesGcm aesGcm = new AesGcm(derivedKey))
                {
                    aesGcm.Decrypt(nonce, cipherText, tag, decryptedData);
                }

                return decryptedData;
            }
        }

        private static int GetIterationsForVersion(byte version)
        {
            switch (version)
            {
                case 1:
                    return Version1Iterations;
                default:
                    string message = string.Format("Unknown encryption token version {0}", version);
                    throw new Exception(message);
            }
        }

        private static byte[] CopySlice(byte[] buffer, int offset, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(buffer, offset, result, 0, length);

            return result;
        }

        private byte[] DetermineEncryptionKeyPassword(byte[] passPhrase)
        {
            byte[] encryptKey;
            if (passPhrase != null)
            {
                encryptKey = new byte[this.masterKey.Length + passPhrase.Length];
                Buffer.BlockCopy(this.masterKey, 0, encryptKey, 0, this.masterKey.Length);
                Buffer.BlockCopy(passPhrase, 0, encryptKey, this.masterKey.Length, passPhrase.Length);
            }
            else
            {
                encryptKey = this.masterKey;
            }

            return encryptKey;
        }

        private byte[] Hash(string passPhrase)
        {
            byte[] result = null;

            if (passPhrase != null)
            {
                using (var sha256 = SHA256.Create())
                {
                    result = sha256.ComputeHash(Encoding.UTF8.GetBytes(passPhrase));
                }
            }

            return result;
        }
    }
}