using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.Directory.Interfaces
{
    public interface IVideoDirectory
    {
        IReadOnlyList<IDirectoryFile> GetFilesInDirectory();
    }
}
