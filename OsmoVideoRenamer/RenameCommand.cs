using Cocona;
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.ConsoleWrapping;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer
{
    public class RenameCommand
    {
        private readonly ILogger<RenameCommand> _logger;
        private readonly IVideoDirectoryFactory _directoryFactory;
        private readonly IFileFilter _fileFilter;
        private readonly IFileSort _fileSort;
        private readonly IFileRename _fileRename;
        private readonly IRenameCollisionChecker _collisionChecker;
        private readonly IConsoleWrapper _console;

        public RenameCommand(
            ILogger<RenameCommand> logger,
            IVideoDirectoryFactory directoryFactory,
            IFileFilter fileFilter,
            IFileSort fileSort,
            IFileRename fileRename,
            IRenameCollisionChecker collisionChecker,
            IConsoleWrapper console)
        {
            _logger = logger;
            _directoryFactory = directoryFactory;
            _fileFilter = fileFilter;
            _fileSort = fileSort;
            _fileRename = fileRename;
            _collisionChecker = collisionChecker;
            _console = console;
        }

        public void Rename(
            [Option(Description = "Directory where the Osmo videos are stored")] string fileLocation,
            [Option(Description = "Text that should appear before each file's number")] string? prefix,
            [Option(Description = "Text that should appear after each file's number")] string? suffix,
            [Option(Description = "What number the renamed files should start at")] int? startingNumber,
            [Option(Description = "The number of digits to include in each file number")] int? digitCount,
            [Option(Description = "Print a list of the files to be renamed, but do not rename them")] bool dryRun = false)
        {
            IVideoDirectory videoDirectory = _directoryFactory.Create(fileLocation);
            IReadOnlyList<IDirectoryFile> allFiles = videoDirectory.GetFilesInDirectory();
            IEnumerable<IVideoFile> matchingVideos = _fileFilter.GetMatchingVideos(allFiles);
            List<INumberedVideoFile> sortedVideos = _fileSort.GetOrderedFiles(matchingVideos, startingNumber);
            if (sortedVideos.Count == 0)
            {
                _console.WriteLine($"No DJI Osmo videos found in {fileLocation}.");
                return;
            }

            _logger.LogInformation("Found {count} DJI Osmo video(s) to rename.", sortedVideos.Count);
            IList<IRenamedVideoFile> renamedFiles = _fileRename.GetRenamedFiles(sortedVideos, allFiles, prefix, suffix, digitCount);
            _collisionChecker.VerifyNoCollisions(renamedFiles, allFiles);
            foreach (IRenamedVideoFile file in renamedFiles)
            {
                _console.WriteLine($"{file.NewIndex}: {file.Name} -> {file.NewName}");
                foreach (IRenamedCompanionFile companion in file.Companions)
                {
                    _console.WriteLine($"    {companion.Name} -> {companion.NewName}");
                }

                if (!dryRun)
                {
                    file.CommitRenameToDisk();
                    foreach (IRenamedCompanionFile companion in file.Companions)
                    {
                        companion.CommitRenameToDisk();
                    }
                }
            }
        }
    }
}
