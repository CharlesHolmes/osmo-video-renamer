using OsmoVideoRenamer.File.VideoFiles;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles
{
    [TestClass]
    public class VideoFileTests
    {
        [TestMethod]
        public void VideoFile_GivenIDirectoryFile_ParsesNameAndCopiesFileInfo()
        {
            var directoryFile = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.MP4");

            var file = new VideoFile(directoryFile);

            file.FileInfo.Should().BeSameAs(directoryFile.FileInfo);
            file.Name.Should().Be("DJI_20240315123456_0007_D.MP4");
            file.BaseName.Should().Be("DJI_20240315123456_0007_D");
            file.FileExtension.Should().Be(".MP4");
            file.CaptureTimestamp.Should().Be(new DateTime(2024, 3, 15, 12, 34, 56));
            file.SequenceNumber.Should().Be(7);
        }

        [TestMethod]
        public void VideoFile_GivenNonDjiName_Throws()
        {
            var directoryFile = DirectoryFileMocking.GetMockedIDirectoryFile("notes.txt");

            Action act = () => new VideoFile(directoryFile);

            act.Should().ThrowExactly<ArgumentException>();
        }

        [TestMethod]
        public void VideoFile_GivenIVideoFile_CopiesParsedValuesWithoutReparsing()
        {
            var other = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 42, new DateTime(2020, 1, 2, 3, 4, 5));

            var file = new VideoFile(other);

            file.FileInfo.Should().BeSameAs(other.FileInfo);
            file.Name.Should().Be("DJI_20240315123456_0007_D.MP4");
            file.SequenceNumber.Should().Be(42);
            file.CaptureTimestamp.Should().Be(new DateTime(2020, 1, 2, 3, 4, 5));
        }
    }
}
