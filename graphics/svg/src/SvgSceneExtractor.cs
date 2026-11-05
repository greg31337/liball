namespace svg;

internal class SvgSceneExtractionState
{
    internal SvgScene Source;
    internal SvgResolvedDocument Document;
    internal SvgSceneExtractionOptions Options;
    internal int NodeCount;
    internal SvgSceneExtractionError Error;

    internal SvgSceneExtractionState(SvgScene source, SvgResolvedDocument document, SvgSceneExtractionOptions options)
    {
        Source = source;
        Document = document;
        Options = options;
        NodeCount = 0;
        Error = SvgSceneExtractionError.None;
    }
}
