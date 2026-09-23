using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces
{
    public interface IRenamedVideoFile : INumberedVideoFile
    {
        string NewName { get; }

        IReadOnlyList<IRenamedCompanionFile> Companions { get; }

        void CommitRenameToDisk();
    }
}
