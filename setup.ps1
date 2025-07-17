#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Automated setup and validation script for Darbot Teams MCP
    
.DESCRIPTION
    This script automates the installation process by:
    1. Detecting existing Microsoft credentials
    2. Validating environment prerequisites  
    3. Auto-configuring environment variables
    4. Setting up MCP server configuration
    5. Validating Microsoft Graph permissions
    
.PARAMETER Interactive
    Run in interactive mode with guided setup
    
.PARAMETER ValidateOnly
    Only validate the current setup without making changes
    
.PARAMETER OutputPath
    Path to write the generated environment configuration
    
.EXAMPLE
    ./setup.ps1 -Interactive
    
.EXAMPLE
    ./setup.ps1 -ValidateOnly
#>

param(
    [switch]$Interactive = $false,
    [switch]$ValidateOnly = $false,
    [string]$OutputPath = ".env"
)

# Script configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Colors for output
$Red = "`e[31m"
$Green = "`e[32m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Magenta = "`e[35m"
$Cyan = "`e[36m"
$Reset = "`e[0m"

function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "${Blue}═══════════════════════════════════════${Reset}" -ForegroundColor Blue
    Write-Host "${Blue} $Text${Reset}" -ForegroundColor Blue  
    Write-Host "${Blue}═══════════════════════════════════════${Reset}" -ForegroundColor Blue
    Write-Host ""
}

function Write-Success {
    param([string]$Text)
    Write-Host "${Green}✅ $Text${Reset}" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Text)
    Write-Host "${Yellow}⚠️  $Text${Reset}" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Text)
    Write-Host "${Red}❌ $Text${Reset}" -ForegroundColor Red
}

function Write-Info {
    param([string]$Text)
    Write-Host "${Cyan}ℹ️  $Text${Reset}" -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Text)
    Write-Host "${Magenta}🔄 $Text${Reset}" -ForegroundColor Magenta
}

class CredentialDetector {
    [bool] DetectAzureCli() {
        try {
            $azPath = Get-Command "az" -ErrorAction SilentlyContinue
            if (-not $azPath) {
                return $false
            }
            
            $account = az account show --output json 2>$null | ConvertFrom-Json
            return $account -ne $null -and $account.state -eq "Enabled"
        }
        catch {
            return $false
        }
    }
    
    [object] GetAzureCliAccount() {
        try {
            $account = az account show --output json 2>$null | ConvertFrom-Json
            return $account
        }
        catch {
            return $null
        }
    }
    
    [bool] DetectVsCodeMicrosoft() {
        $vscodeConfigPaths = @()
        
        if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
            $vscodeConfigPaths = @(
                "$env:APPDATA\Code",
                "$env:APPDATA\Code - Insiders"
            )
        }
        elseif ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) {
            $vscodeConfigPaths = @(
                "$env:HOME/Library/Application Support/Code",
                "$env:HOME/Library/Application Support/Code - Insiders"
            )
        }
        else {
            $vscodeConfigPaths = @(
                "$env:HOME/.config/Code",
                "$env:HOME/.config/Code - Insiders"
            )
        }
        
        foreach ($configPath in $vscodeConfigPaths) {
            $extensionPath = Join-Path $configPath "User\globalStorage\ms-vscode.azure-account"
            if (Test-Path $extensionPath) {
                $tokensPath = Join-Path $extensionPath "azure.json"
                if (Test-Path $tokensPath) {
                    return $true
                }
            }
        }
        
        return $false
    }
    
    [bool] DetectWindowsCredentialManager() {
        if (-not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
            return $false
        }
        
        try {
            $creds = cmdkey /list 2>$null | Where-Object { $_ -match "Microsoft|Azure|Office|Teams" }
            return $creds.Count -gt 0
        }
        catch {
            return $false
        }
    }
}

class SetupValidator {
    [bool] ValidateDotNet() {
        try {
            $dotnetVersion = dotnet --version 2>$null
            if (-not $dotnetVersion) {
                return $false
            }
            
            $version = [Version]$dotnetVersion
            return $version.Major -ge 8
        }
        catch {
            return $false
        }
    }
    
    [bool] ValidateProjectStructure() {
        $requiredPaths = @(
            "src/DarbotTeamsMcp.Server",
            "src/DarbotTeamsMcp.Core", 
            "DarbotTeamsMcp.sln"
        )
        
        foreach ($path in $requiredPaths) {
            if (-not (Test-Path $path)) {
                return $false
            }
        }
        
        return $true
    }
    
