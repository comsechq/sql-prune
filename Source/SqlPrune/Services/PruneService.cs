using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using Amazon.Redshift.Model;
using Comsec.SqlPrune.Interfaces.Services;
using Comsec.SqlPrune.Models;

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
        /// Marks the the n matched day of the set as not prunable (or keep first backup in set if no matching day).
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="dayOfWeekToKeep">The day of week to keep.</param>
        /// <param name="weekNumberToKeepFromFirstOccurence">The occurence to keep (e.g. 0 to keep the first match).</param>
        public void KeepDayOccurences(IEnumerable<BakModel> set, DayOfWeek dayOfWeekToKeep, int weekNumberToKeepFromFirstOccurence)
        {
            KeepDayOccurences(set, dayOfWeekToKeep, new[] {weekNumberToKeepFromFirstOccurence});
        }

        /// <summary>
        /// Marks the the n matched day of the set as not prunable (or keep first backup in set if no matching day).
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="dayOfWeekToKeep">The day of week to keep (e.g. Sunday).</param>
        /// <param name="weekNumberToKeepFromFirstOccurence">The occurences to keep (e.g. for 1st and 3nd week: "new []{ 0, 2}").</param>
        /// <remarks>
        /// If you want to keep the first and third occurence (e.g. new []{0, 2}) but there are less matches, the best 'match' will be 
        /// </remarks>
        public void KeepDayOccurences(IEnumerable<BakModel> set, DayOfWeek dayOfWeekToKeep, int[] weekNumberToKeepFromFirstOccurence)
        {
            var processableBackupsInSet = new List<BakModel>();

            // Group by day an only keep most recent backup for each day
            foreach (var dayGrouping in set.GroupBy(x => x.Created.Date))
            {
                if (dayGrouping.Count() == 1)
                {
                    processableBackupsInSet.AddRange(dayGrouping);
                }
                else
                {
                    var index = 0;

                    foreach (var bak in dayGrouping.OrderByDescending(x => x.Created))
                    {
                        if (index == 0)
                        {
                            processableBackupsInSet.Add(bak);
                        }
                        else
                        {
                            // Prune backups in day that are not the most recent and ignore them for the following step
                            bak.Prunable = true;
                        }
                        
                        index++;
                    }
                }
            }

            // Prune non matches
            foreach (var bak in processableBackupsInSet.Where(x => x.Created.DayOfWeek != dayOfWeekToKeep))
            {
                bak.Prunable = true;
            }
            
            // Oldest first
            var potentialKeepers = processableBackupsInSet.Where(x => x.Created.DayOfWeek == dayOfWeekToKeep)
                                                          .OrderBy(x => x.Created)
                                                          .ToArray();

            if (potentialKeepers.Length == 0)
            {
                KeepFirst(processableBackupsInSet);
            }
            else if (potentialKeepers.Length == 1)
            {
                KeepFirst(potentialKeepers);
            }
            else if (potentialKeepers.Length == weekNumberToKeepFromFirstOccurence.Length)
            {
                foreach (var bak in potentialKeepers)
                {
                    bak.Prunable = false;
                }
            }
            else
            {
                // We'll remove values in this list as we find matches (so that we don't matches more than once)
                var occurencesToMatch = weekNumberToKeepFromFirstOccurence.ToList();

                DateTime? firstMatchCreationDate = null;

                foreach (var bak in potentialKeepers)
                {
                    if (bak.Created.DayOfWeek == dayOfWeekToKeep)
                    {
                        var numberOfWeeksSinceLastMatch = 0;

                        if (firstMatchCreationDate.HasValue)
                        {
                            var deltaDays = (bak.Created.Date - firstMatchCreationDate.Value.Date).Days;

                            numberOfWeeksSinceLastMatch = deltaDays / 7;
                        }
                        else
                        {
                            firstMatchCreationDate = bak.Created;
                        }

                        // Got that far an ran out of occurences to match: I haz can prune
                        if (occurencesToMatch.Count == 0)
                        {
                            bak.Prunable = true;
                        }

                        for (var i = occurencesToMatch.Count - 1; i >= 0; i--)
                        {
                            var occurence = occurencesToMatch[i];

                            // Match on an occurence equal or above what is wanted
                            bak.Prunable = !(numberOfWeeksSinceLastMatch >= occurence);

                            if (!bak.Prunable.Value)
                            {
                                occurencesToMatch.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    else
                    {
                        bak.Prunable = true;
                    }
                }
            }
        }

        /// <summary>
        /// Sets prunable backups in set.
        /// </summary>
        /// <param name="set">The set of backups for a given database.</param>
        /// <returns></returns>
        public void FlagPrunableBackupsInSet(IEnumerable<BakModel> set)
        {
            // Ignore set with 0 or 1 entries
            if (set.Count() < 2)
            {
                foreach (var backup in set)
                {
                    backup.Prunable = false;
                }

                return;
            }
            
            // Make sure the set is ordered (and most recent first)
            set = set.OrderByDescending(x => x.Created);

            var mostRecentBackupDate = set.First().Created;

            var startOfWeek = mostRecentBackupDate.Date.StartOfWeek(DayOfWeek.Sunday);

            var twoWeeksFromStart = mostRecentBackupDate.AddDays(-2 * 7).Date;
            var eightWeeksFromStart = startOfWeek.AddDays(-2 * 4 * 7).Date;
            var fiftyTwoWeeksFromStart = startOfWeek.AddDays(-52 * 7).Date;

            var keepOneDaily = new List<BakModel>();
            var keepFirstSundayWeekly = new List<BakModel>();
            var keepFirstAndThridSundayMonthly = new List<BakModel>();
            var keepFirstSundayYearly = new List<BakModel>();

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

                if (backup.Created.Date >= twoWeeksFromStart)
                {
                    keepOneDaily.Add(backup);
                }
                else if (backup.Created.Date >= eightWeeksFromStart && backup.Created.Date >= fiftyTwoWeeksFromStart)
                {
                    keepFirstSundayWeekly.Add(backup);
                }
                else if(backup.Created.Date >= fiftyTwoWeeksFromStart.Date)
                {
                    keepFirstAndThridSundayMonthly.Add(backup);
                }
                else
                {
                    keepFirstSundayYearly.Add(backup);
                }
            }

            // Keep all backups made in the last 7 days
            foreach (var backup in keepOneDaily)
            {
                backup.Prunable = false;
            }

            var calendar = new JulianCalendar();

            // Keep Sundays for a month
            var weekGrouping =
                keepFirstSundayWeekly.GroupBy(
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
                    KeepDayOccurences(week, DayOfWeek.Sunday, 0);
                }
                else
                {
                    week.First().Prunable = false;
                }
            }

            // Keep first and third Sunday of month
            var monthGrouping = keepFirstAndThridSundayMonthly.GroupBy(x => new {x.Created.Year, x.Created.Month});
            foreach (var month in monthGrouping)
            {
                if (month.Count() > 1)
                {
                    KeepDayOccurences(month, DayOfWeek.Sunday, new[] {0, 2});
                }
                else
                {
                    month.First().Prunable = false;
                }
            }

            // Keep first backup of each year
            var yearGrouping = keepFirstSundayYearly.GroupBy(x => new {x.Created.Year});
            foreach (var year in yearGrouping)
            {
                if (year.Count() > 1)
                {
                    // Order year set ascending
                    KeepDayOccurences(year, DayOfWeek.Sunday, 0);
                }
                else
                {
                    // Last of ordered by DESC = first
                    year.Last().Prunable = false;
                }
            }

            // Make sure only the most recent backup of 'Keeper' days is kept.
            KeepOnePerDay(set);
        }
    }
}
