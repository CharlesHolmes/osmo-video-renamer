using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces
{
    public interface INumberedVideoFile : IVideoFile
    {
        int NewIndex { get; }
    }
}