    [bool] ValidateBuildSuccess() {
        try {
            $buildResult = dotnet build --nologo --verbosity quiet 2>&1
            return $LASTEXITCODE -eq 0
        }
        catch {
            return $false
        }
    }
}

class EnvironmentGenerator {
    [hashtable] GenerateEnvironmentConfig([object]$credentialInfo) {
        $config = @{
            "TEAMS_CLIENT_ID" = "04b07795-8ddb-461a-bbee-02f9e1bf7b46"  # Microsoft Graph Command Line Tools
            "TEAMS_TENANT_ID" = "common"
            "TEAMS_SERVER_PORT" = "3001" 
            "TEAMS_SERVER_HOST" = "localhost"
            "TEAMS_LOG_LEVEL" = "Information"
            "TEAMS_LOG_TO_FILE" = "true"
            "TEAMS_ENABLE_REQUEST_LOGGING" = "false"
            "TEAMS_SIMULATION_MODE" = "false"
            "TEAMS_REQUIRE_AUTHENTICATION" = "true"
        }
        
        # Override with detected credential information
        if ($credentialInfo.AzureCli) {
            $config["TEAMS_TENANT_ID"] = $credentialInfo.AzureCli.tenantId
            Write-Info "Using Azure CLI tenant: $($credentialInfo.AzureCli.tenantId)"
        }
        
        return $config
    }
    
    [void] WriteEnvironmentFile([hashtable]$config, [string]$outputPath) {
        $envContent = @()
        $envContent += "# Darbot Teams MCP Configuration"
        $envContent += "# Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
        $envContent += ""
        
        foreach ($key in $config.Keys | Sort-Object) {
            $envContent += "$key=$($config[$key])"
        }
        
        $envContent | Out-File -FilePath $outputPath -Encoding UTF8
    }
}

class MicrosoftGraphValidator {
    [bool] ValidateGraphPermissions([string]$accessToken) {
        try {
            $headers = @{
                "Authorization" = "Bearer $accessToken"
                "Content-Type" = "application/json"
            }
            
            # Test basic Graph access
            $response = Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/me" -Headers $headers -Method Get
            return $response -ne $null
        }
        catch {
            return $false
        }
    }
    
    [object] GetRequiredPermissions() {
        return @(
            "Team.ReadBasic.All",
            "TeamMember.ReadWrite.All", 
            "Channel.ReadWrite.All",
            "ChannelMessage.Read.All",
            "Files.ReadWrite.All",
            "Calendars.ReadWrite",
            "Tasks.ReadWrite",
            "Presence.Read.All",
            "User.Read"
        )
    }
}

