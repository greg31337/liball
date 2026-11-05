namespace http;

internal class MultipartBoundaryMatch
{
    internal bool IsMatch;
    internal bool IsClosing;
    internal int BoundaryPosition;
    internal int NextPosition;
}
