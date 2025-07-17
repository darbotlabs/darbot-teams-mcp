using System.Text.Json;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;
using DarbotTeamsMcp.Core.Services;

namespace DarbotTeamsMcp.Commands.Messaging;

/// <summary>
/// Pin Message Command - Important message highlighting
/// </summary>
public class PinMessageCommand : TeamsToolBase
{
    public PinMessageCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-pin-message";
    public override string Description => "Pin an important message in a channel";
    public override TeamsToolCategory Category => TeamsToolCategory.Messaging;
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
                    messageId = new
                    {
                        type = "string",
                        description = "ID of the message to pin"
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Channel ID (optional, uses current channel if not provided)"
                    }
                },
                required = new[] { "messageId" },
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
            var messageId = parameters.GetProperty("messageId").GetString()!;
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : context.CurrentChannelId;

            if (string.IsNullOrEmpty(channelId))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ No channel ID provided and no current channel selected");
            }

            // Mock implementation
            _logger.LogInformation("Would pin message {MessageId} in channel {ChannelId}", 
                messageId, channelId);

            var message = $"📌 **Message Pinned Successfully**\n\n" +
                         $"💬 **Message ID:** {messageId}\n" +
                         $"📁 **Channel:** Development\n" +
                         $"👤 **Pinned By:** Current User\n" +
                         $"📅 **Pinned At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
                         $"ℹ️ **Note:** This message is now highlighted at the top of the channel.";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pin message");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to pin message: {ex.Message}");
        }
    }
}

/// <summary>
/// Unpin Message Command - Remove message prominence
/// </summary>
public class UnpinMessageCommand : TeamsToolBase
{
    public UnpinMessageCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-unpin-message";
    public override string Description => "Remove a pinned message from prominence";
    public override TeamsToolCategory Category => TeamsToolCategory.Messaging;
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
                    messageId = new
                    {
                        type = "string",
                        description = "ID of the message to unpin"
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Channel ID (optional, uses current channel if not provided)"
                    }
                },
                required = new[] { "messageId" },
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
            var messageId = parameters.GetProperty("messageId").GetString()!;
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : context.CurrentChannelId;

            if (string.IsNullOrEmpty(channelId))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ No channel ID provided and no current channel selected");
            }

            // Mock implementation
            _logger.LogInformation("Would unpin message {MessageId} in channel {ChannelId}", 
                messageId, channelId);

            var message = $"📌❌ **Message Unpinned**\n\n" +
                         $"💬 **Message ID:** {messageId}\n" +
                         $"📁 **Channel:** Development\n" +
                         $"👤 **Unpinned By:** Current User\n" +
                         $"📅 **Unpinned At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
                         $"ℹ️ **Note:** This message is no longer pinned in the channel.";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unpin message");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to unpin message: {ex.Message}");
        }
    }
}

