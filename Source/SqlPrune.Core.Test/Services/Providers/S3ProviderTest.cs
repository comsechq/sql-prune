using System.Linq;
using Amazon;
using NUnit.Framework;

namespace Comsec.SqlPrune.Services.Providers
{
    [TestFixture]
    public class S3ProviderTest
    {
        private S3Provider provider;

        [SetUp]
        public void Setup()
        {
            provider = new S3Provider();
        }

        [Test]
        public void TestShouldRun()
        {
            Assert.IsTrue(provider.ShouldRun(@"s3://hello-world"));
        }

        [Test]
        public void TestShouldNotRun()
        {
            Assert.IsFalse(provider.ShouldRun(@"\\network-share\folder"));
            Assert.IsFalse(provider.ShouldRun(@"smb://boo"));
            Assert.IsFalse(provider.ShouldRun(@"e:\\boo"));
            Assert.IsFalse(provider.ShouldRun(@"ftp://boo"));
            Assert.IsFalse(provider.ShouldRun(@"http://boo"));
        }

        [Test]
        public void TestGetDefaultRegion()
        {
            var region = provider.GetRegion();

            Assert.AreEqual(RegionEndpoint.EUWest1, region);
        }

        [Test]
        public void TestExtractFilenameFromPath()
        {
            var result = provider.ExtractFilenameFromPath("s3://bucket/folder/sub/file.ext");

            Assert.AreEqual("file.ext", result);
        }

        [Test]
        [Ignore("Integration test: replace bucket-name with a real bucket")]
        public void TestIsDirectory()
        {
            var result = provider.IsDirectory("s3://a-test-bucket/test");

            Assert.IsTrue(result);
        }

        [Test]
        [Ignore("Integration test")]
        public void TestIsNotDirectory()
        {
            var result = provider.IsDirectory("this-bucket-does-not-exist");

            Assert.IsFalse(result);
        }

        [Test]
        [Ignore("Integration test: replace path with a real value")]
        public void TestGetFileSize()
        {
            var result = provider.GetFileSize("s3://a-test-bucket/folder/file.ext");

            Assert.AreEqual(2849199616, result);
        }

        [Test]
        [Ignore("Integration test: replace bucket-name with a real bucket")]
        public void TestGetFiles()
        {
            var files = provider.GetFiles("s3://a-test-bucket/folder", "*.zip");

            Assert.Less(0, files.Count);

            Assert.IsTrue(files.Keys.First().StartsWith("s3://"));
            Assert.Less(0, files.Values.First());
        }

        [Test]
        [Ignore("Integration test: file name with full path of existing file you're willing to delete")]
        public void TestDelete()
        {
            provider.Delete("s3://a-test-bucket/path/to/folder/filename.ext");
        }
    }
}
