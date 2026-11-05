using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailResentBuilder"/>.
/// </summary>
public interface IEmailResentBuilderContract
{
    /// <summary>
    /// Gets the options.
    /// </summary>
    /// <returns>The current options.</returns>
    EmailResentOptions GetOptions();
    /// <summary>
    /// Sets the date used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetDate(EmailDateTime value);
    /// <summary>
    /// Clears the stored date value or values.
    /// </summary>
    void ClearDate();
    /// <summary>
    /// Sets the from used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetFrom(EmailAddress value);
    /// <summary>
    /// Adds a from to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddFrom(EmailAddress value);
    /// <summary>
    /// Clears the stored from value or values.
    /// </summary>
    void ClearFrom();
    /// <summary>
    /// Sets the sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetSender(EmailAddress value);
    /// <summary>
    /// Clears the stored sender value or values.
    /// </summary>
    void ClearSender();
    /// <summary>
    /// Adds a to to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddTo(EmailAddress value);
    /// <summary>
    /// Clears the stored to value or values.
    /// </summary>
    void ClearTo();
    /// <summary>
    /// Adds a cc to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddCc(EmailAddress value);
    /// <summary>
    /// Clears the stored cc value or values.
    /// </summary>
    void ClearCc();
    /// <summary>
    /// Adds a bcc to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddBcc(EmailAddress value);
    /// <summary>
    /// Clears the stored bcc value or values.
    /// </summary>
    void ClearBcc();
    /// <summary>
    /// Sets the message ID used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    void SetMessageId(EmailMessageId value);
    /// <summary>
    /// Clears the stored message ID value or values.
    /// </summary>
    void ClearMessageId();
    /// <summary>
    /// Sets the envelope sender used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetEnvelopeSender(EmailAddress value);
    /// <summary>
    /// Clears the stored envelope sender value or values.
    /// </summary>
    void ClearEnvelopeSender();
    /// <summary>
    /// Adds an envelope recipient to the current collection or model.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void AddEnvelopeRecipient(EmailAddress value);
    /// <summary>
    /// Clears the stored envelope recipients value or values.
    /// </summary>
    void ClearEnvelopeRecipients();
    /// <summary>
    /// Builds the configured value.
    /// </summary>
    /// <returns>The message produced by the operation.</returns>
    EmailMessage Build();
}
