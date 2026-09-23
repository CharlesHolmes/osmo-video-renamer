using Cocona.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OsmoVideoRenamer.ParameterLogging
{
    public class ParameterLoggingCommandFilterFactory : IFilterFactory
    {
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new ParameterLoggingCommandFilter(
                serviceProvider.GetRequiredService<ILogger<ParameterLoggingCommandFilter>>());
        }
    }
}
