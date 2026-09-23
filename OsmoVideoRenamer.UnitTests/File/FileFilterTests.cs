using OsmoVideoRenamer.File;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File
{
    [TestClass]
    public class FileFilterTests
    {
        private readonly Mock<IVideoFileFactory> _videoFileFactoryMock = new Mock<IVideoFileFactory>();

        [TestInitialize]
        public void Setup()
        {
            _videoFileFactoryMock.Reset();
        }

        [TestMethod]
        public void FileFilter_ShouldReturnDjiVideosCreatedByFactory_InDirectoryOrder()
        {
            var entry1 = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.MP4");
            var entry2 = DirectoryFileMocking.GetMockedIDirectoryFile("dji_20240315123500_0008_d.mp4");
            var video1 = new Mock<IVideoFile>().Object;
            var video2 = new Mock<IVideoFile>().Object;
            _videoFileFactoryMock.Setup(m => m.Create(entry1)).Returns(video1);
            _videoFileFactoryMock.Setup(m => m.Create(entry2)).Returns(video2);
            var filter = new FileFilter(_videoFileFactoryMock.Object);

            var result = filter.GetMatchingVideos([entry1, entry2]).ToList();

            result.Should().Equal(video1, video2);
            _videoFileFactoryMock.Verify(m => m.Create(entry1), Times.Once());
            _videoFileFactoryMock.Verify(m => m.Create(entry2), Times.Once());
            _videoFileFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileFilter_ShouldIgnoreCompanionsPhotosAndOtherFiles()
        {
            var entries = new[]
            {
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.LRF"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.WAV"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0008_D.JPG"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0008_D.DNG"),
                DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20241315123456_0009_D.MP4"),
                DirectoryFileMocking.GetMockedIDirectoryFile("GH010001.mp4"),
                DirectoryFileMocking.GetMockedIDirectoryFile("other_video.mp4"),
                DirectoryFileMocking.GetMockedIDirectoryFile("never gonna give you up.mp3"),
                DirectoryFileMocking.GetMockedIDirectoryFile("notes.txt"),
                DirectoryFileMocking.GetMockedIDirectoryFile("system32"),
            };
            var filter = new FileFilter(_videoFileFactoryMock.Object);

            var result = filter.GetMatchingVideos(entries).ToList();

            result.Should().BeEmpty();
            _videoFileFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileFilter_ShouldReturnEmptyForEmptyInput()
        {
            var filter = new FileFilter(_videoFileFactoryMock.Object);

            var result = filter.GetMatchingVideos([]).ToList();

            result.Should().BeEmpty();
            _videoFileFactoryMock.VerifyNoOtherCalls();
        }
    }
}
