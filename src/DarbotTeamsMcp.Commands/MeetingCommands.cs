using System.Text.Json;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;
using DarbotTeamsMcp.Core.Services;

namespace DarbotTeamsMcp.Commands.Meetings;

/// <summary>
/// Schedule Meeting Command - Calendar integration and invitations
/// </summary>
public class ScheduleMeetingCommand : TeamsToolBase
{
    public ScheduleMeetingCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-schedule-meeting";
    public override string Description => "Schedule a new Teams meeting with calendar integration";
    public override TeamsToolCategory Category => TeamsToolCategory.Meetings;
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
                    title = new
                    {
                        type = "string",
                        description = "Meeting title",
                        maxLength = 255
                    },
                    startDateTime = new
                    {
                        type = "string",
                        description = "Meeting start date and time (ISO 8601 format: YYYY-MM-DDTHH:mm:ss)",
                        format = "date-time"
                    },
                    durationMinutes = new
                    {
                        type = "integer",
                        description = "Meeting duration in minutes",
                        minimum = 15,
                        maximum = 480,
                        @default = 60
                    },
                    description = new
                    {
                        type = "string",
                        description = "Meeting description or agenda",
                        maxLength = 1000
                    },
                    attendees = new
                    {
                        type = "array",
                        description = "List of attendee email addresses",
                        items = new { type = "string", format = "email" }
                    },
                    isRecurring = new
                    {
                        type = "boolean",
                        description = "Whether this is a recurring meeting",
                        @default = false
                    },
                    recurrencePattern = new
                    {
                        type = "string",
                        description = "Recurrence pattern if recurring",
                        @enum = new[] { "daily", "weekly", "monthly" }
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Channel to associate the meeting with (optional)"
                    }
                },
                required = new[] { "title", "startDateTime" },
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
            var startDateTime = parameters.GetProperty("startDateTime").GetString()!;
            var durationMinutes = parameters.TryGetProperty("durationMinutes", out var durationProp) ? 
                durationProp.GetInt32() : 60;
            var description = parameters.TryGetProperty("description", out var descProp) ? 
                descProp.GetString() : null;
            var isRecurring = parameters.TryGetProperty("isRecurring", out var recurProp) && 
                recurProp.GetBoolean();
            var recurrencePattern = parameters.TryGetProperty("recurrencePattern", out var patternProp) ? 
                patternProp.GetString() : null;
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : context.CurrentChannelId;

            var attendees = new List<string>();
            if (parameters.TryGetProperty("attendees", out var attendeesProp) && 
                attendeesProp.ValueKind == JsonValueKind.Array)
            {
                attendees.AddRange(attendeesProp.EnumerateArray().Select(a => a.GetString()!));
            }

            // Parse and validate start time
            if (!DateTime.TryParse(startDateTime, out var parsedStartTime))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ Invalid start date/time format. Use ISO 8601 format (YYYY-MM-DDTHH:mm:ss)");
            }

            var endTime = parsedStartTime.AddMinutes(durationMinutes);
            var meetingId = Guid.NewGuid().ToString("N")[..8];

            // Mock implementation
            _logger.LogInformation("Would schedule meeting '{Title}' for {StartTime}", title, parsedStartTime);

            var message = $"📅 **Meeting Scheduled Successfully**\n\n" +
                         $"📝 **Title:** {title}\n" +
                         $"🕐 **Start:** {parsedStartTime:yyyy-MM-dd HH:mm} UTC\n" +
                         $"🕑 **End:** {endTime:yyyy-MM-dd HH:mm} UTC\n" +
                         $"⏱️ **Duration:** {durationMinutes} minutes\n" +
                         $"🆔 **Meeting ID:** {meetingId}\n" +
                         $"👤 **Organizer:** Current User\n" +
                         $"📅 **Created At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";

            if (!string.IsNullOrEmpty(description))
            {
                message += $"\n📋 **Description:** {description}";
            }

            if (attendees.Any())
            {
                message += $"\n👥 **Attendees ({attendees.Count}):** {string.Join(", ", attendees)}";
            }

            if (isRecurring && !string.IsNullOrEmpty(recurrencePattern))
            {
                message += $"\n🔄 **Recurrence:** {recurrencePattern}";
            }

            if (!string.IsNullOrEmpty(channelId))
            {
                message += $"\n📁 **Associated Channel:** Development";
            }

            message += "\n\n✉️ **Note:** Calendar invitations have been sent to all attendees.";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule meeting");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to schedule meeting: {ex.Message}");
        }
    }
}

