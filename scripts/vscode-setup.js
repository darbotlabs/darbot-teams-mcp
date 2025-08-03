#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const os = require('os');
const { spawn } = require('child_process');

function findVSCodeSettingsPath() {
  const platform = os.platform();
  let settingsDir;

  switch (platform) {
    case 'win32':
      settingsDir = path.join(os.homedir(), 'AppData', 'Roaming', 'Code', 'User');
      break;
    case 'darwin':
      settingsDir = path.join(os.homedir(), 'Library', 'Application Support', 'Code', 'User');
      break;
    case 'linux':
      settingsDir = path.join(os.homedir(), '.config', 'Code', 'User');
      break;
    default:
      throw new Error(`Unsupported platform: ${platform}`);
  }

  return path.join(settingsDir, 'settings.json');
}

function getPackageInstallPath() {
  // Get the global npm install location for this package
  let packagePath = __dirname;
  
  // Go up from scripts/ to package root
  packagePath = path.dirname(packagePath);
  
  return packagePath;
}

/**
 * Detects Azure CLI tenant ID if available
 */
async function detectTenantId() {
  try {
    console.log('🔍 Detecting tenant configuration from Azure CLI...');
    
    // Check if Azure CLI is available
    const azPath = await findAzureCli();
    if (!azPath) {
      console.log('ℹ️  Azure CLI not found - will use manual configuration');
      return null;
    }

    // Get current account info
    const accountInfo = await getAzureAccount(azPath);
    if (accountInfo && accountInfo.tenantId) {
      console.log(`✅ Detected tenant ID: ${accountInfo.tenantId}`);
      return accountInfo.tenantId;
    }

    console.log('ℹ️  No Azure CLI session found - will use manual configuration');
    return null;
  } catch (error) {
    console.log(`ℹ️  Tenant detection failed: ${error.message} - will use manual configuration`);
    return null;
  }
}

/**
 * Finds Azure CLI executable
 */
async function findAzureCli() {
  const commands = process.platform === 'win32' ? ['az.cmd', 'az.exe'] : ['az'];
  
  for (const command of commands) {
    try {
      const which = process.platform === 'win32' ? 'where' : 'which';
      const result = await runCommand(which, [command]);
      if (result.success && result.stdout.trim()) {
        return result.stdout.trim().split('\n')[0].trim();
      }
    } catch {
      // Continue to next command
    }
  }
  
  return null;
}

/**
 * Gets Azure CLI account information
 */
async function getAzureAccount(azPath) {
  try {
    const result = await runCommand(azPath, ['account', 'show', '--output', 'json']);
    if (result.success && result.stdout) {
      return JSON.parse(result.stdout);
    }
  } catch (error) {
    // Azure CLI might not be logged in
  }
  
  return null;
}

/**
 * Runs a command and returns the result
 */
function runCommand(command, args, options = {}) {
  return new Promise((resolve) => {
    const child = spawn(command, args, {
      stdio: ['ignore', 'pipe', 'pipe'],
      ...options
    });

    let stdout = '';
    let stderr = '';

    child.stdout?.on('data', (data) => {
      stdout += data.toString();
    });

    child.stderr?.on('data', (data) => {
      stderr += data.toString();
    });

    child.on('close', (code) => {
      resolve({
        success: code === 0,
        stdout,
        stderr,
        code
      });
    });

    child.on('error', (error) => {
      resolve({
        success: false,
        stdout,
        stderr,
        error: error.message,
        code: -1
      });
    });
  });
}

function createMCPServerConfig(packagePath, tenantId = null) {
  // Use detected tenant ID, or provide guidance for manual configuration
  const finalTenantId = tenantId || '${TEAMS_TENANT_ID}';
  const isDetected = tenantId !== null;
  
  return {
    "mcp.servers": {
      "darbot-teams": {
        "command": "npx",
        "args": [
          "darbot-teams-mcp",
          "--stdio"
        ],
        "env": {
          "TEAMS_CLIENT_ID": "04b07795-8ddb-461a-bbee-02f9e1bf7b46",
          "TEAMS_TENANT_ID": finalTenantId,
          "TEAMS_LOG_LEVEL": "Warning",
          "MCP_MODE": "stdio",
          "TEAMS_SIMULATION_MODE": "true",
          "TEAMS_REQUIRE_AUTHENTICATION": "false"
        }
      }
    },
    // Add metadata for configuration guidance
    "_darbot_config_info": {
      "tenant_detected": isDetected,
      "setup_required": !isDetected,
      "instructions": isDetected ? 
        "Tenant ID was automatically detected from Azure CLI" :
        "Replace ${TEAMS_TENANT_ID} with your actual tenant ID or set TEAMS_TENANT_ID environment variable"
    }
  };
}

