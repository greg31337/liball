using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a TNEF attribute.
/// </summary>
public class EmailTnefAttribute : IEmailTnefAttributeContract
{
    int index;
    int rawLevel;
    uint tag;
    long declaredLength;
    byte[] data;
    int checksum;
    int calculatedChecksum;
    bool complete;
    int attachmentIndex;
    EmailRange sourceRange;
    EmailRange headerRange;
    EmailRange dataRange;
    EmailRange? checksumRange;
    /// <summary>
    /// Creates a new TNEF attribute.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <param name = "rawLevel">The raw level used by the operation.</param>
    /// <param name = "tag">The tag used by the operation.</param>
    /// <param name = "declaredLength">The declared length used by the operation.</param>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "checksum">The checksum used by the operation.</param>
    /// <param name = "calculatedChecksum">The calculated checksum used by the operation.</param>
    /// <param name = "complete">Whether to enable the complete behavior.</param>
    /// <param name = "attachmentIndex">The zero-based attachment index.</param>
    /// <param name = "sourceRange">The source range used by the operation.</param>
    /// <param name = "headerRange">The header range used by the operation.</param>
    /// <param name = "dataRange">The data range used by the operation.</param>
    /// <param name = "checksumRange">The checksum range used by the operation.</param>
    public EmailTnefAttribute(int index, int rawLevel, uint tag, long declaredLength, byte[] data, int checksum, int calculatedChecksum, bool complete, int attachmentIndex, EmailRange sourceRange, EmailRange headerRange, EmailRange dataRange, EmailRange? checksumRange)
    {
        this.index = index;
        this.rawLevel = rawLevel;
        this.tag = tag;
        this.declaredLength = declaredLength;
        this.data = Copy(data);
        this.checksum = checksum;
        this.calculatedChecksum = calculatedChecksum;
        this.complete = complete;
        this.attachmentIndex = attachmentIndex;
        this.sourceRange = sourceRange;
        this.headerRange = headerRange;
        this.dataRange = dataRange;
        this.checksumRange = checksumRange;
    }

    /// <summary>
    /// Gets the index.
    /// </summary>
    /// <returns>The current index.</returns>
    public int GetIndex()
    {
        return index;
    }

    /// <summary>
    /// Gets the raw level.
    /// </summary>
    /// <returns>The current raw level.</returns>
    public int GetRawLevel()
    {
        return rawLevel;
    }

    /// <summary>
    /// Gets the level.
    /// </summary>
    /// <returns>The current level.</returns>
    public EmailTnefAttributeLevel GetLevel()
    {
        if (rawLevel == 1)
            return EmailTnefAttributeLevel.Message;
        if (rawLevel == 2)
            return EmailTnefAttributeLevel.Attachment;
        return EmailTnefAttributeLevel.Unknown;
    }

    /// <summary>
    /// Gets the tag.
    /// </summary>
    /// <returns>The current tag.</returns>
    public uint GetTag()
    {
        return tag;
    }

    /// <summary>
    /// Gets the attribute ID.
    /// </summary>
    /// <returns>The current attribute ID.</returns>
    public int GetAttributeId()
    {
        return (int)(tag & 65535);
    }

    /// <summary>
    /// Gets the type code.
    /// </summary>
    /// <returns>The current type code.</returns>
    public int GetTypeCode()
    {
        return (int)(tag >> 16);
    }

