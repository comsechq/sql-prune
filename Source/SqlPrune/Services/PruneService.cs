using System;
using System.Collections.Generic;
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
        /// Return true if the backup can be deleted/pruned.
        /// </summary>
        /// <param name="backupDatetime">The date and time the database was backed up at.</param>
        /// <param name="now">The current date time (now).</param>
        /// <returns>
        /// 	<c>true</c> if the specified snapshot is deletable; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsDeletable(DateTime backupDatetime, DateTime now)
        {
            var canPrune = true;

            var weekOld = now.AddDays(-7);
            var monthOld = now.AddDays(-30);

            if (backupDatetime >= weekOld /* Keep if less than a week old */)
            {
                canPrune = false;
            }
            else if (backupDatetime.Day == 1 /* 1st of the month */)
            {
                canPrune = false;
            }
            else if (backupDatetime.DayOfWeek == DayOfWeek.Sunday && backupDatetime >= monthOld /* Less than a month old */)
            {
                canPrune = false;
            }
            else if (backupDatetime.DayOfWeek == DayOfWeek.Sunday && backupDatetime < monthOld /* More than a month old */)
            {
                canPrune = true;
            }
            else if (backupDatetime < weekOld)
            {
                canPrune = true;
            }

            return canPrune;
        }

        /// <summary>
        /// Sets prunable backups in set.
        /// </summary>
        /// <param name="set">The set of backups for a given database.</param>
        /// <param name="now">The current date and time.</param>
        /// <returns></returns>
        public void FlagPrunableBackupsInSet(IEnumerable<BakModel> set, DateTime now)
        {
            var aWeekFromNow = now.AddDays(-7);
            var aMonthFromNow = now.AddMonths(-1);

            var weekOldSets = new List<BakModel>();
            var monthOldSets = new List<BakModel>();
            var yearOldSets = new List<BakModel>();

            foreach (var backup in set)
            {
                if (backup.Created > aWeekFromNow)
                {
                    weekOldSets.Add(backup);
                }
                else if(backup.Created > aMonthFromNow)
                {
                    monthOldSets.Add(backup);
                }
                else
                {
                    yearOldSets.Add(backup);
                }
            }

            // Keep one week worth of backups
            foreach (var backup in weekOldSets)
            {
                backup.Prunable = false;
            }

            //// If we have at least one sunday
            //if (monthOldSets.Count(x => x.Created.DayOfWeek == DayOfWeek.Sunday) > 0)
            //{
                foreach (var backup in monthOldSets)
                {
                    // Keep sundays only
                    backup.Prunable = backup.Created.DayOfWeek != DayOfWeek.Sunday;
                }
            //}

            foreach (var backup in yearOldSets)
            {
                // Keep sundays only
                backup.Prunable = backup.Created.DayOfWeek != DayOfWeek.Sunday;
            }
        }
    }
}
