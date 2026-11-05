namespace odf;

/// <summary>
/// Describes an inert XForms model embedded in an OpenDocument document.
/// </summary>
public class OdfXFormsModel
{
    /// <summary>The model identifier.</summary>
    public string Id = "";
    /// <summary>The lexical list of extension function names.</summary>
    public string Functions = "";
    /// <summary>The lexical list of external schema references.</summary>
    public string Schema = "";
    /// <summary>The supported XForms version list.</summary>
    public string Version = "";
    /// <summary>The model instances in document order.</summary>
    public OdfXFormsInstance[] Instances = [];
    /// <summary>The top-level binds in document order.</summary>
    public OdfXFormsBind[] Binds = [];
    /// <summary>The submission descriptions in document order.</summary>
    public OdfXFormsSubmission[] Submissions = [];
    /// <summary>The external and inline schema descriptions.</summary>
    public OdfXFormsSchemaReference[] SchemaReferences = [];
    /// <summary>All qualified model attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>All model child nodes, including preserved foreign content and actions.</summary>
    public OdfNode[] ChildNodes = [];
    /// <summary>The XML part containing the model.</summary>
    public string PartPath = "";
    /// <summary>The underlying XForms model element.</summary>
    public OdfNode? Node;
    internal string OriginalId = "";
    internal string OriginalFunctions = "";
    internal string OriginalSchema = "";
    internal string OriginalVersion = "";
}