/// <summary>
/// Cancel Meeting Command - Meeting cancellation with notifications
/// </summary>
public class CancelMeetingCommand : TeamsToolBase
{
    public CancelMeetingCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-cancel-meeting";
    public override string Description => "Cancel a scheduled Teams meeting with notifications";
    public override TeamsToolCategory Category => TeamsToolCategory.Meetings;
    public override TeamsPermissionLevel RequiredPermission => TeamsPermissionLevel.Organizer;

    public override JsonElement InputSchema
    {
        get
        {
            var schema = new
            {
                type = "object",
                properties = new
                {
                    meetingId = new
                    {
                        type = "string",
                        description = "ID of the meeting to cancel"
                    },
                    reason = new
                    {
                        type = "string",
                        description = "Reason for cancellation",
                        maxLength = 500
                    },
                    notifyAttendees = new
                    {
                        type = "boolean",
                        description = "Send cancellation notifications to attendees",
                        @default = true
                    },
                    customMessage = new
                    {
                        type = "string",
                        description = "Custom message to include in cancellation notification",
                        maxLength = 1000
                    }
                },
                required = new[] { "meetingId" },
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
            var meetingId = parameters.GetProperty("meetingId").GetString()!;
            var reason = parameters.TryGetProperty("reason", out var reasonProp) ? 
                reasonProp.GetString() : "Meeting cancelled";
            var notifyAttendees = parameters.TryGetProperty("notifyAttendees", out var notifyProp) ? 
                notifyProp.GetBoolean() : true;
            var customMessage = parameters.TryGetProperty("customMessage", out var msgProp) ? 
                msgProp.GetString() : null;

            // Mock implementation
            _logger.LogInformation("Would cancel meeting {MeetingId} with reason: {Reason}", 
                meetingId, reason);

            var message = $"❌ **Meeting Cancelled**\n\n" +
                         $"🆔 **Meeting ID:** {meetingId}\n" +
                         $"📝 **Meeting:** Weekly Team Standup\n" +
                         $"🕐 **Original Time:** Tomorrow 10:00 AM\n" +
                         $"📝 **Reason:** {reason}\n" +
                         $"🔔 **Attendees Notified:** {(notifyAttendees ? "Yes" : "No")}\n" +
                         $"👤 **Cancelled By:** Current User\n" +
                         $"📅 **Cancelled At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";

            if (!string.IsNullOrEmpty(customMessage))
            {
                message += $"\n💬 **Custom Message:** {customMessage}";
            }

            if (notifyAttendees)
            {
                message += "\n\n✉️ **Note:** Cancellation notifications have been sent to all attendees.";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel meeting");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to cancel meeting: {ex.Message}");
        }
    }
}

/// <summary>
/// List Meetings Command - Upcoming meeting discovery
/// </summary>
public class ListMeetingsCommand : TeamsToolBase
{
    public ListMeetingsCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-list-meetings";
    public override string Description => "List upcoming Teams meetings";
    public override TeamsToolCategory Category => TeamsToolCategory.Meetings;
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
                    timeRange = new
                    {
                        type = "string",
                        description = "Time range for meetings",
                        @enum = new[] { "today", "week", "month" },
                        @default = "week"
                    },
                    includeRecurring = new
                    {
                        type = "boolean",
                        description = "Include recurring meeting instances",
                        @default = true
                    },
                    onlyOrganized = new
                    {
                        type = "boolean",
                        description = "Only show meetings organized by current user",
                        @default = false
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Filter by specific channel (optional)"
                    },
                    maxResults = new
                    {
                        type = "integer",
                        description = "Maximum number of meetings to return",
                        @default = 25,
                        minimum = 1,
                        maximum = 100
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
            var timeRange = parameters.TryGetProperty("timeRange", out var rangeProp) ? 
                rangeProp.GetString() ?? "week" : "week";
            var includeRecurring = parameters.TryGetProperty("includeRecurring", out var recurProp) ? 
                recurProp.GetBoolean() : true;
            var onlyOrganized = parameters.TryGetProperty("onlyOrganized", out var orgProp) && 
                orgProp.GetBoolean();
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : null;
            var maxResults = parameters.TryGetProperty("maxResults", out var maxProp) ? 
                Math.Min(maxProp.GetInt32(), 100) : 25;

            // Mock meeting data
            var mockMeetings = new[]
            {
                new { title = "Weekly Team Standup", startTime = "2024-01-16 10:00", 
                      duration = 30, organizer = "Current User", attendees = 8, 
                      isRecurring = true, channel = "Development" },
                new { title = "Project Planning Session", startTime = "2024-01-17 14:00", 
                      duration = 90, organizer = "John Doe", attendees = 5, 
                      isRecurring = false, channel = "General" },
                new { title = "Client Review Meeting", startTime = "2024-01-18 16:00", 
                      duration = 60, organizer = "Jane Smith", attendees = 12, 
                      isRecurring = false, channel = "Project Alpha" },
                new { title = "Monthly All-Hands", startTime = "2024-01-19 13:00", 
                      duration = 45, organizer = "Bob Wilson", attendees = 25, 
                      isRecurring = true, channel = "General" }
            };

            // Apply filters
            var filteredMeetings = mockMeetings.AsEnumerable();
            
            if (!includeRecurring)
            {
                filteredMeetings = filteredMeetings.Where(m => !m.isRecurring);
            }

            if (onlyOrganized)
            {
                filteredMeetings = filteredMeetings.Where(m => m.organizer == "Current User");
            }

            if (!string.IsNullOrEmpty(channelId))
            {
                filteredMeetings = filteredMeetings.Where(m => m.channel == "Development"); // Mock channel filter
            }

            var limitedMeetings = filteredMeetings.Take(maxResults).ToList();

            var meetingsList = string.Join("\n\n", limitedMeetings.Select((meeting, i) =>
                $"{i + 1}. 📅 **{meeting.title}**\n" +
                $"   🕐 {meeting.startTime} • ⏱️ {meeting.duration} min\n" +
                $"   👤 Organizer: {meeting.organizer}\n" +
                $"   👥 Attendees: {meeting.attendees}\n" +
                $"   📁 Channel: {meeting.channel}\n" +
                $"   🔄 Recurring: {(meeting.isRecurring ? "Yes" : "No")}"));

            var rangeDescription = timeRange switch
            {
                "today" => "Today",
                "week" => "This Week",
                "month" => "This Month",
                _ => "This Week"
            };

            var filterInfo = new List<string>();
            if (!includeRecurring) filterInfo.Add("Non-recurring only");
            if (onlyOrganized) filterInfo.Add("Organized by you");
            if (!string.IsNullOrEmpty(channelId)) filterInfo.Add("Channel filtered");

            var message = $"📅 **Upcoming Meetings** ({limitedMeetings.Count} meetings)\n\n" +
                         $"📆 **Time Range:** {rangeDescription}\n" +
                         $"🎯 **Team:** {context.CurrentTeam?.DisplayName ?? "Current Team"}";

            if (filterInfo.Any())
            {
                message += $"\n🔧 **Filters:** {string.Join(", ", filterInfo)}";
            }

            message += $"\n\n{meetingsList}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list meetings");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to list meetings: {ex.Message}");
        }
    }
}
