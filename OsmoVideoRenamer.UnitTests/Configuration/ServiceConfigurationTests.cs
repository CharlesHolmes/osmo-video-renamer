using Microsoft.Extensions.DependencyInjection;
using OsmoVideoRenamer.Configuration;
using OsmoVideoRenamer.ConsoleWrapping;
using OsmoVideoRenamer.Directory;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File;
using OsmoVideoRenamer.File.CompanionFiles;
using OsmoVideoRenamer.File.CompanionFiles.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using OsmoVideoRenamer.File.Interfaces;
using OsmoVideoRenamer.File.VideoFiles;
using OsmoVideoRenamer.File.VideoFiles.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Numbered;
using OsmoVideoRenamer.File.VideoFiles.Numbered.Interfaces;
using OsmoVideoRenamer.File.VideoFiles.Renamed;
using OsmoVideoRenamer.File.VideoFiles.Renamed.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.UnitTests.Configuration
{
    [TestClass]
    public class ServiceConfigurationTests
    {
        [TestMethod]
        [DataRow(typeof(IConsoleWrapper), typeof(ConsoleWrapper))]
        [DataRow(typeof(IFileSystem), typeof(FileSystem))]
        [DataRow(typeof(IVideoDirectoryFactory), typeof(VideoDirectoryFactory))]
        [DataRow(typeof(IDirectoryFileFactory), typeof(DirectoryFileFactory))]
        [DataRow(typeof(IVideoFileFactory), typeof(VideoFileFactory))]
        [DataRow(typeof(INumberedVideoFileFactory), typeof(NumberedVideoFileFactory))]
        [DataRow(typeof(IRenamedVideoFileFactory), typeof(RenamedVideoFileFactory))]
        [DataRow(typeof(IRenamedCompanionFileFactory), typeof(RenamedCompanionFileFactory))]
        [DataRow(typeof(ICompanionFileFinder), typeof(CompanionFileFinder))]
        [DataRow(typeof(IFileFilter), typeof(FileFilter))]
        [DataRow(typeof(IFileSort), typeof(FileSort))]
        [DataRow(typeof(IFileRename), typeof(FileRename))]
        [DataRow(typeof(IRenameCollisionChecker), typeof(RenameCollisionChecker))]
        public void ServiceConfiguration_ShouldRegisterService(Type serviceType, Type implementationType)
        {
            var collection = new ServiceCollection();
            collection.AddLogging();

            ServiceConfiguration.Configure(collection);

            collection
                .BuildServiceProvider()
                .GetRequiredService(serviceType)
                .Should()
                .BeOfType(implementationType);
        }
    }
}
