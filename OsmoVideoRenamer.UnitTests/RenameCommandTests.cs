using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.ConsoleWrapping;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;

namespace OsmoVideoRenamer.UnitTests
{
    [TestClass]
    public class RenameCommandTests
    {
        private readonly Mock<ILogger<RenameCommand>> _loggerMock = new Mock<ILogger<RenameCommand>>();
        private readonly Mock<IVideoDirectoryFactory> _directoryFactoryMock = new Mock<IVideoDirectoryFactory>();
        private readonly Mock<IVideoDirectory> _directoryMock = new Mock<IVideoDirectory>();
        private readonly Mock<IFileFilter> _fileFilterMock = new Mock<IFileFilter>();
        private readonly Mock<IFileSort> _fileSortMock = new Mock<IFileSort>();
        private readonly Mock<IFileRename> _fileRenameMock = new Mock<IFileRename>();
        private readonly Mock<IRenameCollisionChecker> _collisionCheckerMock = new Mock<IRenameCollisionChecker>();
        private readonly Mock<IConsoleWrapper> _consoleWrapperMock = new Mock<IConsoleWrapper>();
        private readonly Mock<IRenamedVideoFile> _testRenamed1 = new Mock<IRenamedVideoFile>();
        private readonly Mock<IRenamedVideoFile> _testRenamed2 = new Mock<IRenamedVideoFile>();
        private readonly Mock<IRenamedCompanionFile> _testCompanion1 = new Mock<IRenamedCompanionFile>();
        private readonly Mock<IRenamedCompanionFile> _testCompanion2 = new Mock<IRenamedCompanionFile>();
        private readonly IDirectoryFile[] _allFiles = [new Mock<IDirectoryFile>().Object, new Mock<IDirectoryFile>().Object, new Mock<IDirectoryFile>().Object];
        private readonly IVideoFile[] _filtered = [new Mock<IVideoFile>().Object, new Mock<IVideoFile>().Object];
        private readonly List<INumberedVideoFile> _numbered = [new Mock<INumberedVideoFile>().Object, new Mock<INumberedVideoFile>().Object];
        private List<IRenamedVideoFile> _renamed = new List<IRenamedVideoFile>();
        private readonly string _fileLocation = "asdf1234";
        private readonly string _prefix = "prefix1234";
        private readonly string _suffix = "1234suffix";
        private const int _startingNumber = 5;
        private const int _digitCount = 7;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock.Reset();
            _directoryFactoryMock.Reset();
            _directoryMock.Reset();
            _fileFilterMock.Reset();
            _fileSortMock.Reset();
            _fileRenameMock.Reset();
            _collisionCheckerMock.Reset();
            _consoleWrapperMock.Reset();
            _testRenamed1.Reset();
            _testRenamed2.Reset();
            _testCompanion1.Reset();
            _testCompanion2.Reset();

            _directoryFactoryMock.Setup(m => m.Create(_fileLocation)).Returns(_directoryMock.Object);
            _directoryMock.Setup(m => m.GetFilesInDirectory()).Returns(_allFiles);
            _fileFilterMock.Setup(m => m.GetMatchingVideos(_allFiles)).Returns(_filtered);
            _fileSortMock.Setup(m => m.GetOrderedFiles(_filtered, _startingNumber)).Returns(_numbered);
            SetupCompanion(_testCompanion1, "old1.LRF", "new1.LRF");
            SetupCompanion(_testCompanion2, "old1.WAV", "new1.WAV");
            SetupRenamedFile(_testRenamed1, "old1", "new1", 1, new List<IRenamedCompanionFile> { _testCompanion1.Object, _testCompanion2.Object });
            SetupRenamedFile(_testRenamed2, "old2", "new file", 2, new List<IRenamedCompanionFile>());
            _renamed = new List<IRenamedVideoFile> { _testRenamed1.Object, _testRenamed2.Object };
            _fileRenameMock.Setup(m => m.GetRenamedFiles(_numbered, _allFiles, _prefix, _suffix, _digitCount)).Returns(_renamed);
        }

        [TestMethod]
        public void Command_ShouldRunThePipelineInOrder()
        {
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount, true);

            _directoryFactoryMock.Verify(m => m.Create(_fileLocation), Times.Once());
            _directoryMock.Verify(m => m.GetFilesInDirectory(), Times.Once());
            _fileFilterMock.Verify(m => m.GetMatchingVideos(_allFiles), Times.Once());
            _fileSortMock.Verify(m => m.GetOrderedFiles(_filtered, _startingNumber), Times.Once());
            _fileRenameMock.Verify(m => m.GetRenamedFiles(_numbered, _allFiles, _prefix, _suffix, _digitCount), Times.Once());
            _collisionCheckerMock.Verify(m => m.VerifyNoCollisions(_renamed, _allFiles), Times.Once());
            _directoryFactoryMock.VerifyNoOtherCalls();
            _directoryMock.VerifyNoOtherCalls();
            _fileFilterMock.VerifyNoOtherCalls();
            _fileSortMock.VerifyNoOtherCalls();
            _fileRenameMock.VerifyNoOtherCalls();
            _collisionCheckerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Command_ShouldWritePreviewToConsole_IncludingCompanions()
        {
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount, true);

