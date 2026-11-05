namespace svg;

internal class SvgRegionHitResult
{
    internal bool Success;
    internal SvgHitTestError Error;
    internal bool Contains;

    internal SvgRegionHitResult(bool success, SvgHitTestError error, bool contains)
    {
        Success = success;
        Error = error;
        Contains = contains;
    }
}
