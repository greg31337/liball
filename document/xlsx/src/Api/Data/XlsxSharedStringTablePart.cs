using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents the in-memory fields of an OOXML shared string table part, including preserved content that the library does not interpret.
/// </summary>
public class XlsxSharedStringTablePart
{
    /// <summary>
    /// Indicates whether the operation completed without error-level diagnostics.
    /// </summary>
    public bool IsValid;
    /// <summary>
    /// Contains diagnostics produced while the object was read, changed, validated, or written.
    /// </summary>
    public XlsxValidationResult Validation;
    /// <summary>
    /// Stores the normalized absolute package path, beginning with <c>/</c>.
    /// </summary>
    public string PartPath;
    /// <summary>
    /// Stores the namespace uri value used by this model.
    /// </summary>
    public string NamespaceUri;
    /// <summary>
    /// Stores the number of  items processed by the operation.
    /// </summary>
    public uint Count;
    /// <summary>
    /// Indicates whether count is present and should be written.
    /// </summary>
    public bool HasCount;
    /// <summary>
    /// Stores the number of unique items processed by the operation.
    /// </summary>
    public uint UniqueCount;
    /// <summary>
    /// Indicates whether unique count is present and should be written.
    /// </summary>
    public bool HasUniqueCount;
    /// <summary>
    /// Contains items.
    /// </summary>
    public List<XlsxSharedStringItem> Items;
    /// <summary>
    /// Contains unknown root attributes.
    /// </summary>
    public List<XlsxXmlAttribute> UnknownRootAttributes;
    /// <summary>
    /// Contains preserved XML that is not represented by the typed fields.
    /// </summary>
    public List<string> UnknownXml;
    /// <summary>
    /// Initializes a new shared string table part with its default state.
    /// </summary>
    public XlsxSharedStringTablePart()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        PartPath = XlsxPackagePaths.SharedStrings;
        NamespaceUri = XlsxNamespaces.Spreadsheet;
        Count = 0;
        HasCount = false;
        UniqueCount = 0;
        HasUniqueCount = false;
        Items = new List<XlsxSharedStringItem>();
        UnknownRootAttributes = new List<XlsxXmlAttribute>();
        UnknownXml = new List<string>();
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }

    /// <summary>
    /// Returns item from the current model.
    /// </summary>
    /// <param name = "index">The index value used by the operation.</param>
    /// <returns>The shared string item produced by the operation.</returns>
    internal XlsxSharedStringItem? GetItem(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            return null;
        }

        return Items[index];
    }

    /// <summary>
    /// Returns text from the current model.
    /// </summary>
    /// <param name = "index">The index value used by the operation.</param>
    /// <returns>The text produced by the operation.</returns>
    internal string GetText(int index)
    {
        XlsxSharedStringItem? item = GetItem(index);
        if (item == null)
        {
            return "";
        }

        return item.Text;
    }

    /// <summary>
    /// Returns or add text from the current model.
    /// </summary>
    /// <param name = "text">The text value used by the operation.</param>
    /// <returns>The int value produced by the operation.</returns>
    internal int GetOrAddText(string text)
    {
        string value = XlsxCellValueConverter.NormalizeText(text);
        int index = 0;
        while (index < Items.Count)
        {
            XlsxSharedStringItem item = Items[index];
            if (item != null && !item.IsRichText && item.Text == value)
            {
                item.ReferenceCount = item.ReferenceCount + 1;
                RecalculateCountsFromReferences();
                return index;
            }

            index = index + 1;
        }

        XlsxSharedStringItem newItem = new XlsxSharedStringItem();
        newItem.Text = value;
        newItem.ReferenceCount = 1;
        Items.Add(newItem);
        RecalculateCountsFromReferences();
        return Items.Count - 1;
    }

    /// <summary>
    /// Adds text reference to the current model after validating the supplied input.
    /// </summary>
    /// <param name = "text">The text value used by the operation.</param>
    /// <returns>The int value produced by the operation.</returns>
    internal int AddTextReference(string text)
    {
        return GetOrAddText(text);
    }

    /// <summary>
    /// Performs the recalculate counts from references operation using the current model state.
    /// </summary>
    internal void RecalculateCountsFromReferences()
    {
        uint total = 0;
        foreach (XlsxSharedStringItem item in Items)
        {
            if (item != null)
            {
                int count = item.ReferenceCount;
                if (count <= 0)
                {
                    count = 1;
                }

                total = total + (uint)count;
            }
        }

        Count = total;
        HasCount = true;
        UniqueCount = (uint)Items.Count;
        HasUniqueCount = true;
    }
}
