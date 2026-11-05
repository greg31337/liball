namespace odf;

/// <summary>
/// Describes a variable, user-field, sequence, or DDE declaration.
/// </summary>
public class OdfTextFieldDeclaration
{
    /// <summary>
    /// The declaration family.
    /// </summary>
    public OdfTextFieldDeclarationKind Kind;
    /// <summary>
    /// The declaration name.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The declared office value type.
    /// </summary>
    public string ValueType = "";
    /// <summary>
    /// The preserved string value.
    /// </summary>
    public string StringValue = "";
    /// <summary>
    /// The preserved numeric value.
    /// </summary>
    public string Value = "";
    /// <summary>
    /// The preserved currency code.
    /// </summary>
    public string Currency = "";
    /// <summary>
    /// The preserved date or date-time value.
    /// </summary>
    public string DateValue = "";
    /// <summary>
    /// The preserved time or duration value.
    /// </summary>
    public string TimeValue = "";
    /// <summary>
    /// The preserved boolean value.
    /// </summary>
    public string BooleanValue = "";
    /// <summary>
    /// The declaration formula.
    /// </summary>
    public string Formula = "";
    /// <summary>
    /// The sequence display outline level.
    /// </summary>
    public string DisplayOutlineLevel = "";
    /// <summary>
    /// The sequence separation character.
    /// </summary>
    public string SeparationCharacter = "";
    /// <summary>
    /// The DDE application name.
    /// </summary>
    public string DdeApplication = "";
    /// <summary>
    /// The DDE topic.
    /// </summary>
    public string DdeTopic = "";
    /// <summary>
    /// The DDE item.
    /// </summary>
    public string DdeItem = "";
    /// <summary>
    /// The preserved DDE automatic-update value.
    /// </summary>
    public string AutomaticUpdate = "";
    /// <summary>
    /// All qualified declaration attributes.
    /// </summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>
    /// The XML part containing the declaration.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The underlying declaration element.
    /// </summary>
    public OdfNode? Node;
}
