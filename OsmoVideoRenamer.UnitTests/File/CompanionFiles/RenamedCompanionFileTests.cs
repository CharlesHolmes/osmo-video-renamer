using OsmoVideoRenamer.File.CompanionFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.UnitTests.File.CompanionFiles
{
    [TestClass]
    public class RenamedCompanionFileTests
    {
        [TestMethod]
        public void RenamedCompanionFile_GivenIDirectoryFile_HasCorrectProperties()
        {
            var directoryFile = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.LRF");

            var file = new RenamedCompanionFile("Trip - 001.LRF", directoryFile);

            file.FileInfo.Should().BeSameAs(directoryFile.FileInfo);
            file.Name.Should().Be("DJI_20240315123456_0007_D.LRF");
            file.BaseName.Should().Be("DJI_20240315123456_0007_D");
            file.FileExtension.Should().Be(".LRF");
            file.NewName.Should().Be("Trip - 001.LRF");
        }

        [TestMethod]
        public void RenamedCompanionFile_CommitRenameToDisk_MovesFileWithinItsDirectory()
        {
            var directoryInfoMock = new Mock<IDirectoryInfo>();
            directoryInfoMock.Setup(m => m.FullName).Returns("Some directory full name");
            var fileInfoMock = new Mock<IFileInfo>();
            fileInfoMock.Setup(m => m.Directory).Returns(directoryInfoMock.Object);
            var directoryFileMock = new Mock<IDirectoryFile>();
            directoryFileMock.Setup(m => m.FileInfo).Returns(fileInfoMock.Object);
            var file = new RenamedCompanionFile("Trip - 001.LRF", directoryFileMock.Object);

            file.CommitRenameToDisk();

            directoryInfoMock.Verify(m => m.FullName, Times.Once());
            fileInfoMock.Verify(m => m.MoveTo(Path.Combine("Some directory full name", "Trip - 001.LRF")), Times.Once());
            directoryInfoMock.VerifyNoOtherCalls();
            fileInfoMock.VerifyNoOtherCalls();
        }
    }
}
