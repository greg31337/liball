using System;
using System.Collections.Generic;

namespace uri;

internal static class UriNormalizerCore
{
    internal static UriNormalizationChange[] AnalyzeNormalizationChanges(
        UriReference reference,
        UriSchemeProfile? profile)
    {
        UriNormalizationChange[] syntaxChanges = AnalyzeSyntaxChanges(reference);
        if (profile == null)
        {
            return syntaxChanges;
        }

        List<UriNormalizationChange> changes =
            new List<UriNormalizationChange>(syntaxChanges);
        UriReference normalized = NormalizeSyntaxReference(reference);
        if (!normalized.HasScheme ||
            !AsciiCase.EqualsIgnoreCase(normalized.Scheme, profile.Scheme))
        {
            return changes.ToArray();
        }

        if (profile.NormalizeEmptyAuthorityPath &&
            normalized.HasAuthority &&
            normalized.Path.Length == 0)
        {
            changes.Add(UriNormalizationChange.AuthorityPath);
        }

        UriAuthority authority = normalized.Authority;
        if (authority.HasPort)
        {
            if (profile.RemoveEmptyPort && authority.Port.Length == 0)
            {
                changes.Add(UriNormalizationChange.EmptyPort);
            }
            else if (profile.HasDefaultPort &&
                PortMatchesDefault(authority.Port.AsSpan(), profile.DefaultPort))
            {
                changes.Add(UriNormalizationChange.DefaultPort);
            }
        }

        if (profile.RemoveEmptyQuery &&
            normalized.HasQuery &&
            normalized.Query.Length == 0)
        {
            changes.Add(UriNormalizationChange.EmptyQuery);
        }

        if (profile.RemoveEmptyFragment &&
            normalized.HasFragment &&
            normalized.Fragment.Length == 0)
        {
            changes.Add(UriNormalizationChange.EmptyFragment);
        }

        return changes.ToArray();
    }

    internal static UriNormalizationChange[] AnalyzeSyntaxChanges(
        UriReference? reference)
    {
        if (reference == null || reference.Authority == null)
        {
            throw new ArgumentException("A URI reference is required.");
        }

        List<UriNormalizationChange> changes = new List<UriNormalizationChange>();
        if (reference.HasScheme)
        {
            string scheme = NormalizeSchemeCase(reference.Scheme.AsSpan());
            if (scheme != reference.Scheme)
            {
                changes.Add(UriNormalizationChange.SchemeCase);
            }
        }

        UriAuthority authority = reference.Authority;
        if (reference.HasAuthority && authority.HasUserInfo)
        {
            string userInfo = NormalizeRequiredPercentComponent(
                authority.UserInfo.AsSpan(),
                UriComponent.UserInfo);
            if (userInfo != authority.UserInfo)
            {
                changes.Add(UriNormalizationChange.UserInfoPercentEncoding);
            }
        }

        if (reference.HasAuthority &&
            authority.HostKind == UriHostKind.RegisteredName)
        {
            string percentHost = NormalizeRequiredPercentComponent(
                authority.Host.AsSpan(),
                UriComponent.Host);
            if (percentHost != authority.Host)
            {
                changes.Add(UriNormalizationChange.HostPercentEncoding);
            }

            string caseHost = NormalizeRegisteredNameCase(percentHost.AsSpan());
            if (caseHost != percentHost)
            {
                changes.Add(UriNormalizationChange.HostCase);
            }
        }
        else if (reference.HasAuthority &&
            authority.HostKind == UriHostKind.Ipv4)
        {
            string ipv4Host = NormalizeIpv4Address(authority.AddressBytes);
            if (ipv4Host != authority.Host)
            {
                changes.Add(UriNormalizationChange.HostAddress);
            }
        }
        else if (reference.HasAuthority &&
            authority.HostKind == UriHostKind.Ipv6)
        {
            string ipv6Host = NormalizeIpv6Address(authority.AddressBytes);
            if (ipv6Host != authority.Host)
            {
                changes.Add(UriNormalizationChange.HostAddress);
            }
        }

        string percentPath = NormalizeRequiredPercentComponent(
            reference.Path.AsSpan(),
            UriComponent.Path);
        if (percentPath != reference.Path)
        {
            changes.Add(UriNormalizationChange.PathPercentEncoding);
        }

        string dotPath = PathAlgorithm.RemoveDotSegments(percentPath.AsSpan());
        string unambiguousPath = PreserveRelativePathSyntax(reference, dotPath);
        if (unambiguousPath != percentPath)
        {
            changes.Add(UriNormalizationChange.PathDotSegments);
        }

        if (reference.HasQuery)
        {
            string query = NormalizeRequiredPercentComponent(
                reference.Query.AsSpan(),
                UriComponent.Query);
            if (query != reference.Query)
            {
                changes.Add(UriNormalizationChange.QueryPercentEncoding);
            }
        }

        if (reference.HasFragment)
        {
            string fragment = NormalizeRequiredPercentComponent(
                reference.Fragment.AsSpan(),
                UriComponent.Fragment);
            if (fragment != reference.Fragment)
            {
                changes.Add(UriNormalizationChange.FragmentPercentEncoding);
            }
        }

        return changes.ToArray();
    }

