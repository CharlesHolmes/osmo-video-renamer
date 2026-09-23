using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.File.DirectoryFiles
{
    public class DirectoryFile : IDirectoryFile
    {
        public IFileInfo FileInfo { get; private init; }

        public string Name => FileInfo.Name;

        public string BaseName => Path.GetFileNameWithoutExtension(FileInfo.Name);

        public string FileExtension => FileInfo.Extension;

        public DirectoryFile(IFileInfo fileInfo)
        {
            FileInfo = fileInfo;
        }

        public DirectoryFile(IDirectoryFile other)
        {
            FileInfo = other.FileInfo;
        }
    }
}
