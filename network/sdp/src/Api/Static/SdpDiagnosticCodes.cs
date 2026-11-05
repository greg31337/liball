namespace sdp;

/// <summary>
/// Defines stable diagnostic codes returned by SDP operations.
/// </summary>
public static class SdpDiagnosticCodes
{
    /// <summary>The configured diagnostic capacity was exhausted.</summary>
    public const string DiagnosticLimitReached = "SDP_DIAGNOSTIC_LIMIT_REACHED";

    /// <summary>Text input exceeded the configured character limit.</summary>
    public const string ParseInputCharacterLimitExceeded = "SDP_PARSE_INPUT_CHARACTER_LIMIT_EXCEEDED";

    /// <summary>UTF-8 input exceeded the configured byte limit.</summary>
    public const string ParseInputByteLimitExceeded = "SDP_PARSE_INPUT_BYTE_LIMIT_EXCEEDED";

    /// <summary>An input line exceeded the configured length limit.</summary>
    public const string ParseLineLengthLimitExceeded = "SDP_PARSE_LINE_LENGTH_LIMIT_EXCEEDED";

    /// <summary>Input exceeded the configured line-count limit.</summary>
    public const string ParseLineCountLimitExceeded = "SDP_PARSE_LINE_COUNT_LIMIT_EXCEEDED";

    /// <summary>Input exceeded the configured media-description limit.</summary>
    public const string ParseMediaCountLimitExceeded = "SDP_PARSE_MEDIA_COUNT_LIMIT_EXCEEDED";

    /// <summary>An SDP scope exceeded the configured attribute-count limit.</summary>
    public const string ParseAttributeCountLimitExceeded = "SDP_PARSE_ATTRIBUTE_COUNT_LIMIT_EXCEEDED";

    /// <summary>An input token exceeded the configured length limit.</summary>
    public const string ParseTokenLengthLimitExceeded = "SDP_PARSE_TOKEN_LENGTH_LIMIT_EXCEEDED";

    /// <summary>A media scope exceeded the configured ICE candidate-count limit.</summary>
    public const string ParseCandidateCountLimitExceeded = "SDP_PARSE_CANDIDATE_COUNT_LIMIT_EXCEEDED";

    /// <summary>UTF-8 input contains an invalid byte sequence.</summary>
    public const string ParseInvalidUtf8 = "SDP_PARSE_INVALID_UTF8";

    /// <summary>Input contains a byte-order mark that is not accepted by the parse mode.</summary>
    public const string ParseByteOrderMarkNotAllowed = "SDP_PARSE_BYTE_ORDER_MARK_NOT_ALLOWED";

    /// <summary>Input contains a NUL character.</summary>
    public const string ParseNulCharacterNotAllowed = "SDP_PARSE_NUL_CHARACTER_NOT_ALLOWED";

    /// <summary>Input contains a disallowed control character.</summary>
    public const string ParseControlCharacterNotAllowed = "SDP_PARSE_CONTROL_CHARACTER_NOT_ALLOWED";

    /// <summary>Input uses a line ending that is not accepted by the parse mode.</summary>
    public const string ParseInvalidLineEnding = "SDP_PARSE_INVALID_LINE_ENDING";

    /// <summary>Input contains an empty SDP line.</summary>
    public const string ParseEmptyLine = "SDP_PARSE_EMPTY_LINE";

    /// <summary>Input contains a line without a valid field discriminator and equals sign.</summary>
    public const string ParseMalformedLine = "SDP_PARSE_MALFORMED_LINE";

    /// <summary>Input contains an unknown field discriminator.</summary>
    public const string ParseUnknownField = "SDP_PARSE_UNKNOWN_FIELD";

    /// <summary>An SDP field appears outside the required field order.</summary>
    public const string ParseFieldOutOfOrder = "SDP_PARSE_FIELD_OUT_OF_ORDER";

    /// <summary>A non-repeatable SDP field appears more than once.</summary>
    public const string ParseDuplicateField = "SDP_PARSE_DUPLICATE_FIELD";

    /// <summary>A required SDP field is absent.</summary>
    public const string ParseMissingRequiredField = "SDP_PARSE_MISSING_REQUIRED_FIELD";

    /// <summary>An SDP field appears in a scope where it is not permitted.</summary>
    public const string ParseFieldInWrongScope = "SDP_PARSE_FIELD_IN_WRONG_SCOPE";

    /// <summary>An SDP token violates its required syntax.</summary>
    public const string ParseInvalidToken = "SDP_PARSE_INVALID_TOKEN";

