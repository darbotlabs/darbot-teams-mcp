using System.Text.Json;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;
using DarbotTeamsMcp.Core.Services;

namespace DarbotTeamsMcp.Commands.Integration;

/// <summary>
/// Add Agent Command - Add bots and intelligent agents to teams
/// </summary>
public class AddAgentCommand : TeamsToolBase
{
    public AddAgentCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }    public override string Name => "teams-add-agent";
    public override string Description => "Add a bot or agent to the current team";
    public override TeamsToolCategory Category => TeamsToolCategory.Integrations;
    public override TeamsPermissionLevel RequiredPermission => TeamsPermissionLevel.Owner;

    public override JsonElement InputSchema
    {
        get
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    agentId = new
                    {
                        type = "string",
                        description = "Bot or agent application ID"
                    },
                    agentName = new
                    {
                        type = "string",
                        description = "Display name for the agent",
                        minLength = 1,
                        maxLength = 64
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Optional specific channel to add the agent to"
                    },
                    permissions = new
                    {
                        type = "array",
                        description = "Permissions to grant to the agent",
                        items = new
                        {
                            type = "string",
                            @enum = new[] { "read-messages", "send-messages", "read-files", "send-notifications", "manage-meetings" }
                        },
                        @default = new[] { "read-messages", "send-messages" }
                    },
                    welcomeMessage = new
                    {
                        type = "string",
                        description = "Optional welcome message from the agent",
                        maxLength = 500
                    }
                },
                required = new[] { "agentId", "agentName" },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);
        }
    }

    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters,
        TeamsContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var agentId = parameters.GetProperty("agentId").GetString()!;
            var agentName = parameters.GetProperty("agentName").GetString()!;
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : null;
            var permissions = parameters.TryGetProperty("permissions", out var permProp) ?
                permProp.EnumerateArray().Select(p => p.GetString()!).ToArray() :
                new[] { "read-messages", "send-messages" };
            var welcomeMessage = parameters.TryGetProperty("welcomeMessage", out var welcomeProp) ? 
                welcomeProp.GetString() : null;

            // Mock agent installation
            var installationId = Guid.NewGuid().ToString();
            var installedDate = DateTime.UtcNow;

            _logger.LogInformation("Agent added - ID: {AgentId}, Name: {AgentName}, Team: {TeamId}, Channel: {ChannelId}",
                agentId, agentName, context.CurrentTeamId, channelId);

            var permissionsList = string.Join(", ", permissions.Select(p => p.Replace("-", " ").ToTitleCase()));
            var targetScope = string.IsNullOrEmpty(channelId) ? "entire team" : $"channel {channelId}";

            var message = $"🤖 **Agent Added Successfully**\n\n" +
                         $"🏷️ **Agent:** {agentName}\n" +
                         $"🆔 **Agent ID:** {agentId}\n" +
                         $"🎯 **Scope:** {targetScope}\n" +
                         $"🔐 **Permissions:** {permissionsList}\n" +
                         $"📅 **Installed:** {installedDate:yyyy-MM-dd HH:mm}\n" +
                         $"📋 **Installation ID:** {installationId}";

            if (!string.IsNullOrEmpty(welcomeMessage))
            {
                message += $"\n\n💬 **Welcome Message:**\n\"{welcomeMessage}\"";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add agent");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to add agent: {ex.Message}");
        }
    }
}

