using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

/// <summary>
/// Represents a mutually exclusive AcroForm radio-button group and its option widgets.
/// </summary>
public class PdfRadioButtonGroup : IPdfRadioButtonGroup
{
    internal string Name;
    internal string AccessibilityName;
    internal string SelectedValue;
    internal List<PdfAcroFormField> _options;
    internal IReadOnlyList<PdfAcroFormField> Options;
    internal PdfDocument? OwnerDocument;

    /// <summary>
    /// Creates an empty radio-button group with a unique terminal field name.
    /// </summary>
    /// <param name="name">The non-empty terminal field name.</param>
    public PdfRadioButtonGroup(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        PdfAcroFormField.ValidateFieldName(name);
        Name = name;
        AccessibilityName = string.Empty;
        SelectedValue = string.Empty;
        _options = new List<PdfAcroFormField>();
        Options = _options;
        OwnerDocument = null;
    }

    /// <summary>Returns the registered group name.</summary>
    /// <returns>The group name.</returns>
    public string GetGroupName()
    {
        return Name;
    }

    /// <summary>Adds a radio option widget.</summary>
    /// <param name="exportValue">The unique value exported when selected.</param>
    /// <param name="page">The page receiving the option widget.</param>
    /// <param name="x">The horizontal widget coordinate.</param>
    /// <param name="y">The vertical widget coordinate.</param>
    /// <param name="width">The positive widget width.</param>
    /// <param name="height">The positive widget height.</param>
    /// <returns>The option widget field.</returns>
    public PdfAcroFormField AddOption(string exportValue, PdfPage page, double x, double y, double width, double height)
    {
        ValidateExportValue(exportValue);
        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        if (OwnerDocument != null)
        {
            OwnerDocument.ValidateDocumentPage(page, "page", "Radio-button option page must belong to the document.");
        }

        if (double.IsNaN(x) || double.IsInfinity(x) || double.IsNaN(y) || double.IsInfinity(y))
        {
            throw new ArgumentOutOfRangeException("x", "Radio-button option coordinates must be finite.");
        }

        if (double.IsNaN(width) || double.IsInfinity(width) || width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Radio-button option width must be positive.");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Radio-button option height must be positive.");
        }

        foreach (PdfAcroFormField option in _options)
        {
            if (option.ExportValue == exportValue)
            {
                throw new ArgumentException("Radio-button option export values must be unique within a group.", "exportValue");
            }
        }

        string optionName = Name + " option " + (_options.Count + 1).ToString(CultureInfo.InvariantCulture);
        PdfAcroFormField field = new PdfAcroFormField("Btn", optionName, page, new PdfRectangle(x, y, width, height));
        field.ConfigureRadioOption(this, exportValue);
        _options.Add(field);
        return field;
    }

    /// <summary>Adds a radio option widget from a rectangle.</summary>
    /// <param name="exportValue">The unique value exported when selected.</param>
    /// <param name="page">The page receiving the option widget.</param>
    /// <param name="rectangle">The positive widget rectangle.</param>
    /// <returns>The option widget field.</returns>
    public PdfAcroFormField AddOptionInRectangle(string exportValue, PdfPage page, PdfRectangle rectangle)
    {
        if (rectangle == null)
        {
            throw new ArgumentNullException("rectangle");
        }

        return AddOption(exportValue, page, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
    }

    /// <summary>Selects one registered option and clears every other option.</summary>
    /// <param name="exportValue">The option value to select.</param>
    public void SelectOption(string exportValue)
    {
        ValidateExportValue(exportValue);
        bool found = false;
        foreach (PdfAcroFormField option in _options)
        {
            if (option.ExportValue == exportValue)
            {
                found = true;
            }
        }

        if (!found)
        {
            throw new ArgumentException("Radio-button option is not registered in this group.", "exportValue");
        }

        foreach (PdfAcroFormField option in _options)
        {
            option.SetRadioChecked(option.ExportValue == exportValue);
        }

        SelectedValue = exportValue;
    }

    /// <summary>Clears the current selection.</summary>
    public void ClearSelection()
    {
        foreach (PdfAcroFormField option in _options)
        {
            option.SetRadioChecked(false);
        }

        SelectedValue = string.Empty;
    }

    /// <summary>Returns the selected export value.</summary>
    /// <returns>The selected value, or an empty value when no option is selected.</returns>
    public string GetSelectedValue()
    {
        return SelectedValue;
    }

    /// <summary>Returns the number of option widgets.</summary>
    /// <returns>The option count.</returns>
    public int GetOptionCount()
    {
        return _options.Count;
    }

    /// <summary>Returns a registered option widget.</summary>
    /// <param name="index">The zero-based option position.</param>
    /// <returns>The requested option widget.</returns>
    public PdfAcroFormField GetOption(int index)
    {
        if (index < 0 || index >= _options.Count)
        {
            throw new ArgumentOutOfRangeException("index", "Radio-button option position is outside the group.");
        }

        return _options[index];
    }

    /// <summary>Sets the accessible name written on the parent field.</summary>
    /// <param name="name">The non-empty accessible group name.</param>
    public void SetAccessibilityName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (name.Length == 0)
        {
            throw new ArgumentException("Accessible radio-button group name cannot be empty.", "name");
        }

        AccessibilityName = name;
    }

    /// <summary>Returns the accessible group name.</summary>
    /// <returns>The configured accessible name.</returns>
    public string GetAccessibilityName()
    {
        return AccessibilityName;
    }

    internal void SetOwnerDocument(PdfDocument document)
    {
        if (OwnerDocument != null && OwnerDocument != document)
        {
            throw new InvalidOperationException("A radio-button group cannot be shared by multiple documents.");
        }

        foreach (PdfAcroFormField option in _options)
        {
            document.ValidateDocumentPage(option.Page, "page", "Radio-button option page must belong to the document.");
        }

        OwnerDocument = document;
    }

    internal static void ValidateExportValue(string exportValue)
    {
        if (exportValue == null)
        {
            throw new ArgumentNullException("exportValue");
        }

        if (exportValue.Length == 0)
        {
            throw new ArgumentException("Radio-button option export value cannot be empty.", "exportValue");
        }
    }
}
