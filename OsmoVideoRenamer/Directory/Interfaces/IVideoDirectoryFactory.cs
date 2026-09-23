namespace OsmoVideoRenamer.Directory.Interfaces
{
    public interface IVideoDirectoryFactory
    {
        IVideoDirectory Create(string directoryPath);
    }
}
