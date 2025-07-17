using System.Text.Json;

namespace DarbotTeamsMcp.Core.Interfaces;

/// <summary>
/// Represents an MCP (Model Context Protocol) server that can handle JSON-RPC 2.0 requests.
/// Inspired by the proven darbot-mcp TypeScript architecture patterns.
/// </summary>
public interface IMcpServer
{
    /// <summary>
    /// Handles an incoming MCP request following JSON-RPC 2.0 protocol.
    /// </summary>
    /// <param name="request">The JSON-RPC request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The JSON-RPC response</returns>
    Task<JsonElement> HandleRequestAsync(JsonElement request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new tool with the MCP server.
    /// </summary>
    /// <param name="tool">The tool to register</param>
    void RegisterTool(ITeamsToolBase tool);

    /// <summary>
    /// Gets all registered tools.
    /// </summary>
    IReadOnlyList<ITeamsToolBase> GetRegisteredTools();

    /// <summary>
    /// Starts the MCP server.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the MCP server.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
