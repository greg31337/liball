namespace svg;

internal class SvgSceneLayerState
{
    internal SvgSceneLayerPlan Plan;
    internal SvgResolvedDocument Document;
    internal SvgSceneLayerOptions Options;
    internal SvgDrawingBoundsError Error;

    internal SvgSceneLayerState(SvgSceneLayerPlan plan, SvgResolvedDocument document, SvgSceneLayerOptions options)
    {
        Plan = plan;
        Document = document;
        Options = options;
        Error = SvgDrawingBoundsError.None;
    }
}
