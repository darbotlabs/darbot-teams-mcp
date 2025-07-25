# VS Code MCP Integration Example

This example demonstrates how to integrate the Darbot Teams MCP server with VS Code for agent mode.

## Prerequisites

1. VS Code with MCP extension installed
2. Node.js 16.0+ (for npm installation)
3. .NET 8.0+ SDK (automatically checked during installation)

## Setup Instructions (NPM - Recommended)

### 1. Install the Package

```bash
npm install -g darbot-teams-mcp
```

### 2. Auto-Configure VS Code

```bash
npx darbot-teams-mcp --vscode-setup
```

This command automatically:
- ✅ Finds your VS Code settings.json location
- ✅ Adds the MCP server configuration
- ✅ Sets secure default values for testing
- ✅ Creates a backup of your existing settings
- ✅ Validates the installation

### 3. Test the Integration

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

## Manual Setup (Alternative)

If the auto-setup doesn't work for your system:

### 1. Install the Package

```bash
npm install -g darbot-teams-mcp
```

### 2. Manually Configure VS Code

Add the following to your VS Code `settings.json`:

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

## Git Installation (For Development)

### 1. Clone and Build

```bash
git clone https://github.com/darbotlabs/darbot-teams-mcp
cd darbot-teams-mcp
dotnet build
```

### 2. Configure VS Code

Add this to your VS Code `settings.json` (replace `/path/to/darbot-teams-mcp` with actual path):

```json
{
  "mcp.servers": {
    "darbot-teams": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/path/to/darbot-teams-mcp/src/DarbotTeamsMcp.Server",
        "--",
        "--stdio"
      ],
      "env": {
        "TEAMS_TENANT_ID": "common",
        "TEAMS_CLIENT_ID": "04b07795-8ddb-461a-bbee-02f9e1bf7b46",
        "TEAMS_LOG_LEVEL": "Warning",
        "MCP_MODE": "stdio",
        "TEAMS_SIMULATION_MODE": "true",
        "TEAMS_REQUIRE_AUTHENTICATION": "false"
      }
    }
  }
}
```

## Available Teams Tools

Once integrated, you'll have access to 50+ Teams management tools across 9 categories:

### User Management (11 tools)
- `teams-list-members` - List all team members
- `teams-add-member` - Add new team members
- `teams-remove-member` - Remove team members
- `teams-list-owners` - List team owners
- `teams-list-guests` - List guest users
- And more...

### Channel Management (10 tools)
- `teams-list-channels` - List all channels
- `teams-create-channel` - Create new channels
- `teams-archive-channel` - Archive channels
- `teams-get-channel-info` - Get channel details
- And more...

### Messaging (5 tools)
- `teams-pin-message` - Pin important messages
- `teams-search-messages` - Search channel messages
- `teams-send-announcement` - Send announcements
- And more...

### Files (4 tools)
- `teams-upload-file` - Upload files to team
- `teams-download-file` - Download team files
- `teams-list-files` - List team files
- `teams-delete-file` - Delete team files

### And many more across Meetings, Tasks, Integrations, Presence, and Support categories!

## Usage in VS Code

1. Open VS Code with the MCP extension
2. The darbot-teams MCP server should automatically connect
3. You can now use Teams commands in your AI conversations
4. Example prompts:
   - "List all members of my team"
   - "Create a new channel called 'project-updates'"
   - "Show me recent files in the team"

## Troubleshooting

### Server won't start
- Ensure .NET 8.0+ is installed
- Check that the path in settings.json is correct
- Run `dotnet build` in the project directory

### Tools not appearing
- Check the VS Code Developer Console for errors
- Verify the MCP extension is properly installed
- Try restarting VS Code

### Authentication issues
- The server uses mock authentication by default
- For production use, configure proper Azure AD authentication
- Check that CLIENT_ID and TENANT_ID are set correctly

## Manual Testing

You can also test the server manually:

```bash
# Test stdio mode directly
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | \
  dotnet run --project src/DarbotTeamsMcp.Server -- --stdio

# Test HTTP mode
dotnet run --project src/DarbotTeamsMcp.Server &
curl http://localhost:3001/mcp/info
```

## Configuration Files

Ready-made configuration files are available in the `configs/` directory:
- `vscode-settings.json` - VS Code MCP configuration
- `claude-desktop-config.json` - Claude Desktop configuration

Copy the appropriate sections to your respective configuration files.