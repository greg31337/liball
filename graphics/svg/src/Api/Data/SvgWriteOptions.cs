namespace svg;

/// <summary>Configures namespace-repairing XML output.</summary>
public class SvgWriteOptions
{
    /// <summary>The high-level output mode.</summary>
    public SvgWriteMode Mode;
    /// <summary>The XML declaration policy.</summary>
    public SvgXmlDeclarationPolicy DeclarationPolicy;
    /// <summary>The document type policy.</summary>
    public SvgWriteDoctypePolicy DoctypePolicy;
    /// <summary>The empty-element policy.</summary>
    public SvgEmptyElementPolicy EmptyElementPolicy;
    /// <summary>The attribute quote policy.</summary>
    public SvgAttributeQuoteStyle QuoteStyle;
    /// <summary>The attribute ordering policy.</summary>
    public SvgWriteAttributeOrder AttributeOrder;
    /// <summary>Precision and error bounds used for typed SVG and CSS values.</summary>
    public SvgCanonicalizeOptions Values;
    /// <summary>Whether valid caller-authored namespace declarations are retained when not required.</summary>
    public bool PreserveNamespaceDeclarations;
    /// <summary>Whether element-only content receives structural indentation.</summary>
    public bool Indent;
    /// <summary>The text repeated for each indentation depth.</summary>
    public string IndentText;
    /// <summary>The line separator used for generated formatting whitespace.</summary>
    public string NewLine;
    /// <summary>Whether a space precedes the slash in a self-closing tag.</summary>
    public bool SpaceBeforeEmptyElementSlash;
    /// <summary>The declaration encoding label.</summary>
    public string DeclarationEncoding;
    /// <summary>The declaration standalone text, or an empty string to omit it.</summary>
    public string DeclarationStandalone;
    /// <summary>The positive maximum traversed node count.</summary>
    public int MaximumNodes;
    /// <summary>The positive maximum written attribute count.</summary>
    public int MaximumAttributes;
    /// <summary>The positive maximum element depth.</summary>
    public int MaximumDepth;
    /// <summary>The positive maximum UTF-16 output character count.</summary>
    public long MaximumCharacters;
    /// <summary>The synchronous cancellation token.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates deterministic namespace-repairing XML defaults.</summary>
    public SvgWriteOptions()
    {
        Mode = SvgWriteMode.Canonical;
        DeclarationPolicy = SvgXmlDeclarationPolicy.Preserve;
        DoctypePolicy = SvgWriteDoctypePolicy.Omit;
        EmptyElementPolicy = SvgEmptyElementPolicy.SelfClosing;
        QuoteStyle = SvgAttributeQuoteStyle.Double;
        AttributeOrder = SvgWriteAttributeOrder.NamespaceThenExpandedName;
        Values = new SvgCanonicalizeOptions();
        PreserveNamespaceDeclarations = true;
        Indent = false;
        IndentText = "  ";
        NewLine = "\n";
        SpaceBeforeEmptyElementSlash = false;
        DeclarationEncoding = "utf-8";
        DeclarationStandalone = "";
        MaximumNodes = 2000000;
        MaximumAttributes = 8000000;
        MaximumDepth = 1024;
        MaximumCharacters = 268435456;
        CancellationToken = System.Threading.CancellationToken.None;
    }

    internal SvgWriteOptions Copy()
    {
        SvgWriteOptions copy = new SvgWriteOptions();
        copy.Mode = Mode;
        copy.DeclarationPolicy = DeclarationPolicy;
        copy.DoctypePolicy = DoctypePolicy;
        copy.EmptyElementPolicy = EmptyElementPolicy;
        copy.QuoteStyle = QuoteStyle;
        copy.AttributeOrder = AttributeOrder;
        copy.Values = Values == null ? throw new System.InvalidOperationException("A required SVG value is unavailable.") : Values.Copy();
        copy.PreserveNamespaceDeclarations = PreserveNamespaceDeclarations;
        copy.Indent = Indent;
        copy.IndentText = IndentText;
        copy.NewLine = NewLine;
        copy.SpaceBeforeEmptyElementSlash = SpaceBeforeEmptyElementSlash;
        copy.DeclarationEncoding = DeclarationEncoding;
        copy.DeclarationStandalone = DeclarationStandalone;
        copy.MaximumNodes = MaximumNodes;
        copy.MaximumAttributes = MaximumAttributes;
        copy.MaximumDepth = MaximumDepth;
        copy.MaximumCharacters = MaximumCharacters;
        copy.CancellationToken = CancellationToken;
        return copy;
    }
}

/// <summary>Maps one written XML region to retained or regenerated document content.</summary>
public class SvgWriteSourceMapEntry
{
    /// <summary>Whether the written region is original, generated, or rewritten.</summary>
    public SvgSourceMapKind Kind;
    /// <summary>The associated node, or <c>null</c> for a declaration region.</summary>
    public SvgNode? Node;
    /// <summary>The associated attribute, or <c>null</c> for a node or declaration region.</summary>
    public SvgAttribute? Attribute;
    /// <summary>The inclusive UTF-16 output offset.</summary>
    public long OutputStart;
    /// <summary>The exclusive UTF-16 output offset.</summary>
    public long OutputEnd;
    /// <summary>The original source span, or <c>null</c> for generated content.</summary>
    public SvgSourceSpan? SourceSpan;

    internal SvgWriteSourceMapEntry(SvgSourceMapKind kind, SvgNode? node, SvgAttribute? attribute, long outputStart, long outputEnd, SvgSourceSpan? sourceSpan)
    {
        Kind = kind;
        Node = node;
        Attribute = attribute;
        OutputStart = outputStart;
        OutputEnd = outputEnd;
        SourceSpan = sourceSpan;
    }
}

/// <summary>Contains namespace-repaired XML text and bounded write metadata.</summary>
public class SvgWriteResult
{
    /// <summary>The complete XML text, or an empty string after failure.</summary>
    public string Text;
    /// <summary>The ordered write diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The ordered output-to-source region mappings.</summary>
    public System.Collections.Generic.List<SvgWriteSourceMapEntry> SourceMap;
    /// <summary>The stable write failure.</summary>
    public SvgWriteError Error;
    /// <summary>The stable human-readable failure message.</summary>
    public string Message;
    /// <summary>The written node count.</summary>
    public int WrittenNodeCount;
    /// <summary>The written non-namespace and namespace attribute count.</summary>
    public int WrittenAttributeCount;
    /// <summary>The written UTF-16 character count.</summary>
    public long CharacterCount;
    /// <summary>The source document version captured by the result.</summary>
    public long SourceVersion;
    /// <summary>The number of exact raw source regions reused.</summary>
    public int ReusedSourceRegionCount;
    /// <summary>The number of node regions regenerated from current DOM state.</summary>
    public int ReserializedRegionCount;
    /// <summary>Whether output completed successfully.</summary>
    public bool Success;

    internal SvgWriteResult()
    {
        Text = "";
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        SourceMap = new System.Collections.Generic.List<SvgWriteSourceMapEntry>();
        Error = SvgWriteError.None;
        Message = "";
        WrittenNodeCount = 0;
        WrittenAttributeCount = 0;
        CharacterCount = 0;
        SourceVersion = 0;
        ReusedSourceRegionCount = 0;
        ReserializedRegionCount = 0;
        Success = false;
    }
}
