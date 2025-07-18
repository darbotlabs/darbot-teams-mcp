#!/bin/bash

# Test script for Darbot Teams MCP Server
# Tests both HTTP and stdio modes

echo "🧪 Testing Darbot Teams MCP Server"
echo "=================================="

cd "$(dirname "$0")"

# Build the project
echo "📦 Building project..."
dotnet build
if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi
echo "✅ Build successful"

# Test HTTP mode
echo ""
echo "🌐 Testing HTTP mode..."
echo "Starting HTTP server in background..."

# Start HTTP server in background
dotnet run --project src/DarbotTeamsMcp.Server &
SERVER_PID=$!

# Wait for server to start
sleep 8

# Test health endpoint
echo "Testing health endpoint..."
HEALTH_RESPONSE=$(curl -s http://localhost:3001/mcp/health)
if echo "$HEALTH_RESPONSE" | grep -q "healthy"; then
    echo "✅ Health check passed"
else
    echo "❌ Health check failed: $HEALTH_RESPONSE"
    kill $SERVER_PID 2>/dev/null
    exit 1
fi

# Test MCP initialize
echo "Testing MCP initialize..."
INIT_RESPONSE=$(curl -s -X POST http://localhost:3001/mcp \
    -H "Content-Type: application/json" \
    -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"clientInfo":{"name":"test-client","version":"1.0.0"}}}')

if echo "$INIT_RESPONSE" | grep -q "darbot-teams-mcp"; then
    echo "✅ MCP initialize successful"
else
    echo "❌ MCP initialize failed: $INIT_RESPONSE"
    kill $SERVER_PID 2>/dev/null
    exit 1
fi

# Test tools list
echo "Testing tools list..."
TOOLS_RESPONSE=$(curl -s -X POST http://localhost:3001/mcp \
    -H "Content-Type: application/json" \
    -d '{"jsonrpc":"2.0","id":2,"method":"tools/list"}')

TOOL_COUNT=$(echo "$TOOLS_RESPONSE" | jq -r '.result.tools | length' 2>/dev/null)
if [ "$TOOL_COUNT" -gt 40 ]; then
    echo "✅ Tools list successful ($TOOL_COUNT tools found)"
else
    echo "❌ Tools list failed or insufficient tools: $TOOL_COUNT"
    kill $SERVER_PID 2>/dev/null
    exit 1
fi

# Stop HTTP server
echo "Stopping HTTP server..."
kill $SERVER_PID 2>/dev/null
wait $SERVER_PID 2>/dev/null
echo "✅ HTTP server stopped"

# Test stdio mode
echo ""
echo "📡 Testing stdio mode..."

# Test stdio initialize
STDIO_INIT_RESPONSE=$(echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{"tools":{}},"clientInfo":{"name":"test-client","version":"1.0.0"}}}' | \
    timeout 10 dotnet run --project src/DarbotTeamsMcp.Server -- --stdio | head -1)

if echo "$STDIO_INIT_RESPONSE" | grep -q "darbot-teams-mcp"; then
    echo "✅ Stdio initialize successful"
else
    echo "❌ Stdio initialize failed: $STDIO_INIT_RESPONSE"
    exit 1
fi

# Test stdio tools list
STDIO_TOOLS_RESPONSE=$(echo '{"jsonrpc":"2.0","id":2,"method":"tools/list"}' | \
    timeout 10 dotnet run --project src/DarbotTeamsMcp.Server -- --stdio | head -1)

STDIO_TOOL_COUNT=$(echo "$STDIO_TOOLS_RESPONSE" | jq -r '.result.tools | length' 2>/dev/null)
if [ "$STDIO_TOOL_COUNT" -gt 40 ]; then
    echo "✅ Stdio tools list successful ($STDIO_TOOL_COUNT tools found)"
else
    echo "❌ Stdio tools list failed or insufficient tools: $STDIO_TOOL_COUNT"
    exit 1
fi

echo ""
echo "🎉 All tests passed!"
echo ""
echo "📋 Summary:"
echo "  - HTTP mode: ✅ Working (localhost:3001)"
echo "  - Stdio mode: ✅ Working (for VS Code integration)"
echo "  - Tools registered: $TOOL_COUNT"
echo "  - MCP protocol: ✅ JSON-RPC 2.0 compliant"
echo ""
echo "🚀 Ready for VS Code integration!"
echo "   Use the configuration in configs/vscode-settings.json"