using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.File.Interfaces
{
    public interface IFileRename
    {
        IList<IRenamedVideoFile> GetRenamedFiles(
            IList<INumberedVideoFile> files,
            IEnumerable<IDirectoryFile> allFiles,
            string? prefix,
            string? suffix,
            int? digitCount);
    }
}
