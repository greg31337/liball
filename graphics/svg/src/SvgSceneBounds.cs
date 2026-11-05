namespace svg;

internal class SvgSceneBoundsState
{
    internal SvgScene Scene;
    internal SvgResolvedDocument Document;
    internal SvgSceneBoundsOptions Options;
    internal SvgDrawingBoundsError Error;

    internal SvgSceneBoundsState(SvgScene scene, SvgResolvedDocument document, SvgSceneBoundsOptions options)
    {
        Scene = scene;
        Document = document;
        Options = options;
        Error = SvgDrawingBoundsError.None;
    }
}

internal class SvgSceneRectResult
{
    internal bool Success;
    internal SvgRect? Bounds;

    internal SvgSceneRectResult(bool success, SvgRect? bounds)
    {
        Success = success;
        Bounds = bounds;
    }
}
