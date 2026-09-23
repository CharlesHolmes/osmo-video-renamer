using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Numbered
{
    public class NumberedVideoFile : VideoFile, INumberedVideoFile
    {
        public int NewIndex { get; private init; }

        public NumberedVideoFile(int newIndex, IVideoFile file) : base(file)
        {
            NewIndex = newIndex;
        }

        public NumberedVideoFile(INumberedVideoFile other) : this(other.NewIndex, other) { }
    }
}
