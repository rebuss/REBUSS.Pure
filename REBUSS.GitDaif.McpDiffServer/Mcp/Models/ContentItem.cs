using System.Text.Json.Serialization;

namespace REBUSS.GitDaif.McpDiffServer.Mcp.Models
{
    public class ContentItem
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
