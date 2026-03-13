using REBUSS.GitDaif.McpDiffServer.Mcp.Models;
using System.Text.Json;

namespace REBUSS.GitDaif.McpDiffServer.Mcp.Handlers
{
    /// <summary>
    /// Handles the JSON-RPC <c>tools/call</c> method.
    /// Resolves the named tool from registered <see cref="IMcpToolHandler"/> instances and delegates execution.
    /// </summary>
    public class ToolsCallMethodHandler : IMcpMethodHandler
    {
        private readonly Dictionary<string, IMcpToolHandler> _tools;
        private readonly IJsonRpcSerializer _serializer;

        public ToolsCallMethodHandler(IEnumerable<IMcpToolHandler> tools, IJsonRpcSerializer serializer)
        {
            _tools = tools.ToDictionary(h => h.ToolName);
            _serializer = serializer;
        }

        public string MethodName => "tools/call";

        public async Task<object> HandleAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var paramsElement = request.Params as JsonElement?;
            if (paramsElement == null)
                throw new InvalidOperationException("Missing parameters for tools/call");

            var toolCallParams = _serializer.Deserialize<ToolCallParams>(paramsElement.Value.GetRawText());
            if (toolCallParams == null)
                throw new InvalidOperationException("Invalid tool call parameters");

            if (!_tools.TryGetValue(toolCallParams.Name, out var handler))
                throw new InvalidOperationException($"Unknown tool: {toolCallParams.Name}");

            return await handler.ExecuteAsync(toolCallParams.Arguments, cancellationToken);
        }
    }
}
