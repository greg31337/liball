namespace email;
/// <summary>
/// Specifies the supported query values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailQueryKind
{
    /// <summary>
    /// Represents the has field choice or state.
    /// </summary>
    HasField = 0,
    /// <summary>
    /// Represents the raw field contains choice or state.
    /// </summary>
    RawFieldContains = 1,
    /// <summary>
    /// Represents the raw field equals choice or state.
    /// </summary>
    RawFieldEquals = 2,
    /// <summary>
    /// Represents the decoded field contains choice or state.
    /// </summary>
    DecodedFieldContains = 3,
    /// <summary>
    /// Represents the decoded field equals choice or state.
    /// </summary>
    DecodedFieldEquals = 4,
    /// <summary>
    /// Represents the address equals choice or state.
    /// </summary>
    AddressEquals = 5,
    /// <summary>
    /// Represents the address domain equals choice or state.
    /// </summary>
    AddressDomainEquals = 6,
    /// <summary>
    /// Represents the date before choice or state.
    /// </summary>
    DateBefore = 7,
    /// <summary>
    /// Represents the date on or after choice or state.
    /// </summary>
    DateOnOrAfter = 8,
    /// <summary>
    /// Represents the date equals choice or state.
    /// </summary>
    DateEquals = 9,
    /// <summary>
    /// Represents the date between choice or state.
    /// </summary>
    DateBetween = 10,
    /// <summary>
    /// Represents the MIME type choice or state.
    /// </summary>
    MimeType = 11,
    /// <summary>
    /// Represents the MIME top level type choice or state.
    /// </summary>
    MimeTopLevelType = 12,
    /// <summary>
    /// Represents the has attachment choice or state.
    /// </summary>
    HasAttachment = 13,
    /// <summary>
    /// Represents the attachment filename contains choice or state.
    /// </summary>
    AttachmentFilenameContains = 14,
    /// <summary>
    /// Represents the attachment MIME type choice or state.
    /// </summary>
    AttachmentMimeType = 15,
    /// <summary>
    /// Represents the body contains choice or state.
    /// </summary>
    BodyContains = 16,
    /// <summary>
    /// Represents the has issue code choice or state.
    /// </summary>
    HasIssueCode = 17,
    /// <summary>
    /// Represents the has issue severity at least choice or state.
    /// </summary>
    HasIssueSeverityAtLeast = 18,
    /// <summary>
    /// Represents the security state choice or state.
    /// </summary>
    SecurityState = 19,
    /// <summary>
    /// Represents the and choice or state.
    /// </summary>
    And = 20,
    /// <summary>
    /// Represents the or choice or state.
    /// </summary>
    Or = 21,
    /// <summary>
    /// Represents the not choice or state.
    /// </summary>
    Not = 22
}
