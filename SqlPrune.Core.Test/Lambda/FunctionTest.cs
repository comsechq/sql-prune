using System.Threading.Tasks;
using Comsec.SqlPrune.Commands;
using Moq;
using NUnit.Framework;
using SqlPrune.Lambda;

namespace Comsec.SqlPrune.Lambda
{
    [TestFixture]
    public class FunctionTest
    {
        [Test]
        public async Task TestFunction()
        {
            var mockedCommand = new Mock<ICommand<PruneCommand.Input>>();

            var function = new Function(mockedCommand.Object);

            var input = new Function.Input
                        {
                            BucketName = "name",
                            FileExtensions = new[]
                                             {
                                                 ".bak",
                                                 ".bak.7z"
                                             }
                        };

            await function.FunctionHandler(input, null);

            mockedCommand.Verify(
                call => call.Execute(
                    It.Is<PruneCommand.Input>(x => x.Path == "s3://name" &&
                                                   x.FileExtensions == ".bak,.bak.7z" &&
                                                   x.NoConfirm &&
                                                   x.DeleteFiles)), Times.Once());
        }
    }
}
