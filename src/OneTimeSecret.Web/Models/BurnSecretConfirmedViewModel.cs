using NodaTime;

namespace OneTimeSecret.Web.Models
{
    public class BurnSecretConfirmedViewModel
    {
        public Instant BurnedAt { get; internal set; }
    }
}
