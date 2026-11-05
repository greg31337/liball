using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailWriter"/>.
/// </summary>
public interface IEmailWriterContract
{
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteWithMessageAndOutput(EmailMessage message, Stream output);
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteWithMessageAndOutputAndOptions(EmailMessage message, Stream output, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The write result produced by the operation.</returns>
    EmailWriteResult WriteWithMessage(EmailMessage message);
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The write result produced by the operation.</returns>
    EmailWriteResult WriteWithMessageAndOptions(EmailMessage message, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The write result produced by the operation.</returns>
    EmailWriteResult WriteMessageWithMessage(EmailMessage message);
    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailWriteResult WriteMessageWithMessageAndOptions(EmailMessage message, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteMessageWithMessageAndOutputUsingEmailMessageAndStream(EmailMessage message, Stream output);
    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndStreamAndEmailWriteOptions(EmailMessage message, Stream output, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailStreamWriteResult WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailCountingStreamAndEmailWriteOptions(EmailMessage message, EmailCountingStream output, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteMessageWithMessageAndOutputUsingEmailMessageAndEmailCountingStream(EmailMessage message, EmailCountingStream output);
    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailStreamWriteResult WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndEmailHashingStreamAndEmailWriteOptions(EmailMessage message, EmailHashingStream output, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteMessageWithMessageAndOutputUsingEmailMessageAndEmailHashingStream(EmailMessage message, EmailHashingStream output);
    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailStreamWriteResult WriteMessageWithMessageAndOutputAndOptionsUsingEmailMessageAndSmtpDataWriterAndEmailWriteOptions(EmailMessage message, SmtpDataWriter output, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied message to the requested destination.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteMessageWithMessageAndOutputUsingEmailMessageAndSmtpDataWriter(EmailMessage message, SmtpDataWriter output);
    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The write result produced by the operation.</returns>
    EmailWriteResult WriteEntityWithEntity(MimeEntity entity);
    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailWriteResult WriteEntityWithEntityAndOptions(MimeEntity entity, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteEntityWithEntityAndOutputUsingMimeEntityAndStream(MimeEntity entity, Stream output);
    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndStreamAndEmailWriteOptions(MimeEntity entity, Stream output, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailStreamWriteResult WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailCountingStreamAndEmailWriteOptions(MimeEntity entity, EmailCountingStream output, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteEntityWithEntityAndOutputUsingMimeEntityAndEmailCountingStream(MimeEntity entity, EmailCountingStream output);
    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailStreamWriteResult WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndEmailHashingStreamAndEmailWriteOptions(MimeEntity entity, EmailHashingStream output, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteEntityWithEntityAndOutputUsingMimeEntityAndEmailHashingStream(MimeEntity entity, EmailHashingStream output);
    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailStreamWriteResult WriteEntityWithEntityAndOutputAndOptionsUsingMimeEntityAndSmtpDataWriterAndEmailWriteOptions(MimeEntity entity, SmtpDataWriter output, EmailWriteOptions options);
    /// <summary>
    /// Writes the supplied entity to the requested destination.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "output">The destination that receives the generated data.</param>
    /// <returns>The stream write result produced by the operation.</returns>
    EmailStreamWriteResult WriteEntityWithEntityAndOutputUsingMimeEntityAndSmtpDataWriter(MimeEntity entity, SmtpDataWriter output);
    /// <summary>
    /// Copies the current output or content into a new byte array.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] ToArrayWithMessage(EmailMessage message);
    /// <summary>
    /// Copies the current output or content into a new byte array.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] ToArrayWithMessageAndOptions(EmailMessage message, EmailWriteOptions options);
    /// <summary>
    /// Copies the current output or content into a new byte array.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] ToArrayWithEntity(MimeEntity entity);
    /// <summary>
    /// Copies the current output or content into a new byte array.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] ToArrayWithEntityAndOptions(MimeEntity entity, EmailWriteOptions options);
    /// <summary>
    /// Serializes a message known to contain only textual MIME leaves and strictly converts the canonical bytes to text.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The text write result produced by the operation.</returns>
    EmailTextWriteResult WriteTextWithMessage(EmailMessage message);
    /// <summary>
    /// Serializes a message known to contain only textual MIME leaves and strictly converts the byte-writer output to text.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The text write result produced by the operation.</returns>
    EmailTextWriteResult WriteTextWithMessageAndOptions(EmailMessage message, EmailTextWriteOptions options);
    /// <summary>
    /// Serializes a MIME entity known to contain only textual leaves and strictly converts the canonical bytes to text.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The text write result produced by the operation.</returns>
    EmailTextWriteResult WriteTextWithEntity(MimeEntity entity);
    /// <summary>
    /// Serializes a MIME entity known to contain only textual leaves and strictly converts the byte-writer output to text.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The text write result produced by the operation.</returns>
    EmailTextWriteResult WriteTextWithEntityAndOptions(MimeEntity entity, EmailTextWriteOptions options);
    /// <summary>
    /// Counts the bytes that the operation would process.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The computed count or size.</returns>
    long CountBytesWithMessageAndOptions(EmailMessage message, EmailWriteOptions options);
    /// <summary>
    /// Counts the bytes that the operation would process.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The computed count or size.</returns>
    long CountBytesWithMessage(EmailMessage message);
    /// <summary>
    /// Counts the bytes that the operation would process.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The computed count or size.</returns>
    long CountBytesWithEntityAndOptions(MimeEntity entity, EmailWriteOptions options);
    /// <summary>
    /// Counts the bytes that the operation would process.
    /// </summary>
    /// <param name = "entity">The MIME entity to process.</param>
    /// <returns>The computed count or size.</returns>
    long CountBytesWithEntity(MimeEntity entity);
}
