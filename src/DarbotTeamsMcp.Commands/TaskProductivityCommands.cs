using System.Text.Json;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;
using DarbotTeamsMcp.Core.Services;

namespace DarbotTeamsMcp.Commands.TaskProductivity;

/// <summary>
/// Assign Task Command - Create and assign tasks to team members
/// </summary>
public class AssignTaskCommand : TeamsToolBase
{
    public AssignTaskCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }    public override string Name => "teams-assign-task";
    public override string Description => "Assign a task to a team member";
    public override TeamsToolCategory Category => TeamsToolCategory.Tasks;
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
                    taskTitle = new
                    {
                        type = "string",
                        description = "Title of the task",
                        minLength = 1,
                        maxLength = 255
                    },
                    assignedTo = new
                    {
                        type = "string",
                        description = "Email address of the person to assign the task to"
                    },
                    description = new
                    {
                        type = "string",
                        description = "Detailed description of the task",
                        maxLength = 2000
                    },
                    dueDate = new
                    {
                        type = "string",
                        description = "Due date in ISO 8601 format (YYYY-MM-DD)",
                        pattern = @"^\d{4}-\d{2}-\d{2}$"
                    },
                    priority = new
                    {
                        type = "string",
                        description = "Task priority level",
                        @enum = new[] { "low", "normal", "high", "urgent" },
                        @default = "normal"
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Optional channel ID to post task notification"
                    }
                },
                required = new[] { "taskTitle", "assignedTo" },
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
            var taskTitle = parameters.GetProperty("taskTitle").GetString()!;
            var assignedTo = parameters.GetProperty("assignedTo").GetString()!;
            var description = parameters.TryGetProperty("description", out var descProp) ? 
                descProp.GetString() : null;
            var dueDate = parameters.TryGetProperty("dueDate", out var dueProp) ? 
                dueProp.GetString() : null;
            var priority = parameters.TryGetProperty("priority", out var priorityProp) ? 
                priorityProp.GetString() ?? "normal" : "normal";
            var channelId = parameters.TryGetProperty("channelId", out var channelProp) ? 
                channelProp.GetString() : null;

            // Mock task assignment implementation
            var taskId = Guid.NewGuid().ToString();
            var createdDate = DateTime.UtcNow;
            var dueDateParsed = DateTime.TryParse(dueDate, out var parsedDate) ? parsedDate : (DateTime?)null;

            _logger.LogInformation("Task assigned - ID: {TaskId}, Title: {Title}, AssignedTo: {AssignedTo}, Priority: {Priority}",
                taskId, taskTitle, assignedTo, priority);

            var priorityEmoji = priority switch
            {
                "low" => "🟢",
                "normal" => "🟡",
                "high" => "🟠",
                "urgent" => "🔴",
                _ => "🟡"
            };

            var message = $"✅ **Task Assigned Successfully**\n\n" +
                         $"📋 **Task:** {taskTitle}\n" +
                         $"👤 **Assigned To:** {assignedTo}\n" +
                         $"{priorityEmoji} **Priority:** {priority.ToUpperInvariant()}\n" +
                         $"📅 **Created:** {createdDate:yyyy-MM-dd HH:mm}\n";

            if (dueDateParsed.HasValue)
            {
                message += $"⏰ **Due Date:** {dueDateParsed.Value:yyyy-MM-dd}\n";
            }

            if (!string.IsNullOrEmpty(description))
            {
                message += $"📝 **Description:** {description}\n";
            }

            message += $"🆔 **Task ID:** {taskId}";

            if (!string.IsNullOrEmpty(channelId))
            {
                message += $"\n📢 **Notification sent to channel**";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign task");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to assign task: {ex.Message}");
        }
    }
}

