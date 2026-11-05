namespace email;
/// <summary>
/// Specifies the supported issue code values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailIssueCode
{
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Represents the general invalid input choice or state.
    /// </summary>
    GeneralInvalidInput = 1000,
    /// <summary>
    /// Represents the limit exceeded choice or state.
    /// </summary>
    LimitExceeded = 1001,
    /// <summary>
    /// Represents the operation cancelled choice or state.
    /// </summary>
    OperationCancelled = 1002,
    /// <summary>
    /// Represents the recovery applied choice or state.
    /// </summary>
    RecoveryApplied = 1003,
    /// <summary>
    /// Represents the transfer encoding malformed choice or state.
    /// </summary>
    TransferEncodingMalformed = 2000,
    /// <summary>
    /// Represents the charset unsupported choice or state.
    /// </summary>
    CharsetUnsupported = 2001,
    /// <summary>
    /// Represents the base64 invalid character choice or state.
    /// </summary>
    Base64InvalidCharacter = 2002,
    /// <summary>
    /// Represents the base64 invalid padding choice or state.
    /// </summary>
    Base64InvalidPadding = 2003,
    /// <summary>
    /// Represents the base64 truncated quantum choice or state.
    /// </summary>
    Base64TruncatedQuantum = 2004,
    /// <summary>
    /// Represents the base64 data after padding choice or state.
    /// </summary>
    Base64DataAfterPadding = 2005,
    /// <summary>
    /// Represents the base64 non canonical whitespace choice or state.
    /// </summary>
    Base64NonCanonicalWhitespace = 2006,
    /// <summary>
    /// Represents the base64 non zero padding bits choice or state.
    /// </summary>
    Base64NonZeroPaddingBits = 2007,
    /// <summary>
    /// Represents the decoded byte limit exceeded choice or state.
    /// </summary>
    DecodedByteLimitExceeded = 2008,
    /// <summary>
    /// Represents the quoted-printable invalid escape choice or state.
    /// </summary>
    QuotedPrintableInvalidEscape = 2009,
    /// <summary>
    /// Represents the quoted-printable truncated escape choice or state.
    /// </summary>
    QuotedPrintableTruncatedEscape = 2010,
    /// <summary>
    /// Represents the quoted-printable lowercase hex choice or state.
    /// </summary>
    QuotedPrintableLowercaseHex = 2011,
    /// <summary>
    /// Represents the quoted-printable non canonical soft break choice or state.
    /// </summary>
    QuotedPrintableNonCanonicalSoftBreak = 2012,
    /// <summary>
    /// Represents the transfer encoding high bit byte choice or state.
    /// </summary>
    TransferEncodingHighBitByte = 2013,
    /// <summary>
    /// Represents the transfer encoding null byte choice or state.
    /// </summary>
    TransferEncodingNullByte = 2014,
    /// <summary>
    /// Represents the transfer encoding non canonical line break choice or state.
    /// </summary>
    TransferEncodingNonCanonicalLineBreak = 2015,
    /// <summary>
    /// Represents the transfer encoding line too long choice or state.
    /// </summary>
    TransferEncodingLineTooLong = 2016,
    /// <summary>
    /// Represents the transfer encoding byte limit exceeded choice or state.
    /// </summary>
    TransferEncodingByteLimitExceeded = 2017,
    /// <summary>
    /// Represents the uuencode header not found choice or state.
    /// </summary>
    UuencodeHeaderNotFound = 2018,
    /// <summary>
    /// Represents the uuencode invalid line choice or state.
    /// </summary>
    UuencodeInvalidLine = 2019,
    /// <summary>
    /// Represents the uuencode invalid character choice or state.
    /// </summary>
    UuencodeInvalidCharacter = 2020,
    /// <summary>
    /// Represents the uuencode truncated line choice or state.
    /// </summary>
    UuencodeTruncatedLine = 2021,
    /// <summary>
    /// Represents the uuencode missing termination choice or state.
    /// </summary>
    UuencodeMissingTermination = 2022,
    /// <summary>
    /// Represents the uuencode decoded byte limit exceeded choice or state.
    /// </summary>
    UuencodeDecodedByteLimitExceeded = 2023,
    /// <summary>
    /// Represents the encoded word malformed choice or state.
    /// </summary>
    EncodedWordMalformed = 2024,
    /// <summary>
    /// Represents the encoded word unsupported encoding choice or state.
    /// </summary>
    EncodedWordUnsupportedEncoding = 2025,
    /// <summary>
    /// Represents the encoded word invalid payload choice or state.
    /// </summary>
    EncodedWordInvalidPayload = 2026,
    /// <summary>
    /// Represents the encoded word context violation choice or state.
    /// </summary>
    EncodedWordContextViolation = 2027,
    /// <summary>
    /// Represents the encoded word separation violation choice or state.
    /// </summary>
    EncodedWordSeparationViolation = 2028,
    /// <summary>
    /// Represents the encoded word too long choice or state.
    /// </summary>
    EncodedWordTooLong = 2029,
    /// <summary>
    /// Represents the encoded word limit exceeded choice or state.
    /// </summary>
    EncodedWordLimitExceeded = 2030,
    /// <summary>
    /// Represents the encoded word decoded byte limit exceeded choice or state.
    /// </summary>
    EncodedWordDecodedByteLimitExceeded = 2031,
    /// <summary>
    /// Represents the percent encoding invalid escape choice or state.
    /// </summary>
    PercentEncodingInvalidEscape = 2032,
    /// <summary>
    /// Represents the percent encoding lowercase hex choice or state.
    /// </summary>
    PercentEncodingLowercaseHex = 2033,
    /// <summary>
    /// Represents the rfc 2231 malformed name choice or state.
    /// </summary>
    Rfc2231MalformedName = 2034,
    /// <summary>
    /// Represents the rfc 2231 malformed extended value choice or state.
    /// </summary>
    Rfc2231MalformedExtendedValue = 2035,
    /// <summary>
    /// Represents the rfc 2231 continuation gap choice or state.
    /// </summary>
    Rfc2231ContinuationGap = 2036,
    /// <summary>
    /// Represents the rfc 2231 duplicate choice or state.
    /// </summary>
    Rfc2231Duplicate = 2037,
    /// <summary>
    /// Represents the rfc 2231 conflicting forms choice or state.
    /// </summary>
    Rfc2231ConflictingForms = 2038,
    /// <summary>
    /// Represents the rfc 2231 segment limit exceeded choice or state.
    /// </summary>
    Rfc2231SegmentLimitExceeded = 2039,
    /// <summary>
    /// Represents the rfc 2231 decoded byte limit exceeded choice or state.
    /// </summary>
    Rfc2231DecodedByteLimitExceeded = 2040,
    /// <summary>
    /// Represents the charset invalid byte sequence choice or state.
    /// </summary>
    CharsetInvalidByteSequence = 2041,
    /// <summary>
    /// Represents the charset BOM mismatch choice or state.
    /// </summary>
    CharsetBomMismatch = 2042,
    /// <summary>
    /// Represents the charset character limit exceeded choice or state.
    /// </summary>
    CharsetCharacterLimitExceeded = 2043,
    /// <summary>
    /// Represents the charset name limit exceeded choice or state.
    /// </summary>
    CharsetNameLimitExceeded = 2044,
    /// <summary>
    /// Represents the quoted-printable invalid character choice or state.
    /// </summary>
    QuotedPrintableInvalidCharacter = 2045,
    /// <summary>
    /// Represents the transfer encoding unsupported choice or state.
    /// </summary>
    TransferEncodingUnsupported = 2046,
    /// <summary>
    /// Represents the header malformed choice or state.
    /// </summary>
    HeaderMalformed = 3000,
    /// <summary>
    /// Represents the structured value malformed choice or state.
    /// </summary>
    StructuredValueMalformed = 3001,
    /// <summary>
    /// Represents the header lexical malformed choice or state.
    /// </summary>
    HeaderLexicalMalformed = 3002,
    /// <summary>
    /// Represents the header lexical obsolete choice or state.
    /// </summary>
    HeaderLexicalObsolete = 3003,
    /// <summary>
    /// Represents the header lexical token limit exceeded choice or state.
    /// </summary>
    HeaderLexicalTokenLimitExceeded = 3004,
    /// <summary>
    /// Represents the header lexical token byte limit exceeded choice or state.
    /// </summary>
    HeaderLexicalTokenByteLimitExceeded = 3005,
    /// <summary>
    /// Represents the header lexical comment depth exceeded choice or state.
    /// </summary>
    HeaderLexicalCommentDepthExceeded = 3006,
    /// <summary>
    /// Represents the address malformed choice or state.
    /// </summary>
    AddressMalformed = 3007,
    /// <summary>
    /// Represents the address obsolete syntax choice or state.
    /// </summary>
    AddressObsoleteSyntax = 3008,
    /// <summary>
    /// Represents the address limit exceeded choice or state.
    /// </summary>
    AddressLimitExceeded = 3009,
    /// <summary>
    /// Represents the address formatting invalid choice or state.
    /// </summary>
    AddressFormattingInvalid = 3010,
    /// <summary>
    /// Represents the address injection rejected choice or state.
    /// </summary>
    AddressInjectionRejected = 3011,
    /// <summary>
    /// Represents the address internationalization required choice or state.
    /// </summary>
    AddressInternationalizationRequired = 3012,
    /// <summary>
    /// Represents the address output limit exceeded choice or state.
    /// </summary>
    AddressOutputLimitExceeded = 3013,
    /// <summary>
    /// Represents the date malformed choice or state.
    /// </summary>
    DateMalformed = 3014,
    /// <summary>
    /// Represents the date obsolete syntax choice or state.
    /// </summary>
    DateObsoleteSyntax = 3015,
    /// <summary>
    /// Represents the date unknown zone choice or state.
    /// </summary>
    DateUnknownZone = 3016,
    /// <summary>
    /// Represents the date semantic invalid choice or state.
    /// </summary>
    DateSemanticInvalid = 3017,
    /// <summary>
    /// Represents the date day of week mismatch choice or state.
    /// </summary>
    DateDayOfWeekMismatch = 3018,
    /// <summary>
    /// Represents the date input limit exceeded choice or state.
    /// </summary>
    DateInputLimitExceeded = 3019,
    /// <summary>
    /// Represents the date formatting invalid choice or state.
    /// </summary>
    DateFormattingInvalid = 3020,
    /// <summary>
    /// Represents the date output limit exceeded choice or state.
    /// </summary>
    DateOutputLimitExceeded = 3021,
    /// <summary>
    /// Represents the message ID malformed choice or state.
    /// </summary>
    MessageIdMalformed = 3022,
    /// <summary>
    /// Represents the message ID obsolete syntax choice or state.
    /// </summary>
    MessageIdObsoleteSyntax = 3023,
    /// <summary>
    /// Represents the message ID recovery applied choice or state.
    /// </summary>
    MessageIdRecoveryApplied = 3024,
    /// <summary>
    /// Represents the message ID limit exceeded choice or state.
    /// </summary>
    MessageIdLimitExceeded = 3025,
    /// <summary>
    /// Represents the message ID formatting invalid choice or state.
    /// </summary>
    MessageIdFormattingInvalid = 3026,
    /// <summary>
    /// Represents the message ID output limit exceeded choice or state.
    /// </summary>
    MessageIdOutputLimitExceeded = 3027,
    /// <summary>
    /// Represents the structured parameter malformed choice or state.
    /// </summary>
    StructuredParameterMalformed = 3028,
    /// <summary>
    /// Represents the structured parameter limit exceeded choice or state.
    /// </summary>
    StructuredParameterLimitExceeded = 3029,
    /// <summary>
    /// Represents the media type malformed choice or state.
    /// </summary>
    MediaTypeMalformed = 3030,
    /// <summary>
    /// Represents the media type default applied choice or state.
    /// </summary>
    MediaTypeDefaultApplied = 3031,
    /// <summary>
    /// Represents the disposition malformed choice or state.
    /// </summary>
    DispositionMalformed = 3032,
    /// <summary>
    /// Represents the content language malformed choice or state.
    /// </summary>
    ContentLanguageMalformed = 3033,
    /// <summary>
    /// Represents the received malformed choice or state.
    /// </summary>
    ReceivedMalformed = 3034,
    /// <summary>
    /// Represents the received date missing choice or state.
    /// </summary>
    ReceivedDateMissing = 3035,
    /// <summary>
    /// Represents the received date malformed choice or state.
    /// </summary>
    ReceivedDateMalformed = 3036,
    /// <summary>
    /// Represents the received limit exceeded choice or state.
    /// </summary>
    ReceivedLimitExceeded = 3037,
    /// <summary>
    /// Represents the keyword malformed choice or state.
    /// </summary>
    KeywordMalformed = 3038,
    /// <summary>
    /// Represents the metadata value malformed choice or state.
    /// </summary>
    MetadataValueMalformed = 3039,
    /// <summary>
    /// Represents the auto submitted malformed choice or state.
    /// </summary>
    AutoSubmittedMalformed = 3040,
    /// <summary>
    /// Represents the list management malformed choice or state.
    /// </summary>
    ListManagementMalformed = 3041,
    /// <summary>
    /// Represents the list unsubscribe post malformed choice or state.
    /// </summary>
    ListUnsubscribePostMalformed = 3042,
    /// <summary>
    /// Represents the resent field malformed choice or state.
    /// </summary>
    ResentFieldMalformed = 3043,
    /// <summary>
    /// Represents the metadata value limit exceeded choice or state.
    /// </summary>
    MetadataValueLimitExceeded = 3044,
    /// <summary>
    /// Represents the resent field obsolete choice or state.
    /// </summary>
    ResentFieldObsolete = 3045,
    /// <summary>
    /// Represents the authentication results malformed choice or state.
    /// </summary>
    AuthenticationResultsMalformed = 3046,
    /// <summary>
    /// Represents the received spf malformed choice or state.
    /// </summary>
    ReceivedSpfMalformed = 3047,
    /// <summary>
    /// Represents the authentication value limit exceeded choice or state.
    /// </summary>
    AuthenticationValueLimitExceeded = 3048,
    /// <summary>
    /// Represents the authentication formatting invalid choice or state.
    /// </summary>
    AuthenticationFormattingInvalid = 3049,
    /// <summary>
    /// Represents the authentication output limit exceeded choice or state.
    /// </summary>
    AuthenticationOutputLimitExceeded = 3050,
    /// <summary>
    /// Represents the signature tag malformed choice or state.
    /// </summary>
    SignatureTagMalformed = 3051,
    /// <summary>
    /// Represents the signature tag duplicate choice or state.
    /// </summary>
    SignatureTagDuplicate = 3052,
    /// <summary>
    /// Represents the DKIM signature malformed choice or state.
    /// </summary>
    DkimSignatureMalformed = 3053,
    /// <summary>
    /// Represents the ARC header malformed choice or state.
    /// </summary>
    ArcHeaderMalformed = 3054,
    /// <summary>
    /// Represents the signature value limit exceeded choice or state.
    /// </summary>
    SignatureValueLimitExceeded = 3055,
    /// <summary>
    /// Represents the signature formatting invalid choice or state.
    /// </summary>
    SignatureFormattingInvalid = 3056,
    /// <summary>
    /// Represents the signature output limit exceeded choice or state.
    /// </summary>
    SignatureOutputLimitExceeded = 3057,
    /// <summary>
    /// Represents the value formatting invalid choice or state.
    /// </summary>
    ValueFormattingInvalid = 3058,
    /// <summary>
    /// Represents the value output limit exceeded choice or state.
    /// </summary>
    ValueOutputLimitExceeded = 3059,
    /// <summary>
    /// Represents the header block incomplete choice or state.
    /// </summary>
    HeaderBlockIncomplete = 3060,
    /// <summary>
    /// Represents the header name malformed choice or state.
    /// </summary>
    HeaderNameMalformed = 3061,
    /// <summary>
    /// Represents the header continuation without field choice or state.
    /// </summary>
    HeaderContinuationWithoutField = 3062,
    /// <summary>
    /// Represents the header null byte choice or state.
    /// </summary>
    HeaderNullByte = 3063,
    /// <summary>
    /// Represents the header non canonical line ending choice or state.
    /// </summary>
    HeaderNonCanonicalLineEnding = 3064,
    /// <summary>
    /// Represents the header missing colon choice or state.
    /// </summary>
    HeaderMissingColon = 3065,
    /// <summary>
    /// Represents the header validation invalid name choice or state.
    /// </summary>
    HeaderValidationInvalidName = 3066,
    /// <summary>
    /// Represents the header validation invalid value choice or state.
    /// </summary>
    HeaderValidationInvalidValue = 3067,
    /// <summary>
    /// Represents the header validation line too long choice or state.
    /// </summary>
    HeaderValidationLineTooLong = 3068,
    /// <summary>
    /// Represents the header validation cardinality choice or state.
    /// </summary>
    HeaderValidationCardinality = 3069,
    /// <summary>
    /// Represents the header validation required field missing choice or state.
    /// </summary>
    HeaderValidationRequiredFieldMissing = 3070,
    /// <summary>
    /// Represents the header validation context choice or state.
    /// </summary>
    HeaderValidationContext = 3071,
    /// <summary>
    /// Represents the header validation resent block choice or state.
    /// </summary>
    HeaderValidationResentBlock = 3072,
    /// <summary>
    /// Represents the header validation smtp UTF-8 required choice or state.
    /// </summary>
    HeaderValidationSmtpUtf8Required = 3073,
    /// <summary>
    /// Represents the header validation invalid UTF-8 choice or state.
    /// </summary>
    HeaderValidationInvalidUtf8 = 3074,
    /// <summary>
    /// Represents the header validation MIME value choice or state.
    /// </summary>
    HeaderValidationMimeValue = 3075,
    /// <summary>
    /// Represents the header validation bcc policy choice or state.
    /// </summary>
    HeaderValidationBccPolicy = 3076,
    /// <summary>
    /// Represents the header line too long choice or state.
    /// </summary>
    HeaderLineTooLong = 3077,
    /// <summary>
    /// Represents the header duplicate singleton choice or state.
    /// </summary>
    HeaderDuplicateSingleton = 3078,
    /// <summary>
    /// Represents the MIME structure malformed choice or state.
    /// </summary>
    MimeStructureMalformed = 4000,
    /// <summary>
    /// Represents the message structure invalid choice or state.
    /// </summary>
    MessageStructureInvalid = 4001,
    /// <summary>
    /// Represents the partial message malformed choice or state.
    /// </summary>
    PartialMessageMalformed = 4002,
    /// <summary>
    /// Represents the external body malformed choice or state.
    /// </summary>
    ExternalBodyMalformed = 4003,
    /// <summary>
    /// Represents the MIME unexpected boundary choice or state.
    /// </summary>
    MimeUnexpectedBoundary = 4004,
    /// <summary>
    /// Represents the validation MIME boundary missing choice or state.
    /// </summary>
    ValidationMimeBoundaryMissing = 4005,
    /// <summary>
    /// Represents the validation MIME boundary invalid choice or state.
    /// </summary>
    ValidationMimeBoundaryInvalid = 4006,
    /// <summary>
    /// Represents the validation MIME version missing choice or state.
    /// </summary>
    ValidationMimeVersionMissing = 4007,
    /// <summary>
    /// Represents the validation transfer encoding unsupported choice or state.
    /// </summary>
    ValidationTransferEncodingUnsupported = 4008,
    /// <summary>
    /// Represents the validation container transfer encoding choice or state.
    /// </summary>
    ValidationContainerTransferEncoding = 4009,
    /// <summary>
    /// Represents the validation related start unresolved choice or state.
    /// </summary>
    ValidationRelatedStartUnresolved = 4010,
    /// <summary>
    /// Represents the validation duplicate content ID choice or state.
    /// </summary>
    ValidationDuplicateContentId = 4011,
    /// <summary>
    /// Represents the validation alternative structure choice or state.
    /// </summary>
    ValidationAlternativeStructure = 4012,
    /// <summary>
    /// Represents the validation partial message metadata choice or state.
    /// </summary>
    ValidationPartialMessageMetadata = 4013,
    /// <summary>
    /// Represents the validation external body metadata choice or state.
    /// </summary>
    ValidationExternalBodyMetadata = 4014,
    /// <summary>
    /// Represents the validation nested message body missing choice or state.
    /// </summary>
    ValidationNestedMessageBodyMissing = 4015,
    /// <summary>
    /// Represents the validation charset unsupported choice or state.
    /// </summary>
    ValidationCharsetUnsupported = 4016,
    /// <summary>
    /// Represents the validation security protocol missing choice or state.
    /// </summary>
    ValidationSecurityProtocolMissing = 4017,
    /// <summary>
    /// Represents the validation message body missing choice or state.
    /// </summary>
    ValidationMessageBodyMissing = 4018,
    /// <summary>
    /// Represents the partial message duplicate fragment choice or state.
    /// </summary>
    PartialMessageDuplicateFragment = 4019,
    /// <summary>
    /// Represents the partial message missing fragment choice or state.
    /// </summary>
    PartialMessageMissingFragment = 4020,
    /// <summary>
    /// Represents the partial message total mismatch choice or state.
    /// </summary>
    PartialMessageTotalMismatch = 4021,
    /// <summary>
    /// Represents the partial message fragment boundary invalid choice or state.
    /// </summary>
    PartialMessageFragmentBoundaryInvalid = 4022,
    /// <summary>
    /// Represents the partial message assembly malformed choice or state.
    /// </summary>
    PartialMessageAssemblyMalformed = 4023,
    /// <summary>
    /// Represents the partial message reparse failed choice or state.
    /// </summary>
    PartialMessageReparseFailed = 4024,
    /// <summary>
    /// Represents a conflict between MIME content headers and the existing entity shape.
    /// </summary>
    MimeMetadataConflict = 4025,
    /// <summary>
    /// Represents the write policy violation choice or state.
    /// </summary>
    WritePolicyViolation = 5000,
    /// <summary>
    /// Represents the validation failed choice or state.
    /// </summary>
    ValidationFailed = 5001,
    /// <summary>
    /// Represents the header write invalid choice or state.
    /// </summary>
    HeaderWriteInvalid = 5002,
    /// <summary>
    /// Represents the header write injection rejected choice or state.
    /// </summary>
    HeaderWriteInjectionRejected = 5003,
    /// <summary>
    /// Represents the header write cardinality violation choice or state.
    /// </summary>
    HeaderWriteCardinalityViolation = 5004,
    /// <summary>
    /// Represents the header write output limit exceeded choice or state.
    /// </summary>
    HeaderWriteOutputLimitExceeded = 5005,
    /// <summary>
    /// Represents the header write line length exceeded choice or state.
    /// </summary>
    HeaderWriteLineLengthExceeded = 5006,
    /// <summary>
    /// Represents content that cannot be guaranteed to be textual during text serialization.
    /// </summary>
    TextSerializationNotTextual = 5007,
    /// <summary>
    /// Represents content whose transfer encoding cannot be decoded during text serialization validation.
    /// </summary>
    TextSerializationTransferDecodeFailed = 5008,
    /// <summary>
    /// Represents the unsafe content choice or state.
    /// </summary>
    UnsafeContent = 6000,
    /// <summary>
    /// Represents the composition invalid choice or state.
    /// </summary>
    CompositionInvalid = 6001,
    /// <summary>
    /// Represents the media type extension mismatch choice or state.
    /// </summary>
    MediaTypeExtensionMismatch = 6002,
    /// <summary>
    /// Represents the media type magic mismatch choice or state.
    /// </summary>
    MediaTypeMagicMismatch = 6003,
    /// <summary>
    /// Represents the text charset fallback used choice or state.
    /// </summary>
    TextCharsetFallbackUsed = 6004,
    /// <summary>
    /// Represents the text newline normalized choice or state.
    /// </summary>
    TextNewlineNormalized = 6005,
    /// <summary>
    /// Represents the text unicode normalized choice or state.
    /// </summary>
    TextUnicodeNormalized = 6006,
    /// <summary>
    /// Represents the text control character choice or state.
    /// </summary>
    TextControlCharacter = 6007,
    /// <summary>
    /// Represents the text bidi control choice or state.
    /// </summary>
    TextBidiControl = 6008,
    /// <summary>
    /// Represents the HTML markup malformed choice or state.
    /// </summary>
    HtmlMarkupMalformed = 6009,
    /// <summary>
    /// Represents the HTML entity malformed choice or state.
    /// </summary>
    HtmlEntityMalformed = 6010,
    /// <summary>
    /// Represents the HTML sanitized choice or state.
    /// </summary>
    HtmlSanitized = 6011,
    /// <summary>
    /// Represents the HTML remote content blocked choice or state.
    /// </summary>
    HtmlRemoteContentBlocked = 6012,
    /// <summary>
    /// Represents the HTML unsafe url blocked choice or state.
    /// </summary>
    HtmlUnsafeUrlBlocked = 6013,
    /// <summary>
    /// Represents the HTML data URI extracted choice or state.
    /// </summary>
    HtmlDataUriExtracted = 6014,
    /// <summary>
    /// Represents the HTML data URI rejected choice or state.
    /// </summary>
    HtmlDataUriRejected = 6015,
    /// <summary>
    /// Represents the text quoted reply detected choice or state.
    /// </summary>
    TextQuotedReplyDetected = 6016,
    /// <summary>
    /// Represents the text signature detected choice or state.
    /// </summary>
    TextSignatureDetected = 6017,
    /// <summary>
    /// Represents the text search limit reached choice or state.
    /// </summary>
    TextSearchLimitReached = 6018,
    /// <summary>
    /// Represents the display body unavailable choice or state.
    /// </summary>
    DisplayBodyUnavailable = 6019,
    /// <summary>
    /// Represents the display related resource missing choice or state.
    /// </summary>
    DisplayRelatedResourceMissing = 6020,
    /// <summary>
    /// Represents the thread duplicate message ID choice or state.
    /// </summary>
    ThreadDuplicateMessageId = 6021,
    /// <summary>
    /// Represents the thread parent missing choice or state.
    /// </summary>
    ThreadParentMissing = 6022,
    /// <summary>
    /// Represents the thread cycle detected choice or state.
    /// </summary>
    ThreadCycleDetected = 6023,
    /// <summary>
    /// Represents the thread subject fallback used choice or state.
    /// </summary>
    ThreadSubjectFallbackUsed = 6024,
    /// <summary>
    /// Represents the specialized format malformed choice or state.
    /// </summary>
    SpecializedFormatMalformed = 7000,
    /// <summary>
    /// Represents the archive malformed choice or state.
    /// </summary>
    ArchiveMalformed = 7001,
    /// <summary>
    /// Represents the report structure malformed choice or state.
    /// </summary>
    ReportStructureMalformed = 7002,
    /// <summary>
    /// Represents the report type missing choice or state.
    /// </summary>
    ReportTypeMissing = 7003,
    /// <summary>
    /// Represents the report type mismatch choice or state.
    /// </summary>
    ReportTypeMismatch = 7004,
    /// <summary>
    /// Represents the report field block malformed choice or state.
    /// </summary>
    ReportFieldBlockMalformed = 7005,
    /// <summary>
    /// Represents the delivery status malformed choice or state.
    /// </summary>
    DeliveryStatusMalformed = 7006,
    /// <summary>
    /// Represents the delivery status required field missing choice or state.
    /// </summary>
    DeliveryStatusRequiredFieldMissing = 7007,
    /// <summary>
    /// Represents the delivery status action invalid choice or state.
    /// </summary>
    DeliveryStatusActionInvalid = 7008,
    /// <summary>
    /// Represents the delivery status code invalid choice or state.
    /// </summary>
    DeliveryStatusCodeInvalid = 7009,
    /// <summary>
    /// Represents the delivery status status mismatch choice or state.
    /// </summary>
    DeliveryStatusStatusMismatch = 7010,
    /// <summary>
    /// Represents the delivery status internationalization mismatch choice or state.
    /// </summary>
    DeliveryStatusInternationalizationMismatch = 7011,
    /// <summary>
    /// Represents the delivery status original content malformed choice or state.
    /// </summary>
    DeliveryStatusOriginalContentMalformed = 7012,
    /// <summary>
    /// Represents the disposition notification malformed choice or state.
    /// </summary>
    DispositionNotificationMalformed = 7013,
    /// <summary>
    /// Represents the disposition notification required field missing choice or state.
    /// </summary>
    DispositionNotificationRequiredFieldMissing = 7014,
    /// <summary>
    /// Represents the disposition notification value invalid choice or state.
    /// </summary>
    DispositionNotificationValueInvalid = 7015,
    /// <summary>
    /// Represents the disposition notification internationalization mismatch choice or state.
    /// </summary>
    DispositionNotificationInternationalizationMismatch = 7016,
    /// <summary>
    /// Represents the disposition notification original content malformed choice or state.
    /// </summary>
    DispositionNotificationOriginalContentMalformed = 7017,
    /// <summary>
    /// Represents the disposition notification request malformed choice or state.
    /// </summary>
    DispositionNotificationRequestMalformed = 7018,
    /// <summary>
    /// Represents the feedback report malformed choice or state.
    /// </summary>
    FeedbackReportMalformed = 7019,
    /// <summary>
    /// Represents the feedback report required field missing choice or state.
    /// </summary>
    FeedbackReportRequiredFieldMissing = 7020,
    /// <summary>
    /// Represents the feedback report value invalid choice or state.
    /// </summary>
    FeedbackReportValueInvalid = 7021,
    /// <summary>
    /// Represents the feedback report version unsupported choice or state.
    /// </summary>
    FeedbackReportVersionUnsupported = 7022,
    /// <summary>
    /// Represents the feedback report original content missing choice or state.
    /// </summary>
    FeedbackReportOriginalContentMissing = 7023,
    /// <summary>
    /// Represents the feedback report original content malformed choice or state.
    /// </summary>
    FeedbackReportOriginalContentMalformed = 7024,
    /// <summary>
    /// Represents the calendar malformed choice or state.
    /// </summary>
    CalendarMalformed = 7025,
    /// <summary>
    /// Represents the calendar required property missing choice or state.
    /// </summary>
    CalendarRequiredPropertyMissing = 7026,
    /// <summary>
    /// Represents the calendar property invalid choice or state.
    /// </summary>
    CalendarPropertyInvalid = 7027,
    /// <summary>
    /// Represents the calendar component mismatch choice or state.
    /// </summary>
    CalendarComponentMismatch = 7028,
    /// <summary>
    /// Represents the calendar date invalid choice or state.
    /// </summary>
    CalendarDateInvalid = 7029,
    /// <summary>
    /// Represents the calendar time zone invalid choice or state.
    /// </summary>
    CalendarTimeZoneInvalid = 7030,
    /// <summary>
    /// Represents the calendar method mismatch choice or state.
    /// </summary>
    CalendarMethodMismatch = 7031,
    /// <summary>
    /// Represents the calendar UTF-8 invalid choice or state.
    /// </summary>
    CalendarUtf8Invalid = 7032,
    /// <summary>
    /// Represents the calendar non canonical line ending choice or state.
    /// </summary>
    CalendarNonCanonicalLineEnding = 7033,
    /// <summary>
    /// Represents the calendar property duplicate choice or state.
    /// </summary>
    CalendarPropertyDuplicate = 7034,
    /// <summary>
    /// Represents the mbox envelope malformed choice or state.
    /// </summary>
    MboxEnvelopeMalformed = 7035,
    /// <summary>
    /// Represents the mbox message recovered choice or state.
    /// </summary>
    MboxMessageRecovered = 7036,
    /// <summary>
    /// Represents the mbox message malformed choice or state.
    /// </summary>
    MboxMessageMalformed = 7037,
    /// <summary>
    /// Represents the maildir name malformed choice or state.
    /// </summary>
    MaildirNameMalformed = 7038,
    /// <summary>
    /// Represents the maildir flags malformed choice or state.
    /// </summary>
    MaildirFlagsMalformed = 7039,
    /// <summary>
    /// Represents the maildir message malformed choice or state.
    /// </summary>
    MaildirMessageMalformed = 7040,
    /// <summary>
    /// Represents the TNEF signature invalid choice or state.
    /// </summary>
    TnefSignatureInvalid = 7041,
    /// <summary>
    /// Represents the TNEF header malformed choice or state.
    /// </summary>
    TnefHeaderMalformed = 7042,
    /// <summary>
    /// Represents the TNEF attribute malformed choice or state.
    /// </summary>
    TnefAttributeMalformed = 7043,
    /// <summary>
    /// Represents the TNEF checksum mismatch choice or state.
    /// </summary>
    TnefChecksumMismatch = 7044,
    /// <summary>
    /// Represents the TNEF structure malformed choice or state.
    /// </summary>
    TnefStructureMalformed = 7045,
    /// <summary>
    /// Represents the TNEF required attribute missing choice or state.
    /// </summary>
    TnefRequiredAttributeMissing = 7046,
    /// <summary>
    /// Represents the TNEF property malformed choice or state.
    /// </summary>
    TnefPropertyMalformed = 7047,
    /// <summary>
    /// Represents the TNEF recipient malformed choice or state.
    /// </summary>
    TnefRecipientMalformed = 7048,
    /// <summary>
    /// Represents the TNEF attachment malformed choice or state.
    /// </summary>
    TnefAttachmentMalformed = 7049,
    /// <summary>
    /// Represents the TNEF rtf malformed choice or state.
    /// </summary>
    TnefRtfMalformed = 7050,
    /// <summary>
    /// Represents the TNEF embedded message malformed choice or state.
    /// </summary>
    TnefEmbeddedMessageMalformed = 7051,
    /// <summary>
    /// Represents the TNEF charset unsupported choice or state.
    /// </summary>
    TnefCharsetUnsupported = 7052,
    /// <summary>
    /// Represents the TNEF conversion failed choice or state.
    /// </summary>
    TnefConversionFailed = 7053,
    /// <summary>
    /// Represents a collision that prevented a Maildir move or flag update.
    /// </summary>
    MaildirOperationCollision = 7058,
    /// <summary>
    /// Represents unsupported Maildir info data that could not be retained by a canonical flag update.
    /// </summary>
    MaildirUnknownInfoNotPreserved = 7059,
    /// <summary>
    /// Represents the TNEF conversion lossy choice or state.
    /// </summary>
    TnefConversionLossy = 7054,
    /// <summary>
    /// Represents the TNEF content type invalid choice or state.
    /// </summary>
    TnefContentTypeInvalid = 7055,
    /// <summary>
    /// Represents the TNEF content ID invalid choice or state.
    /// </summary>
    TnefContentIdInvalid = 7056,
    /// <summary>
    /// Represents the TNEF attachment skipped choice or state.
    /// </summary>
    TnefAttachmentSkipped = 7057,
    /// <summary>
    /// Represents Outlook compatibility recovery that was explicitly applied.
    /// </summary>
    OutlookCompatibilityRecovery = 7060,
    /// <summary>
    /// Represents Exchange compatibility recovery that was explicitly applied.
    /// </summary>
    ExchangeCompatibilityRecovery = 7061,
    /// <summary>
    /// Represents Gmail compatibility recovery that was explicitly applied.
    /// </summary>
    GmailCompatibilityRecovery = 7062,
    /// <summary>
    /// Represents normalization of a common nonstandard MIME type alias.
    /// </summary>
    MimeTypeAliasRecovery = 7063,
    /// <summary>
    /// Represents tolerant recovery of a broken encoded word.
    /// </summary>
    EncodedWordCompatibilityRecovery = 7064,
    /// <summary>
    /// Represents tolerant recovery of a broken RFC 2231 parameter.
    /// </summary>
    Rfc2231CompatibilityRecovery = 7065,
    /// <summary>
    /// Represents explicit recovery of a malformed MIME boundary.
    /// </summary>
    MimeBoundaryCompatibilityRecovery = 7066,
    /// <summary>
    /// Represents recovery of legacy attachment metadata.
    /// </summary>
    LegacyAttachmentRecovery = 7067,
    /// <summary>
    /// Represents the authentication failed choice or state.
    /// </summary>
    AuthenticationFailed = 8000,
    /// <summary>
    /// Represents the authentication policy violation choice or state.
    /// </summary>
    AuthenticationPolicyViolation = 8001,
    /// <summary>
    /// Represents the DKIM key record malformed choice or state.
    /// </summary>
    DkimKeyRecordMalformed = 8002,
    /// <summary>
    /// Represents the DKIM key revoked choice or state.
    /// </summary>
    DkimKeyRevoked = 8003,
    /// <summary>
    /// Represents the DKIM key algorithm unsupported choice or state.
    /// </summary>
    DkimKeyAlgorithmUnsupported = 8004,
    /// <summary>
    /// Represents the DKIM key material malformed choice or state.
    /// </summary>
    DkimKeyMaterialMalformed = 8005,
    /// <summary>
    /// Represents the DKIM key size policy violation choice or state.
    /// </summary>
    DkimKeySizePolicyViolation = 8006,
    /// <summary>
    /// Represents the DKIM key input limit exceeded choice or state.
    /// </summary>
    DkimKeyInputLimitExceeded = 8007,
    /// <summary>
    /// Represents the DKIM key pem malformed choice or state.
    /// </summary>
    DkimKeyPemMalformed = 8008,
    /// <summary>
    /// Represents the ARC set incomplete choice or state.
    /// </summary>
    ArcSetIncomplete = 8009,
    /// <summary>
    /// Represents the ARC set duplicate header choice or state.
    /// </summary>
    ArcSetDuplicateHeader = 8010,
    /// <summary>
    /// Represents the ARC instance sequence invalid choice or state.
    /// </summary>
    ArcInstanceSequenceInvalid = 8011,
    /// <summary>
    /// Represents the ARC chain status invalid choice or state.
    /// </summary>
    ArcChainStatusInvalid = 8012,
    /// <summary>
    /// Represents the ARC header unassigned choice or state.
    /// </summary>
    ArcHeaderUnassigned = 8013,
    /// <summary>
    /// Represents DKIM key record data that cannot be formatted safely.
    /// </summary>
    DkimKeyFormattingInvalid = 8014,
    /// <summary>
    /// Represents a DKIM key record that exceeded the configured formatting output limit.
    /// </summary>
    DkimKeyOutputLimitExceeded = 8015,
    /// <summary>
    /// Represents the cryptographic format malformed choice or state.
    /// </summary>
    CryptographicFormatMalformed = 9000,
    /// <summary>
    /// Represents the cryptographic operation failed choice or state.
    /// </summary>
    CryptographicOperationFailed = 9001,
    /// <summary>
    /// Represents the security multipart structure malformed choice or state.
    /// </summary>
    SecurityMultipartStructureMalformed = 9002,
    /// <summary>
    /// Represents the security multipart protocol missing choice or state.
    /// </summary>
    SecurityMultipartProtocolMissing = 9003,
    /// <summary>
    /// Represents the security multipart protocol malformed choice or state.
    /// </summary>
    SecurityMultipartProtocolMalformed = 9004,
    /// <summary>
    /// Represents the security multipart protocol mismatch choice or state.
    /// </summary>
    SecurityMultipartProtocolMismatch = 9005,
    /// <summary>
    /// Represents the security multipart mic algorithm missing choice or state.
    /// </summary>
    SecurityMultipartMicAlgorithmMissing = 9006,
    /// <summary>
    /// Represents the security multipart mic algorithm malformed choice or state.
    /// </summary>
    SecurityMultipartMicAlgorithmMalformed = 9007,
    /// <summary>
    /// Represents the security multipart signed content unavailable choice or state.
    /// </summary>
    SecurityMultipartSignedContentUnavailable = 9008,
    /// <summary>
    /// Represents the CMS content info malformed choice or state.
    /// </summary>
    CmsContentInfoMalformed = 9009,
    /// <summary>
    /// Represents the CMS object identifier malformed choice or state.
    /// </summary>
    CmsObjectIdentifierMalformed = 9010,
    /// <summary>
    /// Represents the CMS content malformed choice or state.
    /// </summary>
    CmsContentMalformed = 9011,
    /// <summary>
    /// Represents the CMS ber encoding accepted choice or state.
    /// </summary>
    CmsBerEncodingAccepted = 9012,
    /// <summary>
    /// Represents the CMS der encoding failed choice or state.
    /// </summary>
    CmsDerEncodingFailed = 9013,
    /// <summary>
    /// Represents the S/MIME signed data malformed choice or state.
    /// </summary>
    SmimeSignedDataMalformed = 9014,
    /// <summary>
    /// Represents the S/MIME signer certificate missing choice or state.
    /// </summary>
    SmimeSignerCertificateMissing = 9015,
    /// <summary>
    /// Represents the S/MIME unsupported algorithm choice or state.
    /// </summary>
    SmimeUnsupportedAlgorithm = 9016,
    /// <summary>
    /// Represents the S/MIME digest mismatch choice or state.
    /// </summary>
    SmimeDigestMismatch = 9017,
    /// <summary>
    /// Represents the S/MIME signature mismatch choice or state.
    /// </summary>
    SmimeSignatureMismatch = 9018,
    /// <summary>
    /// Represents the S/MIME certificate chain invalid choice or state.
    /// </summary>
    SmimeCertificateChainInvalid = 9019,
    /// <summary>
    /// Represents the S/MIME algorithm policy rejected choice or state.
    /// </summary>
    SmimeAlgorithmPolicyRejected = 9020,
    /// <summary>
    /// Represents the S/MIME signing failed choice or state.
    /// </summary>
    SmimeSigningFailed = 9021,
    /// <summary>
    /// Represents the S/MIME enveloped data malformed choice or state.
    /// </summary>
    SmimeEnvelopedDataMalformed = 9022,
    /// <summary>
    /// Represents the S/MIME recipient not found choice or state.
    /// </summary>
    SmimeRecipientNotFound = 9023,
    /// <summary>
    /// Represents the S/MIME key transport unsupported choice or state.
    /// </summary>
    SmimeKeyTransportUnsupported = 9024,
    /// <summary>
    /// Represents the S/MIME content encryption unsupported choice or state.
    /// </summary>
    SmimeContentEncryptionUnsupported = 9025,
    /// <summary>
    /// Represents the S/MIME encryption policy rejected choice or state.
    /// </summary>
    SmimeEncryptionPolicyRejected = 9026,
    /// <summary>
    /// Represents the S/MIME encryption failed choice or state.
    /// </summary>
    SmimeEncryptionFailed = 9027,
    /// <summary>
    /// Represents the S/MIME decryption failed choice or state.
    /// </summary>
    SmimeDecryptionFailed = 9028,
    /// <summary>
    /// Represents the S/MIME authentication failed choice or state.
    /// </summary>
    SmimeAuthenticationFailed = 9029,
    /// <summary>
    /// Represents the S/MIME compressed data malformed choice or state.
    /// </summary>
    SmimeCompressedDataMalformed = 9030,
    /// <summary>
    /// Represents the S/MIME compression algorithm unsupported choice or state.
    /// </summary>
    SmimeCompressionAlgorithmUnsupported = 9031,
    /// <summary>
    /// Represents the S/MIME compression failed choice or state.
    /// </summary>
    SmimeCompressionFailed = 9032,
    /// <summary>
    /// Represents the S/MIME decompression failed choice or state.
    /// </summary>
    SmimeDecompressionFailed = 9033,
    /// <summary>
    /// Represents the openPGP armor malformed choice or state.
    /// </summary>
    OpenPgpArmorMalformed = 9034,
    /// <summary>
    /// Represents the openPGP armor checksum mismatch choice or state.
    /// </summary>
    OpenPgpArmorChecksumMismatch = 9035,
    /// <summary>
    /// Represents the openPGP packet malformed choice or state.
    /// </summary>
    OpenPgpPacketMalformed = 9036,
    /// <summary>
    /// Represents the openPGP packet unsupported choice or state.
    /// </summary>
    OpenPgpPacketUnsupported = 9037,
    /// <summary>
    /// Represents the openPGP algorithm unsupported choice or state.
    /// </summary>
    OpenPgpAlgorithmUnsupported = 9038,
    /// <summary>
    /// Represents the openPGP algorithm policy rejected choice or state.
    /// </summary>
    OpenPgpAlgorithmPolicyRejected = 9039,
    /// <summary>
    /// Represents the openPGP operation failed choice or state.
    /// </summary>
    OpenPgpOperationFailed = 9040,
    /// <summary>
    /// Represents the openPGP authentication failed choice or state.
    /// </summary>
    OpenPgpAuthenticationFailed = 9041,
    /// <summary>
    /// Represents the openPGP MIME malformed choice or state.
    /// </summary>
    OpenPgpMimeMalformed = 9042,
    /// <summary>
    /// Represents the openPGP signing failed choice or state.
    /// </summary>
    OpenPgpSigningFailed = 9043,
    /// <summary>
    /// Represents the openPGP verification key missing choice or state.
    /// </summary>
    OpenPgpVerificationKeyMissing = 9044,
    /// <summary>
    /// Represents the openPGP signature mismatch choice or state.
    /// </summary>
    OpenPgpSignatureMismatch = 9045,
    /// <summary>
    /// Represents the openPGP encryption failed choice or state.
    /// </summary>
    OpenPgpEncryptionFailed = 9046,
    /// <summary>
    /// Represents the openPGP recipient not found choice or state.
    /// </summary>
    OpenPgpRecipientNotFound = 9047,
    /// <summary>
    /// Represents the openPGP cleartext malformed choice or state.
    /// </summary>
    OpenPgpCleartextMalformed = 9048,
    /// <summary>
    /// Represents the openPGP inline unsupported choice or state.
    /// </summary>
    OpenPgpInlineUnsupported = 9049,
    /// <summary>
    /// Represents the protected header malformed choice or state.
    /// </summary>
    ProtectedHeaderMalformed = 9050,
    /// <summary>
    /// Represents the protected header intent mismatch choice or state.
    /// </summary>
    ProtectedHeaderIntentMismatch = 9051,
    /// <summary>
    /// Represents the protected header outer mismatch choice or state.
    /// </summary>
    ProtectedHeaderOuterMismatch = 9052,
    /// <summary>
    /// Represents the protected header from mismatch choice or state.
    /// </summary>
    ProtectedHeaderFromMismatch = 9053
}
