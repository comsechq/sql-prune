using System.Threading.Tasks;
using Comsec.SqlPrune.Logging;
using Comsec.SqlPrune.Providers;
using Moq;
using NUnit.Framework;

namespace Comsec.SqlPrune.Services.Providers
{
    [TestFixture]
    public class LocalFilesystemProviderTest
    {
        private LocalFileSystemProvider provider;

        [SetUp]
        public void Setup()
        {
            provider = new LocalFileSystemProvider(new Mock<ILogger>().Object);
        }

        [Test]
        public void TestShouldRun()
        {
            Assert.IsTrue(provider.ShouldRun("d:"));
            Assert.IsTrue(provider.ShouldRun(@"\folder\on\current\drive"));
            Assert.IsTrue(provider.ShouldRun(@"d:\"));
            Assert.IsTrue(provider.ShouldRun(@"d:\folder"));
        }

        [Test]
        public void TestShouldNotRun()
        {
            Assert.IsFalse(provider.ShouldRun(@"\\network-share\folder"));
            Assert.IsFalse(provider.ShouldRun(@"smb://boo"));
            Assert.IsFalse(provider.ShouldRun(@"s3://boo"));
            Assert.IsFalse(provider.ShouldRun(@"ftp://boo"));
            Assert.IsFalse(provider.ShouldRun(@"http://boo"));
        }

        [Test]
        public void TestExtractFilenameFromPath()
        {
            var result = provider.ExtractFilenameFromPath(@"E:\folder\sub\file.ext");

            Assert.AreEqual("file.ext", result);
        }

        [Test]
        public async Task TestGetFileSizeWhenFileDoesNotExist()
        {
            var result = await provider.GetFileSize(@"C:\this\folder\should\not\exists\and\this\file.either");

            Assert.AreEqual(-1, result);
        }
    }
}
