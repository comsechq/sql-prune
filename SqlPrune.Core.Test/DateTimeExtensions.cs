using System;
using NUnit.Framework;

namespace Comsec.SqlPrune
{
    [TestFixture]
    public class DateTimeExtensions
    {
        [Test]
        public void TestStartOfWeek()
        {
            // Monday
            var result = new DateTime(2012, 1, 2).StartOfWeek(DayOfWeek.Monday);
            Assert.That(result, Is.EqualTo(new DateTime(2012, 1, 2)));

            // Tuesday
            var result2 = new DateTime(2012, 1, 3).StartOfWeek(DayOfWeek.Monday);
            Assert.That(result2, Is.EqualTo(new DateTime(2012, 1, 2)));

            // Wednesday
            var result3 = new DateTime(2012, 1, 4).StartOfWeek(DayOfWeek.Monday);
            Assert.That(result3, Is.EqualTo(new DateTime(2012, 1, 2)));

            // Thursday
            var result4 = new DateTime(2012, 1, 5).StartOfWeek(DayOfWeek.Monday);
            Assert.That(result4, Is.EqualTo(new DateTime(2012, 1, 2)));

            // Friday
            var result5 = new DateTime(2012, 1, 6).StartOfWeek(DayOfWeek.Monday);
            Assert.That(result5, Is.EqualTo(new DateTime(2012, 1, 2)));

            // Saturday
            var result6 = new DateTime(2012, 1, 7).StartOfWeek(DayOfWeek.Monday);
            Assert.That(result6, Is.EqualTo(new DateTime(2012, 1, 2)));

            // Sunday
            var result7 = new DateTime(2012, 1, 8).StartOfWeek(DayOfWeek.Monday);
            Assert.That(result7, Is.EqualTo(new DateTime(2012, 1, 2)));

            // Sunday
            var result8 = new DateTime(2011, 12, 11).StartOfWeek(DayOfWeek.Monday);
            Assert.That(result8, Is.EqualTo(new DateTime(2011, 12, 5)));
        }
    }
}
