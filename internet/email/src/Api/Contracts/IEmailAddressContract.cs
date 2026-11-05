using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailAddress"/>.
/// </summary>
public interface IEmailAddressContract
{
    /// <summary>
    /// Determines whether the display name is present or available.
    /// </summary>
    /// <returns><c>true</c> when the display name is available; otherwise, <c>false</c>.</returns>
    bool HasDisplayName();
    /// <summary>
    /// Gets the display name.
    /// </summary>
    /// <returns>The current display name.</returns>
    string? GetDisplayName();
    /// <summary>
    /// Gets the local part.
    /// </summary>
    /// <returns>The current local part.</returns>
    string GetLocalPart();
    /// <summary>
    /// Gets the domain.
    /// </summary>
    /// <returns>The current domain.</returns>
    string GetDomain();
    /// <summary>
    /// Determines whether the current value or state is domain literal.
    /// </summary>
    /// <returns><c>true</c> when the value or state is domain literal; otherwise, <c>false</c>.</returns>
    bool IsDomainLiteral();
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
    /// Determines whether the source route is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source route is available; otherwise, <c>false</c>.</returns>
    bool HasSourceRoute();
    /// <summary>
    /// Gets the source route count.
    /// </summary>
    /// <returns>The current source route count.</returns>
    int GetSourceRouteCount();
    /// <summary>
    /// Gets the source route at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested source route.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    string GetSourceRouteAt(int index);
    /// <summary>
    /// Gets the source route.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current source route.</returns>
    List<string> GetSourceRoute();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the raw local part bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw local part bytes.</returns>
    byte[] GetRawLocalPartBytes();
    /// <summary>
    /// Gets the raw domain bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw domain bytes.</returns>
    byte[] GetRawDomainBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
    /// <summary>
    /// Gets the ascii domain.
    /// </summary>
    /// <returns>The current ascii domain.</returns>
    string GetAsciiDomain();
    /// <summary>
    /// Gets the unicode domain.
    /// </summary>
    /// <returns>The current unicode domain.</returns>
    string GetUnicodeDomain();
    /// <summary>
    /// Formats the supplied value.
    /// </summary>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The address format result produced by the operation.</returns>
    EmailAddressFormatResult Format(EmailAddressFormatOptions options);
}
