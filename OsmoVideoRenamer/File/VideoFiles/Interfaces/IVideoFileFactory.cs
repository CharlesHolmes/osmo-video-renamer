using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Interfaces
{
    public interface IVideoFileFactory
    {
        IVideoFile Create(IDirectoryFile file);
    }
}
