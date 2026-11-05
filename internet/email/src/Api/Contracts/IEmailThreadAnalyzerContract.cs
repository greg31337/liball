using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailThreadAnalyzer"/>.
/// </summary>
public interface IEmailThreadAnalyzerContract
{
    /// <summary>
    /// Builds the configured graph.
    /// </summary>
    /// <param name = "messages">The messages used by the operation.</param>
    /// <returns>The thread graph produced by the operation.</returns>
    EmailThreadGraph BuildGraphWithMessages(List<EmailMessage> messages);
    /// <summary>
    /// Builds the configured graph.
    /// </summary>
    /// <param name = "messages">The messages used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The thread graph produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailThreadGraph BuildGraphWithMessagesAndOptions(List<EmailMessage> messages, EmailThreadOptions options);
    /// <summary>
    /// Extracts the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The thread identity produced by the operation.</returns>
    EmailThreadIdentity ExtractWithMessage(EmailMessage message);
    /// <summary>
    /// Extracts the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The thread identity produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    EmailThreadIdentity ExtractWithMessageAndOptions(EmailMessage message, EmailThreadOptions options);
    /// <summary>
    /// Normalizes the subject.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The string produced by the operation.</returns>
    string NormalizeSubjectWithValue(string value);
    /// <summary>
    /// Normalizes the subject.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    string NormalizeSubjectWithValueAndOptions(string? value, EmailThreadOptions options);
    /// <summary>
    /// Determines whether the instance can onicalize identifier.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string CanonicalizeIdentifier(EmailMessageId? identifier);
}
