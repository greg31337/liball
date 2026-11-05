using System;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifXmp"/>.</summary>
public interface IAvifXmp
{
    /// <summary>Returns the packet validation result.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns a byte-for-byte copy of the original packet.</summary>
    /// <returns>The preserved UTF-8 bytes, or an empty array after failure.</returns>
    byte[] GetData();

    /// <summary>Returns the number of flattened XMP fields.</summary>
    /// <returns>The field count.</returns>
    int GetFieldCount();

    /// <summary>Returns one field's namespace URI.</summary>
    /// <param name="index">The zero-based field index.</param>
    /// <returns>The namespace URI, or an empty string.</returns>
    string GetFieldNamespace(int index);

    /// <summary>Returns one field's local name.</summary>
    /// <param name="index">The zero-based field index.</param>
    /// <returns>The local name, or an empty string.</returns>
    string GetFieldName(int index);

    /// <summary>Returns one field's simple or selected RDF container value.</summary>
    /// <param name="index">The zero-based field index.</param>
    /// <returns>The field value, or an empty string.</returns>
    string GetFieldValue(int index);

    /// <summary>Returns the first field matching a namespace URI and local name.</summary>
    /// <param name="namespaceUri">The exact namespace URI.</param>
    /// <param name="name">The exact local name.</param>
    /// <returns>The value, or an empty string when absent.</returns>
    string GetField(string? namespaceUri, string? name);

    /// <summary>Returns the default-language Dublin Core title.</summary>
    /// <returns>The title, or an empty string.</returns>
    string GetTitle();

    /// <summary>Returns the default-language Dublin Core description.</summary>
    /// <returns>The description, or an empty string.</returns>
    string GetDescription();

    /// <summary>Returns the core XMP creator-tool value.</summary>
    /// <returns>The creator tool, or an empty string.</returns>
    string GetCreatorTool();

    /// <summary>Returns the XMP TIFF orientation.</summary>
    /// <returns>One through eight, or zero when absent or invalid.</returns>
    int GetOrientation();

    /// <summary>Returns a field reconciled with its mapped Exif tag.</summary>
    /// <param name="exif">The validated Exif metadata.</param>
    /// <param name="namespaceUri">The XMP TIFF or Exif namespace.</param>
    /// <param name="name">The mapped field name.</param>
    /// <param name="preferExif">Whether a present Exif value wins a conflict.</param>
    /// <returns>The selected value, or an empty string.</returns>
    string GetReconciledField(
        AvifExif exif,
        string namespaceUri,
        string name,
        bool preferExif);

    /// <summary>Reports whether mapped XMP and Exif values disagree.</summary>
    /// <param name="exif">The validated Exif metadata.</param>
    /// <param name="namespaceUri">The XMP TIFF or Exif namespace.</param>
    /// <param name="name">The mapped field name.</param>
    /// <returns><c>true</c> when both values are present and unequal.</returns>
    bool HasExifConflict(
        AvifExif exif,
        string namespaceUri,
        string name);

    /// <summary>Copies a valid XMP orientation into mutable Exif metadata.</summary>
    /// <param name="exif">The Exif metadata to update.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode ApplyOrientationToExif(AvifExif? exif);
}
