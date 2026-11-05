using System.Collections.Generic;

namespace fix;

internal class ScoreLexResult
{
    internal bool Success;
    internal List<ScoreToken> Tokens = [];
    internal int ErrorOffset = -1;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
