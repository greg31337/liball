using System;

namespace email;
/// <summary>
/// Provides operations for working with encoding selector.
/// </summary>
public class EmailEncodingSelector : IEmailEncodingSelectorContract
{
    internal static EmailEncodingSelectionResult SelectImplementationWithInputAndMediaTypeAndConstraint(ReadOnlySpan<byte> input, string mediaType, EmailTransferEncodingConstraint constraint)
    {
        EmailEncodingSelectionOptions options = new EmailEncodingSelectionOptions();
        options.SetMediaType(mediaType);
        options.SetConstraint(constraint);
        return ChooseImplementationWithInputAndOptions(input, options);
    }

    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <param name = "constraint">The constraint used by the operation.</param>
    /// <returns>The encoding selection result produced by the operation.</returns>
    public EmailEncodingSelectionResult SelectWithInputAndMediaTypeAndConstraint(ReadOnlySpan<byte> input, string mediaType, EmailTransferEncodingConstraint constraint)
    {
        return EmailEncodingSelector.SelectImplementationWithInputAndMediaTypeAndConstraint(input, mediaType, constraint);
    }

    internal static EmailEncodingSelectionResult SelectImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailEncodingSelectionOptions options)
    {
        return ChooseImplementationWithInputAndOptions(input, options);
    }

    /// <summary>
    /// Selects the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The encoding selection result produced by the operation.</returns>
    public EmailEncodingSelectionResult SelectWithInputAndOptions(ReadOnlySpan<byte> input, EmailEncodingSelectionOptions options)
    {
        return EmailEncodingSelector.SelectImplementationWithInputAndOptions(input, options);
    }

    internal static EmailEncodingSelectionResult ChooseImplementationWithInputAndMediaTypeAndConstraint(ReadOnlySpan<byte> input, string mediaType, EmailTransferEncodingConstraint constraint)
    {
        EmailEncodingSelectionOptions options = new EmailEncodingSelectionOptions();
        options.SetMediaType(mediaType);
        options.SetConstraint(constraint);
        return ChooseImplementationWithInputAndOptions(input, options);
    }

    /// <summary>
    /// Chooses the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <param name = "constraint">The constraint used by the operation.</param>
    /// <returns>The encoding selection result produced by the operation.</returns>
    public EmailEncodingSelectionResult ChooseWithInputAndMediaTypeAndConstraint(ReadOnlySpan<byte> input, string mediaType, EmailTransferEncodingConstraint constraint)
    {
        return EmailEncodingSelector.ChooseImplementationWithInputAndMediaTypeAndConstraint(input, mediaType, constraint);
    }

    internal static EmailEncodingSelectionResult ChooseImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailEncodingSelectionOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string mediaType = options.GetMediaType();
        EmailTransferEncodingConstraint constraint = options.GetConstraint();
        int maximumLineBytes = options.GetMaximumLineBytes();
        int minimumTextPercentage = options.GetMinimumTextPercentage();
        EmailEncodingAnalysis analysis = Analyze(input, mediaType, minimumTextPercentage);
        bool validSevenBit = analysis.GetHighBitByteCount() == 0 && IsEightBitIdentitySafe(analysis, maximumLineBytes);
        if (validSevenBit)
        {
            return CreateResult(EmailTransferEncodingKind.SevenBit, EmailEncodingSelectionReason.SevenBitSafe, analysis);
        }

        bool validEightBit = IsEightBitIdentitySafe(analysis, maximumLineBytes);
        bool textualMediaType = analysis.IsTextualMediaType();
        if (constraint >= EmailTransferEncodingConstraint.EightBit && validEightBit && textualMediaType)
        {
            return CreateResult(EmailTransferEncodingKind.EightBit, EmailEncodingSelectionReason.EightBitSafe, analysis);
        }

        if (constraint == EmailTransferEncodingConstraint.Binary)
        {
            return CreateResult(EmailTransferEncodingKind.Binary, EmailEncodingSelectionReason.BinaryTransportAvailable, analysis);
        }

        if (textualMediaType && analysis.IsMostlyText())
        {
            return CreateResult(EmailTransferEncodingKind.QuotedPrintable, EmailEncodingSelectionReason.MostlyText, analysis);
        }

        return CreateResult(EmailTransferEncodingKind.Base64, EmailEncodingSelectionReason.BinaryContent, analysis);
    }

    /// <summary>
    /// Chooses the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The encoding selection result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailEncodingSelectionResult ChooseWithInputAndOptions(ReadOnlySpan<byte> input, EmailEncodingSelectionOptions options)
    {
        return EmailEncodingSelector.ChooseImplementationWithInputAndOptions(input, options);
    }

    static bool IsEightBitIdentitySafe(EmailEncodingAnalysis analysis, int maximumLineBytes)
    {
        return analysis.GetNullByteCount() == 0 && analysis.HasCanonicalLineBreaks() && analysis.GetMaximumLineByteCount() <= maximumLineBytes;
    }

    static EmailEncodingAnalysis Analyze(ReadOnlySpan<byte> input, string mediaType, int minimumTextPercentage)
    {
        long textBytes = 0;
        long highBitBytes = 0;
        long nullBytes = 0;
        long controlBytes = 0;
        long maximumLineBytes = 0;
        long currentLineBytes = 0;
        bool canonicalLineBreaks = true;
        bool pendingCarriageReturn = false;
        int index = 0;
        while (index < input.Length)
        {
            byte value = input[index];
            if (value >= 128)
            {
                highBitBytes++;
            }

            if (value == 0)
            {
                nullBytes++;
            }

            if (IsTextByte(value))
            {
                textBytes++;
            }
            else if (value < 32 || value == 127)
            {
                controlBytes++;
            }

            if (pendingCarriageReturn)
            {
                pendingCarriageReturn = false;
                if (value == 10)
                {
                    maximumLineBytes = Maximum(maximumLineBytes, currentLineBytes);
                    currentLineBytes = 0;
                    index++;
                    continue;
                }

                canonicalLineBreaks = false;
                maximumLineBytes = Maximum(maximumLineBytes, currentLineBytes);
                currentLineBytes = 0;
            }

            if (value == 13)
            {
                pendingCarriageReturn = true;
            }
            else if (value == 10)
            {
                canonicalLineBreaks = false;
                maximumLineBytes = Maximum(maximumLineBytes, currentLineBytes);
                currentLineBytes = 0;
            }
            else
            {
                currentLineBytes++;
            }

            index++;
        }

        if (pendingCarriageReturn)
        {
            canonicalLineBreaks = false;
        }

        maximumLineBytes = Maximum(maximumLineBytes, currentLineBytes);
        bool mostlyText = input.Length == 0 || (long)textBytes * 100 >= (long)input.Length * minimumTextPercentage;
        bool textualMediaType = IsTextualMediaType(mediaType);
        return new EmailEncodingAnalysis(input.Length, textBytes, highBitBytes, nullBytes, controlBytes, maximumLineBytes, canonicalLineBreaks, textualMediaType, mostlyText);
    }

    static bool IsTextByte(byte value)
    {
        return value == 9 || value == 10 || value == 13 || value >= 32 && value <= 126;
    }

    static bool IsTextualMediaType(string mediaType)
    {
        string normalized = mediaType.Trim();
        int semicolon = normalized.IndexOf(';');
        if (semicolon >= 0)
        {
            normalized = normalized.Substring(0, semicolon).Trim();
        }

        if (normalized.StartsWith("text/", StringComparison.OrdinalIgnoreCase) || normalized.StartsWith("message/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        int slash = normalized.IndexOf('/');
        if (slash < 0 || slash == normalized.Length - 1)
        {
            return false;
        }

        string subtype = normalized.Substring(slash + 1);
        if (subtype.Equals("json", StringComparison.OrdinalIgnoreCase) || subtype.EndsWith("+json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (subtype.Equals("xml", StringComparison.OrdinalIgnoreCase) || subtype.EndsWith("+xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (subtype.Equals("javascript", StringComparison.OrdinalIgnoreCase) || subtype.Equals("ecmascript", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (subtype.Equals("sql", StringComparison.OrdinalIgnoreCase) || subtype.Equals("graphql", StringComparison.OrdinalIgnoreCase) || subtype.Equals("x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    static long Maximum(long left, long right)
    {
        if (left >= right)
        {
            return left;
        }

        return right;
    }

    static EmailEncodingSelectionResult CreateResult(EmailTransferEncodingKind encoding, EmailEncodingSelectionReason reason, EmailEncodingAnalysis analysis)
    {
        return new EmailEncodingSelectionResult(encoding, reason, analysis.GetSourceByteCount(), analysis.GetTextByteCount(), analysis.GetHighBitByteCount(), analysis.GetNullByteCount(), analysis.GetControlByteCount(), analysis.GetMaximumLineByteCount(), analysis.HasCanonicalLineBreaks(), analysis.IsTextualMediaType(), analysis.IsMostlyText());
    }
}
