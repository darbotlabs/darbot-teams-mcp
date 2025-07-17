using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using DarbotTeamsMcp.Core.Interfaces;

namespace DarbotTeamsMcp.Server.Controllers;

/// <summary>
/// MCP (Model Context Protocol) endpoint controller.
/// Handles JSON-RPC 2.0 requests following the MCP specification.
/// </summary>
[ApiController]
[Route("mcp")]
[Produces("application/json")]
public class McpController : ControllerBase
{
    private readonly IMcpServer _mcpServer;
    private readonly ILogger<McpController> _logger;

    public McpController(IMcpServer mcpServer, ILogger<McpController> logger)
    {
        _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles MCP JSON-RPC 2.0 requests.
    /// </summary>
    /// <param name="request">The JSON-RPC request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON-RPC response</returns>
    [HttpPost]
    public async Task<IActionResult> HandleMcpRequest([FromBody] JsonElement request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Received MCP request: {RequestSize} bytes", request.GetRawText().Length);

            var response = await _mcpServer.HandleRequestAsync(request, cancellationToken);
            
            return Ok(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in MCP request");
            return BadRequest(new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = -32700,
                    message = "Parse error: Invalid JSON"
                },
                id = (object?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error handling MCP request");
            return StatusCode(500, new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = -32603,
                    message = "Internal error"
                },
                id = (object?)null
            });
        }
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        var health = new
        {
            status = "healthy",
            timestamp = DateTimeOffset.UtcNow,
            version = "1.0.0",
            server = "darbot-teams-mcp",
            toolsCount = _mcpServer.GetRegisteredTools().Count
        };

        return Ok(health);
    }

    /// <summary>
    /// Gets server capabilities and information.
    /// </summary>
    [HttpGet("info")]
    public IActionResult GetServerInfo()
    {
        var tools = _mcpServer.GetRegisteredTools()
            .GroupBy(t => t.Category)
            .ToDictionary(
                g => g.Key.ToString(), 
                g => g.Select(t => new { 
                    name = t.Name, 
                    description = t.Description, 
                    requiredPermission = t.RequiredPermission.ToString() 
                }).ToArray()
            );

        var info = new
        {
            name = "darbot-teams-mcp",
            version = "1.0.0",
            description = "Microsoft Teams MCP Server - 47+ Commands for Teams Management",
            author = "Darbot Team",
            capabilities = new
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
            },
            toolCategories = tools,
            totalTools = _mcpServer.GetRegisteredTools().Count
        };

        return Ok(info);
    }
}
