using OsmoVideoRenamer.File.CompanionFiles;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;

namespace OsmoVideoRenamer.UnitTests.File.CompanionFiles
{
    [TestClass]
    public class RenamedCompanionFileFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldProduceCorrectType()
        {
            var directoryFile = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315123456_0007_D.WAV");
            var factory = new RenamedCompanionFileFactory();

            var result = factory.Create("Trip - 001.WAV", directoryFile);

            result.Should().BeAssignableTo<IRenamedCompanionFile>();
            result.NewName.Should().Be("Trip - 001.WAV");
        }
    }
}
