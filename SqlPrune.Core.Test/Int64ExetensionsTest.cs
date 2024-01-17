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

            Assert.That(value.ToSizeWithSuffix(), Is.EqualTo("0"));
        }

        [Test]
        public void TestToSizeWithSuffixWhenTwoDigitsLong()
        {
            const long value = 12;

            Assert.That(value.ToSizeWithSuffix(), Is.EqualTo("12.0 bytes"));
        }

        [Test]
        public void TestToSizeWithSuffixWhenThreeDigitsLong()
        {
            const long value = 354;

            Assert.That(value.ToSizeWithSuffix(), Is.EqualTo("354.0 bytes"));
        }

        [Test]
        public void TestToSizeWithSuffixWhenFourDigitsLong()
        {
            const long value = 2400;

            Assert.That(value.ToSizeWithSuffix(), Is.EqualTo("2.3 KB"));
        }
    }
}
