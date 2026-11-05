using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a cms ASN.1 node.
/// </summary>
public class EmailCmsAsn1Node : IEmailCmsAsn1NodeContract
{
    EmailCmsAsn1TagClass tagClass;
    int tagNumber;
    bool constructed;
    bool indefiniteLength;
    byte[] encodedBytes;
    int encodedOffset;
    int encodedLength;
    byte[] contentBytes;
    int contentOffset;
    int contentLength;
    List<EmailCmsAsn1Node> children;
    /// <summary>
    /// Creates a CMS ASN.1 node that references ranges in a shared byte array.
    /// </summary>
    /// <param name = "tagClass">The tag class used by the operation.</param>
    /// <param name = "tagNumber">The tag number used by the operation.</param>
    /// <param name = "constructed">Whether to enable the constructed behavior.</param>
    /// <param name = "indefiniteLength">Whether to enable the indefinite length behavior.</param>
    /// <param name = "encodedBytes">The source bytes or data to process.</param>
    /// <param name = "contentBytes">The source bytes or data to process.</param>
    /// <param name = "children">The children used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailCmsAsn1Node(EmailCmsAsn1TagClass tagClass, int tagNumber, bool constructed, bool indefiniteLength, byte[] encodedBytes, byte[] contentBytes, List<EmailCmsAsn1Node> children)
    {
        if (tagClass < EmailCmsAsn1TagClass.Universal || tagClass > EmailCmsAsn1TagClass.Private)
            throw new ArgumentOutOfRangeException("tagClass");
        if (tagNumber < 0)
            throw new ArgumentOutOfRangeException("tagNumber");
        if (encodedBytes == null || contentBytes == null || children == null)
            throw new ArgumentNullException("encodedBytes");
        if (!constructed && children.Count != 0)
            throw new ArgumentException("Primitive ASN.1 nodes cannot have child nodes.", "children");
        if (!constructed && indefiniteLength)
            throw new ArgumentException("Primitive ASN.1 nodes cannot use indefinite length.", "indefiniteLength");
        int index = 0;
        while (index < children.Count)
        {
            if (children[index] == null)
                throw new ArgumentException("ASN.1 child collections cannot contain null entries.", "children");
            index++;
        }

        this.tagClass = tagClass;
        this.tagNumber = tagNumber;
        this.constructed = constructed;
        this.indefiniteLength = indefiniteLength;
        this.encodedBytes = CopyWithValue(encodedBytes);
        encodedOffset = 0;
        encodedLength = encodedBytes.Length;
        this.contentBytes = CopyWithValue(contentBytes);
        contentOffset = 0;
        contentLength = contentBytes.Length;
        this.children = new List<EmailCmsAsn1Node>(children);
    }

    internal static EmailCmsAsn1Node CreateImplementationWithTagClassAndTagNumberAndConstructedAndIndefiniteLengthAndEncodedBytesAndContentBytesAndChildren(EmailCmsAsn1TagClass tagClass, int tagNumber, bool constructed, bool indefiniteLength, byte[] encodedBytes, byte[] contentBytes, List<EmailCmsAsn1Node> children)
    {
        return new EmailCmsAsn1Node(tagClass, tagNumber, constructed, indefiniteLength, encodedBytes, contentBytes, children);
    }

