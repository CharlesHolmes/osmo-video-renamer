using Cocona;
using Cocona.Builder;

namespace OsmoVideoRenamer.Configuration
{
    public static class CommandConfiguration
    {
        public static void RegisterAllCommands(ICoconaCommandsBuilder app)
        {
            app.AddCommands<RenameCommand>();
        }
    }
}
