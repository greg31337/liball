namespace http;

internal class QualityValueParseResult
{
    internal HttpOperationStatus Status;
    internal int Consumed;
    internal int ErrorPosition;
    internal int Quality;
    internal bool IsMalformed;
    internal bool IsLimitExceeded;
}
