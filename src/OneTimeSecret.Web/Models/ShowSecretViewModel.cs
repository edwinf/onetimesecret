namespace OneTimeSecret.Web.Models
{
    public class ShowSecretViewModel
    {
        public string Secret { get; set; }

        public bool DoesntExist { get; internal set; }

        public bool HasPassphrase { get; internal set; }

        public bool DidError { get; internal set; }
    }
}
