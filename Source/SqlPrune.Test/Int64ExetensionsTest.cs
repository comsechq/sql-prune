using NUnit.Framework;

namespace Comsec.SqlPrune
{
    [TestFixture]
    public class Int64ExtensionsTest
    {
        [Test]
        public void TestToSizeWithSuffixWhenZero()
        {
            const long value = 0;

            Assert.AreEqual("0", value.ToSizeWithSuffix());
        }

        [Test]
        public void TestToSizeWithSuffixWhenTwoDigitsLong()
        {
            const long value = 12;

            Assert.AreEqual("12.0 bytes", value.ToSizeWithSuffix());
        }

        [Test]
        public void TestToSizeWithSuffixWhenThreeDigitsLong()
        {
            const long value = 354;

            Assert.AreEqual("354.0 bytes", value.ToSizeWithSuffix());
        }

        [Test]
        public void TestToSizeWithSuffixWhenFourDigitsLong()
        {
            const long value = 2400;

            Assert.AreEqual("2.3 KB", value.ToSizeWithSuffix());
        }
    }
}
