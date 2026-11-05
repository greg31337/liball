namespace svg;

/// <summary>Configures bounded expansion of use elements and resource inheritance references.</summary>
public class SvgFlattenReferencesProcessOptions
{
    /// <summary>The use-element expansion settings.</summary>
    public SvgUseResolveOptions UseOptions;
    /// <summary>The base identifier prefix assigned to cloned use content.</summary>
    public string NamespacePrefix;
    /// <summary>The maximum number of document nodes inspected.</summary>
    public int MaximumNodes;

    /// <summary>Creates deterministic local-reference flattening defaults.</summary>
    public SvgFlattenReferencesProcessOptions()
    {
        UseOptions = new SvgUseResolveOptions();
        NamespacePrefix = "use-";
        MaximumNodes = 1000000;
    }
}

/// <summary>Configures extraction of one identified fragment as root document content.</summary>
public class SvgFragmentExtractionProcessOptions
{
    /// <summary>The fragment identifier with or without a leading number sign.</summary>
    public string FragmentIdentifier;
    /// <summary>Whether root definitions and styles are retained.</summary>
    public bool RetainDefinitions;
    /// <summary>An optional prefix applied to every retained identifier.</summary>
    public string NamespacePrefix;
    /// <summary>The maximum number of source nodes inspected.</summary>
    public int MaximumNodes;

    /// <summary>Creates bounded extraction defaults without a selected fragment.</summary>
    public SvgFragmentExtractionProcessOptions()
    {
        FragmentIdentifier = "";
        RetainDefinitions = true;
        NamespacePrefix = "";
        MaximumNodes = 1000000;
    }
}

/// <summary>Configures splitting every symbol into an independent SVG document.</summary>
public class SvgSymbolSplitProcessOptions
{
    /// <summary>The caller-owned list replaced with generated symbol documents.</summary>
    public System.Collections.Generic.List<SvgDocument> OutputDocuments;
    /// <summary>The identifier prefix used to isolate each generated document.</summary>
    public string NamespacePrefix;
    /// <summary>The maximum generated symbol-document count.</summary>
    public int MaximumSymbols;

    /// <summary>Creates deterministic bounded symbol splitting defaults.</summary>
    public SvgSymbolSplitProcessOptions()
    {
        OutputDocuments = new System.Collections.Generic.List<SvgDocument>();
        NamespacePrefix = "symbol-";
        MaximumSymbols = 10000;
    }
}

/// <summary>Configures deterministic import of multiple documents into the current root.</summary>
public class SvgDocumentMergeProcessOptions
{
    /// <summary>The ordered source documents imported after current content.</summary>
    public System.Collections.Generic.List<SvgDocument> Documents;
    /// <summary>The base prefix used for wrapper and imported identifiers.</summary>
    public string NamespacePrefix;
    /// <summary>The maximum imported document count.</summary>
    public int MaximumDocuments;
    /// <summary>The maximum imported node count across all documents.</summary>
    public int MaximumNodes;

    /// <summary>Creates empty deterministic merge defaults.</summary>
    public SvgDocumentMergeProcessOptions()
    {
        Documents = new System.Collections.Generic.List<SvgDocument>();
        NamespacePrefix = "merge-";
        MaximumDocuments = 1000;
        MaximumNodes = 1000000;
    }
}

/// <summary>Configures conversion of current and imported documents into sprite symbols.</summary>
public class SvgSpriteProcessOptions
{
    /// <summary>The ordered additional documents converted to symbols.</summary>
    public System.Collections.Generic.List<SvgDocument> Documents;
    /// <summary>Whether current root graphical content becomes the first symbol.</summary>
    public bool IncludeCurrentDocument;
    /// <summary>The base symbol and identifier prefix.</summary>
    public string SymbolPrefix;
    /// <summary>The maximum generated symbol count.</summary>
    public int MaximumSymbols;
    /// <summary>The maximum imported or cloned node count.</summary>
    public int MaximumNodes;

    /// <summary>Creates a one-document sprite configuration.</summary>
    public SvgSpriteProcessOptions()
    {
        Documents = new System.Collections.Generic.List<SvgDocument>();
        IncludeCurrentDocument = true;
        SymbolPrefix = "sprite-";
        MaximumSymbols = 1000;
        MaximumNodes = 1000000;
    }
}

/// <summary>Configures complete identifier namespace isolation.</summary>
public class SvgNamespaceIsolationProcessOptions
{
    /// <summary>The underlying complete ID-reference rewrite settings.</summary>
    public SvgIdPrefixOptions IdOptions;

    /// <summary>Creates namespace isolation defaults using an <c>isolate-</c> prefix.</summary>
    public SvgNamespaceIsolationProcessOptions()
    {
        IdOptions = new SvgIdPrefixOptions();
        IdOptions.Prefix = "isolate-";
    }
}
