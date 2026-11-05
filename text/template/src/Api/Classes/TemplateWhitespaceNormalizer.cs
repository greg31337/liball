using System;
using System.Collections.Generic;
using System.Text;

namespace template;

/// <summary>
/// Normalizes line endings and optional trailing or repeated whitespace without interpreting template syntax.
/// </summary>
public class TemplateWhitespaceNormalizer : ITemplateWhitespaceNormalizer
{
    /// <summary>
    /// Initializes a whitespace normalizer with no retained mutable state.
    /// </summary>
    public TemplateWhitespaceNormalizer()
    {
    }

    /// <summary>
    /// Normalizes supplied text according to the selected whitespace policy.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="mode">The behavior mode to apply.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string Normalize(string text, TemplateWhitespaceMode mode)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (mode == TemplateWhitespaceMode.NormalizeLineFeeds)
        {
            return ConvertToLineFeeds(text);
        }

        if (mode == TemplateWhitespaceMode.NormalizeCarriageReturnLineFeeds)
        {
            return ConvertToCarriageReturnLineFeeds(text);
        }

        if (mode == TemplateWhitespaceMode.Minimize)
        {
            return MinimizeWhitespace(text);
        }

        return text;
    }

    /// <summary>
    /// Normalizes supplied text according to the selected whitespace policy.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string NormalizeWithOptions(string text, TemplateOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return Normalize(text, options.WhitespaceMode);
    }

    /// <summary>
    /// Converts carriage-return and carriage-return-line-feed sequences to line feeds.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string ConvertToLineFeeds(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (value == '\r')
            {
                builder.Append('\n');
                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index = index + 1;
                }
            }
            else
            {
                builder.Append(value);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Converts every supported newline sequence to carriage return followed by line feed.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string ConvertToCarriageReturnLineFeeds(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string normalized = ConvertToLineFeeds(text);
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < normalized.Length)
        {
            if (normalized[index] == '\n')
            {
                builder.Append("\r\n");
            }
            else
            {
                builder.Append(normalized[index]);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Removes spaces and tabs immediately before each line ending.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string TrimTrailingLineWhitespace(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        StringBuilder pendingWhitespace = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char value = text[index];
            if (value == ' ' || value == '\t')
            {
                pendingWhitespace.Append(value);
            }
            else if (value == '\n' || value == '\r')
            {
                pendingWhitespace.Clear();
                builder.Append(value);
            }
            else
            {
                builder.Append(pendingWhitespace.ToString());
                pendingWhitespace.Clear();
                builder.Append(value);
            }

            index = index + 1;
        }

        builder.Append(pendingWhitespace.ToString());

        return builder.ToString();
    }

    /// <summary>
    /// Trims each line and collapses consecutive blank lines while preserving tag contents.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string MinimizeWhitespace(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        string normalized = TrimFinalHorizontalWhitespace(TrimTrailingLineWhitespace(ConvertToLineFeeds(text)));
        StringBuilder builder = new StringBuilder();
        int newlineCount = 0;
        int index = 0;
        while (index < normalized.Length)
        {
            char value = normalized[index];
            if (value == '\n')
            {
                newlineCount = newlineCount + 1;
                if (newlineCount <= 2)
                {
                    builder.Append(value);
                }
            }
            else
            {
                newlineCount = 0;
                builder.Append(value);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal string TrimFinalHorizontalWhitespace(string text)
    {
        int end = text.Length;
        while (end > 0 && (text[end - 1] == ' ' || text[end - 1] == '\t'))
        {
            end = end - 1;
        }

        return text.Substring(0, end);
    }
}
