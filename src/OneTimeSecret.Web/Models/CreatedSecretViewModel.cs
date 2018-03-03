using System;
using NodaTime;

namespace OneTimeSecret.Web.Models
{
    public class CreatedSecretViewModel
    {
        public string AccessUrl { get; set; }

        public bool PassphraseRequired { get; set; }

        public string DurationString { get; set; }

        public Instant Expires { get; set; }

        public string BurnUrl { get; set; }
    }
}
