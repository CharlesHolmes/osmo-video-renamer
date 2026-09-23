using OsmoVideoRenamer.File.DirectoryFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Naming;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles
{
    public class VideoFile : DirectoryFile, IVideoFile
    {
        public DateTime CaptureTimestamp { get; private init; }

        public int SequenceNumber { get; private init; }

        public VideoFile(IDirectoryFile file) : base(file)
        {
            DjiVideoFileName parsedName = DjiVideoFileName.Parse(file.Name);
            CaptureTimestamp = parsedName.CaptureTimestamp;
            SequenceNumber = parsedName.SequenceNumber;
        }

        public VideoFile(IVideoFile other) : base(other)
        {
            CaptureTimestamp = other.CaptureTimestamp;
            SequenceNumber = other.SequenceNumber;
        }
    }
}
