using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.Directory.Interfaces;
using OsmoVideoRenamer.File.DirectoryFiles.Interfaces;
using System.IO.Abstractions;

namespace OsmoVideoRenamer.Directory
{
    public class VideoDirectoryFactory : IVideoDirectoryFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public VideoDirectoryFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IVideoDirectory Create(string directoryPath)
        {
            return new VideoDirectory(
                _serviceProvider.GetRequiredService<ILogger<VideoDirectory>>(),
                _serviceProvider.GetRequiredService<IFileSystem>(),
                _serviceProvider.GetRequiredService<IDirectoryFileFactory>(),
                directoryPath);
        }
    }
}
