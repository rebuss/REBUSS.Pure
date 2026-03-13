using REBUSS.GitDaif.McpDiffServer.Mcp.Models;

namespace REBUSS.GitDaif.McpDiffServer.Mcp
{
    /// <summary>
    /// Represents an MCP tool that can be listed and executed by the server.
    /// </summary>
    public interface IMcpToolHandler
    {
        string ToolName { get; }
        McpTool GetToolDefinition();
        Task<ToolResult> ExecuteAsync(
            Dictionary<string, object>? arguments,
            CancellationToken cancellationToken = default);
    }
}
