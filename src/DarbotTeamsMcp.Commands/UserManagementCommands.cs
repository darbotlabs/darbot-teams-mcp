using System.Text.Json;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;
using DarbotTeamsMcp.Core.Services;

namespace DarbotTeamsMcp.Commands.UserManagement;

/// <summary>
/// List Team Members Command - Core user management functionality
/// </summary>
public class ListTeamMembersCommand : TeamsToolBase
{
    public ListTeamMembersCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-list-members";
    public override string Description => "List all members of the current team with roles and status";
    public override TeamsToolCategory Category => TeamsToolCategory.UserManagement;
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
                    includeGuests = new
                    {
                        type = "boolean",
                        description = "Include guest users in the results",
                        @default = false
                    },
                    pageSize = new
                    {
                        type = "integer",
                        description = "Number of members to return (max 100)",
                        @default = 50,
                        minimum = 1,
                        maximum = 100
                    }
                },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);        }
    }

    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters, 
        TeamsContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse parameters
            var includeGuests = parameters.TryGetProperty("includeGuests", out var guestsProp) && guestsProp.GetBoolean();
            var pageSize = parameters.TryGetProperty("pageSize", out var sizeProp) ? 
                Math.Min(sizeProp.GetInt32(), 100) : 50;

            // Mock implementation for now - will be replaced with real Graph API calls
            var mockMembers = new[]
            {
                new { name = "John Doe", role = "Owner", email = "john@company.com", status = "Available" },
                new { name = "Jane Smith", role = "Member", email = "jane@company.com", status = "Busy" },
                new { name = "Bob Wilson", role = "Member", email = "bob@company.com", status = "Away" },
                new { name = "Alice Guest", role = "Guest", email = "alice@external.com", status = "Available" }
            };

            var filteredMembers = includeGuests ? 
                mockMembers : 
                mockMembers.Where(m => m.role != "Guest").ToArray();

            var limitedMembers = filteredMembers.Take(pageSize).ToList();

            var membersList = string.Join("\n", 
                limitedMembers.Select((m, i) => $"{i + 1}. {GetRoleIcon(m.role)} **{m.name}** ({m.role})\n   📧 {m.email} - {GetStatusIcon(m.status)} {m.status}"));            var message = $"📋 **Team Members** ({limitedMembers.Count} of {filteredMembers.Length})\n\n{membersList}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list team members");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to list members: {ex.Message}");
        }
    }

    private static string GetRoleIcon(string role) => role switch
    {
        "Owner" => "👑",
        "Member" => "👤",
        "Guest" => "🎭",
        _ => "❓"
    };

    private static string GetStatusIcon(string status) => status switch
    {
        "Available" => "🟢",
        "Busy" => "🔴",
        "Away" => "🟡",
        "DoNotDisturb" => "⛔",
        _ => "⚪"
    };
}

/// <summary>
/// Add Member Command - Add users to the team
/// </summary>
public class AddMemberCommand : TeamsToolBase
{
    public AddMemberCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-add-member";
    public override string Description => "Add a new member to the current team";
    public override TeamsToolCategory Category => TeamsToolCategory.UserManagement;
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
                    userEmail = new
                    {
                        type = "string",
                        description = "Email address of the user to add",
                        format = "email"
                    },                    role = new
                    {
                        type = "string",
                        description = "Role to assign to the user",
                        @enum = new[] { "member", "owner" },
                        @default = "member"
                    }
                },
                required = new[] { "userEmail" },
                additionalProperties = false
            };

            return JsonSerializer.SerializeToElement(schema);
        }
    }    protected override async Task<McpResponse> ExecuteToolAsync(
        JsonElement parameters, 
        TeamsContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userEmail = parameters.GetProperty("userEmail").GetString()!;
            var role = parameters.TryGetProperty("role", out var roleProp) ? 
                roleProp.GetString() ?? "member" : "member";

            // Mock implementation
            _logger.LogInformation("Would add user {Email} with role {Role} to team {TeamId}", 
                userEmail, role, context.CurrentTeamId);            var teamName = context.CurrentTeam?.DisplayName ?? "Current Team";
            var message = $"✅ **User Added Successfully**\n\n👤 **User:** {userEmail}\n🏷️ **Role:** {role}\n🎯 **Team:** {teamName}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add team member");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to add member: {ex.Message}");
        }
    }
}

