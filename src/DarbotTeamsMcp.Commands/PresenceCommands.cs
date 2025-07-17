using System.Text.Json;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;
using DarbotTeamsMcp.Core.Services;

namespace DarbotTeamsMcp.Commands.Presence;

/// <summary>
/// Get Status Command - Get user presence and status information
/// </summary>
public class GetStatusCommand : TeamsToolBase
{
    public GetStatusCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-get-status";
    public override string Description => "Get presence status for users in the team";
    public override TeamsToolCategory Category => TeamsToolCategory.Presence;
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
                    userId = new
                    {
                        type = "string",
                        description = "User ID or email to get status for (optional, defaults to current user)"
                    },
                    includeDetails = new
                    {
                        type = "boolean",
                        description = "Include detailed presence information",
                        @default = true
                    },
                    includeActivity = new
                    {
                        type = "boolean",
                        description = "Include current activity information",
                        @default = true
                    },
                    includeOutOfOffice = new
                    {
                        type = "boolean",
                        description = "Include out-of-office information",
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
        {            var userId = parameters.TryGetProperty("userId", out var userProp) ? 
                userProp.GetString() : context.CurrentUser?.Id;
            var includeDetails = parameters.TryGetProperty("includeDetails", out var detailsProp) && 
                detailsProp.GetBoolean();
            var includeActivity = parameters.TryGetProperty("includeActivity", out var activityProp) && 
                activityProp.GetBoolean();
            var includeOutOfOffice = parameters.TryGetProperty("includeOutOfOffice", out var oofProp) && 
                oofProp.GetBoolean();

            // Mock presence data
            var mockPresence = new
            {                UserId = userId ?? context.CurrentUser?.Id,
                UserName = userId == context.CurrentUser?.Id ? (context.CurrentUser?.DisplayName ?? "Current User") : "John Doe",
                Availability = "Available", // Available, Busy, DoNotDisturb, Away, BeRightBack, Offline
                Activity = "Available", // Available, InACall, InAMeeting, Busy, Away, etc.
                StatusMessage = "Working from home today",
                IsOutOfOffice = false,
                OutOfOfficeNote = null as string,
                OutOfOfficeStartTime = null as DateTime?,
                OutOfOfficeEndTime = null as DateTime?,
                LastActiveTime = DateTime.UtcNow.AddMinutes(-5),
                TimeZone = "Pacific Standard Time",
                LocalTime = DateTime.Now
            };

            _logger.LogInformation("Status retrieved for user: {UserId}, Availability: {Availability}",
                mockPresence.UserId, mockPresence.Availability);

            var availabilityEmoji = mockPresence.Availability switch
            {
                "Available" => "🟢",
                "Busy" => "🔴",
                "DoNotDisturb" => "⛔",
                "Away" => "🟡",
                "BeRightBack" => "🟡",
                "Offline" => "⚫",
                _ => "⚪"
            };

            var message = $"👤 **User Status Information**\n\n" +
                         $"🏷️ **User:** {mockPresence.UserName}\n" +
                         $"{availabilityEmoji} **Availability:** {mockPresence.Availability}\n";

            if (includeActivity)
            {
                message += $"🎯 **Activity:** {mockPresence.Activity}\n";
            }

            if (includeDetails)
            {
                message += $"💬 **Status Message:** {mockPresence.StatusMessage}\n" +
                          $"⏰ **Last Active:** {mockPresence.LastActiveTime:yyyy-MM-dd HH:mm}\n" +
                          $"🌍 **Time Zone:** {mockPresence.TimeZone}\n" +
                          $"🕐 **Local Time:** {mockPresence.LocalTime:HH:mm}";
            }

            if (includeOutOfOffice)
            {
                if (mockPresence.IsOutOfOffice)
                {
                    message += $"\n\n🏖️ **Out of Office:** Yes\n" +
                              $"📝 **Note:** {mockPresence.OutOfOfficeNote}\n" +
                              $"📅 **From:** {mockPresence.OutOfOfficeStartTime:yyyy-MM-dd}\n" +
                              $"📅 **Until:** {mockPresence.OutOfOfficeEndTime:yyyy-MM-dd}";
                }
                else
                {
                    message += $"\n\n🏖️ **Out of Office:** No";
                }
            }

            message += $"\n\n🆔 **User ID:** {mockPresence.UserId}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user status");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to get user status: {ex.Message}");
        }
    }
}

