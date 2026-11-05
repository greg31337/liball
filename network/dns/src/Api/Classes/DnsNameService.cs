using System;

namespace dns;

/// <summary>Validates, converts, compares, and wire-encodes DNS names.</summary>
public class DnsNameService : IDnsNameService
{
    /// <summary>Creates a DNS name service.</summary>
    public DnsNameService()
    {
    }

    /// <summary>Validates one DNS name.</summary>
    /// <param name="name">The name to validate.</param>
    /// <returns>An independently mutable validated name.</returns>
    /// <exception cref="DnsFormatException">The name is invalid.</exception>
    public DnsName ValidateName(DnsName name)
    {
        return DnsNameServiceOperations.RequireRawName(
            RawDnsName.ValidateName(name),
            "The DNS name is invalid.");
    }

    /// <summary>Parses one presentation-format DNS name.</summary>
    /// <param name="text">The escaped name text.</param>
    /// <param name="origin">The optional absolute origin.</param>
    /// <returns>The parsed DNS name.</returns>
    /// <exception cref="DnsFormatException">The text is invalid.</exception>
    public DnsName ParseName(string text, DnsName? origin)
    {
        DnsTextResult result = this.TryParseName(text, origin);
        return DnsNameServiceOperations.RequireTextName(
            result, "The DNS name text is invalid.");
    }

    /// <summary>Attempts to parse one presentation-format DNS name.</summary>
    /// <param name="text">The escaped name text.</param>
    /// <param name="origin">The optional absolute origin.</param>
    /// <returns>The parsed name or diagnostics.</returns>
    public DnsTextResult TryParseName(string text, DnsName? origin)
    {
        return PresentationDnsName.ParseName(text, origin);
    }

    /// <summary>Formats one DNS name as escaped presentation text.</summary>
    /// <param name="name">The name to format.</param>
    /// <param name="options">The presentation policy.</param>
    /// <returns>The formatted name.</returns>
    /// <exception cref="DnsFormatException">The name or options are invalid.</exception>
    public string FormatName(DnsName name, DnsTextOptions options)
    {
        if (options == null)
        {
            throw new DnsFormatException("DNS text options are required.");
        }

        return DnsNameServiceOperations.RequireTextValue(
            DnsPresentationNameText.FormatName(name, options),
            "The DNS name could not be formatted.");
    }

    /// <summary>Strictly decodes one possibly compressed wire-format name.</summary>
    /// <param name="message">The complete containing DNS message.</param>
    /// <param name="offset">The name starting offset.</param>
    /// <param name="options">The decode limits.</param>
    /// <returns>The successful decoded name result.</returns>
    /// <exception cref="DnsFormatException">The wire name is malformed.</exception>
    public DnsDecodeResult DecodeName(
        ReadOnlySpan<byte> message,
        int offset,
        DnsDecodeOptions options)
    {
        DnsDecodeResult result = this.TryDecodeName(
            message, offset, options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The wire-format DNS name could not be decoded."));
        }

        return result;
    }

    /// <summary>Attempts to decode one possibly compressed wire-format name.</summary>
    /// <param name="message">The complete containing DNS message.</param>
    /// <param name="offset">The name starting offset.</param>
    /// <param name="options">The decode limits.</param>
    /// <returns>The decoded name or diagnostics.</returns>
    public DnsDecodeResult TryDecodeName(
        ReadOnlySpan<byte> message,
        int offset,
        DnsDecodeOptions options)
    {
        return CompressedDnsNameDecoder.DecodeName(
            message, offset, options);
    }

    /// <summary>Strictly encodes one standalone wire-format DNS name.</summary>
    /// <param name="name">The absolute name to encode.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The successful encoded name result.</returns>
    /// <exception cref="DnsFormatException">The name cannot be encoded.</exception>
    public DnsEncodeResult EncodeName(
        DnsName name,
        DnsEncodeOptions options)
    {
        DnsEncodeResult result = this.TryEncodeName(name, options);
        if (!result.Success)
        {
            throw new DnsFormatException(
                CodecEntryPointOperations.ReadDiagnosticMessage(
                    result.Diagnostics,
                    "The wire-format DNS name could not be encoded."));
        }

        return result;
    }

    /// <summary>Attempts to encode one standalone wire-format DNS name.</summary>
    /// <param name="name">The absolute name to encode.</param>
    /// <param name="options">The canonical and size policy.</param>
    /// <returns>The encoded bytes or diagnostics.</returns>
    public DnsEncodeResult TryEncodeName(
        DnsName name,
        DnsEncodeOptions options)
    {
        return StandaloneDnsNameCodec.EncodeName(name, options);
    }

    /// <summary>Tests two names for DNS case-insensitive equality.</summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns>True when the names are equal.</returns>
    public bool NamesEqual(DnsName left, DnsName right)
    {
        return DnsNameServiceOperations.RequireOperationBoolean(
            DnsNameOperations.NamesEqual(left, right),
            "The DNS names could not be compared.");
    }

    /// <summary>Compares two names in canonical DNS order.</summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns>The signed ordering value.</returns>
    public int CompareNames(DnsName left, DnsName right)
    {
        return DnsNameServiceOperations.RequireOperationComparison(
            DnsNameOperations.CompareNames(left, right),
            "The DNS names could not be ordered.");
    }

    /// <summary>Tests whether one name has a supplied suffix.</summary>
    /// <param name="name">The complete name.</param>
    /// <param name="suffix">The candidate suffix.</param>
    /// <returns>True when the suffix matches.</returns>
    public bool HasNameSuffix(DnsName name, DnsName suffix)
    {
        return DnsNameServiceOperations.RequireOperationBoolean(
            DnsNameOperations.HasNameSuffix(name, suffix),
            "The DNS name suffix could not be tested.");
    }

