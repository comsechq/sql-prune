using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Comsec.SqlPrune.Commands;
using Comsec.SqlPrune.LightInject;
using LightInject;

namespace Comsec.SqlPrune
{
    /// <summary>
    /// Entry point for the command line tool.
    /// </summary>
    public class Program
    {
        static int Main(string[] args)
        {
            var options = new ContainerOptions
                          {
                              EnablePropertyInjection = false
                          };

            var container = new ServiceContainer(options);

            container.RegisterFrom<SqlPruneCoreCompositionRoot>();

            var rootCommand = new RootCommand
                              {
                                  Description = "Command line utility to prune (and recover) SQL backups in a folder (or S3 bucket)"
                              };

            PruneCommand.Configure(rootCommand, container);
            RecoverCommand.Configure(rootCommand, container);

            var builder = new CommandLineBuilder(rootCommand);

            builder.UseDefaults();
            builder.Build();

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}
