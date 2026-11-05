using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Stores a lossless structured value for standard fields that do not share another public model.
/// </summary>
public class EmailStandardHeaderValue : IEmailStandardHeaderValueContract
{
    EmailStandardHeaderValueKind kind;
    string originalFieldName;
    string? primaryValue;
    string? secondaryValue;
    List<string> values;
    EmailAddress? address;
    bool nullPath;
    int majorVersion;
    int minorVersion;
    bool knownTransferEncoding;
    EmailTransferEncodingKind transferEncodingKind;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    bool malformed;

    /// <summary>
    /// Creates a standard header value.
    /// </summary>
    /// <param name = "kind">The value family.</param>
    /// <param name = "originalFieldName">The original field name.</param>
    /// <param name = "primaryValue">The primary textual value.</param>
    /// <param name = "secondaryValue">The secondary textual value.</param>
    /// <param name = "values">The ordered item values.</param>
    /// <param name = "address">The return-path mailbox.</param>
    /// <param name = "nullPath">Whether this is the null return path.</param>
    /// <param name = "majorVersion">The MIME major version.</param>
    /// <param name = "minorVersion">The MIME minor version.</param>
    /// <param name = "knownTransferEncoding">Whether the transfer encoding is recognized.</param>
    /// <param name = "transferEncodingKind">The recognized transfer encoding.</param>
    /// <param name = "rawBytes">The original raw bytes.</param>
    /// <param name = "sourcePosition">The source position.</param>
    /// <param name = "malformed">Whether tolerant recovery was required.</param>
    /// <exception cref = "ArgumentException">Thrown when arguments are inconsistent.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an enum or numeric argument is outside the supported range.</exception>
    public EmailStandardHeaderValue(EmailStandardHeaderValueKind kind, string originalFieldName, string? primaryValue, string? secondaryValue, List<string> values, EmailAddress? address, bool nullPath, int majorVersion, int minorVersion, bool knownTransferEncoding, EmailTransferEncodingKind transferEncodingKind, byte[] rawBytes, EmailSourcePosition sourcePosition, bool malformed)
    {
        if (kind < EmailStandardHeaderValueKind.ReturnPath || kind > EmailStandardHeaderValueKind.Trace)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (originalFieldName == null || values == null || rawBytes == null || sourcePosition == null)
        {
            throw new ArgumentNullException("originalFieldName");
        }

        if (originalFieldName.Length == 0 || majorVersion < 0 || minorVersion < 0)
        {
            throw new ArgumentException("The standard header value arguments are inconsistent.", "originalFieldName");
        }

        if (nullPath && address != null || knownTransferEncoding && kind != EmailStandardHeaderValueKind.ContentTransferEncoding)
        {
            throw new ArgumentException("The standard header value arguments are inconsistent.", "kind");
        }

        this.kind = kind;
        this.originalFieldName = originalFieldName;
        this.primaryValue = primaryValue;
        this.secondaryValue = secondaryValue;
        this.values = new List<string>(values);
        this.address = address;
        this.nullPath = nullPath;
        this.majorVersion = majorVersion;
        this.minorVersion = minorVersion;
        this.knownTransferEncoding = knownTransferEncoding;
        this.transferEncodingKind = transferEncodingKind;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
        this.malformed = malformed;
    }

    /// <summary>Gets the value family.</summary>
    /// <returns>The current value family.</returns>
    public EmailStandardHeaderValueKind GetKind() { return kind; }
    /// <summary>Gets the original field name.</summary>
    /// <returns>The original field name.</returns>
    public string GetOriginalFieldName() { return originalFieldName; }
    /// <summary>Gets the primary textual value.</summary>
    /// <returns>The primary value, or null.</returns>
    public string? GetPrimaryValue() { return primaryValue; }
    /// <summary>Gets the secondary textual value.</summary>
    /// <returns>The secondary value, or null.</returns>
    public string? GetSecondaryValue() { return secondaryValue; }
    /// <summary>Gets the item count.</summary>
    /// <returns>The current item count.</returns>
    public int GetCount() { return values.Count; }
    /// <summary>Gets an item at a zero-based index.</summary>
    /// <param name = "index">The zero-based index.</param>
    /// <returns>The requested item.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when the index is outside the collection.</exception>
    public string GetAt(int index)
    {
        if (index < 0 || index >= values.Count) { throw new ArgumentOutOfRangeException("index"); }
        return values[index];
    }
    /// <summary>Gets all items.</summary>
    /// <returns>A snapshot of the current items.</returns>
    public List<string> GetAll() { return new List<string>(values); }
    /// <summary>Determines whether a return-path mailbox is available.</summary>
    /// <returns><c>true</c> when available.</returns>
    public bool HasAddress() { return address != null; }
    /// <summary>Gets the return-path mailbox.</summary>
    /// <returns>The mailbox, or null.</returns>
    public EmailAddress? GetAddress() { return address; }
    /// <summary>Determines whether this is the null return path.</summary>
    /// <returns><c>true</c> for the null path.</returns>
    public bool IsNullPath() { return nullPath; }
    /// <summary>Gets the MIME major version.</summary>
    /// <returns>The major version.</returns>
    public int GetMajorVersion() { return majorVersion; }
    /// <summary>Gets the MIME minor version.</summary>
    /// <returns>The minor version.</returns>
    public int GetMinorVersion() { return minorVersion; }
    /// <summary>Determines whether a recognized transfer encoding is available.</summary>
    /// <returns><c>true</c> when recognized.</returns>
    public bool HasKnownTransferEncoding() { return knownTransferEncoding; }
    /// <summary>Gets the recognized transfer encoding.</summary>
    /// <returns>The recognized transfer encoding.</returns>
    public EmailTransferEncodingKind GetTransferEncodingKind() { return transferEncodingKind; }
    /// <summary>Gets the original raw bytes.</summary>
    /// <returns>A copy of the raw bytes.</returns>
    public byte[] GetRawBytes() { return Copy(rawBytes); }
    /// <summary>Gets the source position.</summary>
    /// <returns>The source position.</returns>
    public EmailSourcePosition GetSourcePosition() { return sourcePosition; }
    /// <summary>Determines whether tolerant recovery was required.</summary>
    /// <returns><c>true</c> when malformed.</returns>
    public bool IsMalformed() { return malformed; }

    static byte[] Copy(byte[] input)
    {
        byte[] copy = new byte[input.Length];
        input.CopyTo(copy, 0);
        return copy;
    }
}
