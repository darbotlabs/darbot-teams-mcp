using System.Text.Json;
using DarbotTeamsMcp.Core.Models;

namespace DarbotTeamsMcp.Core.Interfaces;

/// <summary>
/// Base interface for all Teams tools/commands following the darbot-mcp server.tool pattern.
/// Each tool represents a specific Teams operation that can be invoked via MCP.
/// </summary>
public interface ITeamsToolBase
{
    /// <summary>
    /// The unique name identifier for this tool (e.g., "teams-list-members").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable description of what this tool does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// JSON schema defining the input parameters for this tool.
    /// </summary>
    JsonElement InputSchema { get; }

    /// <summary>
    /// Required permission level to execute this tool.
    /// </summary>
    TeamsPermissionLevel RequiredPermission { get; }

    /// <summary>
    /// Category this tool belongs to for organization.
    /// </summary>
    TeamsToolCategory Category { get; }

    /// <summary>
    /// Executes the tool with the provided parameters.
    /// </summary>
    /// <param name="parameters">Tool input parameters</param>
    /// <param name="context">Current Teams context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool execution result</returns>
    Task<McpResponse> ExecuteAsync(JsonElement parameters, TeamsContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the input parameters against the tool's schema.
    /// </summary>
    /// <param name="parameters">Parameters to validate</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateParametersAsync(JsonElement parameters);
}
