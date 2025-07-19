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

QUICK START:
  # Auto-configure VS Code
  npx darbot-teams-mcp --vscode-setup
  
  # Or run manually in stdio mode
  npx darbot-teams-mcp --stdio
  
  # Test the installation
  npx darbot-teams-mcp --test

For more help: npx darbot-teams-mcp --help
`);
    
  } catch (error) {
    console.error('❌ Setup failed:', error.message);
    
    if (error.message.includes('.NET SDK')) {
      console.log(`
📥 Please install .NET SDK first:
  - Download from: https://dotnet.microsoft.com/download
  - Minimum version: .NET 8.0
  - Then run: npm install darbot-teams-mcp
`);
    }
    
    process.exit(1);
  }
}

setup();