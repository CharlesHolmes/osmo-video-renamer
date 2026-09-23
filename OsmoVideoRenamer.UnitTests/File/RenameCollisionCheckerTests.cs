using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;
using OsmoVideoRenamer.UnitTests.Logging;

namespace OsmoVideoRenamer.UnitTests.File
{
    [TestClass]
    public class RenameCollisionCheckerTests
    {
        private readonly Mock<ILogger<RenameCollisionChecker>> _loggerMock = new Mock<ILogger<RenameCollisionChecker>>();

        [TestInitialize]
        public void Setup()
        {
            _loggerMock.Reset();
        }

        [TestMethod]
        public void Checker_ShouldPassWhenNoPlannedNameCollides()
        {
            var renamed = new[]
            {
                Renamed("Trip - 001.MP4", "Trip - 001.LRF", "Trip - 001.WAV"),
                Renamed("Trip - 002.MP4", "Trip - 002.LRF"),
            };
            var existing = Existing(
                "DJI_20240315120000_0001_D.MP4", "DJI_20240315120000_0001_D.LRF", "DJI_20240315120000_0001_D.WAV",
                "DJI_20240315120100_0002_D.MP4", "DJI_20240315120100_0002_D.LRF", "notes.txt");
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(renamed, existing);

            act.Should().NotThrow();
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Never());
        }

        [TestMethod]
        public void Checker_ShouldThrowWhenPlannedVideoNameAlreadyExists()
        {
            var renamed = new[] { Renamed("Trip - 001.MP4"), Renamed("Trip - 002.MP4") };
            var existing = Existing("DJI_20240315120000_0001_D.MP4", "DJI_20240315120100_0002_D.MP4", "Trip - 002.MP4");
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(renamed, existing);

            act.Should().ThrowExactly<IOException>().WithMessage("*Trip - 002.MP4*");
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
        }

        [TestMethod]
        public void Checker_ShouldThrowWhenPlannedCompanionNameAlreadyExists()
        {
            var renamed = new[] { Renamed("Trip - 001.MP4", "Trip - 001.LRF") };
            var existing = Existing("DJI_20240315120000_0001_D.MP4", "DJI_20240315120000_0001_D.LRF", "Trip - 001.LRF");
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(renamed, existing);

            act.Should().ThrowExactly<IOException>().WithMessage("*Trip - 001.LRF*");
        }

        [TestMethod]
        public void Checker_ShouldThrowWhenTwoPlannedNamesAreEqual()
        {
            var renamed = new[] { Renamed("Trip - 001.MP4"), Renamed("Trip - 001.MP4") };
            var existing = Existing("DJI_20240315120000_0001_D.MP4", "DJI_20240315120100_0002_D.MP4");
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(renamed, existing);

            act.Should().ThrowExactly<IOException>().WithMessage("*Trip - 001.MP4*");
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
        }

        [TestMethod]
        public void Checker_ShouldCompareNamesCaseInsensitively()
        {
            var renamed = new[] { Renamed("Trip - 001.MP4") };
            var existing = Existing("DJI_20240315120000_0001_D.MP4", "trip - 001.mp4");
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(renamed, existing);

            act.Should().ThrowExactly<IOException>();
        }

        [TestMethod]
        public void Checker_ShouldPassForEmptyInput()
        {
            var checker = new RenameCollisionChecker(_loggerMock.Object);

            Action act = () => checker.VerifyNoCollisions(Array.Empty<IRenamedVideoFile>(), Existing("notes.txt"));

            act.Should().NotThrow();
        }

        private static IRenamedVideoFile Renamed(string newName, params string[] companionNewNames)
        {
            var companions = new List<IRenamedCompanionFile>();
            foreach (string companionNewName in companionNewNames)
            {
                var companion = new Mock<IRenamedCompanionFile>();
                companion.Setup(m => m.NewName).Returns(companionNewName);
                companions.Add(companion.Object);
            }

            var file = new Mock<IRenamedVideoFile>();
            file.Setup(m => m.NewName).Returns(newName);
            file.Setup(m => m.Companions).Returns(companions);
            return file.Object;
        }

        private static IDirectoryFile[] Existing(params string[] names) =>
            names.Select(DirectoryFileMocking.GetMockedIDirectoryFile).ToArray();
    }
}
