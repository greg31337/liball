using System;

namespace avif;

/// <summary>
/// Owns and validates an AVIF Exif item payload containing classic TIFF metadata.
/// </summary>
public class AvifExif : IAvifExif, IAvifDiagnosticSource
{
    ExifParsedData? parsed;
    AvifSecurityLimits? limits;
    AvifResultCode code;

    /// <summary>Parses an Exif item payload using default resource limits.</summary>
    /// <param name="data">The complete payload, including its TIFF-header displacement.</param>
    /// <returns>The parsed Exif payload.</returns>
    internal static AvifExif Create(byte[] data)
    {
        return new AvifExif(data, new AvifSecurityLimits());
    }

    /// <summary>Parses an Exif item payload using explicit resource limits.</summary>
    /// <param name="data">The complete payload, including its TIFF-header displacement.</param>
    /// <param name="limits">The metadata, Exif, recursion, and temporary limits.</param>
    public AvifExif(byte[] data, AvifSecurityLimits limits)
    {
        Initialize(data, limits);
    }

    /// <summary>Parses an Exif item payload using explicit resource limits.</summary>
    /// <returns>The parsed Exif payload.</returns>
    internal static AvifExif Create(byte[] data, AvifSecurityLimits limits)
    {
        return new AvifExif(data, limits);
    }

    /// <summary>Returns the parsing or most recent update result.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns diagnostics for the most recent Exif parse or update.</summary>
    /// <returns>The structured metadata diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            code,
            AvifDiagnosticCategory.Metadata,
            "The Exif metadata operation did not complete successfully.");
    }

    /// <summary>Returns a copy of the complete Exif item payload.</summary>
    /// <returns>The preserved bytes, or an empty array after parsing failure.</returns>
    public byte[] GetData()
    {
        return parsed == null
            ? System.Array.Empty<byte>() : (byte[])parsed.Data.Clone();
    }

    /// <summary>Returns the absolute payload offset of the TIFF byte-order marker.</summary>
    /// <returns>The TIFF-header offset, or zero when unavailable.</returns>
    public int GetTiffHeaderOffset()
    {
        return parsed == null ? 0 : parsed.TiffOffset;
    }

    /// <summary>Reports whether TIFF integers use little-endian byte order.</summary>
    /// <returns><c>true</c> for Intel byte order; otherwise <c>false</c>.</returns>
    public bool IsLittleEndian()
    {
        return parsed != null && parsed.LittleEndian;
    }

    /// <summary>Returns the number of reachable validated image-file directories.</summary>
    /// <returns>The IFD count.</returns>
    public int GetIfdCount()
    {
        return parsed == null ? 0 : parsed.Ifds.Count;
    }

    /// <summary>Returns one IFD's traversal depth.</summary>
    /// <param name="index">The zero-based IFD index.</param>
    /// <returns>The depth, or minus one for an invalid index.</returns>
    public int GetIfdDepth(int index)
    {
        if (parsed == null || index < 0 || index >= parsed.Ifds.Count)
        {
            return -1;
        }
        return parsed.Ifds[index].Depth;
    }

    /// <summary>Returns the number of validated tags in all reachable IFDs.</summary>
    /// <returns>The tag count.</returns>
    public int GetTagCount()
    {
        return parsed == null ? 0 : parsed.Tags.Count;
    }

    /// <summary>Returns one TIFF tag identifier.</summary>
    /// <param name="index">The zero-based flattened tag index.</param>
    /// <returns>The tag identifier, or zero for an invalid index.</returns>
    public int GetTagIdentifier(int index)
    {
        return IsValidTagIndex(index) ? (parsed ?? throw new InvalidOperationException("Invalid internal state.")).Tags[index].Identifier : 0;
    }

    /// <summary>Returns one TIFF field type code.</summary>
    /// <param name="index">The zero-based flattened tag index.</param>
    /// <returns>The TIFF type code, or zero for an invalid index.</returns>
    public int GetTagType(int index)
    {
        return IsValidTagIndex(index) ? (parsed ?? throw new InvalidOperationException("Invalid internal state.")).Tags[index].Type : 0;
    }

    /// <summary>Returns one tag's element count.</summary>
    /// <param name="index">The zero-based flattened tag index.</param>
    /// <returns>The unsigned TIFF count represented as a long.</returns>
    public long GetTagValueCount(int index)
    {
        return IsValidTagIndex(index) ? (parsed ?? throw new InvalidOperationException("Invalid internal state.")).Tags[index].Count : 0;
    }

    /// <summary>Returns the IFD index containing one tag.</summary>
    /// <param name="index">The zero-based flattened tag index.</param>
    /// <returns>The containing IFD index, or minus one.</returns>
    public int GetTagIfdIndex(int index)
    {
        return IsValidTagIndex(index) ? (parsed ?? throw new InvalidOperationException("Invalid internal state.")).Tags[index].IfdIndex : -1;
    }

    /// <summary>Returns a copy of one tag's encoded value bytes.</summary>
    /// <param name="index">The zero-based flattened tag index.</param>
    /// <returns>The exact value bytes, or an empty array for an invalid index.</returns>
    public byte[] GetTagData(int index)
    {
        if (!IsValidTagIndex(index))
        {
            return System.Array.Empty<byte>();
        }
        ExifTag tag = (parsed ?? throw new InvalidOperationException("Invalid internal state.")).Tags[index];
        return parsed.Data.AsSpan(
            tag.ValueOffset, tag.ValueLength).ToArray();
    }

    /// <summary>Returns the root IFD orientation.</summary>
    /// <returns>One through eight, or zero when the tag is absent.</returns>
    public int GetOrientation()
    {
        if (parsed == null)
        {
            return 0;
        }
        int index = ExifParser.FindOrientationTag(parsed);
        if (index < 0)
        {
            return 0;
        }
        ExifTag tag = parsed.Tags[index];
        return ExifParser.ReadUInt16(
            parsed.Data, tag.ValueOffset, parsed.LittleEndian);
    }

    /// <summary>Updates or adds the root IFD orientation while preserving unknown data.</summary>
    /// <param name="orientation">The TIFF orientation value, one through eight.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetOrientation(int orientation)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        ExifWriteResult write = ExifWriter.SetOrientation(
            parsed, orientation, limits);
        if (write.Code != AvifResultCode.Success)
        {
            return write.Code;
        }
        ExifParseResult reparsed = ExifParser.Parse(write.Data, limits);
        if (reparsed.Code != AvifResultCode.Success)
        {
            return reparsed.Code;
        }
        parsed = reparsed.Parsed;
        return AvifResultCode.Success;
    }

    void Initialize(byte[] data, AvifSecurityLimits requestedLimits)
    {
        limits = ResourceGuard.CopyLimits(requestedLimits);
        ExifParseResult result = ExifParser.Parse(data, limits);
        code = result.Code;
        parsed = result.Parsed;
    }

    bool IsValidTagIndex(int index)
    {
        return parsed != null && index >= 0 && index < parsed.Tags.Count;
    }

    internal ExifParsedData? GetParsedData()
    {
        return parsed;
    }
}
