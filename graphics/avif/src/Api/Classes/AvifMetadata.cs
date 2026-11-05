using System;
using System.IO;

namespace avif;

/// <summary>Owns one metadata item payload for attachment to an authored image.</summary>
public class AvifMetadata : IAvifMetadata, IAvifDiagnosticSource
{
    AvifMetadataKind kind;
    string? itemType;
    string? contentType;
    string? contentEncoding;
    string? uriType;
    byte[]? data;
    AvifResultCode code;

    /// <summary>Creates an Exif or XMP metadata item from caller-owned bytes.</summary>
    /// <param name="kind">The Exif or XMP representation.</param>
    /// <param name="data">The complete item payload, which is copied.</param>
    /// <returns>The initialized metadata item.</returns>
    internal static AvifMetadata Create(AvifMetadataKind kind, byte[] data)
    {
        if (kind == AvifMetadataKind.Exif)
        {
            AvifMetadata exif = new AvifMetadata("Exif", string.Empty, data);
            exif.kind = kind;
            return exif;
        }
        if (kind == AvifMetadataKind.Xmp)
        {
            AvifMetadata xmp = new AvifMetadata(
                "mime",
                "application/rdf+xml",
                data);
            xmp.kind = kind;
            return xmp;
        }
        AvifMetadata invalid = new AvifMetadata(
            string.Empty,
            string.Empty,
            data);
        invalid.kind = kind;
        invalid.code = AvifResultCode.InvalidArgument;
        return invalid;
    }

    /// <summary>Creates a custom metadata item from caller-selected container identifiers.</summary>
    /// <param name="itemType">The four-character item type.</param>
    /// <param name="contentType">The MIME type for a <c>mime</c> item, otherwise an empty string.</param>
    /// <param name="data">The complete item payload, which is copied.</param>
    public AvifMetadata(string itemType, string contentType, byte[]? data)
    {
        Initialize(AvifMetadataKind.Custom, itemType, contentType, data);
    }

    /// <summary>Creates a custom metadata item.</summary>
    /// <returns>The initialized metadata item.</returns>
    internal static AvifMetadata Create(
        string itemType,
        string contentType,
        byte[]? data)
    {
        return new AvifMetadata(itemType, contentType, data);
    }

    /// <summary>Creates an Exif metadata item.</summary>
    /// <returns>The initialized metadata item.</returns>
    internal static AvifMetadata Create(AvifExif exif)
    {
        return FromExif(exif);
    }

    /// <summary>Creates an XMP metadata item.</summary>
    /// <returns>The initialized metadata item.</returns>
    internal static AvifMetadata Create(AvifXmp xmp)
    {
        return FromXmp(xmp);
    }

    /// <summary>Creates an Exif metadata item from validated Exif data.</summary>
    /// <param name="exif">The Exif object to snapshot.</param>
    /// <returns>The initialized Exif metadata item.</returns>
    internal static AvifMetadata FromExif(AvifExif exif)
    {
        AvifMetadata result;
        if (exif == null)
        {
            result = new AvifMetadata("Exif", string.Empty, null);
        }
        else
        {
            result = new AvifMetadata(
                "Exif",
                string.Empty,
                exif.GetData());
            if (exif.GetCode() != AvifResultCode.Success)
            {
                result.code = exif.GetCode();
            }
        }
        result.kind = AvifMetadataKind.Exif;
        return result;
    }

    /// <summary>Creates an XMP metadata item from a validated XMP packet.</summary>
    /// <param name="xmp">The XMP object to snapshot.</param>
    /// <returns>The initialized XMP metadata item.</returns>
    internal static AvifMetadata FromXmp(AvifXmp xmp)
    {
        AvifMetadata result;
        if (xmp == null)
        {
            result = new AvifMetadata(
                "mime",
                "application/rdf+xml",
                null);
        }
        else
        {
            result = new AvifMetadata(
                "mime",
                "application/rdf+xml",
                xmp.GetData());
            if (xmp.GetCode() != AvifResultCode.Success)
            {
                result.code = xmp.GetCode();
            }
        }
        result.kind = AvifMetadataKind.Xmp;
        return result;
    }

