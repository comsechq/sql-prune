using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
        public void TestKeepDayOccurencesWhenNoSunday()
        {
            var set = new List<BakModel>
                      {
                          // Tuesday
                          new BakModel {Created = new DateTime(2014, 6, 24)},
                          // Wednesday
                          new BakModel {Created = new DateTime(2014, 6, 25)}
                      };

            service.KeepDayOccurrences(set, DayOfWeek.Sunday, 0);
            
            Assert.IsFalse(set[0].Prunable.Value);
            Assert.IsTrue(set[1].Prunable.Value);
        }

        [Test]
        public void TestKeepDayOccurencesWithAtLeastOneSunday()
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

            service.KeepDayOccurrences(set, DayOfWeek.Sunday, 0);

            Assert.IsTrue(set[0].Prunable.Value);
            Assert.IsFalse(set[1].Prunable.Value);
            Assert.IsTrue(set[2].Prunable.Value);
            Assert.IsTrue(set[3].Prunable.Value);
            Assert.IsTrue(set[4].Prunable.Value);
        }

        [Test]
        public void TestKeepFirstAndThridSundayInPeriod()
        {
            var set = new List<BakModel>
                      {
                          // Sunday 1
                          new BakModel {Created = new DateTime(2012, 7, 1)},
                          // Sunday 2
                          new BakModel {Created = new DateTime(2012, 7, 8)},
                          // Sunday 3
                          new BakModel {Created = new DateTime(2012, 7, 15)},
                          // Sunday 4
                          new BakModel {Created = new DateTime(2012, 7, 22)},
                          // Sunday 5
                          new BakModel {Created = new DateTime(2012, 7, 29)},
                      };

            service.KeepDayOccurrences(set, DayOfWeek.Sunday, new[] {0, 2});

            Assert.IsFalse(set[0].Prunable.Value);
            Assert.IsTrue(set[1].Prunable.Value);
            Assert.IsFalse(set[2].Prunable.Value);
            Assert.IsTrue(set[3].Prunable.Value);
            Assert.IsTrue(set[4].Prunable.Value);
        }

        [Test]
        public void TestKeepFirstAndThridSundayInPeriodWhenAlreadyPrunedThatMonthBefore()
        {
            var set = new List<BakModel>
                      {
                          // Sunday 1
                          new BakModel {Created = new DateTime(2012, 7, 1)},
                          // Sunday 2
                          // Previously pruned
                          // Sunday 3
                          new BakModel {Created = new DateTime(2012, 7, 15)},
                          // Sunday 4
                          // Previously pruned
                          // Sunday 5
                          // Previously pruned
                      };

            service.KeepDayOccurrences(set, DayOfWeek.Sunday, new[] {0, 2});

            Assert.IsFalse(set[0].Prunable.Value);
            Assert.IsFalse(set[1].Prunable.Value);
        }

        [Test]
        public void TestKeepFirstAndThridSundayInPeriodWhenAlreadyPrunedThatMonthBeforeWithMoreThanOneBackupInOneDay()
        {
            var set = new List<BakModel>
                      {
                          // Sunday 1 @ 06:00
                          new BakModel {Created = new DateTime(2012, 7, 1, 6, 0, 0)},
                          // Sunday 1 @ 07:00
                          new BakModel {Created = new DateTime(2012, 7, 1, 7, 0, 0)},
                          // Sunday 1 @ 08:00
                          new BakModel {Created = new DateTime(2012, 7, 1, 8, 0, 0)},
                          // Sunday 2
                          // Previously pruned
                          // Sunday 3
                          new BakModel {Created = new DateTime(2012, 7, 15)},
                          // Sunday 4
                          // Previously pruned
                          // Sunday 5
                          // Previously pruned
                      };

            service.KeepDayOccurrences(set, DayOfWeek.Sunday, new[] {0, 2});

            Assert.IsTrue(set[0].Prunable.Value);
            Assert.IsTrue(set[1].Prunable.Value);
            Assert.IsFalse(set[2].Prunable.Value);
            Assert.IsFalse(set[3].Prunable.Value);
        }

        [Test]
        public void TestKeepFirstAndThirdSundayWhenProcessingAMonthWithBackupsOnlyForTheEndOfTheMonth()
        {
            var paths = ReadAllFileList();

            var set = InitaliseBackupSet(paths).Where(x => x.Created >= new DateTime(2013, 4, 1))
                                               .Where(x => x.Created <= new DateTime(2013, 5, 1))
                                               .ToArray();

            service.KeepDayOccurrences(set, DayOfWeek.Sunday, new[] {0, 2});

            Assert.AreEqual(2, set.Count(x => x.Prunable.HasValue && !x.Prunable.Value));
            Assert.AreEqual(12, set.Count(x => x.Prunable.HasValue && x.Prunable.Value));

            Assert.IsFalse(set[4].Prunable.Value);
            Assert.IsFalse(set[11].Prunable.Value);
        }

        [Test]
        public void TestKeepFirstAndThirdSundayWhenProcessingAMonthWithSundayGapBiggerThanOneWeek()
        {
            var paths = ReadAllFileList();

            var set = InitaliseBackupSet(paths).Where(x => x.Created >= new DateTime(2013, 12, 1))
                                               .Where(x => x.Created <= new DateTime(2014, 1, 1))
                                               .ToArray();

            service.KeepDayOccurrences(set, DayOfWeek.Sunday, new[] { 0, 2 });

            Assert.AreEqual(2, set.Count(x => x.Prunable.HasValue && !x.Prunable.Value));
            Assert.AreEqual(12, set.Count(x => x.Prunable.HasValue && x.Prunable.Value));

            Assert.IsFalse(set[0].Prunable.Value);
            Assert.IsFalse(set[4].Prunable.Value);
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
        private static List<BakModel> GenerateBackupSet(DateTime from, int howManyDays)
        {
            var now = from;

            var paths = new BakFileListFactory().WithDatabases(new[] {"db1"})
                                                .Create(now.AddDays(-howManyDays), now);

            return InitaliseBackupSet(paths.Keys);
        }

        /// <summary>
        /// Reads all content of the test file list and splits it into a string[].
        /// </summary>
        /// <returns></returns>
        private static string[] ReadAllFileList()
        {
            var listing = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\Samples\test-files.txt"));

            return Regex.Split(listing, Environment.NewLine);
        }

        /// <summary>
        /// Initalises the backup set.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns></returns>
        private static List<BakModel> InitaliseBackupSet(IEnumerable<string> paths)
        {
            var backupSet = new List<BakModel>();

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
        public void TestFlagPrunableBackupsInSetWhenFirstRun()
        {
            var backupSet = GenerateBackupSet(new DateTime(2014, 6, 27, 11, 54, 10), 1500);

            service.FlagPrunableBackupsInSet(backupSet);

            RenderPrunedData(backupSet);

            Assert.AreEqual(0, backupSet.Count(x => x.Prunable == null));
            Assert.AreEqual(48, backupSet.Count(x => !x.Prunable.Value));
            Assert.AreEqual(1452, backupSet.Count(x => x.Prunable.Value));

            // First backup in the year
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2010, 5, 23)).Prunable.Value);

            // First Sunday of each year
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2011, 1, 2)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2012, 1, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 1, 6)).Prunable.Value);

            // First and thrid Sunday of each month (or at least two backups in the month)
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 6, 23)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 6, 30)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 7, 7)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 7, 21)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 8, 4)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 8, 18)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 9, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 9, 15)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 10, 6)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 10, 20)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 11, 3)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 11, 17)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 12, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 12, 15)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 1, 5)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 1, 19)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 2, 2)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 2, 16)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 3, 2)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 3, 16)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 4, 6)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 4, 20)).Prunable.Value);
            
            // One Sunday per week
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 4, 27)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 5, 4)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 5, 11)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 5, 18)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 5, 25)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 8)).Prunable.Value);

            // Daily for two weeks 
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 12)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 13)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 14)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 15)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 16)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 17)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 18)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 19)).Prunable.Value);
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
        public void TestFlagPrunableBackupsInSetWhenPruningAlreadyHappendedBefore()
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
            Assert.AreEqual(47, backupSet.Count(x => !x.Prunable.Value));
            Assert.AreEqual(44, backupSet.Count(x => x.Prunable.Value));

            // First backup in the year
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2010, 5, 23)).Prunable.Value);

            // First Sunday of each year
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2011, 1, 2)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2012, 1, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2013, 1, 6)).Prunable.Value);

            // First and third Sunday of each month
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

            // One Sunday per week
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 1)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 8)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 15)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 22)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 29)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 6)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 13)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 20)).Prunable.Value);
            
            // Daily for two weeks
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 25)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 26)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 27)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 28)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 29)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 30)).Prunable.Value);
            Assert.IsFalse(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 31)).Prunable.Value);
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
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2013, 7, 21)).Prunable.Value);

            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 4, 27)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 5, 11)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 5, 25)).Prunable.Value);
            
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 12)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 13)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 14)).Prunable.Value);
            
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 16)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 17)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 18)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 19)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 20)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 21)).Prunable.Value);

            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 23)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 24)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 25)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 26)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 27)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 28)).Prunable.Value);

            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 6, 30)).Prunable.Value);
            Assert.IsTrue(backupSet.Single(x => x.Created.Date == new DateTime(2014, 7, 1)).Prunable.Value);
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
        }

        [Test]
        public void TestFlagPrunableBackupsInSetWhenPruningAlreadyHappenedALongTimeBefore()
        {
            var before = new DateTime(2014, 6, 27, 11, 54, 10);

            var backupSet = GenerateBackupSet(before, 1500);

            service.FlagPrunableBackupsInSet(backupSet);

            backupSet.RemoveAll(x => x.Prunable == true);

            var now = new DateTime(2015, 1, 5, 1, 0, 20);

            var newBackups = GenerateBackupSet(now, (now - before).Days);

            backupSet.AddRange(newBackups);

            service.FlagPrunableBackupsInSet(backupSet);

            RenderPrunedData(backupSet);

            Assert.AreEqual(0, backupSet.Count(x => x.Prunable == null));
            Assert.AreEqual(46, backupSet.Count(x => !x.Prunable.Value));
            Assert.AreEqual(193, backupSet.Count(x => x.Prunable.Value));
        }

        [Test]
        public void TestFlagPrunableBackupsInSetWhenMoreThanOneBackupInOneDayOnDayToKeep()
        {
            var now = new DateTime(2014, 2, 28, 21, 15, 0);

            var backupSet = GenerateBackupSet(now, 120);

            service.FlagPrunableBackupsInSet(backupSet);

            var duplicate = new BakModel {DatabaseName = "db1", Created = new DateTime(2013, 11, 17, 22, 30, 5)};

            backupSet.Add(duplicate);
            
            service.FlagPrunableBackupsInSet(backupSet);

            RenderPrunedData(backupSet);

            var keeperDay = backupSet.Where(x => x.Created.Date == new DateTime(2013, 11, 17))
                                       .ToArray();

            Assert.IsTrue(keeperDay[0].Prunable.Value);
            Assert.IsFalse(keeperDay[1].Prunable.Value);
        }

        [Test]
        public void TestFlagPrunableBackupsInSetWhenMoreThanOneBackupInOneDayOnDayNotToKeep()
        {
            var mostRecentBackup = new DateTime(2014, 2, 28, 11, 54, 10);

            var backupSet = GenerateBackupSet(mostRecentBackup, 120);

            backupSet.Add(new BakModel { DatabaseName = "db1", Created = new DateTime(2013, 12, 8, 22, 30, 5) });

            service.FlagPrunableBackupsInSet(backupSet);

            RenderPrunedData(backupSet);

            var pruningDay = backupSet.Where(x => x.Created.Date == new DateTime(2013, 12, 8))
                                      .ToArray();

            Assert.IsTrue(pruningDay[0].Prunable.Value);
            Assert.IsTrue(pruningDay[1].Prunable.Value);
        }

        [Test]
        public void TestFlagPrunableBackupsInSetWithARealFileList()
        {
            var paths = ReadAllFileList();

            var backupSet = InitaliseBackupSet(paths);

            service.FlagPrunableBackupsInSet(backupSet);

            RenderPrunedData(backupSet);

            Assert.AreEqual(0, backupSet.Count(x => x.Prunable == null));
            Assert.AreEqual(41, backupSet.Count(x => !x.Prunable.Value));
            Assert.AreEqual(340, backupSet.Count(x => x.Prunable.Value));
        }
    }
}