    /// <summary>A numeric value contains invalid characters or violates its required form.</summary>
    public const string ParseInvalidNumber = "SDP_PARSE_INVALID_NUMBER";

    /// <summary>A numeric value exceeds the supported representation.</summary>
    public const string ParseNumberOverflow = "SDP_PARSE_NUMBER_OVERFLOW";

    /// <summary>An SDP attribute has malformed name or value syntax.</summary>
    public const string ParseMalformedAttribute = "SDP_PARSE_MALFORMED_ATTRIBUTE";

    /// <summary>Lossless parsing retained a malformed line and resumed at a safe boundary.</summary>
    public const string ParseRecoveredMalformedLine = "SDP_PARSE_RECOVERED_MALFORMED_LINE";

    /// <summary>The protocol version is not the required value.</summary>
    public const string ValidationProtocolVersion = "SDP_VALIDATION_PROTOCOL_VERSION";

    /// <summary>Effective connection data is missing.</summary>
    public const string ValidationMissingConnection = "SDP_VALIDATION_MISSING_CONNECTION";

    /// <summary>The origin field is semantically invalid.</summary>
    public const string ValidationOrigin = "SDP_VALIDATION_ORIGIN";

    /// <summary>Connection data is semantically invalid.</summary>
    public const string ValidationConnection = "SDP_VALIDATION_CONNECTION";

    /// <summary>Bandwidth data is semantically invalid.</summary>
    public const string ValidationBandwidth = "SDP_VALIDATION_BANDWIDTH";

    /// <summary>Timing data is semantically invalid.</summary>
    public const string ValidationTiming = "SDP_VALIDATION_TIMING";

    /// <summary>A media description is semantically invalid.</summary>
    public const string ValidationMedia = "SDP_VALIDATION_MEDIA";

    /// <summary>A port or port count is outside its permitted range.</summary>
    public const string ValidationPort = "SDP_VALIDATION_PORT";

    /// <summary>An RTP payload type is invalid, duplicated, or inconsistent.</summary>
    public const string ValidationPayloadType = "SDP_VALIDATION_PAYLOAD_TYPE";

    /// <summary>An attribute is invalid for its declared syntax or profile.</summary>
    public const string ValidationAttribute = "SDP_VALIDATION_ATTRIBUTE";

    /// <summary>An attribute appears in a scope where it is not registered.</summary>
    public const string ValidationAttributeScope = "SDP_VALIDATION_ATTRIBUTE_SCOPE";

    /// <summary>An attribute violates a cardinality rule.</summary>
    public const string ValidationAttributeCardinality = "SDP_VALIDATION_ATTRIBUTE_CARDINALITY";

    /// <summary>A dynamic RTP payload lacks a required RTP mapping.</summary>
    public const string ValidationDynamicPayloadMissingRtpMap = "SDP_VALIDATION_DYNAMIC_PAYLOAD_MISSING_RTPMAP";

    /// <summary>An attribute refers to a payload that is absent from the media format list.</summary>
    public const string ValidationDanglingPayloadReference = "SDP_VALIDATION_DANGLING_PAYLOAD_REFERENCE";

    /// <summary>ICE signaling violates the selected profile.</summary>
    public const string ValidationIce = "SDP_VALIDATION_ICE";

    /// <summary>TLS or DTLS signaling violates the selected profile.</summary>
    public const string ValidationDtls = "SDP_VALIDATION_DTLS";

    /// <summary>An RTP header-extension mapping is invalid or conflicts with another mapping.</summary>
    public const string ValidationExtMap = "SDP_VALIDATION_EXTMAP";

    /// <summary>An RTP synchronization source or source group is invalid or inconsistent.</summary>
    public const string ValidationSource = "SDP_VALIDATION_SOURCE";

    /// <summary>A media-stream identifier association or semantic declaration is invalid.</summary>
    public const string ValidationMsid = "SDP_VALIDATION_MSID";

    /// <summary>An RTP stream restriction or dependency is invalid or inconsistent.</summary>
    public const string ValidationRid = "SDP_VALIDATION_RID";

    /// <summary>Simulcast signaling is invalid or inconsistent with its RID capabilities.</summary>
    public const string ValidationSimulcast = "SDP_VALIDATION_SIMULCAST";

    /// <summary>A media group is invalid or refers to an unknown media identifier.</summary>
    public const string ValidationGroup = "SDP_VALIDATION_GROUP";

    /// <summary>BUNDLE signaling violates the selected profile.</summary>
    public const string ValidationBundle = "SDP_VALIDATION_BUNDLE";

    /// <summary>SCTP or data-channel signaling violates the selected profile.</summary>
    public const string ValidationDataChannel = "SDP_VALIDATION_DATA_CHANNEL";

