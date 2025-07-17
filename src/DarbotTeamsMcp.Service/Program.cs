using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using DarbotTeamsMcp.Core.Configuration;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Services;

namespace DarbotTeamsMcp.Service;

/// <summary>
/// Main program entry point for the Darbot Teams MCP Windows Service.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the Windows Service.
    /// </summary>
    public static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/darbot-teams-mcp-service-.log", 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .WriteTo.EventLog("Darbot Teams MCP Service", manageEventSource: true)
            .CreateLogger();

        try
        {
            Log.Information("Starting Darbot Teams MCP Service");

            var host = CreateHostBuilder(args).Build();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Service startup failed");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Creates the host builder for the service.
    /// </summary>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseWindowsService(options =>
            {
                options.ServiceName = "DarbotTeamsMcpService";
            })
            .UseSerilog()
            .ConfigureServices((hostContext, services) =>
            {
                // Add configuration
                var configuration = hostContext.Configuration;
                var teamsConfig = new TeamsConfiguration();
                configuration.Bind("Teams", teamsConfig);
                services.AddSingleton(teamsConfig);

                // Add core services
                services.AddMemoryCache();
                services.AddSingleton<ITeamsAuthProvider, SimpleAuthService>();
                services.AddSingleton<IMcpServer, McpServer>();

                // Add the hosted service
                services.AddHostedService<McpServiceWorker>();
            });
}
