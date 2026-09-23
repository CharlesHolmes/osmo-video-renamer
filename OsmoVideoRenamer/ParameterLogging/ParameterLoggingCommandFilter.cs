using Cocona.Command;
using Cocona.CommandLine;
using Cocona.Filters;
using Microsoft.Extensions.Logging;

namespace OsmoVideoRenamer.ParameterLogging
{
    public class ParameterLoggingCommandFilter : ICommandFilter
    {
        private readonly ILogger<ParameterLoggingCommandFilter> _logger;

        public ParameterLoggingCommandFilter(ILogger<ParameterLoggingCommandFilter> logger)
        {
            _logger = logger;
        }

        public async ValueTask<int> OnCommandExecutionAsync(CoconaCommandExecutingContext ctx, CommandExecutionDelegate next)
        {
            LogCommandArguments(ctx);
            return await next(ctx);
        }

        private void LogCommandArguments(CoconaCommandExecutingContext ctx)
        {
            foreach (CommandOption option in ctx.ParsedCommandLine.Options)
            {
                _logger.LogInformation("Received option {name} with value {value}.",
                    option.Option.Name,
                    option.Value);
            }
        }
    }
}
