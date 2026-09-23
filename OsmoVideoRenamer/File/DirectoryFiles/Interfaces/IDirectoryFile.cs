using System.IO.Abstractions;

namespace OsmoVideoRenamer.File.DirectoryFiles.Interfaces
{
    public interface IDirectoryFile
    {
        IFileInfo FileInfo { get; }
        string Name { get; }
        string BaseName { get; }
        string FileExtension { get; }
    }
}
