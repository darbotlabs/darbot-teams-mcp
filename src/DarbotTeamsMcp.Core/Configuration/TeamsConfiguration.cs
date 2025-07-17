using System.ComponentModel.DataAnnotations;

namespace DarbotTeamsMcp.Core.Configuration;

/// <summary>
/// Configuration settings for the Teams MCP server.
/// Follows the darbot-mcp pattern of environment variables with sensible defaults.
/// </summary>
public class TeamsConfiguration
{
    /// <summary>
    /// Azure AD tenant ID. Use "common" for multi-tenant.
    /// </summary>
    [Required]
    public string TenantId { get; set; } = "common";

    /// <summary>
    /// Azure AD application client ID.
    /// </summary>
    [Required]
    public string ClientId { get; set; } = "04b07795-8ddb-461a-bbee-02f9e1bf7b46"; // Microsoft Graph Command Line Tools

    /// <summary>
    /// Redirect URI for authentication flow.
    /// </summary>
    public string RedirectUri { get; set; } = "http://localhost:3000";

    /// <summary>
    /// Microsoft Graph API scopes required.
    /// </summary>
    public List<string> Scopes { get; set; } = new()
    {
        "https://graph.microsoft.com/Team.ReadBasic.All",
        "https://graph.microsoft.com/TeamMember.ReadWrite.All",
        "https://graph.microsoft.com/Channel.ReadWrite.All",
        "https://graph.microsoft.com/ChannelMessage.Read.All",
        "https://graph.microsoft.com/Files.ReadWrite.All",
        "https://graph.microsoft.com/Calendars.ReadWrite",
        "https://graph.microsoft.com/Tasks.ReadWrite",
        "https://graph.microsoft.com/Presence.Read.All",
        "https://graph.microsoft.com/User.Read"
    };

    /// <summary>
    /// Currently selected team ID (can be set via environment).
    /// </summary>
    public string? CurrentTeamId { get; set; }

    /// <summary>
    /// Currently selected channel ID (can be set via environment).
    /// </summary>
    public string? CurrentChannelId { get; set; }

    /// <summary>
    /// MCP server listening port.
    /// </summary>
    public int ServerPort { get; set; } = 3001;

    /// <summary>
    /// MCP server listening host.
    /// </summary>
    public string ServerHost { get; set; } = "localhost";

    /// <summary>
    /// Whether to enable CORS for web clients.
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// Allowed CORS origins.
    /// </summary>
    public List<string> CorsOrigins { get; set; } = new()
    {
        "http://localhost:3000",
        "vscode-webview://*",
        "https://claude.ai"
    };

    /// <summary>
    /// Log level for application logging.
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Whether to log to file in addition to console.
    /// </summary>
    public bool LogToFile { get; set; } = true;

    /// <summary>
    /// Log file path template.
    /// </summary>
    public string LogFilePath { get; set; } = "logs/darbot-teams-mcp-.log";

    /// <summary>
    /// Maximum log file size in MB.
    /// </summary>
    public int MaxLogFileSizeMB { get; set; } = 10;

    /// <summary>
    /// Number of log files to retain.
    /// </summary>
    public int RetainedLogFileCount { get; set; } = 7;

    /// <summary>
    /// Whether to enable request/response logging.
    /// </summary>
    public bool EnableRequestLogging { get; set; } = false;

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Graph API retry count.
    /// </summary>
    public int GraphApiRetryCount { get; set; } = 3;

    /// <summary>
    /// Graph API retry delay in milliseconds.
    /// </summary>
    public int GraphApiRetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Whether to cache Graph API responses.
    /// </summary>
    public bool EnableGraphApiCaching { get; set; } = true;

    /// <summary>
    /// Graph API cache duration in minutes.
    /// </summary>
    public int GraphApiCacheDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Whether to run in simulation mode (for development/testing).
    /// </summary>
    public bool SimulationMode { get; set; } = false;

    /// <summary>
    /// Whether to require authentication for all operations.
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;

    /// <summary>
    /// Creates configuration from environment variables with defaults.
    /// Follows the darbot-mcp getConfig() pattern.
    /// </summary>
    public static TeamsConfiguration FromEnvironment()
    {
        return new TeamsConfiguration
        {
            TenantId = Environment.GetEnvironmentVariable("TEAMS_TENANT_ID") ?? "common",
            ClientId = Environment.GetEnvironmentVariable("TEAMS_CLIENT_ID") ?? "04b07795-8ddb-461a-bbee-02f9e1bf7b46",
            RedirectUri = Environment.GetEnvironmentVariable("TEAMS_REDIRECT_URI") ?? "http://localhost:3000",
            CurrentTeamId = Environment.GetEnvironmentVariable("TEAMS_CURRENT_TEAM_ID"),
            CurrentChannelId = Environment.GetEnvironmentVariable("TEAMS_CURRENT_CHANNEL_ID"),
            ServerPort = int.TryParse(Environment.GetEnvironmentVariable("TEAMS_SERVER_PORT"), out var port) ? port : 3001,
            ServerHost = Environment.GetEnvironmentVariable("TEAMS_SERVER_HOST") ?? "localhost",
            LogLevel = Environment.GetEnvironmentVariable("TEAMS_LOG_LEVEL") ?? "Information",
            LogToFile = bool.TryParse(Environment.GetEnvironmentVariable("TEAMS_LOG_TO_FILE"), out var logToFile) ? logToFile : true,
            EnableRequestLogging = bool.TryParse(Environment.GetEnvironmentVariable("TEAMS_ENABLE_REQUEST_LOGGING"), out var enableRequestLogging) ? enableRequestLogging : false,
            SimulationMode = bool.TryParse(Environment.GetEnvironmentVariable("TEAMS_SIMULATION_MODE"), out var simulationMode) ? simulationMode : false,
            RequireAuthentication = bool.TryParse(Environment.GetEnvironmentVariable("TEAMS_REQUIRE_AUTHENTICATION"), out var requireAuth) ? requireAuth : true
        };
    }

    /// <summary>
    /// Creates configuration with automatic credential detection and smart defaults.
    /// This is the enhanced version that provides simplified onboarding.
    /// </summary>
    public static async Task<TeamsConfiguration> FromEnvironmentWithAutoDetectionAsync()
    {
        var config = FromEnvironment();
        
        // Try to auto-detect credentials and enhance configuration
        try
        {
            var detectionService = new Services.CredentialDetectionService(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<Services.CredentialDetectionService>.Instance);
            
            var credentialSources = await detectionService.DetectCredentialSourcesAsync();
            var preferredSource = credentialSources.GetPreferredSource();
            
            if (preferredSource != null)
            {
                // Override tenant ID if we detected it from Azure CLI
                if (!string.IsNullOrEmpty(preferredSource.TenantId) && config.TenantId == "common")
                {
                    config.TenantId = preferredSource.TenantId;
                }
            }
        }
        catch
        {
            // Ignore detection errors and use defaults
        }
        
        return config;
    }
}