    internal static UriSchemeProfile? CreateBuiltInSchemeProfile(string? scheme)
    {
        if (AsciiCase.EqualsIgnoreCase(scheme, UriSchemeDefaults.HttpScheme))
        {
            return CreateDefaultPortSchemeProfile(
                UriSchemeDefaults.HttpScheme,
                UriSchemeDefaults.HttpDefaultPort);
        }

        if (AsciiCase.EqualsIgnoreCase(scheme, UriSchemeDefaults.HttpsScheme))
        {
            return CreateDefaultPortSchemeProfile(
                UriSchemeDefaults.HttpsScheme,
                UriSchemeDefaults.HttpsDefaultPort);
        }

        if (AsciiCase.EqualsIgnoreCase(scheme, UriSchemeDefaults.WebSocketScheme))
        {
            return CreateDefaultPortSchemeProfile(
                UriSchemeDefaults.WebSocketScheme,
                UriSchemeDefaults.WebSocketDefaultPort);
        }

        if (AsciiCase.EqualsIgnoreCase(
            scheme,
            UriSchemeDefaults.SecureWebSocketScheme))
        {
            return CreateDefaultPortSchemeProfile(
                UriSchemeDefaults.SecureWebSocketScheme,
                UriSchemeDefaults.SecureWebSocketDefaultPort);
        }

        if (AsciiCase.EqualsIgnoreCase(scheme, UriSchemeDefaults.FtpScheme))
        {
            return CreateDefaultPortSchemeProfile(
                UriSchemeDefaults.FtpScheme,
                UriSchemeDefaults.FtpDefaultPort);
        }

        return null;
    }

    internal static UriReference NormalizeWithBuiltInSchemeProfile(
        UriReference? reference)
    {
        if (reference == null)
        {
            throw new ArgumentException("A URI reference is required.");
        }

        UriSchemeProfile? profile = null;
        if (reference.HasScheme)
        {
            profile = CreateBuiltInSchemeProfile(reference.Scheme);
        }

        if (profile == null)
        {
            return NormalizeSyntaxReference(reference);
        }

        return NormalizeWithSchemeProfile(reference, profile);
    }

    internal static UriReference NormalizeWithSchemeProfile(
        UriReference? reference,
        UriSchemeProfile? profile)
    {
        if (profile == null)
        {
            throw new ArgumentException("A URI scheme profile is required.");
        }

        UriReference normalized = NormalizeSyntaxReference(reference);
        if (!normalized.HasScheme ||
            !AsciiCase.EqualsIgnoreCase(normalized.Scheme, profile.Scheme))
        {
            return normalized;
        }

        if (profile.NormalizeEmptyAuthorityPath &&
            normalized.HasAuthority &&
            normalized.Path.Length == 0)
        {
            normalized.Path = "/";
        }

        UriAuthority authority = normalized.Authority;
        if (authority.HasPort)
        {
            bool removePort = profile.RemoveEmptyPort &&
                authority.Port.Length == 0;
            if (!removePort && profile.HasDefaultPort)
            {
                removePort = PortMatchesDefault(
                    authority.Port.AsSpan(),
                    profile.DefaultPort);
            }

            if (removePort)
            {
                authority.HasPort = false;
                authority.Port = "";
            }
        }

        if (profile.RemoveEmptyQuery &&
            normalized.HasQuery &&
            normalized.Query.Length == 0)
        {
            normalized.HasQuery = false;
            normalized.Query = "";
        }

        if (profile.RemoveEmptyFragment &&
            normalized.HasFragment &&
            normalized.Fragment.Length == 0)
        {
            normalized.HasFragment = false;
            normalized.Fragment = "";
        }

        normalized.Kind = ClassifyNormalizedReference(normalized, normalized.Kind);
        return normalized;
    }

