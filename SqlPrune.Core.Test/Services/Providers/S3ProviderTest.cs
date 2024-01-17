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

            Assert.That(provider.ShouldRun(@"s3://hello-world"), Is.True);
        }

        [Test]
        public void TestShouldNotRun()
        {
            var provider = Setup(out var context);

            Assert.That(provider.ShouldRun(@"\\network-share\folder"), Is.False);
            Assert.That(provider.ShouldRun(@"smb://boo"), Is.False);
            Assert.That(provider.ShouldRun(@"e:\\boo"), Is.False);
            Assert.That(provider.ShouldRun(@"ftp://boo"), Is.False);
            Assert.That(provider.ShouldRun(@"http://boo"), Is.False);
        }

        [Test]
        public void TestExtractFilenameFromPath()
        {
            var provider = Setup(out var context);

            var result = provider.ExtractFilenameFromPath("s3://bucket/folder/sub/file.ext");

            Assert.That(result, Is.EqualTo("file.ext"));
        }

        [Test]
        [Ignore("Integration test: replace bucket-name with a real bucket")]
        public async Task TestIsDirectory()
        {
            var provider = Setup(out var context);

            var result = await provider.IsDirectory("s3://a-test-bucket/test");

            Assert.That(result, Is.True);
        }

        [Test]
        [Ignore("Integration test")]
        public async Task TestIsNotDirectory()
        {
            var provider = Setup(out var context);

            var result = await provider.IsDirectory("this-bucket-does-not-exist");

            Assert.That(result, Is.False);
        }

        [Test]
        [Ignore("Integration test: replace path with a real value")]
        public void TestGetFileSize()
        {
            var provider = Setup(out var context);

            var result = provider.GetFileSize("s3://a-test-bucket/folder/file.ext");

            Assert.That(result, Is.EqualTo(2849199616));
        }

        [Test]
        [Ignore("Integration test: replace bucket-name with a real bucket")]
        public async Task TestGetFiles()
        {
            var provider = Setup(out var context);

            var files = await provider.GetFiles("s3://a-test-bucket/folder", "*.zip");

            Assert.That(files.Count, Is.LessThan(0));

            Assert.That(files.Keys.First().StartsWith("s3://"), Is.True);
            Assert.That(files.Values.First(), Is.LessThan(0));
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
