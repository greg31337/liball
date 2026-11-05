using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides operations for working with percent encoding.
/// </summary>
public class EmailPercentEncoding : IEmailPercentEncodingContract
{
    internal static byte[] EncodeImplementation(ReadOnlySpan<byte> input)
    {
        List<byte> output = new List<byte>();
        int index = 0;
        while (index < input.Length)
        {
            byte value = input[index];
            if (IsAttributeCharacter(value))
            {
                output.Add(value);
            }
            else
            {
                output.Add(37);
                output.Add(new EmailByteScanner().ToUpperHex(value >> 4));
                output.Add(new EmailByteScanner().ToUpperHex(value & 15));
            }

            index++;
        }

        return output.ToArray();
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public byte[] Encode(ReadOnlySpan<byte> input)
    {
        return EmailPercentEncoding.EncodeImplementation(input);
    }

    internal static EmailPercentDecodeResult DecodeImplementationWithInputAndModeAndMaximumDecodedBytes(ReadOnlySpan<byte> input, EmailRfc2231DecodeMode mode, long maximumDecodedBytes)
    {
        return DecodeImplementationWithInputAndModeAndMaximumDecodedBytesAndInitialPosition(input, mode, maximumDecodedBytes, new EmailSourcePosition(0, 1, 1));
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailPercentDecodeResult DecodeWithInputAndModeAndMaximumDecodedBytes(ReadOnlySpan<byte> input, EmailRfc2231DecodeMode mode, long maximumDecodedBytes)
    {
        return EmailPercentEncoding.DecodeImplementationWithInputAndModeAndMaximumDecodedBytes(input, mode, maximumDecodedBytes);
    }

    internal static EmailPercentDecodeResult DecodeImplementationWithInputAndModeAndMaximumDecodedBytesAndInitialPosition(ReadOnlySpan<byte> input, EmailRfc2231DecodeMode mode, long maximumDecodedBytes, EmailSourcePosition? initialPosition)
    {
        if (mode < EmailRfc2231DecodeMode.Strict || mode > EmailRfc2231DecodeMode.Tolerant)
        {
            throw new ArgumentOutOfRangeException("mode");
        }

        if (maximumDecodedBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDecodedBytes");
        }

        if (initialPosition == null)
        {
            throw new ArgumentNullException("initialPosition");
        }

        List<byte> output = new List<byte>();
        EmailIssueList issues = new EmailIssueList();
        EmailSourceTracker tracker = new EmailSourceTrackerOperations().CreateWithInitialPosition(initialPosition);
        bool limitReached = false;
        int index = 0;
        while (index < input.Length)
        {
            EmailSourcePosition position = tracker.GetPosition();
            byte decoded = input[index];
            int consumed = 1;
            if (input[index] == 37)
            {
                if (index + 2 < input.Length && new EmailByteScanner().IsHexDigit(input[index + 1]) && new EmailByteScanner().IsHexDigit(input[index + 2]))
                {
                    decoded = (byte)new EmailByteScanner().DecodeHexPair(input[index + 1], input[index + 2]);
                    consumed = 3;
                    if (IsLowerHex(input[index + 1]) || IsLowerHex(input[index + 2]))
                    {
                        AddIssue(issues, mode, EmailIssueCode.PercentEncodingLowercaseHex, "A percent escape uses lowercase hexadecimal digits.", position, "The escape was decoded using its hexadecimal value.");
                    }
                }
                else
                {
                    AddIssue(issues, mode, EmailIssueCode.PercentEncodingInvalidEscape, "A percent sign is not followed by two hexadecimal digits.", position, "The percent sign was retained as a literal byte.");
                }
            }

            if (output.Count < maximumDecodedBytes)
            {
                output.Add(decoded);
            }
            else if (!limitReached)
            {
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.Rfc2231DecodedByteLimitExceeded, "Percent-decoded content exceeds the configured byte limit.", position, null, null, "Only the prefix within the configured limit was retained."));
                limitReached = true;
            }

            tracker.Advance(input.Slice(index, consumed));
            index += consumed;
        }

        return new EmailPercentDecodeResult(output.ToArray(), issues);
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <param name = "initialPosition">The initial position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailPercentDecodeResult DecodeWithInputAndModeAndMaximumDecodedBytesAndInitialPosition(ReadOnlySpan<byte> input, EmailRfc2231DecodeMode mode, long maximumDecodedBytes, EmailSourcePosition? initialPosition)
    {
        return EmailPercentEncoding.DecodeImplementationWithInputAndModeAndMaximumDecodedBytesAndInitialPosition(input, mode, maximumDecodedBytes, initialPosition);
    }

    static void AddIssue(EmailIssueList issues, EmailRfc2231DecodeMode mode, EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = EmailIssueSeverity.Error;
        if (mode == EmailRfc2231DecodeMode.Tolerant)
        {
            severity = EmailIssueSeverity.Warning;
        }

        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    static bool IsLowerHex(byte value)
    {
        return value >= 97 && value <= 102;
    }

    static bool IsAttributeCharacter(byte value)
    {
        if (value < 33 || value > 126 || value == 42 || value == 39 || value == 37)
        {
            return false;
        }

        return value != 40 && value != 41 && value != 60 && value != 62 && value != 64 && value != 44 && value != 59 && value != 58 && value != 92 && value != 34 && value != 47 && value != 91 && value != 93 && value != 63 && value != 61;
    }
}
