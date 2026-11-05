namespace odf;

/// <summary>
/// Holds the standard Dublin Core and OpenDocument metadata of a document.
/// </summary>
public class OdfDocumentMetadata
{
    /// <summary>The document title.</summary>
    public string Title = "";
    /// <summary>The document description.</summary>
    public string Description = "";
    /// <summary>The document subject.</summary>
    public string Subject = "";
    /// <summary>The most recent document creator.</summary>
    public string Creator = "";
    /// <summary>The preserved modification date or date-time.</summary>
    public string Date = "";
    /// <summary>Whether <see cref="Date"/> has a valid XML Schema lexical form.</summary>
    public bool IsDateValid;
    /// <summary>The document language.</summary>
    public string Language = "";
    /// <summary>The producing application.</summary>
    public string Generator = "";
    /// <summary>The original document creator.</summary>
    public string InitialCreator = "";
    /// <summary>The preserved creation date or date-time.</summary>
    public string CreationDate = "";
    /// <summary>Whether <see cref="CreationDate"/> has a valid XML Schema lexical form.</summary>
    public bool IsCreationDateValid;
    /// <summary>The person who most recently printed the document.</summary>
    public string PrintedBy = "";
    /// <summary>The preserved print date or date-time.</summary>
    public string PrintDate = "";
    /// <summary>Whether <see cref="PrintDate"/> has a valid XML Schema lexical form.</summary>
    public bool IsPrintDateValid;
    /// <summary>The preserved total editing duration.</summary>
    public string EditingDuration = "";
    /// <summary>Whether <see cref="EditingDuration"/> is a valid XML Schema duration.</summary>
    public bool IsEditingDurationValid;
    /// <summary>The number of editing sessions, or -1 when absent or invalid.</summary>
    public long EditingCycles = -1;
    /// <summary>Whether the editing-cycle count is present and valid.</summary>
    public bool IsEditingCyclesValid;
    /// <summary>The document keywords in source order.</summary>
    public string[] Keywords = [];
    /// <summary>The originating template metadata.</summary>
    public OdfMetadataTemplate Template = new OdfMetadataTemplate();
    /// <summary>The automatic-reload behavior.</summary>
    public OdfMetadataAutoReload AutoReload = new OdfMetadataAutoReload();
    /// <summary>The default hyperlink behavior.</summary>
    public OdfMetadataHyperlinkBehavior HyperlinkBehavior =
        new OdfMetadataHyperlinkBehavior();
    /// <summary>The typed user-defined metadata values.</summary>
    public OdfUserDefinedMetadata[] UserDefined = [];
    /// <summary>The document statistics.</summary>
    public OdfDocumentStatistics Statistics = new OdfDocumentStatistics();
    /// <summary>The underlying <c>office:meta</c> element, when present.</summary>
    public OdfNode? Node;
    internal OdfNode? TitleNode;
    internal OdfNode? DescriptionNode;
    internal OdfNode? SubjectNode;
    internal OdfNode? CreatorNode;
    internal OdfNode? DateNode;
    internal OdfNode? LanguageNode;
    internal OdfNode? GeneratorNode;
    internal OdfNode? InitialCreatorNode;
    internal OdfNode? CreationDateNode;
    internal OdfNode? PrintedByNode;
    internal OdfNode? PrintDateNode;
    internal OdfNode? EditingDurationNode;
    internal OdfNode? EditingCyclesNode;
    internal OdfNode[] KeywordNodes = [];
    internal OdfNode[] UserDefinedNodes = [];
}
