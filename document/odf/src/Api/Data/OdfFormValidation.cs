namespace odf;

/// <summary>
/// Describes validation, availability, and editability flags of a form control.
/// </summary>
public class OdfFormValidation
{
    /// <summary>The preserved disabled lexical value.</summary>
    public string DisabledValue = "";
    /// <summary>Whether the control is disabled.</summary>
    public bool IsDisabled;
    /// <summary>Whether the disabled value is absent or valid.</summary>
    public bool IsDisabledValid;
    /// <summary>The preserved input-required lexical value.</summary>
    public string InputRequiredValue = "";
    /// <summary>Whether input is required.</summary>
    public bool IsInputRequired;
    /// <summary>Whether the input-required value is absent or valid.</summary>
    public bool IsInputRequiredValid;
    /// <summary>The preserved read-only lexical value.</summary>
    public string ReadOnlyValue = "";
    /// <summary>Whether the control is read-only.</summary>
    public bool IsReadOnly;
    /// <summary>Whether the read-only value is absent or valid.</summary>
    public bool IsReadOnlyValid;
    /// <summary>The preserved validation lexical value.</summary>
    public string ValidationValue = "";
    /// <summary>Whether validation is enabled.</summary>
    public bool IsValidationEnabled;
    /// <summary>Whether the validation value is absent or valid.</summary>
    public bool IsValidationValueValid;
    /// <summary>The maximum permitted text length.</summary>
    public string MaxLength = "";
    internal string OriginalDisabledValue = "";
    internal bool OriginalIsDisabled;
    internal string OriginalInputRequiredValue = "";
    internal bool OriginalIsInputRequired;
    internal string OriginalReadOnlyValue = "";
    internal bool OriginalIsReadOnly;
    internal string OriginalValidationValue = "";
    internal bool OriginalIsValidationEnabled;
    internal string OriginalMaxLength = "";
}
