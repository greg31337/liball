using System.Collections.Generic;

namespace cbor;

internal class CddlGroupMatchResult
{
    internal List<int> Positions = new List<int>();
    internal List<CddlValidationIssue> Issues =
        new List<CddlValidationIssue>();

    internal CddlGroupMatchResult()
    {
    }
}