async function setupVSCode() {
  try {
    const settingsPath = findVSCodeSettingsPath();
    const packagePath = getPackageInstallPath();
    
    console.log('🔧 Configuring VS Code for Darbot Teams MCP...');
    console.log(`📍 VS Code settings: ${settingsPath}`);
    console.log(`📦 Package location: ${packagePath}`);
    
    // Try to detect tenant ID from Azure CLI
    const detectedTenantId = await detectTenantId();
    
    // Ensure settings directory exists
    const settingsDir = path.dirname(settingsPath);
    if (!fs.existsSync(settingsDir)) {
      fs.mkdirSync(settingsDir, { recursive: true });
      console.log(`📁 Created settings directory: ${settingsDir}`);
    }

    // Read existing settings or create new
    let settings = {};
    if (fs.existsSync(settingsPath)) {
      const settingsContent = fs.readFileSync(settingsPath, 'utf8');
      try {
        settings = JSON.parse(settingsContent);
        console.log('📖 Found existing VS Code settings');
      } catch (error) {
        console.warn('⚠️  Invalid JSON in settings.json, creating backup and starting fresh');
        fs.writeFileSync(settingsPath + '.backup', settingsContent);
        settings = {};
      }
    } else {
      console.log('📄 Creating new VS Code settings file');
    }

    // Add or update MCP server configuration
    const mcpConfig = createMCPServerConfig(packagePath, detectedTenantId);
    
    if (!settings["mcp.servers"]) {
      settings["mcp.servers"] = {};
    }
    
    settings["mcp.servers"]["darbot-teams"] = mcpConfig["mcp.servers"]["darbot-teams"];

    // Write updated settings
    fs.writeFileSync(settingsPath, JSON.stringify(settings, null, 2));
    
    console.log('✅ VS Code configuration updated successfully!');
    
    // Show appropriate next steps based on whether tenant was detected
    if (detectedTenantId) {
      console.log(`
🎉 Setup Complete!

✅ TENANT CONFIGURATION:
  • Automatically detected tenant ID: ${detectedTenantId}
  • Configuration is ready to use!

NEXT STEPS:
1. 📱 Restart VS Code to load the new MCP server
2. 🔌 The darbot-teams MCP server should automatically connect
3. 🎯 You can now use Teams commands in your AI conversations

EXAMPLE COMMANDS TO TRY:
  • "List all members of my team"
  • "Create a new channel called 'project-updates'"  
  • "Show me recent files in the team"
  • "Schedule a meeting for tomorrow"

CONFIGURATION DETAILS:
  • Server: darbot-teams
  • Mode: stdio (VS Code compatible)
  • Tenant: ${detectedTenantId} (auto-detected)
  • Authentication: Disabled (for initial testing)
  • Simulation: Enabled (safe for experimentation)
  • Tools available: 50+ Teams management commands
`);
    } else {
      console.log(`
🎉 Setup Complete - Manual Configuration Required!

⚠️  TENANT CONFIGURATION NEEDED:
  • Could not auto-detect your Microsoft 365 tenant ID
  • You need to configure your tenant ID manually

🔧 SETUP YOUR TENANT ID:

Option 1 - Use Azure CLI (Recommended):
  1. Install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
  2. Login: az login
  3. Re-run setup: npx darbot-teams-mcp --vscode-setup

Option 2 - Set Environment Variable:
  1. Find your tenant ID: https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-how-to-find-tenant
  2. Set environment variable: TEAMS_TENANT_ID=your-tenant-id-here
  3. Restart VS Code

Option 3 - Edit VS Code Settings:
  1. Open VS Code Settings (Ctrl/Cmd + ,)
  2. Search for "mcp"
  3. Replace \${TEAMS_TENANT_ID} with your actual tenant ID

📋 FINDING YOUR TENANT ID:
  • Azure Portal: Go to Azure Active Directory → Properties → Tenant ID
  • PowerShell: (Get-AzureADTenantDetail).ObjectId
  • Office 365 Admin Center: Settings → Org settings → Organization profile

NEXT STEPS AFTER CONFIGURATION:
1. 📱 Restart VS Code to load the new MCP server
2. 🔌 The darbot-teams MCP server should automatically connect
3. 🎯 You can now use Teams commands in your AI conversations
`);
    }

    console.log(`
TROUBLESHOOTING:
  • If tools don't appear, check VS Code Developer Console
  • Ensure MCP extension is installed in VS Code
  • Run 'npx darbot-teams-mcp --test' to verify installation

For more help: npx darbot-teams-mcp --help
`);

    // Also create a backup config file for reference
    const configBackupPath = path.join(packagePath, 'configs', 'vscode-settings-generated.json');
    fs.writeFileSync(configBackupPath, JSON.stringify(mcpConfig, null, 2));
    console.log(`📋 Configuration backup saved: ${configBackupPath}`);
    
  } catch (error) {
    console.error('❌ VS Code setup failed:', error.message);
    
    if (error.message.includes('Unsupported platform')) {
      console.log(`
🔧 MANUAL SETUP REQUIRED:

Your platform is not automatically supported, but you can set it up manually:

1️⃣  FIND YOUR VS CODE SETTINGS:
    • Windows: %APPDATA%/Code/User/settings.json
    • macOS: ~/Library/Application Support/Code/User/settings.json  
    • Linux: ~/.config/Code/User/settings.json

2️⃣  ADD THIS CONFIGURATION:

${JSON.stringify(createMCPServerConfig(getPackageInstallPath(), null), null, 2)}

3️⃣  CONFIGURE YOUR TENANT:
    • Replace \${TEAMS_TENANT_ID} with your actual tenant ID
    • Or set TEAMS_TENANT_ID environment variable
    • Find tenant ID: https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-how-to-find-tenant

4️⃣  RESTART VS CODE and you're ready!

NEED HELP?
  • Documentation: https://github.com/darbotlabs/darbot-teams-mcp
  • Issues: https://github.com/darbotlabs/darbot-teams-mcp/issues
`);
    } else if (error.code === 'EACCES' || error.code === 'EPERM') {
      console.log(`
🔒 PERMISSION DENIED

Try one of these solutions:

OPTION 1 - Run with elevated permissions:
  • Windows: Run as Administrator
  • macOS/Linux: Use sudo (not recommended)

OPTION 2 - Manual setup (recommended):
  1. Open VS Code
  2. Go to Settings (Ctrl/Cmd + ,)
  3. Search for "mcp"
  4. Add the configuration manually

CONFIGURATION TO ADD:
${JSON.stringify(createMCPServerConfig(getPackageInstallPath(), null), null, 2)}

⚠️  IMPORTANT: Replace \${TEAMS_TENANT_ID} with your actual tenant ID!

NEED HELP?
  • Documentation: https://github.com/darbotlabs/darbot-teams-mcp
`);
    } else {
      console.log(`
🔧 SETUP FAILED - Manual configuration required

MANUAL SETUP STEPS:
1. Open VS Code Settings (Ctrl/Cmd + ,)
2. Search for "mcp" 
3. Add this configuration:

${JSON.stringify(createMCPServerConfig(getPackageInstallPath(), null), null, 2)}

⚠️  IMPORTANT: Replace \${TEAMS_TENANT_ID} with your actual tenant ID!

ALTERNATIVE:
  • Edit settings.json directly
  • Location varies by OS (see documentation)

GET HELP:
  • Documentation: https://github.com/darbotlabs/darbot-teams-mcp
  • Issues: https://github.com/darbotlabs/darbot-teams-mcp/issues
`);
    }
    
    process.exit(1);
  }
}

setupVSCode();