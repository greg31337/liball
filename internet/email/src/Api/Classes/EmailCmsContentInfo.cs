using System;

namespace email;
/// <summary>
/// Represents a CMS content info.
/// </summary>
/// <remarks>
/// Callers should observe the ownership and lifetime methods when the instance retains streams, temporary storage, or source buffers.
/// </remarks>
public class EmailCmsContentInfo : IEmailCmsContentInfoContract
{
    string contentType;
    EmailCmsContentKind contentKind;
    EmailCmsEncodingKind encodingKind;
    EmailCmsAsn1Node rootNode;
    EmailCmsAsn1Node contentNode;
    byte[]? dataContent;
    byte[] originalBytes;
    /// <summary>
    /// Creates a new CMS content info.
    /// </summary>
    /// <param name = "contentType">The content type used by the operation.</param>
    /// <param name = "contentKind">The content kind used by the operation.</param>
    /// <param name = "encodingKind">The encoding kind used by the operation.</param>
    /// <param name = "rootNode">The root node used by the operation.</param>
    /// <param name = "contentNode">The content node used by the operation.</param>
    /// <param name = "dataContent">The data content used by the operation.</param>
    /// <param name = "originalBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailCmsContentInfo(string contentType, EmailCmsContentKind contentKind, EmailCmsEncodingKind encodingKind, EmailCmsAsn1Node rootNode, EmailCmsAsn1Node contentNode, byte[]? dataContent, byte[] originalBytes)
    {
        if (contentType == null || contentType.Length == 0)
            throw new ArgumentException("A CMS content type object identifier is required.", "contentType");
        if (contentKind < EmailCmsContentKind.Unknown || contentKind > EmailCmsContentKind.AuthEnvelopedData)
            throw new ArgumentOutOfRangeException("contentKind");
        if (encodingKind < EmailCmsEncodingKind.Der || encodingKind > EmailCmsEncodingKind.Ber)
            throw new ArgumentOutOfRangeException("encodingKind");
        if (rootNode == null || contentNode == null || originalBytes == null)
            throw new ArgumentNullException("rootNode");
        this.contentType = contentType;
        this.contentKind = contentKind;
        this.encodingKind = encodingKind;
        this.rootNode = rootNode;
        this.contentNode = contentNode;
        this.dataContent = dataContent == null ? null : Copy(dataContent);
        this.originalBytes = Copy(originalBytes);
    }

    /// <summary>
    /// Gets the content type.
    /// </summary>
    /// <returns>The current content type.</returns>
    public string GetContentType()
    {
        return contentType;
    }

    /// <summary>
    /// Gets the content kind.
    /// </summary>
    /// <returns>The current content kind.</returns>
    public EmailCmsContentKind GetContentKind()
    {
        return contentKind;
    }

    /// <summary>
    /// Gets the encoding kind.
    /// </summary>
    /// <returns>The current encoding kind.</returns>
    public EmailCmsEncodingKind GetEncodingKind()
    {
        return encodingKind;
    }

    /// <summary>
    /// Determines whether the operation or source value was ber encoded.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasBerEncoded()
    {
        return encodingKind == EmailCmsEncodingKind.Ber;
    }

    /// <summary>
    /// Gets the root node.
    /// </summary>
    /// <returns>The current root node.</returns>
    public EmailCmsAsn1Node GetRootNode()
    {
        return rootNode;
    }

    /// <summary>
    /// Gets the content node.
    /// </summary>
    /// <returns>The current content node.</returns>
    public EmailCmsAsn1Node GetContentNode()
    {
        return contentNode;
    }

    /// <summary>
    /// Gets the encoded content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current encoded content.</returns>
    public byte[] GetEncodedContent()
    {
        return contentNode.GetEncodedBytes();
    }

    /// <summary>
    /// Determines whether the data content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the data content is available; otherwise, <c>false</c>.</returns>
    public bool HasDataContent()
    {
        return dataContent != null;
    }

    /// <summary>
    /// Gets the data content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current data content.</returns>
    public byte[]? GetDataContent()
    {
        return dataContent == null ? null : Copy(dataContent);
    }

    /// <summary>
    /// Gets the original bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original bytes.</returns>
    public byte[] GetOriginalBytes()
    {
        return Copy(originalBytes);
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
