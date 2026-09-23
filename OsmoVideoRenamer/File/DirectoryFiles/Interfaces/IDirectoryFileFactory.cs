using System.IO.Abstractions;

namespace OsmoVideoRenamer.File.DirectoryFiles.Interfaces
{
    public interface IDirectoryFileFactory
    {
        IDirectoryFile Create(IFileInfo fileInfo);
    }
}
