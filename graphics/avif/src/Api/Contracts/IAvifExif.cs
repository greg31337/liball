using System;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifExif"/>.</summary>
public interface IAvifExif
{
    /// <summary>Returns the parsing or most recent update result.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns a copy of the complete Exif item payload.</summary>
    /// <returns>The preserved bytes, or an empty array after parsing failure.</returns>
    byte[] GetData();

    /// <summary>Returns the absolute payload offset of the TIFF byte-order marker.</summary>
    /// <returns>The TIFF-header offset, or zero when unavailable.</returns>
    int GetTiffHeaderOffset();

    /// <summary>Reports whether TIFF integers use little-endian byte order.</summary>
    /// <returns><c>true</c> for Intel byte order; otherwise <c>false</c>.</returns>
    bool IsLittleEndian();

    /// <summary>Returns the number of reachable validated image-file directories.</summary>
    /// <returns>The IFD count.</returns>
    int GetIfdCount();

    /// <summary>Returns one IFD's traversal depth.</summary>
    /// <param name="index">The zero-based IFD index.</param>
    /// <returns>The depth, or minus one for an invalid index.</returns>
    int GetIfdDepth(int index);

    /// <summary>Returns the number of validated tags in all reachable IFDs.</summary>
    /// <returns>The tag count.</returns>
    int GetTagCount();

    /// <summary>Returns one TIFF tag identifier.</summary>
    /// <param name="index">The zero-based flattened tag index.</param>
    /// <returns>The tag identifier, or zero for an invalid index.</returns>
    int GetTagIdentifier(int index);

    /// <summary>Returns one TIFF field type code.</summary>
    /// <param name="index">The zero-based flattened tag index.</param>
    /// <returns>The TIFF type code, or zero for an invalid index.</returns>
    int GetTagType(int index);

    /// <summary>Returns one tag's element count.</summary>
    /// <param name="index">The zero-based flattened tag index.</param>
    /// <returns>The unsigned TIFF count represented as a long.</returns>
    long GetTagValueCount(int index);

    /// <summary>Returns the IFD index containing one tag.</summary>
    /// <param name="index">The zero-based flattened tag index.</param>
    /// <returns>The containing IFD index, or minus one.</returns>
    int GetTagIfdIndex(int index);

    /// <summary>Returns a copy of one tag's encoded value bytes.</summary>
    /// <param name="index">The zero-based flattened tag index.</param>
    /// <returns>The exact value bytes, or an empty array for an invalid index.</returns>
    byte[] GetTagData(int index);

    /// <summary>Returns the root IFD orientation.</summary>
    /// <returns>One through eight, or zero when the tag is absent.</returns>
    int GetOrientation();

    /// <summary>Updates or adds the root IFD orientation while preserving unknown data.</summary>
    /// <param name="orientation">The TIFF orientation value, one through eight.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetOrientation(int orientation);
}
