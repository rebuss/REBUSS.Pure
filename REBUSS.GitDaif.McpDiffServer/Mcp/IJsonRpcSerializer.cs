namespace REBUSS.GitDaif.McpDiffServer.Mcp
{
    /// <summary>
    /// Abstraction over JSON-RPC message serialization and deserialization.
    /// Follows DIP: consumers depend on this interface rather than the concrete System.Text.Json APIs.
    /// </summary>
    public interface IJsonRpcSerializer
    {
        T? Deserialize<T>(string json);
        string Serialize<T>(T value);
    }
}
