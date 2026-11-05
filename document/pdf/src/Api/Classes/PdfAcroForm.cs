using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Collects interactive PDF form fields and can flatten their current values into page content.
/// </summary>
public class PdfAcroForm : IPdfAcroForm
{
    internal List<PdfAcroFormField> _fields;
    /// <summary>
    /// Exposes the fields currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfAcroFormField> Fields;
    /// <summary>
    /// Indicates whether need appearances is enabled.
    /// </summary>
    internal bool NeedAppearances;
    /// <summary>
    /// Stores the default appearance used by this object.
    /// </summary>
    internal string DefaultAppearance;
    internal List<PdfRadioButtonGroup> _radioButtonGroups;
    internal IReadOnlyList<PdfRadioButtonGroup> RadioButtonGroups;
    internal PdfDocument? OwnerDocument;

    /// <summary>
    /// Initializes a new acro form.
    /// </summary>
    public PdfAcroForm()
    {
        _fields = new List<PdfAcroFormField>();
        Fields = _fields;
        NeedAppearances = false;
        DefaultAppearance = "/Helv 0 Tf 0 g";
        _radioButtonGroups = new List<PdfRadioButtonGroup>();
        RadioButtonGroups = _radioButtonGroups;
        OwnerDocument = null;
    }

    /// <summary>
    /// Sets the need appearances used for subsequent PDF generation.
    /// </summary>
    /// <param name="needAppearances">The need appearances to use for this operation.</param>
    public void SetNeedAppearances(bool needAppearances)
    {
        NeedAppearances = needAppearances;
    }

    /// <summary>
    /// Sets the default appearance used for subsequent PDF generation.
    /// </summary>
    /// <param name="defaultAppearance">The default appearance to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public void SetDefaultAppearance(string defaultAppearance)
    {
        if (defaultAppearance == null)
        {
            throw new ArgumentNullException("defaultAppearance");
        }

        if (defaultAppearance.Length == 0)
        {
            throw new ArgumentException("Default appearance cannot be empty.", "defaultAppearance");
        }

        DefaultAppearance = defaultAppearance;
    }

    /// <summary>
    /// Adds a field to this acro form.
    /// </summary>
    /// <param name="field">The field to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public void AddField(PdfAcroFormField field)
    {
        if (field == null)
        {
            throw new ArgumentNullException("field");
        }

        if (HasField(field.Name))
        {
            throw new ArgumentException("A form field with the same name already exists.", "field");
        }

        if (OwnerDocument != null)
        {
            OwnerDocument.ValidateDocumentPage(field.Page, "field", "Form field page must belong to the document.");
        }

        _fields.Add(field);
    }

    /// <summary>
    /// Determines whether the current object has field.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    public bool HasField(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        foreach (PdfAcroFormField field in _fields)
        {
            if (field.Name == name)
            {
                return true;
            }
        }

        foreach (PdfRadioButtonGroup group in _radioButtonGroups)
        {
            if (group.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Clears the current fields.
    /// </summary>
    public void ClearFields()
    {
        _fields.Clear();
        _radioButtonGroups.Clear();
    }

    /// <summary>Adds a mutually exclusive radio-button group.</summary>
    /// <param name="group">The group to register.</param>
    public void AddRadioButtonGroup(PdfRadioButtonGroup group)
    {
        if (group == null)
        {
            throw new ArgumentNullException("group");
        }

        if (HasField(group.Name))
        {
            throw new ArgumentException("A form field with the same name already exists.", "group");
        }

        if (OwnerDocument != null)
        {
            group.SetOwnerDocument(OwnerDocument);
        }

        _radioButtonGroups.Add(group);
    }

    /// <summary>Returns the number of terminal non-radio fields.</summary>
    /// <returns>The field count.</returns>
    public int GetFieldCount()
    {
        return _fields.Count;
    }

    /// <summary>Returns a terminal non-radio field.</summary>
    /// <param name="index">The zero-based field position.</param>
    /// <returns>The requested field.</returns>
    public PdfAcroFormField GetField(int index)
    {
        if (index < 0 || index >= _fields.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Form field position is outside the registered range.");
        }

        return _fields[index];
    }

    /// <summary>Returns the number of registered radio-button groups.</summary>
    /// <returns>The radio-button group count.</returns>
    public int GetRadioButtonGroupCount()
    {
        return _radioButtonGroups.Count;
    }

    /// <summary>Returns a registered radio-button group.</summary>
    /// <param name="index">The zero-based group position.</param>
    /// <returns>The requested group.</returns>
    public PdfRadioButtonGroup GetRadioButtonGroup(int index)
    {
        if (index < 0 || index >= _radioButtonGroups.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Radio-button group position is outside the registered range.");
        }

        return _radioButtonGroups[index];
    }

    internal void SetOwnerDocument(PdfDocument document)
    {
        if (OwnerDocument != null && OwnerDocument != document)
        {
            throw new InvalidOperationException("An AcroForm instance cannot be shared by multiple documents.");
        }

        OwnerDocument = document;
        foreach (PdfAcroFormField field in _fields)
        {
            document.ValidateDocumentPage(field.Page, "field", "Form field page must belong to the document.");
        }

        foreach (PdfRadioButtonGroup group in _radioButtonGroups)
        {
            group.SetOwnerDocument(document);
        }
    }
}
