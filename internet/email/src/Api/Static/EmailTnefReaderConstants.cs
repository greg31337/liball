using System;
using System.Collections.Generic;
using System.IO;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailTnefReaderConstants
{
    /// <summary>
    /// The TNEF signature value exposed by this API.
    /// </summary>
    public const uint TnefSignature = 0x223E9F78;
    /// <summary>
    /// The TNEF version tag value exposed by this API.
    /// </summary>
    public const uint TnefVersionTag = 0x00089006;
    /// <summary>
    /// The oem code page tag value exposed by this API.
    /// </summary>
    public const uint OemCodePageTag = 0x00069007;
    /// <summary>
    /// The attachment rendering data tag value exposed by this API.
    /// </summary>
    public const uint AttachmentRenderingDataTag = 0x00069002;
    /// <summary>
    /// The message properties tag value exposed by this API.
    /// </summary>
    public const uint MessagePropertiesTag = 0x00069003;
    /// <summary>
    /// The recipient table tag value exposed by this API.
    /// </summary>
    public const uint RecipientTableTag = 0x00069004;
    /// <summary>
    /// The attachment properties tag value exposed by this API.
    /// </summary>
    public const uint AttachmentPropertiesTag = 0x00069005;
    /// <summary>
    /// The subject tag value exposed by this API.
    /// </summary>
    public const uint SubjectTag = 0x00018004;
    /// <summary>
    /// The body tag value exposed by this API.
    /// </summary>
    public const uint BodyTag = 0x0002800C;
    /// <summary>
    /// The message class tag value exposed by this API.
    /// </summary>
    public const uint MessageClassTag = 0x00078008;
    /// <summary>
    /// The attachment data tag value exposed by this API.
    /// </summary>
    public const uint AttachmentDataTag = 0x0006800F;
    /// <summary>
    /// The attachment title tag value exposed by this API.
    /// </summary>
    public const uint AttachmentTitleTag = 0x00018010;
    /// <summary>
    /// The attachment transport filename tag value exposed by this API.
    /// </summary>
    public const uint AttachmentTransportFilenameTag = 0x00069001;
}
