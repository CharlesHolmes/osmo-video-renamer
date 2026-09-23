using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.File.Interfaces
{
    public interface IRenameCollisionChecker
    {
        void VerifyNoCollisions(IEnumerable<IRenamedVideoFile> renamedFiles, IEnumerable<IDirectoryFile> allFiles);
    }
}
