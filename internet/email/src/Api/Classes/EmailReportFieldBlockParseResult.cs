using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a report field block parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailReportFieldBlockParseResult : IEmailReportFieldBlockParseResultContract
{
    List<EmailReportFieldBlock> blocks;
    EmailIssueList issues;
    int sourceByteCount;
    /// <summary>
    /// Creates a new report field block parse result.
    /// </summary>
    /// <param name = "blocks">The blocks used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailReportFieldBlockParseResult(List<EmailReportFieldBlock> blocks, EmailIssueList issues, int sourceByteCount)
    {
        if (blocks == null)
            throw new ArgumentNullException("blocks");
        if (issues == null)
            throw new ArgumentNullException("issues");
        if (sourceByteCount < 0)
            throw new ArgumentOutOfRangeException("sourceByteCount");
        this.blocks = new List<EmailReportFieldBlock>(blocks);
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.sourceByteCount = sourceByteCount;
    }

    /// <summary>
    /// Gets the block count.
    /// </summary>
    /// <returns>The current block count.</returns>
    public int GetBlockCount()
    {
        return blocks.Count;
    }

    /// <summary>
    /// Gets the block at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested block.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailReportFieldBlock GetBlockAt(int index)
    {
        if (index < 0 || index >= blocks.Count)
            throw new ArgumentOutOfRangeException("index");
        return blocks[index];
    }

    /// <summary>
    /// Gets the blocks.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current blocks.</returns>
    public List<EmailReportFieldBlock> GetBlocks()
    {
        return new List<EmailReportFieldBlock>(blocks);
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
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public int GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
