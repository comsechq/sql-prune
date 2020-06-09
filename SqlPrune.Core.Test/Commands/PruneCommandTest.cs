using System;
using System.Collections.Generic;
using System.Linq;
using Comsec.SqlPrune.Factories;
using Comsec.SqlPrune.Models;
using Comsec.SqlPrune.Providers;
using Comsec.SqlPrune.Services;
using LightInject;
using Moq;
using NUnit.Framework;

namespace Comsec.SqlPrune.Commands
{
    [TestFixture]
    [Parallelizable]
    public class PruneCommandTest
    {
        public PruneCommand Setup(out MockingServiceContainer context, out Mock<IFileProvider> provider1, out Mock<IFileProvider> provider2)
        {
            context = new MockingServiceContainer();

            provider1 = new Mock<IFileProvider>();
            provider2 = new Mock<IFileProvider>();

            provider1.Setup(call => call.ShouldRun(It.IsAny<string>()))
                     .Returns(false);

            provider2.Setup(call => call.ShouldRun(It.IsAny<string>()))
                     .Returns(true);

            context.RegisterInstance<IEnumerable<IFileProvider>>(new List<IFileProvider>
                                                                 {
                                                                     provider1.Object,
                                                                     provider2.Object
                                                                 });

            context.Register<PruneCommand>();

            var command = context.GetInstance<PruneCommand>();

            return command;
        }

        [Test]
        public void TestExecuteWhenPathNotSet()
        {
            var command = Setup(out var context, out var provider1, out var provider2);

            provider1.Setup(call => call.ShouldRun(null))
                     .Returns(false);

            provider2.Setup(call => call.ShouldRun(null))
                     .Returns(false);

            var result = command.Execute(new PruneCommand.Options());

            provider1.Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());
            
            provider2.Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void TestExecuteWhenPathIsNotDirectory()
        {
            var command = Setup(out var context, out var provider1, out var provider2);

            provider1.Setup(call => call.ShouldRun(@"c:\sql-backups\backup.bak"))
                     .Returns(true);

            provider1.Setup(call => call.IsDirectory(@"c:\sql-backups\backup.bak"))
                     .ReturnsAsync(false);

            var result = command.Execute(new PruneCommand.Options {Path = @"c:\sql-backups\backup.bak"});

            provider2.Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            Assert.AreEqual(-1, result);
        }

        [Test]
        public void TestExecuteWhenPathIsDirectoryButSimulationOnly()
        {
            var command = Setup(out var context, out var provider1, out var provider2);

            provider1.Setup(call => call.ShouldRun(@"c:\sql-backups"))
                     .Returns(true);

            provider1.Setup(call => call.IsDirectory(@"c:\sql-backups"))
                     .ReturnsAsync(true);

            var files = new BakFileListFactory().WithDatabases("db1").Create(DateTime.Now, 2);

            provider1.Setup(call => call.GetFiles(@"c:\sql-backups", "*.bak"))
                     .ReturnsAsync(files);
            
            var result = command.Execute(new PruneCommand.Options {Path = @"c:\sql-backups", FileExtensions = "*.bak"});

            provider2.Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            context.GetMock<IPruneService>()
                   .Verify(call => call.FlagPrunableBackupsInSet(It.Is<IEnumerable<BakModel>>(x => x.Count() == 2)),
                       Times.Once());

            provider1.Verify(call => call.Delete(It.IsAny<string>()), Times.Never());
            
            Assert.AreEqual(0, result);
        }

        [Test]
        public void TestExecuteWhenPathIsDirectoryAndDeleteWithoutConfirmation()
        {
            var command = Setup(out var context, out var provider1, out var provider2);

            provider1.Setup(call => call.ShouldRun(@"c:\sql-backups"))
                     .Returns(true);

            provider1.Setup(call => call.IsDirectory(@"c:\sql-backups"))
                     .ReturnsAsync(true);

            var files = new BakFileListFactory().WithDatabases("db1").Create(DateTime.Now, 2);

            provider1.Setup(call => call.GetFiles(@"c:\sql-backups", "*.bak"))
                     .ReturnsAsync(files);

            context.GetMock<IPruneService>()
                .Setup(call => call.FlagPrunableBackupsInSet(It.IsAny<IEnumerable<BakModel>>()))
                .Callback<IEnumerable<BakModel>>((x) => x.First().Prunable = true);

            var result = command.Execute(new PruneCommand.Options {Path = @"c:\sql-backups", Delete = true, FileExtensions = "*.bak", NoConfirm = true});

            provider2.Verify(call => call.IsDirectory(It.IsAny<string>()), Times.Never());

            provider1.Verify(call => call.Delete(It.IsAny<string>()), Times.Once());

            provider1.Verify(call => call.Delete(files.Keys.First()), Times.Once());

            Assert.AreEqual(0, result);
        }
    }
}
