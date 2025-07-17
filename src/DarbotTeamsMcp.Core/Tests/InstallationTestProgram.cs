using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DarbotTeamsMcp.Core.Configuration;
using DarbotTeamsMcp.Core.Services;
using DarbotTeamsMcp.Core.Interfaces;
using DarbotTeamsMcp.Core.Models;

namespace DarbotTeamsMcp.Core.Tests;

/// <summary>
/// Simple test program to validate credential detection and authentication improvements.
/// This demonstrates the enhanced installation experience.
/// </summary>
public class InstallationTestProgram
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Darbot Teams MCP - Installation Test");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();

        // Build service collection
        var services = new ServiceCollection();
        ConfigureServices(services);

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Test 1: Credential Detection
            Console.WriteLine("🔍 Testing credential detection...");
            var credentialService = serviceProvider.GetRequiredService<CredentialDetectionService>();
            var credentialSources = await credentialService.DetectCredentialSourcesAsync();

            DisplayCredentialDetectionResults(credentialSources);

            // Test 2: Configuration Generation
            Console.WriteLine("\n⚙️  Testing configuration generation...");
            var config = await TeamsConfiguration.FromEnvironmentWithAutoDetectionAsync();
            DisplayConfiguration(config);

            // Test 3: Authentication Service
            Console.WriteLine("\n🔐 Testing enhanced authentication service...");
            var authService = serviceProvider.GetRequiredService<ITeamsAuthProvider>();
            var authContext = await authService.GetAuthenticationContextAsync();
            DisplayAuthenticationContext(authContext);

            Console.WriteLine("\n✅ Installation test completed successfully!");
            Console.WriteLine();
            Console.WriteLine("🎯 Next steps:");
            Console.WriteLine("1. Run the setup script: ./setup.ps1 -Interactive");
            Console.WriteLine("2. Start the server: dotnet run --project src/DarbotTeamsMcp.Server");
            Console.WriteLine("3. Configure your MCP client using the generated configs");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
            Console.WriteLine($"Details: {ex}");
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add memory cache
        services.AddMemoryCache();

        // Add configuration
        var config = TeamsConfiguration.FromEnvironment();
        services.AddSingleton(config);

        // Add our services
        services.AddSingleton<CredentialDetectionService>();
        services.AddSingleton<ITeamsAuthProvider, EnhancedAuthService>();
    }

    private static void DisplayCredentialDetectionResults(CredentialSourceResult credentialSources)
    {
        Console.WriteLine("Credential Detection Results:");
        Console.WriteLine("─────────────────────────────");

        var availableSources = credentialSources.GetAvailableSources().ToList();

        if (availableSources.Any())
        {
            foreach (var source in availableSources)
            {
                Console.WriteLine($"✅ {source.Type}: Available");
                if (!string.IsNullOrEmpty(source.TenantId))
                    Console.WriteLine($"   Tenant: {source.TenantId}");
                if (!string.IsNullOrEmpty(source.UserPrincipalName))
                    Console.WriteLine($"   User: {source.UserPrincipalName}");
            }

            var preferred = credentialSources.GetPreferredSource();
            Console.WriteLine($"\n🎯 Preferred source: {preferred?.Type.ToString() ?? "None"}");
        }
        else
        {
            Console.WriteLine("⚠️  No existing credentials detected");
            Console.WriteLine("   Device code flow will be used on first authentication");
        }
    }

    private static void DisplayConfiguration(TeamsConfiguration config)
    {
        Console.WriteLine("Generated Configuration:");
        Console.WriteLine("───────────────────────");
        Console.WriteLine($"Tenant ID: {config.TenantId}");
        Console.WriteLine($"Client ID: {config.ClientId}");
        Console.WriteLine($"Server Port: {config.ServerPort}");
        Console.WriteLine($"Log Level: {config.LogLevel}");
        Console.WriteLine($"Scopes: {config.Scopes.Count} configured");
    }

    private static void DisplayAuthenticationContext(AuthenticationContext authContext)
    {
        Console.WriteLine("Authentication Context:");
        Console.WriteLine("──────────────────────");
        Console.WriteLine($"Authenticated: {authContext.IsAuthenticated}");
        Console.WriteLine($"Method: {authContext.AuthenticationMethod}");
        
        if (!string.IsNullOrEmpty(authContext.UserPrincipalName))
            Console.WriteLine($"User: {authContext.UserPrincipalName}");
        if (!string.IsNullOrEmpty(authContext.TenantId))
            Console.WriteLine($"Tenant: {authContext.TenantId}");
    }
}