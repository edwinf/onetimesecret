using System;

namespace OneTimeSecret.Web.Models
{
    public class BurnSecretViewModel
    { 
        public bool IsBurned { get; set; }

        public DateTime BurnedAt { get; set; }
    }
}
