using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RustServerManager.Utility
{
    internal static class Generators
    {
        private static Random _random { get; } = new Random((int)DateTime.UtcNow.Ticks);

        internal static string GetUniqueKey(int maxSize, string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890")
        {
            char[] chars = charset.ToCharArray();

            byte[] data = new byte[1];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                crypto.GetNonZeroBytes(data);
            }

            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }

            return result.ToString();
        }

        internal static string GetTimeBasedUniqueKey()
        {
            return $"{DateTime.UtcNow.Ticks}AStringOutsideOfTheDateTimeTicks".SHA256();
        }

        internal static string SHA256(this string text)
        {
            SHA256Managed hashString = new SHA256Managed();

            string hex = "";
            foreach (byte x in hashString.ComputeHash(Encoding.ASCII.GetBytes(text)))
            {
                hex += string.Format("{0:x2}", x);
            }

            return hex?.ToUpper() ?? string.Empty;
        }

        internal static int Random(int min = 0, int max = 10)
        {
            return _random.Next(min, max);
        }
    }
}
