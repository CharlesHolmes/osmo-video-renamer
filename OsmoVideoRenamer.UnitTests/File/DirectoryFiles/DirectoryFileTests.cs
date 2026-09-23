using OsmoVideoRenamer.File.DirectoryFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.DirectoryFiles
{
    [TestClass]
    public class DirectoryFileTests
    {
        [TestMethod]
        public void DirectoryFile_GivenIFileInfo_HasCorrectProperties()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("DJI_20240315123456_0007_D.LRF");

            var file = new DirectoryFile(fileInfoMock.Object);

            file.FileInfo.Should().BeSameAs(fileInfoMock.Object);
            file.Name.Should().Be("DJI_20240315123456_0007_D.LRF");
            file.BaseName.Should().Be("DJI_20240315123456_0007_D");
            file.FileExtension.Should().Be(".LRF");
        }

        [TestMethod]
        public void DirectoryFile_GivenIDirectoryFile_CopiesFileInfo()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("holiday.MP4");
            var otherMock = new Mock<IDirectoryFile>();
            otherMock.Setup(m => m.FileInfo).Returns(fileInfoMock.Object);

            var file = new DirectoryFile(otherMock.Object);

            file.FileInfo.Should().BeSameAs(fileInfoMock.Object);
            file.Name.Should().Be("holiday.MP4");
            file.BaseName.Should().Be("holiday");
            file.FileExtension.Should().Be(".MP4");
        }

        [TestMethod]
        public void DirectoryFile_WithoutExtension_HasEmptyExtension()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("system32");

            var file = new DirectoryFile(fileInfoMock.Object);

            file.BaseName.Should().Be("system32");
            file.FileExtension.Should().Be("");
        }
    }
}
