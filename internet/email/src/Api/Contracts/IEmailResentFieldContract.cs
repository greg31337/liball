using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailResentField"/>.
/// </summary>
public interface IEmailResentFieldContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailResentFieldKind GetKind();
    /// <summary>
    /// Gets the original field name.
    /// </summary>
    /// <returns>The current original field name.</returns>
    string GetOriginalFieldName();
    /// <summary>
    /// Determines whether the date is present or available.
    /// </summary>
    /// <returns><c>true</c> when the date is available; otherwise, <c>false</c>.</returns>
    bool HasDate();
    /// <summary>
    /// Gets the date.
    /// </summary>
    /// <returns>The current date.</returns>
    EmailDateTime? GetDate();
    /// <summary>
    /// Determines whether the addresses is present or available.
    /// </summary>
    /// <returns><c>true</c> when the addresses is available; otherwise, <c>false</c>.</returns>
    bool HasAddresses();
    /// <summary>
    /// Gets the addresses.
    /// </summary>
    /// <returns>The current addresses.</returns>
    EmailAddressList? GetAddresses();
    /// <summary>
    /// Determines whether the message ID is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message ID is available; otherwise, <c>false</c>.</returns>
    bool HasMessageId();
    /// <summary>
    /// Gets the message ID.
    /// </summary>
    /// <returns>The current message ID.</returns>
    EmailMessageId? GetMessageId();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>
    /// Determines whether the current value or state uses obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses obsolete syntax; otherwise, <c>false</c>.</returns>
    bool UsesObsoleteSyntax();
}
