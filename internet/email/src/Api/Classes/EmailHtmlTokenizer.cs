using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with HTML tokenizer.
/// </summary>
public class EmailHtmlTokenizer : IEmailHtmlTokenizerContract
{
    internal static EmailHtmlTokenizationResult TokenizeImplementationWithHtml(string html)
    {
        return TokenizeImplementationWithHtmlAndLimits(html, new EmailHtmlLimits());
    }

    /// <summary>
    /// Tokenizes the supplied value.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <returns>The HTML tokenization result produced by the operation.</returns>
    public EmailHtmlTokenizationResult TokenizeWithHtml(string html)
    {
        return EmailHtmlTokenizer.TokenizeImplementationWithHtml(html);
    }

    internal static EmailHtmlTokenizationResult TokenizeImplementationWithHtmlAndLimits(string html, EmailHtmlLimits limits)
    {
        if (html == null)
            throw new ArgumentNullException("html");
        if (limits == null)
            throw new ArgumentNullException("limits");
        EmailHtmlLimits snapshot = limits.Clone();
        if (html.Length > snapshot.GetMaxInputCharacters())
            throw new EmailLimitException("MaxHtmlInputCharacters", snapshot.GetMaxInputCharacters(), html.Length);
        EmailHtmlTokenizerEngine engine = new EmailHtmlTokenizerEngine(html, snapshot);
        return engine.Run();
    }

    /// <summary>
    /// Tokenizes the supplied value.
    /// </summary>
    /// <param name = "html">The html used by the operation.</param>
    /// <param name = "limits">The limits used by the operation.</param>
    /// <returns>The HTML tokenization result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailHtmlTokenizationResult TokenizeWithHtmlAndLimits(string html, EmailHtmlLimits limits)
    {
        return EmailHtmlTokenizer.TokenizeImplementationWithHtmlAndLimits(html, limits);
    }
}
