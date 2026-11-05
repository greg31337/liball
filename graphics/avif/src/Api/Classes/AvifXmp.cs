using System;

namespace avif;

/// <summary>
/// Owns and validates a UTF-8 XMP packet and exposes namespace-aware RDF fields.
/// </summary>
public class AvifXmp : IAvifXmp, IAvifDiagnosticSource
{
    /// <summary>The RDF syntax namespace used by XMP packets.</summary>
    internal const string RdfNamespace =
        "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

    /// <summary>The Dublin Core element namespace.</summary>
    internal const string DublinCoreNamespace =
        "http://purl.org/dc/elements/1.1/";

    /// <summary>The core Adobe XMP property namespace.</summary>
    internal const string XmpNamespace = "http://ns.adobe.com/xap/1.0/";

    /// <summary>The XMP TIFF-property namespace.</summary>
    internal const string TiffNamespace = "http://ns.adobe.com/tiff/1.0/";

    /// <summary>The XMP Exif-property namespace.</summary>
    internal const string ExifNamespace = "http://ns.adobe.com/exif/1.0/";

    XmpParsedData? parsed;
    AvifResultCode code;

    /// <summary>Parses an XMP packet using default metadata limits.</summary>
    /// <param name="data">The complete UTF-8 XML packet.</param>
    /// <returns>The parsed XMP packet.</returns>
    internal static AvifXmp Create(byte[] data)
    {
        return new AvifXmp(data, new AvifSecurityLimits());
    }

    /// <summary>Parses an XMP packet using explicit resource limits.</summary>
    /// <param name="data">The complete UTF-8 XML packet.</param>
    /// <param name="limits">The XMP, metadata, recursion, and temporary limits.</param>
    public AvifXmp(byte[] data, AvifSecurityLimits limits)
    {
        Initialize(data, limits);
    }

    /// <summary>Parses an XMP packet using explicit resource limits.</summary>
    /// <returns>The parsed XMP packet.</returns>
    internal static AvifXmp Create(byte[] data, AvifSecurityLimits limits)
    {
        return new AvifXmp(data, limits);
    }

