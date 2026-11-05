namespace asn1;

internal class NamedListParseResult
{
    internal bool Success;
    internal AsnNamedValue[] RootValues = [];
    internal AsnNamedValue[] ExtensionValues = [];
    internal AsnSourceSpan?[] RootValueSpans = [];
    internal AsnSourceSpan?[] ExtensionValueSpans = [];
    internal AsnToken[] ExtensionExceptionTokens = [];
    internal AsnToken? LastToken;
}
