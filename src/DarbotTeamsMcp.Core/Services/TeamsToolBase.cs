using System.Text.Json;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;

namespace DarbotTeamsMcp.Core.Services;

/// <summary>
/// Abstract base class for all Teams tools following darbot-mcp patterns.
/// Provides common functionality like validation, error handling, and response formatting.
/// </summary>
public abstract class TeamsToolBase : ITeamsToolBase
{
    protected readonly ILogger<TeamsToolBase> _logger;
    protected readonly ITeamsGraphClient _graphClient;

    protected TeamsToolBase(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
    }

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract string Description { get; }

    /// <inheritdoc/>
    public abstract JsonElement InputSchema { get; }

    /// <inheritdoc/>
    public abstract TeamsPermissionLevel RequiredPermission { get; }

    /// <inheritdoc/>
    public abstract TeamsToolCategory Category { get; }

    /// <inheritdoc/>
    public async Task<McpResponse> ExecuteAsync(JsonElement parameters, TeamsContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing tool {ToolName} for user {UserName} in team {TeamId}", 
                Name, context.CurrentUser?.DisplayName, context.CurrentTeamId);

            // Validate parameters
            var validationResult = await ValidateParametersAsync(parameters);
            if (!validationResult.IsValid)
            {
                return CreateValidationErrorResponse(context.CorrelationId, validationResult.Errors);
            }

            // Check permissions
            if (!await CheckPermissionsAsync(context))
            {
                return CreatePermissionErrorResponse(context);
            }

            // Execute the specific tool logic
            var result = await ExecuteToolAsync(parameters, context, cancellationToken);
            
            _logger.LogInformation("Successfully executed tool {ToolName}", Name);
            return result;
        }        catch (Microsoft.Graph.ServiceException ex) when (ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("Permission denied for tool {ToolName}: {Message}", Name, ex.Message);
            return CreateErrorResponse(context.CorrelationId, 403, 
                $"❌ Permission denied: {ex.Message}\n\nTeam: {context.CurrentTeam?.DisplayName ?? "Unknown"}");
        }
        catch (Microsoft.Graph.ServiceException ex) when (ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Resource not found for tool {ToolName}: {Message}", Name, ex.Message);
            return CreateErrorResponse(context.CorrelationId, 404, 
                $"❌ Resource not found: {ex.Message}");
        }
        catch (Microsoft.Graph.ServiceException ex)
        {
            _logger.LogError(ex, "Graph API error in tool {ToolName}", Name);
            return CreateErrorResponse(context.CorrelationId, 500, 
                $"❌ Microsoft Graph error: {ex.Message}\n\nCorrelation ID: {context.CorrelationId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in tool {ToolName}", Name);
            return CreateErrorResponse(context.CorrelationId, 500, 
                $"❌ Unexpected error: {ex.Message}\n\nCorrelation ID: {context.CorrelationId}");
        }
    }

    /// <inheritdoc/>
    public virtual Task<ValidationResult> ValidateParametersAsync(JsonElement parameters)
    {
        // Default implementation - derived classes can override for specific validation
        return Task.FromResult(ValidationResult.Success());
    }

    /// <summary>
    /// Executes the specific tool logic. Must be implemented by derived classes.
    /// </summary>
    protected abstract Task<McpResponse> ExecuteToolAsync(JsonElement parameters, TeamsContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the current user has the required permissions.
    /// </summary>
    protected virtual async Task<bool> CheckPermissionsAsync(TeamsContext context)
    {
        if (RequiredPermission == TeamsPermissionLevel.Guest)
            return true;

        if (string.IsNullOrEmpty(context.CurrentTeamId))
        {
            _logger.LogWarning("No team context available for permission check");
            return false;
        }

        try
        {
            return await _graphClient.CheckUserPermissionAsync(context.CurrentTeamId, RequiredPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permissions for tool {ToolName}", Name);
            return false;
        }
    }

    /// <summary>
    /// Creates a successful response with content.
    /// </summary>
    protected static McpResponse CreateSuccessResponse(string message, TeamsContext context, object? data = null)    {
        var content = new List<McpContent> { McpContent.CreateText(message) };
        
        if (data != null)
        {
            content.Add(McpContent.CreateJson(data));
        }

        return McpResponse.Success(context.CorrelationId, content);
    }

    /// <summary>
    /// Creates an error response following darbot-mcp patterns.
    /// </summary>
    protected static McpResponse CreateErrorResponse(string correlationId, int code, string message)
    {
        return McpResponse.CreateError(correlationId, code, message);
    }

    /// <summary>
    /// Creates a validation error response.
    /// </summary>
    protected static McpResponse CreateValidationErrorResponse(string correlationId, IEnumerable<string> errors)
    {
        var errorMessage = "❌ Validation failed:\n" + string.Join("\n", errors.Select(e => $"  • {e}"));
        return CreateErrorResponse(correlationId, 400, errorMessage);
    }

    /// <summary>
    /// Creates a permission error response.
    /// </summary>
    protected static McpResponse CreatePermissionErrorResponse(TeamsContext context)
    {
        var message = $"❌ Insufficient permissions: This command requires {context.UserPermissionLevel} level access.\n\n" +
                     $"Team: {context.CurrentTeam?.DisplayName ?? "Unknown"}\n" +
                     $"Your Role: {context.UserPermissionLevel}\n" +
                     $"Required Role: {context.UserPermissionLevel}";
        
        return CreateErrorResponse(context.CorrelationId, 403, message);
    }

    /// <summary>
    /// Formats a list of items for display.
    /// </summary>
    protected static string FormatItemList<T>(IEnumerable<T> items, Func<T, string> formatter, string title)
    {
        var itemList = items.ToList();
        if (!itemList.Any())
        {
            return $"📝 {title}: No items found.";
        }

        var formattedItems = itemList.Select((item, index) => $"{index + 1}. {formatter(item)}");
        return $"📝 {title} ({itemList.Count} items):\n\n" + string.Join("\n", formattedItems);
    }

    /// <summary>
    /// Gets a required string parameter from the input.
    /// </summary>
    protected static string GetRequiredStringParameter(JsonElement parameters, string parameterName)
    {
        if (!parameters.TryGetProperty(parameterName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            throw new ArgumentException($"Required parameter '{parameterName}' is missing");
        }

        var value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Required parameter '{parameterName}' cannot be empty");
        }

        return value;
    }

    /// <summary>
    /// Gets an optional string parameter from the input.
    /// </summary>
    protected static string? GetOptionalStringParameter(JsonElement parameters, string parameterName, string? defaultValue = null)
    {
        if (!parameters.TryGetProperty(parameterName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return defaultValue;
        }

        return property.GetString() ?? defaultValue;
    }

    /// <summary>
    /// Gets an optional boolean parameter from the input.
    /// </summary>
    protected static bool GetOptionalBoolParameter(JsonElement parameters, string parameterName, bool defaultValue = false)
    {
        if (!parameters.TryGetProperty(parameterName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return defaultValue;
        }

        return property.ValueKind == JsonValueKind.True || 
               (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out var boolValue) && boolValue);
    }

    /// <summary>
    /// Gets an optional integer parameter from the input.
    /// </summary>
    protected static int GetOptionalIntParameter(JsonElement parameters, string parameterName, int defaultValue = 0)
    {
        if (!parameters.TryGetProperty(parameterName, out var property) || property.ValueKind == JsonValueKind.Null)
        {
            return defaultValue;
        }

        return property.ValueKind == JsonValueKind.Number ? property.GetInt32() : defaultValue;
    }
}
