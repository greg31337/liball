using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailStandardHeaderValue"/>.
/// </summary>
public interface IEmailStandardHeaderValueContract
{
    /// <summary>Gets the value family.</summary>
    /// <returns>The current value family.</returns>
    EmailStandardHeaderValueKind GetKind();
    /// <summary>Gets the original field name.</summary>
    /// <returns>The original field name.</returns>
    string GetOriginalFieldName();
    /// <summary>Gets the primary textual value.</summary>
    /// <returns>The primary value, or null when the family does not use one.</returns>
    string? GetPrimaryValue();
    /// <summary>Gets the secondary textual value.</summary>
    /// <returns>The secondary value, or null when the family does not use one.</returns>
    string? GetSecondaryValue();
    /// <summary>Gets the item count.</summary>
    /// <returns>The current item count.</returns>
    int GetCount();
    /// <summary>Gets an item at a zero-based index.</summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested item.</returns>
    string GetAt(int index);
    /// <summary>Gets all items.</summary>
    /// <returns>A snapshot of the current items.</returns>
    List<string> GetAll();
    /// <summary>Determines whether a return-path mailbox is available.</summary>
    /// <returns><c>true</c> when a mailbox is available; otherwise, <c>false</c>.</returns>
    bool HasAddress();
    /// <summary>Gets the return-path mailbox.</summary>
    /// <returns>The mailbox, or null when none is available.</returns>
    EmailAddress? GetAddress();
    /// <summary>Determines whether this is the null return path.</summary>
    /// <returns><c>true</c> for the null return path; otherwise, <c>false</c>.</returns>
    bool IsNullPath();
    /// <summary>Gets the MIME major version.</summary>
    /// <returns>The major version, or zero for other families.</returns>
    int GetMajorVersion();
    /// <summary>Gets the MIME minor version.</summary>
    /// <returns>The minor version, or zero for other families.</returns>
    int GetMinorVersion();
    /// <summary>Determines whether a recognized transfer encoding is available.</summary>
    /// <returns><c>true</c> when the encoding is recognized; otherwise, <c>false</c>.</returns>
    bool HasKnownTransferEncoding();
    /// <summary>Gets the recognized transfer encoding.</summary>
    /// <returns>The recognized encoding.</returns>
    EmailTransferEncodingKind GetTransferEncodingKind();
    /// <summary>Gets the original raw bytes.</summary>
    /// <returns>A copy of the original raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>Gets the source position.</summary>
    /// <returns>The source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>Determines whether tolerant recovery was required.</summary>
    /// <returns><c>true</c> when the value is malformed; otherwise, <c>false</c>.</returns>
    bool IsMalformed();
}
