using System.Collections.Generic;

namespace OneTimeSecret.Web.Models.Config
{
    public class NetworkingConfig
    {
        public List<string> KnownProxyServers { get; set; } = new List<string>();

        public int UpstreamProxyHops { get; set; }
    }
}
