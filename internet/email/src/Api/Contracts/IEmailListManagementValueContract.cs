using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailListManagementValue"/>.
/// </summary>
public interface IEmailListManagementValueContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailListManagementKind GetKind();
    /// <summary>
    /// Gets the command count.
    /// </summary>
    /// <returns>The current command count.</returns>
    int GetCommandCount();
    /// <summary>
    /// Gets the command.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current command.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailListCommand GetCommand(int index);
    /// <summary>
    /// Gets the commands.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current commands.</returns>
    List<EmailListCommand> GetCommands();
    /// <summary>
    /// Determines whether the current value or state is posting prohibited.
    /// </summary>
    /// <returns><c>true</c> when the value or state is posting prohibited; otherwise, <c>false</c>.</returns>
    bool IsPostingProhibited();
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
}
