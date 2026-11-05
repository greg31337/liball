using System;

namespace http;

internal class HttpDateParseResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int ErrorPosition;
    internal DateTimeOffset Value;
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
}
