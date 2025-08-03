#!/usr/bin/env node

const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

// Get the package root directory (go up from bin/ to package root)
const packageRoot = path.dirname(__dirname);

function showHelp() {
  console.log(`
🎮 Darbot Teams MCP Server
═══════════════════════════════

USAGE:
  npx darbot-teams-mcp [command]

COMMANDS:
  --stdio              Run in stdio mode (for VS Code integration)
  --http               Run in HTTP mode (localhost:3001)
  --vscode-setup       Auto-configure VS Code settings
  --test               Test the MCP server installation
  --help               Show this help message

EXAMPLES:
  # Auto-configure VS Code (recommended first step)
  npx darbot-teams-mcp --vscode-setup

  # Run in stdio mode for VS Code integration
  npx darbot-teams-mcp --stdio

  # Run in HTTP mode for web clients
  npx darbot-teams-mcp --http

  # Test the installation
  npx darbot-teams-mcp --test

QUICK START:
  1. npx darbot-teams-mcp --vscode-setup
  2. Restart VS Code
  3. Start using Teams commands in VS Code!

For more information: https://github.com/darbotlabs/darbot-teams-mcp
`);
}

function runVSCodeSetup() {
  console.log('🔧 Running VS Code setup...');
  const setupScript = path.join(packageRoot, 'scripts', 'vscode-setup.js');
  
  const child = spawn('node', [setupScript], {
    stdio: 'inherit',
    cwd: packageRoot
  });

  child.on('error', (error) => {
    console.error('❌ VS Code setup failed:', error.message);
    process.exit(1);
  });

  child.on('close', (code) => {
    process.exit(code);
  });
}

function runTest() {
  console.log('🧪 Running MCP server tests...');
  const testScript = path.join(packageRoot, 'test-mcp-server.sh');
  
  if (!fs.existsSync(testScript)) {
    console.error('❌ Test script not found:', testScript);
    process.exit(1);
  }

  const child = spawn('bash', [testScript], {
    stdio: 'inherit',
    cwd: packageRoot
  });

  child.on('error', (error) => {
    console.error('❌ Test failed:', error.message);
    process.exit(1);
  });

  child.on('close', (code) => {
    process.exit(code);
  });
}

function runMcpServer(mode) {
  console.log(`🚀 Starting Darbot Teams MCP Server in ${mode} mode...`);
  
  const serverProject = path.join(packageRoot, 'src', 'DarbotTeamsMcp.Server');
  
  if (!fs.existsSync(serverProject)) {
    console.error(`❌ Server project not found: ${serverProject}`);
    console.error(`
🔧 TROUBLESHOOTING:
  • Make sure the package was installed correctly
  • Try reinstalling: npm uninstall -g darbot-teams-mcp && npm install -g darbot-teams-mcp
  • Check .NET SDK is installed: dotnet --version
  • For help: npx darbot-teams-mcp --help
`);
    process.exit(1);
  }

  const args = ['run', '--project', serverProject];
  
  if (mode === 'stdio') {
    args.push('--', '--stdio');
    // Set MCP_MODE environment variable for stdio mode
    process.env.MCP_MODE = 'stdio';
    process.env.TEAMS_LOG_LEVEL = 'Warning'; // Reduce logging noise for stdio
  }

  const child = spawn('dotnet', args, {
    stdio: 'inherit',
    cwd: packageRoot,
    env: process.env
  });

  child.on('error', (error) => {
    if (error.code === 'ENOENT') {
      console.error(`
❌ .NET SDK not found!

📥 REQUIRED: Install .NET SDK first:
  • Download from: https://dotnet.microsoft.com/download
  • Minimum version: .NET 8.0
  • Verify installation: dotnet --version

✅ AFTER INSTALLING .NET:
  • Try again: npx darbot-teams-mcp ${process.argv.slice(2).join(' ')}
  • Or test: npx darbot-teams-mcp --test

🆘 NEED HELP?
  • Documentation: https://github.com/darbotlabs/darbot-teams-mcp
  • Issues: https://github.com/darbotlabs/darbot-teams-mcp/issues
`);
    } else {
      console.error(`❌ Failed to start MCP server: ${error.message}`);
      console.error(`
🔧 TROUBLESHOOTING:
  • Check .NET SDK: dotnet --version
  • Reinstall package: npm install -g darbot-teams-mcp
  • Check logs in the 'logs/' directory
  • Report issue: https://github.com/darbotlabs/darbot-teams-mcp/issues
`);
    }
    process.exit(1);
  });

  child.on('close', (code) => {
    if (code !== 0) {
      console.error(`
⚠️  MCP server exited with code ${code}

🔧 TROUBLESHOOTING:
  • Check logs in the 'logs/' directory
  • Try: npx darbot-teams-mcp --test
  • For help: npx darbot-teams-mcp --help
  • Report issue: https://github.com/darbotlabs/darbot-teams-mcp/issues
`);
    }
    process.exit(code);
  });
}

// Main execution logic
const args = process.argv.slice(2);

if (args.length === 0 || args.includes('--help') || args.includes('-h')) {
  showHelp();
} else if (args.includes('--vscode-setup')) {
  runVSCodeSetup();
} else if (args.includes('--test')) {
  runTest();
} else if (args.includes('--stdio')) {
  runMcpServer('stdio');
} else if (args.includes('--http')) {
  runMcpServer('http');
} else {
  console.error('❌ Unknown command:', args.join(' '));
  console.log('');
  showHelp();
  process.exit(1);
}