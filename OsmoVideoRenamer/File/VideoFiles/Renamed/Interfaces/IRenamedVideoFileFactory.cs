using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces
{
    public interface IRenamedVideoFileFactory
    {
        IRenamedVideoFile Create(string newName, IReadOnlyList<IRenamedCompanionFile> companions, INumberedVideoFile file);
    }
}