    internal static UriReference NormalizeSyntaxReference(UriReference? reference)
    {
        if (reference == null || reference.Authority == null)
        {
            throw new ArgumentException("A URI reference is required.");
        }

        UriReference normalized = new UriReference();
        normalized.HasScheme = reference.HasScheme;
        if (reference.HasScheme)
        {
            normalized.Scheme = NormalizeSchemeCase(reference.Scheme.AsSpan());
        }

        normalized.HasAuthority = reference.HasAuthority;
        UriAuthority sourceAuthority = reference.Authority;
        UriAuthority normalizedAuthority = normalized.Authority;
        normalizedAuthority.HasUserInfo = sourceAuthority.HasUserInfo;
        if (sourceAuthority.HasUserInfo)
        {
            normalizedAuthority.UserInfo = NormalizeRequiredPercentComponent(
                sourceAuthority.UserInfo.AsSpan(),
                UriComponent.UserInfo);
        }

        normalizedAuthority.HostKind = sourceAuthority.HostKind;
        if (sourceAuthority.HostKind == UriHostKind.RegisteredName)
        {
            PercentNormalizeResult host = NormalizeRegisteredName(
                sourceAuthority.Host.AsSpan(),
                int.MaxValue,
                0);
            ThrowForInvalidAnalysisComponent(host);
            normalizedAuthority.Host = host.Text;
        }
        else if (sourceAuthority.HostKind == UriHostKind.Ipv4)
        {
            normalizedAuthority.Host = NormalizeIpv4Address(
                sourceAuthority.AddressBytes);
        }
        else if (sourceAuthority.HostKind == UriHostKind.Ipv6)
        {
            normalizedAuthority.Host = NormalizeIpv6Address(
                sourceAuthority.AddressBytes);
        }
        else
        {
            normalizedAuthority.Host = sourceAuthority.Host;
        }

        normalizedAuthority.AddressBytes = CopyAddressBytes(
            sourceAuthority.AddressBytes);
        normalizedAuthority.HasPort = sourceAuthority.HasPort;
        if (sourceAuthority.HasPort)
        {
            normalizedAuthority.Port = sourceAuthority.Port;
        }

        string percentNormalizedPath = NormalizeRequiredPercentComponent(
            reference.Path.AsSpan(),
            UriComponent.Path);
        string dotNormalizedPath = PathAlgorithm.RemoveDotSegments(
            percentNormalizedPath.AsSpan());
        normalized.Path = PreserveRelativePathSyntax(
            reference,
            dotNormalizedPath);

        normalized.HasQuery = reference.HasQuery;
        if (reference.HasQuery)
        {
            normalized.Query = NormalizeRequiredPercentComponent(
                reference.Query.AsSpan(),
                UriComponent.Query);
        }

        normalized.HasFragment = reference.HasFragment;
        if (reference.HasFragment)
        {
            normalized.Fragment = NormalizeRequiredPercentComponent(
                reference.Fragment.AsSpan(),
                UriComponent.Fragment);
        }

        normalized.OriginalText = reference.OriginalText;
        normalized.Kind = ClassifyNormalizedReference(normalized, reference.Kind);
        return normalized;
    }

