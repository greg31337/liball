using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArchiveIndexRecord"/>.
/// </summary>
public interface IEmailArchiveIndexRecordContract
{
    /// <summary>
    /// Gets the source kind.
    /// </summary>
    /// <returns>The current source kind.</returns>
    EmailArchiveSourceKind GetSourceKind();
    /// <summary>
    /// Determines whether the source path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source path is available; otherwise, <c>false</c>.</returns>
    bool HasSourcePath();
    /// <summary>
    /// Gets the source path.
    /// </summary>
    /// <returns>The current source path.</returns>
    string? GetSourcePath();
    /// <summary>
    /// Determines whether the index is present or available.
    /// </summary>
    /// <returns><c>true</c> when the index is available; otherwise, <c>false</c>.</returns>
    bool HasIndex();
    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    int GetIndex();
    /// <summary>
    /// Determines whether the archive range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the archive range is available; otherwise, <c>false</c>.</returns>
    bool HasArchiveRange();
    /// <summary>
    /// Gets the archive range.
    /// </summary>
    /// <returns>The current archive range.</returns>
    EmailRange? GetArchiveRange();
    /// <summary>
    /// Determines whether the message range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message range is available; otherwise, <c>false</c>.</returns>
    bool HasMessageRange();
    /// <summary>
    /// Gets the message range.
    /// </summary>
    /// <returns>The current message range.</returns>
    EmailRange? GetMessageRange();
    /// <summary>
    /// Gets the message byte count.
    /// </summary>
    /// <returns>The current message byte count.</returns>
    long GetMessageByteCount();
    /// <summary>
    /// Gets the parse status.
    /// </summary>
    /// <returns>The current parse status.</returns>
    EmailArchiveParseStatus GetParseStatus();
    /// <summary>
    /// Gets the selected header count.
    /// </summary>
    /// <returns>The current selected header count.</returns>
    int GetSelectedHeaderCount();
    /// <summary>
    /// Gets the selected header at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested selected header.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailArchiveIndexHeader GetSelectedHeaderAt(int index);
    /// <summary>
    /// Gets the selected headers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current selected headers.</returns>
    List<EmailArchiveIndexHeader> GetSelectedHeaders();
    /// <summary>
    /// Gets the first selected header value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current first selected header value.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    string? GetFirstSelectedHeaderValue(string name);
    /// <summary>
    /// Determines whether the exact hash is present or available.
    /// </summary>
    /// <returns><c>true</c> when the exact hash is available; otherwise, <c>false</c>.</returns>
    bool HasExactHash();
    /// <summary>
    /// Gets the exact hash.
    /// </summary>
    /// <returns>The current exact hash.</returns>
    EmailHashResult? GetExactHash();
    /// <summary>
    /// Determines whether the semantic hash is present or available.
    /// </summary>
    /// <returns><c>true</c> when the semantic hash is available; otherwise, <c>false</c>.</returns>
    bool HasSemanticHash();
    /// <summary>
    /// Gets the semantic hash.
    /// </summary>
    /// <returns>The current semantic hash.</returns>
    EmailHashResult? GetSemanticHash();
    /// <summary>
    /// Determines whether the summary is present or available.
    /// </summary>
    /// <returns><c>true</c> when the summary is available; otherwise, <c>false</c>.</returns>
    bool HasSummary();
    /// <summary>
    /// Gets the summary.
    /// </summary>
    /// <returns>The current summary.</returns>
    EmailMessageSummary? GetSummary();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is parse success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is parse success; otherwise, <c>false</c>.</returns>
    bool IsParseSuccess();
}
