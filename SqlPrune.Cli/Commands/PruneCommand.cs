using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Comsec.SqlPrune.LightInject;
using LightInject;
using Comsec.SqlPrune.Providers;
using Comsec.SqlPrune.Services;
using Sugar.Extensions;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Prunes a given location from its .bak files.
    /// </summary>
    public class PruneCommand : BaseFileProviderCommand
    {
        private readonly IPruneService pruneService;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileProviders"></param>
        /// <param name="pruneService"></param>
        public PruneCommand(IEnumerable<IFileProvider> fileProviders, IPruneService pruneService) 
            : base(fileProviders)
        {
            this.pruneService = pruneService;
        }

        /// <summary>
        /// Defines a sub command, its handler and adds it to the <see cref="parent"/> command.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="container"></param>
        public static void Configure(Command parent, ServiceContainer container)
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

            command.Handler = CommandHandler.Create<Input, string, string, string>(
                async (input, profile, profilesLocation, region) =>
                {
                    container.RegisterOptionalAwsCredentials(profile, profilesLocation)
                             .RegisterOptionalAwsRegion(region);

                    container.Register<PruneCommand>();

                    var instance = container.GetInstance<PruneCommand>();

                    await instance.Execute(input);
                });

            parent.Add(command);
        }

        public class Input
        {
            public Input(string path, string ext, bool delete, bool yes)
            {
                Path = path;
                FileExtensions = ext;
                DeleteFiles = delete;
                NoConfirm = yes;
            }

            public string Path { get; set; }

            public string FileExtensions { get; set; }

            public bool DeleteFiles { get; set; }

            public bool NoConfirm { get; set; }
        }

        public async Task Execute(Input input)
        {
            var provider = await GetProvider(input.Path);

            Console.Write("Listing all ");
            ColorConsole.Write(ConsoleColor.Cyan, input.FileExtensions);
            Console.Write(" files in ");
            ColorConsole.Write(ConsoleColor.Yellow, input.Path);
            Console.WriteLine(" including subfolders...");
            Console.WriteLine();

            var endingWith = input.FileExtensions
                                  .FromCsv()
                                  .Select(x => x.Trim())
                                  .ToArray();

            var getFilesTask = provider.GetFiles(input.Path, endingWith)
                                       .GetAwaiter();

            var paths = getFilesTask.OutputProgress();

            if (paths == null)
            {
                throw new ApplicationException("No bak files found in folder or subfolders.");
            }

            var files = ToBakModels(paths);

            Console.WriteLine("Found {0} file(s) out of which {1} have valid file names.", paths.Count(), files.Count);
            Console.WriteLine();

            if (input.DeleteFiles)
            {
                if (input.NoConfirm)
                {
                    ColorConsole.Write(ConsoleColor.Red, "DEFCON 1: ");
                    Console.WriteLine("All prunable files will be deleted.");
                }
                else
                {
                    ColorConsole.Write(ConsoleColor.Yellow, "DEFCON 3: ");
                    Console.WriteLine("Files will be deleted, but you'll have to confirm each deletion.");
                }
            }
            else
            {
                ColorConsole.Write(ConsoleColor.Green, "DEFCON 5: ");
                Console.WriteLine("Simulation only, no files will be deleted.");
            }
            Console.WriteLine();

            var backupSets = files.GroupBy(x => x.DatabaseName);

            long totalBytes = 0;
            long totalKept = 0;
            long totalPruned = 0;

            foreach (var dbBackupSet in backupSets)
            {
                pruneService.FlagPrunableBackupsInSet(dbBackupSet);

                ColorConsole.Write(ConsoleColor.White, " {0}:", dbBackupSet.Key);
                Console.WriteLine();
                
                ColorConsole.Write(ConsoleColor.DarkGray, " Created         Status\tBytes\t\tPath");
                Console.WriteLine();

                foreach (var model in dbBackupSet)
                {
                    Console.Write(" {0} ", model.Created.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture));

                    if (!model.Prunable.HasValue)
                    {
                        Console.Write(model.Status);
                    }
                    else if (model.Prunable.Value)
                    {
                        ColorConsole.Write(ConsoleColor.DarkRed, model.Status);
                    }
                    else
                    {
                        ColorConsole.Write(ConsoleColor.DarkGreen, model.Status);
                    }
                    Console.Write("\t{0,15:N0}\t", model.Size);

                    Console.Write(model.Path);
                    
                    if (model.Prunable.HasValue && model.Prunable.Value && input.DeleteFiles)
                    {
                        var prompt = $"{Environment.NewLine}Delete {model.Path}?";

                        var delete = input.NoConfirm || Confirm.Prompt(prompt, "y");

                        if (delete)
                        {
                            await provider.Delete(model.Path);
                            ColorConsole.Write(ConsoleColor.Red, " Deleted");
                        }
                        else
                        {
                            ColorConsole.Write(ConsoleColor.DarkGreen, " Skipped");
                        }

                        if (!input.NoConfirm)
                        {
                            Console.WriteLine(" {0}", model.Path);
                        }
                    }

                    Console.WriteLine();

                    totalBytes += model.Size;

                    if (model.Prunable.HasValue)
                    {
                        if (model.Prunable.Value)
                        {
                            totalPruned += model.Size;
                        }
                        else
                        {
                            totalKept += model.Size;
                        }

                    }
                }

                Console.WriteLine();
            }

            // Size Summary
            if (totalBytes > 0)
            {
                ColorConsole.Write(ConsoleColor.DarkGreen, "              Kept");
                Console.WriteLine(": {0,19:N0} ({1})", totalKept, totalKept.ToSizeWithSuffix());

                ColorConsole.Write(ConsoleColor.DarkRed, "            Pruned");
                Console.WriteLine(": {0,19:N0} ({1})", totalPruned, totalPruned.ToSizeWithSuffix());

                Console.WriteLine("             Total: {0,19:N0} ({1})", totalBytes, totalBytes.ToSizeWithSuffix());
            }
        }
    }
}
