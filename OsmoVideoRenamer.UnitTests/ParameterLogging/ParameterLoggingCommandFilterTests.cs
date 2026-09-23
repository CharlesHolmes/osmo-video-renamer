using Cocona.Command;
using Cocona.CommandLine;
using Cocona.Filters;
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.ParameterLogging;
using System.Collections.Immutable;
using System.Reflection;

namespace OsmoVideoRenamer.UnitTests.ParameterLogging
{
    [TestClass]
    public class ParameterLoggingCommandFilterTests
    {
        private readonly Mock<ILogger<ParameterLoggingCommandFilter>> _logger = new Mock<ILogger<ParameterLoggingCommandFilter>>();

        [TestInitialize]
        public void Setup()
        {
            _logger.Reset();
        }

        [TestMethod]
        public async Task Filter_ShouldLogOptionsAsInformational()
        {
            var filter = new ParameterLoggingCommandFilter(_logger.Object);
            CoconaCommandExecutingContext ctx = GetExecutingContext([
                new Tuple<string, string>("someopt", "25"),
                new Tuple<string, string>("food", "steak")]);
            var next = new Mock<CommandExecutionDelegate>();
            next.Setup(m => m.Invoke(ctx)).ReturnsAsync(1);

            await filter.OnCommandExecutionAsync(ctx, next.Object);

            next.Verify(m => m.Invoke(ctx));
            next.VerifyNoOtherCalls();
            VerifyMessageLoggedInformational("Received option someopt with value 25.");
            VerifyMessageLoggedInformational("Received option food with value steak.");
            _logger.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Filter_ShouldLogNothingIfNoOptionsProvided()
        {
            var filter = new ParameterLoggingCommandFilter(_logger.Object);
            CoconaCommandExecutingContext ctx = GetExecutingContext([]);
            var next = new Mock<CommandExecutionDelegate>();
            next.Setup(m => m.Invoke(ctx)).ReturnsAsync(1);

            await filter.OnCommandExecutionAsync(ctx, next.Object);

            next.Verify(m => m.Invoke(ctx));
            next.VerifyNoOtherCalls();
            _logger.VerifyNoOtherCalls();
        }

        private void VerifyMessageLoggedInformational(string message)
        {
            // https://stackoverflow.com/questions/66307477/how-to-verify-iloggert-log-extension-method-has-been-called-using-moq
            _logger.Verify(
                m => m.Log(
                    It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((@object, @type) => @object.ToString() == message && @type.Name == "FormattedLogValues"),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        private static CoconaCommandExecutingContext GetExecutingContext(Tuple<string, string>[] options) =>
            new CoconaCommandExecutingContext(
                new CommandDescriptor(
                    new Mock<MethodInfo>().Object,
                    null,
                    "descriptor",
                    ImmutableList.Create<string>(),
                    "descriptor_description",
                    ImmutableList.Create<object>(),
                    ImmutableList.Create<ICommandParameterDescriptor>(),
                    ImmutableList.Create<CommandOptionDescriptor>(),
                    ImmutableList.Create<CommandArgumentDescriptor>(),
                    ImmutableList.Create<CommandOverloadDescriptor>(),
                    ImmutableList.Create<CommandOptionLikeCommandDescriptor>(),
                    CommandFlags.None,
                    null),
                new ParsedCommandLine(
                    ImmutableList.CreateRange<CommandOption>(
                            options.Select((Tuple<string, string> o, int i) =>
                                new CommandOption(
                                new CommandOptionDescriptor(
                                    typeof(string),
                                    o.Item1,
                                    o.Item1.ToArray(),
                                    $"the description of the option {o.Item1}",
                                    CoconaDefaultValue.None,
                                    null,
                                    CommandOptionFlags.None,
                                    ImmutableList.Create<Attribute>()),
                                o.Item2,
                                i + 1))),
                    ImmutableList.Create<CommandArgument>(),
                    ImmutableList.Create<string>()),
                null);
    }
}
