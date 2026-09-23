using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.VideoFiles.Renamed
{
    [TestClass]
    public class RenamedVideoFileFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldProduceCorrectType()
        {
            var numberedMock = new Mock<INumberedVideoFile>();
            var companions = new List<IRenamedCompanionFile>();
            var factory = new RenamedVideoFileFactory();

            var result = factory.Create("some new name", companions, numberedMock.Object);

            result.Should().BeAssignableTo<IRenamedVideoFile>();
            result.NewName.Should().Be("some new name");
            result.Companions.Should().BeSameAs(companions);
        }
    }
}
