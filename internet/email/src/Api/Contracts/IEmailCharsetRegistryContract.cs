using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCharsetRegistry"/>.
/// </summary>
public interface IEmailCharsetRegistryContract
{
    /// <summary>
    /// Looks up the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The charset lookup result produced by the operation.</returns>
    EmailCharsetLookupResult Lookup(string? name);
    /// <summary>
    /// Determines whether the collection contains the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool Contains(string name);
    /// <summary>
    /// Registers the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "encoding">The encoding used by the operation.</param>
    /// <param name = "replaceExisting">Whether to enable the replace existing behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    void Register(string name, Encoding encoding, bool replaceExisting);
    /// <summary>
    /// Registers the alias.
    /// </summary>
    /// <param name = "alias">The alias used by the operation.</param>
    /// <param name = "registeredName">The registered name used by the operation.</param>
    /// <param name = "replaceExisting">Whether to enable the replace existing behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    void RegisterAlias(string alias, string registeredName, bool replaceExisting);
    /// <summary>
    /// Gets the registered name count.
    /// </summary>
    /// <returns>The current registered name count.</returns>
    int GetRegisteredNameCount();
    /// <summary>
    /// Gets the registered names.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current registered names.</returns>
    List<string> GetRegisteredNames();
    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "declaredCharset">The declared charset used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailCharsetDecodeResult Decode(ReadOnlySpan<byte> input, string? declaredCharset, EmailCharsetDecodeOptions options);
}
