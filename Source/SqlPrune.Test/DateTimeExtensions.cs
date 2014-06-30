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
            Assert.AreEqual(new DateTime(2012, 1, 2), result);

            // Tuesday
            var result2 = new DateTime(2012, 1, 3).StartOfWeek(DayOfWeek.Monday);
            Assert.AreEqual(new DateTime(2012, 1, 2), result2);

            // Wednesday
            var result3 = new DateTime(2012, 1, 4).StartOfWeek(DayOfWeek.Monday);
            Assert.AreEqual(new DateTime(2012, 1, 2), result3);

            // Thursday
            var result4 = new DateTime(2012, 1, 5).StartOfWeek(DayOfWeek.Monday);
            Assert.AreEqual(new DateTime(2012, 1, 2), result4);

            // Friday
            var result5 = new DateTime(2012, 1, 6).StartOfWeek(DayOfWeek.Monday);
            Assert.AreEqual(new DateTime(2012, 1, 2), result5);

            // Saturday
            var result6 = new DateTime(2012, 1, 7).StartOfWeek(DayOfWeek.Monday);
            Assert.AreEqual(new DateTime(2012, 1, 2), result6);

            // Sunday
            var result7 = new DateTime(2012, 1, 8).StartOfWeek(DayOfWeek.Monday);
            Assert.AreEqual(new DateTime(2012, 1, 2), result7);

            // Sunday
            var result8 = new DateTime(2011, 12, 11).StartOfWeek(DayOfWeek.Monday);
            Assert.AreEqual(new DateTime(2011, 12, 5), result8);
        }
    }
}
