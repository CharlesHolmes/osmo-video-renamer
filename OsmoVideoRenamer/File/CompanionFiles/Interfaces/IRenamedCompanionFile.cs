using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.CompanionFiles.Interfaces
{
    public interface IRenamedCompanionFile : IDirectoryFile
    {
        string NewName { get; }

        void CommitRenameToDisk();
    }
}
