using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCmsAsn1Node"/>.
/// </summary>
public interface IEmailCmsAsn1NodeContract
{
    /// <summary>
    /// Gets the tag class.
    /// </summary>
    /// <returns>The current tag class.</returns>
    EmailCmsAsn1TagClass GetTagClass();
    /// <summary>
    /// Gets the tag number.
    /// </summary>
    /// <returns>The current tag number.</returns>
    int GetTagNumber();
    /// <summary>
    /// Determines whether the current value or state is constructed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is constructed; otherwise, <c>false</c>.</returns>
    bool IsConstructed();
    /// <summary>
    /// Determines whether the indefinite length is present or available.
    /// </summary>
    /// <returns><c>true</c> when the indefinite length is available; otherwise, <c>false</c>.</returns>
    bool HasIndefiniteLength();
    /// <summary>
    /// Gets the encoded bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current encoded bytes.</returns>
    byte[] GetEncodedBytes();
    /// <summary>
    /// Gets the content bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content bytes.</returns>
    byte[] GetContentBytes();
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
    EmailCmsAsn1Node GetChildAt(int index);
    /// <summary>
    /// Gets the children.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current children.</returns>
    List<EmailCmsAsn1Node> GetChildren();
    /// <summary>
    /// Determines whether the tag is present or available.
    /// </summary>
    /// <param name = "expectedClass">The expected class used by the operation.</param>
    /// <param name = "expectedNumber">The expected number used by the operation.</param>
    /// <returns><c>true</c> when the tag is available; otherwise, <c>false</c>.</returns>
    bool HasTag(EmailCmsAsn1TagClass expectedClass, int expectedNumber);
}
