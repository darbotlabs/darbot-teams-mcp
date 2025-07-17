using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Configuration;
using DarbotTeamsMcp.Core.Models;

namespace DarbotTeamsMcp.Core.Services;

/// <summary>
/// Detects existing Microsoft credentials from various sources.
/// </summary>
public class CredentialDetectionService
{
    private readonly ILogger<CredentialDetectionService> _logger;

    public CredentialDetectionService(ILogger<CredentialDetectionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Detects available credential sources.
    /// </summary>
    public async Task<CredentialSourceResult> DetectCredentialSourcesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting credential source detection...");
        
        var result = new CredentialSourceResult();
        
        // Check Azure CLI
        result.AzureCli = await DetectAzureCliCredentialsAsync(cancellationToken);
        
        // Check VS Code Microsoft extension
        result.VsCodeMicrosoft = await DetectVsCodeMicrosoftCredentialsAsync(cancellationToken);
        
        // Check Windows Credential Manager (Windows only)
        if (OperatingSystem.IsWindows())
        {
            result.WindowsCredentialManager = await DetectWindowsCredentialManagerAsync(cancellationToken);
        }
        
        _logger.LogInformation("Credential detection completed. Azure CLI: {AzureCli}, VS Code: {VsCode}, Windows CM: {WindowsCM}",
            result.AzureCli?.IsAvailable ?? false,
            result.VsCodeMicrosoft?.IsAvailable ?? false, 
            result.WindowsCredentialManager?.IsAvailable ?? false);
            
        return result;
    }

    /// <summary>
    /// Detects Azure CLI credentials and account information.
    /// </summary>
    private async Task<CredentialSource?> DetectAzureCliCredentialsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Checking for Azure CLI credentials...");
            
            // Check if Azure CLI is installed
            var azPath = await FindAzureCliPathAsync(cancellationToken);
            if (string.IsNullOrEmpty(azPath))
            {
                _logger.LogDebug("Azure CLI not found in PATH");
                return null;
            }

            // Try to get current account
            var accountInfo = await GetAzureCliAccountAsync(azPath, cancellationToken);
            if (accountInfo == null)
            {
                _logger.LogDebug("No Azure CLI account found or not logged in");
                return null;
            }

            return new CredentialSource
            {
                Type = CredentialSourceType.AzureCli,
                IsAvailable = true,
                TenantId = accountInfo.TenantId,
                UserPrincipalName = accountInfo.User?.Name,
                DisplayName = accountInfo.User?.Type,
                Details = new Dictionary<string, object>
                {
                    ["subscription"] = accountInfo.Name ?? "Unknown",
                    ["environment"] = accountInfo.EnvironmentName ?? "AzureCloud",
                    ["isDefault"] = accountInfo.IsDefault
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect Azure CLI credentials");
            return null;
        }
    }

    /// <summary>
    /// Detects VS Code Microsoft extension credentials.
    /// </summary>
    private async Task<CredentialSource?> DetectVsCodeMicrosoftCredentialsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Checking for VS Code Microsoft extension credentials...");
            
            // Check common VS Code paths
            var vscodeConfigPaths = GetVsCodeConfigPaths();
            
            foreach (var configPath in vscodeConfigPaths)
            {
                var extensionPath = Path.Combine(configPath, "User", "globalStorage", "ms-vscode.azure-account");
                
                if (Directory.Exists(extensionPath))
                {
                    // Look for azure-account extension data
                    var tokensPath = Path.Combine(extensionPath, "azure.json");
                    if (File.Exists(tokensPath))
                    {
                        _logger.LogDebug("Found VS Code Azure account data at {Path}", tokensPath);
                        
                        // Note: We don't actually read the tokens for security reasons
                        // We just detect that VS Code has Microsoft credentials available
                        return new CredentialSource
                        {
                            Type = CredentialSourceType.VsCodeMicrosoft,
                            IsAvailable = true,
                            Details = new Dictionary<string, object>
                            {
                                ["configPath"] = configPath,
                                ["extensionPath"] = extensionPath
                            }
                        };
                    }
                }
            }
            
