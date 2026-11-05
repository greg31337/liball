using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlTokenizer"/>.
/// </summary>
public interface IEmailHtmlTokenizerContract
{
    /// <summary>
    /// Tokenizes the supplied value.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <returns>The HTML tokenization result produced by the operation.</returns>
    EmailHtmlTokenizationResult TokenizeWithHtml(string html);
    /// <summary>
    /// Tokenizes the supplied value.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <param name = "limits">The limits used by the operation.</param>
    /// <returns>The HTML tokenization result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailHtmlTokenizationResult TokenizeWithHtmlAndLimits(string html, EmailHtmlLimits limits);
}
