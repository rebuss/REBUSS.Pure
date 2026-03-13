using System.Text.Json.Serialization;

namespace REBUSS.GitDaif.McpDiffServer.Mcp.Models
{
    /// <summary>
    /// MCP Tool Result
    /// </summary>
    public class ToolResult
    {
        [JsonPropertyName("content")]
        public List<ContentItem> Content { get; set; } = new();

        [JsonPropertyName("isError")]
        public bool IsError { get; set; }
    }
}
