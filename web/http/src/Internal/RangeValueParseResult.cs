using System.Collections.Generic;

namespace http;

internal class RangeValueParseResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int ErrorPosition;
    internal int EmptyElements;
    internal HttpRangeRequest Range = new HttpRangeRequest();
    internal HttpByteRange ByteRange = new HttpByteRange();
    internal HttpContentRange ContentRange = new HttpContentRange();
    internal List<string> Units = new List<string>();
    internal bool IsExtensionUnit;
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
}