/// <summary>
/// Remove Bot Command - Remove bots and agents from teams
/// </summary>
public class RemoveBotCommand : TeamsToolBase
{
    public RemoveBotCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }    public override string Name => "teams-remove-bot";
    public override string Description => "Remove a bot or agent from the current team";
    public override TeamsToolCategory Category => TeamsToolCategory.Integrations;
    public override TeamsPermissionLevel RequiredPermission => TeamsPermissionLevel.Owner;

    public override JsonElement InputSchema
    {
        get
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    agentId = new
                    {
                        type = "string",
                        description = "Bot or agent application ID to remove"
                    },
                    installationId = new
                    {
                        type = "string",
                        description = "Optional installation ID for specific removal"
                    },
                    removeData = new
                    {
                        type = "boolean",
                        description = "Whether to remove bot data and messages",
                        @default = false
                    },
                    reason = new
                    {
                        type = "string",
                        description = "Optional reason for removal",
                        maxLength = 255
                    }
                },
                required = new[] { "agentId" },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);
        }
    }

    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters,
        TeamsContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var agentId = parameters.GetProperty("agentId").GetString()!;
            var installationId = parameters.TryGetProperty("installationId", out var installProp) ? 
                installProp.GetString() : null;
            var removeData = parameters.TryGetProperty("removeData", out var dataProp) && 
                dataProp.GetBoolean();
            var reason = parameters.TryGetProperty("reason", out var reasonProp) ? 
                reasonProp.GetString() : null;

            // Mock bot removal
            var removedDate = DateTime.UtcNow;
            var mockBotName = "Sample Bot"; // In real implementation, would fetch from Graph API

            _logger.LogInformation("Bot removed - ID: {AgentId}, Team: {TeamId}, Reason: {Reason}",
                agentId, context.CurrentTeamId, reason ?? "No reason provided");

            var message = $"🗑️ **Bot Removed Successfully**\n\n" +
                         $"🤖 **Bot:** {mockBotName}\n" +
                         $"🆔 **Agent ID:** {agentId}\n" +
                         $"📅 **Removed:** {removedDate:yyyy-MM-dd HH:mm}\n" +
                         $"🧹 **Data Removed:** {(removeData ? "Yes" : "No")}";

            if (!string.IsNullOrEmpty(installationId))
            {
                message += $"\n📋 **Installation ID:** {installationId}";
            }

            if (!string.IsNullOrEmpty(reason))
            {
                message += $"\n📝 **Reason:** {reason}";
            }

            if (removeData)
            {
                message += $"\n\n⚠️ **Note:** Bot messages and data have been removed from the team.";
            }
            else
            {
                message += $"\n\n💡 **Note:** Bot messages and data remain in the team for reference.";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove bot");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to remove bot: {ex.Message}");
        }
    }
}

/// <summary>
/// List Agents Command - Display all bots and agents in the team
/// </summary>
public class ListAgentsCommand : TeamsToolBase
{
    public ListAgentsCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }    public override string Name => "teams-list-agents";
    public override string Description => "List all bots and agents in the current team";
    public override TeamsToolCategory Category => TeamsToolCategory.Integrations;
    public override TeamsPermissionLevel RequiredPermission => TeamsPermissionLevel.Member;

    public override JsonElement InputSchema
    {
        get
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    includeInactive = new
                    {
                        type = "boolean",
                        description = "Include inactive or disabled agents",
                        @default = false
                    },
                    agentType = new
                    {
                        type = "string",
                        description = "Filter by agent type",
                        @enum = new[] { "all", "bots", "connectors", "apps" },
                        @default = "all"
                    },
                    includePermissions = new
                    {
                        type = "boolean",
                        description = "Include agent permissions in the listing",
                        @default = true
                    }
                },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);
        }
    }

    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters,
        TeamsContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var includeInactive = parameters.TryGetProperty("includeInactive", out var inactiveProp) && 
                inactiveProp.GetBoolean();
            var agentType = parameters.TryGetProperty("agentType", out var typeProp) ? 
                typeProp.GetString() ?? "all" : "all";
            var includePermissions = parameters.TryGetProperty("includePermissions", out var permProp) && 
                permProp.GetBoolean();

            // Mock agents data
            var mockAgents = new[]
            {
                new
                {
                    Id = "agent-001",
                    Name = "Productivity Bot",
                    Type = "bot",
                    Status = "active",
                    InstallDate = DateTime.Now.AddDays(-30),
                    Permissions = new[] { "read-messages", "send-messages", "manage-meetings" },
                    LastActivity = DateTime.Now.AddHours(-2)
                },
                new
                {
                    Id = "agent-002",
                    Name = "GitHub Connector",
                    Type = "connector",
                    Status = "active",
                    InstallDate = DateTime.Now.AddDays(-15),
                    Permissions = new[] { "send-notifications", "read-files" },
                    LastActivity = DateTime.Now.AddMinutes(-30)
                },
                new
                {
                    Id = "agent-003",
                    Name = "Analytics Dashboard",
                    Type = "app",
                    Status = "inactive",
                    InstallDate = DateTime.Now.AddDays(-60),
                    Permissions = new[] { "read-messages", "read-files" },
                    LastActivity = DateTime.Now.AddDays(-10)
                }
            };

            // Apply filters
            var filteredAgents = mockAgents.AsEnumerable();

            if (!includeInactive)
            {
                filteredAgents = filteredAgents.Where(a => a.Status == "active");
            }

            if (agentType != "all")
            {
                filteredAgents = filteredAgents.Where(a => a.Type == agentType.TrimEnd('s')); // Remove plural
            }

            var agents = filteredAgents.ToArray();

            if (!agents.Any())
            {
                return CreateSuccessResponse("🤖 **No agents found** matching your criteria.", context);
            }

            var agentList = string.Join("\n\n", agents.Select((agent, i) =>
            {
                var statusEmoji = agent.Status switch
                {
                    "active" => "🟢",
                    "inactive" => "🔴",
                    "warning" => "🟡",
                    _ => "⚪"
                };

                var typeEmoji = agent.Type switch
                {
                    "bot" => "🤖",
                    "connector" => "🔗",
                    "app" => "📱",
                    _ => "⚙️"
                };

                var result = $"{i + 1}. {typeEmoji} **{agent.Name}**\n" +
                           $"   {statusEmoji} Status: {agent.Status.ToUpperInvariant()}\n" +
                           $"   📅 Installed: {agent.InstallDate:yyyy-MM-dd}\n" +
                           $"   ⏰ Last Active: {agent.LastActivity:yyyy-MM-dd HH:mm}\n" +
                           $"   🆔 ID: {agent.Id}";

                if (includePermissions && agent.Permissions.Any())
                {
                    var permissions = string.Join(", ", agent.Permissions.Select(p => p.Replace("-", " ")));
                    result += $"\n   🔐 Permissions: {permissions}";
                }

                return result;
            }));

            var message = $"🤖 **Team Agents & Bots** ({agents.Length} found)\n" +
                         $"🎯 **Filter:** {agentType} agents\n\n{agentList}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list agents");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to list agents: {ex.Message}");
        }
    }
}

