#!/usr/bin/env node

const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

console.log('🚀 Setting up Darbot Teams MCP Server...');

// Get package root directory
const packageRoot = process.cwd();

// Check if .NET SDK is available
function checkDotnetSdk() {
  return new Promise((resolve, reject) => {
    const child = spawn('dotnet', ['--version'], { stdio: 'pipe' });
    
    child.on('error', (error) => {
      reject(new Error('.NET SDK not found. Please install .NET 8.0+ SDK from https://dotnet.microsoft.com/download'));
    });
    
    child.on('close', (code) => {
      if (code === 0) {
        resolve();
      } else {
        reject(new Error('Failed to verify .NET SDK installation'));
      }
    });
  });
}

// Build the .NET project
function buildProject() {
  return new Promise((resolve, reject) => {
    console.log('📦 Building .NET project...');
    
    const child = spawn('dotnet', ['build'], {
      stdio: 'inherit',
      cwd: packageRoot
    });
    
    child.on('error', reject);
    child.on('close', (code) => {
      if (code === 0) {
        console.log('✅ Build completed successfully!');
        resolve();
      } else {
        reject(new Error(`Build failed with exit code ${code}`));
      }
    });
  });
}

async function setup() {
  try {
    // Check for .NET SDK
    await checkDotnetSdk();
    console.log('✅ .NET SDK found');
    
    // Build the project
    await buildProject();
    
    console.log(`
🎉 Darbot Teams MCP Server installed successfully!

📋 WHAT'S NEXT:

1️⃣  QUICK SETUP (Recommended):
    npx darbot-teams-mcp --vscode-setup
    
2️⃣  MANUAL CONFIGURATION:
    npx darbot-teams-mcp --stdio     # For VS Code
    npx darbot-teams-mcp --http      # For web clients
    
3️⃣  TEST INSTALLATION:
    npx darbot-teams-mcp --test
    
4️⃣  GET HELP:
    npx darbot-teams-mcp --help

🔗 More Information:
  • Documentation: https://github.com/darbotlabs/darbot-teams-mcp
  • Issues: https://github.com/darbotlabs/darbot-teams-mcp/issues

✨ You now have 50+ Teams management commands available!
`);
    
  } catch (error) {
    console.error('❌ Setup failed:', error.message);
    
    if (error.message.includes('.NET SDK')) {
      console.log(`
📥 REQUIRED: Install .NET SDK first

DOWNLOAD FROM: https://dotnet.microsoft.com/download
MINIMUM VERSION: .NET 8.0

AFTER INSTALLING:
  • Verify: dotnet --version
  • Retry: npm install darbot-teams-mcp
  • Test: npx darbot-teams-mcp --test

ALTERNATIVE APPROACH:
  • Use git clone method instead of npm
  • See: https://github.com/darbotlabs/darbot-teams-mcp#installation

NEED HELP?
  • Documentation: https://github.com/darbotlabs/darbot-teams-mcp
  • Issues: https://github.com/darbotlabs/darbot-teams-mcp/issues
`);
    } else {
      console.log(`
🔧 TROUBLESHOOTING:
  • Check .NET SDK: dotnet --version
  • Try cleaning: dotnet clean && dotnet build
  • Check logs for more details
  • Check internet connection for package restore

GET HELP:
  • Documentation: https://github.com/darbotlabs/darbot-teams-mcp
  • Issues: https://github.com/darbotlabs/darbot-teams-mcp/issues
`);
    }
    
    process.exit(1);
  }
}

setup();