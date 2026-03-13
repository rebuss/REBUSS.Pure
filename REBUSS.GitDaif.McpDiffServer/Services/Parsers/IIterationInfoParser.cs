using REBUSS.GitDaif.McpDiffServer.Services.Models;

namespace REBUSS.GitDaif.McpDiffServer.Services.Parsers
{
    public interface IIterationInfoParser
    {
        IterationInfo ParseLast(string json);
    }
}
