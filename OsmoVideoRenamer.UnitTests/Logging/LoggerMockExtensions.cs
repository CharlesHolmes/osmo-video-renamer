using Microsoft.Extensions.Logging;

namespace OsmoVideoRenamer.UnitTests.Logging
{
    /// <summary>
    /// ILogger extension methods (LogInformation etc.) all funnel into ILogger.Log with a
    /// FormattedLogValues state, so that is what gets verified.
    /// https://stackoverflow.com/questions/66307477/how-to-verify-iloggert-log-extension-method-has-been-called-using-moq
    /// </summary>
    internal static class LoggerMockExtensions
    {
        public static void VerifyLogged<T>(this Mock<ILogger<T>> logger, LogLevel level, Times times)
        {
            logger.Verify(
                m => m.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((@object, @type) => @type.Name == "FormattedLogValues"),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }
    }
}