/// <summary>
/// Remove Member Command - User removal with confirmation and audit
/// </summary>
public class RemoveMemberCommand : TeamsToolBase
{
    public RemoveMemberCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-remove-member";
    public override string Description => "Remove a member from the current team";
    public override TeamsToolCategory Category => TeamsToolCategory.UserManagement;
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
                    userEmail = new
                    {
                        type = "string",
                        description = "Email address of the user to remove",
                        format = "email"
                    },
                    userId = new
                    {
                        type = "string",
                        description = "Alternative: User ID instead of email"
                    },
                    reason = new
                    {
                        type = "string",
                        description = "Reason for removal (for audit log)",
                        maxLength = 500
                    },
                    transferOwnership = new
                    {
                        type = "boolean",
                        description = "Transfer ownership if removing an owner",
                        @default = false
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
            var userEmail = parameters.TryGetProperty("userEmail", out var emailProp) ? 
                emailProp.GetString() : null;
            var userId = parameters.TryGetProperty("userId", out var idProp) ? 
                idProp.GetString() : null;
            var reason = parameters.TryGetProperty("reason", out var reasonProp) ? 
                reasonProp.GetString() : "Administrative removal";

            if (string.IsNullOrEmpty(userEmail) && string.IsNullOrEmpty(userId))
            {
                return CreateErrorResponse(context.CorrelationId, 400, 
                    "❌ Either userEmail or userId must be provided");
            }

            var userIdentifier = userEmail ?? userId!;
            
            // Mock implementation
            _logger.LogInformation("Would remove user {User} from team {TeamId} with reason: {Reason}", 
                userIdentifier, context.CurrentTeamId, reason);

            var teamName = context.CurrentTeam?.DisplayName ?? "Current Team";
            var message = $"🗑️ **User Removed Successfully**\n\n" +
                         $"👤 **User:** {userIdentifier}\n" +
                         $"📝 **Reason:** {reason}\n" +
                         $"🎯 **Team:** {teamName}\n" +
                         $"👤 **Removed By:** Current User\n" +
                         $"📅 **Removed At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove team member");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to remove member: {ex.Message}");
        }
    }
}

/// <summary>
/// List Owners Command - Team owners with contact information
/// </summary>
public class ListOwnersCommand : TeamsToolBase
{
    public ListOwnersCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-list-owners";
    public override string Description => "List all team owners with contact information";
    public override TeamsToolCategory Category => TeamsToolCategory.UserManagement;
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
                    includeContactInfo = new
                    {
                        type = "boolean",
                        description = "Include detailed contact information",
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
            var includeContactInfo = parameters.TryGetProperty("includeContactInfo", out var contactProp) ? 
                contactProp.GetBoolean() : true;

            // Mock owners data
            var mockOwners = new[]
            {
                new { name = "John Doe", email = "john@company.com", phone = "+1-555-0123", status = "Available", joinedDate = "2023-01-15" },
                new { name = "Sarah Wilson", email = "sarah@company.com", phone = "+1-555-0456", status = "Busy", joinedDate = "2023-02-20" }
            };

            var ownersList = string.Join("\n\n", mockOwners.Select((owner, i) =>
            {
                var basic = $"{i + 1}. 👑 **{owner.name}**\n   📧 {owner.email}\n   {GetStatusIcon(owner.status)} {owner.status}";
                
                if (includeContactInfo)
                {
                    basic += $"\n   📞 {owner.phone}\n   📅 Owner since: {owner.joinedDate}";
                }
                
                return basic;
            }));

            var teamName = context.CurrentTeam?.DisplayName ?? "Current Team";
            var message = $"👑 **Team Owners** ({mockOwners.Length} owners)\n\n" +
                         $"🎯 **Team:** {teamName}\n\n{ownersList}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list team owners");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to list owners: {ex.Message}");
        }
    }

    private static string GetStatusIcon(string status) => status switch
    {
        "Available" => "🟢",
        "Busy" => "🔴",
        "Away" => "🟡",
        "DoNotDisturb" => "⛔",
        _ => "⚪"
    };
}

