using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace email;
/// <summary>
/// Provides operations for working with fingerprint.
/// </summary>
public class EmailFingerprint : IEmailFingerprintContract
{
    internal static EmailHashResult ComputeExactImplementationWithMessage(EmailMessage message)
    {
        return ComputeExactImplementationWithMessageAndAlgorithmAndMaximumBytes(message, EmailHashAlgorithm.Sha256, EmailParsingLimitsConstants.DefaultMaxMessageBytes);
    }

    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeExactWithMessage(EmailMessage message)
    {
        return EmailFingerprint.ComputeExactImplementationWithMessage(message);
    }

    internal static EmailHashResult ComputeExactImplementationWithMessageAndAlgorithm(EmailMessage message, EmailHashAlgorithm algorithm)
    {
        return ComputeExactImplementationWithMessageAndAlgorithmAndMaximumBytes(message, algorithm, EmailParsingLimitsConstants.DefaultMaxMessageBytes);
    }

    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeExactWithMessageAndAlgorithm(EmailMessage message, EmailHashAlgorithm algorithm)
    {
        return EmailFingerprint.ComputeExactImplementationWithMessageAndAlgorithm(message, algorithm);
    }

    internal static EmailHashResult ComputeExactImplementationWithMessageAndAlgorithmAndMaximumBytes(EmailMessage message, EmailHashAlgorithm algorithm, long maximumBytes)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        ValidateAlgorithm(algorithm);
        if (maximumBytes < 1)
        {
            throw new ArgumentOutOfRangeException("maximumBytes");
        }

        if (message.HasSourceInfo() && ((message.GetSourceInfo() ?? throw new InvalidOperationException()).GetMessageRange() ?? throw new InvalidOperationException()).GetLength() > maximumBytes)
        {
            throw new EmailLimitException("MaxExactHashBytes", maximumBytes, ((message.GetSourceInfo() ?? throw new InvalidOperationException()).GetMessageRange() ?? throw new InvalidOperationException()).GetLength());
        }

        EmailWriteOptions writeOptions = new EmailWriteOptionsOperations().CreateExactPreservation();
        writeOptions.SetMaximumOutputBytes(maximumBytes);
        EmailHashingStream hashing = new EmailHashingStream(algorithm);
        EmailStreamWriteResult written = new EmailWriter().WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailHashingStreamAndEmailWriteOptions(message, hashing, writeOptions);
        if (!written.IsSuccess())
        {
            hashing.Close();
            throw new InvalidOperationException("The message does not have a complete exact-byte representation under the selected limit.");
        }

        return hashing.Complete();
    }

    /// <summary>
    /// Computes the exact.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public EmailHashResult ComputeExactWithMessageAndAlgorithmAndMaximumBytes(EmailMessage message, EmailHashAlgorithm algorithm, long maximumBytes)
    {
        return EmailFingerprint.ComputeExactImplementationWithMessageAndAlgorithmAndMaximumBytes(message, algorithm, maximumBytes);
    }

    internal static EmailHashResult ComputeSemanticImplementationWithMessage(EmailMessage message)
    {
        return ComputeSemanticImplementationWithMessageAndOptions(message, new EmailCanonicalComparisonOptions());
    }

    /// <summary>
    /// Computes the semantic.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeSemanticWithMessage(EmailMessage message)
    {
        return EmailFingerprint.ComputeSemanticImplementationWithMessage(message);
    }

    internal static EmailHashResult ComputeSemanticImplementationWithMessageAndOptions(EmailMessage message, EmailCanonicalComparisonOptions options)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailCanonicalComparisonOptions snapshot = options.Clone();
        if (message.HasBody())
        {
            new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(message.GetBody(), snapshot.GetMaximumDepth(), snapshot.GetMaximumEntityCount());
        }

        EmailMessage filtered = EmailFingerprintFilter.FilterMessage(message, snapshot);
        EmailHashingStream hashing = new EmailHashingStream(snapshot.GetHashAlgorithm());
        EmailStreamWriteResult written = new EmailWriter().WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailHashingStreamAndEmailWriteOptions(filtered, hashing, CreateWriteOptions(snapshot));
        return CompleteSemantic(hashing, written);
    }

    /// <summary>
    /// Computes the semantic.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHashResult ComputeSemanticWithMessageAndOptions(EmailMessage message, EmailCanonicalComparisonOptions options)
    {
        return EmailFingerprint.ComputeSemanticImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailHashResult ComputeSemanticImplementationWithEntity(MimeEntity entity)
    {
        return ComputeSemanticImplementationWithEntityAndOptions(entity, new EmailCanonicalComparisonOptions());
    }

    /// <summary>
    /// Computes the semantic.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    public EmailHashResult ComputeSemanticWithEntity(MimeEntity entity)
    {
        return EmailFingerprint.ComputeSemanticImplementationWithEntity(entity);
    }

    internal static EmailHashResult ComputeSemanticImplementationWithEntityAndOptions(MimeEntity entity, EmailCanonicalComparisonOptions options)
    {
        if (entity == null)
        {
            throw new ArgumentNullException("entity");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailCanonicalComparisonOptions snapshot = options.Clone();
        new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(entity, snapshot.GetMaximumDepth(), snapshot.GetMaximumEntityCount());
        MimeEntity filtered = EmailFingerprintFilter.FilterEntity(entity, snapshot);
        EmailHashingStream hashing = new EmailHashingStream(snapshot.GetHashAlgorithm());
        EmailStreamWriteResult written = new EmailWriter().WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailHashingStreamAndEmailWriteOptions(filtered, hashing, CreateWriteOptions(snapshot));
        return CompleteSemantic(hashing, written);
    }

    /// <summary>
    /// Computes the semantic.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailHashResult ComputeSemanticWithEntityAndOptions(MimeEntity entity, EmailCanonicalComparisonOptions options)
    {
        return EmailFingerprint.ComputeSemanticImplementationWithEntityAndOptions(entity, options);
    }

    static EmailHashResult CompleteSemantic(EmailHashingStream hashing, EmailStreamWriteResult written)
    {
        if (!written.IsSuccess())
        {
            hashing.Close();
            throw new InvalidOperationException("Canonical semantic serialization failed under the selected comparison limits.");
        }

        return hashing.Complete();
    }

    static EmailWriteOptions CreateWriteOptions(EmailCanonicalComparisonOptions options)
    {
        EmailWriteOptions writeOptions = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        writeOptions.SetDeterministicOutput(true);
        writeOptions.SetDeterministicSeed(options.GetDeterministicSeed());
        writeOptions.SetIncludeBlindRecipientHeaders(true);
        writeOptions.SetMaximumOutputBytes(options.GetMaximumOutputBytes());
        writeOptions.SetMaximumNestingDepth(options.GetMaximumDepth() == 0 ? 1 : options.GetMaximumDepth());
        writeOptions.SetMaximumPartCount(options.GetMaximumEntityCount());
        return writeOptions;
    }

    static void ValidateAlgorithm(EmailHashAlgorithm algorithm)
    {
        if (algorithm < EmailHashAlgorithm.Sha256 || algorithm > EmailHashAlgorithm.Sha512)
        {
            throw new ArgumentOutOfRangeException("algorithm");
        }
    }
}
