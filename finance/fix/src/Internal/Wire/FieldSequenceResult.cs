using System.Collections.Generic;

namespace fix;

internal class FieldSequenceResult
{
    internal bool Success;
    internal List<FixField> Fields = [];
    internal string ErrorCode = "";
}
