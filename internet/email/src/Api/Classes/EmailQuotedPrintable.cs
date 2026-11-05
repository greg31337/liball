using System;

namespace email;
/// <summary>
/// Provides operations for working with quoted-printable.
/// </summary>
public class EmailQuotedPrintable : IEmailQuotedPrintableContract
{
    /// <summary>
    /// The default line length used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultLineLength = 76;
    /// <summary>
    /// The minimum line length value exposed by this API.
    /// </summary>
    internal const int MinimumLineLength = 4;
    /// <summary>
    /// The maximum line length value exposed by this API.
    /// </summary>
    internal const int MaximumLineLength = 76;
    internal static EmailQuotedPrintableDecodeResult DecodeImplementation(ReadOnlySpan<byte> input, EmailQuotedPrintableDecodeMode mode, long maximumDecodedBytes)
    {
        EmailQuotedPrintableDecoder decoder = new EmailQuotedPrintableDecoder(mode, maximumDecodedBytes);
        byte[] body = decoder.Feed(input);
        byte[] tail = decoder.Complete();
        long decodedSize = new EmailSafeArithmetic().AddNonNegative(body.Length, tail.Length);
        byte[] combined = new byte[new EmailSafeArithmetic().ToInt32NonNegative(decodedSize)];
        if (body.Length > 0)
        {
            body.CopyTo(combined, 0);
        }

        if (tail.Length > 0)
        {
            tail.CopyTo(combined, body.Length);
        }

        return new EmailQuotedPrintableDecodeResult(combined, decoder.GetIssues());
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailQuotedPrintableDecodeResult Decode(ReadOnlySpan<byte> input, EmailQuotedPrintableDecodeMode mode, long maximumDecodedBytes)
    {
        return EmailQuotedPrintable.DecodeImplementation(input, mode, maximumDecodedBytes);
    }

    internal static long GetMaximumDecodedSizeImplementation(long encodedByteCount)
    {
        if (encodedByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("encodedByteCount");
        }

        return encodedByteCount;
    }

    /// <summary>
    /// Gets the maximum decoded size.
    /// </summary>
    /// <param name = "encodedByteCount">The number of items or bytes to process.</param>
    /// <returns>The current maximum decoded size.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public long GetMaximumDecodedSize(long encodedByteCount)
    {
        return EmailQuotedPrintable.GetMaximumDecodedSizeImplementation(encodedByteCount);
    }

    internal static byte[] EncodeImplementationWithInput(ReadOnlySpan<byte> input)
    {
        return EncodeImplementationWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(input, DefaultLineLength, EmailQuotedPrintableNewlinePolicy.NormalizeToCrlf, false);
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public byte[] EncodeWithInput(ReadOnlySpan<byte> input)
    {
        return EmailQuotedPrintable.EncodeImplementationWithInput(input);
    }

    internal static byte[] EncodeImplementationWithInputAndNewlinePolicyAndProtectFromLines(ReadOnlySpan<byte> input, EmailQuotedPrintableNewlinePolicy newlinePolicy, bool protectFromLines)
    {
        return EncodeImplementationWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(input, DefaultLineLength, newlinePolicy, protectFromLines);
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "newlinePolicy">The newline policy used by the operation.</param>
    /// <param name = "protectFromLines">Whether to enable the protect from lines behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public byte[] EncodeWithInputAndNewlinePolicyAndProtectFromLines(ReadOnlySpan<byte> input, EmailQuotedPrintableNewlinePolicy newlinePolicy, bool protectFromLines)
    {
        return EmailQuotedPrintable.EncodeImplementationWithInputAndNewlinePolicyAndProtectFromLines(input, newlinePolicy, protectFromLines);
    }

    internal static byte[] EncodeImplementationWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(ReadOnlySpan<byte> input, int lineLength, EmailQuotedPrintableNewlinePolicy newlinePolicy, bool protectFromLines)
    {
        EmailQuotedPrintableEncoder encoder = new EmailQuotedPrintableEncoder(lineLength, newlinePolicy, protectFromLines);
        byte[] body = encoder.Feed(input);
        byte[] tail = encoder.Complete();
        long encodedSize = new EmailSafeArithmetic().AddNonNegative(body.Length, tail.Length);
        byte[] combined = new byte[new EmailSafeArithmetic().ToInt32NonNegative(encodedSize)];
        if (body.Length > 0)
        {
            body.CopyTo(combined, 0);
        }

        if (tail.Length > 0)
        {
            tail.CopyTo(combined, body.Length);
        }

        return combined;
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "lineLength">The line length used by the operation.</param>
    /// <param name = "newlinePolicy">The newline policy used by the operation.</param>
    /// <param name = "protectFromLines">Whether to enable the protect from lines behavior.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public byte[] EncodeWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(ReadOnlySpan<byte> input, int lineLength, EmailQuotedPrintableNewlinePolicy newlinePolicy, bool protectFromLines)
    {
        return EmailQuotedPrintable.EncodeImplementationWithInputAndLineLengthAndNewlinePolicyAndProtectFromLines(input, lineLength, newlinePolicy, protectFromLines);
    }

    internal static void ValidateLineLengthImplementation(int lineLength)
    {
        if (lineLength < MinimumLineLength || lineLength > MaximumLineLength)
        {
            throw new ArgumentOutOfRangeException("lineLength");
        }
    }

    /// <summary>
    /// Validates the supplied line length against the selected policy.
    /// </summary>
    /// <param name = "lineLength">The line length used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void ValidateLineLength(int lineLength)
    {
        EmailQuotedPrintable.ValidateLineLengthImplementation(lineLength);
    }
}
