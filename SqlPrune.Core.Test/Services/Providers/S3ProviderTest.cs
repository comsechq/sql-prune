using System.Linq;
using System.Threading.Tasks;
using Comsec.SqlPrune.Providers;
using LightInject;
using NUnit.Framework;

namespace Comsec.SqlPrune.Services.Providers
{
    [TestFixture]
    [Parallelizable]
    public class S3ProviderTest
    {
        public S3Provider Setup(out MockingServiceContainer context)
        {
            context = new MockingServiceContainer();

            context.Register<S3Provider>();

            return context.GetInstance<S3Provider>();
        }

        [Test]
        public void TestShouldRun()
        {
            var provider = Setup(out var context);

            Assert.IsTrue(provider.ShouldRun(@"s3://hello-world"));
        }

        [Test]
        public void TestShouldNotRun()
        {
            var provider = Setup(out var context);

            Assert.IsFalse(provider.ShouldRun(@"\\network-share\folder"));
            Assert.IsFalse(provider.ShouldRun(@"smb://boo"));
            Assert.IsFalse(provider.ShouldRun(@"e:\\boo"));
            Assert.IsFalse(provider.ShouldRun(@"ftp://boo"));
            Assert.IsFalse(provider.ShouldRun(@"http://boo"));
        }

        [Test]
        public void TestExtractFilenameFromPath()
        {
            var provider = Setup(out var context);

            var result = provider.ExtractFilenameFromPath("s3://bucket/folder/sub/file.ext");

            Assert.AreEqual("file.ext", result);
        }

        [Test]
        [Ignore("Integration test: replace bucket-name with a real bucket")]
        public async Task TestIsDirectory()
        {
            var provider = Setup(out var context);

            var result = await provider.IsDirectory("s3://a-test-bucket/test");

            Assert.IsTrue(result);
        }

        [Test]
        [Ignore("Integration test")]
        public async Task TestIsNotDirectory()
        {
            var provider = Setup(out var context);

            var result = await provider.IsDirectory("this-bucket-does-not-exist");

            Assert.IsFalse(result);
        }

        [Test]
        [Ignore("Integration test: replace path with a real value")]
        public void TestGetFileSize()
        {
            var provider = Setup(out var context);

            var result = provider.GetFileSize("s3://a-test-bucket/folder/file.ext");

            Assert.AreEqual(2849199616, result);
        }

        [Test]
        [Ignore("Integration test: replace bucket-name with a real bucket")]
        public async Task TestGetFiles()
        {
            var provider = Setup(out var context);

            var files = await provider.GetFiles("s3://a-test-bucket/folder", "*.zip");

            Assert.Less(0, files.Count);

            Assert.IsTrue(files.Keys.First().StartsWith("s3://"));
            Assert.Less(0, files.Values.First());
        }

        [Test]
        [Ignore("Integration test: file name with full path of existing file you're willing to delete")]
        public async Task TestDelete()
        {
            var provider = Setup(out var context);

            await provider.Delete("s3://a-test-bucket/path/to/folder/filename.ext");
        }
    }
}
