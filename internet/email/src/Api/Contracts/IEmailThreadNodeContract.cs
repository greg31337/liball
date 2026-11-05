using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailThreadNode"/>.
/// </summary>
public interface IEmailThreadNodeContract
{
    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    EmailMessage GetMessage();
    /// <summary>
    /// Gets the identity.
    /// </summary>
    /// <returns>The current identity.</returns>
    EmailThreadIdentity GetIdentity();
    /// <summary>
    /// Gets the input index.
    /// </summary>
    /// <returns>The current input index.</returns>
    int GetInputIndex();
    /// <summary>
    /// Determines whether the parent is present or available.
    /// </summary>
    /// <returns><c>true</c> when the parent is available; otherwise, <c>false</c>.</returns>
    bool HasParent();
    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <returns>The current parent.</returns>
    EmailThreadNode? GetParent();
    /// <summary>
    /// Determines whether the current value or state is root.
    /// </summary>
    /// <returns><c>true</c> when the value or state is root; otherwise, <c>false</c>.</returns>
    bool IsRoot();
    /// <summary>
    /// Gets the child count.
    /// </summary>
    /// <returns>The current child count.</returns>
    int GetChildCount();
    /// <summary>
    /// Gets the child at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested child.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailThreadNode GetChildAt(int index);
    /// <summary>
    /// Gets the children.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current children.</returns>
    List<EmailThreadNode> GetChildren();
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
    /// <summary>
    /// Attaches the to.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    void AttachTo(EmailThreadNode value);
    /// <summary>
    /// Adds an issue to the current collection or model.
    /// </summary>
    /// <param name = "issue">The issue used by the operation.</param>
    void AddIssue(EmailIssue issue);
}
