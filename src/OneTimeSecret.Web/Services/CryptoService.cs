using System;
using System.Security.Cryptography;
using OneTimeSecret.Web.Utiltiies;

namespace OneTimeSecret.Web.Services
{
    public class CryptoService : ICryptoService
    {
        private readonly IAesEncryptionService aesEncryptionService;

        public CryptoService(IAesEncryptionService aesEncryptionService)
        {
            this.aesEncryptionService = aesEncryptionService;
        }

        public string CreateRandomString(int length)
        {
            byte[] randomBytes = new byte[length];
            RandomNumberGenerator.Fill(randomBytes);

            return randomBytes.ToHex();
        }

        public string DecryptData(string data, string passphrase = null)
        {
            byte[] bytes = Convert.FromBase64String(data);
            byte[] decryptedBites = this.aesEncryptionService.Decrypt(bytes, passphrase);
            return System.Text.Encoding.UTF8.GetString(decryptedBites);
        }

        public string EncryptData(string data, string passphrase = null)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
            byte[] encryptedBits = this.aesEncryptionService.Encrypt(bytes, passphrase);
            return Convert.ToBase64String(encryptedBits);
        }
    }
}
