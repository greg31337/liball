using System.Collections.Generic;

namespace http;

internal class LanguageRangeParseResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int ErrorPosition;
    internal int EmptyElements;
    internal List<HttpLanguageRange> Values = new List<HttpLanguageRange>();
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
}