    /// <summary>Returns the construction and validation result.</summary>
    /// <returns>The stable metadata result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns diagnostics for metadata construction or validation.</summary>
    /// <returns>The structured metadata diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            code,
            AvifDiagnosticCategory.Metadata,
            "The metadata operation did not complete successfully.");
    }

    /// <summary>Returns the interpreted metadata kind.</summary>
    /// <returns>The metadata kind.</returns>
    public AvifMetadataKind GetKind()
    {
        return kind;
    }

    /// <summary>Returns the four-character item type.</summary>
    /// <returns>The item type, or an empty string after failure.</returns>
    public string? GetItemType()
    {
        return code == AvifResultCode.Success ? itemType : string.Empty;
    }

    /// <summary>Returns the MIME content type.</summary>
    /// <returns>The MIME type, or an empty string for non-MIME metadata.</returns>
    public string? GetContentType()
    {
        return code == AvifResultCode.Success ? contentType : string.Empty;
    }

    /// <summary>Returns the MIME content encoding.</summary>
    /// <returns>The content encoding, or an empty string for non-MIME metadata.</returns>
    public string? GetContentEncoding()
    {
        return code == AvifResultCode.Success ? contentEncoding : string.Empty;
    }

    /// <summary>Sets the optional MIME content encoding.</summary>
    /// <param name="value">The encoding token or an empty string.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetContentEncoding(string value)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        if (itemType != "mime" || value == null
            || value.IndexOf('\0') >= 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        contentEncoding = value;
        return AvifResultCode.Success;
    }

    /// <summary>Returns the URI metadata item type identifier.</summary>
    /// <returns>The URI identifier, or an empty string for non-URI metadata.</returns>
    public string? GetUriType()
    {
        return code == AvifResultCode.Success ? uriType : string.Empty;
    }

    /// <summary>Sets the required URI identifier for a <c>uri </c> metadata item.</summary>
    /// <param name="value">The nonempty URI identifier.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetUriType(string value)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        if (itemType != "uri " || value == null
            || value.Length == 0 || value.IndexOf('\0') >= 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        uriType = value;
        return AvifResultCode.Success;
    }

    /// <summary>Returns an independent payload copy.</summary>
    /// <returns>The metadata bytes, or an empty array after failure.</returns>
    public byte[] GetData()
    {
        return code == AvifResultCode.Success
            ? (byte[])(data ?? throw new InvalidOperationException("Invalid internal state.")).Clone()
            : System.Array.Empty<byte>();
    }

    /// <summary>Returns the metadata payload byte count without copying it.</summary>
    /// <returns>The payload length, or zero after failure.</returns>
    public int GetDataLength()
    {
        return code == AvifResultCode.Success
            ? (data ?? throw new InvalidOperationException("Invalid internal state.")).Length
            : 0;
    }

    /// <summary>Copies the metadata payload into a caller-provided buffer.</summary>
    /// <param name="destination">The destination buffer, which may be larger than the payload.</param>
    /// <returns>Success, BufferTooSmall, or the metadata failure code.</returns>
    public AvifResultCode CopyDataTo(Span<byte> destination)
    {
        return code == AvifResultCode.Success
            ? CallerOutput.Copy(
                data ?? throw new InvalidOperationException("Invalid internal state."),
                destination)
            : code;
    }

    /// <summary>Writes the metadata payload without closing the destination stream.</summary>
    /// <param name="destination">The writable destination.</param>
    /// <returns>Success, InvalidArgument, or the metadata failure code.</returns>
    public AvifResultCode WriteDataTo(Stream destination)
    {
        return code == AvifResultCode.Success
            ? CallerOutput.Write(
                data ?? throw new InvalidOperationException("Invalid internal state."),
                destination)
            : code;
    }

    void Initialize(
        AvifMetadataKind requestedKind,
        string requestedItemType,
        string requestedContentType,
        byte[]? requestedData)
    {
        kind = requestedKind;
        itemType = requestedItemType == null ? string.Empty : requestedItemType;
        contentType = requestedContentType == null
            ? string.Empty : requestedContentType;
        contentEncoding = string.Empty;
        uriType = string.Empty;
        data = requestedData == null
            ? System.Array.Empty<byte>() : (byte[])requestedData.Clone();
        code = requestedData == null
            || !IsoBoxType.IsValid(itemType)
            || itemType == "uuid"
            || itemType == "free"
            || itemType == "skip"
            || itemType == "av01"
            || itemType == "grid"
            || itemType == "tmap"
            || itemType == "sato"
            || contentType.IndexOf('\0') >= 0
            || itemType == "mime" && contentType.Length == 0
            || itemType != "mime" && contentType.Length != 0
            ? AvifResultCode.InvalidArgument
            : AvifResultCode.Success;
    }
}
