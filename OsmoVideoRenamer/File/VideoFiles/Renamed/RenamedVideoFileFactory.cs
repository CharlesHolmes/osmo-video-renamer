using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Renamed
{
    public class RenamedVideoFileFactory : IRenamedVideoFileFactory
    {
        public IRenamedVideoFile Create(string newName, IReadOnlyList<IRenamedCompanionFile> companions, INumberedVideoFile file) =>
            new RenamedVideoFile(newName, companions, file);
    }
}
