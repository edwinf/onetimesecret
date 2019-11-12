namespace OneTimeSecret.Web.Services
{
    public interface IAesEncryptionService
    {
        byte[] Encrypt(byte[] data, string passPhrase);

        byte[] Decrypt(byte[] token, string passPhrase);
    }
}