    /// <summary>BFCP signaling violates the selected profile.</summary>
    public const string ValidationBfcp = "SDP_VALIDATION_BFCP";

    /// <summary>A timing value cannot be represented exactly in the requested destination form.</summary>
    public const string TimingUnrepresentable = "SDP_TIMING_UNREPRESENTABLE";

    /// <summary>An obsolete SDP field or attribute is present.</summary>
    public const string ValidationObsoleteSyntax = "SDP_VALIDATION_OBSOLETE_SYNTAX";

    /// <summary>The description cannot be written because required state is invalid or absent.</summary>
    public const string WriteInvalidDescription = "SDP_WRITE_INVALID_DESCRIPTION";

    /// <summary>Generated text would exceed the configured character limit.</summary>
    public const string WriteOutputCharacterLimitExceeded = "SDP_WRITE_OUTPUT_CHARACTER_LIMIT_EXCEEDED";

    /// <summary>Generated UTF-8 data would exceed the configured byte limit.</summary>
    public const string WriteOutputByteLimitExceeded = "SDP_WRITE_OUTPUT_BYTE_LIMIT_EXCEEDED";

    /// <summary>The caller-provided output buffer is too small.</summary>
    public const string WriteBufferTooSmall = "SDP_WRITE_BUFFER_TOO_SMALL";

    /// <summary>Lossless output was requested but required source details are unavailable.</summary>
    public const string WriteLosslessDataUnavailable = "SDP_WRITE_LOSSLESS_DATA_UNAVAILABLE";

    /// <summary>The supplied SDP offer is invalid.</summary>
    public const string NegotiationInvalidOffer = "SDP_NEGOTIATION_INVALID_OFFER";

    /// <summary>The supplied SDP answer is invalid for its offer.</summary>
    public const string NegotiationInvalidAnswer = "SDP_NEGOTIATION_INVALID_ANSWER";

    /// <summary>The media description was already rejected by its offerer.</summary>
    public const string NegotiationMediaAlreadyRejected = "SDP_NEGOTIATION_MEDIA_ALREADY_REJECTED";

    /// <summary>The offered media kind is unsupported.</summary>
    public const string NegotiationUnsupportedMedia = "SDP_NEGOTIATION_UNSUPPORTED_MEDIA";

    /// <summary>The offered transport protocol is unsupported.</summary>
    public const string NegotiationUnsupportedProtocol = "SDP_NEGOTIATION_UNSUPPORTED_PROTOCOL";

    /// <summary>No offered codec is compatible with local capabilities.</summary>
    public const string NegotiationNoCompatibleCodec = "SDP_NEGOTIATION_NO_COMPATIBLE_CODEC";

    /// <summary>The offered and local media directions are incompatible.</summary>
    public const string NegotiationIncompatibleDirection = "SDP_NEGOTIATION_INCOMPATIBLE_DIRECTION";

    /// <summary>No nonconflicting RTP payload type can be allocated.</summary>
    public const string NegotiationPayloadAllocationFailed = "SDP_NEGOTIATION_PAYLOAD_ALLOCATION_FAILED";

    /// <summary>No nonconflicting RTP header-extension identifier can be allocated.</summary>
    public const string NegotiationExtMapAllocationFailed = "SDP_NEGOTIATION_EXTMAP_ALLOCATION_FAILED";

    /// <summary>ICE capabilities or credentials are incompatible.</summary>
    public const string NegotiationIceIncompatible = "SDP_NEGOTIATION_ICE_INCOMPATIBLE";

    /// <summary>TLS or DTLS capabilities are incompatible.</summary>
    public const string NegotiationDtlsIncompatible = "SDP_NEGOTIATION_DTLS_INCOMPATIBLE";

    /// <summary>BUNDLE requirements cannot be satisfied.</summary>
    public const string NegotiationBundleIncompatible = "SDP_NEGOTIATION_BUNDLE_INCOMPATIBLE";

    /// <summary>Data-channel capabilities are incompatible.</summary>
    public const string NegotiationDataChannelIncompatible = "SDP_NEGOTIATION_DATA_CHANNEL_INCOMPATIBLE";

    /// <summary>RTP and RTCP multiplexing requirements are incompatible.</summary>
    public const string NegotiationRtcpMuxIncompatible = "SDP_NEGOTIATION_RTCP_MUX_INCOMPATIBLE";

    /// <summary>The media description was rejected by local policy.</summary>
    public const string NegotiationRejectedByPolicy = "SDP_NEGOTIATION_REJECTED_BY_POLICY";
}
