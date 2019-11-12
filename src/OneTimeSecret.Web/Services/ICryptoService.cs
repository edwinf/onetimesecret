namespace OneTimeSecret.Web.Services
{
    public interface ICryptoService
    {
        string CreateRandomString(int length);

        string EncryptData(string data, string passphrase);

        string DecryptData(string data, string passphrase);
    }
}
