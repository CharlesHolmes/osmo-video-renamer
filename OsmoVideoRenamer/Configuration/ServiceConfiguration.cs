using Microsoft.Extensions.DependencyInjection;
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

namespace OsmoVideoRenamer.Configuration
{
    public static class ServiceConfiguration
    {
        public static void Configure(IServiceCollection collection)
        {
            collection
                .AddTransient<IConsoleWrapper, ConsoleWrapper>()
                .AddTransient<IFileSystem, FileSystem>()
                .AddTransient<IVideoDirectoryFactory, VideoDirectoryFactory>()
                .AddTransient<IDirectoryFileFactory, DirectoryFileFactory>()
                .AddTransient<IVideoFileFactory, VideoFileFactory>()
                .AddTransient<INumberedVideoFileFactory, NumberedVideoFileFactory>()
                .AddTransient<IRenamedVideoFileFactory, RenamedVideoFileFactory>()
                .AddTransient<IRenamedCompanionFileFactory, RenamedCompanionFileFactory>()
                .AddTransient<ICompanionFileFinder, CompanionFileFinder>()
                .AddTransient<IFileFilter, FileFilter>()
                .AddTransient<IFileSort, FileSort>()
                .AddTransient<IFileRename, FileRename>()
                .AddTransient<IRenameCollisionChecker, RenameCollisionChecker>();
        }
    }
}