    internal static UriComponent[] AnalyzeSyntaxNormalization(UriReference? reference)
    {
        if (reference == null || reference.Authority == null)
        {
            throw new ArgumentException("A URI reference is required.");
        }

        List<UriComponent> changes = new List<UriComponent>();
        if (reference.HasScheme)
        {
            string normalizedScheme = NormalizeSchemeCase(reference.Scheme.AsSpan());
            if (normalizedScheme != reference.Scheme)
            {
                changes.Add(UriComponent.Scheme);
            }
        }

        if (reference.HasAuthority)
        {
            UriAuthority authority = reference.Authority;
            if (authority.HasUserInfo)
            {
                PercentNormalizeResult userInfo = NormalizeComponentPercentEncoding(
                    authority.UserInfo.AsSpan(),
                    int.MaxValue,
                    UriComponent.UserInfo,
                    0);
                ThrowForInvalidAnalysisComponent(userInfo);
                if (userInfo.Text != authority.UserInfo)
                {
                    changes.Add(UriComponent.UserInfo);
                }
            }

            string normalizedHost = authority.Host;
            if (authority.HostKind == UriHostKind.RegisteredName)
            {
                PercentNormalizeResult host = NormalizeRegisteredName(
                    authority.Host.AsSpan(),
                    int.MaxValue,
                    0);
                ThrowForInvalidAnalysisComponent(host);
                normalizedHost = host.Text;
            }
            else if (authority.HostKind == UriHostKind.Ipv4)
            {
                normalizedHost = NormalizeIpv4Address(authority.AddressBytes);
            }
            else if (authority.HostKind == UriHostKind.Ipv6)
            {
                normalizedHost = NormalizeIpv6Address(authority.AddressBytes);
            }

            if (normalizedHost != authority.Host)
            {
                changes.Add(UriComponent.Host);
            }
        }

        PercentNormalizeResult path = NormalizeComponentPercentEncoding(
            reference.Path.AsSpan(),
            int.MaxValue,
            UriComponent.Path,
            0);
        ThrowForInvalidAnalysisComponent(path);
        string dotNormalizedPath = PathAlgorithm.RemoveDotSegments(
            path.Text.AsSpan());
        string normalizedPath = PreserveRelativePathSyntax(
            reference,
            dotNormalizedPath);
        if (normalizedPath != reference.Path)
        {
            changes.Add(UriComponent.Path);
        }

        if (reference.HasQuery)
        {
            PercentNormalizeResult query = NormalizeComponentPercentEncoding(
                reference.Query.AsSpan(),
                int.MaxValue,
                UriComponent.Query,
                0);
            ThrowForInvalidAnalysisComponent(query);
            if (query.Text != reference.Query)
            {
                changes.Add(UriComponent.Query);
            }
        }

        if (reference.HasFragment)
        {
            PercentNormalizeResult fragment = NormalizeComponentPercentEncoding(
                reference.Fragment.AsSpan(),
                int.MaxValue,
                UriComponent.Fragment,
                0);
            ThrowForInvalidAnalysisComponent(fragment);
            if (fragment.Text != reference.Fragment)
            {
                changes.Add(UriComponent.Fragment);
            }
        }

        return changes.ToArray();
    }

    internal static string NormalizeIpv4Address(ReadOnlySpan<byte> addressBytes)
    {
        if (addressBytes.Length != 4)
        {
            throw new ArgumentException("An IPv4 address must contain four bytes.");
        }

        char[] normalized = new char[15];
        int outputOffset = 0;
        int byteOffset = 0;
        while (byteOffset < addressBytes.Length)
        {
            if (byteOffset > 0)
            {
                normalized[outputOffset] = '.';
                outputOffset = outputOffset + 1;
            }

            outputOffset = WriteDecimalByte(
                addressBytes[byteOffset],
                normalized,
                outputOffset);
            byteOffset = byteOffset + 1;
        }

        return new string(normalized, 0, outputOffset);
    }

