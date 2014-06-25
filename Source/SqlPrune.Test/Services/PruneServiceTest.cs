using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Comsec.SqlPrune.Factories;
using Comsec.SqlPrune.Models;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Comsec.SqlPrune.Services
{
    [TestFixture]
    public class PruneServiceTest
    {
        private PruneService service;

        [SetUp]
        public void Setup()
        {
            service = new PruneService();
        }

        [Test]
        public void TestKeepSnapshotThatAreLessThanAWeekOld()
        {
            var now = DateTime.Now;

            var isDeletable = PruneService.IsDeletable(now, now);
            Assert.IsFalse(isDeletable);

            isDeletable = PruneService.IsDeletable(now.AddDays(-3), now);
            Assert.IsFalse(isDeletable);

            isDeletable = PruneService.IsDeletable(now.AddDays(-7).AddMinutes(1), now);
            Assert.IsFalse(isDeletable);
        }

        [Test]
        public void TestKeepSundaysForTheLast30Days()
        {
            var now = new DateTime(2011, 9, 1);

            while (now.DayOfWeek != DayOfWeek.Sunday)
            {
                now = now.AddDays(-1);
            }

            var isDeletable = PruneService.IsDeletable(now, now);
            Assert.IsFalse(isDeletable);

            isDeletable = PruneService.IsDeletable(now.AddDays(-7), now);
            Assert.IsFalse(isDeletable);

            isDeletable = PruneService.IsDeletable(now.AddDays(-31), now);
            Assert.IsTrue(isDeletable);
        }

        [Test]
        public void TestKeepFirstSnapshotOfTheMonth()
        {
            var first = DateTime.Now;
            while (first.Day != 1)
            {
                first = first.AddDays(-1);
            }

            var isDeletable = PruneService.IsDeletable(first.AddMonths(-1), first);

            Assert.IsFalse(isDeletable);
        }

        [Test]
        public void TestDeleteSnapshotOlderThanTwoMonths()
        {
            var now = new DateTime(2010, 10, 10);

            var isDeletable = PruneService.IsDeletable(now.AddMonths(-2), now);

            Assert.IsTrue(isDeletable);
        }

        [Test]
        public void TestPruneBackupSet()
        {
            var now = DateTime.Now;

            var paths = new BakFileListFactory().WithDatabases(new[] {"db1"})
                                                .Create(now.AddDays(-999), now);

            var backupSet = new List<BakModel>(paths.Count);

            foreach (var path in paths)
            {
                BakModel model;

                if (!BakFilenameExtractor.ValidateFilenameAndExtract(path, out model)) continue;

                backupSet.Add(model);
            }

            service.FlagPrunableBackupsInSet(backupSet, now);

            // TODO: Assert

            // Rendering the modified backup set to visualise which backups will be pruned or not.
            // Hint: Open Calendar.html at in this project.
            RenderPrunedData(backupSet);
        }

        private void RenderPrunedData(IList<BakModel> backupSet)
        {
            var calendarModel = new CalendarModel
                                {
                                    Values = backupSet.Select(x => new DateModel
                                                                   {
                                                                       Date = x.Created,
                                                                       Count = x.Prunable.HasValue && x.Prunable.Value ? 1 : 0,
                                                                   }),
                                    StartYear = backupSet.Min(x => x.Created).Year,
                                    EndYear = backupSet.Max(x => x.Created).Year,
                                    MinValue = 0,
                                    MaxValue = 3
                                };

            var json = JsonConvert.SerializeObject(calendarModel);

            File.WriteAllText(@"..\..\prune-test-data.json", json);
        }
    }
}
