namespace dns;

/// <summary>Formats DNS protocol and operation values for presentation and diagnostics.</summary>
public interface IDnsTextCodec
{
    /// <summary>Parses one presentation-format DNS question.</summary>
    /// <param name="text">The question text.</param>
    /// <param name="options">The origin and mnemonic parsing policy.</param>
    /// <returns>The parsed question or diagnostics.</returns>
    DnsTextResult ParseQuestionText(string text, DnsTextOptions options);

    /// <summary>Formats one DNS question as presentation text.</summary>
    /// <param name="question">The question to format.</param>
    /// <param name="options">The origin, canonical, and mnemonic policy.</param>
    /// <returns>The formatted question text.</returns>
    DnsTextResult FormatQuestionText(DnsQuestion question, DnsTextOptions options);

    /// <summary>Parses one presentation-format resource record.</summary>
    /// <param name="text">The complete resource-record text.</param>
    /// <param name="options">The origin and mnemonic parsing policy.</param>
    /// <returns>The parsed resource record or diagnostics.</returns>
    DnsTextResult ParseResourceRecordText(string text, DnsTextOptions options);

    /// <summary>Formats one resource record as presentation text.</summary>
    /// <param name="record">The resource record to format.</param>
    /// <param name="options">The canonical, origin, and multiline policy.</param>
    /// <returns>The formatted resource-record text.</returns>
    DnsTextResult FormatResourceRecordText(DnsResourceRecord record, DnsTextOptions options);

    /// <summary>Formats one complete DNS message as presentation text.</summary>
    /// <param name="message">The message to format.</param>
    /// <param name="options">The header, sections, canonical, and mnemonic policy.</param>
    /// <returns>The formatted message text.</returns>
    DnsTextResult FormatMessageText(DnsMessage message, DnsTextOptions options);

    /// <summary>Parses a presentation TTL with optional unit suffixes.</summary>
    /// <param name="text">The TTL text.</param>
    /// <returns>The TTL in seconds.</returns>
    uint ParseTimeToLive(string text);

    /// <summary>Formats a TTL as portable presentation text.</summary>
    /// <param name="timeToLive">The TTL in seconds.</param>
    /// <returns>The formatted TTL text.</returns>
    string FormatTimeToLive(uint timeToLive);

    /// <summary>Formats a DNS class value.</summary>
    /// <param name="value">The class value to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsClass(DnsClass value);

    /// <summary>Formats a DNS record type.</summary>
    /// <param name="value">The record type to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsRecordType(DnsRecordType value);

    /// <summary>Formats a DNS operation code.</summary>
    /// <param name="value">The operation code to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsOpcode(DnsOpcode value);

    /// <summary>Formats a DNS response code.</summary>
    /// <param name="value">The response code to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsResponseCode(DnsResponseCode value);

    /// <summary>Formats a DNS message section.</summary>
    /// <param name="value">The message section to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsMessageSection(DnsMessageSection value);

    /// <summary>Formats DNS header flags.</summary>
    /// <param name="value">The header flags to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsHeaderFlags(DnsHeaderFlag value);

    /// <summary>Formats EDNS flags.</summary>
    /// <param name="value">The EDNS flags to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsEdnsFlags(DnsEdnsFlag value);

    /// <summary>Formats an EDNS option code.</summary>
    /// <param name="value">The option code to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsEdnsOptionCode(DnsEdnsOptionCode value);

    /// <summary>Formats a Service Binding parameter key.</summary>
    /// <param name="value">The parameter key to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsSvcbParameterKey(DnsSvcbParameterKey value);

    /// <summary>Formats a DNSSEC algorithm.</summary>
    /// <param name="value">The algorithm to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsDnssecAlgorithm(DnsDnssecAlgorithm value);

    /// <summary>Formats a delegation-signer digest algorithm.</summary>
    /// <param name="value">The digest algorithm to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsDigestAlgorithm(DnsDigestAlgorithm value);

    /// <summary>Formats an NSEC3 hash algorithm.</summary>
    /// <param name="value">The hash algorithm to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsNsec3HashAlgorithm(DnsNsec3HashAlgorithm value);

    /// <summary>Formats a TSIG error.</summary>
    /// <param name="value">The TSIG error to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsTsigError(DnsTsigError value);

    /// <summary>Formats an Extended DNS Error code.</summary>
    /// <param name="value">The error code to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsExtendedErrorCode(DnsExtendedErrorCode value);

    /// <summary>Formats a DNSSEC security status.</summary>
    /// <param name="value">The security status to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsSecurityStatus(DnsSecurityStatus value);

    /// <summary>Formats a DNSSEC algorithm policy.</summary>
    /// <param name="value">The algorithm policy to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsAlgorithmPolicy(DnsAlgorithmPolicy value);

    /// <summary>Formats a diagnostic code.</summary>
    /// <param name="value">The diagnostic code to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsDiagnosticCode(DnsDiagnosticCode value);

    /// <summary>Formats a DNS transport kind.</summary>
    /// <param name="value">The transport kind to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsTransportKind(DnsTransportKind value);

    /// <summary>Formats a DNS transport status.</summary>
    /// <param name="value">The transport status to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsTransportStatus(DnsTransportStatus value);

    /// <summary>Formats a DNS cache entry kind.</summary>
    /// <param name="value">The cache entry kind to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsCacheEntryKind(DnsCacheEntryKind value);

    /// <summary>Formats a DNS resolution status.</summary>
    /// <param name="value">The resolution status to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsResolveStatus(DnsResolveStatus value);

    /// <summary>Formats an authoritative answer kind.</summary>
    /// <param name="value">The answer kind to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsAuthoritativeAnswerKind(DnsAuthoritativeAnswerKind value);

    /// <summary>Formats a zone change kind.</summary>
    /// <param name="value">The zone change kind to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsZoneChangeKind(DnsZoneChangeKind value);

    /// <summary>Formats a zone transfer kind.</summary>
    /// <param name="value">The transfer kind to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsTransferKind(DnsTransferKind value);

    /// <summary>Formats a dynamic-update prerequisite kind.</summary>
    /// <param name="value">The prerequisite kind to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsUpdatePrerequisiteKind(DnsUpdatePrerequisiteKind value);

    /// <summary>Formats a dynamic-update change kind.</summary>
    /// <param name="value">The update change kind to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsUpdateChangeKind(DnsUpdateChangeKind value);

    /// <summary>Formats a DNS server status.</summary>
    /// <param name="value">The server status to format.</param>
    /// <returns>The presentation text.</returns>
    string FormatDnsServerStatus(DnsServerStatus value);
}
