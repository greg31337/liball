using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailCmsAsn1Node"/>.
/// </summary>
public class EmailCmsAsn1NodeOperations : IEmailCmsAsn1NodeOperationsContract
{
    /// <summary>Creates a CMS ASN.1 node from independent encoded and content byte arrays.</summary>
    /// <param name = "tagClass">The ASN.1 tag class.</param>
    /// <param name = "tagNumber">The ASN.1 tag number.</param>
    /// <param name = "constructed">Whether the node is constructed.</param>
    /// <param name = "indefiniteLength">Whether the node uses indefinite length.</param>
    /// <param name = "encodedBytes">The complete encoded bytes.</param>
    /// <param name = "contentBytes">The content bytes.</param>
    /// <param name = "children">The child nodes.</param>
    /// <returns>The configured ASN.1 node.</returns>
    public EmailCmsAsn1Node CreateWithTagClassAndTagNumberAndConstructedAndIndefiniteLengthAndEncodedBytesAndContentBytesAndChildren(EmailCmsAsn1TagClass tagClass, int tagNumber, bool constructed, bool indefiniteLength, byte[] encodedBytes, byte[] contentBytes, List<EmailCmsAsn1Node> children)
    {
        return EmailCmsAsn1Node.CreateImplementationWithTagClassAndTagNumberAndConstructedAndIndefiniteLengthAndEncodedBytesAndContentBytesAndChildren(tagClass, tagNumber, constructed, indefiniteLength, encodedBytes, contentBytes, children);
    }

    /// <summary>
    /// Creates a new cms ASN.1 node.
    /// </summary>
    /// <param name = "tagClass">The tag class used by the operation.</param>
    /// <param name = "tagNumber">The tag number used by the operation.</param>
    /// <param name = "constructed">Whether to enable the constructed behavior.</param>
    /// <param name = "indefiniteLength">Whether to enable the indefinite length behavior.</param>
    /// <param name = "bytes">The source bytes or data to process.</param>
    /// <param name = "encodedOffset">The zero-based encoded offset.</param>
    /// <param name = "encodedLength">The encoded length used by the operation.</param>
    /// <param name = "contentOffset">The zero-based content offset.</param>
    /// <param name = "contentLength">The content length used by the operation.</param>
    /// <param name = "children">The children used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <returns>The configured ASN.1 node.</returns>
    public EmailCmsAsn1Node CreateWithTagClassAndTagNumberAndConstructedAndIndefiniteLengthAndBytesAndEncodedOffsetAndEncodedLengthAndContentOffsetAndContentLengthAndChildren(EmailCmsAsn1TagClass tagClass, int tagNumber, bool constructed, bool indefiniteLength, byte[] bytes, int encodedOffset, int encodedLength, int contentOffset, int contentLength, List<EmailCmsAsn1Node> children)
    {
        return EmailCmsAsn1Node.CreateImplementationWithTagClassAndTagNumberAndConstructedAndIndefiniteLengthAndBytesAndEncodedOffsetAndEncodedLengthAndContentOffsetAndContentLengthAndChildren(tagClass, tagNumber, constructed, indefiniteLength, bytes, encodedOffset, encodedLength, contentOffset, contentLength, children);
    }
}
