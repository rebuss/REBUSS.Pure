using Microsoft.Extensions.Logging;
using REBUSS.GitDaif.McpDiffServer.Mcp;
using REBUSS.GitDaif.McpDiffServer.Mcp.Models;
using REBUSS.GitDaif.McpDiffServer.Services;
using REBUSS.GitDaif.McpDiffServer.Services.Models;
using REBUSS.GitDaif.McpDiffServer.Tools.Models;
using System.Text.Json;

namespace REBUSS.GitDaif.McpDiffServer.Tools
{
    /// <summary>
    /// Handles the execution of the get_file_diff MCP tool.
    /// Validates input, delegates to <see cref="IPullRequestDiffProvider"/>,
    /// and formats the result as text or structured JSON for a single file.
    /// </summary>
    public class GetFileDiffToolHandler : IMcpToolHandler
    {
        private readonly IPullRequestDiffProvider _diffProvider;
        private readonly ILogger<GetFileDiffToolHandler> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public string ToolName => "get_file_diff";

        public GetFileDiffToolHandler(
            IPullRequestDiffProvider diffProvider,
            ILogger<GetFileDiffToolHandler> logger)
        {
            _diffProvider = diffProvider;
            _logger = logger;
        }

        public McpTool GetToolDefinition() => new()
        {
            Name = ToolName,
            Description = "Retrieves the diff for a single file in a specific Pull Request from Azure DevOps. " +
                          "Returns the diff scoped to the specified file path. " +
                          "Use the optional 'format' parameter to choose the output format: " +
                          "'text' (default) returns a human-readable summary followed by unified diff content; " +
                          "'json' or 'structured' returns a structured JSON object with the file diff.",
            InputSchema = new ToolInputSchema
            {
                Type = "object",
                Properties = new Dictionary<string, ToolProperty>
                {
                    ["prNumber"] = new ToolProperty
                    {
                        Type = "integer",
                        Description = "The Pull Request number/ID to retrieve the diff for"
                    },
                    ["path"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "The repository-relative path of the file (e.g. 'src/Cache/CacheService.cs')"
                    },
                    ["format"] = new ToolProperty
                    {
                        Type = "string",
                        Description = "Output format for the diff result. " +
                                      "'text' (default) returns a human-readable summary + unified diff. " +
                                      "'json' or 'structured' returns a structured JSON object with prNumber and per-file diffs.",
                        Enum = new List<string> { "text", "json", "structured" },
                        Default = "text"
                    }
                },
                Required = new List<string> { "prNumber", "path" }
            }
        };

        public async Task<ToolResult> ExecuteAsync(
            Dictionary<string, object>? arguments,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!TryExtractPrNumber(arguments, out var prNumber, out var error))
                    return CreateErrorResult(error);

                if (!TryExtractPath(arguments!, out var path, out error))
                    return CreateErrorResult(error);

                var format = ExtractStringArgument(arguments!, "format", "text");

                _logger.LogInformation(
                    "Fetching diff for file '{Path}' in PR #{PrNumber} (format={Format})",
                    path, prNumber, format);

                var diff = await _diffProvider.GetFileDiffAsync(prNumber, path, cancellationToken);

                return format.ToLowerInvariant() switch
                {
                    "json" or "structured" => BuildStructuredResult(prNumber, diff),
                    _ => BuildTextResult(prNumber, path, diff)
                };
            }
            catch (PullRequestNotFoundException ex)
            {
                _logger.LogWarning(ex, "Pull request not found");
                return CreateErrorResult($"Pull Request not found: {ex.Message}");
            }
            catch (FileNotFoundInPullRequestException ex)
            {
                _logger.LogWarning(ex, "File not found in pull request");
                return CreateErrorResult($"File not found in Pull Request: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing get_file_diff tool");
                return CreateErrorResult($"Error retrieving file diff: {ex.Message}");
            }
        }

        // --- Input extraction -----------------------------------------------------

        private bool TryExtractPrNumber(
            Dictionary<string, object>? arguments,
            out int prNumber,
            out string errorMessage)
        {
            prNumber = 0;
            errorMessage = string.Empty;

            if (arguments == null || !arguments.TryGetValue("prNumber", out var prNumberObj))
            {
                errorMessage = "Missing required parameter: prNumber";
                return false;
            }

            try
            {
                prNumber = prNumberObj is JsonElement jsonElement
                    ? jsonElement.GetInt32()
                    : Convert.ToInt32(prNumberObj);
            }
            catch
            {
                errorMessage = "Invalid prNumber parameter: must be an integer";
                return false;
            }

            if (prNumber <= 0)
            {
                errorMessage = "prNumber must be greater than 0";
                return false;
            }

            return true;
        }

        private static bool TryExtractPath(
            Dictionary<string, object> arguments,
            out string path,
            out string errorMessage)
        {
            path = string.Empty;
            errorMessage = string.Empty;

            if (!arguments.TryGetValue("path", out var pathObj))
            {
                errorMessage = "Missing required parameter: path";
                return false;
            }

            path = pathObj is JsonElement jsonElement
                ? jsonElement.GetString() ?? string.Empty
                : pathObj?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                errorMessage = "path parameter must not be empty";
                return false;
            }

            return true;
        }

        private static string ExtractStringArgument(
            Dictionary<string, object> arguments, string key, string defaultValue)
        {
            if (!arguments.TryGetValue(key, out var value))
                return defaultValue;

            if (value is JsonElement jsonElement)
            {
                return jsonElement.ValueKind == JsonValueKind.String
                    ? jsonElement.GetString() ?? defaultValue
                    : defaultValue;
            }

            return value?.ToString() ?? defaultValue;
        }

        // --- Result builders ------------------------------------------------------

        private static ToolResult BuildTextResult(int prNumber, string path, PullRequestDiff diff)
        {
            var lines = new List<string>
            {
                $"Pull Request #{prNumber} File Diff: {path}",
                "",
                $"Files Changed: {diff.Files.Count}",
                "",
                "Changed Files:"
            };

            foreach (var file in diff.Files)
                lines.Add($"  - {file.Path} ({file.ChangeType})");

            lines.Add("");
            lines.Add(new string('=', 80));
            lines.Add("DIFF CONTENT:");
            lines.Add(new string('=', 80));
            lines.Add("");
            lines.Add(diff.DiffContent);

            return CreateSuccessResult(string.Join(Environment.NewLine, lines));
        }

        private static ToolResult BuildStructuredResult(int prNumber, PullRequestDiff diff)
        {
            var structured = new StructuredDiffResult
            {
                PrNumber = prNumber,
                Files = diff.Files.Select(f => new StructuredFileChange
                {
                    Path = f.Path,
                    ChangeType = f.ChangeType,
                    Diff = f.Diff
                }).ToList()
            };

            return CreateSuccessResult(JsonSerializer.Serialize(structured, JsonOptions));
        }

        private static ToolResult CreateSuccessResult(string text) => new()
        {
            Content = new List<ContentItem> { new() { Type = "text", Text = text } },
            IsError = false
        };

        private static ToolResult CreateErrorResult(string errorMessage) => new()
        {
            Content = new List<ContentItem> { new() { Type = "text", Text = $"Error: {errorMessage}" } },
            IsError = true
        };
    }
}
