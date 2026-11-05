using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a thread node.
/// </summary>
public class EmailThreadNode : IEmailThreadNodeContract
{
    EmailMessage message;
    EmailThreadIdentity identity;
    int inputIndex;
    EmailThreadNode? parent;
    List<EmailThreadNode> children;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new thread node.
    /// </summary>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "identity">The identity used by the operation.</param>
    /// <param name = "inputIndex">The zero-based input index.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailThreadNode(EmailMessage message, EmailThreadIdentity identity, int inputIndex)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (identity == null)
        {
            throw new ArgumentNullException("identity");
        }

        if (inputIndex < 0)
        {
            throw new ArgumentOutOfRangeException("inputIndex");
        }

        this.message = message;
        this.identity = identity;
        this.inputIndex = inputIndex;
        parent = null;
        children = new List<EmailThreadNode>();
        issues = identity.GetIssues();
    }

    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    public EmailMessage GetMessage()
    {
        return message;
    }

    /// <summary>
    /// Gets the identity.
    /// </summary>
    /// <returns>The current identity.</returns>
    public EmailThreadIdentity GetIdentity()
    {
        return identity;
    }

    /// <summary>
    /// Gets the input index.
    /// </summary>
    /// <returns>The current input index.</returns>
    public int GetInputIndex()
    {
        return inputIndex;
    }

    /// <summary>
    /// Determines whether the parent is present or available.
    /// </summary>
    /// <returns><c>true</c> when the parent is available; otherwise, <c>false</c>.</returns>
    public bool HasParent()
    {
        return parent != null;
    }

    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <returns>The current parent.</returns>
    public EmailThreadNode? GetParent()
    {
        return parent;
    }

    /// <summary>
    /// Determines whether the current value or state is root.
    /// </summary>
    /// <returns><c>true</c> when the value or state is root; otherwise, <c>false</c>.</returns>
    public bool IsRoot()
    {
        return parent == null;
    }

    /// <summary>
    /// Gets the child count.
    /// </summary>
    /// <returns>The current child count.</returns>
    public int GetChildCount()
    {
        return children.Count;
    }

    /// <summary>
    /// Gets the child at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested child.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailThreadNode GetChildAt(int index)
    {
        if (index < 0 || index >= children.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return children[index];
    }

    /// <summary>
    /// Gets the children.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current children.</returns>
    public List<EmailThreadNode> GetChildren()
    {
        return new List<EmailThreadNode>(children);
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
    /// Attaches the to.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public void AttachTo(EmailThreadNode value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (parent != null)
        {
            throw new InvalidOperationException("A thread node can have only one parent.");
        }

        parent = value;
        value.children.Add(this);
    }

    /// <summary>
    /// Adds an issue to the current collection or model.
    /// </summary>
    /// <param name = "issue">The issue used by the operation.</param>
    public void AddIssue(EmailIssue issue)
    {
        issues.Add(issue);
    }
}
