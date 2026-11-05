using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCmsContentInfo"/>.
/// </summary>
public interface IEmailCmsContentInfoContract
{
    /// <summary>
    /// Gets the content type.
    /// </summary>
    /// <returns>The current content type.</returns>
    string GetContentType();
    /// <summary>
    /// Gets the content kind.
    /// </summary>
    /// <returns>The current content kind.</returns>
    EmailCmsContentKind GetContentKind();
    /// <summary>
    /// Gets the encoding kind.
    /// </summary>
    /// <returns>The current encoding kind.</returns>
    EmailCmsEncodingKind GetEncodingKind();
    /// <summary>
    /// Determines whether the operation or source value was ber encoded.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasBerEncoded();
    /// <summary>
    /// Gets the root node.
    /// </summary>
    /// <returns>The current root node.</returns>
    EmailCmsAsn1Node GetRootNode();
    /// <summary>
    /// Gets the content node.
    /// </summary>
    /// <returns>The current content node.</returns>
    EmailCmsAsn1Node GetContentNode();
    /// <summary>
    /// Gets the encoded content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current encoded content.</returns>
    byte[] GetEncodedContent();
    /// <summary>
    /// Determines whether the data content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the data content is available; otherwise, <c>false</c>.</returns>
    bool HasDataContent();
    /// <summary>
    /// Gets the data content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current data content.</returns>
    byte[]? GetDataContent();
    /// <summary>
    /// Gets the original bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original bytes.</returns>
    byte[] GetOriginalBytes();
}
