using System;
using System.Collections.Generic;

namespace fix;

internal class SessionHeaderValidationResult
{
    internal bool IsValid;
    internal bool IsPossibleDuplicate;
    internal bool HasSendingTime;
    internal DateTime SendingTimeUtc;
    internal bool HasOrigSendingTime;
    internal DateTime OrigSendingTimeUtc;
    internal List<ValidationIssue> Issues = [];
}