            _logger.LogDebug("No VS Code Microsoft extension credentials found");
            await Task.CompletedTask;
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect VS Code Microsoft extension credentials");
            return null;
        }
    }

    /// <summary>
    /// Detects Windows Credential Manager entries.
    /// </summary>
    private async Task<CredentialSource?> DetectWindowsCredentialManagerAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Checking Windows Credential Manager...");
            
            // Use PowerShell to check for Microsoft-related credentials
            var script = @"
                try {
                    $creds = @()
                    $targets = @('Microsoft*', 'Azure*', 'Office*', 'Teams*')
                    foreach ($target in $targets) {
                        try {
                            $result = cmdkey /list:$target 2>$null
                            if ($result -match 'Target:') {
                                $creds += $result
                            }
                        } catch { }
                    }
                    $creds | ConvertTo-Json
                } catch {
                    '[]'
                }
            ";

            var output = await RunPowerShellAsync(script, cancellationToken);
            
            if (!string.IsNullOrEmpty(output) && output.Trim() != "[]")
            {
                _logger.LogDebug("Found Windows Credential Manager entries");
                
                return new CredentialSource
                {
                    Type = CredentialSourceType.WindowsCredentialManager,
                    IsAvailable = true,
                    Details = new Dictionary<string, object>
                    {
                        ["credentialCount"] = output.Split('\n').Length,
                        ["detectedAt"] = DateTimeOffset.UtcNow
                    }
                };
            }
            
            _logger.LogDebug("No relevant Windows Credential Manager entries found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect Windows Credential Manager credentials");
            return null;
        }
    }

    /// <summary>
    /// Finds Azure CLI executable path.
    /// </summary>
    private async Task<string?> FindAzureCliPathAsync(CancellationToken cancellationToken)
    {
        try
        {
            var commands = OperatingSystem.IsWindows() 
                ? new[] { "az.cmd", "az.exe" }
                : new[] { "az" };

            foreach (var command in commands)
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = OperatingSystem.IsWindows() ? "where" : "which",
                            Arguments = command,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };

                    process.Start();
                    var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                    await process.WaitForExitAsync(cancellationToken);

                    if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        return output.Trim().Split('\n')[0].Trim();
                    }
                }
                catch
                {
                    // Continue to next command
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error finding Azure CLI path");
            return null;
        }
    }

    /// <summary>
    /// Gets current Azure CLI account information.
    /// </summary>
    private async Task<AzureAccountInfo?> GetAzureCliAccountAsync(string azPath, CancellationToken cancellationToken)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = azPath,
                    Arguments = "account show --output json",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return JsonSerializer.Deserialize<AzureAccountInfo>(output, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting Azure CLI account info");
            return null;
        }
    }

    /// <summary>
    /// Gets VS Code configuration paths for different platforms.
    /// </summary>
    private static string[] GetVsCodeConfigPaths()
    {
        var paths = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            paths.Add(Path.Combine(appData, "Code"));
            paths.Add(Path.Combine(appData, "Code - Insiders"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(home, "Library", "Application Support", "Code"));
            paths.Add(Path.Combine(home, "Library", "Application Support", "Code - Insiders"));
        }
        else if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(home, ".config", "Code"));
            paths.Add(Path.Combine(home, ".config", "Code - Insiders"));
        }

        return paths.ToArray();
    }

    /// <summary>
    /// Runs a PowerShell script and returns the output.
    /// </summary>
    private async Task<string> RunPowerShellAsync(string script, CancellationToken cancellationToken)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"{script}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return output;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error running PowerShell script");
            return string.Empty;
        }
    }
}

/// <summary>
/// Result of credential source detection.
/// </summary>
public record CredentialSourceResult
{
    /// <summary>
    /// Azure CLI credential information.
    /// </summary>
    public CredentialSource? AzureCli { get; set; }

    /// <summary>
    /// VS Code Microsoft extension credential information.
    /// </summary>
    public CredentialSource? VsCodeMicrosoft { get; set; }

    /// <summary>
    /// Windows Credential Manager information.
    /// </summary>
    public CredentialSource? WindowsCredentialManager { get; set; }

    /// <summary>
    /// Whether device code flow should be used as fallback.
    /// </summary>
    public bool DeviceCodeFallback { get; set; } = true;

    /// <summary>
    /// Gets the preferred credential source in priority order.
    /// </summary>
    public CredentialSource? GetPreferredSource()
    {
        // Priority: Azure CLI > VS Code > Windows Credential Manager
        return AzureCli ?? VsCodeMicrosoft ?? WindowsCredentialManager;
    }

    /// <summary>
    /// Gets all available credential sources.
    /// </summary>
    public IEnumerable<CredentialSource> GetAvailableSources()
    {
        var sources = new List<CredentialSource>();
        
        if (AzureCli?.IsAvailable == true) sources.Add(AzureCli);
        if (VsCodeMicrosoft?.IsAvailable == true) sources.Add(VsCodeMicrosoft);
        if (WindowsCredentialManager?.IsAvailable == true) sources.Add(WindowsCredentialManager);
        
        return sources;
    }
}

/// <summary>
/// Information about a credential source.
/// </summary>
public record CredentialSource
{
    /// <summary>
    /// Type of credential source.
    /// </summary>
    public CredentialSourceType Type { get; set; }

    /// <summary>
    /// Whether this credential source is available.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Tenant ID if available.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// User principal name if available.
    /// </summary>
    public string? UserPrincipalName { get; set; }

    /// <summary>
    /// Display name if available.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Additional details about the credential source.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Types of credential sources.
/// </summary>
public enum CredentialSourceType
{
    /// <summary>
    /// Azure CLI credentials.
    /// </summary>
    AzureCli,

    /// <summary>
    /// VS Code Microsoft extension credentials.
    /// </summary>
    VsCodeMicrosoft,

    /// <summary>
    /// Windows Credential Manager.
    /// </summary>
    WindowsCredentialManager,

    /// <summary>
    /// Device code flow.
    /// </summary>
    DeviceCode
}

/// <summary>
/// Azure CLI account information.
/// </summary>
internal record AzureAccountInfo
{
    public string? EnvironmentName { get; set; }
    public string? HomeTenantId { get; set; }
    public string? Id { get; set; }
    public bool IsDefault { get; set; }
    public string? Name { get; set; }
    public string? State { get; set; }
    public string? TenantId { get; set; }
    public AzureUserInfo? User { get; set; }
}

/// <summary>
/// Azure CLI user information.
/// </summary>
internal record AzureUserInfo
{
    public string? Name { get; set; }
    public string? Type { get; set; }
}