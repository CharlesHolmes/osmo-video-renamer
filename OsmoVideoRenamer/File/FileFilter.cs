using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.Naming;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File
{
    public class FileFilter : IFileFilter
    {
        private readonly IVideoFileFactory _videoFileFactory;

        public FileFilter(IVideoFileFactory videoFileFactory)
        {
            _videoFileFactory = videoFileFactory;
        }

        public IEnumerable<IVideoFile> GetMatchingVideos(IEnumerable<IDirectoryFile> allFiles) =>
            allFiles
                .Where(file => DjiVideoFileName.TryParse(file.Name, out _))
                .Select(file => _videoFileFactory.Create(file));
    }
}
