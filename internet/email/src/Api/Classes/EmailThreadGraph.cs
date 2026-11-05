using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a thread graph.
/// </summary>
public class EmailThreadGraph : IEmailThreadGraphContract
{
    List<EmailThreadNode> nodes;
    List<EmailThreadNode> roots;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new thread graph.
    /// </summary>
    /// <param name = "nodes">The nodes used by the operation.</param>
    /// <param name = "roots">The roots used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailThreadGraph(List<EmailThreadNode> nodes, List<EmailThreadNode> roots, EmailIssueList issues)
    {
        if (nodes == null)
        {
            throw new ArgumentNullException("nodes");
        }

        if (roots == null)
        {
            throw new ArgumentNullException("roots");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        this.nodes = CopyNodes(nodes, "nodes");
        this.roots = CopyNodes(roots, "roots");
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the node count.
    /// </summary>
    /// <returns>The current node count.</returns>
    public int GetNodeCount()
    {
        return nodes.Count;
    }

    /// <summary>
    /// Gets the node at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested node.</returns>
    public EmailThreadNode GetNodeAt(int index)
    {
        return GetAt(nodes, index);
    }

    /// <summary>
    /// Gets the nodes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current nodes.</returns>
    public List<EmailThreadNode> GetNodes()
    {
        return new List<EmailThreadNode>(nodes);
    }

    /// <summary>
    /// Gets the root count.
    /// </summary>
    /// <returns>The current root count.</returns>
    public int GetRootCount()
    {
        return roots.Count;
    }

    /// <summary>
    /// Gets the root at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested root.</returns>
    public EmailThreadNode GetRootAt(int index)
    {
        return GetAt(roots, index);
    }

    /// <summary>
    /// Gets the roots.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current roots.</returns>
    public List<EmailThreadNode> GetRoots()
    {
        return new List<EmailThreadNode>(roots);
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

    static EmailThreadNode GetAt(List<EmailThreadNode> values, int index)
    {
        if (index < 0 || index >= values.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return values[index];
    }

    static List<EmailThreadNode> CopyNodes(List<EmailThreadNode> values, string parameterName)
    {
        List<EmailThreadNode> copy = new List<EmailThreadNode>();
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Thread node collections cannot contain null entries.", parameterName);
            }

            copy.Add(values[index]);
            index++;
        }

        return copy;
    }
}
