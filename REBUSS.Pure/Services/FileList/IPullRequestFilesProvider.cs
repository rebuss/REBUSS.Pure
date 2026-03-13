using REBUSS.Pure.Services.FileList.Models;

namespace REBUSS.Pure.Services.FileList
{
    /// <summary>
    /// Interface for retrieving classified file information for a Pull Request.
    /// </summary>
    public interface IPullRequestFilesProvider
    {
        /// <exception cref="Common.PullRequestNotFoundException">Thrown when PR is not found.</exception>
        Task<PullRequestFiles> GetFilesAsync(int prNumber, CancellationToken cancellationToken = default);
    }
}
