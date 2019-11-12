namespace OneTimeSecret.Web.Models.Config
{
    public class AesConfig
    {
        public string MasterKey { get; set; } = default!;

        public byte Version { get; set; } = default!;
    }
}
