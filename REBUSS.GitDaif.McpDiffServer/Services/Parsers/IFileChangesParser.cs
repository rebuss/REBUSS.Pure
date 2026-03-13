using REBUSS.GitDaif.McpDiffServer.Services.Models;

namespace REBUSS.GitDaif.McpDiffServer.Services.Parsers
{
    public interface IFileChangesParser
    {
        List<FileChange> Parse(string json);
    }
}
