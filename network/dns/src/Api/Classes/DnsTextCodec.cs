namespace dns;

/// <summary>Parses and formats DNS presentation text.</summary>
public class DnsTextCodec : IDnsTextCodec
{
    /// <summary>Creates a DNS presentation-text codec.</summary>
    public DnsTextCodec()
    {
    }

    /// <summary>Parses one presentation-format DNS question.</summary>
    /// <param name="text">The question text.</param>
    /// <param name="options">The origin and mnemonic parsing policy.</param>
    /// <returns>The parsed question or diagnostics.</returns>
    public DnsTextResult ParseQuestionText(
        string text,
        DnsTextOptions options)
    {
        return DnsQuestionText.ParseQuestion(text, options);
    }

    /// <summary>Formats one DNS question as presentation text.</summary>
    /// <param name="question">The question to format.</param>
    /// <param name="options">The presentation policy.</param>
    /// <returns>The formatted question or diagnostics.</returns>
    public DnsTextResult FormatQuestionText(
        DnsQuestion question,
        DnsTextOptions options)
    {
        return DnsQuestionText.FormatQuestion(question, options);
    }

    /// <summary>Parses an RFC 3597 presentation-format resource record.</summary>
    /// <param name="text">The complete resource-record text.</param>
    /// <param name="options">The origin and mnemonic parsing policy.</param>
    /// <returns>The parsed resource record or diagnostics.</returns>
    public DnsTextResult ParseResourceRecordText(
        string text,
        DnsTextOptions options)
    {
        return DnsResourceRecordText.ParseResourceRecord(text, options);
    }

    /// <summary>Formats one opaque resource record using RFC 3597 syntax.</summary>
    /// <param name="record">The resource record to format.</param>
    /// <param name="options">The presentation policy.</param>
    /// <returns>The formatted resource record or diagnostics.</returns>
    public DnsTextResult FormatResourceRecordText(
        DnsResourceRecord record,
        DnsTextOptions options)
    {
        return DnsResourceRecordText.FormatResourceRecord(record, options);
    }

    /// <summary>Formats one complete DNS message for presentation and diagnostics.</summary>
    /// <param name="message">The message to format.</param>
    /// <param name="options">The presentation policy.</param>
    /// <returns>The formatted message or diagnostics.</returns>
    public DnsTextResult FormatMessageText(
        DnsMessage message,
        DnsTextOptions options)
    {
        return DnsMessageText.FormatMessage(message, options);
    }

    /// <summary>Parses a TTL with optional compound unit suffixes.</summary>
    /// <param name="text">The TTL text.</param>
    /// <returns>The TTL value in seconds.</returns>
    /// <exception cref="DnsFormatException">The TTL is malformed or out of range.</exception>
    public uint ParseTimeToLive(string text)
    {
        DnsTextNumberResult result = DnsTimeToLiveText.ParseTimeToLive(text);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The TTL text is invalid."));
        }

