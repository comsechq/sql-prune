using System;
using System.Linq;
using NUnit.Framework;

namespace Comsec.SqlPrune.Factories
{
    [TestFixture]
    public class BakFileListFactoryTest
    {
        private BakFileListFactory factory;

        [OneTimeSetUp]
        public void SetupOnce()
        {
            var names = new[] {"db1", "db2", "db3"};

            factory = new BakFileListFactory().WithDatabases(names);
        }

        [Test]
        public void TestCreateDigitString()
        {
            var result = BakFileListFactory.CreateDigitString(7);

            Assert.That(result, Is.Not.Null.Or.Empty);
            Assert.That(result.Length, Is.EqualTo(7));
        }

        [Test]
        public void TestGenerateList()
        {
            var from = new DateTime(2010, 11, 20, 1, 2, 3);

            // 3 years of backup files
            var result = factory.Create(from, 365 * 3);

            Assert.That(result.Count, Is.EqualTo(365 * 3 * 3));

            var first = result.First();

            Assert.That(first.Key.StartsWith("db1_backup_2010_11_20_010203_"), Is.True);
            Assert.That(first.Key.EndsWith(".bak"), Is.True);
            Assert.That(first.Value, Is.AtLeast(1));

            var last = result.Last();

            Assert.That(last.Key.StartsWith("db3_backup_2013_11_18_010203_"), Is.True);
            Assert.That(last.Key.EndsWith(".bak"), Is.True);
            Assert.That(last.Value, Is.AtLeast(1));
        }
    }
}