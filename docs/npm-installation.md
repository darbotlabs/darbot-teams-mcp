# Darbot Teams MCP - NPM Installation Guide

## Quick Installation (NPM - Recommended)

### 1. Install the Package

```bash
npm install -g darbot-teams-mcp
```

This will:
- Install the MCP server globally
- Build the .NET components automatically
- Make the `darbot-teams-mcp` command available anywhere

### 2. Auto-Configure VS Code (One Command!)

```bash
npx darbot-teams-mcp --vscode-setup
```

This command automatically:
- ✅ Finds your VS Code settings.json location (Windows/Mac/Linux)
- ✅ Adds the MCP server configuration
- ✅ Sets secure default values for testing
- ✅ Creates a backup of your existing settings
- ✅ Validates the installation

### 3. Test the Installation

```bash
npx darbot-teams-mcp --test
```

Expected output:
```
🎉 All tests passed!
📋 Summary:
  - HTTP mode: ✅ Working (localhost:3001)
  - Stdio mode: ✅ Working (for VS Code integration)
  - Tools registered: 50
  - MCP protocol: ✅ JSON-RPC 2.0 compliant
🚀 Ready for VS Code integration!
```

## Usage

Once installed, you can:

```bash
# Run in VS Code stdio mode (default)
npx darbot-teams-mcp --stdio

# Run in HTTP mode for testing
npx darbot-teams-mcp --http

# Show all available options
npx darbot-teams-mcp --help
```

## VS Code Integration

After running `--vscode-setup`, restart VS Code. The MCP server will automatically connect and you'll have access to 50+ Teams management tools in your AI conversations.

Example commands you can try:
- "List all members of my team"
- "Create a new channel called 'project-updates'"
- "Show me recent files in the team"
- "Schedule a meeting for tomorrow"

## Configuration

The package uses secure defaults for initial testing:

```json
{
  "TEAMS_CLIENT_ID": "04b07795-8ddb-461a-bbee-02f9e1bf7b46",
  "TEAMS_TENANT_ID": "common",
  "TEAMS_SIMULATION_MODE": "true",
  "TEAMS_REQUIRE_AUTHENTICATION": "false"
}
```

For production use, set environment variables:
```bash
TEAMS_SIMULATION_MODE=false TEAMS_REQUIRE_AUTHENTICATION=true npx darbot-teams-mcp --stdio
```

## Troubleshooting

### Package Installation Issues
```bash
# If npm install fails, try:
npm cache clean --force
npm install -g darbot-teams-mcp --verbose
```

### VS Code Setup Issues
```bash
# If auto-setup fails, try manual setup:
npx darbot-teams-mcp --help
# Then copy the configuration to your VS Code settings.json manually
```

### .NET SDK Missing
```bash
# Install .NET 8.0+ SDK from:
# https://dotnet.microsoft.com/download
# Then reinstall the package:
npm install -g darbot-teams-mcp
```

## Manual VS Code Configuration (Alternative)

If the auto-setup doesn't work, add this to your VS Code `settings.json`:

```json
{
  "mcp.servers": {
    "darbot-teams": {
      "command": "npx",
      "args": ["darbot-teams-mcp", "--stdio"],
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
}
```

## Benefits of NPM Installation

✅ **No Git Required**: Install directly from npm registry
✅ **No Manual Paths**: Works from anywhere via `npx`
✅ **Auto-Configuration**: One command VS Code setup  
✅ **Version Management**: Easy updates via npm
✅ **Cross-Platform**: Works on Windows, Mac, Linux
✅ **Default Values**: Secure defaults for immediate testing
✅ **Automatic Build**: .NET project built automatically

This makes the MCP server accessible to users without requiring Git knowledge, manual path configuration, or complex setup procedures.