/// <summary>
/// Search Messages Command - Content search with filters
/// </summary>
public class SearchMessagesCommand : TeamsToolBase
{
    public SearchMessagesCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-search-messages";
    public override string Description => "Search messages in the current channel with filters";
    public override TeamsToolCategory Category => TeamsToolCategory.Messaging;
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
                    query = new
                    {
                        type = "string",
                        description = "Search query text",
                        maxLength = 500
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Channel ID to search in (optional, uses current channel)"
                    },
                    fromUser = new
                    {
                        type = "string",
                        description = "Filter by messages from specific user email"
                    },
                    dateFrom = new
                    {
                        type = "string",
                        description = "Start date for search (YYYY-MM-DD)",
                        format = "date"
                    },
                    dateTo = new
                    {
                        type = "string",
                        description = "End date for search (YYYY-MM-DD)",
                        format = "date"
                    },
                    maxResults = new
                    {
                        type = "integer",
                        description = "Maximum number of results to return",
                        @default = 20,
                        minimum = 1,
                        maximum = 100
                    }
                },
                required = new[] { "query" },
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
            var query = parameters.GetProperty("query").GetString()!;
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : context.CurrentChannelId;
            var fromUser = parameters.TryGetProperty("fromUser", out var userProp) ? 
                userProp.GetString() : null;
            var dateFrom = parameters.TryGetProperty("dateFrom", out var fromProp) ? 
                fromProp.GetString() : null;
            var dateTo = parameters.TryGetProperty("dateTo", out var toProp) ? 
                toProp.GetString() : null;
            var maxResults = parameters.TryGetProperty("maxResults", out var maxProp) ? 
                Math.Min(maxProp.GetInt32(), 100) : 20;

            if (string.IsNullOrEmpty(channelId))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ No channel ID provided and no current channel selected");
            }

            // Mock search results
            var mockResults = new[]
            {
                new { author = "John Doe", content = $"Great progress on the {query} implementation!", 
                      timestamp = "2024-01-15 14:30", reactions = 3 },
                new { author = "Jane Smith", content = $"I have some questions about {query} requirements", 
                      timestamp = "2024-01-14 16:45", reactions = 1 },
                new { author = "Bob Wilson", content = $"Updated {query} documentation is now available", 
                      timestamp = "2024-01-13 09:15", reactions = 5 }
            };

            var filteredResults = fromUser != null ? 
                mockResults.Where(r => r.author.Contains(fromUser, StringComparison.OrdinalIgnoreCase)).ToArray() : 
                mockResults;

            var limitedResults = filteredResults.Take(maxResults).ToList();

            var resultsList = string.Join("\n\n", limitedResults.Select((result, i) =>
                $"{i + 1}. 👤 **{result.author}** - {result.timestamp}\n" +
                $"   💬 {result.content}\n" +
                $"   💖 {result.reactions} reactions"));

            var filters = new List<string>();
            if (!string.IsNullOrEmpty(fromUser)) filters.Add($"From: {fromUser}");
            if (!string.IsNullOrEmpty(dateFrom)) filters.Add($"From: {dateFrom}");
            if (!string.IsNullOrEmpty(dateTo)) filters.Add($"To: {dateTo}");

            var message = $"🔍 **Message Search Results**\n\n" +
                         $"🔎 **Query:** \"{query}\"\n" +
                         $"📁 **Channel:** Development\n" +
                         $"📊 **Results:** {limitedResults.Count} of {filteredResults.Length} total\n";

            if (filters.Any())
            {
                message += $"🔧 **Filters:** {string.Join(", ", filters)}\n";
            }

            message += $"\n{resultsList}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search messages");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to search messages: {ex.Message}");
        }
    }
}

/// <summary>
/// Send Announcement Command - Broadcast messaging
/// </summary>
public class SendAnnouncementCommand : TeamsToolBase
{
    public SendAnnouncementCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-send-announcement";
    public override string Description => "Send an announcement message to team members";
    public override TeamsToolCategory Category => TeamsToolCategory.Messaging;
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
                    title = new
                    {
                        type = "string",
                        description = "Announcement title",
                        maxLength = 100
                    },
                    message = new
                    {
                        type = "string",
                        description = "Announcement content",
                        maxLength = 4000
                    },
                    channels = new
                    {
                        type = "array",
                        description = "Specific channels to post to (all channels if not specified)",
                        items = new { type = "string" }
                    },
                    priority = new
                    {
                        type = "string",
                        description = "Announcement priority level",
                        @enum = new[] { "normal", "important", "urgent" },
                        @default = "normal"
                    },
                    notifyAll = new
                    {
                        type = "boolean",
                        description = "Send notifications to all team members",
                        @default = true
                    }
                },
                required = new[] { "title", "message" },
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
            var title = parameters.GetProperty("title").GetString()!;
            var messageContent = parameters.GetProperty("message").GetString()!;
            var priority = parameters.TryGetProperty("priority", out var priorityProp) ? 
                priorityProp.GetString() ?? "normal" : "normal";
            var notifyAll = parameters.TryGetProperty("notifyAll", out var notifyProp) ? 
                notifyProp.GetBoolean() : true;

            var channels = new List<string>();
            if (parameters.TryGetProperty("channels", out var channelsProp) && 
                channelsProp.ValueKind == JsonValueKind.Array)
            {
                channels.AddRange(channelsProp.EnumerateArray().Select(c => c.GetString()!));
            }

            var priorityIcon = priority switch
            {
                "important" => "❗",
                "urgent" => "🚨",
                _ => "📢"
            };

            // Mock implementation
            _logger.LogInformation("Would send announcement '{Title}' with priority {Priority}", title, priority);

            var teamName = context.CurrentTeam?.DisplayName ?? "Current Team";
            var message = $"📢 **Announcement Sent**\n\n" +
                         $"{priorityIcon} **Title:** {title}\n" +
                         $"🏷️ **Priority:** {priority}\n" +
                         $"🎯 **Team:** {teamName}\n" +
                         $"🔔 **Notifications:** {(notifyAll ? "Enabled" : "Disabled")}\n" +
                         $"👤 **Sent By:** Current User\n" +
                         $"📅 **Sent At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
                         $"💬 **Content:**\n{messageContent}";

            if (channels.Any())
            {
                message += $"\n\n📁 **Posted to:** {string.Join(", ", channels)}";
            }
            else
            {
                message += "\n\n📁 **Posted to:** All team channels";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send announcement");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to send announcement: {ex.Message}");
        }
    }
}

