using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.Directory
{
    public class VideoDirectory : IVideoDirectory
    {
        private readonly ILogger<VideoDirectory> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly string _directoryPath;
        private readonly IDirectoryInfo _directoryInfo;
        private readonly IDirectoryFileFactory _directoryFileFactory;

        public VideoDirectory(
            ILogger<VideoDirectory> logger,
            IFileSystem fileSystem,
            IDirectoryFileFactory directoryFileFactory,
            string directoryPath)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _directoryFileFactory = directoryFileFactory;
            _directoryPath = directoryPath;
            _directoryInfo = _fileSystem.DirectoryInfo.New(_directoryPath);
            VerifyDirectoryExists();
        }

        private void VerifyDirectoryExists()
        {
            _logger.LogInformation("Checking for existence of directory at path {path}", _directoryPath);
            if (!_directoryInfo.Exists)
            {
                _logger.LogCritical("Directory at path {path} does not exist!", _directoryPath);
                throw new DirectoryNotFoundException("Unable to find specified source directory.");
            }

            _logger.LogInformation("Directory at path {path} does exist.", _directoryPath);
        }

        public IReadOnlyList<IDirectoryFile> GetFilesInDirectory() =>
            _directoryInfo.GetFiles().Select(file => _directoryFileFactory.Create(file)).ToList();
    }
}
