using Microsoft.Graph.Models;

namespace DarbotTeamsMcp.Core.Models;

/// <summary>
/// Represents the current Teams context for the authenticated user.
/// Maintains state for current team, channel, and user information.
/// </summary>
public record TeamsContext
{
    /// <summary>
    /// Current authenticated user information.
    /// </summary>
    public User? CurrentUser { get; init; }

    /// <summary>
    /// Currently selected team ID.
    /// </summary>
    public string? CurrentTeamId { get; init; }

    /// <summary>
    /// Currently selected team information.
    /// </summary>
    public Team? CurrentTeam { get; init; }

    /// <summary>
    /// Currently selected channel ID.
    /// </summary>
    public string? CurrentChannelId { get; init; }

    /// <summary>
    /// Currently selected channel information.
    /// </summary>
    public Channel? CurrentChannel { get; init; }

    /// <summary>
    /// User's tenant ID.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// User's role in the current team.
    /// </summary>
    public TeamsPermissionLevel UserPermissionLevel { get; init; } = TeamsPermissionLevel.Guest;

    /// <summary>
    /// Session correlation ID for tracking operations.
    /// </summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Creates a new context with updated team information.
    /// </summary>
    public TeamsContext WithTeam(string teamId, Team team, TeamsPermissionLevel permissionLevel)
    {
        return this with 
        { 
            CurrentTeamId = teamId, 
            CurrentTeam = team, 
            UserPermissionLevel = permissionLevel,
            CurrentChannelId = null,
            CurrentChannel = null
        };
    }

    /// <summary>
    /// Creates a new context with updated channel information.
    /// </summary>
    public TeamsContext WithChannel(string channelId, Channel channel)
    {
        return this with 
        { 
            CurrentChannelId = channelId, 
            CurrentChannel = channel 
        };
    }
}

/// <summary>
/// Represents permission levels in Teams following the defined command permissions.
/// </summary>
public enum TeamsPermissionLevel
{
    /// <summary>
    /// Guest user with limited permissions.
    /// </summary>
    Guest = 0,

    /// <summary>
    /// Regular team member.
    /// </summary>
    Member = 1,

    /// <summary>
    /// Team owner with full administrative permissions.
    /// </summary>
    Owner = 2,

    /// <summary>
    /// Meeting organizer (for meeting-specific operations).
    /// </summary>
    Organizer = 3
}

/// <summary>
/// Categories for organizing Teams tools.
/// </summary>
public enum TeamsToolCategory
{
    UserManagement,
    ChannelManagement,
    Messaging,
    Files,
    Meetings,
    Tasks,
    Integrations,
    Presence,
    Support,
    Reporting,
    TeamManagement,
    Notifications,
    Polls
}
