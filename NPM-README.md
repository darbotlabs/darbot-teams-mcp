# NPM Package - Darbot Teams MCP

Easy NPM installation for Darbot Teams MCP Server with automatic VS Code integration.

## Quick Installation

```bash
# Install globally
npm install -g darbot-teams-mcp

# Auto-configure VS Code
npx darbot-teams-mcp --vscode-setup

# Test the installation
npx darbot-teams-mcp --test
```

## Commands

- `npx darbot-teams-mcp --stdio` - Run in stdio mode (VS Code)
- `npx darbot-teams-mcp --http` - Run in HTTP mode
- `npx darbot-teams-mcp --vscode-setup` - Auto-configure VS Code
- `npx darbot-teams-mcp --test` - Run test suite
- `npx darbot-teams-mcp --help` - Show help

## Default Configuration

The package uses these secure defaults for initial testing:

```json
{
  "TEAMS_CLIENT_ID": "04b07795-8ddb-461a-bbee-02f9e1bf7b46",
  "TEAMS_TENANT_ID": "common", 
  "TEAMS_SIMULATION_MODE": "true",
  "TEAMS_REQUIRE_AUTHENTICATION": "false",
  "TEAMS_LOG_LEVEL": "Warning"
}
```

## Production Configuration

For production use, override environment variables:

```bash
TEAMS_SIMULATION_MODE=false TEAMS_REQUIRE_AUTHENTICATION=true npx darbot-teams-mcp --stdio
```

Or set them in your VS Code settings after running `--vscode-setup`.