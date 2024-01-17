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
            Assert.That(provider.ShouldRun("d:"), Is.True);
            Assert.That(provider.ShouldRun(@"\folder\on\current\drive"), Is.True);
            Assert.That(provider.ShouldRun(@"d:\"), Is.True);
            Assert.That(provider.ShouldRun(@"d:\folder"), Is.True);
        }

        [Test]
        public void TestShouldNotRun()
        {
            Assert.That(provider.ShouldRun(@"\\network-share\folder"), Is.False);
            Assert.That(provider.ShouldRun(@"smb://boo"), Is.False);
            Assert.That(provider.ShouldRun(@"s3://boo"), Is.False);
            Assert.That(provider.ShouldRun(@"ftp://boo"), Is.False);
            Assert.That(provider.ShouldRun(@"http://boo"), Is.False);
        }

        [Test]
        public void TestExtractFilenameFromPath()
        {
            var result = provider.ExtractFilenameFromPath(@"E:\folder\sub\file.ext");

            Assert.That(result, Is.EqualTo("file.ext"));
        }

        [Test]
        public async Task TestGetFileSizeWhenFileDoesNotExist()
        {
            var result = await provider.GetFileSize(@"C:\this\folder\should\not\exists\and\this\file.either");

            Assert.That(result, Is.EqualTo(-1));
        }
    }
}
