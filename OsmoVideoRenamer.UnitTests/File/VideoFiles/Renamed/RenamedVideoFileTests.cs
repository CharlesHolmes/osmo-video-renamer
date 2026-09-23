using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles.Renamed
{
    [TestClass]
    public class RenamedVideoFileTests
    {
        private static readonly DateTime _timestamp = new DateTime(2020, 1, 2, 3, 4, 5);

        [TestMethod]
        public void RenamedVideoFile_GivenINumberedVideoFile_HasCorrectProperties()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("DJI_20240315123456_0007_D.MP4");
            var numberedMock = new Mock<INumberedVideoFile>();
            numberedMock.Setup(m => m.FileInfo).Returns(fileInfoMock.Object);
            numberedMock.Setup(m => m.SequenceNumber).Returns(42);
            numberedMock.Setup(m => m.CaptureTimestamp).Returns(_timestamp);
            numberedMock.Setup(m => m.NewIndex).Returns(10);
            var companion = new Mock<IRenamedCompanionFile>().Object;
            var companions = new List<IRenamedCompanionFile> { companion };

            var file = new RenamedVideoFile("new file 5.MP4", companions, numberedMock.Object);

            file.NewIndex.Should().Be(10);
            file.Name.Should().Be("DJI_20240315123456_0007_D.MP4");
            file.FileExtension.Should().Be(".MP4");
            file.SequenceNumber.Should().Be(42);
            file.CaptureTimestamp.Should().Be(_timestamp);
            file.NewName.Should().Be("new file 5.MP4");
            file.Companions.Should().Equal(companion);
        }

        [TestMethod]
        public void RenamedVideoFile_CommitRenameToDisk_MovesOnlyTheVideoWithinItsDirectory()
        {
            var directoryInfoMock = new Mock<IDirectoryInfo>();
            directoryInfoMock.Setup(m => m.FullName).Returns("Some directory full name");
            var fileInfoMock = new Mock<IFileInfo>();
            fileInfoMock.Setup(m => m.Directory).Returns(directoryInfoMock.Object);
            var numberedMock = new Mock<INumberedVideoFile>();
            numberedMock.Setup(m => m.FileInfo).Returns(fileInfoMock.Object);
            var companionMock = new Mock<IRenamedCompanionFile>();
            var file = new RenamedVideoFile("new file 5.MP4", new List<IRenamedCompanionFile> { companionMock.Object }, numberedMock.Object);

            file.CommitRenameToDisk();

            directoryInfoMock.Verify(m => m.FullName, Times.Once());
            fileInfoMock.Verify(m => m.MoveTo(Path.Combine("Some directory full name", "new file 5.MP4")), Times.Once());
            directoryInfoMock.VerifyNoOtherCalls();
            fileInfoMock.VerifyNoOtherCalls();
            companionMock.VerifyNoOtherCalls();
        }
    }
}
