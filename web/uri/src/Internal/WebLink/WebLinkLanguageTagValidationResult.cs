namespace uri;

internal class WebLinkLanguageTagValidationResult
{
    internal bool Valid;
    internal int ErrorOffset;
    internal int ErrorLength;

    internal WebLinkLanguageTagValidationResult(
        bool valid,
        int errorOffset,
        int errorLength)
    {
        Valid = valid;
        ErrorOffset = errorOffset;
        ErrorLength = errorLength;
    }
}
