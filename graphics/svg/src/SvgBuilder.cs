namespace svg;

internal class SvgBuilderStyleResult
{
    internal bool Success;
    internal string Text;

    internal SvgBuilderStyleResult(bool success, string text)
    {
        Success = success;
        Text = text;
    }
}
