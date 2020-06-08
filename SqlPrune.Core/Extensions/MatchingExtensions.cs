using System;
using System.Collections.Generic;

namespace Comsec.SqlPrune.Extensions
{
    public static class MatchingExtensions
    {
        /// <summary>
        /// Enumerates the <see cref="elements"/> and returns any entry matching the <see cref="searchPatterns"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements">The elements.</param>
        /// <param name="selector">Hook to define which string value should be searched.</param>
        /// <param name="searchPatterns">The search patterns.</param>
        /// <returns></returns>
        public static IEnumerable<T> MatchOnAny<T>(this IEnumerable<T> elements, Func<T, string> selector, params string[] searchPatterns)
        {
            foreach (var element in elements)
            {
                var value = selector.Invoke(element);

                var isMatch = false;

                foreach (var pattern in searchPatterns)
                {
                    if (!pattern.Contains("*") && value == pattern)
                    {
                        isMatch = true;
                        break;
                    }

                    if (pattern.StartsWith("*") || pattern.StartsWith("."))
                    {
                        if (value.EndsWith(pattern.Replace("*", "")))
                        {
                            isMatch = true;
                            break;
                        }
                    }
                    else
                    {
                        if (value.StartsWith(pattern.Replace("*", "")))
                        {
                            isMatch = true;
                            break;
                        }
                    }
                }

                if (isMatch)
                {
                    yield return element;
                }
            }
        }
    }
}