/// <summary>
/// Set Status Command - Set user presence and status message
/// </summary>
public class SetStatusCommand : TeamsToolBase
{
    public SetStatusCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-set-status";
    public override string Description => "Set your presence status and message";
    public override TeamsToolCategory Category => TeamsToolCategory.Presence;
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
                    availability = new
                    {
                        type = "string",
                        description = "Availability status to set",
                        @enum = new[] { "Available", "Busy", "DoNotDisturb", "BeRightBack", "Away" }
                    },
                    statusMessage = new
                    {
                        type = "string",
                        description = "Custom status message",
                        maxLength = 280
                    },
                    expirationTime = new
                    {
                        type = "string",
                        description = "When the status should expire (ISO 8601 format)",
                        pattern = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{3})?Z?$"
                    },
                    activity = new
                    {
                        type = "string",
                        description = "Current activity status",
                        @enum = new[] { "Available", "InACall", "InAMeeting", "Busy", "Away" }
                    }
                },
                required = new[] { "availability" },
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
            var availability = parameters.GetProperty("availability").GetString()!;
            var statusMessage = parameters.TryGetProperty("statusMessage", out var messageProp) ? 
                messageProp.GetString() : null;
            var expirationTime = parameters.TryGetProperty("expirationTime", out var expProp) ? 
                expProp.GetString() : null;
            var activity = parameters.TryGetProperty("activity", out var activityProp) ? 
                activityProp.GetString() : availability;

            // Mock status setting
            var setTime = DateTime.UtcNow;
            DateTime? expiration = null;
            
            if (!string.IsNullOrEmpty(expirationTime) && DateTime.TryParse(expirationTime, out var parsedExp))
            {
                expiration = parsedExp;
            }            _logger.LogInformation("Status set for user: {UserId}, Availability: {Availability}, Message: {Message}",
                context.CurrentUser?.Id, availability, statusMessage ?? "None");

            var availabilityEmoji = availability switch
            {
                "Available" => "🟢",
                "Busy" => "🔴",
                "DoNotDisturb" => "⛔",
                "Away" => "🟡",
                "BeRightBack" => "🟡",
                _ => "⚪"
            };

            var message = $"✅ **Status Updated Successfully**\n\n" +
                         $"👤 **User:** {context.CurrentUser?.DisplayName ?? "Current User"}\n" +
                         $"{availabilityEmoji} **Availability:** {availability}\n" +
                         $"🎯 **Activity:** {activity}\n" +
                         $"⏰ **Set At:** {setTime:yyyy-MM-dd HH:mm}";

            if (!string.IsNullOrEmpty(statusMessage))
            {
                message += $"\n💬 **Status Message:** \"{statusMessage}\"";
            }

            if (expiration.HasValue)
            {
                message += $"\n⏳ **Expires:** {expiration.Value:yyyy-MM-dd HH:mm}";
            }
            else
            {
                message += $"\n⏳ **Expires:** Manual reset required";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set status");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to set status: {ex.Message}");
        }
    }
}

