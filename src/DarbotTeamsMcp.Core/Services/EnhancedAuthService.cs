using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using DarbotTeamsMcp.Core.Configuration;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;
using System.Diagnostics;

namespace DarbotTeamsMcp.Core.Services;

/// <summary>
/// Enhanced authentication service with credential auto-discovery.
/// Implements interactive device code flow with automatic credential detection.
/// </summary>
public class EnhancedAuthService : ITeamsAuthProvider
{
    private readonly ILogger<EnhancedAuthService> _logger;
    private readonly IMemoryCache _cache;
    private readonly TeamsConfiguration _configuration;
    private readonly CredentialDetectionService _credentialDetection;
    private IPublicClientApplication? _app;
    private const string TokenCacheKey = "teams_auth_result";

    public EnhancedAuthService(
        ILogger<EnhancedAuthService> logger,
        IMemoryCache cache,
        TeamsConfiguration configuration,
        CredentialDetectionService credentialDetection)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _credentialDetection = credentialDetection ?? throw new ArgumentNullException(nameof(credentialDetection));
    }

    /// <inheritdoc/>
    public async Task<Models.AuthenticationResult> AuthenticateInteractiveAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting enhanced authentication with credential detection...");

        try
        {
            // First, try to detect existing credentials
            var credentialSources = await _credentialDetection.DetectCredentialSourcesAsync(cancellationToken);
            var preferredSource = credentialSources.GetPreferredSource();

            if (preferredSource != null)
            {
                _logger.LogInformation("Found existing credentials from {SourceType}", preferredSource.Type);
                
                // Try to use existing credentials
                var existingResult = await TryUseExistingCredentialsAsync(preferredSource, cancellationToken);
                if (existingResult != null)
                {
                    _logger.LogInformation("Successfully authenticated using existing {SourceType} credentials", preferredSource.Type);
                    return existingResult;
                }
            }

            // Fall back to device code flow
            _logger.LogInformation("Falling back to device code authentication flow...");
            return await AuthenticateWithDeviceCodeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(TokenCacheKey, out Models.AuthenticationResult? cachedResult) && 
            cachedResult?.IsValid == true)
        {
            return cachedResult.AccessToken;
        }

        // Re-authenticate if no valid token
        var result = await AuthenticateInteractiveAsync(cancellationToken);
        return result.AccessToken;
    }

    /// <inheritdoc/>
    public async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            
            // Create Graph client and get user info
            var graphClient = CreateGraphServiceClient(accessToken);
            var user = await graphClient.Me.GetAsync(cancellationToken: cancellationToken);
            
            return user ?? throw new InvalidOperationException("Failed to retrieve user information");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user information");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task ClearTokenCacheAsync()
    {
        _logger.LogInformation("Clearing token cache...");
        _cache.Remove(TokenCacheKey);
        
        if (_app != null)
        {
            var accounts = await _app.GetAccountsAsync();
            foreach (var account in accounts)
            {
                await _app.RemoveAsync(account);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAuthenticatedAsync()
    {
        if (_cache.TryGetValue(TokenCacheKey, out Models.AuthenticationResult? cachedResult))
        {
            return cachedResult?.IsValid == true;
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<AuthenticationContext> GetAuthenticationContextAsync()
    {
        try
        {
            if (!await IsAuthenticatedAsync())
            {
                return new AuthenticationContext
                {
                    IsAuthenticated = false,
                    AuthenticationMethod = "None"
                };
            }

            var user = await GetCurrentUserAsync();
            var cachedResult = _cache.Get<Models.AuthenticationResult>(TokenCacheKey);

            return new AuthenticationContext
            {
                IsAuthenticated = true,
                UserPrincipalName = user.UserPrincipalName,
                DisplayName = user.DisplayName,
                TenantId = cachedResult?.TenantId,
                AuthenticationMethod = "Enhanced/AutoDetected",
                AuthenticatedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get authentication context");
            return new AuthenticationContext
            {
                IsAuthenticated = false,
                AuthenticationMethod = "Error"
            };
        }
    }

    /// <summary>
    /// Attempts to use existing credentials from detected sources.
    /// </summary>
    private async Task<Models.AuthenticationResult?> TryUseExistingCredentialsAsync(
        CredentialSource credentialSource, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (credentialSource.Type)
            {
                case CredentialSourceType.AzureCli:
                    return await TryAzureCliTokenAsync(cancellationToken);
                
                case CredentialSourceType.VsCodeMicrosoft:
                    _logger.LogInformation("VS Code credential integration not yet implemented, falling back to device code");
                    return null;
                
                case CredentialSourceType.WindowsCredentialManager:
                    _logger.LogInformation("Windows Credential Manager integration not yet implemented, falling back to device code");
                    return null;
                
                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to use existing credentials from {SourceType}", credentialSource.Type);
            return null;
        }
    }

    /// <summary>
    /// Attempts to get a token using Azure CLI.
    /// </summary>
    private async Task<Models.AuthenticationResult?> TryAzureCliTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Attempting to get token from Azure CLI...");

            var process = new System.Diagnostics.Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "az" : "az",
                    Arguments = $"account get-access-token --resource https://graph.microsoft.com --output json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorOutput = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Azure CLI token request failed: {Error}", errorOutput);
                return null;
            }

            var tokenInfo = System.Text.Json.JsonSerializer.Deserialize<AzureCliTokenInfo>(output);
            if (tokenInfo?.AccessToken == null)
            {
                _logger.LogWarning("Azure CLI returned invalid token response");
                return null;
            }

            var result = new Models.AuthenticationResult
            {
                AccessToken = tokenInfo.AccessToken,
                ExpiresOn = DateTimeOffset.Parse(tokenInfo.ExpiresOn ?? DateTimeOffset.UtcNow.AddHours(1).ToString()),
                TenantId = tokenInfo.Tenant ?? _configuration.TenantId,
                Scopes = _configuration.Scopes.ToList()
            };

            // Cache the result
            _cache.Set(TokenCacheKey, result, result.ExpiresOn.AddMinutes(-5));
            
            _logger.LogInformation("Successfully obtained token from Azure CLI");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get token from Azure CLI");
            return null;
        }
    }

    /// <summary>
    /// Performs device code authentication flow.
    /// </summary>
    private async Task<Models.AuthenticationResult> AuthenticateWithDeviceCodeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting device code authentication flow...");

        _app = PublicClientApplicationBuilder
            .Create(_configuration.ClientId)
            .WithAuthority($"https://login.microsoftonline.com/{_configuration.TenantId}")
            .WithRedirectUri(_configuration.RedirectUri)
            .Build();

        try
        {
            var deviceCodeResult = await _app.AcquireTokenWithDeviceCode(
                _configuration.Scopes,
                deviceCodeCallback =>
                {
                    _logger.LogInformation("Device Code Authentication Required:");
                    _logger.LogInformation("1. Open a web browser and navigate to: {Url}", deviceCodeCallback.VerificationUrl);
                    _logger.LogInformation("2. Enter the code: {Code}", deviceCodeCallback.UserCode);
                    _logger.LogInformation("3. Sign in with your Microsoft account");
                    _logger.LogInformation("Waiting for authentication to complete...");
                    
                    Console.WriteLine();
                    Console.WriteLine("🔐 Microsoft Authentication Required");
                    Console.WriteLine("═══════════════════════════════════════");
                    Console.WriteLine($"1. Open: {deviceCodeCallback.VerificationUrl}");
                    Console.WriteLine($"2. Enter code: {deviceCodeCallback.UserCode}");
                    Console.WriteLine("3. Sign in with your Microsoft account");
                    Console.WriteLine();
                    Console.WriteLine("Waiting for authentication...");
                    
                    return Task.FromResult(0);
                })
                .ExecuteAsync(cancellationToken);

            var result = new Models.AuthenticationResult
            {
                AccessToken = deviceCodeResult.AccessToken,
                RefreshToken = deviceCodeResult.AccessToken, // MSAL handles refresh automatically
                ExpiresOn = deviceCodeResult.ExpiresOn,
                TenantId = deviceCodeResult.TenantId ?? _configuration.TenantId,
                Scopes = deviceCodeResult.Scopes.ToList()
            };

            // Cache the result
            _cache.Set(TokenCacheKey, result, result.ExpiresOn.AddMinutes(-5));
            
            _logger.LogInformation("Device code authentication completed successfully");
            Console.WriteLine("✅ Authentication successful!");
            Console.WriteLine();
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device code authentication failed");
            Console.WriteLine("❌ Authentication failed. Please try again.");
            Console.WriteLine();
            throw;
        }
    }

    /// <summary>
    /// Creates a Graph service client with the given access token.
    /// </summary>
    private GraphServiceClient CreateGraphServiceClient(string accessToken)
    {
        // Create a simple HTTP client with authorization header
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        return new GraphServiceClient(httpClient);
    }
}

/// <summary>
/// Azure CLI token information.
/// </summary>
internal record AzureCliTokenInfo
{
    public string? AccessToken { get; set; }
    public string? ExpiresOn { get; set; }
    public string? Subscription { get; set; }
    public string? Tenant { get; set; }
    public string? TokenType { get; set; }
}