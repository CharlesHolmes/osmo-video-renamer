using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.CompanionFiles.Interfaces
{
    public interface IRenamedCompanionFileFactory
    {
        IRenamedCompanionFile Create(string newName, IDirectoryFile file);
    }
}
