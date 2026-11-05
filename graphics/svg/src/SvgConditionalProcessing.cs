namespace svg;

internal class SvgConditionalProcessingState
{
    internal int Checks;
    internal bool LimitExceeded;

    internal SvgConditionalProcessingState()
    {
        Checks = 0;
        LimitExceeded = false;
    }
}
