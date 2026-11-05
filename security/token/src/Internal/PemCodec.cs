using System;
using System.Text;

namespace token;

/// <summary>
/// Provides PEM block reading and writing helpers.
/// </summary>
internal static class PemCodec
{
    const string BeginPrefix = "-----BEGIN ";
    const string EndPrefix = "-----END ";
    const string BoundarySuffix = "-----";

    /// <summary>
    /// Writes binary data as a PEM block with 64-character base64 lines.
    /// </summary>
    /// <param name="label">The PEM label.</param>
    /// <param name="bytes">The binary data to write.</param>
    /// <returns>A result containing canonical PEM text, or failure details for an invalid label.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>label</c> or <c>bytes</c> is null.</exception>
    internal static TokenTextResult Write(string label, byte[] bytes)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        return Write(label, bytes.AsSpan());
    }

    /// <summary>
    /// Writes a binary data span as a PEM block with 64-character base64 lines.
    /// </summary>
    /// <param name="label">The PEM label.</param>
    /// <param name="bytes">The binary data to write.</param>
    /// <returns>A result containing canonical PEM text, or failure details for an invalid label.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>label</c> is null.</exception>
    internal static TokenTextResult Write(string label, ReadOnlySpan<byte> bytes)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        int invalidLabelPosition = FindInvalidLabelPosition(label.AsSpan());
        if (invalidLabelPosition >= 0)
        {
            TokenTextResult invalidResult = new TokenTextResult();
            invalidResult.Error = new TokenError(
                "invalid_pem_label",
                "The PEM label is invalid.",
                invalidLabelPosition);
            return invalidResult;
        }

        TokenTextResult base64Result = Base64Codec.Encode(bytes);
        StringBuilder builder = new StringBuilder();
        builder.Append(BeginPrefix);
        builder.Append(label);
        builder.Append(BoundarySuffix);
        builder.Append('\n');

        int position = 0;
        while (position < (base64Result.Text ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            int lineLength = base64Result.Text.Length - position;
            if (lineLength > 64)
            {
                lineLength = 64;
            }

            builder.Append(base64Result.Text.AsSpan(position, lineLength));
            builder.Append('\n');
            position += lineLength;
        }

        builder.Append(EndPrefix);
        builder.Append(label);
        builder.Append(BoundarySuffix);
        builder.Append('\n');

        TokenTextResult result = new TokenTextResult();
        result.Text = builder.ToString();
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Reads one PEM block with optional surrounding ASCII whitespace.
    /// </summary>
    /// <param name="text">The PEM text to read.</param>
    /// <returns>A result containing the decoded label and bytes, or failure details for malformed input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    internal static PemReadResult Read(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        PemReadResult result = new PemReadResult();
        int start = SkipWhitespace(text, 0);
        int headerLineEnd = FindLineEnd(text, start);
        int headerContentEnd = GetLineContentEnd(text, start, headerLineEnd);

        if (!StartsWithAt(text, start, BeginPrefix))
        {
            return InvalidReadResult("A PEM begin boundary is required.", start);
        }

        int labelStart = start + BeginPrefix.Length;
        int labelLength = headerContentEnd - labelStart - BoundarySuffix.Length;
        if (labelLength <= 0 || !EndsWithAt(text, headerContentEnd, BoundarySuffix))
        {
            return InvalidReadResult("The PEM begin boundary is malformed.", start);
        }

        ReadOnlySpan<char> labelSpan = text.AsSpan(labelStart, labelLength);
        int invalidLabelPosition = FindInvalidLabelPosition(labelSpan);
        if (invalidLabelPosition >= 0)
        {
            return InvalidReadResult("The PEM label is invalid.", labelStart + invalidLabelPosition);
        }

        if (headerLineEnd == text.Length)
        {
            return InvalidReadResult("The PEM end boundary is missing.", text.Length);
        }

        string label = new string(labelSpan);
        string expectedEnd = EndPrefix + label + BoundarySuffix;
        int bodyStart = NextLineStart(text, headerLineEnd);
        int lineStart = bodyStart;
        int footerStart = -1;
        int footerLineEnd = -1;

        while (lineStart <= text.Length)
        {
            int lineEnd = FindLineEnd(text, lineStart);
            int lineContentEnd = GetLineContentEnd(text, lineStart, lineEnd);
            int lineLength = lineContentEnd - lineStart;

            if (lineLength >= EndPrefix.Length && StartsWithAt(text, lineStart, EndPrefix))
            {
                if (lineLength != expectedEnd.Length || !EqualsAt(text, lineStart, expectedEnd))
                {
                    return InvalidReadResult("The PEM end boundary does not match the begin boundary.", lineStart);
                }

                footerStart = lineStart;
                footerLineEnd = lineEnd;
                break;
            }

            if (lineEnd == text.Length)
            {
                break;
            }

            lineStart = NextLineStart(text, lineEnd);
        }

        if (footerStart < 0)
        {
            return InvalidReadResult("The PEM end boundary is missing.", text.Length);
        }

        int afterFooter = footerLineEnd == text.Length ? footerLineEnd : NextLineStart(text, footerLineEnd);
        int trailingPosition = SkipWhitespace(text, afterFooter);
        if (trailingPosition != text.Length)
        {
            return InvalidReadResult("Unexpected data follows the PEM block.", trailingPosition);
        }

        int bodyEnd = footerStart;
        TokenBytesResult decoded = Base64Codec.Decode(text.AsSpan(bodyStart, bodyEnd - bodyStart), true);
        if (!decoded.Success)
        {
            result.Error = new TokenError(
                "invalid_pem",
                (decoded.Error ?? throw new InvalidOperationException("A required value is null.")).Message,
                bodyStart + decoded.Error.Position);
            return result;
        }

        PemBlock block = new PemBlock();
        block.Label = label;
        block.Bytes = decoded.Bytes;
        result.Block = block;
        result.Success = true;
        return result;
    }

    static int FindInvalidLabelPosition(ReadOnlySpan<char> label)
    {
        if (label.Length == 0)
        {
            return 0;
        }

        int index = 0;
        bool previousWasSeparator = false;
        while (index < label.Length)
        {
            char value = label[index];
            bool separator = value == ' ' || value == '-';

            if (value < 33 || value > 126)
            {
                if (value != ' ')
                {
                    return index;
                }
            }

            if (separator && (index == 0 || index == label.Length - 1 || previousWasSeparator))
            {
                return index;
            }

            previousWasSeparator = separator;
            index += 1;
        }

        return -1;
    }

    static int SkipWhitespace(string text, int position)
    {
        while (position < text.Length && IsWhitespace(text[position]))
        {
            position += 1;
        }

        return position;
    }

    static bool IsWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    static int FindLineEnd(string text, int position)
    {
        while (position < text.Length && text[position] != '\r' && text[position] != '\n')
        {
            position += 1;
        }

        return position;
    }

    static int GetLineContentEnd(string text, int lineStart, int lineEnd)
    {
        return lineEnd;
    }

    static int NextLineStart(string text, int lineEnd)
    {
        if (lineEnd < text.Length && text[lineEnd] == '\r')
        {
            lineEnd += 1;
            if (lineEnd < text.Length && text[lineEnd] == '\n')
            {
                lineEnd += 1;
            }

            return lineEnd;
        }

        if (lineEnd < text.Length && text[lineEnd] == '\n')
        {
            return lineEnd + 1;
        }

        return lineEnd;
    }

    static bool StartsWithAt(string text, int position, string value)
    {
        if (position < 0 || value.Length > text.Length - position)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (text[position + index] != value[index])
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static bool EndsWithAt(string text, int endPosition, string value)
    {
        return EqualsAt(text, endPosition - value.Length, value);
    }

    static bool EqualsAt(string text, int position, string value)
    {
        if (position < 0 || position + value.Length > text.Length)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (text[position + index] != value[index])
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static PemReadResult InvalidReadResult(string message, int position)
    {
        PemReadResult result = new PemReadResult();
        result.Error = new TokenError("invalid_pem", message, position);
        return result;
    }
}
