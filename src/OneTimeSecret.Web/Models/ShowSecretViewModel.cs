namespace OneTimeSecret.Web.Models
{
    public class ShowSecretViewModel
    {
        public string Secret { get; set; } = default!;

        public bool DoesntExist { get; internal set; } = default!;

        public bool HasPassphrase { get; internal set; } = default!;

        public bool DidError { get; internal set; } = default!;
    }
}
