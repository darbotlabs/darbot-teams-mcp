using Microsoft.Graph;
using DarbotTeamsMcp.Core.Models;

namespace DarbotTeamsMcp.Core.Interfaces;

/// <summary>
/// Wrapper interface for Microsoft Graph API client with Teams-specific extensions.
/// Provides retry policies, rate limiting, and comprehensive error handling.
/// </summary>
public interface ITeamsGraphClient
{
    /// <summary>
    /// Gets the underlying Microsoft Graph service client.
    /// </summary>
    GraphServiceClient GraphServiceClient { get; }    /// <summary>
    /// Gets teams for the current user.
    /// </summary>
    Task<IList<Microsoft.Graph.Models.Team>> GetUserTeamsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets team members with role information.
    /// </summary>
    Task<IList<TeamMemberInfo>> GetTeamMembersAsync(string teamId, bool includeGuests = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets team channels with metadata.
    /// </summary>
    Task<IList<ChannelInfo>> GetTeamChannelsAsync(string teamId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new channel in the specified team.
    /// </summary>
    Task<ChannelInfo> CreateChannelAsync(string teamId, CreateChannelRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a member to the specified team.
    /// </summary>
    Task<TeamMemberInfo> AddTeamMemberAsync(string teamId, AddMemberRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a member from the specified team.
    /// </summary>
    Task RemoveTeamMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current user's presence information.
    /// </summary>
    Task<UserPresence> GetUserPresenceAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches messages in a channel.
    /// </summary>
    Task<IList<ChatMessageInfo>> SearchMessagesAsync(string teamId, string channelId, SearchMessagesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current user has the required permission for the specified team.
    /// </summary>
    Task<bool> CheckUserPermissionAsync(string teamId, TeamsPermissionLevel requiredPermission, CancellationToken cancellationToken = default);
}