    /// <summary>Returns the packet validation result.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns diagnostics for the XMP parse operation.</summary>
    /// <returns>The structured metadata diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            code,
            AvifDiagnosticCategory.Metadata,
            "The XMP metadata operation did not complete successfully.");
    }

    /// <summary>Returns a byte-for-byte copy of the original packet.</summary>
    /// <returns>The preserved UTF-8 bytes, or an empty array after failure.</returns>
    public byte[] GetData()
    {
        return parsed == null
            ? System.Array.Empty<byte>() : (byte[])parsed.Data.Clone();
    }

    /// <summary>Returns the number of flattened XMP fields.</summary>
    /// <returns>The field count.</returns>
    public int GetFieldCount()
    {
        return parsed == null ? 0 : parsed.Fields.Count;
    }

    /// <summary>Returns one field's namespace URI.</summary>
    /// <param name="index">The zero-based field index.</param>
    /// <returns>The namespace URI, or an empty string.</returns>
    public string GetFieldNamespace(int index)
    {
        return IsValidFieldIndex(index)
            ? (parsed ?? throw new InvalidOperationException("Invalid internal state.")).Fields[index].NamespaceUri : string.Empty;
    }

    /// <summary>Returns one field's local name.</summary>
    /// <param name="index">The zero-based field index.</param>
    /// <returns>The local name, or an empty string.</returns>
    public string GetFieldName(int index)
    {
        return IsValidFieldIndex(index)
            ? (parsed ?? throw new InvalidOperationException("Invalid internal state.")).Fields[index].Name : string.Empty;
    }

    /// <summary>Returns one field's simple or selected RDF container value.</summary>
    /// <param name="index">The zero-based field index.</param>
    /// <returns>The field value, or an empty string.</returns>
    public string GetFieldValue(int index)
    {
        return IsValidFieldIndex(index)
            ? (parsed ?? throw new InvalidOperationException("Invalid internal state.")).Fields[index].Value : string.Empty;
    }

    /// <summary>Returns the first field matching a namespace URI and local name.</summary>
    /// <param name="namespaceUri">The exact namespace URI.</param>
    /// <param name="name">The exact local name.</param>
    /// <returns>The value, or an empty string when absent.</returns>
    public string GetField(string? namespaceUri, string? name)
    {
        if (parsed == null || namespaceUri == null || name == null)
        {
            return string.Empty;
        }
        int index = 0;
        while (index < parsed.Fields.Count)
        {
            XmpField field = parsed.Fields[index];
            if (field.NamespaceUri == namespaceUri && field.Name == name)
            {
                return field.Value;
            }
            index++;
        }
        return string.Empty;
    }

    /// <summary>Returns the default-language Dublin Core title.</summary>
    /// <returns>The title, or an empty string.</returns>
    public string GetTitle()
    {
        return GetField(DublinCoreNamespace, "title");
    }

    /// <summary>Returns the default-language Dublin Core description.</summary>
    /// <returns>The description, or an empty string.</returns>
    public string GetDescription()
    {
        return GetField(DublinCoreNamespace, "description");
    }

    /// <summary>Returns the core XMP creator-tool value.</summary>
    /// <returns>The creator tool, or an empty string.</returns>
    public string GetCreatorTool()
    {
        return GetField(XmpNamespace, "CreatorTool");
    }

    /// <summary>Returns the XMP TIFF orientation.</summary>
    /// <returns>One through eight, or zero when absent or invalid.</returns>
    public int GetOrientation()
    {
        return ParseOrientation(GetField(TiffNamespace, "Orientation"));
    }

    /// <summary>Returns a field reconciled with its mapped Exif tag.</summary>
    /// <param name="exif">The validated Exif metadata.</param>
    /// <param name="namespaceUri">The XMP TIFF or Exif namespace.</param>
    /// <param name="name">The mapped field name.</param>
    /// <param name="preferExif">Whether a present Exif value wins a conflict.</param>
    /// <returns>The selected value, or an empty string.</returns>
    public string GetReconciledField(
        AvifExif exif,
        string namespaceUri,
        string name,
        bool preferExif)
    {
        string xmpValue = GetField(namespaceUri, name);
        string exifValue = XmpReconciler.GetExifValue(
            exif, namespaceUri, name);
        if (preferExif && exifValue.Length != 0)
        {
            return exifValue;
        }
        return xmpValue.Length != 0 ? xmpValue : exifValue;
    }

    /// <summary>Reports whether mapped XMP and Exif values disagree.</summary>
    /// <param name="exif">The validated Exif metadata.</param>
    /// <param name="namespaceUri">The XMP TIFF or Exif namespace.</param>
    /// <param name="name">The mapped field name.</param>
    /// <returns><c>true</c> when both values are present and unequal.</returns>
    public bool HasExifConflict(
        AvifExif exif,
        string namespaceUri,
        string name)
    {
        return XmpReconciler.ValuesConflict(
            GetField(namespaceUri, name),
            XmpReconciler.GetExifValue(exif, namespaceUri, name));
    }

    /// <summary>Copies a valid XMP orientation into mutable Exif metadata.</summary>
    /// <param name="exif">The Exif metadata to update.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode ApplyOrientationToExif(AvifExif? exif)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        if (exif == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        int orientation = GetOrientation();
        return orientation == 0
            ? AvifResultCode.NotFound : exif.SetOrientation(orientation);
    }

    void Initialize(byte[] data, AvifSecurityLimits limits)
    {
        XmpParseResult result = XmpParser.Parse(
            data, ResourceGuard.CopyLimits(limits));
        code = result.Code;
        parsed = result.Parsed;
    }

    bool IsValidFieldIndex(int index)
    {
        return parsed != null && index >= 0 && index < parsed.Fields.Count;
    }

    static int ParseOrientation(string value)
    {
        if (value == null)
        {
            return 0;
        }
        string trimmed = value.Trim();
        if (trimmed.Length != 1
            || trimmed[0] < '1' || trimmed[0] > '8')
        {
            return 0;
        }
        return trimmed[0] - '0';
    }
}
