using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.CompanionFiles
{
    public class RenamedCompanionFile : DirectoryFile, IRenamedCompanionFile
    {
        public string NewName { get; private init; }

        public RenamedCompanionFile(string newName, IDirectoryFile file) : base(file)
        {
            NewName = newName;
        }

        public void CommitRenameToDisk()
        {
            FileInfo.MoveTo(Path.Combine(FileInfo.Directory!.FullName, NewName));
        }
    }
}
