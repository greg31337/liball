using System;

namespace email;
/// <summary>
/// Provides operations for working with base64.
/// </summary>
public class EmailBase64 : IEmailBase64Contract
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
    internal static EmailBase64DecodeResult DecodeImplementation(ReadOnlySpan<byte> input, EmailBase64DecodeMode mode, long maximumDecodedBytes)
    {
        EmailBase64Decoder decoder = new EmailBase64Decoder(mode, maximumDecodedBytes);
        byte[] body = decoder.Feed(input);
        byte[] tail = decoder.Complete();
        byte[] combined = new byte[body.Length + tail.Length];
        if (body.Length > 0)
        {
            body.CopyTo(combined, 0);
        }

        if (tail.Length > 0)
        {
            tail.CopyTo(combined, body.Length);
        }

        return new EmailBase64DecodeResult(combined, decoder.GetIssues());
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailBase64DecodeResult Decode(ReadOnlySpan<byte> input, EmailBase64DecodeMode mode, long maximumDecodedBytes)
    {
        return EmailBase64.DecodeImplementation(input, mode, maximumDecodedBytes);
    }

    internal static long GetMaximumDecodedSizeImplementation(long encodedByteCount)
    {
        if (encodedByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("encodedByteCount");
        }

        long quantumCount = encodedByteCount / 4;
        if (encodedByteCount % 4 != 0)
        {
            quantumCount++;
        }

        return new EmailSafeArithmetic().MultiplyNonNegative(quantumCount, 3);
    }

    /// <summary>
    /// Gets the maximum decoded size.
    /// </summary>
    /// <param name = "encodedByteCount">The number of items or bytes to process.</param>
    /// <returns>The current maximum decoded size.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public long GetMaximumDecodedSize(long encodedByteCount)
    {
        return EmailBase64.GetMaximumDecodedSizeImplementation(encodedByteCount);
    }

    internal static long GetDecodedSizeFromQuantumCountImplementation(long quantumCount, int paddingCount)
    {
        if (quantumCount < 0)
        {
            throw new ArgumentOutOfRangeException("quantumCount");
        }

        if (paddingCount < 0 || paddingCount > 2)
        {
            throw new ArgumentOutOfRangeException("paddingCount");
        }

        if (quantumCount == 0 && paddingCount != 0)
        {
            throw new ArgumentException("Padding requires at least one quantum.", "paddingCount");
        }

        long fullSize = new EmailSafeArithmetic().MultiplyNonNegative(quantumCount, 3);
        return fullSize - paddingCount;
    }

    /// <summary>
    /// Gets the decoded size from quantum count.
    /// </summary>
    /// <param name = "quantumCount">The number of items or bytes to process.</param>
    /// <param name = "paddingCount">The number of items or bytes to process.</param>
    /// <returns>The current decoded size from quantum count.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public long GetDecodedSizeFromQuantumCount(long quantumCount, int paddingCount)
    {
        return EmailBase64.GetDecodedSizeFromQuantumCountImplementation(quantumCount, paddingCount);
    }

    internal static byte[] EncodeImplementationWithInput(ReadOnlySpan<byte> input)
    {
        return EncodeImplementationWithInputAndLineLength(input, DefaultLineLength);
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public byte[] EncodeWithInput(ReadOnlySpan<byte> input)
    {
        return EmailBase64.EncodeImplementationWithInput(input);
    }

    internal static byte[] EncodeImplementationWithInputAndLineLength(ReadOnlySpan<byte> input, int lineLength)
    {
        int encodedSize = new EmailSafeArithmetic().ToInt32NonNegative(GetEncodedSizeImplementation(input.Length, lineLength));
        EmailBase64Encoder encoder = new EmailBase64Encoder(lineLength);
        byte[] body = encoder.Feed(input);
        byte[] tail = encoder.Complete();
        byte[] combined = new byte[encodedSize];
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
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public byte[] EncodeWithInputAndLineLength(ReadOnlySpan<byte> input, int lineLength)
    {
        return EmailBase64.EncodeImplementationWithInputAndLineLength(input, lineLength);
    }

    internal static long GetEncodedSizeImplementation(long inputByteCount, int lineLength)
    {
        if (inputByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("inputByteCount");
        }

        ValidateLineLengthImplementation(lineLength);
        long quantumCount = inputByteCount / 3;
        if (inputByteCount % 3 != 0)
        {
            quantumCount++;
        }

        long characterCount = new EmailSafeArithmetic().MultiplyNonNegative(quantumCount, 4);
        if (characterCount == 0)
        {
            return 0;
        }

        long lineBreakCount = (characterCount - 1) / lineLength;
        long lineBreakBytes = new EmailSafeArithmetic().MultiplyNonNegative(lineBreakCount, 2);
        return new EmailSafeArithmetic().AddNonNegative(characterCount, lineBreakBytes);
    }

    /// <summary>
    /// Gets the encoded size.
    /// </summary>
    /// <param name = "inputByteCount">The number of items or bytes to process.</param>
    /// <param name = "lineLength">The line length used by the operation.</param>
    /// <returns>The current encoded size.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public long GetEncodedSize(long inputByteCount, int lineLength)
    {
        return EmailBase64.GetEncodedSizeImplementation(inputByteCount, lineLength);
    }

    internal static void ValidateLineLengthImplementation(int lineLength)
    {
        if (lineLength < MinimumLineLength || lineLength > MaximumLineLength || lineLength % 4 != 0)
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
        EmailBase64.ValidateLineLengthImplementation(lineLength);
    }
}
