using System;

namespace pdf;

/// <summary>
/// Represents an acro form field used by the PDF generation API.
/// </summary>
public class PdfAcroFormField : IPdfAcroFormField
{
    /// <summary>
    /// Stores the field type used by this object.
    /// </summary>
    internal string FieldType;
    /// <summary>
    /// Stores the name used by this object.
    /// </summary>
    internal string Name;
    /// <summary>
    /// Stores the page used by this object.
    /// </summary>
    internal PdfPage Page;
    /// <summary>
    /// Stores the rectangle used by this object.
    /// </summary>
    internal PdfRectangle Rectangle;
    /// <summary>
    /// Stores the value used by this object.
    /// </summary>
    internal string Value;
    /// <summary>
    /// Indicates whether checked is enabled.
    /// </summary>
    internal bool Checked;
    /// <summary>
    /// Indicates whether this object has signature placeholder.
    /// </summary>
    internal bool HasSignaturePlaceholder;
    /// <summary>
    /// Stores the numeric signature contents length used by this object.
    /// </summary>
    internal int SignatureContentsLength;
    /// <summary>
    /// Stores the signature filter used by this object.
    /// </summary>
    internal string SignatureFilter;
    /// <summary>
    /// Stores the signature sub filter used by this object.
    /// </summary>
    internal string SignatureSubFilter;
    /// <summary>
    /// Stores the signature reason used by this object.
    /// </summary>
    internal string SignatureReason;
    /// <summary>
    /// Stores the signature location used by this object.
    /// </summary>
    internal string SignatureLocation;
    /// <summary>
    /// Stores the signature contact info used by this object.
    /// </summary>
    internal string SignatureContactInfo;
    internal string AccessibilityName;
    internal bool IsRadioOption;
    internal string ExportValue;
    internal PdfRadioButtonGroup? RadioGroup;
    internal PdfAccessibilityNode? TagNode;
    internal int StructParent;

    /// <summary>
    /// Initializes a new acro form field.
    /// </summary>
    /// <param name="fieldType">The field type to use for this operation.</param>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="rectangle">The rectangle to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfAcroFormField(string fieldType, string name, PdfPage page, PdfRectangle rectangle)
    {
        if (fieldType == null)
        {
            throw new ArgumentNullException("fieldType");
        }

        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        ValidateFieldType(fieldType);
        ValidateFieldName(name);
        if (double.IsNaN(rectangle.X) || double.IsInfinity(rectangle.X) || double.IsNaN(rectangle.Y) || double.IsInfinity(rectangle.Y) || double.IsNaN(rectangle.Width) || double.IsInfinity(rectangle.Width) || double.IsNaN(rectangle.Height) || double.IsInfinity(rectangle.Height) || rectangle.Width <= 0 || rectangle.Height <= 0)
        {
            throw new ArgumentOutOfRangeException("rectangle", "Form field rectangle must have positive dimensions.");
        }

        FieldType = fieldType;
        Name = name;
        Page = page;
        Rectangle = new PdfRectangle(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        Value = string.Empty;
        Checked = false;
        HasSignaturePlaceholder = false;
        SignatureContentsLength = 8192;
        SignatureFilter = "Adobe.PPKLite";
        SignatureSubFilter = "ETSI.CAdES.detached";
        SignatureReason = string.Empty;
        SignatureLocation = string.Empty;
        SignatureContactInfo = string.Empty;
        AccessibilityName = string.Empty;
        IsRadioOption = false;
        ExportValue = string.Empty;
        RadioGroup = null;
        TagNode = null;
        StructParent = -1;
    }

    /// <summary>Returns the PDF field type name.</summary>
    /// <returns>The field type name.</returns>
    public string GetFieldType()
    {
        return FieldType;
    }

    /// <summary>Returns the fully registered field name.</summary>
    /// <returns>The field name.</returns>
    public string GetFieldName()
    {
        if (IsRadioOption && RadioGroup != null)
        {
            return RadioGroup.Name;
        }

        return Name;
    }

    /// <summary>Determines whether this widget is a child of a radio-button group.</summary>
    /// <returns>True for radio option widgets; otherwise, false.</returns>
    public bool IsRadioButtonOption()
    {
        return IsRadioOption;
    }

    /// <summary>Returns the radio option export value.</summary>
    /// <returns>The export value, or an empty value for other field types.</returns>
    public string GetExportValue()
    {
        return ExportValue;
    }

    /// <summary>Returns the current text value.</summary>
    /// <returns>The field value.</returns>
    public string GetValue()
    {
        return Value;
    }

    /// <summary>
    /// Sets the value used for subsequent PDF generation.
    /// </summary>
    /// <param name="value">The value to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (FieldType != "Tx")
        {
            throw new InvalidOperationException("Only text fields accept text values.");
        }

        Value = value;
    }

