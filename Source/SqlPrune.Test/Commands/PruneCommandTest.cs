using System;
using System.Collections.Generic;
using System.Linq;
using Comsec.SqlPrune.Factories;
using Comsec.SqlPrune.Interfaces.Services;
using Comsec.SqlPrune.Models;
using Moq;
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
        public void TestExecuteWhenPathNotSet()
        {
            Mock<IFileService>()
                .Setup(call => call.IsDirectory(@"c:\sql-backups\backup.bak"))
                .Returns(false);

            var result = command.Execute(new PruneCommand.Options());

            Mock<IFileService>()
                .Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void TestExecuteWhenPathIsFlag()
        {
            Mock<IFileService>()
                .Setup(call => call.IsDirectory(@"-verbose"))
                .Returns(false);

            var result = command.Execute(new PruneCommand.Options());

            Mock<IFileService>()
                .Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void TestExecuteWhenPathIsNotDirectory()
        {
            Mock<IFileService>()
                .Setup(call => call.IsDirectory(@"c:\sql-backups\backup.bak"))
                .Returns(false);

            var result = command.Execute(new PruneCommand.Options {Path = @"c:\sql-backups\backup.bak"});

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void TestExecuteWhenPathIsDirectoryButSimulationOnlys()
        {
            Mock<IFileService>()
                .Setup(call => call.IsDirectory(@"c:\sql-backups"))
                .Returns(true);

            var files = new BakFileListFactory().WithDatabases("db1").Create(DateTime.Now, 2);

            Mock<IFileService>()
                .Setup(call => call.GetFiles(@"c:\sql-backups", "*.bak"))
                .Returns(files);
            
            var result = command.Execute(new PruneCommand.Options {Path = @"c:\sql-backups"});

            Mock<IPruneService>()
                .Verify(call => call.FlagPrunableBackupsInSet(It.Is<IEnumerable<BakModel>>(x => x.Count() == 2)),
                    Times.Once());
            
            Mock<IFileService>()
                .Verify(call => call.Delete(It.IsAny<string>()), Times.Never());
            
            Assert.AreEqual(0, result);
        }

        [Test]
        public void TestExecuteWhenPathIsDirectotyAndDeleteWithoutConfirmation()
        {
            Mock<IFileService>()
                .Setup(call => call.IsDirectory(@"c:\sql-backups"))
                .Returns(true);

            var files = new BakFileListFactory().WithDatabases("db1").Create(DateTime.Now, 2);

            Mock<IFileService>()
                .Setup(call => call.GetFiles(@"c:\sql-backups", "*.bak"))
                .Returns(files);

            Mock<IPruneService>()
                .Setup(call => call.FlagPrunableBackupsInSet(It.IsAny<IEnumerable<BakModel>>()))
                .Callback<IEnumerable<BakModel>>((x) => x.First().Prunable = true);

            var result = command.Execute(new PruneCommand.Options {Path = @"c:\sql-backups", Delete = true, NoConfirm = true});

            Mock<IFileService>()
                .Verify(call => call.Delete(It.IsAny<string>()), Times.Once());

            Mock<IFileService>()
                .Verify(call => call.Delete(files.First()), Times.Once());

            Assert.AreEqual(0, result);
        }
        
    }
}
