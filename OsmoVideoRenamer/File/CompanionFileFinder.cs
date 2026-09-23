using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.File
{
    /// <summary>
    /// Finds the files DJI writes next to a video: the .LRF low-resolution proxy and the
    /// optional .WAV audio backup. Both share the video's base name.
    /// </summary>
    public class CompanionFileFinder : ICompanionFileFinder
    {
        private static readonly string[] COMPANION_EXTENSIONS = [".LRF", ".WAV"];

        public IEnumerable<IDirectoryFile> GetCompanions(IVideoFile video, IEnumerable<IDirectoryFile> allFiles) =>
            allFiles.Where(file =>
                string.Equals(file.BaseName, video.BaseName, StringComparison.OrdinalIgnoreCase)
                && COMPANION_EXTENSIONS.Contains(file.FileExtension, StringComparer.OrdinalIgnoreCase));
    }
}
