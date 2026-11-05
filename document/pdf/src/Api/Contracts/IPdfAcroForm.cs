using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfAcroForm"/>.
/// </summary>
public interface IPdfAcroForm
{
    /// <summary>
    /// Sets the need appearances used for subsequent PDF generation.
    /// </summary>
    /// <param name="needAppearances">The need appearances to use for this operation.</param>
    void SetNeedAppearances(bool needAppearances);

    /// <summary>
    /// Sets the default appearance used for subsequent PDF generation.
    /// </summary>
    /// <param name="defaultAppearance">The default appearance to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    void SetDefaultAppearance(string defaultAppearance);

    /// <summary>
    /// Adds a field to this acro form.
    /// </summary>
    /// <param name="field">The field to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    void AddField(PdfAcroFormField field);

    /// <summary>
    /// Determines whether the current object has field.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    bool HasField(string name);

    /// <summary>
    /// Clears the current fields.
    /// </summary>
    void ClearFields();

    /// <summary>Adds a mutually exclusive radio-button group.</summary>
    /// <param name="group">The group to register.</param>
    void AddRadioButtonGroup(PdfRadioButtonGroup group);

    /// <summary>Returns the number of terminal non-radio fields.</summary>
    /// <returns>The field count.</returns>
    int GetFieldCount();

    /// <summary>Returns a terminal non-radio field.</summary>
    /// <param name="index">The zero-based field position.</param>
    /// <returns>The requested field.</returns>
    PdfAcroFormField GetField(int index);

    /// <summary>Returns the number of registered radio-button groups.</summary>
    /// <returns>The group count.</returns>
    int GetRadioButtonGroupCount();

    /// <summary>Returns a registered radio-button group.</summary>
    /// <param name="index">The zero-based group position.</param>
    /// <returns>The requested group.</returns>
    PdfRadioButtonGroup GetRadioButtonGroup(int index);
}
