using OsmoVideoRenamer.File.VideoFiles.Numbered;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles.Numbered
{
    [TestClass]
    public class NumberedVideoFileFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldCreateCorrectType()
        {
            var videoFile = DirectoryFileMocking.GetMockedIVideoFile("DJI_20240315123456_0007_D.MP4", 7, new DateTime(2024, 3, 15, 12, 34, 56));
            var factory = new NumberedVideoFileFactory();

            var result = factory.Create(1, videoFile);

            result.Should().BeAssignableTo<INumberedVideoFile>();
            result.NewIndex.Should().Be(1);
        }
    }
}
