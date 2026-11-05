using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefBody"/>.
/// </summary>
public interface IEmailTnefBodyContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailTnefBodyKind GetKind();
    /// <summary>
    /// Determines whether the text is present or available.
    /// </summary>
    /// <returns><c>true</c> when the text is available; otherwise, <c>false</c>.</returns>
    bool HasText();
    /// <summary>
    /// Gets the text.
    /// </summary>
    /// <returns>The current text.</returns>
    string? GetText();
    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content.</returns>
    byte[] GetContent();
    /// <summary>
    /// Gets the original content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original content.</returns>
    byte[] GetOriginalContent();
    /// <summary>
    /// Gets the charset.
    /// </summary>
    /// <returns>The current charset.</returns>
    string GetCharset();
    /// <summary>
    /// Determines whether the source property is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source property is available; otherwise, <c>false</c>.</returns>
    bool HasSourceProperty();
    /// <summary>
    /// Gets the source property.
    /// </summary>
    /// <returns>The current source property.</returns>
    EmailTnefProperty? GetSourceProperty();
    /// <summary>
    /// Determines whether the source attribute is present or available.
    /// </summary>
    /// <returns><c>true</c> when the source attribute is available; otherwise, <c>false</c>.</returns>
    bool HasSourceAttribute();
    /// <summary>
    /// Gets the source attribute.
    /// </summary>
    /// <returns>The current source attribute.</returns>
    EmailTnefAttribute? GetSourceAttribute();
}
