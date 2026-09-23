using Cocona;
using Cocona.Builder;
using Cocona.Filters;
using OsmoVideoRenamer.ParameterLogging;

namespace OsmoVideoRenamer.Configuration
{
    public static class FilterConfiguration
    {
        public static void RegisterAllFilters(ICoconaCommandsBuilder app)
        {
            app.UseFilter(new ParameterLoggingCommandFilterFactory());
        }
    }
}
