using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFlowedTextDecodeResult"/>.
/// </summary>
public interface IEmailFlowedTextDecodeResultContract
{
    /// <summary>
    /// Gets the line count.
    /// </summary>
    /// <returns>The current line count.</returns>
    int GetLineCount();
    /// <summary>
    /// Gets the line.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current line.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailFlowedTextLine GetLine(int index);
    /// <summary>
    /// Gets the lines.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current lines.</returns>
    List<EmailFlowedTextLine> GetLines();
    /// <summary>
    /// Gets the delete space.
    /// </summary>
    /// <returns><c>true</c> when the delete space setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetDeleteSpace();
    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    string GetText();
}
