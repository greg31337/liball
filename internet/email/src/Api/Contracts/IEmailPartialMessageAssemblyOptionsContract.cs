using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPartialMessageAssemblyOptions"/>.
/// </summary>
public interface IEmailPartialMessageAssemblyOptionsContract
{
    /// <summary>
    /// Gets the maximum fragment count.
    /// </summary>
    /// <returns>The current maximum fragment count.</returns>
    int GetMaximumFragmentCount();
    /// <summary>
    /// Sets the maximum fragment count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumFragmentCount(int value);
    /// <summary>
    /// Gets the maximum fragment bytes.
    /// </summary>
    /// <returns>The current maximum fragment bytes.</returns>
    long GetMaximumFragmentBytes();
    /// <summary>
    /// Sets the maximum fragment bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumFragmentBytes(long value);
    /// <summary>
    /// Gets the maximum reassembled bytes.
    /// </summary>
    /// <returns>The current maximum reassembled bytes.</returns>
    long GetMaximumReassembledBytes();
    /// <summary>
    /// Sets the maximum reassembled bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumReassembledBytes(long value);
    /// <summary>
    /// Gets the reparse.
    /// </summary>
    /// <returns><c>true</c> when the reparse setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetReparse();
    /// <summary>
    /// Sets the reparse used by this instance.
    /// </summary>
    /// <param name = "value">Whether to enable the reparse behavior.</param>
    void SetReparse(bool value);
    /// <summary>
    /// Gets the parse options.
    /// </summary>
    /// <returns>The current parse options.</returns>
    EmailParseOptions GetParseOptions();
    /// <summary>
    /// Sets the parse options used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetParseOptions(EmailParseOptions value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailPartialMessageAssemblyOptions Clone();
}
