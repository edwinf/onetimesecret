using System.Collections.Generic;

namespace OneTimeSecret.Web.Models.Config
{
    public class NetworkingConfig
    {
        public List<string> KnownProxyServers { get; set; }

        public int UpstreamProxyHops { get; set; }
    }
}
