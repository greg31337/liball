namespace svg;

/// <summary>Configures bounded typed-attribute parsing and element metadata.</summary>
public class SvgTypedAttributeOptions
{
    /// <summary>The element metadata used for applicability, requirements, and defaults.</summary>
    public SvgElementCatalog ElementCatalog;
    /// <summary>The geometry limits used for path and points parsing.</summary>
    public SvgGeometryLimits GeometryLimits;
    /// <summary>The maximum length of one parsed token.</summary>
    public int MaxTokenCharacters;
    /// <summary>The maximum number of items in one typed list.</summary>
    public int MaxListItems;
    /// <summary>The maximum number of functions in one transform list.</summary>
    public int MaxTransforms;

    /// <summary>Creates secure typed-attribute parsing defaults.</summary>
    public SvgTypedAttributeOptions()
    {
        ElementCatalog = new SvgCatalogFactory().CreateElementCatalog();
        GeometryLimits = new SvgGeometryLimits();
        MaxTokenCharacters = 4096;
        MaxListItems = 1000000;
        MaxTransforms = 100000;
    }
}

/// <summary>Contains one source-preserving, tagged typed attribute result.</summary>
/// <remarks>
/// <see cref="Value"/> is interpreted according to <see cref="ValueKind"/>. A failed
/// parse may retain a parser-produced valid prefix in that field.
/// </remarks>
public class SvgTypedAttributeValue
{
    /// <summary>The element for which the value was requested.</summary>
    public SvgNode Node;
    /// <summary>The requested authored attribute, or <c>null</c> when absent.</summary>
    public SvgAttribute? Attribute;
    /// <summary>The authored attribute supplying the effective text, including a related default, or <c>null</c>.</summary>
    public SvgAttribute? SourceAttribute;
    /// <summary>The applicable catalog definition, or <c>null</c> for an unknown attribute.</summary>
    public SvgElementAttributeDefinition? Definition;
    /// <summary>The requested namespace URI.</summary>
    public string NamespaceUri;
    /// <summary>The requested case-sensitive local name.</summary>
    public string LocalName;
    /// <summary>The exact unexpanded text inside the requested authored attribute's quotes, or an empty string.</summary>
    public string OriginalText;
    /// <summary>The XML-normalized authored or default text that was parsed, or an empty string.</summary>
    public string EffectiveText;
    /// <summary>The origin of the effective text.</summary>
    public SvgTypedAttributeOrigin Origin;
    /// <summary>The typed payload tag.</summary>
    public SvgTypedAttributeKind ValueKind;
    /// <summary>The typed payload, or <c>null</c> when unavailable.</summary>
    public object? Value;
    /// <summary>Whether the requested attribute is applicable according to the current element catalog.</summary>
    public bool Applicable;
    /// <summary>Whether an effective value is available.</summary>
    public bool HasValue;
    /// <summary>Whether the effective value parsed successfully or no value was required.</summary>
    public bool Success;
    /// <summary>The parser failure offset within <see cref="EffectiveText"/>, or <c>-1</c>.</summary>
    public int ErrorOffset;
    /// <summary>The element version used to compute this cached result.</summary>
    public long NodeVersion;
    /// <summary>Located diagnostics produced while obtaining or parsing the value.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgTypedAttributeValue(SvgNode node, string namespaceUri, string localName)
    {
        Node = node;
        Attribute = null;
        SourceAttribute = null;
        Definition = null;
        NamespaceUri = namespaceUri;
        LocalName = localName;
        OriginalText = "";
        EffectiveText = "";
        Origin = SvgTypedAttributeOrigin.Missing;
        ValueKind = SvgTypedAttributeKind.Missing;
        Value = null;
        Applicable = false;
        HasValue = false;
        Success = true;
        ErrorOffset = -1;
        NodeVersion = node.Version;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }
}

/// <summary>Reports cache activity for one typed-attribute accessor.</summary>
public class SvgTypedAttributeStatistics
{
    /// <summary>The number of requests served from the node-version cache.</summary>
    public long CacheHits;
    /// <summary>The number of requests that computed a new result.</summary>
    public long CacheMisses;
    /// <summary>The number of effective texts passed to a typed parser.</summary>
    public long ParseCount;
    /// <summary>The current number of cached attribute results.</summary>
    public int EntryCount;

    /// <summary>Creates a statistics snapshot.</summary>
    /// <param name="cacheHits">The cache-hit count.</param>
    /// <param name="cacheMisses">The cache-miss count.</param>
    /// <param name="parseCount">The typed-parse count.</param>
    /// <param name="entryCount">The current cache entry count.</param>
    public SvgTypedAttributeStatistics(long cacheHits, long cacheMisses, long parseCount, int entryCount)
    {
        CacheHits = cacheHits;
        CacheMisses = cacheMisses;
        ParseCount = parseCount;
        EntryCount = entryCount;
    }
}
