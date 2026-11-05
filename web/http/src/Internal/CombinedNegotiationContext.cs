namespace http;

internal class CombinedNegotiationContext
{
    internal bool HasMedia;
    internal bool HasCharset;
    internal bool HasEncoding;
    internal bool HasLanguage;
    internal MediaTypeParseResult Media = new MediaTypeParseResult();
    internal MediaTypeParseResult Charsets = new MediaTypeParseResult();
    internal MediaTypeParseResult Encodings = new MediaTypeParseResult();
    internal LanguageRangeParseResult Languages = new LanguageRangeParseResult();
}
