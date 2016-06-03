using System;
using Comsec.SqlPrune.Models;
using NUnit.Framework;

namespace Comsec.SqlPrune
{
    [TestFixture]
    public class BakFilenameExtractorTest
    {
        [Test]
        public void TestValidateFilenameAndExtractDateWhenNullOrEmpty()
        {
            string dbName;
            DateTime created;

            Assert.IsFalse(BakFilenameExtractor.ValidateFilenameAndExtract(null, out dbName, out created));
            Assert.IsNull(dbName);
            Assert.AreEqual(DateTime.MinValue, created);

            Assert.IsFalse(BakFilenameExtractor.ValidateFilenameAndExtract("", out dbName, out created));
            Assert.IsNull(dbName);
            Assert.AreEqual(DateTime.MinValue, created);
        }

        [Test]
        public void TestValidateFilenameAndExtractDateWhenNotEndingWithDotBak()
        {
            string dbName;
            DateTime created;

            Assert.IsFalse(BakFilenameExtractor.ValidateFilenameAndExtract("dbname1_backup_2014_03_29_010006_1882358", out dbName, out created));
            Assert.IsNull(dbName);
            Assert.AreEqual(DateTime.MinValue, created);
        }

        [Test]
        public void TestValidateFilenameAndExtractDateWhenNoUnderscores()
        {
            string dbName;
            DateTime created;

            Assert.IsFalse(BakFilenameExtractor.ValidateFilenameAndExtract("dbname1.bak", out dbName, out created));
            Assert.IsNull(dbName);
            Assert.AreEqual(DateTime.MinValue, created);

            Assert.IsFalse(BakFilenameExtractor.ValidateFilenameAndExtract("dbname2_backup.bak", out dbName, out created));
            Assert.IsNull(dbName);
            Assert.AreEqual(DateTime.MinValue, created);
        }

        [Test]
        public void TestValidateFilenameAndExtractWhenDatabaseNameContainsUnderscrores()
        {
            string dbName;
            DateTime created;

            Assert.True(BakFilenameExtractor.ValidateFilenameAndExtract("db_name_backup_2014_03_29_010006_1882358.bak", out dbName, out created));

            Assert.AreEqual("db_name", dbName);
            Assert.AreEqual(new DateTime(2014, 3, 29, 01, 0, 6), created.ToUniversalTime());

            Assert.True(BakFilenameExtractor.ValidateFilenameAndExtract("db_name_with_loads_of_underscores_backup_2014_04_29_010006_1882358.bak", out dbName, out created));

            Assert.AreEqual("db_name_with_loads_of_underscores", dbName);
            Assert.AreEqual(new DateTime(2014, 4, 29, 01, 0, 6), created.ToUniversalTime());
        }

        [Test]
        public void TestValidateFilenameAndExtractDateIsValid()
        {
            string dbName;
            DateTime created;

            Assert.IsTrue(BakFilenameExtractor.ValidateFilenameAndExtract("dbname1_backup_2014_03_29_010006_1882358.bak", out dbName, out created));
            Assert.AreEqual("dbname1", dbName);
            Assert.AreEqual(new DateTime(2014, 3, 29, 1, 0, 6), created);

            Assert.IsTrue(BakFilenameExtractor.ValidateFilenameAndExtract("dbname2_backup_2014_12_31_225906_1882358.BAK", out dbName, out created));
            Assert.AreEqual("dbname2", dbName);
            Assert.AreEqual(new DateTime(2014, 12, 31, 22, 59, 6), created);
        }

        [Test]
        public void TestValidateFilenameAndExtractDateIsValidWithModel()
        {
            BakModel model;

            Assert.IsTrue(BakFilenameExtractor.ValidateFilenameAndExtract("dbname1_backup_2014_03_29_010006_1882358.bak", out model));
            Assert.AreEqual("dbname1", model.DatabaseName);
            Assert.AreEqual(new DateTime(2014, 3, 29, 1, 0, 6), model.Created);
        }
    }
}