    internal static string NormalizeIpv6Address(ReadOnlySpan<byte> addressBytes)
    {
        if (addressBytes.Length != 16)
        {
            throw new ArgumentException("An IPv6 address must contain sixteen bytes.");
        }

        int[] groups = new int[8];
        int groupOffset = 0;
        while (groupOffset < groups.Length)
        {
            int byteOffset = groupOffset * 2;
            groups[groupOffset] = addressBytes[byteOffset] * 256 +
                addressBytes[byteOffset + 1];
            groupOffset = groupOffset + 1;
        }

        bool useEmbeddedIpv4 = IsRecognizedEmbeddedIpv4Address(addressBytes);
        int hexadecimalGroupCount = 8;
        if (useEmbeddedIpv4)
        {
            hexadecimalGroupCount = 6;
        }

        int zeroRunStart = FindLongestZeroRunStart(
            groups,
            hexadecimalGroupCount);
        int zeroRunLength = CountZeroRun(
            groups,
            zeroRunStart,
            hexadecimalGroupCount);

        char[] normalized = new char[45];
        int outputOffset = 0;
        groupOffset = 0;
        while (groupOffset < hexadecimalGroupCount)
        {
            if (groupOffset == zeroRunStart)
            {
                normalized[outputOffset] = ':';
                normalized[outputOffset + 1] = ':';
                outputOffset = outputOffset + 2;
                groupOffset = groupOffset + zeroRunLength;
                continue;
            }

            if (groupOffset > 0 &&
                groupOffset != zeroRunStart + zeroRunLength)
            {
                normalized[outputOffset] = ':';
                outputOffset = outputOffset + 1;
            }

            outputOffset = WriteLowerHexGroup(
                groups[groupOffset],
                normalized,
                outputOffset);
            groupOffset = groupOffset + 1;
        }

        if (useEmbeddedIpv4)
        {
            if (outputOffset == 0 || normalized[outputOffset - 1] != ':')
            {
                normalized[outputOffset] = ':';
                outputOffset = outputOffset + 1;
            }

            int ipv4Offset = 12;
            while (ipv4Offset < addressBytes.Length)
            {
                if (ipv4Offset > 12)
                {
                    normalized[outputOffset] = '.';
                    outputOffset = outputOffset + 1;
                }

                outputOffset = WriteDecimalByte(
                    addressBytes[ipv4Offset],
                    normalized,
                    outputOffset);
                ipv4Offset = ipv4Offset + 1;
            }
        }

        return new string(normalized, 0, outputOffset);
    }

    internal static PercentNormalizeResult NormalizeComponentPercentEncoding(
        ReadOnlySpan<char> text,
        int maximumOutputCharacters,
        UriComponent component,
        int baseOffset)
    {
        return PercentNormalizerCore.Transform(
            text,
            maximumOutputCharacters,
            component,
            baseOffset,
            true,
            true);
    }

    internal static string NormalizeSchemeCase(ReadOnlySpan<char> scheme)
    {
        char[] normalized = new char[scheme.Length];
        int offset = 0;
        while (offset < scheme.Length)
        {
            normalized[offset] = AsciiCase.FoldToLower(scheme[offset]);
            offset = offset + 1;
        }

        return new string(normalized);
    }

    internal static string NormalizeHostCase(
        ReadOnlySpan<char> host,
        UriHostKind hostKind)
    {
        if (hostKind != UriHostKind.RegisteredName)
        {
            return host.ToString();
        }

        return NormalizeRegisteredNameCase(host);
    }

    internal static string NormalizeRegisteredNameCase(ReadOnlySpan<char> host)
    {
        char[] normalized = new char[host.Length];
        int offset = 0;
        while (offset < host.Length)
        {
            if (host[offset] == '%' &&
                offset <= host.Length - 3 &&
                Ascii.IsHexDigit(host[offset + 1]) &&
                Ascii.IsHexDigit(host[offset + 2]))
            {
                normalized[offset] = host[offset];
                normalized[offset + 1] = host[offset + 1];
                normalized[offset + 2] = host[offset + 2];
                offset = offset + 3;
                continue;
            }

            normalized[offset] = AsciiCase.FoldToLower(host[offset]);
            offset = offset + 1;
        }

        return new string(normalized);
    }

    internal static PercentNormalizeResult NormalizeRegisteredName(
        ReadOnlySpan<char> host,
        int maximumOutputCharacters,
        int baseOffset)
    {
        PercentNormalizeResult percentNormalized = NormalizeComponentPercentEncoding(
            host,
            maximumOutputCharacters,
            UriComponent.Host,
            baseOffset);
        if (!percentNormalized.Success)
        {
            return percentNormalized;
        }

        string normalized = NormalizeRegisteredNameCase(
            percentNormalized.Text.AsSpan());
        return new PercentNormalizeResult(true, normalized, null);
    }

