using OsmoVideoRenamer.File.DirectoryFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.DirectoryFiles
{
    [TestClass]
    public class DirectoryFileFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldInstantiateCorrectType()
        {
            var fileInfoMock = DirectoryFileMocking.GetMockedIFileInfo("DJI_20240315123456_0007_D.MP4");
            var factory = new DirectoryFileFactory();

            var result = factory.Create(fileInfoMock.Object);

            result.Should().BeAssignableTo<IDirectoryFile>();
            result.FileInfo.Should().BeSameAs(fileInfoMock.Object);
        }
    }
}
