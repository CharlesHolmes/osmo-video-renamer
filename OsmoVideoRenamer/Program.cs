using Cocona;
using OsmoVideoRenamer.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace OsmoVideoRenamer
{
    [ExcludeFromCodeCoverage]
    internal static class Program
    {
        static void Main(string[] args)
        {
            var builder = CoconaApp.CreateBuilder(args);
            ServiceConfiguration.Configure(builder.Services);
            var app = builder.Build();
            FilterConfiguration.RegisterAllFilters(app);
            CommandConfiguration.RegisterAllCommands(app);
            app.Run();
        }
    }
}
