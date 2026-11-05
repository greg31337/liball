using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a TNEF container.
/// </summary>
public class EmailTnefContainer : IEmailTnefContainerContract
{
    EmailTnefHeader header;
    List<EmailTnefAttribute> attributes;
    byte[] source;
    int attachmentCount;
    /// <summary>
    /// Creates a new TNEF container.
    /// </summary>
    /// <param name = "header">The header used by the operation.</param>
    /// <param name = "attributes">The attributes used by the operation.</param>
    /// <param name = "source">The source byte[] to read or copy from.</param>
    /// <param name = "attachmentCount">The number of items or bytes to process.</param>
    public EmailTnefContainer(EmailTnefHeader header, List<EmailTnefAttribute> attributes, byte[] source, int attachmentCount)
    {
        this.header = header;
        this.attributes = new List<EmailTnefAttribute>(attributes);
        this.source = Copy(source);
        this.attachmentCount = attachmentCount;
    }

    /// <summary>
    /// Gets the header.
    /// </summary>
    /// <returns>The current header.</returns>
    public EmailTnefHeader GetHeader()
    {
        return header;
    }

    /// <summary>
    /// Gets the attribute count.
    /// </summary>
    /// <returns>The current attribute count.</returns>
    public int GetAttributeCount()
    {
        return attributes.Count;
    }

    /// <summary>
    /// Gets the attribute at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested attribute.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTnefAttribute GetAttributeAt(int index)
    {
        if (index < 0 || index >= attributes.Count)
            throw new ArgumentOutOfRangeException("index");
        return attributes[index];
    }

    /// <summary>
    /// Gets the attributes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current attributes.</returns>
    public List<EmailTnefAttribute> GetAttributes()
    {
        return new List<EmailTnefAttribute>(attributes);
    }

    /// <summary>
    /// Gets the attachment count.
    /// </summary>
    /// <returns>The current attachment count.</returns>
    public int GetAttachmentCount()
    {
        return attachmentCount;
    }

    /// <summary>
    /// Gets the source bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current source bytes.</returns>
    public byte[] GetSourceBytes()
    {
        return Copy(source);
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        return source.LongLength;
    }

    /// <summary>
    /// Determines whether the version is present or available.
    /// </summary>
    /// <returns><c>true</c> when the version is available; otherwise, <c>false</c>.</returns>
    public bool HasVersion()
    {
        return FindScalar(EmailTnefAttributeKind.TnefVersion) >= 0;
    }

    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    public uint GetVersion()
    {
        long value = FindScalar(EmailTnefAttributeKind.TnefVersion);
        return value < 0 ? 0 : (uint)value;
    }

    /// <summary>
    /// Determines whether the code page is present or available.
    /// </summary>
    /// <returns><c>true</c> when the code page is available; otherwise, <c>false</c>.</returns>
    public bool HasCodePage()
    {
        return FindScalar(EmailTnefAttributeKind.OemCodePage) >= 0;
    }

    /// <summary>
    /// Gets the code page.
    /// </summary>
    /// <returns>The current code page.</returns>
    public int GetCodePage()
    {
        long value = FindScalar(EmailTnefAttributeKind.OemCodePage);
        return value < 0 ? 0 : ToInt32Bits(value);
    }

    /// <summary>
    /// Determines whether the sub code page is present or available.
    /// </summary>
    /// <returns><c>true</c> when the sub code page is available; otherwise, <c>false</c>.</returns>
    public bool HasSubCodePage()
    {
        EmailTnefAttribute? attribute = Find(EmailTnefAttributeKind.OemCodePage);
        return attribute != null && attribute.GetData().Length >= 8;
    }

    /// <summary>
    /// Gets the sub code page.
    /// </summary>
    /// <returns>The current sub code page.</returns>
    public int GetSubCodePage()
    {
        EmailTnefAttribute? attribute = Find(EmailTnefAttributeKind.OemCodePage);
        if (attribute == null)
            return 0;
        byte[] value = attribute.GetData();
        if (value.Length < 8)
            return 0;
        return ToInt32Bits(ReadUInt32(value, 4));
    }

    EmailTnefAttribute? Find(EmailTnefAttributeKind kind)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            if (attributes[index].GetKind() == kind && attributes[index].IsComplete())
                return attributes[index];
            index++;
        }

        return null;
    }

    long FindScalar(EmailTnefAttributeKind kind)
    {
        EmailTnefAttribute? attribute = Find(kind);
        if (attribute == null)
            return -1;
        byte[] value = attribute.GetData();
        if (value.Length < 4)
            return -1;
        return ReadUInt32(value, 0);
    }

    static uint ReadUInt32(byte[] value, int offset)
    {
        return (uint)value[offset] | (uint)value[offset + 1] << 8 | (uint)value[offset + 2] << 16 | (uint)value[offset + 3] << 24;
    }

    static int ToInt32Bits(long value)
    {
        return value <= int.MaxValue ? (int)value : (int)(value - 4294967296L);
    }

    static byte[] Copy(byte[] value)
    {
        byte[] result = new byte[value.Length];
        value.CopyTo(result, 0);
        return result;
    }
}
