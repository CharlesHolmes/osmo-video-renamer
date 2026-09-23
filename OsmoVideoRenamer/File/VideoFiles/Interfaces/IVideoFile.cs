using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Interfaces
{
    public interface IVideoFile : IDirectoryFile
    {
        DateTime CaptureTimestamp { get; }
        int SequenceNumber { get; }
    }
}
