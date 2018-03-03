using System;

namespace OneTimeSecret.Web.Utiltiies
{
    public static class ByteExtensions
    {
        public static string ToHex(this byte[] bytes)
        {
            string hex = BitConverter.ToString(bytes);
            return hex.Replace("-", "").ToLower();
        }
    }
}
