using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Comsec.SqlPrune.Commands;
using Comsec.SqlPrune.LightInject;
using LightInject;
using Sugar.Extensions;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SqlPrune.Lambda
{
    /// <summary>
    /// Lambda function to prune the files in a given S3 bucket.
    /// </summary>
    public class Function
    {
        private readonly ServiceContainer container;
        private readonly ICommand<PruneCommand.Input> pruneCommand;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Function()
        {
            var options = new ContainerOptions
                          {
                              EnablePropertyInjection = false
                          };

            container = new ServiceContainer(options);

            container.RegisterFrom<SqlPruneCoreCompositionRoot>();

            container.Register<PruneCommand>();

            pruneCommand = container.GetInstance<PruneCommand>();
        }

        /// <summary>
        /// Test constructor.
        /// </summary>
        /// <param name="command"></param>
        public Function(ICommand<PruneCommand.Input> command)
        {
            pruneCommand = command;
        }

        public class Input
        {
            /// <summary>
            /// Default constructor.
            /// </summary>
            public Input()
            {
                FileExtensions = new[] {"*.bak,", "*.bak.7z", "*.sql", "*.sql.gz"};
            }

            /// <summary>
            /// The bucket name
            /// </summary>
            public string BucketName { get; set; }

            /// <summary>
            /// The file extensions to restrict to when listing files in the bucket.
            /// </summary>
            public string[] FileExtensions { get; set; }
        }

        /// <summary>
        /// A function that prunes the backup files from a bucket.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(Input input, ILambdaContext context)
        {
            var commandInput = new PruneCommand.Input("s3://" + input.BucketName, input.FileExtensions.ToCsv(), true, true);

            await pruneCommand.Execute(commandInput);
        }
    }
}
