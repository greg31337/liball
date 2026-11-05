using System;
using System.Collections.Generic;

namespace http;

internal class CommonFieldParseResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int ErrorPosition;
    internal int EmptyElements;
    internal long Number;
    internal DateTimeOffset Date;
    internal bool IsDate;
    internal List<HttpViaEntry> ViaEntries = new List<HttpViaEntry>();
    internal List<HttpExpectationValue> Expectations = new List<HttpExpectationValue>();
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
}
