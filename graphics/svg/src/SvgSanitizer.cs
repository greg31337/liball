namespace svg;

internal class SvgSanitizationState
{
    internal SvgSanitizationResult Result;
    internal SvgSanitizationOptions Options;
    internal SvgNodeApi Nodes;
    internal SvgElementCatalog Elements;
    internal int VisitedNodes;

    internal SvgSanitizationState(SvgSanitizationResult result, SvgSanitizationOptions options)
    {
        Result = result;
        Options = options;
        Nodes = new SvgNodeApi();
        Elements = new SvgCatalogFactory().CreateElementCatalog();
        VisitedNodes = 0;
    }
}
