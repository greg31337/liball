namespace cookies;

internal class CookieScanResult
{
    internal bool Success;
    internal bool InvalidBounds;
    internal bool LimitExceeded;
    internal int SegmentStart;
    internal int SegmentLength;
    internal int NameStart;
    internal int NameLength;
    internal bool HasValue;
    internal int ValueStart;
    internal int ValueLength;
    internal int NextOffset;
}
