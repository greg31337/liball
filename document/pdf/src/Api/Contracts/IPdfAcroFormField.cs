using System;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfAcroFormField"/>.
/// </summary>
public interface IPdfAcroFormField
{
    /// <summary>Returns the PDF field type name.</summary>
    /// <returns>The field type name.</returns>
    string GetFieldType();

    /// <summary>Returns the fully registered field name.</summary>
    /// <returns>The field name.</returns>
    string GetFieldName();

    /// <summary>Determines whether this widget is a child of a radio-button group.</summary>
    /// <returns>True for radio option widgets; otherwise, false.</returns>
    bool IsRadioButtonOption();

    /// <summary>Returns the radio option export value.</summary>
    /// <returns>The export value, or an empty value for other fields.</returns>
    string GetExportValue();

    /// <summary>Returns the current text value.</summary>
    /// <returns>The field value.</returns>
    string GetValue();
    /// <summary>
    /// Sets the value used for subsequent PDF generation.
    /// </summary>
    /// <param name="value">The value to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetValue(string value);

    /// <summary>
    /// Sets the checked used for subsequent PDF generation.
    /// </summary>
    /// <param name="value">The value to use for this operation.</param>
    void SetChecked(bool value);

    /// <summary>Determines whether this checkbox or radio option is selected.</summary>
    /// <returns>True when selected; otherwise, false.</returns>
    bool IsChecked();

    /// <summary>Sets the accessible field name written as the widget alternate name.</summary>
    /// <param name="name">The accessible field name.</param>
    void SetAccessibilityName(string name);

    /// <summary>Returns the accessible field name.</summary>
    /// <returns>The configured accessible name.</returns>
    string GetAccessibilityName();

    /// <summary>
    /// Configures signature placeholder for subsequent PDF generation.
    /// </summary>
    /// <param name="contentsLength">The positive placeholder size in bytes. It cannot exceed <c>int.MaxValue / 2</c> because each byte is written as two hexadecimal characters.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    void ConfigureSignaturePlaceholder(int contentsLength);

    /// <summary>
    /// Sets the signature reason used for subsequent PDF generation.
    /// </summary>
    /// <param name="reason">The reason to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetSignatureReason(string reason);

    /// <summary>
    /// Sets the signature location used for subsequent PDF generation.
    /// </summary>
    /// <param name="location">The location to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetSignatureLocation(string location);

    /// <summary>
    /// Sets the signature contact info used for subsequent PDF generation.
    /// </summary>
    /// <param name="contactInfo">The contact info to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetSignatureContactInfo(string contactInfo);
}
