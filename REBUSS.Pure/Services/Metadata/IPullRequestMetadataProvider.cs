using REBUSS.Pure.Services.Common.Models;

namespace REBUSS.Pure.Services.Metadata
{
    /// <summary>
    /// Interface for retrieving full Pull Request metadata.
    /// </summary>
    public interface IPullRequestMetadataProvider
    {
        /// <exception cref="Common.PullRequestNotFoundException">Thrown when PR is not found.</exception>
        Task<FullPullRequestMetadata> GetMetadataAsync(int prNumber, CancellationToken cancellationToken = default);
    }
}