/// <summary>
/// Complete Task Command - Mark tasks as completed
/// </summary>
public class CompleteTaskCommand : TeamsToolBase
{
    public CompleteTaskCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }    public override string Name => "teams-complete-task";
    public override string Description => "Mark a task as completed";
    public override TeamsToolCategory Category => TeamsToolCategory.Tasks;
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
                    taskId = new
                    {
                        type = "string",
                        description = "ID of the task to complete"
                    },
                    completionNotes = new
                    {
                        type = "string",
                        description = "Optional completion notes or comments",
                        maxLength = 1000
                    },
                    notifyAssigner = new
                    {
                        type = "boolean",
                        description = "Whether to notify the person who assigned the task",
                        @default = true
                    }
                },
                required = new[] { "taskId" },
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
            var taskId = parameters.GetProperty("taskId").GetString()!;
            var completionNotes = parameters.TryGetProperty("completionNotes", out var notesProp) ? 
                notesProp.GetString() : null;
            var notifyAssigner = parameters.TryGetProperty("notifyAssigner", out var notifyProp) && 
                notifyProp.GetBoolean();

            // Mock task completion implementation
            var completedDate = DateTime.UtcNow;
            var mockTaskTitle = "Sample Task"; // In real implementation, would fetch from Graph API

            _logger.LogInformation("Task completed - ID: {TaskId}, CompletedBy: {UserId}", 
                taskId, context.CurrentUser?.Id);

            var message = $"✅ **Task Completed Successfully**\n\n" +
                         $"📋 **Task:** {mockTaskTitle}\n" +
                         $"👤 **Completed By:** {context.CurrentUser?.DisplayName ?? "Current User"}\n" +
                         $"⏰ **Completed:** {completedDate:yyyy-MM-dd HH:mm}\n" +
                         $"🆔 **Task ID:** {taskId}";

            if (!string.IsNullOrEmpty(completionNotes))
            {
                message += $"\n📝 **Completion Notes:** {completionNotes}";
            }

            if (notifyAssigner)
            {
                message += $"\n📧 **Notification sent to task assigner**";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete task");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to complete task: {ex.Message}");
        }
    }
}

/// <summary>
/// List Tasks Command - Display team tasks and their status
/// </summary>
public class ListTasksCommand : TeamsToolBase
{
    public ListTasksCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }    public override string Name => "teams-list-tasks";
    public override string Description => "List tasks for the current team or user";
    public override TeamsToolCategory Category => TeamsToolCategory.Tasks;
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
                    filter = new
                    {
                        type = "string",
                        description = "Filter tasks by status",
                        @enum = new[] { "all", "pending", "completed", "overdue", "my-tasks" },
                        @default = "all"
                    },
                    assignedTo = new
                    {
                        type = "string",
                        description = "Filter by specific user email (optional)"
                    },
                    priority = new
                    {
                        type = "string",
                        description = "Filter by priority level",
                        @enum = new[] { "low", "normal", "high", "urgent" }
                    },
                    limit = new
                    {
                        type = "integer",
                        description = "Maximum number of tasks to return",
                        minimum = 1,
                        maximum = 50,
                        @default = 20
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
            var filter = parameters.TryGetProperty("filter", out var filterProp) ? 
                filterProp.GetString() ?? "all" : "all";
            var assignedTo = parameters.TryGetProperty("assignedTo", out var assignedProp) ? 
                assignedProp.GetString() : null;
            var priority = parameters.TryGetProperty("priority", out var priorityProp) ? 
                priorityProp.GetString() : null;
            var limit = parameters.TryGetProperty("limit", out var limitProp) ? 
                limitProp.GetInt32() : 20;

            // Mock tasks data
            var mockTasks = new[]
            {
                new { 
                    Id = "task-001", 
                    Title = "Review project proposal", 
                    AssignedTo = "john@company.com", 
                    Priority = "high", 
                    Status = "pending", 
                    DueDate = DateTime.Now.AddDays(2),
                    CreatedDate = DateTime.Now.AddDays(-3)
                },
                new { 
                    Id = "task-002", 
                    Title = "Update documentation", 
                    AssignedTo = "sarah@company.com", 
                    Priority = "normal", 
                    Status = "completed", 
                    DueDate = DateTime.Now.AddDays(-1),
                    CreatedDate = DateTime.Now.AddDays(-5)
                },
                new { 
                    Id = "task-003", 
                    Title = "Prepare presentation", 
                    AssignedTo = context.CurrentUser?.Mail ?? "current@company.com", 
                    Priority = "urgent", 
                    Status = "overdue", 
                    DueDate = DateTime.Now.AddDays(-2),
                    CreatedDate = DateTime.Now.AddDays(-7)
                }
            };

            // Apply filters
            var filteredTasks = mockTasks.AsEnumerable();

            if (filter != "all")
            {
                filteredTasks = filter switch
                {
                    "pending" => filteredTasks.Where(t => t.Status == "pending"),
                    "completed" => filteredTasks.Where(t => t.Status == "completed"),
                    "overdue" => filteredTasks.Where(t => t.Status == "overdue" || (t.Status == "pending" && t.DueDate < DateTime.Now)),
                    "my-tasks" => filteredTasks.Where(t => t.AssignedTo == context.CurrentUser?.Mail),
                    _ => filteredTasks
                };
            }

            if (!string.IsNullOrEmpty(assignedTo))
            {
                filteredTasks = filteredTasks.Where(t => t.AssignedTo.Contains(assignedTo, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(priority))
            {
                filteredTasks = filteredTasks.Where(t => t.Priority == priority);
            }

            var tasks = filteredTasks.Take(limit).ToArray();

            if (!tasks.Any())
            {
                return CreateSuccessResponse("📝 **No tasks found** matching your criteria.", context);
            }

            var taskList = string.Join("\n\n", tasks.Select((task, i) =>
            {
                var statusEmoji = task.Status switch
                {
                    "completed" => "✅",
                    "overdue" => "🔴",
                    "pending" => "⏳",
                    _ => "📝"
                };

                var priorityEmoji = task.Priority switch
                {
                    "low" => "🟢",
                    "normal" => "🟡",
                    "high" => "🟠",
                    "urgent" => "🔴",
                    _ => "🟡"
                };

                return $"{i + 1}. {statusEmoji} **{task.Title}**\n" +
                       $"   👤 Assigned to: {task.AssignedTo}\n" +
                       $"   {priorityEmoji} Priority: {task.Priority.ToUpperInvariant()}\n" +
                       $"   📅 Due: {task.DueDate:yyyy-MM-dd}\n" +
                       $"   🆔 ID: {task.Id}";
            }));

            var message = $"📋 **Team Tasks** ({tasks.Length} tasks found)\n" +
                         $"🎯 **Filter:** {filter}\n\n{taskList}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list tasks");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to list tasks: {ex.Message}");
        }
    }
}