            _consoleWrapperMock.Verify(m => m.WriteLine("1: old1 -> new1"), Times.Once());
            _consoleWrapperMock.Verify(m => m.WriteLine("    old1.LRF -> new1.LRF"), Times.Once());
            _consoleWrapperMock.Verify(m => m.WriteLine("    old1.WAV -> new1.WAV"), Times.Once());
            _consoleWrapperMock.Verify(m => m.WriteLine("2: old2 -> new file"), Times.Once());
            _consoleWrapperMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Command_ShouldNotCommitToDisk_IfDryRun()
        {
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount, true);

            _collisionCheckerMock.Verify(m => m.VerifyNoCollisions(_renamed, _allFiles), Times.Once());
            _testRenamed1.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testRenamed2.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testCompanion1.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testCompanion2.Verify(m => m.CommitRenameToDisk(), Times.Never());
        }

        [TestMethod]
        public void Command_ShouldCommitVideosAndCompanionsToDisk_IfNotDryRun()
        {
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount);

            _testRenamed1.Verify(m => m.CommitRenameToDisk(), Times.Once());
            _testCompanion1.Verify(m => m.CommitRenameToDisk(), Times.Once());
            _testCompanion2.Verify(m => m.CommitRenameToDisk(), Times.Once());
            _testRenamed2.Verify(m => m.CommitRenameToDisk(), Times.Once());
        }

        [TestMethod]
        public void Command_ShouldCommitEachVideoBeforeItsCompanions()
        {
            var order = new List<string>();
            _testRenamed1.Setup(m => m.CommitRenameToDisk()).Callback(() => order.Add("video1"));
            _testCompanion1.Setup(m => m.CommitRenameToDisk()).Callback(() => order.Add("companion1"));
            _testCompanion2.Setup(m => m.CommitRenameToDisk()).Callback(() => order.Add("companion2"));
            _testRenamed2.Setup(m => m.CommitRenameToDisk()).Callback(() => order.Add("video2"));
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount);

            order.Should().Equal("video1", "companion1", "companion2", "video2");
        }

        [TestMethod]
        public void Command_ShouldPrintMessageAndStop_IfNoMatchingFiles()
        {
            _fileSortMock.Setup(m => m.GetOrderedFiles(_filtered, _startingNumber)).Returns(new List<INumberedVideoFile>());
            var command = CreateCommand();

            command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount);

            _consoleWrapperMock.Verify(m => m.WriteLine("No DJI Osmo videos found in asdf1234."), Times.Once());
            _consoleWrapperMock.VerifyNoOtherCalls();
            _fileRenameMock.VerifyNoOtherCalls();
            _collisionCheckerMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Command_ShouldNotPrintOrRename_IfCollisionCheckFails()
        {
            _collisionCheckerMock.Setup(m => m.VerifyNoCollisions(_renamed, _allFiles)).Throws(new IOException("collision"));
            var command = CreateCommand();

            Action act = () => command.Rename(_fileLocation, _prefix, _suffix, _startingNumber, _digitCount);

            act.Should().ThrowExactly<IOException>();
            _consoleWrapperMock.VerifyNoOtherCalls();
            _testRenamed1.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testRenamed2.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testCompanion1.Verify(m => m.CommitRenameToDisk(), Times.Never());
            _testCompanion2.Verify(m => m.CommitRenameToDisk(), Times.Never());
        }

        private RenameCommand CreateCommand() =>
            new RenameCommand(
                _loggerMock.Object,
                _directoryFactoryMock.Object,
                _fileFilterMock.Object,
                _fileSortMock.Object,
                _fileRenameMock.Object,
                _collisionCheckerMock.Object,
                _consoleWrapperMock.Object);

        private static void SetupRenamedFile(Mock<IRenamedVideoFile> file, string oldName, string newName, int order, IReadOnlyList<IRenamedCompanionFile> companions)
        {
            file.Setup(m => m.Name).Returns(oldName);
            file.Setup(m => m.NewName).Returns(newName);
            file.Setup(m => m.NewIndex).Returns(order);
            file.Setup(m => m.Companions).Returns(companions);
        }

        private static void SetupCompanion(Mock<IRenamedCompanionFile> file, string oldName, string newName)
        {
            file.Setup(m => m.Name).Returns(oldName);
            file.Setup(m => m.NewName).Returns(newName);
        }
    }
}
