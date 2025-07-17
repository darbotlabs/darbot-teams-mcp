using System.Text.Json;
using System.Text.Json.Serialization;

namespace DarbotTeamsMcp.Core.Models;

/// <summary>
/// Represents an MCP (Model Context Protocol) request following JSON-RPC 2.0 specification.
/// </summary>
public record McpRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; init; }

    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}

/// <summary>
/// Represents an MCP response following JSON-RPC 2.0 specification.
/// </summary>
public record McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; init; }

    [JsonPropertyName("result")]
    public object? Result { get; init; }

    [JsonPropertyName("error")]
    public McpError? Error { get; init; }

    [JsonPropertyName("content")]
    public List<McpContent>? Content { get; init; }

    /// <summary>
    /// Creates a successful response with content.
    /// </summary>
    public static McpResponse Success(object? id, List<McpContent> content)
    {
        return new McpResponse
        {
            Id = id,
            Content = content
        };
    }

    /// <summary>
    /// Creates a successful response with result.
    /// </summary>
    public static McpResponse Success(object? id, object result)
    {
        return new McpResponse
        {
            Id = id,
            Result = result
        };
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public static McpResponse CreateError(object? id, int code, string message, object? data = null)
    {
        return new McpResponse
        {
            Id = id,
            Error = new McpError
            {
                Code = code,
                Message = message,
                Data = data
            }
        };
    }
}

/// <summary>
/// Represents an MCP error following JSON-RPC 2.0 error specification.
/// </summary>
public record McpError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; init; }
}

/// <summary>
/// Represents content in an MCP response.
/// </summary>
public record McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("data")]
    public object? Data { get; init; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    /// <summary>
    /// Creates text content.
    /// </summary>
    public static McpContent CreateText(string text) => new() { Type = "text", Text = text };

    /// <summary>
    /// Creates JSON content.
    /// </summary>
    public static McpContent CreateJson(object data) => new() { Type = "application/json", Data = data, MimeType = "application/json" };
}
