using System;
using System.Collections.Generic;
using Comsec.SqlPrune.Commands;
using Comsec.SqlPrune.LightInject;
using Comsec.SqlPrune.Providers;
using Comsec.SqlPrune.Services;
using LightInject;
using Sugar.Command.Binder;

namespace Comsec.SqlPrune
{
    /// <summary>
    /// Entry point for the command line tool.
    /// </summary>
    public class Program
    {
        public static ServiceContainer Container;

        static void InstallIoc(Parameters parameters)
        {
            var options = new ContainerOptions
                          {
                              EnablePropertyInjection = false
                          };

            Container = new ServiceContainer(options);

            Container.RegisterInstance(parameters);

            Container.RegisterFrom<AwsSdkCompositionRoot>();
            Container.RegisterFrom<SqlPruneCoreCompositionRoot>();

            Container.Register<PruneCommand>();
            Container.Register<RecoverCommand>(f =>
                new RecoverCommand(f.GetInstance<IEnumerable<IFileProvider>>(), f.GetInstance<IFileProvider>("local")));
        }

        static int Main(string[] args)
        {
            // string[] args does contain as much as Environment.CommandLine
            var parameters = new Parameters(Environment.CommandLine);

            InstallIoc(parameters);

            var console = new PruneConsole();
            
            return console.Run(parameters);
        }
    }
}
