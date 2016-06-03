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
            
            Assert.AreEqual(4, results.Length);
            Assert.AreEqual("file2.bak", results[0]);
            Assert.AreEqual("file4.bak", results[1]);
            Assert.AreEqual("file5.bak.7z", results[2]);
            Assert.AreEqual("exact_match.txt", results[3]);
        }
    }
}