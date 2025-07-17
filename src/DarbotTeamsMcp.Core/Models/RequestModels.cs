namespace DarbotTeamsMcp.Core.Models;

/// <summary>
/// Request model for creating a new channel.
/// </summary>
public record CreateChannelRequest
{
    /// <summary>
    /// Channel display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Channel description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Channel membership type.
    /// </summary>
    public string MembershipType { get; init; } = "standard";

    /// <summary>
    /// Whether to include all team members.
    /// </summary>
    public bool IncludeAllMembers { get; init; } = true;
}

/// <summary>
/// Request model for adding a team member.
/// </summary>
public record AddMemberRequest
{
    /// <summary>
    /// User email or UPN to add.
    /// </summary>
    public string UserPrincipalName { get; init; } = string.Empty;

    /// <summary>
    /// Role to assign to the user.
    /// </summary>
    public TeamsPermissionLevel Role { get; init; } = TeamsPermissionLevel.Member;

    /// <summary>
    /// Whether this is a guest invitation.
    /// </summary>
    public bool IsGuest { get; init; } = false;

    /// <summary>
    /// Welcome message for the invitation.
    /// </summary>
    public string? WelcomeMessage { get; init; }
}

/// <summary>
/// Request model for searching messages.
/// </summary>
public record SearchMessagesRequest
{
    /// <summary>
    /// Search query text.
    /// </summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>
    /// Start date for search range.
    /// </summary>
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>
    /// End date for search range.
    /// </summary>
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>
    /// Author filter.
    /// </summary>
    public string? AuthorFilter { get; init; }

    /// <summary>
    /// Maximum number of results.
    /// </summary>
    public int MaxResults { get; init; } = 50;

    /// <summary>
    /// Whether to include attachments in search.
    /// </summary>
    public bool IncludeAttachments { get; init; } = false;
}
