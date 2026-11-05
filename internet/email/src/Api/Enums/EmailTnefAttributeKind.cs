using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported TNEF attribute values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTnefAttributeKind
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the TNEF version choice or state.
    /// </summary>
    TnefVersion = 1,
    /// <summary>
    /// Represents the oem code page choice or state.
    /// </summary>
    OemCodePage = 2,
    /// <summary>
    /// Represents the attachment rendering data choice or state.
    /// </summary>
    AttachmentRenderingData = 3,
    /// <summary>
    /// Represents the message properties choice or state.
    /// </summary>
    MessageProperties = 4,
    /// <summary>
    /// Represents the recipient table choice or state.
    /// </summary>
    RecipientTable = 5,
    /// <summary>
    /// Represents the attachment properties choice or state.
    /// </summary>
    AttachmentProperties = 6,
    /// <summary>
    /// Represents the subject choice or state.
    /// </summary>
    Subject = 7,
    /// <summary>
    /// Represents the body choice or state.
    /// </summary>
    Body = 8,
    /// <summary>
    /// Represents the message class choice or state.
    /// </summary>
    MessageClass = 9,
    /// <summary>
    /// Represents the attachment data choice or state.
    /// </summary>
    AttachmentData = 10,
    /// <summary>
    /// Represents the attachment title choice or state.
    /// </summary>
    AttachmentTitle = 11,
    /// <summary>
    /// Represents the attachment transport filename choice or state.
    /// </summary>
    AttachmentTransportFilename = 12
}
