using System.Text;
using Microsoft.Extensions.Logging;
using REBUSS.GitDaif.McpDiffServer.AzureDevOpsIntegration.Services;
using REBUSS.GitDaif.McpDiffServer.Services.Models;
using REBUSS.GitDaif.McpDiffServer.Services.Parsers;

namespace REBUSS.GitDaif.McpDiffServer.Services
{
    /// <summary>
    /// Fetches real unified-diff content from Azure DevOps by:
    /// 1. Reading PR details (title, status, refs).
    /// 2. Reading the last iteration to get the base and target commit SHAs.
    /// 3. Enumerating changed files from the iteration changes endpoint.
    /// 4. For each file, fetching raw content at both commits.
    /// 5. Producing standard unified-diff text via <see cref="UnifiedDiffBuilder"/>.
    /// </summary>
    public class AzureDevOpsDiffProvider : IPullRequestDiffProvider
    {
        private readonly IAzureDevOpsApiClient _apiClient;
        private readonly IPullRequestMetadataParser _metadataParser;
        private readonly IIterationInfoParser _iterationParser;
        private readonly IFileChangesParser _changesParser;
        private readonly IUnifiedDiffBuilder _diffBuilder;
        private readonly ILogger<AzureDevOpsDiffProvider> _logger;

        public AzureDevOpsDiffProvider(
            IAzureDevOpsApiClient apiClient,
            IPullRequestMetadataParser metadataParser,
            IIterationInfoParser iterationParser,
            IFileChangesParser changesParser,
            IUnifiedDiffBuilder diffBuilder,
            ILogger<AzureDevOpsDiffProvider> logger)
        {
            _apiClient = apiClient;
            _metadataParser = metadataParser;
            _iterationParser = iterationParser;
            _changesParser = changesParser;
            _diffBuilder = diffBuilder;
            _logger = logger;
        }

        public async Task<PullRequestDiff> GetDiffAsync(int prNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching diff for PR #{PrNumber}", prNumber);

                var (metadata, files, baseCommit, targetCommit) = await FetchPullRequestDataAsync(prNumber);

                await BuildFileDiffsAsync(files, baseCommit, targetCommit, cancellationToken);

                return BuildDiff(metadata, files, baseCommit, targetCommit);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Pull Request #{PrNumber} not found", prNumber);
                throw new PullRequestNotFoundException($"Pull Request #{prNumber} not found", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching diff for PR #{PrNumber}", prNumber);
                throw;
            }
        }

        public async Task<PullRequestDiff> GetFileDiffAsync(int prNumber, string path, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching diff for file '{Path}' in PR #{PrNumber}", path, prNumber);

                var (metadata, files, baseCommit, targetCommit) = await FetchPullRequestDataAsync(prNumber);

                var normalizedPath = NormalizePath(path);
                var matchingFiles = files
                    .Where(f => NormalizePath(f.Path).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchingFiles.Count == 0)
                {
                    _logger.LogWarning("File '{Path}' not found in PR #{PrNumber}", path, prNumber);
                    throw new FileNotFoundInPullRequestException(
                        $"File '{path}' not found in Pull Request #{prNumber}");
                }

                await BuildFileDiffsAsync(matchingFiles, baseCommit, targetCommit, cancellationToken);

                return BuildDiff(metadata, matchingFiles, baseCommit, targetCommit);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Pull Request #{PrNumber} not found", prNumber);
                throw new PullRequestNotFoundException($"Pull Request #{prNumber} not found", ex);
            }
            catch (FileNotFoundInPullRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching diff for file '{Path}' in PR #{PrNumber}", path, prNumber);
                throw;
            }
        }

        private async Task<(PullRequestMetadata metadata, List<FileChange> files, string baseCommit, string targetCommit)>
            FetchPullRequestDataAsync(int prNumber)
        {
            var metadata  = _metadataParser.Parse(await _apiClient.GetPullRequestDetailsAsync(prNumber));
            var iteration = _iterationParser.ParseLast(await _apiClient.GetPullRequestIterationsAsync(prNumber));
            var files     = await FetchFileChangesAsync(prNumber, iteration.Id);

            return (metadata, files, iteration.BaseCommit, iteration.TargetCommit);
        }

        private static string NormalizePath(string path) => path.TrimStart('/');

        private async Task<List<FileChange>> FetchFileChangesAsync(int prNumber, int iterationId)
        {
            var changesJson = iterationId > 0
                ? await _apiClient.GetPullRequestIterationChangesAsync(prNumber, iterationId)
                : "{}";

            return _changesParser.Parse(changesJson);
        }

        private async Task BuildFileDiffsAsync(
            List<FileChange> files,
            string baseCommit,
            string targetCommit,
            CancellationToken cancellationToken)
        {
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(baseCommit) || string.IsNullOrEmpty(targetCommit))
                    continue;

                var baseContent   = await _apiClient.GetFileContentAtCommitAsync(baseCommit,   file.Path);
                var targetContent = await _apiClient.GetFileContentAtCommitAsync(targetCommit, file.Path);
                file.Diff = _diffBuilder.Build(file.Path, baseContent, targetContent);
            }
        }

        private static PullRequestDiff BuildDiff(
            PullRequestMetadata metadata,
            List<FileChange> files,
            string baseCommit,
            string targetCommit)
        {
            return new PullRequestDiff
            {
                Title         = metadata.Title,
                Status        = metadata.Status,
                SourceBranch  = metadata.SourceBranch,
                TargetBranch  = metadata.TargetBranch,
                SourceRefName = metadata.SourceRefName,
                TargetRefName = metadata.TargetRefName,
                Files         = files,
                DiffContent   = BuildDiffContent(files, baseCommit, targetCommit)
            };
        }

        private static string BuildDiffContent(List<FileChange> files, string baseCommit, string targetCommit)
        {
            var diffSections = files
                .Where(f => !string.IsNullOrEmpty(f.Diff))
                .Select(f => f.Diff)
                .ToList();

            return diffSections.Count > 0
                ? string.Join("\n", diffSections)
                : GenerateFallbackDiff(files, string.IsNullOrEmpty(baseCommit) || string.IsNullOrEmpty(targetCommit));
        }

        private static string GenerateFallbackDiff(List<FileChange> files, bool noCommitShas)
        {
            if (files.Count == 0) return string.Empty;

            var reason = noCommitShas ? "commit SHAs not resolved from iteration" : "file content unavailable";
            var sb = new StringBuilder();

            foreach (var f in files)
            {
                var p = f.Path.TrimStart('/');
                sb.AppendLine($"diff --git a/{p} b/{p}");
                sb.AppendLine($"--- a/{p}");
                sb.AppendLine($"+++ b/{p}");
                sb.AppendLine($"# {f.ChangeType} ({reason})");
            }

            return sb.ToString().TrimEnd();
        }
    }
}