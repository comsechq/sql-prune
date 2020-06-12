using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Comsec.SqlPrune.LightInject;
using Comsec.SqlPrune.Providers;
using LightInject;

namespace Comsec.SqlPrune.Commands
{
    public static class CommandExtensions
    {
        /// <summary>
        /// Defines a sub command, its handler and adds it to the <see cref="parent"/> command.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="container"></param>
        /// <returns>The <see cref="parent" /> for method chaining.</returns>
        public static Command ConfigurePruneCommand(this Command parent, ServiceContainer container)
        {
            var command = new Command("prune")
                          {
                              new Argument<string>("path",
                                  description: "The path to a local folder or S3 bucket name"),
                              new Option<string>(new[] {"--ext", "-ext"},
                                  getDefaultValue: () => "*.bak,*.bak.7z,*.sql,*.sql.gz",
                                  description: "Overrides the default file extensions (coma separated values can be used)"),
                              new Option<bool>(new[] {"--delete", "-d"},
                                  getDefaultValue: () => false,
                                  description: "When specified files will be deleted") {Required = false},
                              new Option<bool>(new[] {"--yes", "-y"},
                                  getDefaultValue: () => false,
                                  description: "Deletes files without confirmation") {Required = false}
                          }.AddAwsSdkCredentialsOptions();

            command.Handler = CommandHandler.Create<PruneCommand.Input, string, string, string>(
                async (input, profile, profilesLocation, region) =>
                {
                    container.RegisterOptionalAwsCredentials(profile, profilesLocation)
                             .RegisterOptionalAwsRegion(region);

                    container.Register<PruneCommand>();

                    var instance = container.GetInstance<PruneCommand>();

                    await instance.Execute(input);
                });

            parent.Add(command);

            return parent;
        }

        /// <summary>
        /// Defines a sub command, its handler and adds it to the <see cref="parent"/> command.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="container"></param>
        /// <returns>The <see cref="parent" /> for method chaining.</returns>
        public static Command ConfigureRecoverCommand(this Command parent, ServiceContainer container)
        {
            var command = new Command("recover")
                          {
                              new Argument<string>("path",
                                  "The path to a local folder or S3 bucket name"),
                              new Option<string>(new[] {"--ext"},
                                  getDefaultValue: () => "*.bak,*.bak.7z,*.sql,*.sql.gz",
                                  description:
                                  "Overrides the default file extensions (coma separated values can be used)"),
                              new Option<string>("--dbName",
                                  description: "Name of the database to recover")
                              {Required = true},
                              new Option<DirectoryInfo>("--dest",
                                  description: "Folder where to recover the backup file to")
                              {Required = true},
                              new Option<string>("--date",
                                  getDefaultValue: () => "",
                                  description:
                                  "The date (and optionally the time) of the backup (e.g. \"2020-06-09 00:01:02\") to restrict to. If only the date is specified the most recent backup for that day will be recovered."),
                              new Option<bool>(new[] {"--yes", "-y"},
                                  getDefaultValue: () => false,
                                  description: "Recover without confirmation")
                              {Required = false}
                          }.AddAwsSdkCredentialsOptions();

            command.Handler =
                CommandHandler.Create<RecoverCommand.Input, string, string, string>(async (input, profile, profilesLocation, region) =>
                {
                    container.RegisterOptionalAwsCredentials(profile, profilesLocation)
                             .RegisterOptionalAwsRegion(region);

                    container.Register<RecoverCommand>(f =>
                        new RecoverCommand(f.GetInstance<IEnumerable<IFileProvider>>(),
                            f.GetInstance<IFileProvider>("local")));

                    var instance = container.GetInstance<RecoverCommand>();

                    await instance.Execute(input);
                });

            parent.Add(command);

            return parent;
        }

        public static Command AddAwsSdkCredentialsOptions(this Command command)
        {
            command.AddOption(new Option<string>("--profile",
                getDefaultValue: () => "",
                description: "The name of the AWS profile to use"));

            command.AddOption(new Option<string>("--profiles-location",
                getDefaultValue: () => "",
                description: "The path to the folder containing the AWS profiles"));
            
            command.AddOption(new Option<string>("--region",
                getDefaultValue: () => "",
                description: "The name of the AWS region to use"));

            return command;
        }
    }
}
