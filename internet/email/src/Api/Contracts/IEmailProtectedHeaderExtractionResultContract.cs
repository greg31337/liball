using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailProtectedHeaderExtractionResult"/>.
/// </summary>
public interface IEmailProtectedHeaderExtractionResultContract
{
    /// <summary>
    /// Determines whether the current value or state is recognized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is recognized; otherwise, <c>false</c>.</returns>
    bool IsRecognized();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Gets the intent.
    /// </summary>
    /// <returns>The current intent.</returns>
    EmailProtectedHeaderIntent GetIntent();
    /// <summary>
    /// Gets the protocol.
    /// </summary>
    /// <returns>The current protocol.</returns>
    EmailProtectedHeaderProtocol GetProtocol();
    /// <summary>
    /// Gets the protected headers.
    /// </summary>
    /// <returns>The current protected headers.</returns>
    EmailHeaders GetProtectedHeaders();
    /// <summary>
    /// Gets the recorded outer headers.
    /// </summary>
    /// <returns>The current recorded outer headers.</returns>
    EmailHeaders GetRecordedOuterHeaders();
    /// <summary>
    /// Gets the display headers.
    /// </summary>
    /// <returns>The current display headers.</returns>
    EmailHeaders GetDisplayHeaders();
    /// <summary>
    /// Gets the reply headers.
    /// </summary>
    /// <returns>The current reply headers.</returns>
    EmailHeaders GetReplyHeaders();
    /// <summary>
    /// Gets the entry count.
    /// </summary>
    /// <returns>The current entry count.</returns>
    int GetEntryCount();
    /// <summary>
    /// Gets the entry at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested entry.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailProtectedHeaderEntry GetEntryAt(int index);
    /// <summary>
    /// Gets the entries.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current entries.</returns>
    List<EmailProtectedHeaderEntry> GetEntries();
    /// <summary>
    /// Gets the mismatch count.
    /// </summary>
    /// <returns>The current mismatch count.</returns>
    int GetMismatchCount();
    /// <summary>
    /// Gets the mismatch at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested mismatch.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailProtectedHeaderMismatch GetMismatchAt(int index);
    /// <summary>
    /// Gets the mismatches.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current mismatches.</returns>
    List<EmailProtectedHeaderMismatch> GetMismatches();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
