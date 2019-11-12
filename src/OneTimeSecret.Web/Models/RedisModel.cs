namespace OneTimeSecret.Web.Models
{
    public class RedisModel
    {
        public bool HasPassphrase { get; set; } = default!;

        public string EncryptedData { get; set; } = default!;
    }
}
