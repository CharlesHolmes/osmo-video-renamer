using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles
{
    public class VideoFileFactory : IVideoFileFactory
    {
        public IVideoFile Create(IDirectoryFile file) => new VideoFile(file);
    }
}
