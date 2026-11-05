namespace http;

internal class MultipartHeaderResult
{
    internal int NextPosition;
    internal bool HasMediaType;
    internal HttpMediaType? MediaType;
    internal bool HasContentRange;
    internal HttpContentRange? ContentRange;
}
