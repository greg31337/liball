using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a rfc 2231 decode operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailRfc2231DecodeResult : IEmailRfc2231DecodeResultContract
{
    List<EmailRfc2231DecodedParameter> parameters;
    List<EmailRfc2231Parameter> sourceParameters;
    EmailIssueList issues;
    long decodedByteCount;
    /// <summary>
    /// Creates a new rfc 2231 decode result.
    /// </summary>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "sourceParameters">The source parameters used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "decodedByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRfc2231DecodeResult(List<EmailRfc2231DecodedParameter> parameters, List<EmailRfc2231Parameter> sourceParameters, EmailIssueList issues, long decodedByteCount)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        if (sourceParameters == null)
        {
            throw new ArgumentNullException("sourceParameters");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (decodedByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("decodedByteCount");
        }

        this.parameters = new List<EmailRfc2231DecodedParameter>(parameters);
        this.sourceParameters = new List<EmailRfc2231Parameter>(sourceParameters);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.decodedByteCount = decodedByteCount;
    }

    /// <summary>
    /// Gets the parameter count.
    /// </summary>
    /// <returns>The current parameter count.</returns>
    public int GetParameterCount()
    {
        return parameters.Count;
    }

    /// <summary>
    /// Gets the parameter.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current parameter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRfc2231DecodedParameter GetParameter(int index)
    {
        if (index < 0 || index >= parameters.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return parameters[index];
    }

    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parameters.</returns>
    public List<EmailRfc2231DecodedParameter> GetParameters()
    {
        return new List<EmailRfc2231DecodedParameter>(parameters);
    }

    /// <summary>
    /// Gets the source parameter count.
    /// </summary>
    /// <returns>The current source parameter count.</returns>
    public int GetSourceParameterCount()
    {
        return sourceParameters.Count;
    }

    /// <summary>
    /// Gets the source parameter.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current source parameter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRfc2231Parameter GetSourceParameter(int index)
    {
        if (index < 0 || index >= sourceParameters.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return sourceParameters[index];
    }

    /// <summary>
    /// Gets the decoded byte count.
    /// </summary>
    /// <returns>The current decoded byte count.</returns>
    public long GetDecodedByteCount()
    {
        return decodedByteCount;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    public bool HasFatalError()
    {
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal);
    }
}