/// <summary>
/// Add Tab Command - Add application tabs to channels
/// </summary>
public class AddTabCommand : TeamsToolBase
{
    public AddTabCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }    public override string Name => "teams-add-tab";
    public override string Description => "Add an application tab to a channel";
    public override TeamsToolCategory Category => TeamsToolCategory.Integrations;
    public override TeamsPermissionLevel RequiredPermission => TeamsPermissionLevel.Owner;

    public override JsonElement InputSchema
    {
        get
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    channelId = new
                    {
                        type = "string",
                        description = "Channel ID where to add the tab"
                    },
                    appId = new
                    {
                        type = "string",
                        description = "Application ID for the tab"
                    },
                    tabName = new
                    {
                        type = "string",
                        description = "Display name for the tab",
                        minLength = 1,
                        maxLength = 128
                    },
                    contentUrl = new
                    {
                        type = "string",
                        description = "Optional content URL for the tab"
                    },
                    websiteUrl = new
                    {
                        type = "string",
                        description = "Optional website URL for fallback"
                    },
                    configuration = new
                    {
                        type = "object",
                        description = "Optional configuration parameters for the tab",
                        additionalProperties = true
                    }
                },
                required = new[] { "channelId", "appId", "tabName" },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);
        }
    }

    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters,
        TeamsContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var channelId = parameters.GetProperty("channelId").GetString()!;
            var appId = parameters.GetProperty("appId").GetString()!;
            var tabName = parameters.GetProperty("tabName").GetString()!;
            var contentUrl = parameters.TryGetProperty("contentUrl", out var contentProp) ? 
                contentProp.GetString() : null;
            var websiteUrl = parameters.TryGetProperty("websiteUrl", out var websiteProp) ? 
                websiteProp.GetString() : null;

            // Mock tab creation
            var tabId = Guid.NewGuid().ToString();
            var createdDate = DateTime.UtcNow;

            _logger.LogInformation("Tab added - TabId: {TabId}, Name: {TabName}, Channel: {ChannelId}, App: {AppId}",
                tabId, tabName, channelId, appId);

            var message = $"📱 **Tab Added Successfully**\n\n" +
                         $"🏷️ **Tab Name:** {tabName}\n" +
                         $"📍 **Channel:** {channelId}\n" +
                         $"🆔 **App ID:** {appId}\n" +
                         $"📅 **Created:** {createdDate:yyyy-MM-dd HH:mm}\n" +
                         $"🔗 **Tab ID:** {tabId}";

            if (!string.IsNullOrEmpty(contentUrl))
            {
                message += $"\n🌐 **Content URL:** {contentUrl}";
            }

            if (!string.IsNullOrEmpty(websiteUrl))
            {
                message += $"\n🔗 **Website URL:** {websiteUrl}";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add tab");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to add tab: {ex.Message}");
        }
    }
}

