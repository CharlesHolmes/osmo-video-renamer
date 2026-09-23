using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.File;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;
using OsmoVideoRenamer.UnitTests.Logging;

namespace OsmoVideoRenamer.UnitTests.File
{
    [TestClass]
    public class FileRenameTests
    {
        private readonly Mock<ILogger<FileRename>> _loggerMock = new Mock<ILogger<FileRename>>();
        private readonly Mock<IRenamedVideoFileFactory> _renamedVideoFactoryMock = new Mock<IRenamedVideoFileFactory>();
        private readonly Mock<IRenamedCompanionFileFactory> _renamedCompanionFactoryMock = new Mock<IRenamedCompanionFileFactory>();
        private readonly Mock<ICompanionFileFinder> _companionFinderMock = new Mock<ICompanionFileFinder>();
        private readonly IDirectoryFile[] _allFiles = [DirectoryFileMocking.GetMockedIDirectoryFile("unrelated.txt")];

        [TestInitialize]
        public void Setup()
        {
            _loggerMock.Reset();
            _renamedVideoFactoryMock.Reset();
            _renamedCompanionFactoryMock.Reset();
            _companionFinderMock.Reset();
            _companionFinderMock
                .Setup(m => m.GetCompanions(It.IsAny<IVideoFile>(), _allFiles))
                .Returns(Array.Empty<IDirectoryFile>());
            _renamedVideoFactoryMock
                .Setup(m => m.Create(It.IsAny<string>(), It.IsAny<IReadOnlyList<IRenamedCompanionFile>>(), It.IsAny<INumberedVideoFile>()))
                .Returns<string, IReadOnlyList<IRenamedCompanionFile>, INumberedVideoFile>((newName, companions, file) => GetRenamedVideo(newName, companions));
            _renamedCompanionFactoryMock
                .Setup(m => m.Create(It.IsAny<string>(), It.IsAny<IDirectoryFile>()))
                .Returns<string, IDirectoryFile>((newName, file) => GetRenamedCompanion(newName));
        }

