using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHtmlToken"/>.
/// </summary>
public interface IEmailHtmlTokenContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailHtmlTokenKind GetKind();
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string GetName();
    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <returns>The current data.</returns>
    string GetData();
    /// <summary>
    /// Gets the attribute count.
    /// </summary>
    /// <returns>The current attribute count.</returns>
    int GetAttributeCount();
    /// <summary>
    /// Gets the attribute.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current attribute.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailHtmlAttribute GetAttribute(int index);
    /// <summary>
    /// Gets the attribute.
    /// </summary>
    /// <param name = "expectedName">The expected name used by the operation.</param>
    /// <returns>The current attribute.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailHtmlAttribute? GetAttributeNamed(string expectedName);
    /// <summary>
    /// Gets the attributes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attributes.</returns>
    List<EmailHtmlAttribute> GetAttributes();
    /// <summary>
    /// Determines whether the current value or state is self closing.
    /// </summary>
    /// <returns><c>true</c> when the value or state is self closing; otherwise, <c>false</c>.</returns>
    bool IsSelfClosing();
    /// <summary>
    /// Determines whether the current value or state is raw text.
    /// </summary>
    /// <returns><c>true</c> when the value or state is raw text; otherwise, <c>false</c>.</returns>
    bool IsRawText();
    /// <summary>
    /// Determines whether the current value or state is malformed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is malformed; otherwise, <c>false</c>.</returns>
    bool IsMalformed();
    /// <summary>
    /// Gets the source offset.
    /// </summary>
    /// <returns>The current source offset.</returns>
    int GetSourceOffset();
    /// <summary>
    /// Gets the source length.
    /// </summary>
    /// <returns>The current source length.</returns>
    int GetSourceLength();
}
