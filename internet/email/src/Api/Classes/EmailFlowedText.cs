using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with flowed text.
/// </summary>
public class EmailFlowedText : IEmailFlowedTextContract
{
    internal static string DecodeImplementationWithValueAndDeleteSpace(string value, bool deleteSpace)
    {
        EmailFlowedTextOptions options = new EmailFlowedTextOptions();
        options.SetDeleteSpace(deleteSpace);
        return DecodeImplementationWithValueAndOptions(value, options);
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "deleteSpace">Whether to enable the delete space behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public string DecodeWithValueAndDeleteSpace(string value, bool deleteSpace)
    {
        return EmailFlowedText.DecodeImplementationWithValueAndDeleteSpace(value, deleteSpace);
    }

    internal static string DecodeImplementationWithValueAndOptions(string value, EmailFlowedTextOptions options)
    {
        return DecodeResultImplementationWithValueAndOptions(value, options).GetText();
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public string DecodeWithValueAndOptions(string value, EmailFlowedTextOptions options)
    {
        return EmailFlowedText.DecodeImplementationWithValueAndOptions(value, options);
    }

    internal static EmailFlowedTextDecodeResult DecodeResultImplementationWithValueAndDeleteSpace(string value, bool deleteSpace)
    {
        EmailFlowedTextOptions options = new EmailFlowedTextOptions();
        options.SetDeleteSpace(deleteSpace);
        return DecodeResultImplementationWithValueAndOptions(value, options);
    }

    /// <summary>
    /// Decodes the result.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "deleteSpace">Whether to enable the delete space behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailFlowedTextDecodeResult DecodeResultWithValueAndDeleteSpace(string value, bool deleteSpace)
    {
        return EmailFlowedText.DecodeResultImplementationWithValueAndDeleteSpace(value, deleteSpace);
    }

    internal static EmailFlowedTextDecodeResult DecodeResultImplementationWithValueAndOptions(string value, EmailFlowedTextOptions options)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailFlowedTextOptions snapshot = options.Clone();
        EnforceCharacterLimitWithValueAndMaximumCharacters(value, snapshot.GetMaximumCharacters());
        List<EmailFlowedPhysicalLine> lines = ParsePhysicalLines(value);
        List<EmailFlowedTextLine> result = new List<EmailFlowedTextLine>();
        StringBuilder contentBuilder = new StringBuilder();
        bool logicalStart = true;
        int logicalQuoteDepth = 0;
        int index = 0;
        while (index < lines.Count)
        {
            EmailFlowedPhysicalLine line = lines[index];
            if (logicalStart)
                logicalQuoteDepth = line.GetQuoteDepth();
            string content = line.GetContent();
            if (line.IsFlowed() && snapshot.GetDeleteSpace())
                content = content.Substring(0, content.Length - 1);
            contentBuilder.Append(content);
            bool hasNext = index + 1 < lines.Count;
            EmailFlowedPhysicalLine? next = hasNext ? lines[index + 1] : null;
            bool continues = line.IsFlowed() && hasNext && !(next ?? throw new InvalidOperationException()).IsSignatureSeparator() && next.GetQuoteDepth() == line.GetQuoteDepth();
            if (continues)
            {
                logicalStart = false;
            }
            else
            {
                string ending = !line.IsFlowed() || hasNext ? line.GetEnding() : "";
                result.Add(new EmailFlowedTextLine(contentBuilder.ToString(), ending, logicalQuoteDepth));
                contentBuilder.Clear();
                logicalStart = true;
            }

            index++;
        }

        EmailFlowedTextDecodeResult decoded = new EmailFlowedTextDecodeResult(result, snapshot.GetDeleteSpace());
        EnforceCharacterLimitWithLengthAndMaximumCharacters(decoded.GetText().Length, snapshot.GetMaximumCharacters());
        return decoded;
    }

    /// <summary>
    /// Decodes the result.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailFlowedTextDecodeResult DecodeResultWithValueAndOptions(string value, EmailFlowedTextOptions options)
    {
        return EmailFlowedText.DecodeResultImplementationWithValueAndOptions(value, options);
    }

    internal static string EncodeImplementationWithValueUsingString(string value)
    {
        return EncodeImplementationWithValueAndOptionsUsingStringAndEmailFlowedTextOptions(value, new EmailFlowedTextOptions());
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public string EncodeWithValueUsingString(string value)
    {
        return EmailFlowedText.EncodeImplementationWithValueUsingString(value);
    }

    internal static string EncodeImplementationWithValueAndDeleteSpace(string value, bool deleteSpace)
    {
        EmailFlowedTextOptions options = new EmailFlowedTextOptions();
        options.SetDeleteSpace(deleteSpace);
        return EncodeImplementationWithValueAndOptionsUsingStringAndEmailFlowedTextOptions(value, options);
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "deleteSpace">Whether to enable the delete space behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public string EncodeWithValueAndDeleteSpace(string value, bool deleteSpace)
    {
        return EmailFlowedText.EncodeImplementationWithValueAndDeleteSpace(value, deleteSpace);
    }

    internal static string EncodeImplementationWithValueAndOptionsUsingStringAndEmailFlowedTextOptions(string value, EmailFlowedTextOptions options)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailFlowedTextOptions snapshot = options.Clone();
        EnforceCharacterLimitWithValueAndMaximumCharacters(value, snapshot.GetMaximumCharacters());
        List<EmailFlowedSourceLine> lines = ParseSourceLines(value);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < lines.Count)
        {
            EncodeLine(result, lines[index].GetValue(), snapshot);
            index++;
        }

        EnforceCharacterLimitWithLengthAndMaximumCharacters(result.Length, snapshot.GetMaximumCharacters());
        return result.ToString();
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string EncodeWithValueAndOptionsUsingStringAndEmailFlowedTextOptions(string value, EmailFlowedTextOptions options)
    {
        return EmailFlowedText.EncodeImplementationWithValueAndOptionsUsingStringAndEmailFlowedTextOptions(value, options);
    }

    internal static string EncodeImplementationWithValueUsingEmailFlowedTextDecodeResult(EmailFlowedTextDecodeResult value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        EmailFlowedTextOptions options = new EmailFlowedTextOptions();
        options.SetDeleteSpace(value.GetDeleteSpace());
        return EncodeImplementationWithValueAndOptionsUsingEmailFlowedTextDecodeResultAndEmailFlowedTextOptions(value, options);
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string EncodeWithValueUsingEmailFlowedTextDecodeResult(EmailFlowedTextDecodeResult value)
    {
        return EmailFlowedText.EncodeImplementationWithValueUsingEmailFlowedTextDecodeResult(value);
    }

    internal static string EncodeImplementationWithValueAndOptionsUsingEmailFlowedTextDecodeResultAndEmailFlowedTextOptions(EmailFlowedTextDecodeResult value, EmailFlowedTextOptions options)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailFlowedTextOptions snapshot = options.Clone();
        EnforceCharacterLimitWithValueAndMaximumCharacters(value.GetText(), snapshot.GetMaximumCharacters());
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.GetLineCount())
        {
            EmailFlowedTextLine line = value.GetLine(index);
            EncodeContent(result, line.GetQuoteDepth(), line.GetContent(), snapshot);
            index++;
        }

        EnforceCharacterLimitWithLengthAndMaximumCharacters(result.Length, snapshot.GetMaximumCharacters());
        return result.ToString();
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string EncodeWithValueAndOptionsUsingEmailFlowedTextDecodeResultAndEmailFlowedTextOptions(EmailFlowedTextDecodeResult value, EmailFlowedTextOptions options)
    {
        return EmailFlowedText.EncodeImplementationWithValueAndOptionsUsingEmailFlowedTextDecodeResultAndEmailFlowedTextOptions(value, options);
    }

    static void EncodeLine(StringBuilder result, string value, EmailFlowedTextOptions options)
    {
        int quoteDepth = CountQuoteDepth(value);
        string content = value.Substring(quoteDepth);
        EncodeContent(result, quoteDepth, content, options);
    }

    static void EncodeContent(StringBuilder result, int quoteDepth, string content, EmailFlowedTextOptions options)
    {
        if (string.Equals(content, "-- ", StringComparison.Ordinal))
        {
            AppendPhysicalLine(result, quoteDepth, content, false, options.GetDeleteSpace());
            return;
        }

        content = TrimTrailingSpaces(content);
        while (NeedsWrapping(content, quoteDepth, options.GetMaximumLineLength()))
        {
            int split = FindBreak(content, quoteDepth, options.GetMaximumLineLength(), options.GetDeleteSpace());
            if (split < 1)
                break;
            string segment = content.Substring(0, split);
            AppendPhysicalLine(result, quoteDepth, segment, true, options.GetDeleteSpace());
            content = content.Substring(split);
        }

        AppendPhysicalLine(result, quoteDepth, content, false, options.GetDeleteSpace());
    }

    static bool NeedsWrapping(string content, int quoteDepth, int maximumLineLength)
    {
        int stuffing = NeedsStuffing(content) ? 1 : 0;
        return quoteDepth + stuffing + content.Length > maximumLineLength;
    }

    static int FindBreak(string content, int quoteDepth, int maximumLineLength, bool deleteSpace)
    {
        int best = -1;
        int first = -1;
        int index = 0;
        while (index < content.Length - 1)
        {
            if (content[index] == ' ')
            {
                int split = index + 1;
                string segment = content.Substring(0, split);
                if (!IsSignatureSeparator(segment))
                {
                    if (first < 0)
                        first = split;
                    int stuffing = NeedsStuffing(segment) ? 1 : 0;
                    int flow = deleteSpace ? 1 : 0;
                    if (quoteDepth + stuffing + segment.Length + flow <= maximumLineLength)
                        best = split;
                    else if (best >= 0)
                        break;
                }
            }

            index++;
        }

        return best >= 0 ? best : first;
    }

    static void AppendPhysicalLine(StringBuilder result, int quoteDepth, string content, bool flowed, bool deleteSpace)
    {
        AppendQuote(result, quoteDepth);
        if (NeedsStuffing(content))
            result.Append(' ');
        result.Append(content);
        if (flowed && deleteSpace)
            result.Append(' ');
        result.Append("\r\n");
    }

    static bool NeedsStuffing(string content)
    {
        if (content.Length == 0)
            return false;
        if (content[0] == ' ' || content[0] == '>')
            return true;
        return content.StartsWith("From ", StringComparison.Ordinal);
    }

    static string TrimTrailingSpaces(string value)
    {
        int length = value.Length;
        while (length > 0 && value[length - 1] == ' ')
            length--;
        return length == value.Length ? value : value.Substring(0, length);
    }

    static List<EmailFlowedPhysicalLine> ParsePhysicalLines(string value)
    {
        List<EmailFlowedPhysicalLine> result = new List<EmailFlowedPhysicalLine>();
        int start = 0;
        while (start < value.Length)
        {
            int end = start;
            while (end < value.Length && value[end] != '\r' && value[end] != '\n')
                end++;
            string ending = GetEnding(value, end);
            string physical = value.Substring(start, end - start);
            int quoteDepth = CountQuoteDepth(physical);
            string content = physical.Substring(quoteDepth);
            if (content.Length > 0 && content[0] == ' ')
                content = content.Substring(1);
            bool signature = IsSignatureSeparator(content);
            bool flowed = !signature && ending.Length > 0 && content.EndsWith(" ", StringComparison.Ordinal);
            result.Add(new EmailFlowedPhysicalLine(content, ending, quoteDepth, flowed, signature));
            start = end + ending.Length;
        }

        return result;
    }

    static List<EmailFlowedSourceLine> ParseSourceLines(string value)
    {
        List<EmailFlowedSourceLine> result = new List<EmailFlowedSourceLine>();
        int start = 0;
        while (start < value.Length)
        {
            int end = start;
            while (end < value.Length && value[end] != '\r' && value[end] != '\n')
                end++;
            string ending = GetEnding(value, end);
            result.Add(new EmailFlowedSourceLine(value.Substring(start, end - start)));
            start = end + ending.Length;
        }

        return result;
    }

    static string GetEnding(string value, int index)
    {
        if (index >= value.Length)
            return "";
        if (value[index] == '\r' && index + 1 < value.Length && value[index + 1] == '\n')
            return "\r\n";
        return value[index] == '\r' ? "\r" : "\n";
    }

    static int CountQuoteDepth(string value)
    {
        int depth = 0;
        while (depth < value.Length && value[depth] == '>')
            depth++;
        return depth;
    }

    static bool IsSignatureSeparator(string value)
    {
        return string.Equals(value, "-- ", StringComparison.Ordinal);
    }

    static void AppendQuote(StringBuilder result, int quoteDepth)
    {
        int index = 0;
        while (index < quoteDepth)
        {
            result.Append('>');
            index++;
        }
    }

    static void EnforceCharacterLimitWithValueAndMaximumCharacters(string value, int maximumCharacters)
    {
        EnforceCharacterLimitWithLengthAndMaximumCharacters(value.Length, maximumCharacters);
    }

    static void EnforceCharacterLimitWithLengthAndMaximumCharacters(int length, int maximumCharacters)
    {
        if (length > maximumCharacters)
            throw new EmailLimitException("MaxTextCharacters", maximumCharacters, length);
    }
}