    internal static EmailCmsAsn1Node CreateImplementationWithTagClassAndTagNumberAndConstructedAndIndefiniteLengthAndBytesAndEncodedOffsetAndEncodedLengthAndContentOffsetAndContentLengthAndChildren(EmailCmsAsn1TagClass tagClass, int tagNumber, bool constructed, bool indefiniteLength, byte[] bytes, int encodedOffset, int encodedLength, int contentOffset, int contentLength, List<EmailCmsAsn1Node> children)
    {
        if (tagClass < EmailCmsAsn1TagClass.Universal || tagClass > EmailCmsAsn1TagClass.Private)
            throw new ArgumentOutOfRangeException("tagClass");
        if (tagNumber < 0)
            throw new ArgumentOutOfRangeException("tagNumber");
        if (bytes == null || children == null)
            throw new ArgumentNullException("bytes");
        if (encodedOffset < 0 || encodedLength < 0 || encodedOffset > bytes.Length - encodedLength)
            throw new ArgumentOutOfRangeException("encodedOffset");
        if (contentOffset < 0 || contentLength < 0 || contentOffset > bytes.Length - contentLength)
            throw new ArgumentOutOfRangeException("contentOffset");
        if (!constructed && children.Count != 0)
            throw new ArgumentException("Primitive ASN.1 nodes cannot have child nodes.", "children");
        if (!constructed && indefiniteLength)
            throw new ArgumentException("Primitive ASN.1 nodes cannot use indefinite length.", "indefiniteLength");
        int index = 0;
        while (index < children.Count)
        {
            if (children[index] == null)
                throw new ArgumentException("ASN.1 child collections cannot contain null entries.", "children");
            index++;
        }

        EmailCmsAsn1Node value = new EmailCmsAsn1Node(tagClass, tagNumber, constructed, indefiniteLength, CopyWithValueAndOffsetAndLength(bytes, encodedOffset, encodedLength), CopyWithValueAndOffsetAndLength(bytes, contentOffset, contentLength), children);
        value.encodedBytes = bytes;
        value.encodedOffset = encodedOffset;
        value.encodedLength = encodedLength;
        value.contentBytes = bytes;
        value.contentOffset = contentOffset;
        value.contentLength = contentLength;
        return value;
    }

    /// <summary>
    /// Gets the tag class.
    /// </summary>
    /// <returns>The current tag class.</returns>
    public EmailCmsAsn1TagClass GetTagClass()
    {
        return tagClass;
    }

    /// <summary>
    /// Gets the tag number.
    /// </summary>
    /// <returns>The current tag number.</returns>
    public int GetTagNumber()
    {
        return tagNumber;
    }

    /// <summary>
    /// Determines whether the current value or state is constructed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is constructed; otherwise, <c>false</c>.</returns>
    public bool IsConstructed()
    {
        return constructed;
    }

    /// <summary>
    /// Determines whether the indefinite length is present or available.
    /// </summary>
    /// <returns><c>true</c> when the indefinite length is available; otherwise, <c>false</c>.</returns>
    public bool HasIndefiniteLength()
    {
        return indefiniteLength;
    }

    /// <summary>
    /// Gets the encoded bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current encoded bytes.</returns>
    public byte[] GetEncodedBytes()
    {
        return CopyWithValueAndOffsetAndLength(encodedBytes, encodedOffset, encodedLength);
    }

    /// <summary>
    /// Gets the content bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content bytes.</returns>
    public byte[] GetContentBytes()
    {
        return CopyWithValueAndOffsetAndLength(contentBytes, contentOffset, contentLength);
    }

    /// <summary>
    /// Gets the child count.
    /// </summary>
    /// <returns>The current child count.</returns>
    public int GetChildCount()
    {
        return children.Count;
    }

    /// <summary>
    /// Gets the child at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested child.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailCmsAsn1Node GetChildAt(int index)
    {
        if (index < 0 || index >= children.Count)
            throw new ArgumentOutOfRangeException("index");
        return children[index];
    }

    /// <summary>
    /// Gets the children.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current children.</returns>
    public List<EmailCmsAsn1Node> GetChildren()
    {
        return new List<EmailCmsAsn1Node>(children);
    }

    /// <summary>
    /// Determines whether the tag is present or available.
    /// </summary>
    /// <param name = "expectedClass">The expected class used by the operation.</param>
    /// <param name = "expectedNumber">The expected number used by the operation.</param>
    /// <returns><c>true</c> when the tag is available; otherwise, <c>false</c>.</returns>
    public bool HasTag(EmailCmsAsn1TagClass expectedClass, int expectedNumber)
    {
        return tagClass == expectedClass && tagNumber == expectedNumber;
    }

    static byte[] CopyWithValue(byte[] value)
    {
        return CopyWithValueAndOffsetAndLength(value, 0, value.Length);
    }

    static byte[] CopyWithValueAndOffsetAndLength(byte[] value, int offset, int length)
    {
        byte[] copy = new byte[length];
        new ReadOnlySpan<byte>(value, offset, length).CopyTo(copy);
        return copy;
    }
}
