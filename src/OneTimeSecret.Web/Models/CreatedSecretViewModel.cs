using NodaTime;

namespace OneTimeSecret.Web.Models
{
    public class CreatedSecretViewModel
    {
        public string AccessUrl { get; set; } = default!;

        public bool PassphraseRequired { get; set; } = default!;

        public string DurationString { get; set; } = default!;

        public Instant Expires { get; set; } = default!;

        public string BurnUrl { get; set; } = default!;
    }
}
