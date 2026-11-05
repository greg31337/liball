using System;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDecodedContent"/>.
/// </summary>
public interface IEmailDecodedContentContract
{
    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The computed count or size.</returns>
    long CopyToWithContentAndOutput(EmailContent content, Stream output);
    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    long CopyToWithContentAndOutputAndMaximumBytes(EmailContent content, Stream output, long maximumBytes);
    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "limitName">The limit name used by the operation.</param>
    /// <param name = "reportedLimit">The reported limit used by the operation.</param>
    /// <param name = "reportedObserved">The reported observed used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    long CopyToWithContentAndOutputAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(EmailContent? content, Stream output, long maximumBytes, string limitName, long reportedLimit, long reportedObserved);
    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The computed count or size.</returns>
    long CopyToWithEntityAndOutput(MimeEntity entity, Stream output);
    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    long CopyToWithEntityAndOutputAndMaximumBytes(MimeEntity entity, Stream output, long maximumBytes);
    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "limitName">The limit name used by the operation.</param>
    /// <param name = "reportedLimit">The reported limit used by the operation.</param>
    /// <param name = "reportedObserved">The reported observed used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    long CopyToWithEntityAndOutputAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(MimeEntity entity, Stream output, long maximumBytes, string limitName, long reportedLimit, long reportedObserved);
    /// <summary>
    /// Saves the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The computed count or size.</returns>
    long SaveWithContentAndPath(EmailContent content, string path);
    /// <summary>
    /// Saves the supplied value.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    long SaveWithContentAndPathAndMaximumBytes(EmailContent content, string path, long maximumBytes);
    /// <summary>
    /// Saves the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <returns>The computed count or size.</returns>
    long SaveWithEntityAndPath(MimeEntity entity, string path);
    /// <summary>
    /// Saves the supplied value.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "path">The file-system or message path to use.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    long SaveWithEntityAndPathAndMaximumBytes(MimeEntity entity, string path, long maximumBytes);
    /// <summary>
    /// Reads the prefix from the input.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "maximumPrefixBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] ReadPrefixWithContentAndMaximumPrefixBytes(EmailContent content, int maximumPrefixBytes);
    /// <summary>
    /// Reads the prefix from the input.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "maximumPrefixBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] ReadPrefixWithEntityAndMaximumPrefixBytes(MimeEntity entity, int maximumPrefixBytes);
    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    long MeasureLengthWithContent(EmailContent content);
    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    long MeasureLengthWithContentAndMaximumBytes(EmailContent content, long maximumBytes);
    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "limitName">The limit name used by the operation.</param>
    /// <param name = "reportedLimit">The reported limit used by the operation.</param>
    /// <param name = "reportedObserved">The reported observed used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    long MeasureLengthWithContentAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(EmailContent content, long maximumBytes, string limitName, long reportedLimit, long reportedObserved);
    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The computed count or size.</returns>
    long MeasureLengthWithEntity(MimeEntity entity);
    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The computed count or size.</returns>
    long MeasureLengthWithEntityAndMaximumBytes(MimeEntity entity, long maximumBytes);
    /// <summary>
    /// Measures the length.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "limitName">The limit name used by the operation.</param>
    /// <param name = "reportedLimit">The reported limit used by the operation.</param>
    /// <param name = "reportedObserved">The reported observed used by the operation.</param>
    /// <returns>The computed count or size.</returns>
    long MeasureLengthWithEntityAndMaximumBytesAndLimitNameAndReportedLimitAndReportedObserved(MimeEntity entity, long maximumBytes, string limitName, long reportedLimit, long reportedObserved);
    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeHashWithContentAndAlgorithm(EmailContent content, EmailHashAlgorithm algorithm);
    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "content">The content used by the operation.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeHashWithContentAndAlgorithmAndMaximumBytes(EmailContent content, EmailHashAlgorithm algorithm, long maximumBytes);
    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeHashWithEntityAndAlgorithm(MimeEntity entity, EmailHashAlgorithm algorithm);
    /// <summary>
    /// Computes the hash.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "algorithm">The algorithm used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>The hash result produced by the operation.</returns>
    EmailHashResult ComputeHashWithEntityAndAlgorithmAndMaximumBytes(MimeEntity entity, EmailHashAlgorithm algorithm, long maximumBytes);
}
