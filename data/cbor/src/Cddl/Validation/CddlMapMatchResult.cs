using System.Collections.Generic;

namespace cbor;

internal class CddlMapMatchResult
{
    internal List<CddlMapMatchState> States =
        new List<CddlMapMatchState>();
    internal List<CddlValidationIssue> Issues =
        new List<CddlValidationIssue>();
    internal bool CutMatched;

    internal CddlMapMatchResult()
    {
    }
}
