using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.Interfaces
{
    public interface IFileFilter
    {
        IEnumerable<IVideoFile> GetMatchingVideos(IEnumerable<IDirectoryFile> allFiles);
    }
}