/// <summary>
/// Start Poll Command - Create polls for team decision making
/// </summary>
public class StartPollCommand : TeamsToolBase
{
    public StartPollCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }    public override string Name => "teams-start-poll";
    public override string Description => "Start a poll in a team channel";
    public override TeamsToolCategory Category => TeamsToolCategory.Polls;
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
                    question = new
                    {
                        type = "string",
                        description = "The poll question",
                        minLength = 1,
                        maxLength = 255
                    },
                    options = new
                    {
                        type = "array",
                        description = "Poll options (2-10 options)",
                        items = new { type = "string", minLength = 1, maxLength = 80 },
                        minItems = 2,
                        maxItems = 10
                    },
                    channelId = new
                    {
                        type = "string",
                        description = "Channel ID where to post the poll"
                    },
                    allowMultipleChoices = new
                    {
                        type = "boolean",
                        description = "Allow users to select multiple options",
                        @default = false
                    },
                    anonymousVoting = new
                    {
                        type = "boolean",
                        description = "Enable anonymous voting",
                        @default = false
                    },
                    durationHours = new
                    {
                        type = "integer",
                        description = "Poll duration in hours (1-168)",
                        minimum = 1,
                        maximum = 168,
                        @default = 24
                    }
                },
                required = new[] { "question", "options", "channelId" },
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
            var question = parameters.GetProperty("question").GetString()!;
            var optionsArray = parameters.GetProperty("options").EnumerateArray()
                .Select(o => o.GetString()!).ToArray();
            var channelId = parameters.GetProperty("channelId").GetString()!;
            var allowMultipleChoices = parameters.TryGetProperty("allowMultipleChoices", out var multiProp) && 
                multiProp.GetBoolean();
            var anonymousVoting = parameters.TryGetProperty("anonymousVoting", out var anonProp) && 
                anonProp.GetBoolean();
            var durationHours = parameters.TryGetProperty("durationHours", out var durationProp) ? 
                durationProp.GetInt32() : 24;

            // Mock poll creation
            var pollId = Guid.NewGuid().ToString();
            var createdDate = DateTime.UtcNow;
            var endDate = createdDate.AddHours(durationHours);

            _logger.LogInformation("Poll started - ID: {PollId}, Question: {Question}, Options: {OptionsCount}, Channel: {ChannelId}",
                pollId, question, optionsArray.Length, channelId);

            var optionsList = string.Join("\n", optionsArray.Select((option, i) => $"   {i + 1}. {option}"));

            var message = $"🗳️ **Poll Started Successfully**\n\n" +
                         $"❓ **Question:** {question}\n\n" +
                         $"📊 **Options:**\n{optionsList}\n\n" +
                         $"📍 **Channel:** {channelId}\n" +
                         $"⏰ **Duration:** {durationHours} hours\n" +
                         $"📅 **Ends:** {endDate:yyyy-MM-dd HH:mm}\n" +
                         $"🔄 **Multiple Choices:** {(allowMultipleChoices ? "Yes" : "No")}\n" +
                         $"🕶️ **Anonymous:** {(anonymousVoting ? "Yes" : "No")}\n" +
                         $"🆔 **Poll ID:** {pollId}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start poll");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to start poll: {ex.Message}");
        }
    }
}