/// <summary>
/// List Guests Command - External users with access levels
/// </summary>
public class ListGuestsCommand : TeamsToolBase
{
    public ListGuestsCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-list-guests";
    public override string Description => "List all guest users with their access levels";
    public override TeamsToolCategory Category => TeamsToolCategory.UserManagement;
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
                    includeAccessDetails = new
                    {
                        type = "boolean",
                        description = "Include detailed access information",
                        @default = true
                    },
                    sortBy = new
                    {
                        type = "string",
                        description = "Sort guests by criteria",
                        @enum = new[] { "name", "email", "joinDate", "lastActivity" },
                        @default = "name"
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
            var includeAccessDetails = parameters.TryGetProperty("includeAccessDetails", out var accessProp) ? 
                accessProp.GetBoolean() : true;
            var sortBy = parameters.TryGetProperty("sortBy", out var sortProp) ? 
                sortProp.GetString() ?? "name" : "name";

            // Mock guest data
            var mockGuests = new[]
            {
                new { name = "Alice External", email = "alice@external.com", company = "Partner Corp", 
                      joinedDate = "2024-01-10", lastActivity = "2024-01-15", channels = new[] { "General", "Project Alpha" } },
                new { name = "Bob Contractor", email = "bob@contractor.com", company = "Freelance", 
                      joinedDate = "2024-01-20", lastActivity = "2024-01-14", channels = new[] { "Development" } }
            };

            var guestsList = string.Join("\n\n", mockGuests.Select((guest, i) =>
            {
                var basic = $"{i + 1}. 🎭 **{guest.name}**\n   📧 {guest.email}\n   🏢 {guest.company}";
                
                if (includeAccessDetails)
                {
                    basic += $"\n   📅 Joined: {guest.joinedDate}\n   🕒 Last Active: {guest.lastActivity}\n   📁 Channels: {string.Join(", ", guest.channels)}";
                }
                
                return basic;
            }));

            var teamName = context.CurrentTeam?.DisplayName ?? "Current Team";
            var message = $"🎭 **Guest Users** ({mockGuests.Length} guests)\n\n" +
                         $"🎯 **Team:** {teamName}\n" +
                         $"📊 **Sorted By:** {sortBy}\n\n{guestsList}";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list guest users");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to list guests: {ex.Message}");
        }
    }
}

/// <summary>
/// Invite Guest Command - External invitation with permissions
/// </summary>
public class InviteGuestCommand : TeamsToolBase
{
    public InviteGuestCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-invite-guest";
    public override string Description => "Invite an external user as a guest to the team";
    public override TeamsToolCategory Category => TeamsToolCategory.UserManagement;
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
                    guestEmail = new
                    {
                        type = "string",
                        description = "Email address of the guest to invite",
                        format = "email"
                    },
                    displayName = new
                    {
                        type = "string",
                        description = "Display name for the guest",
                        maxLength = 64
                    },
                    welcomeMessage = new
                    {
                        type = "string",
                        description = "Custom welcome message",
                        maxLength = 1000
                    },
                    channelAccess = new
                    {
                        type = "array",
                        description = "Specific channels to grant access to",
                        items = new { type = "string" }
                    }
                },
                required = new[] { "guestEmail" },
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
            var guestEmail = parameters.GetProperty("guestEmail").GetString()!;
            var displayName = parameters.TryGetProperty("displayName", out var nameProp) ? 
                nameProp.GetString() : null;
            var welcomeMessage = parameters.TryGetProperty("welcomeMessage", out var welcomeProp) ? 
                welcomeProp.GetString() : "Welcome to our team!";

            var channelAccess = new List<string>();
            if (parameters.TryGetProperty("channelAccess", out var channelsProp) && 
                channelsProp.ValueKind == JsonValueKind.Array)
            {
                channelAccess.AddRange(channelsProp.EnumerateArray().Select(c => c.GetString()!));
            }

            // Mock implementation
            _logger.LogInformation("Would invite guest {Email} to team {TeamId}", guestEmail, context.CurrentTeamId);

            var teamName = context.CurrentTeam?.DisplayName ?? "Current Team";
            var message = $"📨 **Guest Invitation Sent**\n\n" +
                         $"🎭 **Guest:** {guestEmail}\n" +
                         $"📛 **Display Name:** {displayName ?? "Not specified"}\n" +
                         $"🎯 **Team:** {teamName}\n" +
                         $"💬 **Welcome Message:** {welcomeMessage}\n" +
                         $"👤 **Invited By:** Current User\n" +
                         $"📅 **Sent At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";

            if (channelAccess.Any())
            {
                message += $"\n📁 **Channel Access:** {string.Join(", ", channelAccess)}";
            }

            message += "\n\n✉️ **Note:** The guest will receive an email invitation with instructions to join.";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invite guest");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to invite guest: {ex.Message}");
        }
    }
}

