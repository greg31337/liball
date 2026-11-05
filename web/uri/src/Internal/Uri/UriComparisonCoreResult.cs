namespace uri;

internal class UriComparisonCoreResult
{
    internal bool Equal;
    internal UriComponent DifferentComponent;

    internal UriComparisonCoreResult(bool equal, UriComponent differentComponent)
    {
        Equal = equal;
        DifferentComponent = differentComponent;
    }
}
