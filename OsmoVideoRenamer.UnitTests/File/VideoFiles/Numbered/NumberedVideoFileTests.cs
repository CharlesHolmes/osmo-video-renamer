using OsmoVideoRenamer.File.VideoFiles.Numbered;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles.Numbered
{
    [TestClass]
    public class NumberedVideoFileTests
    {
        private static readonly DateTime _timestamp = new DateTime(2024, 3, 15, 12, 34, 56);

        [TestMethod]
        public void NumberedVideoFile_GivenIVideoFile_HasCorrectProperties()
        {
            var videoFile = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 7, _timestamp);

            var file = new NumberedVideoFile(9, videoFile);

            file.NewIndex.Should().Be(9);
            file.FileInfo.Should().BeSameAs(videoFile.FileInfo);
            file.Name.Should().Be("DJI_20240315123456_0007_D.MP4");
            file.BaseName.Should().Be("DJI_20240315123456_0007_D");
            file.FileExtension.Should().Be(".MP4");
            file.SequenceNumber.Should().Be(7);
            file.CaptureTimestamp.Should().Be(_timestamp);
        }

        [TestMethod]
        public void NumberedVideoFile_GivenINumberedVideoFile_HasCorrectProperties()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("DJI_20240315123456_0007_D.MP4");
            var numberedMock = new Mock<INumberedVideoFile>();
            numberedMock.Setup(m => m.FileInfo).Returns(fileInfoMock.Object);
            numberedMock.Setup(m => m.SequenceNumber).Returns(7);
            numberedMock.Setup(m => m.CaptureTimestamp).Returns(_timestamp);
            numberedMock.Setup(m => m.NewIndex).Returns(10);

            var file = new NumberedVideoFile(numberedMock.Object);

            file.NewIndex.Should().Be(10);
            file.FileInfo.Should().BeSameAs(fileInfoMock.Object);
            file.Name.Should().Be("DJI_20240315123456_0007_D.MP4");
            file.FileExtension.Should().Be(".MP4");
            file.SequenceNumber.Should().Be(7);
            file.CaptureTimestamp.Should().Be(_timestamp);
        }
    }
}
