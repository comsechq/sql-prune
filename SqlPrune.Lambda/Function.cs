using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Comsec.SqlPrune.Commands;
using Comsec.SqlPrune.LightInject;
using Comsec.SqlPrune.Logging;
using LightInject;
using Sugar.Amazon.Lambda;
using Sugar.Extensions;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<SqlPrune.Lambda.CustomJsonSerializerContext>))]

namespace SqlPrune.Lambda;

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
        container = new ServiceContainer(new ContainerOptions
                                         {
                                             EnablePropertyInjection = false
                                         });

        container.Register<ILambdaLogger, FilteredLambdaLogger>();
        container.Register<ILogger, Logger.LambdaLogger>();
        
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
    
    /// <summary>
    /// A function that prunes the backup files from a bucket.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task FunctionHandler(Input input)
    {
        if (string.IsNullOrEmpty(input.BucketName))
        {
            throw new ArgumentNullException(nameof(input), "Missing BucketName value");
        }

        var commandInput = new PruneCommand.Input("s3://" + input.BucketName, input.FileExtensions.ToCsv(), true, true);

        await pruneCommand.Execute(commandInput);
    }
}