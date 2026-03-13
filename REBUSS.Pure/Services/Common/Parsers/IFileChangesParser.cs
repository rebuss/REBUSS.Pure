using REBUSS.Pure.Services.Common.Models;

namespace REBUSS.Pure.Services.Common.Parsers
{
    public interface IFileChangesParser
    {
        List<FileChange> Parse(string json);
    }
}
