using OsmoVideoRenamer.File;

namespace OsmoVideoRenamer.UnitTests.File
{
    [TestClass]
    public class CompanionFileFinderTests
    {
        private static readonly DateTime _timestamp = new DateTime(2024, 3, 15, 12, 34, 56);

        [TestMethod]
        public void Finder_ShouldReturnLrfAndWavWithSameBaseName_InDirectoryOrder()
        {
            var video = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 7, _timestamp);
            var videoEntry = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.MP4");
            var lrf = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.LRF");
            var wav = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.WAV");
            var otherLrf = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0008_D.LRF");
            var finder = new CompanionFileFinder();

            var result = finder.GetCompanions(video, [videoEntry, wav, otherLrf, lrf]);

            result.Should().Equal(wav, lrf);
        }

        [TestMethod]
        public void Finder_ShouldMatchBaseNameAndExtensionCaseInsensitively()
        {
            var video = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 7, _timestamp);
            var lrf = DirectoryFileMocking.GetMockedIDirectoryFile("dji_20240315123456_0007_d.lrf");
            var wav = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.wav");
            var finder = new CompanionFileFinder();

            var result = finder.GetCompanions(video, [lrf, wav]);

            result.Should().Equal(lrf, wav);
        }

        [TestMethod]
        public void Finder_ShouldIgnoreOtherExtensionsAndOtherBaseNames()
        {
            var video = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 7, _timestamp);
            var entries = new[]
            {
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.MP4"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.JPG"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.DNG"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.SRT"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0008_D.LRF"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0008_D.WAV"),
                DirectoryFileMocking.GetMockedIDirectoryFile("notes.txt"),
            };
            var finder = new CompanionFileFinder();

            var result = finder.GetCompanions(video, entries);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Finder_ShouldReturnEmptyForEmptyDirectory()
        {
            var video = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 7, _timestamp);
            var finder = new CompanionFileFinder();

            var result = finder.GetCompanions(video, []);

            result.Should().BeEmpty();
        }
    }
}