/// <summary>
/// Remove Guest Command - Guest removal with access cleanup
/// </summary>
public class RemoveGuestCommand : TeamsToolBase
{
    public RemoveGuestCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-remove-guest";
    public override string Description => "Remove a guest user and clean up their access";
    public override TeamsToolCategory Category => TeamsToolCategory.UserManagement;
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
                    guestEmail = new
                    {
                        type = "string",
                        description = "Email address of the guest to remove",
                        format = "email"
                    },
                    reason = new
                    {
                        type = "string",
                        description = "Reason for removal",
                        maxLength = 500
                    },
                    revokeSharePointAccess = new
                    {
                        type = "boolean",
                        description = "Also revoke SharePoint file access",
                        @default = true
                    }
                },
                required = new[] { "guestEmail" },
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
            var guestEmail = parameters.GetProperty("guestEmail").GetString()!;
            var reason = parameters.TryGetProperty("reason", out var reasonProp) ? 
                reasonProp.GetString() : "Administrative removal";
            var revokeSharePoint = parameters.TryGetProperty("revokeSharePointAccess", out var revokeProp) ? 
                revokeProp.GetBoolean() : true;

            // Mock implementation
            _logger.LogInformation("Would remove guest {Email} from team {TeamId}", guestEmail, context.CurrentTeamId);

            var teamName = context.CurrentTeam?.DisplayName ?? "Current Team";
            var message = $"🎭 **Guest Removed Successfully**\n\n" +
                         $"👤 **Guest:** {guestEmail}\n" +
                         $"📝 **Reason:** {reason}\n" +
                         $"🎯 **Team:** {teamName}\n" +
                         $"🗂️ **SharePoint Access:** {(revokeSharePoint ? "Revoked" : "Preserved")}\n" +
                         $"👤 **Removed By:** Current User\n" +
                         $"📅 **Removed At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
                         $"✅ **Access Cleanup:** All team channels and resources";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove guest");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to remove guest: {ex.Message}");
        }
    }
}

