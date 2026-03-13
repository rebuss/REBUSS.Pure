using REBUSS.Pure.Services.Common.Models;

namespace REBUSS.Pure.Services.Common.Parsers
{
    public interface IIterationInfoParser
    {
        IterationInfo ParseLast(string json);
    }
}
