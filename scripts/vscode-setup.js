#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const os = require('os');

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

function createMCPServerConfig(packagePath) {
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
          "TEAMS_TENANT_ID": "common",
          "TEAMS_LOG_LEVEL": "Warning",
          "MCP_MODE": "stdio",
          "TEAMS_SIMULATION_MODE": "true",
          "TEAMS_REQUIRE_AUTHENTICATION": "false"
        }
      }
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
    const mcpConfig = createMCPServerConfig(packagePath);
    
    if (!settings["mcp.servers"]) {
      settings["mcp.servers"] = {};
    }
    
    settings["mcp.servers"]["darbot-teams"] = mcpConfig["mcp.servers"]["darbot-teams"];

    // Write updated settings
    fs.writeFileSync(settingsPath, JSON.stringify(settings, null, 2));
    
    console.log('✅ VS Code configuration updated successfully!');
    console.log(`
🎉 Setup Complete!

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
  • Authentication: Disabled (for initial testing)
  • Simulation: Enabled (safe for experimentation)
  • Tools available: 50+ Teams management commands

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

${JSON.stringify(createMCPServerConfig(getPackageInstallPath()), null, 2)}

3️⃣  RESTART VS CODE and you're ready!

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
${JSON.stringify(createMCPServerConfig(getPackageInstallPath()), null, 2)}

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

${JSON.stringify(createMCPServerConfig(getPackageInstallPath()), null, 2)}

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