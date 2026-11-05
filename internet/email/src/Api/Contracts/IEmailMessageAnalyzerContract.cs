using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMessageAnalyzer"/>.
/// </summary>
public interface IEmailMessageAnalyzerContract
{
    /// <summary>
    /// Analyzes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The message summary produced by the operation.</returns>
    EmailMessageSummary AnalyzeWithMessage(EmailMessage message);
    /// <summary>
    /// Analyzes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message summary produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailMessageSummary AnalyzeWithMessageAndOptions(EmailMessage message, EmailMessageAnalysisOptions options);
    /// <summary>
    /// Summarizes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The message summary produced by the operation.</returns>
    EmailMessageSummary SummarizeWithMessage(EmailMessage message);
    /// <summary>
    /// Summarizes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message summary produced by the operation.</returns>
    EmailMessageSummary SummarizeWithMessageAndOptions(EmailMessage message, EmailMessageAnalysisOptions options);
}
