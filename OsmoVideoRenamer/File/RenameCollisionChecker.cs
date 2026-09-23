using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.File
{
    /// <summary>
    /// Runs before any file is touched so that a run either renames everything or nothing.
    /// Comparisons are case-insensitive because macOS and Windows file systems usually are.
    /// </summary>
    public class RenameCollisionChecker : IRenameCollisionChecker
    {
        private readonly ILogger<RenameCollisionChecker> _logger;

        public RenameCollisionChecker(ILogger<RenameCollisionChecker> logger)
        {
            _logger = logger;
        }

        public void VerifyNoCollisions(IEnumerable<IRenamedVideoFile> renamedFiles, IEnumerable<IDirectoryFile> allFiles)
        {
            List<string> plannedNames = renamedFiles
                .SelectMany(file => file.Companions.Select(companion => companion.NewName).Prepend(file.NewName))
                .ToList();
            List<string> duplicates = plannedNames
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            var existingNames = new HashSet<string>(allFiles.Select(file => file.Name), StringComparer.OrdinalIgnoreCase);
            List<string> alreadyPresent = plannedNames
                .Where(name => existingNames.Contains(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (duplicates.Count > 0 || alreadyPresent.Count > 0)
            {
                string message = BuildMessage(duplicates, alreadyPresent);
                _logger.LogCritical("{message}", message);
                throw new IOException(message);
            }

            _logger.LogInformation("Verified that no planned file name is used twice or already exists in the directory.");
        }

        private static string BuildMessage(List<string> duplicates, List<string> alreadyPresent)
        {
            var parts = new List<string> { "Cannot rename; no files were changed." };
            if (duplicates.Count > 0)
            {
                parts.Add($"These new names would be used more than once: {string.Join(", ", duplicates)}.");
            }

            if (alreadyPresent.Count > 0)
            {
                parts.Add($"These new names already exist in the directory: {string.Join(", ", alreadyPresent)}.");
            }

            return string.Join(" ", parts);
        }
    }
}
