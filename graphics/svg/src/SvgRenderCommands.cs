namespace svg;

internal class SvgRenderCommandState
{
    internal SvgRenderCommandList Result;
    internal SvgRenderCommandOptions Options;

    internal SvgRenderCommandState(SvgRenderCommandList result, SvgRenderCommandOptions options)
    {
        Result = result;
        Options = options;
    }
}
