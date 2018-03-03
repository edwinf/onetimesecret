using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OneTimeSecret.Web.Models
{
    public class RedisModel
    {
        public bool HasPassphrase { get; set; }

        public string EncryptedData { get; set; }
    }
}
