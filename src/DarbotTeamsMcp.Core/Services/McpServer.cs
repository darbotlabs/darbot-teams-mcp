using System.Text.Json;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;

namespace DarbotTeamsMcp.Core.Services;

/// <summary>
/// Main MCP server implementation following JSON-RPC 2.0 protocol.
/// Inspired by the darbot-mcp TypeScript server architecture.
/// </summary>
public class McpServer : IMcpServer
{
    private readonly ILogger<McpServer> _logger;
    private readonly ITeamsAuthProvider _authProvider;
    private readonly List<ITeamsToolBase> _tools = new();
    private readonly Dictionary<string, ITeamsToolBase> _toolsByName = new();
    private bool _isStarted = false;

    public McpServer(ILogger<McpServer> logger, ITeamsAuthProvider authProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
    }

    /// <inheritdoc/>
    public async Task<JsonElement> HandleRequestAsync(JsonElement request, CancellationToken cancellationToken = default)
    {
        try
        {            // Parse the JSON-RPC request
            var mcpRequest = JsonSerializer.Deserialize<McpRequest>(request.GetRawText());
            if (mcpRequest == null)
            {
                return JsonSerializer.SerializeToElement(CreateErrorResponse(null, -32700, "Parse error: Invalid JSON"));
            }

            _logger.LogDebug("Handling MCP request: {Method}", mcpRequest.Method);

            // Handle different MCP methods
            var response = mcpRequest.Method switch
            {
                "initialize" => await HandleInitializeAsync(mcpRequest, cancellationToken),
                "tools/list" => await HandleToolsListAsync(mcpRequest, cancellationToken),
                "tools/call" => await HandleToolCallAsync(mcpRequest, cancellationToken),
                "ping" => HandlePing(mcpRequest),
                _ => CreateErrorResponse(mcpRequest.Id, -32601, $"Method not found: {mcpRequest.Method}")
            };

            return JsonSerializer.SerializeToElement(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error in MCP request");
            return JsonSerializer.SerializeToElement(CreateErrorResponse(null, -32700, "Parse error: Invalid JSON"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error handling MCP request");
            return JsonSerializer.SerializeToElement(CreateErrorResponse(null, -32603, "Internal error"));
        }
    }

    /// <inheritdoc/>
    public void RegisterTool(ITeamsToolBase tool)
    {
        if (tool == null) throw new ArgumentNullException(nameof(tool));

        if (_toolsByName.ContainsKey(tool.Name))
        {
            throw new InvalidOperationException($"Tool with name '{tool.Name}' is already registered");
        }

        _tools.Add(tool);
        _toolsByName[tool.Name] = tool;

        _logger.LogInformation("Registered tool: {ToolName} ({Category})", tool.Name, tool.Category);
    }

    /// <inheritdoc/>
    public IReadOnlyList<ITeamsToolBase> GetRegisteredTools()
    {
        return _tools.AsReadOnly();
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted)
        {
            throw new InvalidOperationException("Server is already started");
        }

        _logger.LogInformation("Starting MCP Server with {ToolCount} registered tools", _tools.Count);
        _isStarted = true;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Stopping MCP Server");
        _isStarted = false;

        return Task.CompletedTask;
    }

    private async Task<McpResponse> HandleInitializeAsync(McpRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing MCP Server");

        var capabilities = new
        {
            tools = new
            {
                list = true,
                call = true
            },
            resources = new
            {
                list = false,
                read = false
            },
            prompts = new
            {
                list = false,
                get = false
            }
        };

        var serverInfo = new
        {
            name = "darbot-teams-mcp",
            version = "1.0.0",
            description = "Microsoft Teams MCP Server - 47+ Commands for Teams Management",
            author = "Darbot Team",
            capabilities
        };

        return McpResponse.Success(request.Id, serverInfo);
    }

    private async Task<McpResponse> HandleToolsListAsync(McpRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listing available tools");

        var tools = _tools.Select(tool => new
        {
            name = tool.Name,
            description = tool.Description,
            inputSchema = tool.InputSchema,
            category = tool.Category.ToString(),
            requiredPermission = tool.RequiredPermission.ToString()
        }).ToArray();

        return McpResponse.Success(request.Id, new { tools });
    }

    private async Task<McpResponse> HandleToolCallAsync(McpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.Params.HasValue)
            {
                return CreateErrorResponse(request.Id, -32602, "Invalid params: tool call requires parameters");
            }

            var parameters = request.Params.Value;
            if (!parameters.TryGetProperty("name", out var toolNameElement))
            {
                return CreateErrorResponse(request.Id, -32602, "Invalid params: missing tool name");
            }

            var toolName = toolNameElement.GetString();
            if (string.IsNullOrEmpty(toolName))
            {
                return CreateErrorResponse(request.Id, -32602, "Invalid params: tool name cannot be empty");
            }

            if (!_toolsByName.TryGetValue(toolName, out var tool))
            {
                return CreateErrorResponse(request.Id, -32601, $"Tool not found: {toolName}");
            }

            // Get tool arguments
            var toolArguments = parameters.TryGetProperty("arguments", out var argsElement) 
                ? argsElement 
                : new JsonElement();

            // Get current Teams context
            var context = await GetCurrentTeamsContextAsync(cancellationToken);

            // Execute the tool
            _logger.LogInformation("Executing tool: {ToolName}", toolName);
            var result = await tool.ExecuteAsync(toolArguments, context, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool call");
            return CreateErrorResponse(request.Id, -32603, $"Internal error: {ex.Message}");
        }
    }

    private McpResponse HandlePing(McpRequest request)
    {
        return McpResponse.Success(request.Id, new { status = "pong", timestamp = DateTimeOffset.UtcNow });
    }

    private async Task<TeamsContext> GetCurrentTeamsContextAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if user is authenticated
            if (!await _authProvider.IsAuthenticatedAsync())
            {
                // Return empty context for unauthenticated scenarios
                return new TeamsContext
                {
                    CorrelationId = Guid.NewGuid().ToString()
                };
            }

            // Get current user
            var currentUser = await _authProvider.GetCurrentUserAsync();
            var authContext = await _authProvider.GetAuthenticationContextAsync();

            return new TeamsContext
            {
                CurrentUser = currentUser,
                TenantId = authContext.TenantId,
                CorrelationId = Guid.NewGuid().ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Teams context, using empty context");
            return new TeamsContext
            {
                CorrelationId = Guid.NewGuid().ToString()
            };
        }
    }

    private static McpResponse CreateErrorResponse(object? id, int code, string message)
    {
        return McpResponse.CreateError(id, code, message);
    }
}
