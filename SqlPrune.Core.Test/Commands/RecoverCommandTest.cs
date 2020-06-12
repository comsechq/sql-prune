using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Comsec.SqlPrune.Providers;
using LightInject;
using Moq;
using NUnit.Framework;

namespace Comsec.SqlPrune.Commands
{
    [TestFixture]
    [Parallelizable]
    public class RecoverCommandTest
    {
        public RecoverCommand Setup(out MockingServiceContainer context, out Mock<IFileProvider> provider1, out Mock<IFileProvider> provider2)
        {
            context = new MockingServiceContainer();

            provider1 = new Mock<IFileProvider>();
            provider2 = new Mock<IFileProvider>();

            context.RegisterInstance<IEnumerable<IFileProvider>>(new List<IFileProvider>
                                                                 {
                                                                     provider1.Object,
                                                                     provider2.Object
                                                                 });

            context.RegisterInstance<IFileProvider>(provider1.Object);

            context.Register<RecoverCommand>();

            var command = context.GetInstance<RecoverCommand>();

            return command;
        }

        [Test]
        public async Task TestExecuteWithoutDateRestriction()
        {
            var command = Setup(out var context, out var provider1, out var provider2);

            provider1.Setup(call => call.ShouldRun("s3://bucket"))
                     .Returns(false);

            provider2.Setup(call => call.ShouldRun("s3://bucket"))
                     .Returns(true);

            provider2.Setup(call => call.IsDirectory("s3://bucket"))
                     .ReturnsAsync(true);

            provider2.Setup(call =>
                         call.ExtractFilenameFromPath("s3://bucket/DbName_backup_2014_12_30_010001_3427331.bak.7z"))
                     .Returns("DbName_backup_2014_12_30_010001_3427331.bak.7z");

            var files = new Dictionary<string, long>
                        {
                            {"s3://bucket/DbName_backup_2013_10_27_010003_3477881.zip", 1093753342},
                            {"s3://bucket/DbName_backup_2013_10_27_010003_3477881.bak", 1493753344},
                            {"s3://bucket/DbName_backup_2014_11_30_010002_5357881.bak", 1693732345},
                            {"s3://bucket/DbName_backup_2014_12_30_010001_3427331.bak.7z", 90349033}
                        };

            provider2.Setup(call => call.GetFiles("s3://bucket", "DbName_backup_*"))
                     .ReturnsAsync(files);

            provider1.Setup(call => call.IsDirectory(@"c:\folder"))
                     .ReturnsAsync(true);

            await command.Execute(new RecoverCommand.Input("s3://bucket","*.bak,*.bak.7z", "DbName", new FileInfo(@"c:\folder"), null, true));

            provider2.Verify(
                call => call.CopyToLocalAsync("s3://bucket/DbName_backup_2014_12_30_010001_3427331.bak.7z",
                    @"c:\folder\DbName_backup_2014_12_30_010001_3427331.bak.7z"), Times.Once());
        }

        [Test]
        public async Task TestExecuteWithDateTimeRestriction()
        {
            var command = Setup(out var context, out var provider1, out var provider2);

            provider1.Setup(call => call.ShouldRun("s3://bucket"))
                     .Returns(false);

            provider2.Setup(call => call.ShouldRun("s3://bucket"))
                     .Returns(true);

            provider2.Setup(call => call.IsDirectory("s3://bucket"))
                     .ReturnsAsync(true);

            provider2.Setup(call => call.ExtractFilenameFromPath("s3://bucket/DbName_backup_2013_10_27_010003_3477881.bak"))
                     .Returns("DbName_backup_2013_10_27_010003_3477881.bak");

            var files = new Dictionary<string, long>
                        {
                            {"s3://bucket/DbName_backup_2013_10_27_010003_3477881.zip", 1093753342},
                            {"s3://bucket/DbName_backup_2013_10_27_010003_3477881.bak", 1493753344},
                            {"s3://bucket/DbName_backup_2014_11_30_010002_5357881.bak", 1693732345},
                        };

            provider2.Setup(call => call.GetFiles("s3://bucket", "DbName_backup_*"))
                     .ReturnsAsync(files);

            provider1.Setup(call => call.IsDirectory(@"c:\folder"))
                     .ReturnsAsync(true);

            await command.Execute(new RecoverCommand.Input("s3://bucket","*.bak", "DbName", new FileInfo(@"c:\folder"), "2013-10-27 01:00:03", true));

            provider2.Verify(
                call =>
                    call.CopyToLocalAsync("s3://bucket/DbName_backup_2013_10_27_010003_3477881.bak",
                        @"c:\folder\DbName_backup_2013_10_27_010003_3477881.bak"), Times.Once());
        }

        [Test]
        public async Task TestExecuteWithDateRestriction()
        {
            var command = Setup(out var context, out var provider1, out var provider2);

            provider1.Setup(call => call.ShouldRun("s3://bucket"))
                     .Returns(false);

            provider2.Setup(call => call.ShouldRun("s3://bucket"))
                     .Returns(true);

            provider2.Setup(call => call.IsDirectory("s3://bucket"))
                     .ReturnsAsync(true);

            provider2.Setup(call =>
                         call.ExtractFilenameFromPath("s3://bucket/DbName_backup_2013_10_27_020003_3477822.bak"))
                     .Returns("DbName_backup_2013_10_27_020003_3477822.bak");

            var files = new Dictionary<string, long>
                        {
                            {"s3://bucket/DbName_backup_2013_10_27_010003_3477881.zip", 1093753342},
                            {"s3://bucket/DbName_backup_2013_10_27_020003_3477822.bak", 1493753344},
                            {"s3://bucket/DbName_backup_2014_11_30_010002_5357881.bak.7z", 1693732345},
                        };

            provider2.Setup(call => call.GetFiles("s3://bucket", "DbName_backup_*"))
                     .ReturnsAsync(files);

            provider1.Setup(call => call.IsDirectory(@"c:\folder"))
                     .ReturnsAsync(true);

            await command.Execute(new RecoverCommand.Input("s3://bucket", "*.bak", "DbName", new FileInfo(@"c:\folder"), "2013-10-27", true));

            provider2.Verify(
                call => call.CopyToLocalAsync("s3://bucket/DbName_backup_2013_10_27_020003_3477822.bak",
                    @"c:\folder\DbName_backup_2013_10_27_020003_3477822.bak"), Times.Once());
        }
    }
}