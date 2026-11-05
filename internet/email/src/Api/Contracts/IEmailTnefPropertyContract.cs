using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefProperty"/>.
/// </summary>
public interface IEmailTnefPropertyContract
{
    /// <summary>
    /// Gets the type code.
    /// </summary>
    /// <returns>The current type code.</returns>
    int GetTypeCode();
    /// <summary>
    /// Gets the property ID.
    /// </summary>
    /// <returns>The current property ID.</returns>
    int GetPropertyId();
    /// <summary>
    /// Gets the tag.
    /// </summary>
    /// <returns>The current tag.</returns>
    uint GetTag();
    /// <summary>
    /// Determines whether the current value or state is multi valued.
    /// </summary>
    /// <returns><c>true</c> when the value or state is multi valued; otherwise, <c>false</c>.</returns>
    bool IsMultiValued();
    /// <summary>
    /// Determines whether the current value or state is named.
    /// </summary>
    /// <returns><c>true</c> when the value or state is named; otherwise, <c>false</c>.</returns>
    bool IsNamed();
    /// <summary>
    /// Gets the named kind.
    /// </summary>
    /// <returns>The current named kind.</returns>
    EmailTnefNamedPropertyKind GetNamedKind();
    /// <summary>
    /// Gets the named property set.
    /// </summary>
    /// <returns>The current named property set.</returns>
    string? GetNamedPropertySet();
    /// <summary>
    /// Gets the named numeric ID.
    /// </summary>
    /// <returns>The current named numeric ID.</returns>
    long GetNamedNumericId();
    /// <summary>
    /// Gets the named string ID.
    /// </summary>
    /// <returns>The current named string ID.</returns>
    string? GetNamedStringId();
    /// <summary>
    /// Gets the value count.
    /// </summary>
    /// <returns>The current value count.</returns>
    int GetValueCount();
    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    byte[] GetValueAt(int index);
    /// <summary>
    /// Determines whether the text value at is present or available.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns><c>true</c> when the text value at is available; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    bool HasTextValueAt(int index);
    /// <summary>
    /// Gets the text value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested text value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    string? GetTextValueAt(int index);
    /// <summary>
    /// Determines whether the integer value at is present or available.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns><c>true</c> when the integer value at is available; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    bool HasIntegerValueAt(int index);
    /// <summary>
    /// Gets the integer value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested integer value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    long GetIntegerValueAt(int index);
    /// <summary>
    /// Determines whether the text value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the text value is available; otherwise, <c>false</c>.</returns>
    bool HasTextValue();
    /// <summary>
    /// Gets the text value.
    /// </summary>
    /// <returns>The current text value.</returns>
    string? GetTextValue();
    /// <summary>
    /// Determines whether the integer value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the integer value is available; otherwise, <c>false</c>.</returns>
    bool HasIntegerValue();
    /// <summary>
    /// Gets the integer value.
    /// </summary>
    /// <returns>The current integer value.</returns>
    long GetIntegerValue();
    /// <summary>
    /// Gets the raw encoding.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw encoding.</returns>
    byte[] GetRawEncoding();
    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    EmailRange GetSourceRange();
    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    bool IsComplete();
}
