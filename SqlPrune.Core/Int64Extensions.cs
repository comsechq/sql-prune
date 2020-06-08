using System;

namespace Comsec.SqlPrune
{
    /// <summary>
    /// Extension methods for long/Int64 numbers
    /// </summary>
    public static class Int64Extensions
    {
        private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        /// <summary>
        /// Sizes the suffix.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// JLRishe's implementation from http://stackoverflow.com/questions/14488796/does-net-provide-an-easy-way-convert-bytes-to-kb-mb-gb-etc
        /// </remarks>
        public static string ToSizeWithSuffix(this Int64 value)
        {
            if (value == 0)
            {
                return "0";
            }

            var mag = (int) Math.Log(value, 1024);

            var adjustedSize = (decimal) value/(1L << (mag*10));

            return string.Format("{0:N1} {1}", adjustedSize, SizeSuffixes[mag]);
        }
    }
}
