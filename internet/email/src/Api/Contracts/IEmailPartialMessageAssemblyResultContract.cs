using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPartialMessageAssemblyResult"/>.
/// </summary>
public interface IEmailPartialMessageAssemblyResultContract
{
    /// <summary>
    /// Gets the fragment ID.
    /// </summary>
    /// <returns>The current fragment ID.</returns>
    string GetFragmentId();
    /// <summary>
    /// Determines whether the expected fragment count is present or available.
    /// </summary>
    /// <returns><c>true</c> when the expected fragment count is available; otherwise, <c>false</c>.</returns>
    bool HasExpectedFragmentCount();
    /// <summary>
    /// Gets the expected fragment count.
    /// </summary>
    /// <returns>The current expected fragment count.</returns>
    long GetExpectedFragmentCount();
    /// <summary>
    /// Determines whether the current value or state is sequence complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is sequence complete; otherwise, <c>false</c>.</returns>
    bool IsSequenceComplete();
    /// <summary>
    /// Gets the fragment count.
    /// </summary>
    /// <returns>The current fragment count.</returns>
    int GetFragmentCount();
    /// <summary>
    /// Gets the fragment at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested fragment.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailPartialMessageFragment GetFragmentAt(int index);
    /// <summary>
    /// Gets the fragments.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current fragments.</returns>
    List<EmailPartialMessageFragment> GetFragments();
    /// <summary>
    /// Gets the missing fragment count.
    /// </summary>
    /// <returns>The current missing fragment count.</returns>
    int GetMissingFragmentCount();
    /// <summary>
    /// Gets the missing fragment number at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested missing fragment number.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    long GetMissingFragmentNumberAt(int index);
    /// <summary>
    /// Gets the missing fragment numbers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current missing fragment numbers.</returns>
    List<long> GetMissingFragmentNumbers();
    /// <summary>
    /// Gets the duplicate fragment count.
    /// </summary>
    /// <returns>The current duplicate fragment count.</returns>
    int GetDuplicateFragmentCount();
    /// <summary>
    /// Gets the duplicate fragment number at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested duplicate fragment number.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    long GetDuplicateFragmentNumberAt(int index);
    /// <summary>
    /// Gets the duplicate fragment numbers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current duplicate fragment numbers.</returns>
    List<long> GetDuplicateFragmentNumbers();
    /// <summary>
    /// Determines whether the current value or state is assembled.
    /// </summary>
    /// <returns><c>true</c> when the value or state is assembled; otherwise, <c>false</c>.</returns>
    bool IsAssembled();
    /// <summary>
    /// Gets the inner bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current inner bytes.</returns>
    byte[]? GetInnerBytes();
    /// <summary>
    /// Determines whether the operation or source value was reparse requested.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasReparseRequested();
    /// <summary>
    /// Determines whether the inner message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the inner message is available; otherwise, <c>false</c>.</returns>
    bool HasInnerMessage();
    /// <summary>
    /// Gets the inner message.
    /// </summary>
    /// <returns>The current inner message.</returns>
    EmailMessage? GetInnerMessage();
    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    bool HasMessage();
    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    EmailMessage? GetMessage();
    /// <summary>
    /// Determines whether the reassembled bytes is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reassembled bytes is available; otherwise, <c>false</c>.</returns>
    bool HasReassembledBytes();
    /// <summary>
    /// Gets the reassembled bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current reassembled bytes.</returns>
    byte[]? GetReassembledBytes();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
