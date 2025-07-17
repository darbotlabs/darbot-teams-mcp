using Microsoft.Graph.Models;
using DarbotTeamsMcp.Core.Models;

namespace DarbotTeamsMcp.Core.Interfaces;

/// <summary>
/// Provides authentication services for Teams Graph API access.
/// Implements interactive device code flow similar to Azure CLI.
/// </summary>
public interface ITeamsAuthProvider
{
    /// <summary>
    /// Performs interactive authentication using device code flow.
    /// </summary>
    Task<AuthenticationResult> AuthenticateInteractiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a valid access token, refreshing if necessary.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current authenticated user information.
    /// </summary>
    Task<User> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the cached authentication tokens.
    /// </summary>
    Task ClearTokenCacheAsync();

    /// <summary>
    /// Checks if the user is currently authenticated.
    /// </summary>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// Gets the current authentication context.
    /// </summary>
    Task<AuthenticationContext> GetAuthenticationContextAsync();
}