/// <summary>
/// Promote to Owner Command - Role elevation with validation
/// </summary>
public class PromoteToOwnerCommand : TeamsToolBase
{
    public PromoteToOwnerCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-promote-to-owner";
    public override string Description => "Promote a team member to owner role";
    public override TeamsToolCategory Category => TeamsToolCategory.UserManagement;
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
                    userEmail = new
                    {
                        type = "string",
                        description = "Email address of the user to promote",
                        format = "email"
                    },
                    notifyUser = new
                    {
                        type = "boolean",
                        description = "Send notification to the promoted user",
                        @default = true
                    },
                    reason = new
                    {
                        type = "string",
                        description = "Reason for promotion (for audit log)",
                        maxLength = 500
                    }
                },
                required = new[] { "userEmail" },
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
            var userEmail = parameters.GetProperty("userEmail").GetString()!;
            var notifyUser = parameters.TryGetProperty("notifyUser", out var notifyProp) ? 
                notifyProp.GetBoolean() : true;
            var reason = parameters.TryGetProperty("reason", out var reasonProp) ? 
                reasonProp.GetString() : "Role promotion";

            // Mock implementation
            _logger.LogInformation("Would promote user {Email} to owner in team {TeamId}", 
                userEmail, context.CurrentTeamId);

            var teamName = context.CurrentTeam?.DisplayName ?? "Current Team";
            var message = $"👑 **User Promoted to Owner**\n\n" +
                         $"👤 **User:** {userEmail}\n" +
                         $"🎯 **Team:** {teamName}\n" +
                         $"📝 **Reason:** {reason}\n" +
                         $"🔔 **Notification Sent:** {(notifyUser ? "Yes" : "No")}\n" +
                         $"👤 **Promoted By:** Current User\n" +
                         $"📅 **Promoted At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
                         $"✅ **New Permissions:** Full team administration access";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to promote user to owner");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to promote user: {ex.Message}");
        }
    }
}

/// <summary>
/// Demote from Owner Command - Role reduction with safeguards
/// </summary>
public class DemoteFromOwnerCommand : TeamsToolBase
{
    public DemoteFromOwnerCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-demote-from-owner";
    public override string Description => "Demote an owner to regular member role";
    public override TeamsToolCategory Category => TeamsToolCategory.UserManagement;
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
                    userEmail = new
                    {
                        type = "string",
                        description = "Email address of the owner to demote",
                        format = "email"
                    },
                    reason = new
                    {
                        type = "string",
                        description = "Reason for demotion (for audit log)",
                        maxLength = 500
                    },
                    confirmLastOwner = new
                    {
                        type = "boolean",
                        description = "Confirm if demoting the last owner",
                        @default = false
                    }
                },
                required = new[] { "userEmail" },
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
            var userEmail = parameters.GetProperty("userEmail").GetString()!;
            var reason = parameters.TryGetProperty("reason", out var reasonProp) ? 
                reasonProp.GetString() : "Role change";
            var confirmLastOwner = parameters.TryGetProperty("confirmLastOwner", out var confirmProp) && 
                confirmProp.GetBoolean();

            // Mock implementation with safeguard check
            var remainingOwners = 2; // Mock count
            if (remainingOwners <= 1 && !confirmLastOwner)
            {
                return CreateErrorResponse(context.CorrelationId, 400,
                    "❌ Cannot demote the last owner. Use 'confirmLastOwner: true' to override this safeguard.");
            }

            _logger.LogInformation("Would demote owner {Email} in team {TeamId}", userEmail, context.CurrentTeamId);

            var teamName = context.CurrentTeam?.DisplayName ?? "Current Team";
            var message = $"📉 **Owner Demoted to Member**\n\n" +
                         $"👤 **User:** {userEmail}\n" +
                         $"🎯 **Team:** {teamName}\n" +
                         $"📝 **Reason:** {reason}\n" +
                         $"👤 **Demoted By:** Current User\n" +
                         $"📅 **Demoted At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
                         $"⚠️ **Note:** User retains member access but loses administrative privileges.";

            if (remainingOwners <= 1)
            {
                message += "\n🚨 **Warning:** This was the last owner. Ensure proper team management!";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to demote owner");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to demote owner: {ex.Message}");
        }
    }
}

