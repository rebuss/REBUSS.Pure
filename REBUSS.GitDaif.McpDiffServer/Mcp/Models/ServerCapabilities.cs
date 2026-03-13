using System.Text.Json.Serialization;

namespace REBUSS.GitDaif.McpDiffServer.Mcp.Models
{
    /// <summary>
    /// MCP Server Capabilities
    /// </summary>
    public class ServerCapabilities
    {
        [JsonPropertyName("tools")]
        public ToolsCapability? Tools { get; set; }
    }
}