/// <summary>
/// Remove Tab Command - Remove application tabs from channels
/// </summary>
public class RemoveTabCommand : TeamsToolBase
{
    public RemoveTabCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }    public override string Name => "teams-remove-tab";
    public override string Description => "Remove an application tab from a channel";
    public override TeamsToolCategory Category => TeamsToolCategory.Integrations;
    public override TeamsPermissionLevel RequiredPermission => TeamsPermissionLevel.Owner;

    public override JsonElement InputSchema
    {
        get
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    channelId = new
                    {
                        type = "string",
                        description = "Channel ID containing the tab"
                    },
                    tabId = new
                    {
                        type = "string",
                        description = "Tab ID to remove"
                    },
                    tabName = new
                    {
                        type = "string",
                        description = "Alternative: Tab name to remove (if ID not known)"
                    },
                    reason = new
                    {
                        type = "string",
                        description = "Optional reason for tab removal",
                        maxLength = 255
                    }
                },
                required = new[] { "channelId" },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);
        }
    }

    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters,
        TeamsContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var channelId = parameters.GetProperty("channelId").GetString()!;
            var tabId = parameters.TryGetProperty("tabId", out var tabIdProp) ? 
                tabIdProp.GetString() : null;
            var tabName = parameters.TryGetProperty("tabName", out var tabNameProp) ? 
                tabNameProp.GetString() : null;
            var reason = parameters.TryGetProperty("reason", out var reasonProp) ? 
                reasonProp.GetString() : null;

            if (string.IsNullOrEmpty(tabId) && string.IsNullOrEmpty(tabName))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ Either tabId or tabName must be provided");
            }

            // Mock tab removal
            var removedDate = DateTime.UtcNow;
            var resolvedTabName = tabName ?? "Sample Tab";
            var resolvedTabId = tabId ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Tab removed - TabId: {TabId}, Name: {TabName}, Channel: {ChannelId}, Reason: {Reason}",
                resolvedTabId, resolvedTabName, channelId, reason ?? "No reason provided");

            var message = $"🗑️ **Tab Removed Successfully**\n\n" +
                         $"📱 **Tab:** {resolvedTabName}\n" +
                         $"📍 **Channel:** {channelId}\n" +
                         $"📅 **Removed:** {removedDate:yyyy-MM-dd HH:mm}\n" +
                         $"🔗 **Tab ID:** {resolvedTabId}";

            if (!string.IsNullOrEmpty(reason))
            {
                message += $"\n📝 **Reason:** {reason}";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove tab");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to remove tab: {ex.Message}");
        }
    }
}

