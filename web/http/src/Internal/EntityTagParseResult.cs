using System.Collections.Generic;

namespace http;

internal class EntityTagParseResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int NextPosition;
    internal int ErrorPosition;
    internal int EmptyElements;
    internal HttpEntityTag Value = new HttpEntityTag();
    internal List<HttpEntityTag> Values = new List<HttpEntityTag>();
    internal bool IsWildcard;
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
}