    internal static int WriteDecimalByte(
        byte value,
        char[] output,
        int outputOffset)
    {
        if (value >= 100)
        {
            output[outputOffset] = (char)('0' + value / 100);
            output[outputOffset + 1] = (char)('0' + value / 10 % 10);
            output[outputOffset + 2] = (char)('0' + value % 10);
            return outputOffset + 3;
        }

        if (value >= 10)
        {
            output[outputOffset] = (char)('0' + value / 10);
            output[outputOffset + 1] = (char)('0' + value % 10);
            return outputOffset + 2;
        }

        output[outputOffset] = (char)('0' + value);
        return outputOffset + 1;
    }

    internal static int FindLongestZeroRunStart(int[] groups, int groupCount)
    {
        int bestStart = -1;
        int bestLength = 1;
        int groupOffset = 0;
        while (groupOffset < groupCount)
        {
            if (groups[groupOffset] != 0)
            {
                groupOffset = groupOffset + 1;
                continue;
            }

            int runStart = groupOffset;
            while (groupOffset < groupCount && groups[groupOffset] == 0)
            {
                groupOffset = groupOffset + 1;
            }

            int runLength = groupOffset - runStart;
            if (runLength > bestLength)
            {
                bestStart = runStart;
                bestLength = runLength;
            }
        }

        return bestStart;
    }

    internal static int CountZeroRun(
        int[] groups,
        int runStart,
        int groupCount)
    {
        if (runStart < 0)
        {
            return 0;
        }

        int runEnd = runStart;
        while (runEnd < groupCount && groups[runEnd] == 0)
        {
            runEnd = runEnd + 1;
        }

        return runEnd - runStart;
    }

    internal static int WriteLowerHexGroup(
        int value,
        char[] output,
        int outputOffset)
    {
        int divisor = 4096;
        bool hasDigit = false;
        while (divisor > 0)
        {
            int digit = value / divisor % 16;
            if (digit != 0 || hasDigit || divisor == 1)
            {
                output[outputOffset] = AsciiCase.FoldToLower(
                    Ascii.GetUpperHexCharacter(digit));
                outputOffset = outputOffset + 1;
                hasDigit = true;
            }

            divisor = divisor / 16;
        }

        return outputOffset;
    }

    internal static bool IsRecognizedEmbeddedIpv4Address(
        ReadOnlySpan<byte> addressBytes)
    {
        bool firstEightZero = AreZeroBytes(addressBytes, 0, 8);
        bool firstTenZero = firstEightZero &&
            addressBytes[8] == 0 &&
            addressBytes[9] == 0;
        bool firstTwelveZero = firstTenZero &&
            addressBytes[10] == 0 &&
            addressBytes[11] == 0;

        bool mapped = firstTenZero &&
            addressBytes[10] == 255 &&
            addressBytes[11] == 255;
        if (mapped)
        {
            return true;
        }

        bool translated = firstEightZero &&
            addressBytes[8] == 255 &&
            addressBytes[9] == 255 &&
            addressBytes[10] == 0 &&
            addressBytes[11] == 0;
        if (translated)
        {
            return true;
        }

        bool wellKnownTranslation = addressBytes[0] == 0 &&
            addressBytes[1] == 100 &&
            addressBytes[2] == 255 &&
            addressBytes[3] == 155 &&
            AreZeroBytes(addressBytes, 4, 8);
        if (wellKnownTranslation)
        {
            return true;
        }

        bool isatap = (addressBytes[8] == 0 || addressBytes[8] == 2) &&
            addressBytes[9] == 0 &&
            addressBytes[10] == 94 &&
            addressBytes[11] == 254;
        if (isatap)
        {
            return true;
        }

        if (!firstTwelveZero)
        {
            return false;
        }

        bool unspecifiedOrLoopback = addressBytes[12] == 0 &&
            addressBytes[13] == 0 &&
            addressBytes[14] == 0 &&
            addressBytes[15] <= 1;
        return !unspecifiedOrLoopback;
    }

    internal static bool AreZeroBytes(
        ReadOnlySpan<byte> addressBytes,
        int startOffset,
        int count)
    {
        int offset = startOffset;
        int endOffset = startOffset + count;
        while (offset < endOffset)
        {
            if (addressBytes[offset] != 0)
            {
                return false;
            }

            offset = offset + 1;
        }

        return true;
    }

