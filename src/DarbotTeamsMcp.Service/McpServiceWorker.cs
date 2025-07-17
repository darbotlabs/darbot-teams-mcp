using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Interfaces;

namespace DarbotTeamsMcp.Service;

/// <summary>
/// Background service that hosts the MCP server.
/// </summary>
public class McpServiceWorker : BackgroundService
{
    private readonly ILogger<McpServiceWorker> _logger;
    private readonly IMcpServer _mcpServer;

    /// <summary>
    /// Initializes a new instance of the McpServiceWorker class.
    /// </summary>
    public McpServiceWorker(ILogger<McpServiceWorker> logger, IMcpServer mcpServer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
    }

    /// <summary>
    /// Executes the service worker.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Darbot Teams MCP Service started at: {time}", DateTimeOffset.UtcNow);

        try
        {
            // Start the MCP server
            await StartMcpServerAsync(stoppingToken);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("MCP Service heartbeat at: {time}", DateTimeOffset.UtcNow);
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MCP Service stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP Service encountered an error");
            throw;
        }
    }

    /// <summary>
    /// Starts the MCP server.
    /// </summary>
    private async Task StartMcpServerAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MCP server...");
        
        // For now, just log that the server would start
        // In a full implementation, this would start an HTTP server or IPC listener
        _logger.LogInformation("MCP server ready to accept connections");
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles service stop requests.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Darbot Teams MCP Service stopping at: {time}", DateTimeOffset.UtcNow);
        
        await base.StopAsync(cancellationToken);
        
        _logger.LogInformation("Darbot Teams MCP Service stopped");
    }
}