        return (uint)result.Value;
    }

    /// <summary>Formats a TTL as portable decimal seconds.</summary>
    /// <param name="timeToLive">The TTL value in seconds.</param>
    /// <returns>The formatted TTL.</returns>
    public string FormatTimeToLive(uint timeToLive)
    {
        return DnsTimeToLiveText.FormatTimeToLive(timeToLive);
    }

    /// <summary>Formats a DNS class.</summary>
    /// <param name="value">The class value.</param>
    /// <returns>The class mnemonic or generic code.</returns>
    public string FormatDnsClass(DnsClass value)
    {
        return DnsMnemonicText.FormatClass((int)value, true);
    }

    /// <summary>Formats a DNS record type.</summary>
    /// <param name="value">The record type.</param>
    /// <returns>The type mnemonic or generic code.</returns>
    public string FormatDnsRecordType(DnsRecordType value)
    {
        return DnsMnemonicText.FormatRecordType((int)value, true);
    }

    /// <summary>Formats a DNS operation code.</summary>
    /// <param name="value">The operation code.</param>
    /// <returns>The operation text.</returns>
    public string FormatDnsOpcode(DnsOpcode value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "OPCODE");
    }

    /// <summary>Formats a DNS response code.</summary>
    /// <param name="value">The response code.</param>
    /// <returns>The response-code text.</returns>
    public string FormatDnsResponseCode(DnsResponseCode value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "RCODE");
    }

    /// <summary>Formats a DNS message section.</summary>
    /// <param name="value">The message section.</param>
    /// <returns>The section text.</returns>
    public string FormatDnsMessageSection(DnsMessageSection value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "SECTION");
    }

    /// <summary>Formats DNS header flags.</summary>
    /// <param name="value">The header flags.</param>
    /// <returns>The flags text.</returns>
    public string FormatDnsHeaderFlags(DnsHeaderFlag value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "FLAGS");
    }

    /// <summary>Formats EDNS flags.</summary>
    /// <param name="value">The EDNS flags.</param>
    /// <returns>The flags text.</returns>
    public string FormatDnsEdnsFlags(DnsEdnsFlag value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "EDNSFLAGS");
    }

    /// <summary>Formats an EDNS option code.</summary>
    /// <param name="value">The option code.</param>
    /// <returns>The option text.</returns>
    public string FormatDnsEdnsOptionCode(DnsEdnsOptionCode value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "OPTION");
    }

    /// <summary>Formats a Service Binding parameter key.</summary>
    /// <param name="value">The parameter key.</param>
    /// <returns>The parameter-key text.</returns>
    public string FormatDnsSvcbParameterKey(DnsSvcbParameterKey value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "KEY");
    }

    /// <summary>Formats a DNSSEC algorithm.</summary>
    /// <param name="value">The algorithm.</param>
    /// <returns>The algorithm text.</returns>
    public string FormatDnsDnssecAlgorithm(DnsDnssecAlgorithm value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "ALGORITHM");
    }

    /// <summary>Formats a delegation-signer digest algorithm.</summary>
    /// <param name="value">The digest algorithm.</param>
    /// <returns>The digest text.</returns>
    public string FormatDnsDigestAlgorithm(DnsDigestAlgorithm value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "DIGEST");
    }

    /// <summary>Formats an NSEC3 hash algorithm.</summary>
    /// <param name="value">The hash algorithm.</param>
    /// <returns>The hash text.</returns>
    public string FormatDnsNsec3HashAlgorithm(DnsNsec3HashAlgorithm value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "HASH");
    }

    /// <summary>Formats a TSIG error.</summary>
    /// <param name="value">The TSIG error.</param>
    /// <returns>The error text.</returns>
    public string FormatDnsTsigError(DnsTsigError value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "TSIGERROR");
    }

    /// <summary>Formats an Extended DNS Error code.</summary>
    /// <param name="value">The error code.</param>
    /// <returns>The error text.</returns>
    public string FormatDnsExtendedErrorCode(DnsExtendedErrorCode value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "EDE");
    }

    /// <summary>Formats a DNSSEC security status.</summary>
    /// <param name="value">The security status.</param>
    /// <returns>The status text.</returns>
    public string FormatDnsSecurityStatus(DnsSecurityStatus value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "SECURITY");
    }

    /// <summary>Formats a DNSSEC algorithm policy.</summary>
    /// <param name="value">The algorithm policy.</param>
    /// <returns>The policy text.</returns>
    public string FormatDnsAlgorithmPolicy(DnsAlgorithmPolicy value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "POLICY");
    }

    /// <summary>Formats a DNS diagnostic code.</summary>
    /// <param name="value">The diagnostic code.</param>
    /// <returns>The diagnostic text.</returns>
    public string FormatDnsDiagnosticCode(DnsDiagnosticCode value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "DIAGNOSTIC");
    }

    /// <summary>Formats a DNS transport kind.</summary>
    /// <param name="value">The transport kind.</param>
    /// <returns>The transport text.</returns>
    public string FormatDnsTransportKind(DnsTransportKind value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "TRANSPORT");
    }

    /// <summary>Formats a DNS transport status.</summary>
    /// <param name="value">The transport status.</param>
    /// <returns>The status text.</returns>
    public string FormatDnsTransportStatus(DnsTransportStatus value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "STATUS");
    }

    /// <summary>Formats a DNS cache-entry kind.</summary>
    /// <param name="value">The cache-entry kind.</param>
    /// <returns>The cache-entry text.</returns>
    public string FormatDnsCacheEntryKind(DnsCacheEntryKind value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "CACHE");
    }

    /// <summary>Formats a DNS resolution status.</summary>
    /// <param name="value">The resolution status.</param>
    /// <returns>The resolution text.</returns>
    public string FormatDnsResolveStatus(DnsResolveStatus value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "RESOLVE");
    }

    /// <summary>Formats an authoritative answer kind.</summary>
    /// <param name="value">The answer kind.</param>
    /// <returns>The answer text.</returns>
    public string FormatDnsAuthoritativeAnswerKind(
        DnsAuthoritativeAnswerKind value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "ANSWER");
    }

    /// <summary>Formats a zone change kind.</summary>
    /// <param name="value">The zone change kind.</param>
    /// <returns>The change text.</returns>
    public string FormatDnsZoneChangeKind(DnsZoneChangeKind value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "CHANGE");
    }

    /// <summary>Formats a zone transfer kind.</summary>
    /// <param name="value">The transfer kind.</param>
    /// <returns>The transfer text.</returns>
    public string FormatDnsTransferKind(DnsTransferKind value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "TRANSFER");
    }

    /// <summary>Formats a dynamic-update prerequisite kind.</summary>
    /// <param name="value">The prerequisite kind.</param>
    /// <returns>The prerequisite text.</returns>
    public string FormatDnsUpdatePrerequisiteKind(
        DnsUpdatePrerequisiteKind value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "PREREQUISITE");
    }

    /// <summary>Formats a dynamic-update change kind.</summary>
    /// <param name="value">The update change kind.</param>
    /// <returns>The change text.</returns>
    public string FormatDnsUpdateChangeKind(DnsUpdateChangeKind value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "UPDATE");
    }

    /// <summary>Formats a DNS server status.</summary>
    /// <param name="value">The server status.</param>
    /// <returns>The server-status text.</returns>
    public string FormatDnsServerStatus(DnsServerStatus value)
    {
        return DnsMnemonicText.FormatNamedValue(
            (int)value, value.ToString(), "SERVER");
    }
}
