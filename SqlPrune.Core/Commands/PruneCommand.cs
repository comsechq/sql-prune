using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Comsec.SqlPrune.Logging;
using Comsec.SqlPrune.Providers;
using Comsec.SqlPrune.Services;
using Sugar.Extensions;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Interface representing a command that can execute a payload <see cref="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of payload.</typeparam>
    public interface ICommand<in T>
    {
        Task Execute(T input);
    }

    /// <summary>
    /// Prunes a given location from its .bak files.
    /// </summary>
    public class PruneCommand : BaseFileProviderCommand, ICommand<PruneCommand.Input>
    {
        private readonly IPruneService pruneService;
        private readonly ILogger logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileProviders"></param>
        /// <param name="pruneService"></param>
        /// <param name="logger"></param>
        public PruneCommand(IEnumerable<IFileProvider> fileProviders, IPruneService pruneService, ILogger logger) 
            : base(fileProviders)
        {
            this.pruneService = pruneService;
            this.logger = logger;
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

            logger.Write("Listing all ");
            logger.Write(ConsoleColor.Cyan, input.FileExtensions);
            logger.Write(" files in ");
            logger.Write(ConsoleColor.Yellow, input.Path);
            logger.WriteLine(" including subfolders...");
            logger.WriteLine();

            var endingWith = input.FileExtensions
                                  .FromCsv()
                                  .Select(x => x.Trim())
                                  .ToArray();

            var getFilesTask = provider.GetFiles(input.Path, endingWith)
                                       .GetAwaiter();

            var paths = getFilesTask.OutputProgress(logger);

            if (paths == null)
            {
                throw new ApplicationException("No bak files found in folder or subfolders.");
            }

            var files = ToBakModels(paths);

            logger.WriteLine("Found {0} file(s) out of which {1} have valid file names.", paths.Count(), files.Count);
            logger.WriteLine();

            if (input.DeleteFiles)
            {
                if (input.NoConfirm)
                {
                    logger.Write(ConsoleColor.Red, "DEFCON 1: ");
                    logger.WriteLine("All prunable files will be deleted.");
                }
                else
                {
                    logger.Write(ConsoleColor.Yellow, "DEFCON 3: ");
                    logger.WriteLine("Files will be deleted, but you'll have to confirm each deletion.");
                }
            }
            else
            {
                logger.Write(ConsoleColor.Green, "DEFCON 5: ");
                logger.WriteLine("Simulation only, no files will be deleted.");
            }
            logger.WriteLine();

            var backupSets = files.GroupBy(x => x.DatabaseName);

            long totalBytes = 0;
            long totalKept = 0;
            long totalPruned = 0;

            foreach (var dbBackupSet in backupSets)
            {
                pruneService.FlagPrunableBackupsInSet(dbBackupSet);

                logger.Write(ConsoleColor.White, " {0}:", dbBackupSet.Key);
                logger.WriteLine();
                
                logger.Write(ConsoleColor.DarkGray, " Created         Status\tBytes\t\tPath");
                logger.WriteLine();

                foreach (var model in dbBackupSet)
                {
                    logger.Write(" {0} ", model.Created.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture));

                    if (!model.Prunable.HasValue)
                    {
                        logger.Write(model.Status);
                    }
                    else if (model.Prunable.Value)
                    {
                        logger.Write(ConsoleColor.DarkRed, model.Status);
                    }
                    else
                    {
                        logger.Write(ConsoleColor.DarkGreen, model.Status);
                    }
                    logger.Write("\t{0,15:N0}\t", model.Size);

                    logger.Write(model.Path);
                    
                    if (model.Prunable.HasValue && model.Prunable.Value && input.DeleteFiles)
                    {
                        var prompt = $"{Environment.NewLine}Delete {model.Path}?";

                        var delete = input.NoConfirm || new Confirm(logger).Prompt(prompt, "y");

                        if (delete)
                        {
                            await provider.Delete(model.Path);
                            logger.Write(ConsoleColor.Red, " Deleted");
                        }
                        else
                        {
                            logger.Write(ConsoleColor.DarkGreen, " Skipped");
                        }

                        if (!input.NoConfirm)
                        {
                            logger.WriteLine(" {0}", model.Path);
                        }
                    }

                    logger.WriteLine();

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

                logger.WriteLine();
            }

            // Size Summary
            if (totalBytes > 0)
            {
                logger.Write(ConsoleColor.DarkGreen, "              Kept");
                logger.WriteLine(": {0,19:N0} ({1})", totalKept, totalKept.ToSizeWithSuffix());

                logger.Write(ConsoleColor.DarkRed, "            Pruned");
                logger.WriteLine(": {0,19:N0} ({1})", totalPruned, totalPruned.ToSizeWithSuffix());

                logger.WriteLine("             Total: {0,19:N0} ({1})", totalBytes, totalBytes.ToSizeWithSuffix());
            }
        }
    }
}
