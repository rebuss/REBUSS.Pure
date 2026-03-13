using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using REBUSS.GitDaif.McpDiffServer.AzureDevOpsIntegration.Configuration;

namespace REBUSS.GitDaif.McpDiffServer.AzureDevOpsIntegration.Services
{
    /// <summary>
    /// HTTP client for Azure DevOps REST API.
    /// Expects a pre-configured HttpClient (BaseAddress + auth header) injected via IHttpClientFactory.
    /// </summary>
    public class AzureDevOpsApiClient : IAzureDevOpsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly AzureDevOpsOptions _options;
        private readonly ILogger<AzureDevOpsApiClient> _logger;

        public AzureDevOpsApiClient(
            HttpClient httpClient,
            IOptions<AzureDevOpsOptions> options,
            ILogger<AzureDevOpsApiClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> GetPullRequestDetailsAsync(int pullRequestId)
        {
            var url = $"{_options.ProjectName}/_apis/git/repositories/{_options.RepositoryName}" +
                      $"/pullRequests/{pullRequestId}?api-version=7.0";

            return await GetStringAsync(url);
        }

        public async Task<string> GetPullRequestIterationsAsync(int pullRequestId)
        {
            var url = $"{_options.ProjectName}/_apis/git/repositories/{_options.RepositoryName}" +
                      $"/pullRequests/{pullRequestId}/iterations?api-version=7.0";

            return await GetStringAsync(url);
        }

        public async Task<string> GetPullRequestIterationChangesAsync(int pullRequestId, int iterationId)
        {
            var url = $"{_options.ProjectName}/_apis/git/repositories/{_options.RepositoryName}" +
                      $"/pullRequests/{pullRequestId}/iterations/{iterationId}/changes?api-version=7.0";

            return await GetStringAsync(url);
        }

        public async Task<string> GetPullRequestCommitsAsync(int pullRequestId)
        {
            var url = $"{_options.ProjectName}/_apis/git/repositories/{_options.RepositoryName}" +
                      $"/pullRequests/{pullRequestId}/commits?api-version=7.0";

            return await GetStringAsync(url);
        }

        public async Task<string?> GetFileContentAtCommitAsync(string commitId, string filePath)
        {
            try
            {
                var encodedPath = Uri.EscapeDataString(filePath);
                var url = $"{_options.ProjectName}/_apis/git/repositories/{_options.RepositoryName}/items" +
                          $"?path={encodedPath}" +
                          $"&versionDescriptor.version={commitId}" +
                          $"&versionDescriptor.versionType=commit" +
                          $"&$format=text" +
                          $"&api-version=7.0";

                _logger.LogDebug("Fetching file content for {FilePath} at commit {CommitId}", filePath, commitId);

                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("File {FilePath} not found at commit {CommitId} (new/deleted file)", filePath, commitId);
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Items API returned {StatusCode} for {FilePath}@{CommitId}: {Error}",
                        response.StatusCode, filePath, commitId, error);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching file content for {FilePath} at {CommitId}", filePath, commitId);
                return null;
            }
        }

        public async Task<string?> GetFileContentAtRefAsync(string gitRef, string filePath)
        {
            try
            {
                var (version, versionType) = ResolveVersionDescriptor(gitRef);
                var encodedPath = Uri.EscapeDataString(filePath);
                var url = $"{_options.ProjectName}/_apis/git/repositories/{_options.RepositoryName}/items" +
                          $"?path={encodedPath}" +
                          $"&versionDescriptor.version={version}" +
                          $"&versionDescriptor.versionType={versionType}" +
                          $"&$format=text" +
                          $"&api-version=7.0";

                _logger.LogDebug("Fetching file content for {FilePath} at ref {GitRef} (type={VersionType})",
                    filePath, gitRef, versionType);

                var response = await _httpClient.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogDebug("File {FilePath} not found at ref {GitRef}", filePath, gitRef);
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Items API returned {StatusCode} for {FilePath}@{GitRef}: {Error}",
                        response.StatusCode, filePath, gitRef, error);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching file content for {FilePath} at ref {GitRef}", filePath, gitRef);
                return null;
            }
        }

        public static (string version, string versionType) ResolveVersionDescriptor(string gitRef)
        {
            if (gitRef.StartsWith("refs/tags/", StringComparison.OrdinalIgnoreCase))
                return (gitRef["refs/tags/".Length..], "tag");

            if (gitRef.StartsWith("refs/heads/", StringComparison.OrdinalIgnoreCase))
                return (gitRef["refs/heads/".Length..], "branch");

            if (gitRef.Length >= 7 && gitRef.All(c => char.IsAsciiHexDigit(c)))
                return (gitRef, "commit");

            return (gitRef, "branch");
        }

        /// <summary>
        /// Sends a GET request to the specified relative URL and returns the response body as a string.
        /// Logs the full URL at Debug level and throws on non-success status codes.
        /// </summary>
        private async Task<string> GetStringAsync(string relativeUrl)
        {
            var fullUrl = new Uri(_httpClient.BaseAddress!, relativeUrl);
            _logger.LogDebug("GET {FullUrl}", fullUrl);

            var response = await _httpClient.GetAsync(relativeUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Azure DevOps API returned {StatusCode}: {ErrorContent}",
                    response.StatusCode, errorContent);
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}