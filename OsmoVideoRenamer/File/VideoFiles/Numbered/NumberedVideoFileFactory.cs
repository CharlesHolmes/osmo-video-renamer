using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.File.VideoFiles.Numbered
{
    public class NumberedVideoFileFactory : INumberedVideoFileFactory
    {
        public INumberedVideoFile Create(int newIndex, IVideoFile file) => new NumberedVideoFile(newIndex, file);
    }
}
