using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a path.
/// </summary>
public class EmailPath : IEmailPathContract
{
    List<int> segments;
    /// <summary>
    /// Creates a path containing the supplied segments.
    /// </summary>
    public EmailPath()
    {
        segments = new List<int>();
    }

    internal static EmailPath CreateImplementationWith()
    {
        return new EmailPath();
    }

    internal static EmailPath CreateImplementationWithSegments(List<int> segments)
    {
        if (segments == null)
        {
            throw new ArgumentNullException("segments");
        }

        EmailPath value = new EmailPath();
        int index = 0;
        while (index < segments.Count)
        {
            int segment = segments[index];
            if (segment < 0)
            {
                throw new ArgumentOutOfRangeException("segments");
            }

            value.segments.Add(segment);
            index++;
        }

        return value;
    }

    internal static EmailPath CreateRootImplementation()
    {
        return new EmailPath();
    }

    /// <summary>
    /// Determines whether the current value or state is root.
    /// </summary>
    /// <returns><c>true</c> when the value or state is root; otherwise, <c>false</c>.</returns>
    public bool IsRoot()
    {
        return segments.Count == 0;
    }

    /// <summary>
    /// Gets the depth.
    /// </summary>
    /// <returns>The current depth.</returns>
    public int GetDepth()
    {
        return segments.Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public int GetAt(int index)
    {
        if (index < 0 || index >= segments.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return segments[index];
    }

    /// <summary>
    /// Gets the segments.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current segments.</returns>
    public List<int> GetSegments()
    {
        return new List<int>(segments);
    }

    /// <summary>
    /// Appends the supplied value.
    /// </summary>
    /// <param name = "childIndex">The zero-based child index.</param>
    /// <returns>The path produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailPath Append(int childIndex)
    {
        if (childIndex < 0)
        {
            throw new ArgumentOutOfRangeException("childIndex");
        }

        List<int> appended = new List<int>(segments);
        appended.Add(childIndex);
        return CreateImplementationWithSegments(appended);
    }

    /// <summary>
    /// Determines whether the parent is present or available.
    /// </summary>
    /// <returns><c>true</c> when the parent is available; otherwise, <c>false</c>.</returns>
    public bool HasParent()
    {
        return segments.Count > 0;
    }

    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <returns>The current parent.</returns>
    public EmailPath? GetParent()
    {
        if (segments.Count == 0)
        {
            return null;
        }

        List<int> parent = new List<int>();
        int index = 0;
        while (index < segments.Count - 1)
        {
            parent.Add(segments[index]);
            index++;
        }

        return CreateImplementationWithSegments(parent);
    }

    /// <summary>
    /// Gets the last child index.
    /// </summary>
    /// <returns>The current last child index.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public int GetLastChildIndex()
    {
        if (segments.Count == 0)
        {
            throw new InvalidOperationException("The root path has no child index.");
        }

        return segments[segments.Count - 1];
    }

    /// <summary>
    /// Determines whether the current value or state is prefix of.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <returns><c>true</c> when the value or state is prefix of; otherwise, <c>false</c>.</returns>
    public bool IsPrefixOf(EmailPath other)
    {
        if (other == null)
        {
            return false;
        }

        if (segments.Count > other.GetDepth())
        {
            return false;
        }

        int index = 0;
        while (index < segments.Count)
        {
            if (segments[index] != other.GetAt(index))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    /// <summary>
    /// Determines whether this value equals the supplied path.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool EqualsPath(EmailPath other)
    {
        if (other == null)
        {
            return false;
        }

        if (segments.Count != other.GetDepth())
        {
            return false;
        }

        int index = 0;
        while (index < segments.Count)
        {
            if (segments[index] != other.GetAt(index))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    /// <summary>
    /// Gets the display text.
    /// </summary>
    /// <returns>The current display text.</returns>
    public string GetDisplayText()
    {
        if (segments.Count == 0)
        {
            return "root";
        }

        StringBuilder builder = new StringBuilder("root");
        int index = 0;
        while (index < segments.Count)
        {
            builder.Append('/');
            builder.Append((long)segments[index] + 1);
            index++;
        }

        return builder.ToString();
    }
}
