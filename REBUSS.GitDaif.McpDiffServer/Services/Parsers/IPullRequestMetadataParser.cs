using REBUSS.GitDaif.McpDiffServer.Services.Models;

namespace REBUSS.GitDaif.McpDiffServer.Services.Parsers
{
    public interface IPullRequestMetadataParser
    {
        PullRequestMetadata Parse(string json);
        FullPullRequestMetadata ParseFull(string json);
    }
}