/// <summary>
/// Mute User Command - Temporary user restrictions
/// </summary>
public class MuteUserCommand : TeamsToolBase
{
    public MuteUserCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-mute-user";
    public override string Description => "Temporarily restrict a user's posting abilities";
    public override TeamsToolCategory Category => TeamsToolCategory.UserManagement;
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
                    userEmail = new
                    {
                        type = "string",
                        description = "Email address of the user to mute",
                        format = "email"
                    },
                    duration = new
                    {
                        type = "string",
                        description = "Mute duration",
                        @enum = new[] { "1hour", "1day", "1week", "permanent" },
                        @default = "1day"
                    },
                    reason = new
                    {
                        type = "string",
                        description = "Reason for muting",
                        maxLength = 500
                    },
                    channels = new
                    {
                        type = "array",
                        description = "Specific channels to mute in (all channels if not specified)",
                        items = new { type = "string" }
                    }
                },
                required = new[] { "userEmail" },
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
            var userEmail = parameters.GetProperty("userEmail").GetString()!;
            var duration = parameters.TryGetProperty("duration", out var durationProp) ? 
                durationProp.GetString() ?? "1day" : "1day";
            var reason = parameters.TryGetProperty("reason", out var reasonProp) ? 
                reasonProp.GetString() : "Moderation action";

            var channels = new List<string>();
            if (parameters.TryGetProperty("channels", out var channelsProp) && 
                channelsProp.ValueKind == JsonValueKind.Array)
            {
                channels.AddRange(channelsProp.EnumerateArray().Select(c => c.GetString()!));
            }

            // Mock implementation
            _logger.LogInformation("Would mute user {Email} for {Duration} in team {TeamId}", 
                userEmail, duration, context.CurrentTeamId);

            var expiryTime = duration switch
            {
                "1hour" => DateTime.UtcNow.AddHours(1),
                "1day" => DateTime.UtcNow.AddDays(1),
                "1week" => DateTime.UtcNow.AddDays(7),
                "permanent" => DateTime.MaxValue,
                _ => DateTime.UtcNow.AddDays(1)
            };

            var teamName = context.CurrentTeam?.DisplayName ?? "Current Team";
            var message = $"🔇 **User Muted**\n\n" +
                         $"👤 **User:** {userEmail}\n" +
                         $"🎯 **Team:** {teamName}\n" +
                         $"⏰ **Duration:** {duration}\n" +
                         $"📝 **Reason:** {reason}\n" +
                         $"👤 **Muted By:** Current User\n" +
                         $"📅 **Muted At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";

            if (expiryTime != DateTime.MaxValue)
            {
                message += $"\n⏳ **Expires At:** {expiryTime:yyyy-MM-dd HH:mm} UTC";
            }

            if (channels.Any())
            {
                message += $"\n📁 **Affected Channels:** {string.Join(", ", channels)}";
            }
            else
            {
                message += "\n📁 **Scope:** All team channels";
            }

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mute user");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to mute user: {ex.Message}");
        }
    }
}

/// <summary>
/// Unmute User Command - Restore user permissions
/// </summary>
public class UnmuteUserCommand : TeamsToolBase
{
    public UnmuteUserCommand(ILogger<TeamsToolBase> logger, ITeamsGraphClient graphClient)
        : base(logger, graphClient)
    {
    }

    public override string Name => "teams-unmute-user";
    public override string Description => "Restore a user's posting abilities";
    public override TeamsToolCategory Category => TeamsToolCategory.UserManagement;
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
                    userEmail = new
                    {
                        type = "string",
                        description = "Email address of the user to unmute",
                        format = "email"
                    },
                    reason = new
                    {
                        type = "string",
                        description = "Reason for unmuting",
                        maxLength = 500
                    }
                },
                required = new[] { "userEmail" },
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
            var userEmail = parameters.GetProperty("userEmail").GetString()!;
            var reason = parameters.TryGetProperty("reason", out var reasonProp) ? 
                reasonProp.GetString() : "Moderation lifted";

            // Mock implementation
            _logger.LogInformation("Would unmute user {Email} in team {TeamId}", userEmail, context.CurrentTeamId);

            var teamName = context.CurrentTeam?.DisplayName ?? "Current Team";
            var message = $"🔊 **User Unmuted**\n\n" +
                         $"👤 **User:** {userEmail}\n" +
                         $"🎯 **Team:** {teamName}\n" +
                         $"📝 **Reason:** {reason}\n" +
                         $"👤 **Unmuted By:** Current User\n" +
                         $"📅 **Unmuted At:** {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC\n\n" +
                         $"✅ **Note:** User can now post in all team channels.";

            return CreateSuccessResponse(message, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unmute user");
            return CreateErrorResponse(context.CorrelationId, 500, $"❌ Failed to unmute user: {ex.Message}");
        }
    }
}
