using System.Collections.Generic;

namespace http;

internal class MediaTypeParseResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int ErrorPosition;
    internal int EmptyElements;
    internal HttpMediaType Value = new HttpMediaType();
    internal List<HttpWeightedValue> Values = new List<HttpWeightedValue>();
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
    internal bool IsDuplicateQuality;
    internal bool IsConflictingQuality;
}
