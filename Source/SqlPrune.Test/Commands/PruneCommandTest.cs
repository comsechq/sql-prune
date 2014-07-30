using System;
using System.Collections.Generic;
using System.Linq;
using Comsec.SqlPrune.Factories;
using Comsec.SqlPrune.Interfaces.Services;
using Comsec.SqlPrune.Interfaces.Services.Providers;
using Comsec.SqlPrune.Models;
using Comsec.SqlPrune.Services.Providers;
using Moq;
using NUnit.Framework;

namespace Comsec.SqlPrune.Commands
{
    [TestFixture]
    public class PruneCommandTest : AutoMockingTest
    {
        private PruneCommand command;

        private Mock<IFileProvider> localFileSystemProviderMock;
        private Mock<IFileProvider> s3ProviderMock;

        [SetUp]
        public void Setup()
        {
            localFileSystemProviderMock = new Mock<IFileProvider>();
            s3ProviderMock = new Mock<IFileProvider>();

            localFileSystemProviderMock.Setup(call => call.ShouldRun(It.IsAny<string>()))
                                       .Returns(true);

            s3ProviderMock.Setup(call => call.ShouldRun(It.IsAny<string>()))
                          .Returns(false);

            command = Create<PruneCommand>();

            command.FileProviders = new[] {localFileSystemProviderMock.Object, s3ProviderMock.Object};
        }

        [Test]
        public void TestFileProvidersOrder()
        {
            var instance = new PruneCommand();

            Assert.AreEqual(2, instance.FileProviders.Length);
            Assert.AreEqual(typeof(S3Provider), instance.FileProviders[0].GetType());
            Assert.AreEqual(typeof(LocalFileSystemProvider), instance.FileProviders[1].GetType());
        }

        [Test]
        public void TestExecuteWhenPathNotSet()
        {
            var result = command.Execute(new PruneCommand.Options());

            s3ProviderMock
                .Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            localFileSystemProviderMock
                .Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void TestExecuteWhenPathIsFlag()
        {
            localFileSystemProviderMock
                .Setup(call => call.IsDirectory(@"-verbose"))
                .Returns(false);

            var result = command.Execute(new PruneCommand.Options());

            s3ProviderMock
                .Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            localFileSystemProviderMock
                .Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void TestExecuteWhenPathIsNotDirectory()
        {
            localFileSystemProviderMock
                .Setup(call => call.IsDirectory(@"c:\sql-backups\backup.bak"))
                .Returns(false);

            var result = command.Execute(new PruneCommand.Options {Path = @"c:\sql-backups\backup.bak"});

            s3ProviderMock
                .Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void TestExecuteWhenPathIsDirectoryButSimulationOnlys()
        {
            localFileSystemProviderMock
                .Setup(call => call.IsDirectory(@"c:\sql-backups"))
                .Returns(true);

            var files = new BakFileListFactory().WithDatabases("db1").Create(DateTime.Now, 2);

            localFileSystemProviderMock
                .Setup(call => call.GetFiles(@"c:\sql-backups", "*.bak"))
                .Returns(files);
            
            var result = command.Execute(new PruneCommand.Options {Path = @"c:\sql-backups"});

            s3ProviderMock
                .Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            Mock<IPruneService>()
                .Verify(call => call.FlagPrunableBackupsInSet(It.Is<IEnumerable<BakModel>>(x => x.Count() == 2)),
                    Times.Once());

            localFileSystemProviderMock
                .Verify(call => call.Delete(It.IsAny<string>()), Times.Never());
            
            Assert.AreEqual(0, result);
        }

        [Test]
        public void TestExecuteWhenPathIsDirectotyAndDeleteWithoutConfirmation()
        {
            localFileSystemProviderMock
                .Setup(call => call.IsDirectory(@"c:\sql-backups"))
                .Returns(true);

            var files = new BakFileListFactory().WithDatabases("db1").Create(DateTime.Now, 2);

            localFileSystemProviderMock
                .Setup(call => call.GetFiles(@"c:\sql-backups", "*.bak"))
                .Returns(files);

            Mock<IPruneService>()
                .Setup(call => call.FlagPrunableBackupsInSet(It.IsAny<IEnumerable<BakModel>>()))
                .Callback<IEnumerable<BakModel>>((x) => x.First().Prunable = true);

            var result = command.Execute(new PruneCommand.Options {Path = @"c:\sql-backups", Delete = true, NoConfirm = true});

            s3ProviderMock
                .Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            localFileSystemProviderMock
                .Verify(call => call.Delete(It.IsAny<string>()), Times.Once());

            localFileSystemProviderMock
                .Verify(call => call.Delete(files.First()), Times.Once());

            Assert.AreEqual(0, result);
        }
        
    }
}
