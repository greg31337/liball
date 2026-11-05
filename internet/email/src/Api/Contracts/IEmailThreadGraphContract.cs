using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailThreadGraph"/>.
/// </summary>
public interface IEmailThreadGraphContract
{
    /// <summary>
    /// Gets the node count.
    /// </summary>
    /// <returns>The current node count.</returns>
    int GetNodeCount();
    /// <summary>
    /// Gets the node at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested node.</returns>
    EmailThreadNode GetNodeAt(int index);
    /// <summary>
    /// Gets the nodes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current nodes.</returns>
    List<EmailThreadNode> GetNodes();
    /// <summary>
    /// Gets the root count.
    /// </summary>
    /// <returns>The current root count.</returns>
    int GetRootCount();
    /// <summary>
    /// Gets the root at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested root.</returns>
    EmailThreadNode GetRootAt(int index);
    /// <summary>
    /// Gets the roots.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current roots.</returns>
    List<EmailThreadNode> GetRoots();
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