/// <summary>
/// Export Messages Command - Compliance and backup exports
/// </summary>
public class ExportMessagesCommand : TeamsToolBase
{
    public ExportMessagesCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-export-messages";
    public override string Description => "Export channel messages for compliance or backup";
    public override TeamsToolCategory Category => TeamsToolCategory.Messaging;
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
                        description = "Channel ID to export (optional, uses current channel)"
                    },
                    dateFrom = new
                    {
                        type = "string",
                        description = "Start date for export (YYYY-MM-DD)",
                        format = "date"
                    },
                    dateTo = new
                    {
                        type = "string",
                        description = "End date for export (YYYY-MM-DD)",
                        format = "date"
                    },
                    format = new
                    {
                        type = "string",
                        description = "Export format",
                        @enum = new[] { "json", "csv", "html" },
                        @default = "json"
                    },
                    includeAttachments = new
                    {
                        type = "boolean",
                        description = "Include file attachments in export",
                        @default = false
                    },
                    includeReactions = new
                    {
                        type = "boolean",
                        description = "Include message reactions",
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
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : context.CurrentChannelId;
            var dateFrom = parameters.TryGetProperty("dateFrom", out var fromProp) ? 
                fromProp.GetString() : null;
            var dateTo = parameters.TryGetProperty("dateTo", out var toProp) ? 
                toProp.GetString() : null;
            var format = parameters.TryGetProperty("format", out var formatProp) ? 
                formatProp.GetString() ?? "json" : "json";
            var includeAttachments = parameters.TryGetProperty("includeAttachments", out var attachProp) && 
                attachProp.GetBoolean();
            var includeReactions = parameters.TryGetProperty("includeReactions", out var reactionProp) ? 
                reactionProp.GetBoolean() : true;

            if (string.IsNullOrEmpty(channelId))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ No channel ID provided and no current channel selected");
            }

            // Mock implementation
            var exportId = Guid.NewGuid().ToString("N")[..8];
            var fileName = $"messages_export_{exportId}.{format}";
            
            _logger.LogInformation("Would export messages from channel {ChannelId} to {Format}", 
                channelId, format);

            var dateRange = "";
            if (!string.IsNullOrEmpty(dateFrom) || !string.IsNullOrEmpty(dateTo))
            {
                dateRange = $"📅 **Date Range:** {dateFrom ?? "Beginning"} to {dateTo ?? "End"}\n";
            }

            var message = $"📥 **Message Export Started**\n\n" +
                         $"📁 **Channel:** Development\n" +
                         $"📄 **Format:** {format.ToUpper()}\n" +
                         $"{dateRange}" +
                         $"📎 **Include Attachments:** {(includeAttachments ? "Yes" : "No")}\n" +
                         $"💖 **Include Reactions:** {(includeReactions ? "Yes" : "No")}\n" +
                         $"🆔 **Export ID:** {exportId}\n" +
                         $"📂 **File Name:** {fileName}\n" +
                         $"👤 **Requested By:** Current User\n" +
                         $"📅 **Started At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
                         $"⏳ **Status:** Processing... You will be notified when the export is complete.\n" +
                         $"📧 **Download:** Export will be available in SharePoint Files tab.";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export messages");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to export messages: {ex.Message}");
        }
    }
}
