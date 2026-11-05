namespace diameter;

/// <summary>
/// Defines Diameter wire-format, transport, time, and protocol-limit constants.
/// </summary>
public static class DiameterProtocolConstants
{
    /// <summary>The supported Diameter protocol version.</summary>
    public const byte Version = 1;

    /// <summary>The size of a Diameter message header in octets.</summary>
    public const int MessageHeaderSize = 20;

    /// <summary>The size of an AVP header without a Vendor-ID in octets.</summary>
    public const int AvpHeaderSize = 8;

    /// <summary>The size of an AVP header containing a Vendor-ID in octets.</summary>
    public const int VendorSpecificAvpHeaderSize = 12;

    /// <summary>The size of a 24-bit wire field in octets.</summary>
    public const int UInt24Size = 3;

    /// <summary>The size of a Diameter identifier in octets.</summary>
    public const int IdentifierSize = 4;

    /// <summary>The required message and AVP alignment in octets.</summary>
    public const int Alignment = 4;

    /// <summary>The smallest valid Diameter message length in octets.</summary>
    public const int MinimumMessageLength = 20;

    /// <summary>The largest aligned length representable by a Diameter message header.</summary>
    public const int MaximumMessageLength = 16777212;

    /// <summary>The largest length representable by an AVP length field.</summary>
    public const int MaximumAvpLength = 16777215;

    /// <summary>The largest command code representable by a Diameter header.</summary>
    public const uint MaximumCommandCode = 16777215;

    /// <summary>The first command code reserved for experimental use.</summary>
    public const uint ExperimentalCommandCodeMinimum = 16777214;

    /// <summary>The standard Diameter TCP and SCTP port.</summary>
    public const int Port = 3868;

    /// <summary>The secure Diameter TLS/TCP and DTLS/SCTP port.</summary>
    public const int SecurePort = 5868;

    /// <summary>The SCTP payload protocol identifier for clear-text Diameter.</summary>
    public const uint SctpPayloadProtocolIdentifier = 46;

    /// <summary>The SCTP payload protocol identifier for Diameter over DTLS.</summary>
    public const uint DtlsSctpPayloadProtocolIdentifier = 47;

    /// <summary>The mask containing all assigned Diameter message flag bits.</summary>
    public const byte MessageFlagMask = 240;

    /// <summary>The mask containing all reserved Diameter message flag bits.</summary>
    public const byte MessageReservedFlagMask = 15;

    /// <summary>The mask containing all assigned Diameter AVP flag bits.</summary>
    public const byte AvpFlagMask = 224;

    /// <summary>The mask containing all reserved Diameter AVP flag bits.</summary>
    public const byte AvpReservedFlagMask = 31;

    /// <summary>The number of seconds between the NTP and Unix epochs.</summary>
    public const long NtpUnixEpochOffsetSeconds = 2208988800;

    /// <summary>The number of seconds in one 32-bit NTP era.</summary>
    public const long NtpEraSeconds = 4294967296;

    /// <summary>The minimum lifetime of End-to-End Identifier uniqueness in seconds.</summary>
    public const int EndToEndIdentifierLifetimeSeconds = 240;

    /// <summary>The RFC 6733 recommended connection retry interval in seconds.</summary>
    public const int RecommendedConnectionRetrySeconds = 30;
}
