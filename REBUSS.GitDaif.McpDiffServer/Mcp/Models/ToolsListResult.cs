using System.Text.Json.Serialization;

namespace REBUSS.GitDaif.McpDiffServer.Mcp.Models
{
    /// <summary>
    /// Tools List Result
    /// </summary>
    public class ToolsListResult
    {
        [JsonPropertyName("tools")]
        public List<McpTool> Tools { get; set; } = new();
    }
}
