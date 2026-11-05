namespace svg;

internal class SvgSceneBuildState
{
    internal SvgResolvedDocument Document;
    internal SvgSceneBuildOptions Options;
    internal SvgSceneBuildResult Result;
    internal System.Collections.Generic.List<SvgNode> ActiveResources;
    internal bool LimitReached;

    internal SvgSceneBuildState(SvgResolvedDocument document, SvgSceneBuildOptions options, SvgSceneBuildResult result)
    {
        Document = document;
        Options = options;
        Result = result;
        ActiveResources = new System.Collections.Generic.List<SvgNode>();
        LimitReached = false;
    }
}
