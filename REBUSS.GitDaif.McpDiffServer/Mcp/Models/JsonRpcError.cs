using System.Text.Json.Serialization;

namespace REBUSS.GitDaif.McpDiffServer.Mcp.Models
{
    /// <summary>
    /// JSON-RPC Error object
    /// </summary>
    public class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }
}
