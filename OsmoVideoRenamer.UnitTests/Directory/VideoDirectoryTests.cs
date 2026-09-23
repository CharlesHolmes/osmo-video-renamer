using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.Directory;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.UnitTests.Directory
{
    [TestClass]
    public class VideoDirectoryTests
    {
        private readonly Mock<ILogger<VideoDirectory>> _loggerMock = new Mock<ILogger<VideoDirectory>>();
        private readonly Mock<IFileSystem> _fileSystemMock = new Mock<IFileSystem>();
        private readonly Mock<IDirectoryInfoFactory> _directoryInfoFactoryMock = new Mock<IDirectoryInfoFactory>();
        private readonly Mock<IDirectoryFileFactory> _directoryFileFactoryMock = new Mock<IDirectoryFileFactory>();
        private readonly string _directoryPath = "asdf1234";

        [TestInitialize]
        public void Setup()
        {
            _loggerMock.Reset();
            _fileSystemMock.Reset();
            _directoryInfoFactoryMock.Reset();
            _directoryFileFactoryMock.Reset();
            _fileSystemMock.Setup(m => m.DirectoryInfo).Returns(_directoryInfoFactoryMock.Object);
        }

        [TestMethod]
        public void VideoDirectory_ShouldThrow_WhenDirectoryDoesNotExist()
        {
            var badDirectory = new Mock<IDirectoryInfo>();
            badDirectory.Setup(m => m.Exists).Returns(false);
            _directoryInfoFactoryMock.Setup(m => m.New(_directoryPath)).Returns(badDirectory.Object);

            Action act = () => new VideoDirectory(
                _loggerMock.Object,
                _fileSystemMock.Object,
                _directoryFileFactoryMock.Object,
                _directoryPath);

            act.Should().ThrowExactly<DirectoryNotFoundException>();
        }

        [TestMethod]
        public void VideoDirectory_ShouldCreateDirectoryFileObjects_InDirectoryOrder()
        {
            int fileCount = 5;
            IFileInfo[] directoryFiles = new IFileInfo[fileCount];
            IDirectoryFile[] expected = new IDirectoryFile[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                directoryFiles[i] = new Mock<IFileInfo>().Object;
                expected[i] = new Mock<IDirectoryFile>().Object;
                _directoryFileFactoryMock.Setup(m => m.Create(directoryFiles[i])).Returns(expected[i]);
            }

            var goodDirectory = new Mock<IDirectoryInfo>();
            goodDirectory.Setup(m => m.Exists).Returns(true);
            goodDirectory.Setup(m => m.GetFiles()).Returns(directoryFiles);
            _directoryInfoFactoryMock.Setup(m => m.New(_directoryPath)).Returns(goodDirectory.Object);
            var videoDirectory = new VideoDirectory(
                _loggerMock.Object,
                _fileSystemMock.Object,
                _directoryFileFactoryMock.Object,
                _directoryPath);

            var result = videoDirectory.GetFilesInDirectory();

            result.Should().Equal(expected);
            for (int i = 0; i < fileCount; i++)
            {
                _directoryFileFactoryMock.Verify(m => m.Create(directoryFiles[i]), Times.Once());
            }
        }
    }
}