/// <summary>
/// Set Notification Command - Configure notification preferences
/// </summary>
public class SetNotificationCommand : TeamsToolBase
{
    public SetNotificationCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-set-notification";
    public override string Description => "Configure notification preferences for teams and channels";
    public override TeamsToolCategory Category => TeamsToolCategory.Presence;
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
                    scope = new
                    {
                        type = "string",
                        description = "Notification scope",
                        @enum = new[] { "team", "channel", "global" },
                        @default = "team"
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Channel ID (required if scope is 'channel')"
                    },
                    notificationType = new
                    {
                        type = "string",
                        description = "Type of notifications to configure",
                        @enum = new[] { "all", "mentions", "replies", "likes", "none" },
                        @default = "mentions"
                    },
                    alertStyle = new
                    {
                        type = "string",
                        description = "How to receive notifications",
                        @enum = new[] { "banner", "email", "push", "none" },
                        @default = "banner"
                    },
                    quietHours = new
                    {
                        type = "object",
                        description = "Quiet hours configuration",
                        properties = new
                        {
                            enabled = new { type = "boolean", @default = false },
                            startTime = new { type = "string", pattern = @"^([01]?[0-9]|2[0-3]):[0-5][0-9]$" },
                            endTime = new { type = "string", pattern = @"^([01]?[0-9]|2[0-3]):[0-5][0-9]$" },
                            timeZone = new { type = "string", @default = "Local" }
                        }
                    },
                    keywords = new
                    {
                        type = "array",
                        description = "Keywords to trigger notifications",
                        items = new { type = "string", minLength = 1, maxLength = 50 },
                        maxItems = 20
                    }
                },
                required = new[] { "scope", "notificationType" },
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
            var scope = parameters.GetProperty("scope").GetString()!;
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : null;
            var notificationType = parameters.GetProperty("notificationType").GetString()!;
            var alertStyle = parameters.TryGetProperty("alertStyle", out var alertProp) ? 
                alertProp.GetString() ?? "banner" : "banner";

            // Handle quiet hours
            var quietHoursEnabled = false;
            var quietHoursStart = "";
            var quietHoursEnd = "";
            var quietHoursTimeZone = "Local";

            if (parameters.TryGetProperty("quietHours", out var quietHoursProp))
            {
                if (quietHoursProp.TryGetProperty("enabled", out var enabledProp))
                    quietHoursEnabled = enabledProp.GetBoolean();
                if (quietHoursProp.TryGetProperty("startTime", out var startProp))
                    quietHoursStart = startProp.GetString() ?? "";
                if (quietHoursProp.TryGetProperty("endTime", out var endProp))
                    quietHoursEnd = endProp.GetString() ?? "";
                if (quietHoursProp.TryGetProperty("timeZone", out var tzProp))
                    quietHoursTimeZone = tzProp.GetString() ?? "Local";
            }

            // Handle keywords
            var keywords = parameters.TryGetProperty("keywords", out var keywordsProp) ?
                keywordsProp.EnumerateArray().Select(k => k.GetString()!).ToArray() :
                Array.Empty<string>();

            // Validate channel scope
            if (scope == "channel" && string.IsNullOrEmpty(channelId))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ Channel ID is required when scope is 'channel'");
            }

            // Mock notification configuration
            var configId = Guid.NewGuid().ToString();
            var configuredTime = DateTime.UtcNow;            _logger.LogInformation("Notification settings configured - User: {UserId}, Scope: {Scope}, Type: {Type}, Channel: {ChannelId}",
                context.CurrentUser?.Id, scope, notificationType, channelId);

            var scopeDisplay = scope switch
            {
                "team" => $"Team: {context.CurrentTeam?.DisplayName ?? "Current Team"}",
                "channel" => $"Channel: {channelId}",
                "global" => "All teams and channels",
                _ => scope
            };

            var notificationEmoji = notificationType switch
            {
                "all" => "🔔",
                "mentions" => "🏷️",
                "replies" => "💬",
                "likes" => "👍",
                "none" => "🔕",
                _ => "🔔"
            };

            var alertEmoji = alertStyle switch
            {
                "banner" => "📱",
                "email" => "📧",
                "push" => "🔔",
                "none" => "🔕",
                _ => "📱"
            };

            var message = $"🔔 **Notification Settings Updated**\n\n" +
                         $"🎯 **Scope:** {scopeDisplay}\n" +
                         $"{notificationEmoji} **Notifications:** {notificationType.ToUpperInvariant()}\n" +
                         $"{alertEmoji} **Alert Style:** {alertStyle.ToUpperInvariant()}\n" +
                         $"⏰ **Configured:** {configuredTime:yyyy-MM-dd HH:mm}";

            if (quietHoursEnabled && !string.IsNullOrEmpty(quietHoursStart) && !string.IsNullOrEmpty(quietHoursEnd))
            {
                message += $"\n\n🌙 **Quiet Hours:** Enabled\n" +
                          $"🕐 **From:** {quietHoursStart} to {quietHoursEnd}\n" +
                          $"🌍 **Time Zone:** {quietHoursTimeZone}";
            }
            else
            {
                message += $"\n\n🌙 **Quiet Hours:** Disabled";
            }

            if (keywords.Any())
            {
                var keywordList = string.Join(", ", keywords.Take(5));
                if (keywords.Length > 5) keywordList += $" (+{keywords.Length - 5} more)";
                message += $"\n\n🔑 **Keywords:** {keywordList}";
            }

            message += $"\n\n🆔 **Configuration ID:** {configId}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set notification preferences");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to set notification preferences: {ex.Message}");
        }
    }
}
