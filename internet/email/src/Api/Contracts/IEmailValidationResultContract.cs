using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailValidationResult"/>.
/// </summary>
public interface IEmailValidationResultContract
{
    /// <summary>
    /// Gets the syntax issues.
    /// </summary>
    /// <returns>The current syntax issues.</returns>
    EmailIssueList GetSyntaxIssues();
    /// <summary>
    /// Gets the semantic issues.
    /// </summary>
    /// <returns>The current semantic issues.</returns>
    EmailIssueList GetSemanticIssues();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the validated entity count.
    /// </summary>
    /// <returns>The current validated entity count.</returns>
    int GetValidatedEntityCount();
    /// <summary>
    /// Gets the validated header count.
    /// </summary>
    /// <returns>The current validated header count.</returns>
    int GetValidatedHeaderCount();
    /// <summary>
    /// Determines whether the current value or state is syntax valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is syntax valid; otherwise, <c>false</c>.</returns>
    bool IsSyntaxValid();
    /// <summary>
    /// Determines whether the current value or state is semantically valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is semantically valid; otherwise, <c>false</c>.</returns>
    bool IsSemanticallyValid();
    /// <summary>
    /// Determines whether the current value or state is valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is valid; otherwise, <c>false</c>.</returns>
    bool IsValid();
}
