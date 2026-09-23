using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces
{
    public interface INumberedVideoFileFactory
    {
        INumberedVideoFile Create(int newIndex, IVideoFile file);
    }
}
