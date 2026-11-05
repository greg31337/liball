using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAddressGroup"/>.
/// </summary>
public interface IEmailAddressGroupContract
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string GetName();
    /// <summary>
    /// Gets the address count.
    /// </summary>
    /// <returns>The current address count.</returns>
    int GetAddressCount();
    /// <summary>
    /// Gets the address.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current address.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailAddress GetAddress(int index);
    /// <summary>
    /// Gets the addresses.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current addresses.</returns>
    List<EmailAddress> GetAddresses();
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
    /// Determines whether the current value or state is internationalized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is internationalized; otherwise, <c>false</c>.</returns>
    bool IsInternationalized();
    /// <summary>
    /// Determines whether the current value or state uses obsolete syntax.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses obsolete syntax; otherwise, <c>false</c>.</returns>
    bool UsesObsoleteSyntax();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    EmailAddressFormatResult Format(EmailAddressFormatOptions options);
}
