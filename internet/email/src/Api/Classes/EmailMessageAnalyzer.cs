using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Analyzes message and returns a structured result.
/// </summary>
public class EmailMessageAnalyzer : IEmailMessageAnalyzerContract
{
    internal static EmailMessageSummary AnalyzeImplementationWithMessage(EmailMessage message)
    {
        return AnalyzeImplementationWithMessageAndOptions(message, new EmailMessageAnalysisOptions());
    }

    /// <summary>
    /// Analyzes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The message summary produced by the operation.</returns>
    public EmailMessageSummary AnalyzeWithMessage(EmailMessage message)
    {
        return EmailMessageAnalyzer.AnalyzeImplementationWithMessage(message);
    }

    internal static EmailMessageSummary AnalyzeImplementationWithMessageAndOptions(EmailMessage message, EmailMessageAnalysisOptions options)
    {
        if (message == null)
            throw new ArgumentNullException("message");
        if (options == null)
            throw new ArgumentNullException("options");
        EmailMessageAnalyzerWorker worker = new EmailMessageAnalyzerWorker(message, options.Clone());
        return worker.Analyze();
    }

    /// <summary>
    /// Analyzes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message summary produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMessageSummary AnalyzeWithMessageAndOptions(EmailMessage message, EmailMessageAnalysisOptions options)
    {
        return EmailMessageAnalyzer.AnalyzeImplementationWithMessageAndOptions(message, options);
    }

    internal static EmailMessageSummary SummarizeImplementationWithMessage(EmailMessage message)
    {
        return AnalyzeImplementationWithMessage(message);
    }

    /// <summary>
    /// Summarizes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <returns>The message summary produced by the operation.</returns>
    public EmailMessageSummary SummarizeWithMessage(EmailMessage message)
    {
        return EmailMessageAnalyzer.SummarizeImplementationWithMessage(message);
    }

    internal static EmailMessageSummary SummarizeImplementationWithMessageAndOptions(EmailMessage message, EmailMessageAnalysisOptions options)
    {
        return AnalyzeImplementationWithMessageAndOptions(message, options);
    }

    /// <summary>
    /// Summarizes the supplied value.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>The message summary produced by the operation.</returns>
    public EmailMessageSummary SummarizeWithMessageAndOptions(EmailMessage message, EmailMessageAnalysisOptions options)
    {
        return EmailMessageAnalyzer.SummarizeImplementationWithMessageAndOptions(message, options);
    }
}
