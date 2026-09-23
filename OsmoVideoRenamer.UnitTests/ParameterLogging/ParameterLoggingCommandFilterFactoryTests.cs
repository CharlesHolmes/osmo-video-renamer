using Cocona.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OsmoVideoRenamer.ParameterLogging;

namespace OsmoVideoRenamer.UnitTests.ParameterLogging
{
    [TestClass]
    public class ParameterLoggingCommandFilterFactoryTests
    {
        [TestMethod]
        public void Factory_ShouldCreateFilterOfCorrectType()
        {
            var factory = new ParameterLoggingCommandFilterFactory();
            var logger = new Mock<ILogger<ParameterLoggingCommandFilter>>();
            var services = new ServiceCollection()
                .AddSingleton<ILogger<ParameterLoggingCommandFilter>>(logger.Object)
                .BuildServiceProvider();

            var instance = factory.CreateInstance(services);

            instance.Should().BeAssignableTo<IFilterMetadata>();
            instance.Should().BeAssignableTo<ICommandFilter>();
            instance.Should().BeAssignableTo<ParameterLoggingCommandFilter>();
        }
    }
}
