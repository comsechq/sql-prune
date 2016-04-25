using System;
using System.Collections.Generic;
using Comsec.SqlPrune.Interfaces.Services.Providers;
using Moq;
using NUnit.Framework;
using Sugar.Command;

namespace Comsec.SqlPrune.Commands
{
    [TestFixture]
    public class RecoverCommandTest : AutoMockingTest
    {
        private RecoverCommand command;

        private Mock<IFileProvider> localFileSystemProviderMock;
        private Mock<IFileProvider> s3ProviderMock;
        
        [SetUp]
        public void Setup()
        {
            localFileSystemProviderMock = new Mock<IFileProvider>();
            s3ProviderMock = new Mock<IFileProvider>();
            
            command = Create<RecoverCommand>();

            command.FileProviders = new[] {localFileSystemProviderMock.Object, s3ProviderMock.Object};
            command.LocalFileSystemProvider = localFileSystemProviderMock.Object;
        }

        [Test]
        public void TestExecuteWithoutDateRestriction()
        {
            var options = new RecoverCommand.Options
                          {
                              Path = "s3://bucket",
                              DatabaseName = "DbName",
                              DestinationPath = @"c:\folder",
                              NoConfirm = true
                          };

            localFileSystemProviderMock
                .Setup(call => call.ShouldRun("s3://bucket"))
                .Returns(false);

            s3ProviderMock
                .Setup(call => call.ShouldRun("s3://bucket"))
                .Returns(true);

            s3ProviderMock
                .Setup(call => call.IsDirectory("s3://bucket"))
                .Returns(true);

            s3ProviderMock
                .Setup(call => call.ExtractFilenameFromPath("s3://bucket/DbName_backup_2014_11_30_010002_5357881.bak"))
                .Returns("DbName_backup_2014_11_30_010002_5357881.bak");

            var files = new Dictionary<string, long>
                        {
                            {"s3://bucket/DbName_backup_2013_10_27_010003_3477881.zip", 1093753342},
                            {"s3://bucket/DbName_backup_2013_10_27_010003_3477881.bak", 1493753344},
                            {"s3://bucket/DbName_backup_2014_11_30_010002_5357881.bak", 1693732345},
                        };

            s3ProviderMock
                .Setup(call => call.GetFiles("s3://bucket", "DbName_backup_*"))
                .Returns(files);

            localFileSystemProviderMock
                .Setup(call => call.IsDirectory(@"c:\folder"))
                .Returns(true);

            var result = command.Execute(options);

            s3ProviderMock
                .Verify(
                    call =>
                        call.CopyToLocalAsync("s3://bucket/DbName_backup_2014_11_30_010002_5357881.bak", @"c:\folder\DbName_backup_2014_11_30_010002_5357881.bak"), Times.Once());
            
            Assert.AreEqual((int) ExitCode.Success, result);
        }

        [Test]
        public void TestExecuteWithDateTimeRestriction()
        {
            var options = new RecoverCommand.Options
                          {
                              Path = "s3://bucket",
                              DatabaseName = "DbName",
                              DestinationPath = @"c:\folder",
                              DateTime = new DateTime(2013, 10, 27, 1, 0, 3),
                              NoConfirm = true
                          };

            localFileSystemProviderMock
                .Setup(call => call.ShouldRun("s3://bucket"))
                .Returns(false);

            s3ProviderMock
                .Setup(call => call.ShouldRun("s3://bucket"))
                .Returns(true);

            s3ProviderMock
                .Setup(call => call.IsDirectory("s3://bucket"))
                .Returns(true);

            s3ProviderMock
                .Setup(call => call.ExtractFilenameFromPath("s3://bucket/DbName_backup_2013_10_27_010003_3477881.bak"))
                .Returns("DbName_backup_2013_10_27_010003_3477881.bak");

            var files = new Dictionary<string, long>
                        {
                            {"s3://bucket/DbName_backup_2013_10_27_010003_3477881.zip", 1093753342},
                            {"s3://bucket/DbName_backup_2013_10_27_010003_3477881.bak", 1493753344},
                            {"s3://bucket/DbName_backup_2014_11_30_010002_5357881.bak", 1693732345},
                        };

            s3ProviderMock
                .Setup(call => call.GetFiles("s3://bucket", "DbName_backup_*"))
                .Returns(files);

            localFileSystemProviderMock
                .Setup(call => call.IsDirectory(@"c:\folder"))
                .Returns(true);

            var result = command.Execute(options);

            s3ProviderMock
                .Verify(
                    call =>
                        call.CopyToLocalAsync("s3://bucket/DbName_backup_2013_10_27_010003_3477881.bak", @"c:\folder\DbName_backup_2013_10_27_010003_3477881.bak"), Times.Once());

            Assert.AreEqual((int)ExitCode.Success, result);
        }

        [Test]
        public void TestExecuteWithDateRestriction()
        {
            var options = new RecoverCommand.Options
            {
                Path = "s3://bucket",
                DatabaseName = "DbName",
                DestinationPath = @"c:\folder",
                Date = new DateTime(2013, 10, 27),
                NoConfirm = true
            };

            localFileSystemProviderMock
                .Setup(call => call.ShouldRun("s3://bucket"))
                .Returns(false);

            s3ProviderMock
                .Setup(call => call.ShouldRun("s3://bucket"))
                .Returns(true);

            s3ProviderMock
                .Setup(call => call.IsDirectory("s3://bucket"))
                .Returns(true);

            s3ProviderMock
                .Setup(call => call.ExtractFilenameFromPath("s3://bucket/DbName_backup_2013_10_27_020003_3477822.bak"))
                .Returns("DbName_backup_2013_10_27_020003_3477822.bak");

            var files = new Dictionary<string, long>
                        {
                            {"s3://bucket/DbName_backup_2013_10_27_010003_3477881.zip", 1093753342},
                            {"s3://bucket/DbName_backup_2013_10_27_020003_3477822.bak", 1493753344},
                            {"s3://bucket/DbName_backup_2014_11_30_010002_5357881.bak", 1693732345},
                        };

            s3ProviderMock
                .Setup(call => call.GetFiles("s3://bucket", "DbName_backup_*"))
                .Returns(files);

            localFileSystemProviderMock
                .Setup(call => call.IsDirectory(@"c:\folder"))
                .Returns(true);

            var result = command.Execute(options);

            s3ProviderMock
                .Verify(
                    call =>
                        call.CopyToLocalAsync("s3://bucket/DbName_backup_2013_10_27_020003_3477822.bak", @"c:\folder\DbName_backup_2013_10_27_020003_3477822.bak"), Times.Once());

            Assert.AreEqual((int)ExitCode.Success, result);
        }
    }
}