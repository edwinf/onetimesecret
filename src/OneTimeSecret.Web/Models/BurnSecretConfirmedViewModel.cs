using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaTime;

namespace OneTimeSecret.Web.Models
{
    public class BurnSecretConfirmedViewModel
    {
        public Instant BurnedAt { get; internal set; }
    }
}
