using System;

namespace dns;

/// <summary>Validates, converts, compares, and encodes DNS names.</summary>
public interface IDnsNameService
{
    /// <summary>Validates the labels, absolute state, and expanded length of a DNS name.</summary>
    /// <param name="name">The name to validate.</param>
    /// <returns>The validated name.</returns>
    DnsName ValidateName(DnsName name);

    /// <summary>Parses one presentation-format DNS name.</summary>
    /// <param name="text">The escaped presentation name.</param>
    /// <param name="origin">The optional origin for a relative name.</param>
    /// <returns>The parsed DNS name.</returns>
    DnsName ParseName(string text, DnsName? origin);

    /// <summary>Attempts to parse one presentation-format DNS name.</summary>
    /// <param name="text">The escaped presentation name.</param>
    /// <param name="origin">The optional origin for a relative name.</param>
    /// <returns>The parsed name or diagnostics describing the failure.</returns>
    DnsTextResult TryParseName(string text, DnsName? origin);

    /// <summary>Formats one DNS name as escaped presentation text.</summary>
    /// <param name="name">The name to format.</param>
    /// <param name="options">The origin and canonical formatting policy.</param>
    /// <returns>The formatted presentation name.</returns>
    string FormatName(DnsName name, DnsTextOptions options);

    /// <summary>Strictly decodes one possibly compressed wire-format DNS name.</summary>
    /// <param name="message">The complete containing DNS message.</param>
    /// <param name="offset">The name's starting message offset.</param>
    /// <param name="options">The decoding limits and compression policy.</param>
    /// <returns>The decoded name and continuation offset.</returns>
    DnsDecodeResult DecodeName(ReadOnlySpan<byte> message, int offset, DnsDecodeOptions options);

    /// <summary>Attempts to decode one possibly compressed wire-format DNS name.</summary>
    /// <param name="message">The complete containing DNS message.</param>
    /// <param name="offset">The name's starting message offset.</param>
    /// <param name="options">The decoding limits and compression policy.</param>
    /// <returns>The decoded name or offset-aware diagnostics.</returns>
    DnsDecodeResult TryDecodeName(ReadOnlySpan<byte> message, int offset, DnsDecodeOptions options);

    /// <summary>Strictly encodes one standalone wire-format DNS name.</summary>
    /// <param name="name">The name to encode.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The encoded name bytes.</returns>
    DnsEncodeResult EncodeName(DnsName name, DnsEncodeOptions options);

    /// <summary>Attempts to encode one standalone wire-format DNS name.</summary>
    /// <param name="name">The name to encode.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The encoded bytes or diagnostics describing the failure.</returns>
    DnsEncodeResult TryEncodeName(DnsName name, DnsEncodeOptions options);

    /// <summary>Compares two names using DNS ASCII case-insensitive equality.</summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns>True when both names identify the same DNS name.</returns>
    bool NamesEqual(DnsName left, DnsName right);

    /// <summary>Compares two names in canonical DNSSEC order.</summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns>A negative, zero, or positive ordering value.</returns>
    int CompareNames(DnsName left, DnsName right);

    /// <summary>Tests whether a name ends with a supplied suffix.</summary>
    /// <param name="name">The complete name.</param>
    /// <param name="suffix">The candidate suffix.</param>
    /// <returns>True when the suffix is equal to or below the name.</returns>
    bool HasNameSuffix(DnsName name, DnsName suffix);

    /// <summary>Tests whether one name is a strict ancestor of another.</summary>
    /// <param name="ancestor">The candidate ancestor.</param>
    /// <param name="name">The candidate descendant.</param>
    /// <returns>True when the ancestor is strictly above the name.</returns>
    bool IsAncestorName(DnsName ancestor, DnsName name);

    /// <summary>Counts the labels in one DNS name.</summary>
    /// <param name="name">The name to inspect.</param>
    /// <returns>The signed label count.</returns>
    int CountNameLabels(DnsName name);

    /// <summary>Returns the immediate parent of a non-root name.</summary>
    /// <param name="name">The child name.</param>
    /// <returns>The parent DNS name.</returns>
    DnsName GetParentName(DnsName name);

    /// <summary>Appends a suffix to a relative prefix.</summary>
    /// <param name="prefix">The relative leading labels.</param>
    /// <param name="suffix">The trailing suffix.</param>
    /// <returns>The concatenated DNS name.</returns>
    DnsName ConcatenateNames(DnsName prefix, DnsName suffix);

    /// <summary>Removes an origin suffix from an absolute name.</summary>
    /// <param name="name">The absolute name.</param>
    /// <param name="origin">The origin suffix.</param>
    /// <returns>The relative DNS name.</returns>
    DnsName RelativizeName(DnsName name, DnsName origin);

    /// <summary>Finds the longest suffix common to two names.</summary>
    /// <param name="first">The first name.</param>
    /// <param name="second">The second name.</param>
    /// <returns>The common ancestor name.</returns>
    DnsName FindCommonAncestor(DnsName first, DnsName second);

    /// <summary>Creates the canonical lowercase form of one DNS name.</summary>
    /// <param name="name">The case-preserving input name.</param>
    /// <returns>The canonical DNS name.</returns>
    DnsName CreateCanonicalName(DnsName name);

    /// <summary>Converts a Unicode domain name through IDNA.</summary>
    /// <param name="text">The Unicode domain name.</param>
    /// <param name="origin">The optional origin for a relative name.</param>
    /// <returns>The ASCII-compatible DNS name.</returns>
    DnsName ConvertIdnaToName(string text, DnsName? origin);

    /// <summary>Converts an IDNA DNS name to Unicode display text.</summary>
    /// <param name="name">The ASCII-compatible DNS name.</param>
    /// <returns>The Unicode display form.</returns>
    string ConvertNameToUnicode(DnsName name);

    /// <summary>Builds an in-addr.arpa name from four IPv4 address octets.</summary>
    /// <param name="address">The IPv4 address octets.</param>
    /// <returns>The absolute reverse-map name.</returns>
    DnsName BuildIpv4ReverseName(byte[] address);

    /// <summary>Builds an ip6.arpa name from sixteen IPv6 address octets.</summary>
    /// <param name="address">The IPv6 address octets.</param>
    /// <returns>The absolute nibble-form reverse-map name.</returns>
    DnsName BuildIpv6ReverseName(byte[] address);

    /// <summary>Parses an in-addr.arpa name into four IPv4 address octets.</summary>
    /// <param name="name">The absolute IPv4 reverse-map name.</param>
    /// <returns>The IPv4 address octets.</returns>
    byte[] ParseIpv4ReverseAddress(DnsName name);

    /// <summary>Parses an ip6.arpa name into sixteen IPv6 address octets.</summary>
    /// <param name="name">The absolute IPv6 reverse-map name.</param>
    /// <returns>The IPv6 address octets.</returns>
    byte[] ParseIpv6ReverseAddress(DnsName name);
}
