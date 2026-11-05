namespace xml;

internal class XsltExecutableConformance
{
    internal XsltConformanceLevel Level;
    internal bool SchemaAware;
    internal bool Streaming;
    internal bool HasBackwardsCompatibleCode;
    internal bool HasForwardsCompatibleCode;
    internal bool UsesPackages;
    internal bool UsesSchemaImports;
    internal bool DeclaredModes;

    internal XsltExecutableConformance()
    {
        Level = XsltConformanceLevel.Basic;
        SchemaAware = false;
        Streaming = false;
        HasBackwardsCompatibleCode = false;
        HasForwardsCompatibleCode = false;
        UsesPackages = false;
        UsesSchemaImports = false;
        DeclaredModes = false;
    }
}

internal class XsltSourceMapEntry
{
    internal object Item;
    internal string Kind;
    internal string? SourceUri;
    internal SourceRange? Range;

    internal XsltSourceMapEntry(object item, string kind, string? sourceUri, SourceRange? range)
    {
        Item = item;
        Kind = kind;
        SourceUri = sourceUri;
        Range = range;
    }
}
