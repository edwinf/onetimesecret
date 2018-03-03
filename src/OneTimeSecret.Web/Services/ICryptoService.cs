namespace OneTimeSecret.Web.Services
{
    public interface ICryptoService
    {
        string CreateRandomString(int length);

        string EncryptData(string data, string passphrase = null);

        string DecryptData(string data, string passphrase = null);
    }
}