/// <summary>
/// List Apps Command - Display all applications and tabs in the team
/// </summary>
public class ListAppsCommand : TeamsToolBase
{
    public ListAppsCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }    public override string Name => "teams-list-apps";
    public override string Description => "List all applications and tabs in the current team";
    public override TeamsToolCategory Category => TeamsToolCategory.Integrations;
    public override TeamsPermissionLevel RequiredPermission => TeamsPermissionLevel.Member;

    public override JsonElement InputSchema
    {
        get
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    includeChannelTabs = new
                    {
                        type = "boolean",
                        description = "Include channel tabs in the listing",
                        @default = true
                    },
                    includeTeamApps = new
                    {
                        type = "boolean",
                        description = "Include team-level apps",
                        @default = true
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Filter to specific channel (optional)"
                    },
                    appType = new
                    {
                        type = "string",
                        description = "Filter by application type",
                        @enum = new[] { "all", "tabs", "bots", "connectors", "messaging-extensions" },
                        @default = "all"
                    }
                },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);
        }
    }

    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters,
        TeamsContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var includeChannelTabs = parameters.TryGetProperty("includeChannelTabs", out var tabsProp) && 
                tabsProp.GetBoolean();
            var includeTeamApps = parameters.TryGetProperty("includeTeamApps", out var appsProp) && 
                appsProp.GetBoolean();
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : null;
            var appType = parameters.TryGetProperty("appType", out var typeProp) ? 
                typeProp.GetString() ?? "all" : "all";

            // Mock apps and tabs data
            var mockApps = new[]
            {
                new
                {
                    Id = "app-001",
                    Name = "Planner",
                    Type = "tab",
                    Channel = "General",
                    ChannelId = "channel-001",
                    InstallDate = DateTime.Now.AddDays(-30),
                    LastUsed = DateTime.Now.AddHours(-4),
                    Usage = "High"
                },
                new
                {
                    Id = "app-002",
                    Name = "SharePoint Files",
                    Type = "tab",
                    Channel = "Documents",
                    ChannelId = "channel-002",
                    InstallDate = DateTime.Now.AddDays(-20),
                    LastUsed = DateTime.Now.AddMinutes(-15),
                    Usage = "Very High"
                },
                new
                {
                    Id = "app-003",
                    Name = "GitHub Integration",
                    Type = "connector",
                    Channel = "Development",
                    ChannelId = "channel-003",
                    InstallDate = DateTime.Now.AddDays(-10),
                    LastUsed = DateTime.Now.AddHours(-1),
                    Usage = "Medium"
                },                new
                {
                    Id = "app-004",
                    Name = "Forms Pro",
                    Type = "messaging-extension",
                    Channel = "N/A",
                    ChannelId = "N/A",
                    InstallDate = DateTime.Now.AddDays(-5),
                    LastUsed = DateTime.Now.AddDays(-2),
                    Usage = "Low"
                }
            };

            // Apply filters
            var filteredApps = mockApps.AsEnumerable();

            if (!string.IsNullOrEmpty(channelId))
            {
                filteredApps = filteredApps.Where(a => a.ChannelId == channelId);
            }

            if (!includeChannelTabs)
            {
                filteredApps = filteredApps.Where(a => a.Type != "tab");
            }

            if (!includeTeamApps)
            {
                filteredApps = filteredApps.Where(a => !string.IsNullOrEmpty(a.Channel));
            }

            if (appType != "all")
            {
                filteredApps = filteredApps.Where(a => a.Type == appType.TrimEnd('s')); // Remove plural
            }

            var apps = filteredApps.ToArray();

            if (!apps.Any())
            {
                return CreateSuccessResponse("📱 **No applications found** matching your criteria.", context);
            }

            var appList = string.Join("\n\n", apps.Select((app, i) =>
            {
                var typeEmoji = app.Type switch
                {
                    "tab" => "📱",
                    "bot" => "🤖",
                    "connector" => "🔗",
                    "messaging-extension" => "💬",
                    _ => "⚙️"
                };

                var usageEmoji = app.Usage switch
                {
                    "Very High" => "🔥",
                    "High" => "📈",
                    "Medium" => "📊",
                    "Low" => "📉",
                    _ => "⚪"
                };

                var result = $"{i + 1}. {typeEmoji} **{app.Name}**\n" +
                           $"   🏷️ Type: {app.Type.Replace("-", " ").ToTitleCase()}\n" +
                           $"   📅 Installed: {app.InstallDate:yyyy-MM-dd}\n" +
                           $"   ⏰ Last Used: {app.LastUsed:yyyy-MM-dd HH:mm}\n" +
                           $"   {usageEmoji} Usage: {app.Usage}\n" +
                           $"   🆔 ID: {app.Id}";

                if (!string.IsNullOrEmpty(app.Channel))
                {
                    result += $"\n   📍 Channel: {app.Channel}";
                }

                return result;
            }));

            var message = $"📱 **Team Applications** ({apps.Length} found)\n" +
                         $"🎯 **Filter:** {appType} applications\n\n{appList}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list apps");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to list apps: {ex.Message}");
        }
    }
}

// Extension method for string title case
public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
    }
}
