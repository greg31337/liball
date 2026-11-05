namespace odf;

/// <summary>
/// Describes XForms submission instructions without executing a submission.
/// </summary>
public class OdfXFormsSubmission
{
    /// <summary>The submission identifier.</summary>
    public string Id = "";
    /// <summary>The submitted-node XPath expression.</summary>
    public string Ref = "";
    /// <summary>The referenced bind identifier.</summary>
    public string Bind = "";
    /// <summary>The submission resource IRI.</summary>
    public string Resource = "";
    /// <summary>The deprecated action IRI.</summary>
    public string Action = "";
    /// <summary>The synchronous or asynchronous mode token.</summary>
    public string Mode = "";
    /// <summary>The submission method.</summary>
    public string Method = "";
    /// <summary>The preserved validation flag.</summary>
    public string Validate = "";
    /// <summary>The preserved relevance-pruning flag.</summary>
    public string Relevant = "";
    /// <summary>The serialization media token.</summary>
    public string Serialization = "";
    /// <summary>The serialization version.</summary>
    public string Version = "";
    /// <summary>The preserved indentation flag.</summary>
    public string Indent = "";
    /// <summary>The serialization media type.</summary>
    public string MediaType = "";
    /// <summary>The serialization character encoding.</summary>
    public string Encoding = "";
    /// <summary>The preserved omit-XML-declaration flag.</summary>
    public string OmitXmlDeclaration = "";
    /// <summary>The preserved standalone flag.</summary>
    public string Standalone = "";
    /// <summary>The lexical list of elements whose text uses CDATA sections.</summary>
    public string CDataSectionElements = "";
    /// <summary>The submission response replacement mode.</summary>
    public string Replace = "";
    /// <summary>The target instance identifier.</summary>
    public string Instance = "";
    /// <summary>The replacement target XPath expression.</summary>
    public string TargetRef = "";
    /// <summary>The URL-encoded pair separator.</summary>
    public string Separator = "";
    /// <summary>The namespace prefixes included during serialization.</summary>
    public string IncludeNamespacePrefixes = "";
    /// <summary>The resolved bind, when present.</summary>
    public OdfXFormsBind? BindTarget;
    /// <summary>The resolved replacement instance, when present.</summary>
    public OdfXFormsInstance? InstanceTarget;
    /// <summary>Whether a non-empty bind reference is unresolved.</summary>
    public bool IsBindReferenceBroken;
    /// <summary>Whether a non-empty instance reference is unresolved.</summary>
    public bool IsInstanceReferenceBroken;
    /// <summary>The optional child resource element.</summary>
    public OdfNode? ResourceNode;
    /// <summary>All submission child nodes, including headers and inert actions.</summary>
    public OdfNode[] ChildNodes = [];
    /// <summary>All qualified submission attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>The underlying XForms submission element.</summary>
    public OdfNode? Node;
    internal string OriginalId = "";
    internal string OriginalRef = "";
    internal string OriginalBind = "";
    internal string OriginalResource = "";
    internal string OriginalAction = "";
    internal string OriginalMode = "";
    internal string OriginalMethod = "";
    internal string OriginalValidate = "";
    internal string OriginalRelevant = "";
    internal string OriginalSerialization = "";
    internal string OriginalVersion = "";
    internal string OriginalIndent = "";
    internal string OriginalMediaType = "";
    internal string OriginalEncoding = "";
    internal string OriginalOmitXmlDeclaration = "";
    internal string OriginalStandalone = "";
    internal string OriginalCDataSectionElements = "";
    internal string OriginalReplace = "";
    internal string OriginalInstance = "";
    internal string OriginalTargetRef = "";
    internal string OriginalSeparator = "";
    internal string OriginalIncludeNamespacePrefixes = "";
}
