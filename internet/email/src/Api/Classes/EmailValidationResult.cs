using System;

namespace email;
/// <summary>
/// Describes the outcome of a validation operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailValidationResult : IEmailValidationResultContract
{
    EmailIssueList syntaxIssues;
    EmailIssueList semanticIssues;
    int validatedEntityCount;
    int validatedHeaderCount;
    /// <summary>
    /// Creates a new validation result.
    /// </summary>
    /// <param name = "syntaxIssues">The syntax issues used by the operation.</param>
    /// <param name = "semanticIssues">The semantic issues used by the operation.</param>
    /// <param name = "validatedEntityCount">The number of items or bytes to process.</param>
    /// <param name = "validatedHeaderCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailValidationResult(EmailIssueList syntaxIssues, EmailIssueList semanticIssues, int validatedEntityCount, int validatedHeaderCount)
    {
        if (syntaxIssues == null)
        {
            throw new ArgumentNullException("syntaxIssues");
        }

        if (semanticIssues == null)
        {
            throw new ArgumentNullException("semanticIssues");
        }

        if (validatedEntityCount < 0 || validatedHeaderCount < 0)
        {
            throw new ArgumentOutOfRangeException("validatedEntityCount");
        }

        this.syntaxIssues = new EmailIssueList();
        this.syntaxIssues.AddRange(syntaxIssues);
        this.semanticIssues = new EmailIssueList();
        this.semanticIssues.AddRange(semanticIssues);
        this.validatedEntityCount = validatedEntityCount;
        this.validatedHeaderCount = validatedHeaderCount;
    }

    /// <summary>
    /// Gets the syntax issues.
    /// </summary>
    /// <returns>The current syntax issues.</returns>
    public EmailIssueList GetSyntaxIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(syntaxIssues);
        return copy;
    }

    /// <summary>
    /// Gets the semantic issues.
    /// </summary>
    /// <returns>The current semantic issues.</returns>
    public EmailIssueList GetSemanticIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(semanticIssues);
        return copy;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList combined = GetSyntaxIssues();
        combined.AddRange(semanticIssues);
        return combined;
    }

    /// <summary>
    /// Gets the validated entity count.
    /// </summary>
    /// <returns>The current validated entity count.</returns>
    public int GetValidatedEntityCount()
    {
        return validatedEntityCount;
    }

    /// <summary>
    /// Gets the validated header count.
    /// </summary>
    /// <returns>The current validated header count.</returns>
    public int GetValidatedHeaderCount()
    {
        return validatedHeaderCount;
    }

    /// <summary>
    /// Determines whether the current value or state is syntax valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is syntax valid; otherwise, <c>false</c>.</returns>
    public bool IsSyntaxValid()
    {
        return !syntaxIssues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the current value or state is semantically valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is semantically valid; otherwise, <c>false</c>.</returns>
    public bool IsSemanticallyValid()
    {
        return !semanticIssues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the current value or state is valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is valid; otherwise, <c>false</c>.</returns>
    public bool IsValid()
    {
        return IsSyntaxValid() && IsSemanticallyValid();
    }
}
