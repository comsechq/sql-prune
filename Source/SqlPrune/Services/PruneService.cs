using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Comsec.SqlPrune.Interfaces.Services;
using Comsec.SqlPrune.Models;
using Sugar;

namespace Comsec.SqlPrune.Services
{
    /// <summary>
    /// Service to hold business logic that decides wether or not to prune a backup from a set.
    /// </summary>
    public class PruneService : IPruneService
    {
        /// <summary>
        /// Keeps the first item in the set (set prunable to false) all other items are set as 'prunable'.
        /// </summary>
        /// <param name="set">The set.</param>
        public void KeepFirst(IEnumerable<BakModel> set)
        {
            var index = 0;

            foreach (var backup in set)
            {
                backup.Prunable = index != 0;

                index++;
            }
        }

        /// <summary>
        /// Ensures only one backup of per day is kept in given <see cref="set"/>.
        /// </summary>
        /// <param name="set">The set.</param>
        /// <remarks>
        /// This rule should apply last after prunability has already been set.
        /// </remarks>
        public void KeepOnePerDay(IEnumerable<BakModel> set)
        {
            var dayGrouping = set.Where(x => x.Prunable.HasValue)
                                 .GroupBy(x => x.Created.Date);

            foreach (var day in dayGrouping)
            {
                if (day.Count() > 1 && day.Any(x => !x.Prunable.Value))
                {
                    var mostRecentFirst = day.OrderByDescending(x => x.Created);

                    KeepFirst(mostRecentFirst);
                }
            }
        }

        /// <summary>
        /// Marks the the first sunday of the set as not prunable (or keep first backup in set if no sunday).
        /// </summary>
        /// <param name="set">The set.</param>
        public void KeepFirstSundayOrKeepOne(IEnumerable<BakModel> set)
        {
            // Oldest first (Created Ascending)
            var bakModels = set.OrderBy(x => x.Created)
                               .ToArray();

            if (bakModels.Count(x => x.Created.DayOfWeek == DayOfWeek.Sunday) > 1)
            {
                var sundayCount = 0;

                foreach (var bak in bakModels)
                {
                    if (bak.Created.DayOfWeek == DayOfWeek.Sunday && sundayCount == 0)
                    {
                        bak.Prunable = false;
                        sundayCount++;
                    }
                    else
                    {
                        bak.Prunable = true;
                    }
                }

            }
            else
            {
                KeepFirst(bakModels);
            }
        }

        public void KeepFirstSundayOfYear(IEnumerable<BakModel> set)
        {
            
        }

        /// <summary>
        /// Sets prunable backups in set.
        /// </summary>
        /// <param name="set">The set of backups for a given database.</param>
        /// <param name="now">The current date and time.</param>
        /// <returns></returns>
        public void FlagPrunableBackupsInSet(IEnumerable<BakModel> set, DateTime now)
        {
            // Make sure the set is ordered (and most recent first)
            set = set.OrderByDescending(x => x.Created);

            var startOfWeek = now.Date.StartOfWeek(DayOfWeek.Sunday);

            var aWeekFromNow = now.AddDays(-7).Date;
            var fourWeeksFromStartOfWeek = startOfWeek.AddDays(-7*4);
            var oneYearFromStartOfWeek = startOfWeek.AddYears(-1);

            var oneWeekOld = new List<BakModel>();
            var oneMonthOld = new List<BakModel>();
            var monthsOld = new List<BakModel>();
            var yearsOld = new List<BakModel>();

            string databaseName = null;

            foreach (var backup in set)
            {
                // Make sure there is only one database
                if (databaseName != backup.DatabaseName)
                {
                    if (databaseName != null && databaseName != backup.DatabaseName)
                    {
                        throw new ArgumentException("More than one database name in the set", "set");
                    }

                    databaseName = backup.DatabaseName;
                }

                if (backup.Created.Date >= aWeekFromNow)
                {
                    oneWeekOld.Add(backup);
                }
                else if (backup.Created.Date >= fourWeeksFromStartOfWeek && backup.Created.Date >= oneYearFromStartOfWeek)
                {
                    oneMonthOld.Add(backup);
                }
                else if(backup.Created.Date >= oneYearFromStartOfWeek.Date)
                {
                    monthsOld.Add(backup);
                }
                else
                {
                    yearsOld.Add(backup);
                }
            }

            // Keep all backups made in the last 7 days
            foreach (var backup in oneWeekOld)
            {
                backup.Prunable = false;
            }

            var calendar = new JulianCalendar();

            // Keep Sundays for a month after one week
            var weekGrouping =
                oneMonthOld.GroupBy(
                    x =>
                        new
                        {
                            x.Created.Year,
                            WeekNumber = calendar.GetWeekOfYear(x.Created, CalendarWeekRule.FirstDay, DayOfWeek.Sunday)
                        });
            foreach (var week in weekGrouping)
            {
                if (week.Count() > 1)
                {
                    KeepFirstSundayOrKeepOne(week);
                }
                else
                {
                    week.First().Prunable = false;
                }
            }

            // Keep first sunday of month after one week for one year
            var monthGrouping = monthsOld.GroupBy(x => new {x.Created.Year, x.Created.Month});
            foreach (var month in monthGrouping)
            {
                if (month.Count() > 1)
                {
                    KeepFirstSundayOrKeepOne(month);
                }
                else
                {
                    month.First().Prunable = false;
                }
            }

            // Keep first backup of each year after one year
            var yearGrouping = yearsOld.GroupBy(x => new {x.Created.Year});
            foreach (var year in yearGrouping)
            {
                if (year.Count() > 1)
                {
                    // Order year set ascending
                    KeepFirstSundayOrKeepOne(year);
                }
                else
                {
                    // Last of ordered by DESC = first
                    year.Last().Prunable = false;
                }
            }

            // Make sur only the most recent backup of 'Keeper' days is kept.
            KeepOnePerDay(set);
        }
    }
}
