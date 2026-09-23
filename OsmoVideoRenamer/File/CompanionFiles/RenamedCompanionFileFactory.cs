using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.CompanionFiles
{
    public class RenamedCompanionFileFactory : IRenamedCompanionFileFactory
    {
        public IRenamedCompanionFile Create(string newName, IDirectoryFile file) => new RenamedCompanionFile(newName, file);
    }
}
