namespace OneTimeSecret.Web.Services
{
    public interface IAesEncryptionService
    {
        byte[] GenerateSecureRandomBytes(int bytes);

        byte[] Encrypt(byte[] data, string passPhrase = null);

        byte[] Decrypt(byte[] token, string passPhrase = null);
    }
}