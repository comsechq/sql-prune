using System;

namespace Comsec.SqlPrune
{
    /// <summary>
    /// Extension method to help manipulating date and time.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Gets the start of the week given by the date time.
        /// </summary>
        /// <param name="dt">The dt.</param>
        /// <param name="startOfWeek">The start of week.</param>
        /// <returns></returns>
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            var takeDays = dt.DayOfWeek - startOfWeek;

            if (takeDays < 0)
            {
                takeDays = 7 - Math.Abs(takeDays);
            }

            return dt.AddDays(-takeDays);
        }
    }
}
