using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefAttribute"/>.
/// </summary>
public interface IEmailTnefAttributeContract
{
    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    int GetIndex();
    /// <summary>
    /// Gets the raw level.
    /// </summary>
    /// <returns>The current raw level.</returns>
    int GetRawLevel();
    /// <summary>
    /// Gets the level.
    /// </summary>
    /// <returns>The current level.</returns>
    EmailTnefAttributeLevel GetLevel();
    /// <summary>
    /// Gets the tag.
    /// </summary>
    /// <returns>The current tag.</returns>
    uint GetTag();
    /// <summary>
    /// Gets the attribute ID.
    /// </summary>
    /// <returns>The current attribute ID.</returns>
    int GetAttributeId();
    /// <summary>
    /// Gets the type code.
    /// </summary>
    /// <returns>The current type code.</returns>
    int GetTypeCode();
    /// <summary>
    /// Gets the attribute type.
    /// </summary>
    /// <returns>The current attribute type.</returns>
    EmailTnefAttributeType GetAttributeType();
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailTnefAttributeKind GetKind();
    /// <summary>
    /// Determines whether the current value or state is known.
    /// </summary>
    /// <returns><c>true</c> when the value or state is known; otherwise, <c>false</c>.</returns>
    bool IsKnown();
    /// <summary>
    /// Gets the declared length.
    /// </summary>
    /// <returns>The current declared length.</returns>
    long GetDeclaredLength();
    /// <summary>
    /// Gets the retained length.
    /// </summary>
    /// <returns>The current retained length.</returns>
    long GetRetainedLength();
    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current data.</returns>
    byte[] GetData();
    /// <summary>
    /// Determines whether the checksum is present or available.
    /// </summary>
    /// <returns><c>true</c> when the checksum is available; otherwise, <c>false</c>.</returns>
    bool HasChecksum();
    /// <summary>
    /// Gets the checksum.
    /// </summary>
    /// <returns>The current checksum.</returns>
    int GetChecksum();
    /// <summary>
    /// Gets the calculated checksum.
    /// </summary>
    /// <returns>The current calculated checksum.</returns>
    int GetCalculatedChecksum();
    /// <summary>
    /// Determines whether the current value or state is checksum valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is checksum valid; otherwise, <c>false</c>.</returns>
    bool IsChecksumValid();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
    /// <summary>
    /// Determines whether the attachment index is present or available.
    /// </summary>
    /// <returns><c>true</c> when the attachment index is available; otherwise, <c>false</c>.</returns>
    bool HasAttachmentIndex();
    /// <summary>
    /// Gets the attachment index.
    /// </summary>
    /// <returns>The current attachment index.</returns>
    int GetAttachmentIndex();
    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    EmailRange GetSourceRange();
    /// <summary>
    /// Gets the header range.
    /// </summary>
    /// <returns>The current header range.</returns>
    EmailRange GetHeaderRange();
    /// <summary>
    /// Gets the data range.
    /// </summary>
    /// <returns>The current data range.</returns>
    EmailRange GetDataRange();
    /// <summary>
    /// Determines whether the checksum range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the checksum range is available; otherwise, <c>false</c>.</returns>
    bool HasChecksumRange();
    /// <summary>
    /// Gets the checksum range.
    /// </summary>
    /// <returns>The current checksum range.</returns>
    EmailRange? GetChecksumRange();
}