        [TestMethod]
        public void FileRename_ShouldWorkWhenPrefixSupplied()
        {
            CheckPrefixSuffixNaming(
                "some result - ",
                null,
                "some result - 1.MP4",
                "some result - 2.MP4",
                "some result - 3.MP4",
                "some result - 4.MP4",
                "some result - 5.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldWorkWhenSuffixSupplied()
        {
            CheckPrefixSuffixNaming(
                null,
                " - some suffix to follow",
                "1 - some suffix to follow.MP4",
                "2 - some suffix to follow.MP4",
                "3 - some suffix to follow.MP4",
                "4 - some suffix to follow.MP4",
                "5 - some suffix to follow.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldWorkWhenPrefixAndSuffixSupplied()
        {
            CheckPrefixSuffixNaming(
                "a prefix - ",
                " - and a suffix",
                "a prefix - 1 - and a suffix.MP4",
                "a prefix - 2 - and a suffix.MP4",
                "a prefix - 3 - and a suffix.MP4",
                "a prefix - 4 - and a suffix.MP4",
                "a prefix - 5 - and a suffix.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldWorkWhenNeitherPrefixNorSuffixSupplied()
        {
            CheckPrefixSuffixNaming(null, null, "1.MP4", "2.MP4", "3.MP4", "4.MP4", "5.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldWorkWhenDigitCountSpecified()
        {
            var input = GetMockedInput(5);
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, null, null, 3);

            result.Select(r => r.NewName).Should().Equal("001.MP4", "002.MP4", "003.MP4", "004.MP4", "005.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldPadWhenDigitCountNotSpecified()
        {
            var input = GetMockedInput(10);
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, null, null, null);

            result.Select(r => r.NewName).Should().Equal(
                "01.MP4", "02.MP4", "03.MP4", "04.MP4", "05.MP4", "06.MP4", "07.MP4", "08.MP4", "09.MP4", "10.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldPadToOneDigitWhenIndexesStartAtZero()
        {
            var input = GetMockedInput(3, firstIndex: 0);
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, null, null, null);

            result.Select(r => r.NewName).Should().Equal("0.MP4", "1.MP4", "2.MP4");
        }

        [TestMethod]
        public void FileRename_ShouldKeepOriginalExtensionCase()
        {
            var input = GetMockedInput(2, extension: ".mp4");
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, null, null, null);

            result.Select(r => r.NewName).Should().Equal("1.mp4", "2.mp4");
        }

        [TestMethod]
        public void FileRename_ShouldThrowIfSpecifiedDigitsTooLow()
        {
            var input = GetMockedInput(10);
            var rename = CreateFileRename();

            Action act = () => rename.GetRenamedFiles(input, _allFiles, null, null, 1);

            act.Should().ThrowExactly<ArgumentOutOfRangeException>();
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
            _renamedVideoFactoryMock.VerifyNoOtherCalls();
            _renamedCompanionFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow("../", null)]
        [DataRow(null, "/../out")]
        [DataRow("bad\0name", null)]
        public void FileRename_ShouldThrowIfPrefixOrSuffixDoesNotProduceAPlainFileName(string? prefix, string? suffix)
        {
            var input = GetMockedInput(3);
            var rename = CreateFileRename();

            Action act = () => rename.GetRenamedFiles(input, _allFiles, prefix, suffix, null);

            act.Should().ThrowExactly<ArgumentException>().WithMessage("*plain file name*");
            _loggerMock.VerifyLogged(LogLevel.Critical, Times.Once());
            _renamedVideoFactoryMock.VerifyNoOtherCalls();
            _renamedCompanionFactoryMock.VerifyNoOtherCalls();
            _companionFinderMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileRename_ShouldRenameCompanionsToMatchTheirVideo()
        {
            var input = GetMockedInput(2);
            var lrf1 = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315120000_0001_D.LRF");
            var wav1 = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315120000_0001_D.WAV");
            var lrf2 = DirectoryFileMocking.GetMockedIDirectoryFile("DJI_20240315120000_0002_D.LRF");
            _companionFinderMock.Setup(m => m.GetCompanions(input[0], _allFiles)).Returns(new[] { lrf1, wav1 });
            _companionFinderMock.Setup(m => m.GetCompanions(input[1], _allFiles)).Returns(new[] { lrf2 });
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, "Trip - ", null, 3);

            result.Select(r => r.NewName).Should().Equal("Trip - 001.MP4", "Trip - 002.MP4");
            result[0].Companions.Select(c => c.NewName).Should().Equal("Trip - 001.LRF", "Trip - 001.WAV");
            result[1].Companions.Select(c => c.NewName).Should().Equal("Trip - 002.LRF");
            _renamedCompanionFactoryMock.Verify(m => m.Create("Trip - 001.LRF", lrf1), Times.Once());
            _renamedCompanionFactoryMock.Verify(m => m.Create("Trip - 001.WAV", wav1), Times.Once());
            _renamedCompanionFactoryMock.Verify(m => m.Create("Trip - 002.LRF", lrf2), Times.Once());
            _renamedCompanionFactoryMock.VerifyNoOtherCalls();
            _renamedVideoFactoryMock.Verify(m => m.Create("Trip - 001.MP4", It.Is<IReadOnlyList<IRenamedCompanionFile>>(c => c.Count == 2), input[0]), Times.Once());
            _renamedVideoFactoryMock.Verify(m => m.Create("Trip - 002.MP4", It.Is<IReadOnlyList<IRenamedCompanionFile>>(c => c.Count == 1), input[1]), Times.Once());
            _renamedVideoFactoryMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void FileRename_ShouldReturnEmptyForEmptyInput()
        {
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(new List<INumberedVideoFile>(), _allFiles, "prefix", "suffix", 3);

            result.Should().BeEmpty();
            _renamedVideoFactoryMock.VerifyNoOtherCalls();
            _renamedCompanionFactoryMock.VerifyNoOtherCalls();
            _companionFinderMock.VerifyNoOtherCalls();
        }

        private void CheckPrefixSuffixNaming(string? prefix, string? suffix, params string[] expectedFilenames)
        {
            var input = GetMockedInput(5);
            var rename = CreateFileRename();

            var result = rename.GetRenamedFiles(input, _allFiles, prefix, suffix, null);

            result.Select(r => r.NewName).Should().Equal(expectedFilenames);
            foreach (INumberedVideoFile file in input)
            {
                _renamedVideoFactoryMock.Verify(m => m.Create(It.IsAny<string>(), It.IsAny<IReadOnlyList<IRenamedCompanionFile>>(), file), Times.Once());
            }
        }

        private FileRename CreateFileRename() =>
            new FileRename(_loggerMock.Object, _renamedVideoFactoryMock.Object, _renamedCompanionFactoryMock.Object, _companionFinderMock.Object);

        private static IRenamedVideoFile GetRenamedVideo(string newName, IReadOnlyList<IRenamedCompanionFile> companions)
        {
            var result = new Mock<IRenamedVideoFile>();
            result.Setup(m => m.NewName).Returns(newName);
            result.Setup(m => m.Companions).Returns(companions);
            return result.Object;
        }

        private static IRenamedCompanionFile GetRenamedCompanion(string newName)
        {
            var result = new Mock<IRenamedCompanionFile>();
            result.Setup(m => m.NewName).Returns(newName);
            return result.Object;
        }

        private static IList<INumberedVideoFile> GetMockedInput(int count, int firstIndex = 1, string extension = ".MP4")
        {
            var result = new List<INumberedVideoFile>();
            for (int i = 0; i < count; i++)
            {
                var file = new Mock<INumberedVideoFile>();
                file.Setup(m => m.FileExtension).Returns(extension);
                file.Setup(m => m.Name).Returns($"DJI_20240315120000_{i + 1:D4}_D{extension}");
                file.Setup(m => m.NewIndex).Returns(i + firstIndex);
                result.Add(file.Object);
            }

            return result;
        }
    }
}
