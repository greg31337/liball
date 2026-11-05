using System;
using System.IO;

namespace email;
/// <summary>
/// Provides operations for working with transfer encoding.
/// </summary>
public class EmailTransferEncoding : IEmailTransferEncodingContract
{
    /// <summary>
    /// The default maximum line bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumLineBytes = 998;
    /// <summary>
    /// The default buffer bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultBufferBytes = 81920;
    internal static EmailTransferValidationResult ValidateImplementationWithInputAndKind(ReadOnlySpan<byte> input, EmailTransferEncodingKind kind)
    {
        return ValidateImplementationWithInputAndKindAndMaximumBytesAndMaximumLineBytes(input, kind, input.Length, DefaultMaximumLineBytes);
    }

    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTransferValidationResult ValidateWithInputAndKind(ReadOnlySpan<byte> input, EmailTransferEncodingKind kind)
    {
        return EmailTransferEncoding.ValidateImplementationWithInputAndKind(input, kind);
    }

    internal static EmailTransferValidationResult ValidateImplementationWithInputAndKindAndMaximumBytesAndMaximumLineBytes(ReadOnlySpan<byte> input, EmailTransferEncodingKind kind, long maximumBytes, int maximumLineBytes)
    {
        EmailTransferValidator validator = new EmailTransferValidator(kind, maximumBytes, maximumLineBytes);
        validator.Feed(input);
        validator.Complete();
        return validator.GetResult();
    }

    /// <summary>
    /// Validates the supplied value against the selected policy.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "maximumLineBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailTransferValidationResult ValidateWithInputAndKindAndMaximumBytesAndMaximumLineBytes(ReadOnlySpan<byte> input, EmailTransferEncodingKind kind, long maximumBytes, int maximumLineBytes)
    {
        return EmailTransferEncoding.ValidateImplementationWithInputAndKindAndMaximumBytesAndMaximumLineBytes(input, kind, maximumBytes, maximumLineBytes);
    }

    internal static EmailTransferValidationResult CopyImplementationWithInputAndOutputAndKindAndMaximumBytes(Stream input, Stream output, EmailTransferEncodingKind kind, long maximumBytes)
    {
        return CopyImplementationWithInputAndOutputAndKindAndMaximumBytesAndMaximumLineBytesAndBufferBytesAndCancellationAndProgress(input, output, kind, maximumBytes, DefaultMaximumLineBytes, DefaultBufferBytes, null, null);
    }

    /// <summary>
    /// Copies the value to a separate destination.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The transfer validation result produced by the operation.</returns>
    public EmailTransferValidationResult CopyWithInputAndOutputAndKindAndMaximumBytes(Stream input, Stream output, EmailTransferEncodingKind kind, long maximumBytes)
    {
        return EmailTransferEncoding.CopyImplementationWithInputAndOutputAndKindAndMaximumBytes(input, output, kind, maximumBytes);
    }

    internal static EmailTransferValidationResult CopyImplementationWithInputAndOutputAndKindAndMaximumBytesAndMaximumLineBytesAndBufferBytesAndCancellationAndProgress(Stream input, Stream output, EmailTransferEncodingKind kind, long maximumBytes, int maximumLineBytes, int bufferBytes, IEmailCancellation? cancellation, IEmailProgress? progress)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        if (output == null)
        {
            throw new ArgumentNullException("output");
        }

        if (!input.CanRead)
        {
            throw new ArgumentException("The input stream must be readable.", "input");
        }

        if (!output.CanWrite)
        {
            throw new ArgumentException("The output stream must be writable.", "output");
        }

        if (bufferBytes <= 0)
        {
            throw new ArgumentOutOfRangeException("bufferBytes");
        }

        EmailTransferValidator validator = new EmailTransferValidator(kind, maximumBytes, maximumLineBytes);
        byte[] buffer = new byte[bufferBytes];
        bool finished = false;
        while (!finished)
        {
            if (cancellation != null && cancellation.IsCancellationRequested())
            {
                validator.Cancel();
                break;
            }

            int read = input.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            if (progress != null)
            {
                progress.AddSourceBytesRead(read);
            }

            byte[] copied = validator.Feed(new ReadOnlySpan<byte>(buffer, 0, read));
            if (copied.Length > 0)
            {
                output.Write(copied, 0, copied.Length);
                if (progress != null)
                {
                    progress.AddOutputBytesWritten(copied.Length);
                    progress.AddDecodedBytesProduced(copied.Length);
                }
            }

            if (validator.HasFatalError())
            {
                finished = true;
            }
        }

        validator.Complete();
        if (progress != null)
        {
            progress.AddOperationCount(1);
        }

        return validator.GetResult();
    }

    /// <summary>
    /// Copies the value to a separate destination.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "maximumLineBytes">The source bytes or data to process.</param>
    /// <param name = "bufferBytes">The source bytes or data to process.</param>
    /// <param name = "cancellation">The cancellation used by the operation.</param>
    /// <param name = "progress">The progress used by the operation.</param>
    /// <returns>The transfer validation result produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTransferValidationResult CopyWithInputAndOutputAndKindAndMaximumBytesAndMaximumLineBytesAndBufferBytesAndCancellationAndProgress(Stream input, Stream output, EmailTransferEncodingKind kind, long maximumBytes, int maximumLineBytes, int bufferBytes, IEmailCancellation? cancellation, IEmailProgress? progress)
    {
        return EmailTransferEncoding.CopyImplementationWithInputAndOutputAndKindAndMaximumBytesAndMaximumLineBytesAndBufferBytesAndCancellationAndProgress(input, output, kind, maximumBytes, maximumLineBytes, bufferBytes, cancellation, progress);
    }

    internal static void ValidateKindImplementation(EmailTransferEncodingKind kind)
    {
        if (kind < EmailTransferEncodingKind.SevenBit || kind > EmailTransferEncodingKind.Base64)
        {
            throw new ArgumentOutOfRangeException("kind");
        }
    }

    /// <summary>
    /// Validates the supplied kind against the selected policy.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void ValidateKind(EmailTransferEncodingKind kind)
    {
        EmailTransferEncoding.ValidateKindImplementation(kind);
    }

    internal static void ValidateIdentityKindImplementation(EmailTransferEncodingKind kind)
    {
        ValidateKindImplementation(kind);
        if (kind > EmailTransferEncodingKind.Binary)
        {
            throw new ArgumentOutOfRangeException("kind");
        }
    }

    /// <summary>
    /// Validates the supplied identity kind against the selected policy.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void ValidateIdentityKind(EmailTransferEncodingKind kind)
    {
        EmailTransferEncoding.ValidateIdentityKindImplementation(kind);
    }

    internal static bool IsIdentityKindImplementation(EmailTransferEncodingKind kind)
    {
        ValidateKindImplementation(kind);
        return kind <= EmailTransferEncodingKind.Binary;
    }

    /// <summary>
    /// Determines whether the current value or state is identity kind.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns><c>true</c> when the value or state is identity kind; otherwise, <c>false</c>.</returns>
    public bool IsIdentityKind(EmailTransferEncodingKind kind)
    {
        return EmailTransferEncoding.IsIdentityKindImplementation(kind);
    }
}
