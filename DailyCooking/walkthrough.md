# Walkthrough: Unity MCP Integration

We have successfully integrated the `CoplayDev/unity-mcp` package and configured the Model Context Protocol (MCP) server for this workspace.

## Changes Made

### 1. Package Manifest Updated
Modified [manifest.json](file:///d:/Unity/Unity%20Project/DailyCooking/DailyCooking/Packages/manifest.json) to add the `com.coplaydev.unity-mcp` Git dependency:
```json
"com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main"
```

### 2. Workspace MCP Config Created
Created a project-specific configuration file at [.agents/mcp_config.json](file:///d:/Unity/Unity%20Project/DailyCooking/DailyCooking/.agents/mcp_config.json) to register the `UnityMCP` server:
```json
{
  "mcpServers": {
    "UnityMCP": {
      "command": "uvx",
      "args": [
        "--from",
        "mcpforunityserver",
        "mcp-for-unity",
        "--transport",
        "stdio"
      ]
    }
  }
}
```

### 3. Custom EditorPrefs Tool Exposed
Created a custom C# Editor tool at [EditorPrefsMCPTool.cs](file:///d:/Unity/Unity%20Project/DailyCooking/DailyCooking/Assets/Editor/EditorPrefsMCPTool.cs) to allow the AI agent to interact with `EditorPrefs` via the `manage_editorprefs` tool.

## How to Verify inside Unity Editor

1. **Open/Focus Unity Editor**:
   - Unity will automatically detect the changes to `manifest.json` and download the `com.coplaydev.unity-mcp` package.
   - It will also compile the new `EditorPrefsMCPTool.cs` script.
2. **Access MCP Configuration**:
   - Go to the menu bar: **Window > MCP for Unity**.
   - Check that the server is active and running.
3. **Verify AI Agent connection**:
   - In your next prompt/interaction, Antigravity will automatically be able to locate the local Unity MCP server and connect to it using the tools defined in the project, including the new `manage_editorprefs` tool.

## Using the `manage_editorprefs` Tool

Once the Unity Editor compiles the script and the MCP server is running, the AI agent can call the `manage_editorprefs` tool with the following parameters:

- **Get an EditorPref**:
  ```json
  {
    "action": "get",
    "key": "MCPForUnity.UseHttpTransport",
    "type": "bool"
  }
  ```
- **Set an EditorPref**:
  ```json
  {
    "action": "set",
    "key": "MCPForUnity.UseHttpTransport",
    "type": "bool",
    "value": true
  }
  ```
- **Check if key exists**:
  ```json
  {
    "action": "has",
    "key": "MCPForUnity.SetupCompleted"
  }
  ```
- **Delete an EditorPref**:
  ```json
  {
    "action": "delete",
    "key": "SomeOldKey"
  }
  ```

