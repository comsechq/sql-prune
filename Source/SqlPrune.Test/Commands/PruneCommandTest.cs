using Comsec.SqlPrune.Interfaces;
using NUnit.Framework;

namespace Comsec.SqlPrune.Commands
{
    [TestFixture]
    public class PruneCommandTest : AutoMockingTest
    {
        private PruneCommand command;

        [SetUp]
        public void Setup()
        {
            command = Create<PruneCommand>();
        }

        [Test]
        public void TestExecuteWhenPathIsDirectory()
        {
            Mock<IFileService>()
                .Setup(call => call.IsDirectory(@"c:\sql-backups"))
                .Returns(true);
            
            var result = command.Execute(new PruneCommand.Options {Path = @"c:\sql-backups"});

            Assert.AreEqual(0, result);
        }

        [Test]
        public void TestExecuteWhenPathIsNotDirectory()
        {
            Mock<IFileService>()
                .Setup(call => call.IsDirectory(@"c:\sql-backups\backup.bak"))
                .Returns(false);

            var result = command.Execute(new PruneCommand.Options { Path = @"c:\sql-backups\backup.bak" });

            Assert.AreEqual(-1, result);
        }
    }
}
