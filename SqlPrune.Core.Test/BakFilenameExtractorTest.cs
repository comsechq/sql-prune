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

            Assert.That(BakFilenameExtractor.ValidateFilenameAndExtract(null, out dbName, out created), Is.False);
            Assert.That(dbName, Is.Null);
            Assert.That(created, Is.EqualTo(DateTime.MinValue));

            Assert.That(BakFilenameExtractor.ValidateFilenameAndExtract("", out dbName, out created), Is.False);
            Assert.That(dbName, Is.Null);
            Assert.That(created, Is.EqualTo(DateTime.MinValue));
        }
        
        [Test]
        public void TestValidateFilenameAndExtractDateWhenNoUnderscores()
        {
            string dbName;
            DateTime created;

            Assert.That(BakFilenameExtractor.ValidateFilenameAndExtract("dbname1.bak", out dbName, out created), Is.False);
            Assert.That(dbName, Is.Null);
            Assert.That(created, Is.EqualTo(DateTime.MinValue));

            Assert.That(BakFilenameExtractor.ValidateFilenameAndExtract("dbname2_backup.bak", out dbName, out created), Is.False);
            Assert.That(dbName, Is.Null);
            Assert.That(created, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void TestValidateFilenameAndExtractWhenDatabaseNameContainsUnderscores()
        {
            string dbName;
            DateTime created;

            Assert.That(BakFilenameExtractor.ValidateFilenameAndExtract("db_name_backup_2014_03_29_010006_1882358.bak", out dbName, out created), Is.True);

            Assert.That(dbName, Is.EqualTo("db_name"));
            Assert.That(created.ToUniversalTime(), Is.EqualTo(new DateTime(2014, 3, 29, 01, 0, 6)));

            Assert.That(BakFilenameExtractor.ValidateFilenameAndExtract("db_name_with_loads_of_underscores_backup_2014_04_29_010006_1882358.bak", out dbName, out created), Is.True);

            Assert.That(dbName, Is.EqualTo("db_name_with_loads_of_underscores"));
            Assert.That(created.ToUniversalTime(), Is.EqualTo(new DateTime(2014, 4, 29, 01, 0, 6)));
        }

        [Test]
        public void TestValidateFilenameAndExtractDateIsValid()
        {
            string dbName;
            DateTime created;

            Assert.That(BakFilenameExtractor.ValidateFilenameAndExtract("dbname1_backup_2014_03_29_010006_1882358.bak", out dbName, out created), Is.True);
            Assert.That(dbName, Is.EqualTo("dbname1"));
            Assert.That(created, Is.EqualTo(new DateTime(2014, 3, 29, 1, 0, 6)));

            Assert.That(BakFilenameExtractor.ValidateFilenameAndExtract("dbname2_backup_2014_12_31_225906_1882358.BAK", out dbName, out created), Is.True);
            Assert.That(dbName, Is.EqualTo("dbname2"));
            Assert.That(created, Is.EqualTo(new DateTime(2014, 12, 31, 22, 59, 6)));
        }

        [Test]
        public void TestValidateFilenameAndExtractDateIsValidWithModel()
        {
            BakModel model;

            Assert.That(BakFilenameExtractor.ValidateFilenameAndExtract("dbname1_backup_2014_03_29_010006_1882358.bak", out model), Is.True);
            Assert.That(model.DatabaseName, Is.EqualTo("dbname1"));
            Assert.That(model.Created, Is.EqualTo(new DateTime(2014, 3, 29, 1, 0, 6)));
        }
    }
}
