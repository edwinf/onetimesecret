using System.ComponentModel.DataAnnotations;

namespace OneTimeSecret.Web.Models
{
    public class CreateSecretViewModel
    {
        [Required]
        public string Secret { get; set; } = default!;

        public string Passphrase { get; set; } = default!;

        [Required]
        [Range(0, 604800, ErrorMessage = "The lifetime cannot be less then 1 second and cannot be greater then 7 days")]
        public int TTL { get; set; }
    }
}
