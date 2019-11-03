namespace OneTimeSecret.Web.Services
{
    public interface IAesEncryptionService
    {
        byte[] Encrypt(byte[] data, string passPhrase = null);

        byte[] Decrypt(byte[] token, string passPhrase = null);
    }
}