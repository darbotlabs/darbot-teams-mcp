namespace DarbotTeamsMcp.Core.Models;

/// <summary>
/// Represents validation result for tool parameters.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Whether the validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors if any.
    /// </summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    public static ValidationResult Failure(params string[] errors) => new() 
    { 
        IsValid = false, 
        Errors = errors.ToList() 
    };

    /// <summary>
    /// Creates a failed validation result with error list.
    /// </summary>
    public static ValidationResult Failure(IEnumerable<string> errors) => new() 
    { 
        IsValid = false, 
        Errors = errors.ToList() 
    };
}

/// <summary>
/// Authentication result containing token information.
/// </summary>
public record AuthenticationResult
{
    /// <summary>
    /// Access token for Graph API calls.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Refresh token for token renewal.
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Token expiration time.
    /// </summary>
    public DateTimeOffset ExpiresOn { get; init; }

    /// <summary>
    /// User's tenant ID.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Token scopes granted.
    /// </summary>
    public List<string> Scopes { get; init; } = new();

    /// <summary>
    /// Whether the token is still valid.
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(AccessToken) && ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5);
}

/// <summary>
/// Authentication context information.
/// </summary>
public record AuthenticationContext
{
    /// <summary>
    /// Whether user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>
    /// Current user's UPN/email.
    /// </summary>
    public string? UserPrincipalName { get; init; }

    /// <summary>
    /// User's display name.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// User's tenant ID.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Authentication method used.
    /// </summary>
    public string? AuthenticationMethod { get; init; }

    /// <summary>
    /// When the authentication occurred.
    /// </summary>
    public DateTimeOffset? AuthenticatedAt { get; init; }
}
