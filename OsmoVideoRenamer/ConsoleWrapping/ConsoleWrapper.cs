namespace OsmoVideoRenamer.ConsoleWrapping
{
    public class ConsoleWrapper : IConsoleWrapper
    {
        public void WriteLine(string line)
        {
            Console.Out.WriteLine(line);
        }

        public void WriteErrorLine(string line)
        {
            Console.Error.WriteLine(line);
        }
    }
}