    /// <summary>Tests whether one name is a strict ancestor of another.</summary>
    /// <param name="ancestor">The candidate ancestor.</param>
    /// <param name="name">The candidate descendant.</param>
    /// <returns>True when the ancestor is strict.</returns>
    public bool IsAncestorName(DnsName ancestor, DnsName name)
    {
        return DnsNameServiceOperations.RequireOperationBoolean(
            DnsNameOperations.IsAncestorName(ancestor, name),
            "The DNS name ancestry could not be tested.");
    }

    /// <summary>Counts the labels in one DNS name.</summary>
    /// <param name="name">The name to inspect.</param>
    /// <returns>The label count.</returns>
    public int CountNameLabels(DnsName name)
    {
        return DnsNameServiceOperations.RequireOperationLabelCount(
            DnsNameOperations.CountNameLabels(name),
            "The DNS name labels could not be counted.");
    }

    /// <summary>Returns the immediate parent of a non-root name.</summary>
    /// <param name="name">The child name.</param>
    /// <returns>The parent name.</returns>
    public DnsName GetParentName(DnsName name)
    {
        return DnsNameServiceOperations.RequireOperationName(
            DnsNameOperations.GetParentName(name),
            "The DNS parent name is unavailable.");
    }

    /// <summary>Appends an absolute or relative suffix to a relative prefix.</summary>
    /// <param name="prefix">The relative leading name.</param>
    /// <param name="suffix">The trailing suffix.</param>
    /// <returns>The combined name.</returns>
    public DnsName ConcatenateNames(DnsName prefix, DnsName suffix)
    {
        return DnsNameServiceOperations.RequireOperationName(
            DnsNameOperations.ConcatenateNames(prefix, suffix),
            "The DNS names could not be concatenated.");
    }

    /// <summary>Removes an origin suffix from an absolute name.</summary>
    /// <param name="name">The absolute name.</param>
    /// <param name="origin">The absolute origin suffix.</param>
    /// <returns>The relative name.</returns>
    public DnsName RelativizeName(DnsName name, DnsName origin)
    {
        return DnsNameServiceOperations.RequireOperationName(
            DnsNameOperations.RelativizeName(name, origin),
            "The DNS name could not be relativized.");
    }

    /// <summary>Finds the longest suffix common to two names.</summary>
    /// <param name="first">The first name.</param>
    /// <param name="second">The second name.</param>
    /// <returns>The common ancestor.</returns>
    public DnsName FindCommonAncestor(DnsName first, DnsName second)
    {
        return DnsNameServiceOperations.RequireOperationName(
            DnsNameOperations.FindCommonAncestor(first, second),
            "The common DNS ancestor is unavailable.");
    }

    /// <summary>Creates a lowercase canonical copy of one DNS name.</summary>
    /// <param name="name">The case-preserving source name.</param>
    /// <returns>The canonical name.</returns>
    public DnsName CreateCanonicalName(DnsName name)
    {
        DnsName validated = this.ValidateName(name);
        return RecordSetOperations.CopyName(validated, true);
    }

    /// <summary>Converts a Unicode domain name through IDNA.</summary>
    /// <param name="text">The Unicode domain text.</param>
    /// <param name="origin">The optional absolute origin.</param>
    /// <returns>The ASCII-compatible DNS name.</returns>
    public DnsName ConvertIdnaToName(string text, DnsName? origin)
    {
        return DnsNameServiceOperations.RequireTextName(
            IdnaDnsName.ConvertIdnaToName(text, origin),
            "The IDNA name could not be converted.");
    }

    /// <summary>Converts an IDNA DNS name to Unicode display text.</summary>
    /// <param name="name">The ASCII-compatible DNS name.</param>
    /// <returns>The Unicode display text.</returns>
    public string ConvertNameToUnicode(DnsName name)
    {
        return DnsNameServiceOperations.RequireTextValue(
            IdnaDnsName.ConvertNameToUnicode(name),
            "The IDNA name could not be displayed.");
    }

    /// <summary>Builds an IPv4 reverse-map name.</summary>
    /// <param name="address">The four address octets.</param>
    /// <returns>The absolute reverse-map name.</returns>
    public DnsName BuildIpv4ReverseName(byte[] address)
    {
        return DnsNameServiceOperations.RequireReverseName(
            ReverseDnsName.BuildIpv4ReverseName(address),
            "The IPv4 reverse-map name could not be built.");
    }

    /// <summary>Builds an IPv6 reverse-map name.</summary>
    /// <param name="address">The sixteen address octets.</param>
    /// <returns>The absolute reverse-map name.</returns>
    public DnsName BuildIpv6ReverseName(byte[] address)
    {
        return DnsNameServiceOperations.RequireReverseName(
            ReverseDnsName.BuildIpv6ReverseName(address),
            "The IPv6 reverse-map name could not be built.");
    }

    /// <summary>Parses an IPv4 reverse-map name.</summary>
    /// <param name="name">The reverse-map name.</param>
    /// <returns>The four address octets.</returns>
    public byte[] ParseIpv4ReverseAddress(DnsName name)
    {
        return DnsNameServiceOperations.RequireReverseAddress(
            ReverseDnsName.ParseIpv4ReverseAddress(name),
            "The IPv4 reverse-map name could not be parsed.");
    }

    /// <summary>Parses an IPv6 reverse-map name.</summary>
    /// <param name="name">The reverse-map name.</param>
    /// <returns>The sixteen address octets.</returns>
    public byte[] ParseIpv6ReverseAddress(DnsName name)
    {
        return DnsNameServiceOperations.RequireReverseAddress(
            ReverseDnsName.ParseIpv6ReverseAddress(name),
            "The IPv6 reverse-map name could not be parsed.");
    }
}
