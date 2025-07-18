using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Configuration;
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
/// MCP Server implementation for stdio communication (used by VS Code and other MCP clients).
/// This runs as a standalone console application that communicates via standard input/output.
/// </summary>
public class StdioMcpServer
{
    private readonly ILogger<StdioMcpServer> _logger;
    private readonly IMcpServer _mcpServer;
    private readonly IServiceProvider _serviceProvider;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public StdioMcpServer(ILogger<StdioMcpServer> logger, IMcpServer mcpServer, IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public static async Task<int> RunStdioServerAsync(string[] args)
    {
        // Configure logging for stdio mode
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise in stdio mode
        });

        var logger = loggerFactory.CreateLogger<StdioMcpServer>();

        try
        {
            // Configure services
            var services = new ServiceCollection();
            ConfigureServices(services);
            
            var serviceProvider = services.BuildServiceProvider();
            var mcpServer = serviceProvider.GetRequiredService<IMcpServer>();
            
            // Register all tools
            await RegisterAllToolsAsync(mcpServer, serviceProvider, logger);
            
            // Start the MCP server
            await mcpServer.StartAsync();
            
            // Create and run stdio server
            var stdioServer = new StdioMcpServer(logger, mcpServer, serviceProvider);
            await stdioServer.RunAsync();
            
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in stdio MCP server");
            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Load configuration
        var teamsConfig = TeamsConfiguration.FromEnvironment();
        services.AddSingleton(teamsConfig);

        // Add basic services
        services.AddMemoryCache();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise for stdio
        });

        // Register MCP services
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
    }

    private static async Task RegisterAllToolsAsync(IMcpServer mcpServer, IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var graphClient = scope.ServiceProvider.GetService<ITeamsGraphClient>();
            var toolLogger = scope.ServiceProvider.GetRequiredService<ILogger<TeamsToolBase>>();

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
                mcpServer.RegisterTool(new SetNotificationCommand(toolLogger, graphClient));

                // Basic/Support Commands (3 tools)
                mcpServer.RegisterTool(new GetTeamInfoCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new HelpCommand(toolLogger, graphClient));
                mcpServer.RegisterTool(new ReportIssueCommand(toolLogger, graphClient));

                var toolCount = mcpServer.GetRegisteredTools().Count;
                logger.LogInformation("Successfully registered {ToolCount} Teams tools for stdio mode", toolCount);
            }
            else
            {
                logger.LogWarning("GraphClient not available, running in simulation mode");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register Teams tools for stdio mode");
            throw;
        }
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting MCP stdio server for VS Code integration");

        try
        {
            // Process stdin line by line
            string? line;
            while ((line = await Console.In.ReadLineAsync()) != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var request = JsonSerializer.Deserialize<JsonElement>(line);
                    var response = await _mcpServer.HandleRequestAsync(request, _cancellationTokenSource.Token);
                    
                    var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        WriteIndented = false
                    });
                    
                    await Console.Out.WriteLineAsync(responseJson);
                    await Console.Out.FlushAsync();
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Invalid JSON received: {Line}", line);
                    // Send JSON-RPC error response
                    var errorResponse = new
                    {
                        jsonrpc = "2.0",
                        error = new
                        {
                            code = -32700,
                            message = "Parse error: Invalid JSON"
                        },
                        id = (object?)null
                    };
                    
                    var errorJson = JsonSerializer.Serialize(errorResponse);
                    await Console.Out.WriteLineAsync(errorJson);
                    await Console.Out.FlushAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing request: {Line}", line);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in stdio loop");
            throw;
        }
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }
}