/// <summary>
/// Show Poll Results Command - Display poll results and analytics
/// </summary>
public class ShowPollResultsCommand : TeamsToolBase
{
    public ShowPollResultsCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }    public override string Name => "teams-show-poll-results";
    public override string Description => "Show results for a specific poll";
    public override TeamsToolCategory Category => TeamsToolCategory.Polls;
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
                    pollId = new
                    {
                        type = "string",
                        description = "ID of the poll to show results for"
                    },
                    includeVoterDetails = new
                    {
                        type = "boolean",
                        description = "Include voter names (if not anonymous)",
                        @default = false
                    },
                    includeStatistics = new
                    {
                        type = "boolean",
                        description = "Include detailed statistics",
                        @default = true
                    }
                },
                required = new[] { "pollId" },
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
            var pollId = parameters.GetProperty("pollId").GetString()!;
            var includeVoterDetails = parameters.TryGetProperty("includeVoterDetails", out var voterProp) && 
                voterProp.GetBoolean();
            var includeStatistics = parameters.TryGetProperty("includeStatistics", out var statsProp) && 
                statsProp.GetBoolean();

            // Mock poll results data
            var mockPoll = new
            {
                Id = pollId,
                Question = "Which technology should we use for our next project?",
                Options = new[]
                {
                    new { Text = "React", Votes = 8, Percentage = 40.0 },
                    new { Text = "Vue.js", Votes = 6, Percentage = 30.0 },
                    new { Text = "Angular", Votes = 4, Percentage = 20.0 },
                    new { Text = "Svelte", Votes = 2, Percentage = 10.0 }
                },
                TotalVotes = 20,
                TotalVoters = 18, // Some people voted for multiple options
                IsAnonymous = false,
                IsActive = false,
                CreatedDate = DateTime.Now.AddDays(-2),
                EndDate = DateTime.Now.AddHours(-2)
            };

            var resultsList = string.Join("\n", mockPoll.Options.Select((option, i) =>
            {
                var barLength = (int)(option.Percentage / 5); // 5% per bar segment
                var bar = new string('█', barLength) + new string('░', 20 - barLength);
                return $"   {i + 1}. **{option.Text}**\n" +
                       $"      📊 {bar} {option.Percentage:F1}% ({option.Votes} votes)";
            }));

            var message = $"📊 **Poll Results**\n\n" +
                         $"❓ **Question:** {mockPoll.Question}\n\n" +
                         $"**Results:**\n{resultsList}\n\n" +
                         $"🗳️ **Total Votes:** {mockPoll.TotalVotes}\n" +
                         $"👥 **Total Voters:** {mockPoll.TotalVoters}\n" +
                         $"📅 **Created:** {mockPoll.CreatedDate:yyyy-MM-dd HH:mm}\n" +
                         $"⏰ **Ended:** {mockPoll.EndDate:yyyy-MM-dd HH:mm}\n" +
                         $"🆔 **Poll ID:** {pollId}";

            if (includeStatistics)
            {
                var winningOption = mockPoll.Options.OrderByDescending(o => o.Votes).First();
                message += $"\n\n📈 **Statistics:**\n" +
                          $"🏆 **Winner:** {winningOption.Text} ({winningOption.Percentage:F1}%)\n" +
                          $"📊 **Participation Rate:** {(mockPoll.TotalVoters * 100.0 / 25):F1}% (25 team members)";
            }

            if (includeVoterDetails && !mockPoll.IsAnonymous)
            {
                message += $"\n\n👥 **Top Voters:** john@company.com, sarah@company.com, mike@company.com...";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show poll results");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to show poll results: {ex.Message}");
        }
    }
}
