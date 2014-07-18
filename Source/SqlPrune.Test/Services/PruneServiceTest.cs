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
        public void TestKeepFirstInSet()
        {
            var set = new[]
                      {
                          new BakModel(),
                          new BakModel()
                      };

            service.KeepFirst(set);

            Assert.IsFalse(set[0].Prunable.Value);
            Assert.IsTrue(set[1].Prunable.Value);
        }

        [Test]
        public void TestKeepOnePerDayWhenPrunabilityHasNotBeenAssestedYet()
        {
            var set = new[]
                      {
                          new BakModel {Created = new DateTime(2014, 6, 29, 12, 8, 0)},
                          new BakModel {Created = new DateTime(2014, 6, 29, 12, 40, 23)}
                      };

            service.KeepOnePerDay(set);

            Assert.IsFalse(set[0].Prunable.HasValue);
            Assert.IsFalse(set[1].Prunable.HasValue);
        }

        [Test]
        public void TestKeepOnePerDayButAllArePrunable()
        {
            var set = new[]
                      {
                          new BakModel {Created = new DateTime(2014, 6, 29, 12, 8, 0), Prunable = true},
                          new BakModel {Created = new DateTime(2014, 6, 29, 12, 40, 23), Prunable = true}
                      };

            service.KeepOnePerDay(set);

            Assert.IsTrue(set[0].Prunable.Value);
            Assert.IsTrue(set[1].Prunable.Value);
        }

        [Test]
        public void TestKeepOnePerDayOnDaysThatWeWantToKeepBackupsFor()
        {
            var set = new[]
                      {
                          new BakModel {Created = new DateTime(2014, 6, 28), Prunable = true},
                          new BakModel {Created = new DateTime(2014, 6, 29, 12, 8, 0), Prunable = false},
                          new BakModel {Created = new DateTime(2014, 6, 29, 12, 40, 23), Prunable = false},
                          new BakModel {Created = new DateTime(2014, 6, 30, 12, 9, 9), Prunable = false},
                          new BakModel {Created = new DateTime(2014, 6, 30, 6, 10, 10), Prunable = false}
                      };
            
            service.KeepOnePerDay(set);

            Assert.IsTrue(set[0].Prunable.Value);
            Assert.IsTrue(set[1].Prunable.Value);
            Assert.IsFalse(set[2].Prunable.Value);
            Assert.IsFalse(set[3].Prunable.Value);
            Assert.IsTrue(set[4].Prunable.Value);
        }

        [Test]
        public void TestKeepFirstSundayOrKeepOneWhenNoSunday()
        {
            var set = new List<BakModel>
                      {
                          // Tuesday
                          new BakModel {Created = new DateTime(2014, 6, 24)},
                          // Wednesday
                          new BakModel {Created = new DateTime(2014, 6, 25)}
                      };

            service.KeepFirstSundayOrKeepOne(set);
            
            Assert.IsFalse(set[0].Prunable.Value);
            Assert.IsTrue(set[1].Prunable.Value);
        }

        [Test]
        public void TestKeepFirstSundayOrKeepOneWithAtLeastOneSunday()
        {
            var set = new List<BakModel>
                      {
                          // Saturday
                          new BakModel {Created = new DateTime(2014, 6, 21)},
                          // Sunday
                          new BakModel {Created = new DateTime(2014, 6, 22)},
                          // Tuesday
                          new BakModel {Created = new DateTime(2014, 6, 24)},
                          // Wednesday
                          new BakModel {Created = new DateTime(2014, 6, 25)},
                          // Another Sunday
                          new BakModel {Created = new DateTime(2014, 6, 29)},
                      };

            service.KeepFirstSundayOrKeepOne(set);

            Assert.IsTrue(set[0].Prunable.Value);
            Assert.False(set[1].Prunable.Value);
            Assert.IsTrue(set[2].Prunable.Value);
            Assert.IsTrue(set[3].Prunable.Value);
            Assert.IsTrue(set[4].Prunable.Value);
        }

        [Test]
        public void TestPruneBackupSetWhenMoreThanOneDatabaseNameInSet()
        {
            var set = new List<BakModel>
                      {
                          new BakModel {DatabaseName = "db1", Created = new DateTime(2014, 7, 18, 9, 51, 0)},
                          new BakModel {DatabaseName = "db2", Created = new DateTime(2014, 7, 17, 9, 40, 0)}
                      };

            Assert.Throws<ArgumentException>(() => service.FlagPrunableBackupsInSet(set));
        }

        /// <summary>
        /// Generates a backup set.
        /// </summary>
        /// <param name="from">Date to generate backups from.</param>
        /// <param name="howManyDays">The how many days to go in the past from the <see cref="from"/> date (must be a positive value).</param>
        /// <returns></returns>
        private List<BakModel> GenerateBackupSet(DateTime from, int howManyDays)
        {
            var now = from;

            var paths = new BakFileListFactory().WithDatabases(new[] {"db1"})
                                                .Create(now.AddDays(-howManyDays), now);

            var backupSet = new List<BakModel>(paths.Count);

            foreach (var path in paths)
            {
                BakModel model;

                if (!BakFilenameExtractor.ValidateFilenameAndExtract(path, out model)) continue;

                backupSet.Add(model);
            }

            return backupSet;
        }

        /// <summary>
        /// Renders the modified backup set to visualise which backups will be pruned or not.
        /// Hint: Open Calendar.html at in this project.
        /// </summary>
        /// <param name="backupSet">The backup set.</param>
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

            var json = JsonConvert.SerializeObject(calendarModel, Formatting.Indented);

            File.WriteAllText(@"..\..\prune-test-data.json", json);
        }

        [Test]
        public void TestPruneBackupSetWhenFirstRun()
        {
            var backupSet = GenerateBackupSet(new DateTime(2014, 6, 27, 11, 54, 10), 1500);

            service.FlagPrunableBackupsInSet(backupSet);

            RenderPrunedData(backupSet);

            Assert.AreEqual(0, backupSet.Count(x => x.Prunable == null));
            Assert.AreEqual(28, backupSet.Count(x => !x.Prunable.Value));
            Assert.AreEqual(1472, backupSet.Count(x => x.Prunable.Value));

            // Year
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2010, 5, 23)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2011, 1, 2)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2012, 1, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 1, 6)).Prunable.Value);

            // One year from now
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 6, 23)).Prunable.Value);

            // Month
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 7, 7)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 8, 4)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 9, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 10, 6)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 11, 3)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 12, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 1, 5)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 2, 2)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 3, 2)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 4, 6)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 5, 4)).Prunable.Value);
            
            // Four weeks from now
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 5, 25)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 5, 28)).Prunable.Value);

            // One Sunday per week for a month
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 8)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 15)).Prunable.Value);

            //// Week
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 20)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 20)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 21)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 22)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 23)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 24)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 25)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 26)).Prunable.Value);
        }

        [Test]
        public void TestPruneBackupSetWhenPruningAlreadyHappendedBefore()
        {
            var before = new DateTime(2014, 6, 27, 11, 54, 10);

            var backupSet = GenerateBackupSet(before, 1500);

            service.FlagPrunableBackupsInSet(backupSet);

            backupSet.RemoveAll(x => x.Prunable == true);

            var now = new DateTime(2014, 8, 9, 12, 40, 20);

            var newBackups = GenerateBackupSet(now, (now - before).Days);

            backupSet.AddRange(newBackups);

            service.FlagPrunableBackupsInSet(backupSet);

            RenderPrunedData(backupSet);

            Assert.AreEqual(0, backupSet.Count(x => x.Prunable == null));
            Assert.AreEqual(28, backupSet.Count(x => !x.Prunable.Value));
            Assert.AreEqual(43, backupSet.Count(x => x.Prunable.Value));

            // Year
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2010, 5, 23)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2011, 1, 2)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2012, 1, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 1, 6)).Prunable.Value);

            // Month
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 8, 4)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 9, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 10, 6)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 11, 3)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 12, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 1, 5)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 2, 2)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 3, 2)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 4, 6)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 5, 4)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 1)).Prunable.Value);

            // One Sunday per week for a month
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 13)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 20)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 27)).Prunable.Value);

            //// Week
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 8, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 8, 2)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 8, 3)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 8, 4)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 8, 5)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 8, 5)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 8, 6)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 8, 7)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 8, 8)).Prunable.Value);

            // Pruned
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2013, 6, 23)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2013, 7, 7)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 5, 25)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 8)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 15)).Prunable.Value);

            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 20)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 21)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 22)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 23)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 24)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 25)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 26)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 30)).Prunable.Value);

            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 2)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 3)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 4)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 5)).Prunable.Value);

            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 7)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 8)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 9)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 10)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 11)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 12)).Prunable.Value);

            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 14)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 15)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 16)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 17)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 18)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 19)).Prunable.Value);

            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 21)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 22)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 23)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 24)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 25)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 26)).Prunable.Value);

            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 28)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 29)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 30)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 31)).Prunable.Value);
        }

        [Test]
        public void TestPruneBackupSetWhenPruningAlreadyHappenedALongTimeBefore()
        {
            var before = new DateTime(2014, 6, 27, 11, 54, 10);

            var backupSet = GenerateBackupSet(before, 1500);

            service.FlagPrunableBackupsInSet(backupSet);

            backupSet.RemoveAll(x => x.Prunable == true);

            var now = new DateTime(2015, 1, 5, 1, 0, 20);

            var newBackups = GenerateBackupSet(now, (now - before).Days);

            backupSet.AddRange(newBackups);

            service.FlagPrunableBackupsInSet(backupSet);

            Assert.AreEqual(0, backupSet.Count(x => x.Prunable == null));
            Assert.AreEqual(27, backupSet.Count(x => !x.Prunable.Value));
            Assert.AreEqual(192, backupSet.Count(x => x.Prunable.Value));

            RenderPrunedData(backupSet);
        }

        [Test]
        public void TestPrunBackupSetWhenMoreThanOneBackupInOneDayOnDayToKeep()
        {
            var now = new DateTime(2014, 2, 28, 21, 15, 0);

            var backupSet = GenerateBackupSet(now, 60);

            service.FlagPrunableBackupsInSet(backupSet);

            Assert.IsFalse(backupSet[0].Prunable.Value);

            var duplicate = new BakModel {DatabaseName = "db1", Created = new DateTime(2013, 12, 30, 22, 30, 5)};

            backupSet.Add(duplicate);
            
            service.FlagPrunableBackupsInSet(backupSet);

            Assert.IsTrue(backupSet[0].Prunable.Value);
            Assert.IsFalse(duplicate.Prunable.Value);
        }

        [Test]
        public void TestFlagPrunableBackupsInSetWhenMoreThanOneBackupInOneDayOnDayNotToKeep()
        {
            var now = new DateTime(2014, 2, 28, 11, 54, 10);

            var backupSet = GenerateBackupSet(now, 60);

            backupSet.Add(new BakModel { DatabaseName = "db1", Created = new DateTime(2013, 12, 31, 22, 30, 5) });

            service.FlagPrunableBackupsInSet(backupSet);

            Assert.IsTrue(backupSet.Last().Prunable.Value);
        }
    }
}
