using OsmoVideoRenamer.File.VideoFiles;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles
{
    [TestClass]
    public class VideoFileFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldInstantiateCorrectType()
        {
            var directoryFile = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.MP4");
            var factory = new VideoFileFactory();

            var result = factory.Create(directoryFile);

            result.Should().BeAssignableTo<IVideoFile>();
            result.SequenceNumber.Should().Be(7);
        }
    }
}