    /// <summary>
    /// Gets the attribute type.
    /// </summary>
    /// <returns>The current attribute type.</returns>
    public EmailTnefAttributeType GetAttributeType()
    {
        int value = GetTypeCode();
        if (value >= 0 && value <= 8)
            return (EmailTnefAttributeType)value;
        return EmailTnefAttributeType.Unknown;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailTnefAttributeKind GetKind()
    {
        if (tag == EmailTnefReaderConstants.TnefVersionTag)
            return EmailTnefAttributeKind.TnefVersion;
        if (tag == EmailTnefReaderConstants.OemCodePageTag)
            return EmailTnefAttributeKind.OemCodePage;
        if (tag == EmailTnefReaderConstants.AttachmentRenderingDataTag)
            return EmailTnefAttributeKind.AttachmentRenderingData;
        if (tag == EmailTnefReaderConstants.MessagePropertiesTag)
            return EmailTnefAttributeKind.MessageProperties;
        if (tag == EmailTnefReaderConstants.RecipientTableTag)
            return EmailTnefAttributeKind.RecipientTable;
        if (tag == EmailTnefReaderConstants.AttachmentPropertiesTag)
            return EmailTnefAttributeKind.AttachmentProperties;
        if (tag == EmailTnefReaderConstants.SubjectTag)
            return EmailTnefAttributeKind.Subject;
        if (tag == EmailTnefReaderConstants.BodyTag)
            return EmailTnefAttributeKind.Body;
        if (tag == EmailTnefReaderConstants.MessageClassTag)
            return EmailTnefAttributeKind.MessageClass;
        if (tag == EmailTnefReaderConstants.AttachmentDataTag)
            return EmailTnefAttributeKind.AttachmentData;
        if (tag == EmailTnefReaderConstants.AttachmentTitleTag)
            return EmailTnefAttributeKind.AttachmentTitle;
        if (tag == EmailTnefReaderConstants.AttachmentTransportFilenameTag)
            return EmailTnefAttributeKind.AttachmentTransportFilename;
        return EmailTnefAttributeKind.Unknown;
    }

    /// <summary>
    /// Determines whether the current value or state is known.
    /// </summary>
    /// <returns><c>true</c> when the value or state is known; otherwise, <c>false</c>.</returns>
    public bool IsKnown()
    {
        return GetKind() != EmailTnefAttributeKind.Unknown;
    }

    /// <summary>
    /// Gets the declared length.
    /// </summary>
    /// <returns>The current declared length.</returns>
    public long GetDeclaredLength()
    {
        return declaredLength;
    }

    /// <summary>
    /// Gets the retained length.
    /// </summary>
    /// <returns>The current retained length.</returns>
    public long GetRetainedLength()
    {
        return data.LongLength;
    }

    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current data.</returns>
    public byte[] GetData()
    {
        return Copy(data);
    }

    /// <summary>
    /// Determines whether the checksum is present or available.
    /// </summary>
    /// <returns><c>true</c> when the checksum is available; otherwise, <c>false</c>.</returns>
    public bool HasChecksum()
    {
        return checksum >= 0;
    }

    /// <summary>
    /// Gets the checksum.
    /// </summary>
    /// <returns>The current checksum.</returns>
    public int GetChecksum()
    {
        return checksum;
    }

    /// <summary>
    /// Gets the calculated checksum.
    /// </summary>
    /// <returns>The current calculated checksum.</returns>
    public int GetCalculatedChecksum()
    {
        return calculatedChecksum;
    }

    /// <summary>
    /// Determines whether the current value or state is checksum valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is checksum valid; otherwise, <c>false</c>.</returns>
    public bool IsChecksumValid()
    {
        return checksum >= 0 && checksum == calculatedChecksum;
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return complete;
    }

    /// <summary>
    /// Determines whether the attachment index is present or available.
    /// </summary>
    /// <returns><c>true</c> when the attachment index is available; otherwise, <c>false</c>.</returns>
    public bool HasAttachmentIndex()
    {
        return attachmentIndex >= 0;
    }

    /// <summary>
    /// Gets the attachment index.
    /// </summary>
    /// <returns>The current attachment index.</returns>
    public int GetAttachmentIndex()
    {
        return attachmentIndex;
    }

    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    public EmailRange GetSourceRange()
    {
        return sourceRange;
    }

    /// <summary>
    /// Gets the header range.
    /// </summary>
    /// <returns>The current header range.</returns>
    public EmailRange GetHeaderRange()
    {
        return headerRange;
    }

    /// <summary>
    /// Gets the data range.
    /// </summary>
    /// <returns>The current data range.</returns>
    public EmailRange GetDataRange()
    {
        return dataRange;
    }

    /// <summary>
    /// Determines whether the checksum range is present or available.
    /// </summary>
    /// <returns><c>true</c> when the checksum range is available; otherwise, <c>false</c>.</returns>
    public bool HasChecksumRange()
    {
        return checksumRange != null;
    }

    /// <summary>
    /// Gets the checksum range.
    /// </summary>
    /// <returns>The current checksum range.</returns>
    public EmailRange? GetChecksumRange()
    {
        return checksumRange;
    }

    static byte[] Copy(byte[] source)
    {
        byte[] result = new byte[source.Length];
        source.CopyTo(result, 0);
        return result;
    }
}
