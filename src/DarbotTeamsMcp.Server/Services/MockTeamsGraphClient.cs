using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;
using CoreChatMessageInfo = DarbotTeamsMcp.Core.Models.ChatMessageInfo;

namespace DarbotTeamsMcp.Server.Services;

/// <summary>
/// Mock implementation of ITeamsGraphClient for testing and initial development.
/// Provides realistic sample data without requiring actual Microsoft Graph authentication.
/// </summary>
public class MockTeamsGraphClient : ITeamsGraphClient
{
    private readonly ILogger<MockTeamsGraphClient> _logger;

    public MockTeamsGraphClient(ILogger<MockTeamsGraphClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // This is a mock implementation, so we don't have a real GraphServiceClient
    public GraphServiceClient GraphServiceClient => throw new NotImplementedException("Mock implementation - no real Graph client available");

    public async Task<IList<Microsoft.Graph.Models.Team>> GetUserTeamsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Getting user teams");
        await Task.Delay(100, cancellationToken); // Simulate API delay
        
        return new List<Microsoft.Graph.Models.Team>
        {
            new() { Id = "team-1", DisplayName = "Engineering Team", Description = "Main engineering team" },
            new() { Id = "team-2", DisplayName = "Marketing Team", Description = "Marketing and communications" },
            new() { Id = "team-3", DisplayName = "Sales Team", Description = "Sales organization" }
        };
    }

    public async Task<IList<TeamMemberInfo>> GetTeamMembersAsync(string teamId, bool includeGuests = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Getting team members for {TeamId}, includeGuests: {IncludeGuests}", teamId, includeGuests);
        await Task.Delay(150, cancellationToken);

        var members = new List<TeamMemberInfo>
        {
            new() { UserId = "user-1", DisplayName = "John Doe", Email = "john@contoso.com", Role = TeamsPermissionLevel.Owner, IsGuest = false, JoinedDateTime = DateTimeOffset.Now.AddDays(-30) },
            new() { UserId = "user-2", DisplayName = "Jane Smith", Email = "jane@contoso.com", Role = TeamsPermissionLevel.Member, IsGuest = false, JoinedDateTime = DateTimeOffset.Now.AddDays(-60) },
            new() { UserId = "user-3", DisplayName = "Bob Wilson", Email = "bob@contoso.com", Role = TeamsPermissionLevel.Member, IsGuest = false, JoinedDateTime = DateTimeOffset.Now.AddDays(-45) },
            new() { UserId = "user-4", DisplayName = "Alice Guest", Email = "alice@external.com", Role = TeamsPermissionLevel.Guest, IsGuest = true, JoinedDateTime = DateTimeOffset.Now.AddDays(-10) }
        };

        if (!includeGuests)
        {
            members = members.Where(m => !m.IsGuest).ToList();
        }

        return members;
    }

    public async Task<IList<ChannelInfo>> GetTeamChannelsAsync(string teamId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Getting channels for team {TeamId}", teamId);
        await Task.Delay(100, cancellationToken);

        return new List<ChannelInfo>
        {
            new() { Id = "channel-1", DisplayName = "General", Description = "Default team channel", MembershipType = ChannelMembershipType.Standard, CreatedDateTime = DateTimeOffset.Now.AddDays(-30) },
            new() { Id = "channel-2", DisplayName = "Development", Description = "Development discussions", MembershipType = ChannelMembershipType.Standard, CreatedDateTime = DateTimeOffset.Now.AddDays(-20) },
            new() { Id = "channel-3", DisplayName = "Project Alpha", Description = "Private project channel", MembershipType = ChannelMembershipType.Private, CreatedDateTime = DateTimeOffset.Now.AddDays(-10) }
        };
    }

    public async Task<ChannelInfo> CreateChannelAsync(string teamId, CreateChannelRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Creating channel '{ChannelName}' in team {TeamId}", request.DisplayName, teamId);
        await Task.Delay(200, cancellationToken);

        return new ChannelInfo
        {
            Id = $"channel-{Guid.NewGuid()}",
            DisplayName = request.DisplayName,
            Description = request.Description,
            MembershipType = request.MembershipType == "private" ? ChannelMembershipType.Private : ChannelMembershipType.Standard,
            CreatedDateTime = DateTimeOffset.Now
        };
    }

    public async Task<TeamMemberInfo> AddTeamMemberAsync(string teamId, AddMemberRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Adding member {UserPrincipalName} to team {TeamId} with role {Role}", request.UserPrincipalName, teamId, request.Role);
        await Task.Delay(300, cancellationToken);

        return new TeamMemberInfo
        {
            UserId = $"user-{Guid.NewGuid()}",
            DisplayName = request.UserPrincipalName.Split('@')[0],
            Email = request.UserPrincipalName,
            Role = request.Role,
            IsGuest = request.IsGuest,
            JoinedDateTime = DateTimeOffset.Now
        };
    }

    public async Task RemoveTeamMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Removing member {UserId} from team {TeamId}", userId, teamId);
        await Task.Delay(200, cancellationToken);
    }

    public async Task<UserPresence> GetUserPresenceAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Getting presence for user {UserId}", userId);
        await Task.Delay(50, cancellationToken);

        return new UserPresence
        {
            UserId = userId,
            Status = "Available",
            Availability = "Available",
            Activity = "Available"
        };
    }

    public async Task<IList<CoreChatMessageInfo>> SearchMessagesAsync(string teamId, string channelId, SearchMessagesRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Searching messages in team {TeamId}, channel {ChannelId} for '{Query}'", teamId, channelId, request.Query);
        await Task.Delay(250, cancellationToken);

        return new List<CoreChatMessageInfo>
        {
            new() { Id = "msg-1", Content = $"Found message containing '{request.Query}'", AuthorDisplayName = "John Doe", CreatedDateTime = DateTimeOffset.Now.AddHours(-1) },
            new() { Id = "msg-2", Content = $"Another message with '{request.Query}' in it", AuthorDisplayName = "Jane Smith", CreatedDateTime = DateTimeOffset.Now.AddHours(-2) }
        };
    }

    public async Task<bool> CheckUserPermissionAsync(string teamId, TeamsPermissionLevel requiredPermission, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Checking permission {Permission} for team {TeamId}", requiredPermission, teamId);
        await Task.Delay(50, cancellationToken);

        // Mock implementation: allow most operations for testing
        return requiredPermission switch
        {
            TeamsPermissionLevel.Guest => true,
            TeamsPermissionLevel.Member => true,
            TeamsPermissionLevel.Owner => true, // For testing, allow owner operations
            TeamsPermissionLevel.Organizer => true,
            _ => false
        };
    }
}
