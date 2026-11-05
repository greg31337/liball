using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSourcePosition"/>.
/// </summary>
public interface IEmailSourcePositionContract
{
    /// <summary>
    /// Gets the byte offset.
    /// </summary>
    /// <returns>The current byte offset.</returns>
    long GetByteOffset();
    /// <summary>
    /// Gets the line number.
    /// </summary>
    /// <returns>The current line number.</returns>
    long GetLineNumber();
    /// <summary>
    /// Gets the column number.
    /// </summary>
    /// <returns>The current column number.</returns>
    long GetColumnNumber();
}
