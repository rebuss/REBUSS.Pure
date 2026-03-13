using REBUSS.Pure.Services.Common.Models;

namespace REBUSS.Pure.Services.Common.Parsers
{
    public interface IPullRequestMetadataParser
    {
        PullRequestMetadata Parse(string json);
        FullPullRequestMetadata ParseFull(string json);
    }
}
