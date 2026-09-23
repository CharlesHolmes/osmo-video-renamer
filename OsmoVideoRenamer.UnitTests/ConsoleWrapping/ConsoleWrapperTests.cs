using OsmoVideoRenamer.ConsoleWrapping;

namespace OsmoVideoRenamer.UnitTests.ConsoleWrapping
{
    [TestClass]
    public class ConsoleWrapperTests
    {
        [TestMethod]
        [DoNotParallelize]
        public void ConsoleWrapper_ShouldWriteToConsoleOut()
        {
            string expected = "hello";
            TextWriter original = Console.Out;
            var writer = new StringWriter();
            Console.SetOut(writer);
            try
            {
                var consoleWrapper = new ConsoleWrapper();

                consoleWrapper.WriteLine(expected);

                Assert.AreEqual(expected + Environment.NewLine, writer.ToString());
            }
            finally
            {
                Console.SetOut(original);
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public void ConsoleWrapper_ShouldWriteToConsoleError()
        {
            string expected = "hello";
            TextWriter original = Console.Error;
            var writer = new StringWriter();
            Console.SetError(writer);
            try
            {
                var consoleWrapper = new ConsoleWrapper();

                consoleWrapper.WriteErrorLine(expected);

                Assert.AreEqual(expected + Environment.NewLine, writer.ToString());
            }
            finally
            {
                Console.SetError(original);
            }
        }
    }
}
