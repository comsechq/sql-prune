using System.Linq;
using NUnit.Framework;

namespace Comsec.SqlPrune.Extensions
{
    [TestFixture]
    public class MatchingExtensionsTest
    {
        private readonly string[] values =
        {
            "file1.txt", "file2.bak", "file3.backup",
            "file4.bak", "file5.bak.7z", "exact_match.txt"
        };
        
        [Test]
        public void TestGetFilesByExtensions()
        {
            var results = values.MatchOnAny(x => x, "*bak", "file4*", ".bak.7z", "exact_match.txt")
                                .ToArray();
            
            Assert.That(results.Length, Is.EqualTo(4));
            Assert.That(results[0], Is.EqualTo("file2.bak"));
            Assert.That(results[1], Is.EqualTo("file4.bak"));
            Assert.That(results[2], Is.EqualTo("file5.bak.7z"));
            Assert.That(results[3], Is.EqualTo("exact_match.txt"));
        }
    }
}