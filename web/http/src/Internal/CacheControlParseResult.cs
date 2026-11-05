namespace http;

internal class CacheControlParseResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int ErrorPosition;
    internal HttpCacheControl Value = new HttpCacheControl();
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
}
