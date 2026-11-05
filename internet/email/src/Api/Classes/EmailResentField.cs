using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a resent field.
/// </summary>
public class EmailResentField : IEmailResentFieldContract
{
    EmailResentFieldKind kind;
    string originalFieldName;
    EmailDateTime? date;
    EmailAddressList? addresses;
    EmailMessageId? messageId;
    byte[] rawBytes;
    EmailSourcePosition sourcePosition;
    bool obsolete;
    /// <summary>
    /// Creates a new resent field.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "originalFieldName">The original field name used by the operation.</param>
    /// <param name = "date">The date used by the operation.</param>
    /// <param name = "addresses">The addresses used by the operation.</param>
    /// <param name = "messageId">The message ID used by the operation.</param>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "sourcePosition">The source position used by the operation.</param>
    /// <param name = "obsolete">Whether to enable the obsolete behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailResentField(EmailResentFieldKind kind, string? originalFieldName, EmailDateTime? date, EmailAddressList? addresses, EmailMessageId? messageId, byte[] rawBytes, EmailSourcePosition sourcePosition, bool obsolete)
    {
        if (kind < EmailResentFieldKind.Date || kind > EmailResentFieldKind.Extension)
        {
            throw new ArgumentOutOfRangeException("kind");
        }

        if (originalFieldName == null || originalFieldName.Length == 0)
        {
            throw new ArgumentException("A resent field name is required.", "originalFieldName");
        }

        if (rawBytes == null)
        {
            throw new ArgumentNullException("rawBytes");
        }

        if (sourcePosition == null)
        {
            throw new ArgumentNullException("sourcePosition");
        }

        this.kind = kind;
        this.originalFieldName = originalFieldName;
        this.date = date;
        this.addresses = addresses;
        this.messageId = messageId;
        this.rawBytes = Copy(rawBytes);
        this.sourcePosition = sourcePosition;
        this.obsolete = obsolete;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailResentFieldKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the original field name.
    /// </summary>
    /// <returns>The current original field name.</returns>
    public string GetOriginalFieldName()
    {
        return originalFieldName;
    }

    /// <summary>
    /// Determines whether the date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the date is available; otherwise, <c>false</c>.</returns>
    public bool HasDate()
    {
        return date != null;
    }

    /// <summary>
    /// Gets the date.
    /// </summary>
    /// <returns>The current date.</returns>
    public EmailDateTime? GetDate()
    {
        return date;
    }

    /// <summary>
    /// Determines whether the addresses is present or available.
    /// </summary>
    /// <returns><c>true</c> when the addresses is available; otherwise, <c>false</c>.</returns>
    public bool HasAddresses()
    {
        return addresses != null;
    }

    /// <summary>
    /// Gets the addresses.
    /// </summary>
    /// <returns>The current addresses.</returns>
    public EmailAddressList? GetAddresses()
    {
        return addresses;
    }

    /// <summary>
    /// Determines whether the message ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message ID is available; otherwise, <c>false</c>.</returns>
    public bool HasMessageId()
    {
        return messageId != null;
    }

    /// <summary>
    /// Gets the message ID.
    /// </summary>
    /// <returns>The current message ID.</returns>
    public EmailMessageId? GetMessageId()
    {
        return messageId;
    }

    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    public byte[] GetRawBytes()
    {
        return Copy(rawBytes);
    }

    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    public EmailSourcePosition GetSourcePosition()
    {
        return sourcePosition;
    }

    /// <summary>
    /// Determines whether the current value or state uses obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses obsolete syntax; otherwise, <c>false</c>.</returns>
    public bool UsesObsoleteSyntax()
    {
        return obsolete;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