function Main {
    Write-Header "🚀 Darbot Teams MCP Setup & Validation"
    
    if ($ValidateOnly) {
        Write-Info "Running in validation-only mode"
    }
    
    # Initialize components
    $detector = [CredentialDetector]::new()
    $validator = [SetupValidator]::new()
    $envGenerator = [EnvironmentGenerator]::new()
    $graphValidator = [MicrosoftGraphValidator]::new()
    
    $validationResults = @{
        DotNet = $false
        ProjectStructure = $false
        Build = $false
        AzureCli = $false
        VsCodeMicrosoft = $false
        WindowsCredentialManager = $false
    }
    
    # Step 1: Validate prerequisites
    Write-Step "Validating prerequisites..."
    
    $validationResults.DotNet = $validator.ValidateDotNet()
    if ($validationResults.DotNet) {
        Write-Success ".NET SDK 8.0+ detected"
    } else {
        Write-Error ".NET SDK 8.0+ not found. Please install from https://dotnet.microsoft.com/download"
        exit 1
    }
    
    $validationResults.ProjectStructure = $validator.ValidateProjectStructure()
    if ($validationResults.ProjectStructure) {
        Write-Success "Project structure validated"
    } else {
        Write-Error "Invalid project structure. Please run from the repository root."
        exit 1
    }
    
    # Step 2: Test build
    Write-Step "Testing project build..."
    $validationResults.Build = $validator.ValidateBuildSuccess()
    if ($validationResults.Build) {
        Write-Success "Project builds successfully"
    } else {
        Write-Warning "Project build failed. This may cause runtime issues."
    }
    
    # Step 3: Detect credentials
    Write-Step "Detecting existing Microsoft credentials..."
    
    $credentialInfo = @{}
    
    $validationResults.AzureCli = $detector.DetectAzureCli()
    if ($validationResults.AzureCli) {
        Write-Success "Azure CLI credentials detected"
        $credentialInfo.AzureCli = $detector.GetAzureCliAccount()
        Write-Info "Account: $($credentialInfo.AzureCli.user.name)"
        Write-Info "Tenant: $($credentialInfo.AzureCli.tenantId)"
    } else {
        Write-Warning "Azure CLI not found or not logged in"
        Write-Info "Run 'az login' to authenticate with Azure CLI for seamless integration"
    }
    
    $validationResults.VsCodeMicrosoft = $detector.DetectVsCodeMicrosoft()
    if ($validationResults.VsCodeMicrosoft) {
        Write-Success "VS Code Microsoft extension credentials detected"
    } else {
        Write-Info "VS Code Microsoft extension credentials not found"
    }
    
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        $validationResults.WindowsCredentialManager = $detector.DetectWindowsCredentialManager()
        if ($validationResults.WindowsCredentialManager) {
            Write-Success "Windows Credential Manager entries detected"
        } else {
            Write-Info "No relevant Windows Credential Manager entries found"
        }
    }
    
    # Step 4: Generate configuration (if not validation-only)
    if (-not $ValidateOnly) {
        Write-Step "Generating environment configuration..."
        
        $envConfig = $envGenerator.GenerateEnvironmentConfig($credentialInfo)
        $envGenerator.WriteEnvironmentFile($envConfig, $OutputPath)
        
        Write-Success "Environment configuration written to $OutputPath"
        
        # Step 5: Update MCP configurations
        Write-Step "Updating MCP server configurations..."
        
        # Update Claude Desktop config
        $claudeConfigPath = "configs/claude-desktop-config.json"
        if (Test-Path $claudeConfigPath) {
            $claudeConfig = Get-Content $claudeConfigPath | ConvertFrom-Json
            $claudeConfig.mcpServers."darbot-teams".env = $envConfig
            $claudeConfig | ConvertTo-Json -Depth 4 | Out-File -FilePath $claudeConfigPath -Encoding UTF8
            Write-Success "Updated Claude Desktop configuration"
        }
        
        # Update VS Code config
        $vscodeConfigPath = "configs/vscode-settings.json"
        if (Test-Path $vscodeConfigPath) {
            $vscodeConfig = Get-Content $vscodeConfigPath | ConvertFrom-Json
            $vscodeConfig."mcp.servers"."darbot-teams".env = $envConfig
            $vscodeConfig | ConvertTo-Json -Depth 4 | Out-File -FilePath $vscodeConfigPath -Encoding UTF8
            Write-Success "Updated VS Code configuration"
        }
    }
    
    # Step 6: Display summary
    Write-Header "📊 Setup Summary"
    
    $successCount = ($validationResults.Values | Where-Object { $_ -eq $true }).Count
    $totalChecks = $validationResults.Count
    
    Write-Host "Prerequisites: " -NoNewline
    if ($validationResults.DotNet -and $validationResults.ProjectStructure) {
        Write-Success "✅ Ready"
    } else {
        Write-Error "❌ Issues found"
    }
    
    Write-Host "Credentials: " -NoNewline
    $credentialCount = @($validationResults.AzureCli, $validationResults.VsCodeMicrosoft, $validationResults.WindowsCredentialManager) | Where-Object { $_ } | Measure-Object | Select-Object -ExpandProperty Count
    if ($credentialCount -gt 0) {
        Write-Success "✅ $credentialCount source(s) detected"
    } else {
        Write-Warning "⚠️  No existing credentials found - device code flow will be used"
    }
    
    if (-not $ValidateOnly) {
        Write-Header "🎯 Next Steps"
        
        Write-Info "1. Start the MCP server:"
        Write-Host "   ${Cyan}cd src/DarbotTeamsMcp.Server${Reset}"
        Write-Host "   ${Cyan}dotnet run${Reset}"
        Write-Host ""
        
        Write-Info "2. Test authentication (first run will trigger device code flow if needed):"
        Write-Host "   The server will guide you through authentication on first use"
        Write-Host ""
        
        Write-Info "3. Configure your MCP client:"
        Write-Host "   • Claude Desktop: Use configs/claude-desktop-config.json"
        Write-Host "   • VS Code: Use configs/vscode-settings.json"
        Write-Host ""
        
        if ($credentialCount -eq 0) {
            Write-Warning "For fastest setup, consider running 'az login' first to enable automatic credential detection"
        }
    }
    
    Write-Success "Setup validation completed! 🎉"
}

# Run the main function
Main