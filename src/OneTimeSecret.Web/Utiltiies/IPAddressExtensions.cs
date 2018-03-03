using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OneTimeSecret.Web.Utiltiies
{
    public static class IPAddressExtensions
    {
        public static bool IsInternal(this IPAddress toTest)
        {
            if (IPAddress.IsLoopback(toTest))
            {
                return true;
            }

            byte[] bytes = toTest.GetAddressBytes();
            switch (bytes[0])
            {
                case 10:
                    return true;
                case 172:
                    return bytes[1] < 32 && bytes[1] >= 16;
                case 192:
                    return bytes[1] == 168;
                default:
                    return false;
            }
        }
    }
}
