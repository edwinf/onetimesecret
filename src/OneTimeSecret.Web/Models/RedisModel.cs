namespace OneTimeSecret.Web.Models
{
    public class RedisModel
    {
        public bool HasPassphrase { get; set; }

        public string EncryptedData { get; set; }
    }
}
