using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.UnitTests.Logging;

namespace OsmoVideoRenamer.UnitTests.File
{
    [TestClass]
    public class FileSortTests
    {
        private readonly Mock<ILogger<FileSort>> _loggerMock = new Mock<ILogger<FileSort>>();
        private readonly Mock<INumberedVideoFileFactory> _numberedFactoryMock = new Mock<INumberedVideoFileFactory>();

        [TestInitialize]
        public void Setup()
        {
            _loggerMock.Reset();
            _numberedFactoryMock.Reset();
        }

        [TestMethod]
        public void FileSort_ShouldOrderBySequenceNumber()
        {
            var file1 = Video("DJI_20240315120000_0001_D.MP4", 1, Time(12, 0));
            var file2 = Video("DJI_20240315120100_0002_D.MP4", 2, Time(12, 1));
            var file3 = Video("DJI_20240315120200_0003_D.MP4", 3, Time(12, 2));
            var file4 = Video("DJI_20240315120300_0010_D.MP4", 10, Time(12, 3));
            var numbered1 = SetupNumbered(1, file1);
            var numbered2 = SetupNumbered(2, file2);
            var numbered3 = SetupNumbered(3, file3);
            var numbered4 = SetupNumbered(4, file4);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file3, file1, file4, file2], null);

            result.Should().Equal(numbered1, numbered2, numbered3, numbered4);
            _loggerMock.VerifyLogged(LogLevel.Warning, Times.Never());
        }

        [TestMethod]
        public void FileSort_ShouldUseStartingNumber()
        {
            var file1 = Video("DJI_20240315120000_0001_D.MP4", 1, Time(12, 0));
            var file2 = Video("DJI_20240315120100_0002_D.MP4", 2, Time(12, 1));
            var numbered5 = SetupNumbered(5, file1);
            var numbered6 = SetupNumbered(6, file2);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file2, file1], 5);

            result.Should().Equal(numbered5, numbered6);
        }

        [TestMethod]
        public void FileSort_ShouldAllowStartingNumberZero()
        {
            var file1 = Video("DJI_20240315120000_0001_D.MP4", 1, Time(12, 0));
            var file2 = Video("DJI_20240315120100_0002_D.MP4", 2, Time(12, 1));
            var numbered0 = SetupNumbered(0, file1);
            var numbered1 = SetupNumbered(1, file2);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file1, file2], 0);

            result.Should().Equal(numbered0, numbered1);
        }

        [TestMethod]
        public void FileSort_ShouldThrowForNegativeStartingNumber()
        {
            var file1 = Video("DJI_20240315120000_0001_D.MP4", 1, Time(12, 0));
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            Action act = () => sort.GetOrderedFiles([file1], -1);

            act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("startingNumber");
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
            _numberedFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileSort_ShouldOrderBySequenceAndWarn_WhenTimestampGoesBackwards()
        {
            var file1 = Video("DJI_20240315150000_0001_D.MP4", 1, Time(15, 0));
            var file2 = Video("DJI_20240315090000_0002_D.MP4", 2, Time(9, 0));
            var numbered1 = SetupNumbered(1, file1);
            var numbered2 = SetupNumbered(2, file2);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file2, file1], null);

            result.Should().Equal(numbered1, numbered2);
            _loggerMock.VerifyLogged(LogLevel.Warning, Times.Once());
        }

        [TestMethod]
        public void FileSort_ShouldNotWarn_WhenTimestampsAreEqual()
        {
            var file1 = Video("DJI_20240315120000_0001_D.MP4", 1, Time(12, 0));
            var file2 = Video("DJI_20240315120000_0002_D.MP4", 2, Time(12, 0));
            var file3 = Video("DJI_20240315120000_0003_D.MP4", 3, Time(12, 0));
            var numbered1 = SetupNumbered(1, file1);
            var numbered2 = SetupNumbered(2, file2);
            var numbered3 = SetupNumbered(3, file3);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file3, file2, file1], null);

            result.Should().Equal(numbered1, numbered2, numbered3);
            _loggerMock.VerifyLogged(LogLevel.Warning, Times.Never());
        }

        [TestMethod]
        public void FileSort_ShouldWarnOnlyOnce_WhenTimestampsGoBackwardsMoreThanOnce()
        {
            var file1 = Video("DJI_20240315150000_0001_D.MP4", 1, Time(15, 0));
            var file2 = Video("DJI_20240315090000_0002_D.MP4", 2, Time(9, 0));
            var file3 = Video("DJI_20240315080000_0003_D.MP4", 3, Time(8, 0));
            var file4 = Video("DJI_20240315200000_0004_D.MP4", 4, Time(20, 0));
            var numbered1 = SetupNumbered(1, file1);
            var numbered2 = SetupNumbered(2, file2);
            var numbered3 = SetupNumbered(3, file3);
            var numbered4 = SetupNumbered(4, file4);
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([file4, file3, file2, file1], null);

            result.Should().Equal(numbered1, numbered2, numbered3, numbered4);
            _loggerMock.VerifyLogged(LogLevel.Warning, Times.Once());
        }

        [TestMethod]
        public void FileSort_ShouldThrowForDuplicateSequenceNumbers()
        {
            var file1 = Video("DJI_20240315090000_0003_D.MP4", 3, Time(9, 0));
            var file2 = Video("DJI_20240315140000_0003_D.MP4", 3, Time(14, 0));
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            Action act = () => sort.GetOrderedFiles([file1, file2], null);

            act.Should().ThrowExactly<ArgumentException>().WithMessage("*3*");
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
            _numberedFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileSort_ShouldReturnEmptyForEmptyInput()
        {
            var sort = new FileSort(_loggerMock.Object, _numberedFactoryMock.Object);

            var result = sort.GetOrderedFiles([], null);

            result.Should().BeEmpty();
            _numberedFactoryMock.VerifyNoOtherCalls();
        }

        private static DateTime Time(int hour, int minute) => new DateTime(2024, 3, 15, hour, minute, 0);

        private static IVideoFile Video(string name, int sequenceNumber, DateTime timestamp) =>
            DirectoryFileMocking.GetMockedIVideoFile(name, sequenceNumber, timestamp);

        private INumberedVideoFile SetupNumbered(int newIndex, IVideoFile file)
        {
            var numbered = new Mock<INumberedVideoFile>().Object;
            _numberedFactoryMock.Setup(m => m.Create(newIndex, file)).Returns(numbered);
            return numbered;
        }
    }
}
