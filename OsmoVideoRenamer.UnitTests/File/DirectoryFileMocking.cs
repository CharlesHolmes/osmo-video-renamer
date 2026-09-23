using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.UnitTests.File
{
    internal static class DirectoryFileMocking
    {
        public static Mock<IFileInfo> GetMockedIFileInfo(string fileName)
        {
            var fileInfo = new Mock<IFileInfo>();
            fileInfo.Setup(m => m.Name).Returns(fileName);
            fileInfo.Setup(m => m.Extension).Returns(Path.GetExtension(fileName));
            return fileInfo;
        }

        public static IDirectoryFile GetMockedIDirectoryFile(string fileName)
        {
            var result = new Mock<IDirectoryFile>();
            result.Setup(m => m.Name).Returns(fileName);
            result.Setup(m => m.BaseName).Returns(Path.GetFileNameWithoutExtension(fileName));
            result.Setup(m => m.FileExtension).Returns(Path.GetExtension(fileName));
            result.Setup(m => m.FileInfo).Returns(GetMockedIFileInfo(fileName).Object);
            return result.Object;
        }

        public static IVideoFile GetMockedIVideoFile(string fileName, int sequenceNumber, DateTime captureTimestamp)
        {
            var result = new Mock<IVideoFile>();
            result.Setup(m => m.Name).Returns(fileName);
            result.Setup(m => m.BaseName).Returns(Path.GetFileNameWithoutExtension(fileName));
            result.Setup(m => m.FileExtension).Returns(Path.GetExtension(fileName));
            result.Setup(m => m.SequenceNumber).Returns(sequenceNumber);
            result.Setup(m => m.CaptureTimestamp).Returns(captureTimestamp);
            result.Setup(m => m.FileInfo).Returns(GetMockedIFileInfo(fileName).Object);
            return result.Object;
        }
    }
}
