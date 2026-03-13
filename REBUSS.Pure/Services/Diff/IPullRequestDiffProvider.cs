using REBUSS.Pure.Services.Common.Models;

namespace REBUSS.Pure.Services.Diff
{
    /// <summary>
    /// Interface for retrieving Pull Request diffs.
    /// </summary>
    public interface IPullRequestDiffProvider
    {
        /// <exception cref="Common.PullRequestNotFoundException">Thrown when PR is not found.</exception>
        Task<PullRequestDiff> GetDiffAsync(int prNumber, CancellationToken cancellationToken = default);

        /// <exception cref="Common.PullRequestNotFoundException">Thrown when PR is not found.</exception>
        /// <exception cref="Common.FileNotFoundInPullRequestException">Thrown when the file does not exist in the PR.</exception>
        Task<PullRequestDiff> GetFileDiffAsync(int prNumber, string path, CancellationToken cancellationToken = default);
    }
}
