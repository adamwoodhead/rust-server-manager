using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerNode.Utility
{
    internal static class StringExtension
    {
        internal static bool EndsWithAny(this string haystack, IEnumerable<string> needles) => EndsWithAny(haystack, needles.ToArray());

        internal static bool EndsWithAny(this string haystack, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (haystack.EndsWith(needle))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool ContainsAny(this string haystack, IEnumerable<string> needles) => ContainsAny(haystack, needles.ToArray());

        internal static bool ContainsAny(this string haystack, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (haystack.Contains(needle))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
