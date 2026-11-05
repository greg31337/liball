using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailFlowedTextLine"/>.
/// </summary>
public interface IEmailFlowedTextLineContract
{
    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <returns>The current content.</returns>
    string GetContent();
    /// <summary>
    /// Gets the ending.
    /// </summary>
    /// <returns>The current ending.</returns>
    string GetEnding();
    /// <summary>
    /// Gets the quote depth.
    /// </summary>
    /// <returns>The current quote depth.</returns>
    int GetQuoteDepth();
}
