namespace OneTimeSecret.Web.Services
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public class AesEncryptionService : IAesEncryptionService
    {
        private const int AesKeyLengthBytes = 32; // aes-256 => 32 byte key
        private const int Version1Iterations = 10000;

        private const int VersionLengthBytes = 1;
        private const int SaltLengthBytes = 8;
        private const int IVLengthBytes = 16;
        private const int TokenPrefixLengthBytes = VersionLengthBytes + SaltLengthBytes + IVLengthBytes;
        private const int CipherBlockLengthBytes = 16; // aes is always 128 bit blocks

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

        /// <summary>
        /// Use the operating system crypto rng to generate a salt of the given length
        /// </summary>
        /// <param name="bytes">The number of bytes to generate the salt with</param>
        /// <returns>The salt generated</returns>
        public byte[] GenerateSecureRandomBytes(int bytes)
        {
            byte[] result = new byte[bytes];

            using (var rngCryptoProvider = new RNGCryptoServiceProvider())
            {
                rngCryptoProvider.GetBytes(result);
            }

            return result;
        }

        public byte[] Encrypt(byte[] data, string passPhrase = null)
        {
            byte[] salt = GenerateSecureRandomBytes(SaltLengthBytes);
            byte[] iv = GenerateSecureRandomBytes(IVLengthBytes);
            byte[] hashPassPhrase = this.Hash(passPhrase);

            return this.EncryptRaw(data, salt, iv, this.encryptVersion, hashPassPhrase);
        }

        public byte[] Decrypt(byte[] token, string passPhrase = null)
        {
            if (token == null)
            {
                throw new ArgumentNullException("token");
            }
            else if ((token.Length - TokenPrefixLengthBytes) % CipherBlockLengthBytes != 0)
            {
                string message = string.Format("Expected token length in bytes to be {0} plus a multiple of {1}", TokenPrefixLengthBytes, CipherBlockLengthBytes);

                throw new Exception(message);
            }

            byte[] hashPassPhrase = this.Hash(passPhrase);

            return DecryptRaw(token, hashPassPhrase);
        }

        // NOTE: The format of the result is as follows
        // format: | version | salt | iv | ciphertext |
        // bytes:  |    1    |  8   | 16 |    16*n    |
        // NOTE: n is the number of blocks needed in the range [1, inf)
        private byte[] EncryptRaw(byte[] data, byte[] salt, byte[] iv, byte version, byte[] passPhrase = null)
        {
            int iterations = GetIterationsForVersion(version);

            byte[] encryptKeyPassword = DetermineEncryptionKeyPassword(passPhrase);

            // rfc 2898 is sha1, so we have to do pbkdf2 this way in native c#
            using (var pbkdf2 = new Rfc2898DeriveBytes(encryptKeyPassword, salt, iterations))
            {
                byte[] derivedKey = pbkdf2.GetBytes(AesKeyLengthBytes);

                var aes = new AesManaged
                {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7,
                    IV = iv,
                    KeySize = derivedKey.Length * 8,
                    Key = derivedKey
                };

                using (var encryptedData = new MemoryStream())
                {
                    byte[] versionBuffer = { version };

                    // write the version, salt, and iv first
                    encryptedData.Write(versionBuffer, 0, VersionLengthBytes);
                    encryptedData.Write(salt, 0, SaltLengthBytes);
                    encryptedData.Write(iv, 0, IVLengthBytes);

                    // write the cipher text
                    // you have to dispose cryptostream instead of flush for some reason
                    using (var encryption = new CryptoStream(encryptedData, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        encryption.Write(data, 0, data.Length);
                    }

                    byte[] result = encryptedData.ToArray();

                    if ((result.Length - TokenPrefixLengthBytes) % CipherBlockLengthBytes != 0)
                    {
                        string message = string.Format("Expected token length in bytes to be {0} plus a multiple of {1}", TokenPrefixLengthBytes, CipherBlockLengthBytes);

                        throw new Exception(message);
                    }

                    return result;
                }
            }
        }

        private byte[] DecryptRaw(byte[] token, byte[] passPhrase)
        {
            int tokenCipherTextLength = token.Length - TokenPrefixLengthBytes;

            byte version = token[0];
            byte[] salt = CopySlice(token, VersionLengthBytes, SaltLengthBytes);
            byte[] iv = CopySlice(token, VersionLengthBytes + SaltLengthBytes, IVLengthBytes);
            byte[] cipherText = CopySlice(token, VersionLengthBytes + SaltLengthBytes + IVLengthBytes, tokenCipherTextLength);

            int iterations = GetIterationsForVersion(version);
            byte[] encryptKeyPassword = DetermineEncryptionKeyPassword(passPhrase);

            using (var pbkdf2 = new Rfc2898DeriveBytes(encryptKeyPassword, salt, iterations))
            {
                byte[] derivedKey = pbkdf2.GetBytes(AesKeyLengthBytes);

                var aes = new AesManaged
                {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7,
                    IV = iv,
                    KeySize = derivedKey.Length * 8,
                    Key = derivedKey
                };

                using (var encryptedData = new MemoryStream())
                {
                    using (var decryption = new CryptoStream(encryptedData, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        decryption.Write(cipherText, 0, cipherText.Length);
                    }

                    byte[] result = encryptedData.ToArray();

                    return result;
                }
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