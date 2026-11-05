using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPath"/>.
/// </summary>
public interface IEmailPathContract
{
    /// <summary>
    /// Determines whether the current value or state is root.
    /// </summary>
    /// <returns><c>true</c> when the value or state is root; otherwise, <c>false</c>.</returns>
    bool IsRoot();
    /// <summary>
    /// Gets the depth.
    /// </summary>
    /// <returns>The current depth.</returns>
    int GetDepth();
    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    int GetAt(int index);
    /// <summary>
    /// Gets the segments.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current segments.</returns>
    List<int> GetSegments();
    /// <summary>
    /// Appends the supplied value.
    /// </summary>
    /// <param name = "childIndex">The zero-based child index.</param>
    /// <returns>The path produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailPath Append(int childIndex);
    /// <summary>
    /// Determines whether the parent is present or available.
    /// </summary>
    /// <returns><c>true</c> when the parent is available; otherwise, <c>false</c>.</returns>
    bool HasParent();
    /// <summary>
    /// Gets the parent.
    /// </summary>
    /// <returns>The current parent.</returns>
    EmailPath? GetParent();
    /// <summary>
    /// Gets the last child index.
    /// </summary>
    /// <returns>The current last child index.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    int GetLastChildIndex();
    /// <summary>
    /// Determines whether the current value or state is prefix of.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <returns><c>true</c> when the value or state is prefix of; otherwise, <c>false</c>.</returns>
    bool IsPrefixOf(EmailPath other);
    /// <summary>
    /// Determines whether this value equals the supplied path.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool EqualsPath(EmailPath other);
    /// <summary>
    /// Gets the display text.
    /// </summary>
    /// <returns>The current display text.</returns>
    string GetDisplayText();
}
