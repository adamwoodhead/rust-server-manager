using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerNode.Utility
{
    internal static class StringExtension
    {
        /// <summary>
        /// Checks if the string ends with any of the states strings
        /// </summary>
        /// <param name="haystack"></param>
        /// <param name="needles"></param>
        /// <returns></returns>
        internal static bool EndsWithAny(this string haystack, IEnumerable<string> needles) => EndsWithAny(haystack, needles.ToArray());

        /// <summary>
        /// Checks if the string ends with any of the states strings
        /// </summary>
        /// <param name="haystack"></param>
        /// <param name="needles"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Checks if the string contains any of the states strings
        /// </summary>
        /// <param name="haystack"></param>
        /// <param name="needles"></param>
        /// <returns></returns>
        internal static bool ContainsAny(this string haystack, IEnumerable<string> needles) => ContainsAny(haystack, needles.ToArray());

        /// <summary>
        /// Checks if the string contains any of the states strings
        /// </summary>
        /// <param name="haystack"></param>
        /// <param name="needles"></param>
        /// <returns></returns>
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

        internal static bool IsDigitsOnly(this string str)
        {
            foreach (char c in str)
            {
                if (!IsRealDigitOnly(c))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsRealDigitOnly(this char ch)
        {
            if (ch < '0' || ch > '9')
            {
                return false;
            }

            return true;
        }
    }
}
