using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;
using System.Globalization;

namespace OsmoVideoRenamer.File
{
    public class FileRename : IFileRename
    {
        private readonly ILogger<FileRename> _logger;
        private readonly IRenamedVideoFileFactory _renamedVideoFileFactory;
        private readonly IRenamedCompanionFileFactory _renamedCompanionFileFactory;
        private readonly ICompanionFileFinder _companionFileFinder;

        // No file index can have more digits than int.MaxValue, and a larger count only produces unusable names.
        private const int MAX_DIGIT_COUNT = 10;

        public FileRename(
            ILogger<FileRename> logger,
            IRenamedVideoFileFactory renamedVideoFileFactory,
            IRenamedCompanionFileFactory renamedCompanionFileFactory,
            ICompanionFileFinder companionFileFinder)
        {
            _logger = logger;
            _renamedVideoFileFactory = renamedVideoFileFactory;
            _renamedCompanionFileFactory = renamedCompanionFileFactory;
            _companionFileFinder = companionFileFinder;
        }

        public IList<IRenamedVideoFile> GetRenamedFiles(
            IList<INumberedVideoFile> files,
            IEnumerable<IDirectoryFile> allFiles,
            string? prefix,
            string? suffix,
            int? digitCount)
        {
            if (files.Count == 0)
            {
                return new List<IRenamedVideoFile>();
            }

            int maxNewIndex = files.Max(file => file.NewIndex);
            int digits = GetDigitCount(digitCount, maxNewIndex);
            return files
                .Select(file => CreateRenamedFile(file, allFiles, prefix, suffix, digits))
                .ToList();
        }

        private IRenamedVideoFile CreateRenamedFile(
            INumberedVideoFile file,
            IEnumerable<IDirectoryFile> allFiles,
            string? prefix,
            string? suffix,
            int digits)
        {
            string numberFormat = "D" + digits.ToString(CultureInfo.InvariantCulture);
            string newBaseName = prefix + file.NewIndex.ToString(numberFormat, CultureInfo.InvariantCulture) + suffix;
            VerifyIsPlainFileName(newBaseName);
            List<IRenamedCompanionFile> companions = _companionFileFinder
                .GetCompanions(file, allFiles)
                .Select(companion => _renamedCompanionFileFactory.Create(newBaseName + companion.FileExtension, companion))
                .ToList();
            return _renamedVideoFileFactory.Create(newBaseName + file.FileExtension, companions, file);
        }

        private void VerifyIsPlainFileName(string newBaseName)
        {
            bool containsSeparator = Path.GetFileName(newBaseName) != newBaseName;
            bool containsInvalidCharacter = newBaseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
            if (containsSeparator || containsInvalidCharacter)
            {
                _logger.LogCritical(
                    "Cannot use the new base name {newBaseName} because it is not a plain file name; check the prefix and suffix.",
                    newBaseName);
                throw new ArgumentException(
                    $"The prefix and suffix must produce a plain file name, but '{newBaseName}' contains a path separator or a character that is not allowed in file names.");
            }
        }

        private int GetDigitCount(int? digitCount, int maxNewIndex)
        {
            int requiredDigits = maxNewIndex.ToString(CultureInfo.InvariantCulture).Length;
            if (!digitCount.HasValue)
            {
                return requiredDigits;
            }

            _logger.LogInformation("Verifying that digit count is large enough to accommodate maximum file index...");
            if (digitCount.Value > MAX_DIGIT_COUNT)
            {
                _logger.LogCritical(
                    "Cannot use provided digit count {digitCount} because it is greater than the maximum of {maxDigitCount}.",
                    digitCount.Value,
                    MAX_DIGIT_COUNT);
                throw new ArgumentOutOfRangeException(
                    nameof(digitCount),
                    $"Digit count must be at most {MAX_DIGIT_COUNT}");
            }

            if (digitCount.Value < requiredDigits)
            {
                _logger.LogCritical(
                    "Cannot use provided digit count {digitCount} because maximum file index digits {maxDigits} is greater.",
                    digitCount.Value,
                    requiredDigits);
                throw new ArgumentOutOfRangeException(
                    nameof(digitCount),
                    $"Digit count must be at least the number of digits in the largest renamed file index, which is {requiredDigits}");
            }

            _logger.LogInformation("Verified that digit count is large enough to accommodate maximum file index.");
            return digitCount.Value;
        }
    }
}
