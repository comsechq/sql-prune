using System;
using System.Linq;
using NUnit.Framework;

namespace Comsec.SqlPrune.Factories
{
    [TestFixture]
    public class BakFileListFactoryTest
    {
        private BakFileListFactory factory;

        [TestFixtureSetUp]
        public void SetupOnce()
        {
            var names = new[] {"db1", "db2", "db3"};

            factory = new BakFileListFactory().WithDatabases(names);
        }

        [Test]
        public void TestCreateDigitString()
        {
            var result = BakFileListFactory.CreateDigitString(7);

            Assert.IsNotNullOrEmpty(result);
            Assert.AreEqual(7, result.Length);
        }

        [Test]
        public void TestGenerateList()
        {
            var from = new DateTime(2010, 11, 20, 1, 2, 3);

            // 3 years of backup files
            var result = factory.Create(from, 365 * 3);

            Assert.AreEqual(365 * 3 * 3, result.Count);

            var first = result.First();

            Assert.IsTrue(first.StartsWith("db1_backup_2010_11_20_010203_"));
            Assert.IsTrue(first.EndsWith(".bak"));

            var last = result.Last();

            Assert.IsTrue(last.StartsWith("db3_backup_2013_11_18_010203_"));
            Assert.IsTrue(last.EndsWith(".bak"));
        }
    }
}