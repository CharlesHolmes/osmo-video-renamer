using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.Interfaces
{
    public interface ICompanionFileFinder
    {
        IEnumerable<IDirectoryFile> GetCompanions(IVideoFile video, IEnumerable<IDirectoryFile> allFiles);
    }
}