    /// <summary>
    /// Sets the checked used for subsequent PDF generation.
    /// </summary>
    /// <param name="value">The value to use for this operation.</param>
    public void SetChecked(bool value)
    {
        if (FieldType != "Btn")
        {
            throw new InvalidOperationException("Only checkbox and radio-button fields have checked state.");
        }

        if (IsRadioOption && RadioGroup != null)
        {
            if (value)
            {
                RadioGroup.SelectOption(ExportValue);
            }
            else if (Checked)
            {
                RadioGroup.ClearSelection();
            }

            return;
        }

        Checked = value;
    }

    /// <summary>Determines whether this checkbox or radio option is selected.</summary>
    /// <returns>True when the field is selected; otherwise, false.</returns>
    public bool IsChecked()
    {
        if (FieldType != "Btn")
        {
            throw new InvalidOperationException("Only checkbox and radio-button fields have checked state.");
        }

        return Checked;
    }

    /// <summary>Sets the accessible field name written as the widget alternate name.</summary>
    /// <param name="name">The non-empty accessible field name.</param>
    public void SetAccessibilityName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Accessible field name cannot be empty.", "name");
        }

        AccessibilityName = name;
    }

    /// <summary>Returns the accessible field name.</summary>
    /// <returns>The configured accessible name, or an empty value when none is configured.</returns>
    public string GetAccessibilityName()
    {
        return AccessibilityName;
    }

    /// <summary>
    /// Configures signature placeholder for subsequent PDF generation.
    /// </summary>
    /// <param name="contentsLength">The positive placeholder size in bytes. It cannot exceed <c>int.MaxValue / 2</c> because each byte is written as two hexadecimal characters.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public void ConfigureSignaturePlaceholder(int contentsLength)
    {
        if (FieldType != "Sig")
        {
            throw new InvalidOperationException("Only signature fields accept signature placeholders.");
        }

        if (contentsLength <= 0 || contentsLength > int.MaxValue / 2)
        {
            throw new ArgumentOutOfRangeException("contentsLength", "Signature contents length must be positive and small enough to encode as hexadecimal text.");
        }

        HasSignaturePlaceholder = true;
        SignatureContentsLength = contentsLength;
    }

    /// <summary>
    /// Sets the signature reason used for subsequent PDF generation.
    /// </summary>
    /// <param name="reason">The reason to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetSignatureReason(string reason)
    {
        if (reason == null)
        {
            throw new ArgumentNullException("reason");
        }

        ValidateSignatureField();

        SignatureReason = reason;
    }

    /// <summary>
    /// Sets the signature location used for subsequent PDF generation.
    /// </summary>
    /// <param name="location">The location to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetSignatureLocation(string location)
    {
        if (location == null)
        {
            throw new ArgumentNullException("location");
        }

        ValidateSignatureField();

        SignatureLocation = location;
    }

    /// <summary>
    /// Sets the signature contact info used for subsequent PDF generation.
    /// </summary>
    /// <param name="contactInfo">The contact info to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetSignatureContactInfo(string contactInfo)
    {
        if (contactInfo == null)
        {
            throw new ArgumentNullException("contactInfo");
        }

        ValidateSignatureField();

        SignatureContactInfo = contactInfo;
    }

    internal void ConfigureRadioOption(PdfRadioButtonGroup group, string exportValue)
    {
        RadioGroup = group;
        ExportValue = exportValue;
        IsRadioOption = true;
    }

    internal void SetRadioChecked(bool value)
    {
        Checked = value;
    }

    internal void ValidateSignatureField()
    {
        if (FieldType != "Sig")
        {
            throw new InvalidOperationException("Only signature fields accept signature metadata.");
        }
    }

    internal static void ValidateFieldType(string fieldType)
    {
        if (fieldType == "Tx" || fieldType == "Btn" || fieldType == "Sig")
        {
            return;
        }

        throw new ArgumentException("Form field type is not supported.", "fieldType");
    }

    internal static void ValidateFieldName(string name)
    {
        if (name.Length == 0)
        {
            throw new ArgumentException("Form field name cannot be empty.", "name");
        }

        foreach (char character in name)
        {
            if (character == '.' || character < ' ')
            {
                throw new ArgumentException("Form field names cannot contain periods or control characters.", "name");
            }
        }
    }
}