    internal static void ThrowForInvalidAnalysisComponent(
        PercentNormalizeResult result)
    {
        if (!result.Success)
        {
            throw new ArgumentException(
                "The URI reference must contain valid percent-encoded components.");
        }
    }

    internal static string NormalizeRequiredPercentComponent(
        ReadOnlySpan<char> text,
        UriComponent component)
    {
        PercentNormalizeResult result = NormalizeComponentPercentEncoding(
            text,
            int.MaxValue,
            component,
            0);
        ThrowForInvalidAnalysisComponent(result);
        return result.Text;
    }

    internal static byte[] CopyAddressBytes(byte[]? addressBytes)
    {
        if (addressBytes == null)
        {
            throw new ArgumentException("URI address bytes are required.");
        }

        byte[] copy = new byte[addressBytes.Length];
        int offset = 0;
        while (offset < addressBytes.Length)
        {
            copy[offset] = addressBytes[offset];
            offset = offset + 1;
        }

        return copy;
    }

    internal static UriReferenceKind ClassifyNormalizedReference(
        UriReference reference,
        UriReferenceKind sourceKind)
    {
        if (reference.HasScheme)
        {
            if (sourceKind == UriReferenceKind.AbsoluteUri)
            {
                return UriReferenceKind.AbsoluteUri;
            }

            return UriReferenceKind.Uri;
        }

        if (reference.HasAuthority)
        {
            return UriReferenceKind.NetworkPathReference;
        }

        if (reference.Path.Length > 0)
        {
            if (reference.Path[0] == '/')
            {
                return UriReferenceKind.AbsolutePathReference;
            }

            return UriReferenceKind.RelativePathReference;
        }

        if (reference.HasQuery)
        {
            return UriReferenceKind.RelativeReference;
        }

        if (reference.HasFragment)
        {
            return UriReferenceKind.SameDocumentReference;
        }

        return UriReferenceKind.EmptyReference;
    }

    internal static string PreserveRelativePathSyntax(
        UriReference reference,
        string normalizedPath)
    {
        if (reference.HasScheme ||
            reference.HasAuthority ||
            normalizedPath.Length == 0 ||
            normalizedPath[0] == '/')
        {
            return normalizedPath;
        }

        int offset = 0;
        while (offset < normalizedPath.Length && normalizedPath[offset] != '/')
        {
            if (normalizedPath[offset] == ':')
            {
                return "./" + normalizedPath;
            }

            offset = offset + 1;
        }

        return normalizedPath;
    }

    internal static bool PortMatchesDefault(
        ReadOnlySpan<char> port,
        int defaultPort)
    {
        if (port.Length == 0 || defaultPort < 0)
        {
            return false;
        }

        int value = 0;
        int offset = 0;
        while (offset < port.Length)
        {
            char character = port[offset];
            if (!Ascii.IsDigit(character))
            {
                return false;
            }

            int digit = character - '0';
            if (value > (int.MaxValue - digit) / 10)
            {
                return false;
            }

            value = value * 10 + digit;
            offset = offset + 1;
        }

        return value == defaultPort;
    }

    internal static UriSchemeProfile CreateDefaultPortSchemeProfile(
        string scheme,
        int defaultPort)
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = scheme;
        profile.NormalizeEmptyAuthorityPath = true;
        profile.RemoveEmptyPort = true;
        profile.HasDefaultPort = true;
        profile.DefaultPort = defaultPort;
        profile.RemoveEmptyQuery = false;
        profile.RemoveEmptyFragment = false;
        return profile;
    }

    internal static bool IsValidSchemeProfile(UriSchemeProfile? profile)
    {
        if (profile == null || profile.Scheme == null)
        {
            return false;
        }

        UriDiagnostic? schemeFailure = UriValidatorCore.ValidateSchemeValue(
            profile.Scheme.AsSpan());
        if (schemeFailure != null)
        {
            return false;
        }

        if (profile.HasDefaultPort &&
            (profile.DefaultPort < 0 || profile.DefaultPort > 65535))
        {
            return false;
        }

        return true;
    }
}
