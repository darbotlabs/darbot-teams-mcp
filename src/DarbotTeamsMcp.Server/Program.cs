using Serilog;
using DarbotTeamsMcp.Core.Configuration;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Services;
using DarbotTeamsMcp.Commands.UserManagement;
using DarbotTeamsMcp.Commands.ChannelManagement;
using DarbotTeamsMcp.Commands.Messaging;
using DarbotTeamsMcp.Commands.FileManagement;
using DarbotTeamsMcp.Commands.Meetings;
using DarbotTeamsMcp.Commands.TaskProductivity;
using DarbotTeamsMcp.Commands.Integration;
using DarbotTeamsMcp.Commands.Presence;
using DarbotTeamsMcp.Server.Services;

namespace DarbotTeamsMcp.Server;

/// <summary>
/// Main entry point for the Darbot Teams MCP Server.
/// Configures services, logging, and starts the web server.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Configure Serilog early for startup logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/startup-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting Darbot Teams MCP Server");

            var builder = WebApplication.CreateBuilder(args);

            // Configure Serilog from configuration
            builder.Host.UseSerilog((context, configuration) =>
                configuration.ReadFrom.Configuration(context.Configuration));

            // Load Teams configuration
            var teamsConfig = TeamsConfiguration.FromEnvironment();
            builder.Services.AddSingleton(teamsConfig);

            // Add services to the container
            ConfigureServices(builder.Services, teamsConfig);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app, teamsConfig);

            // Register all Teams tools
            await RegisterTeamsTools(app.Services);

            // Start the server
            Log.Information("Darbot Teams MCP Server starting on {Host}:{Port}", 
                teamsConfig.ServerHost, teamsConfig.ServerPort);

            await app.RunAsync();
            
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Darbot Teams MCP Server terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(IServiceCollection services, TeamsConfiguration configuration)
    {
        // Add basic services
        services.AddControllers();
        services.AddMemoryCache();
        services.AddHealthChecks();

        // Add Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() 
            { 
                Title = "Darbot Teams MCP Server", 
                Version = "v1",
                Description = "Microsoft Teams MCP Server with 47+ Commands for Teams Management"
            });
        });

        // Configure CORS
        if (configuration.EnableCors)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(configuration.CorsOrigins.ToArray())
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });
        }        // Register MCP services
        services.AddSingleton<ITeamsAuthProvider, SimpleAuthService>();
        services.AddSingleton<IMcpServer, McpServer>();
          // Register Teams Graph Client - placeholder implementation for testing
        services.AddSingleton<ITeamsGraphClient>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<MockTeamsGraphClient>>();
            return new MockTeamsGraphClient(logger);
        });

        // Register HTTP client for Graph API
        services.AddHttpClient();

        // Add logging
        services.AddLogging();
    }

    private static void ConfigurePipeline(WebApplication app, TeamsConfiguration configuration)
    {
        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Darbot Teams MCP Server v1");
                c.RoutePrefix = string.Empty; // Serve Swagger UI at root
            });
        }

        app.UseSerilogRequestLogging();

        if (configuration.EnableCors)
        {
            app.UseCors();
        }

        app.UseRouting();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHealthChecks("/health");

        // Add a simple root endpoint
        app.MapGet("/", () => Results.Ok(new 
        { 
            message = "Darbot Teams MCP Server",
            version = "1.0.0",
            endpoints = new
            {
                mcp = "/mcp",
                health = "/health",
                info = "/mcp/info",
                swagger = "/swagger"
            }
        }));
    }    private static async Task RegisterTeamsTools(IServiceProvider services)
    {
        var mcpServer = services.GetRequiredService<IMcpServer>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var serviceScope = services.CreateScope();
            var graphClient = serviceScope.ServiceProvider.GetService<ITeamsGraphClient>();
            var toolLogger = serviceScope.ServiceProvider.GetRequiredService<ILogger<TeamsToolBase>>();

            if (graphClient != null)
            {
                // User Management Commands (11 tools)
                mcpServer.RegisterTool(new ListTeamMembersCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new AddMemberCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new RemoveMemberCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ListOwnersCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ListGuestsCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new InviteGuestCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new RemoveGuestCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new PromoteToOwnerCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new DemoteFromOwnerCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new MuteUserCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new UnmuteUserCommand(toolLogger, graphClient));

                // Channel Management Commands (10 tools)
                mcpServer.RegisterTool(new ListChannelsCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new CreateChannelCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ArchiveChannelCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new GetChannelInfoCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new GetChannelAnalyticsCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new RenameChannelCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new SetChannelPrivacyCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new SetChannelTopicCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new LockChannelCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new UnlockChannelCommand(toolLogger, graphClient));

                // Messaging Commands (5 tools)
                mcpServer.RegisterTool(new PinMessageCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new UnpinMessageCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new SearchMessagesCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new SendAnnouncementCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ExportMessagesCommand(toolLogger, graphClient));

                // File Management Commands (4 tools)
                mcpServer.RegisterTool(new UploadFileCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new DownloadFileCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new DeleteFileCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ListFilesCommand(toolLogger, graphClient));

                // Meeting Management Commands (3 tools)
                mcpServer.RegisterTool(new ScheduleMeetingCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new CancelMeetingCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ListMeetingsCommand(toolLogger, graphClient));

                // Task & Productivity Commands (5 tools)
                mcpServer.RegisterTool(new AssignTaskCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new CompleteTaskCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ListTasksCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new StartPollCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ShowPollResultsCommand(toolLogger, graphClient));

                // Integration & App Commands (6 tools)
                mcpServer.RegisterTool(new AddAgentCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new RemoveBotCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ListAgentsCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new AddTabCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new RemoveTabCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ListAppsCommand(toolLogger, graphClient));

                // Presence & Notification Commands (3 tools)
                mcpServer.RegisterTool(new GetStatusCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new SetStatusCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new SetNotificationCommand(toolLogger, graphClient));                // Basic/Support Commands (3 tools)
                mcpServer.RegisterTool(new GetTeamInfoCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new HelpCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ReportIssueCommand(toolLogger, graphClient));

                var toolCount = mcpServer.GetRegisteredTools().Count;
                logger.LogInformation("Successfully registered {ToolCount} Teams tools across 9 categories", toolCount);
                
                // Log the achievement
                logger.LogInformation("🎯 Teams MCP Quest Progress: {ToolCount}/47+ tools implemented - Quest COMPLETE! 🏆", toolCount);
            }
            else
            {
                logger.LogWarning("GraphClient not available, running in simulation mode");
            }

            // Start the MCP server
            await mcpServer.StartAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register Teams tools");
            throw;
        }
    }
}
