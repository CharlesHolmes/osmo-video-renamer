using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Renamed
{
    public class RenamedVideoFile : NumberedVideoFile, IRenamedVideoFile
    {
        public string NewName { get; private init; }

        public IReadOnlyList<IRenamedCompanionFile> Companions { get; private init; }

        public RenamedVideoFile(string newName, IReadOnlyList<IRenamedCompanionFile> companions, INumberedVideoFile file) : base(file)
        {
            NewName = newName;
            Companions = companions;
        }

        public void CommitRenameToDisk()
        {
            FileInfo.MoveTo(Path.Combine(FileInfo.Directory!.FullName, NewName));
        }
    }
}
