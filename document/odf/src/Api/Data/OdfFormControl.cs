namespace odf;

/// <summary>
/// Describes the kind of a form control.
/// </summary>
public class OdfFormControl
{
    /// <summary>
    /// The form-control kind.
    /// </summary>
    public OdfFormControlKind Kind;
    /// <summary>The source element local name.</summary>
    public string ElementName = "";
    /// <summary>The control name.</summary>
    public string Name = "";
    /// <summary>The form control identifier.</summary>
    public string Id = "";
    /// <summary>The XML identifier.</summary>
    public string XmlId = "";
    /// <summary>The namespaced control-implementation token.</summary>
    public string ControlImplementation = "";
    /// <summary>The displayed control label.</summary>
    public string Label = "";
    /// <summary>The lexical list of control identifiers labelled by this control.</summary>
    public string For = "";
    /// <summary>The default control value.</summary>
    public string Value = "";
    /// <summary>The current control value.</summary>
    public string CurrentValue = "";
    /// <summary>The minimum accepted value.</summary>
    public string MinValue = "";
    /// <summary>The maximum accepted value.</summary>
    public string MaxValue = "";
    /// <summary>The control title.</summary>
    public string Title = "";
    /// <summary>The data style used to format the control value.</summary>
    public string DataStyleName = "";
    /// <summary>The control data and XForms bindings.</summary>
    public OdfFormBinding Binding = new OdfFormBinding();
    /// <summary>The control validation and availability description.</summary>
    public OdfFormValidation Validation = new OdfFormValidation();
    /// <summary>The application-defined control properties.</summary>
    public OdfFormProperty[] Properties = [];
    /// <summary>The list or combo-box choices.</summary>
    public OdfFormOption[] Options = [];
    /// <summary>The inert event-listener descriptions.</summary>
    public OdfDrawingEvent[] Events = [];
    /// <summary>The resolved label and drawing references.</summary>
    public OdfFormReference[] References = [];
    /// <summary>The containing form, grid, or grid column.</summary>
    public OdfFormControl? Parent;
    /// <summary>The directly contained forms, controls, or grid columns.</summary>
    public OdfFormControl[] Children = [];
    /// <summary>All qualified source attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>The XML part containing the control.</summary>
    public string PartPath = "";
    /// <summary>The underlying form element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalId = "";
    internal string OriginalXmlId = "";
    internal string OriginalControlImplementation = "";
    internal string OriginalLabel = "";
    internal string OriginalFor = "";
    internal string OriginalValue = "";
    internal string OriginalCurrentValue = "";
    internal string OriginalMinValue = "";
    internal string OriginalMaxValue = "";
    internal string OriginalTitle = "";
    internal string OriginalDataStyleName = "";
}
