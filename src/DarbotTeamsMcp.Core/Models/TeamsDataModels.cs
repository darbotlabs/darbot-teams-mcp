using Microsoft.Graph.Models;

namespace DarbotTeamsMcp.Core.Models;

/// <summary>
/// Extended team member information with role details.
/// </summary>
public record TeamMemberInfo
{
    /// <summary>
    /// Member's user ID.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Member's display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Member's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Member's role in the team.
    /// </summary>
    public TeamsPermissionLevel Role { get; init; }

    /// <summary>
    /// Whether the member is a guest user.
    /// </summary>
    public bool IsGuest { get; init; }

    /// <summary>
    /// When the member joined the team.
    /// </summary>
    public DateTimeOffset? JoinedDateTime { get; init; }

    /// <summary>
    /// User's presence status.
    /// </summary>
    public string? PresenceStatus { get; init; }
}

/// <summary>
/// Extended channel information with metadata.
/// </summary>
public record ChannelInfo
{
    /// <summary>
    /// Channel ID.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Channel display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Channel description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Channel type (standard, private, shared).
    /// </summary>
    public ChannelMembershipType MembershipType { get; init; }

    /// <summary>
    /// When the channel was created.
    /// </summary>
    public DateTimeOffset? CreatedDateTime { get; init; }

    /// <summary>
    /// Channel email address for external communication.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Whether the channel is archived.
    /// </summary>
    public bool IsArchived { get; init; }

    /// <summary>
    /// Number of members in the channel (for private channels).
    /// </summary>
    public int? MemberCount { get; init; }
}

/// <summary>
/// Chat message information for search results.
/// </summary>
public record ChatMessageInfo
{
    /// <summary>
    /// Message ID.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Message content.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Message author information.
    /// </summary>
    public string? AuthorDisplayName { get; init; }

    /// <summary>
    /// Author's user ID.
    /// </summary>
    public string? AuthorUserId { get; init; }

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTimeOffset? CreatedDateTime { get; init; }

    /// <summary>
    /// When the message was last modified.
    /// </summary>
    public DateTimeOffset? LastModifiedDateTime { get; init; }

    /// <summary>
    /// Message importance level.
    /// </summary>
    public ChatMessageImportance? Importance { get; init; }

    /// <summary>
    /// Whether the message is pinned.
    /// </summary>
    public bool IsPinned { get; init; }

    /// <summary>
    /// Message attachments.
    /// </summary>
    public List<string> AttachmentNames { get; init; } = new();
}

/// <summary>
/// User presence information.
/// </summary>
public record UserPresence
{
    /// <summary>
    /// User ID.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// Current presence status.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Availability status.
    /// </summary>
    public string? Availability { get; init; }

    /// <summary>
    /// Activity message.
    /// </summary>
    public string? Activity { get; init; }

    /// <summary>
    /// Out of office message.
    /// </summary>
    public string? OutOfOfficeMessage { get; init; }
}
