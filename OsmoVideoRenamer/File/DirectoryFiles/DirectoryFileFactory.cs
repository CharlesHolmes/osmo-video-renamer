using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.File.DirectoryFiles
{
    public class DirectoryFileFactory : IDirectoryFileFactory
    {
        public IDirectoryFile Create(IFileInfo fileInfo) => new DirectoryFile(fileInfo);
    }
}
