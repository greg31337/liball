using System;
using System.Collections.Generic;
using System.Text;

namespace htmlgui;

internal class RouteUrlResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string BasePath = string.Empty;
    internal string CanonicalUrl = string.Empty;
    internal string Path = string.Empty;
    internal string[] PathSegments = Array.Empty<string>();
    internal UiValueEntry[] Query = Array.Empty<UiValueEntry>();
    internal string? Fragment;
    internal string Scheme = string.Empty;
    internal bool IsExternal;
}

internal class RouteSegmentResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string Text = string.Empty;
}

internal class RoutePathResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string[] Segments = Array.Empty<string>();
}

internal class RouteQueryResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiValueEntry[] Entries = Array.Empty<UiValueEntry>();
}

internal class RouteValueResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string Text = string.Empty;
}

internal class RouteCore
{
    internal const int MaximumRouteUrlLength = 8192;
    internal const int MaximumRouteSegmentCount = 256;
    internal const int MaximumRouteComponentLength = 2048;

    internal RouteUrlResult ParseRouteUrl(string? url, string? basePath)
    {
        RouteUrlResult normalizedBase = CanonicalizeRouteBasePath(basePath);
        if (!normalizedBase.Success)
        {
            return normalizedBase;
        }

        if (url == null || url.Length == 0)
        {
            return CreateRouteUrlFailure("route.invalid-url");
        }

        if (url.Length > MaximumRouteUrlLength || ContainsInvalidRouteText(url))
        {
            return CreateRouteUrlFailure("route.invalid-url");
        }

        if (url.Length >= 2 && url[0] == '/' && url[1] == '/')
        {
            return ParseSchemeRelativeRouteUrl(url, normalizedBase.BasePath);
        }

        int schemeSeparator = FindRouteSchemeSeparator(url);
        if (schemeSeparator >= 0)
        {
            return ParseExternalRouteUrl(url, schemeSeparator, normalizedBase.BasePath);
        }

        return ParseInternalRouteUrl(url, normalizedBase);
    }

    internal RouteUrlResult CanonicalizeRouteBasePath(string? basePath)
    {
        string supplied = basePath ?? "/";
        if (supplied.Length == 0)
        {
            supplied = "/";
        }

        if (supplied.Length > MaximumRouteUrlLength || ContainsInvalidRouteText(supplied) || supplied.IndexOf('?') >= 0 || supplied.IndexOf('#') >= 0 || supplied.IndexOf('\\') >= 0 || FindRouteSchemeSeparator(supplied) >= 0)
        {
            return CreateRouteUrlFailure("route.invalid-base-path");
        }

        if (supplied.Length >= 2 && supplied[0] == '/' && supplied[1] == '/')
        {
            return CreateRouteUrlFailure("route.invalid-base-path");
        }

        string path = supplied[0] == '/' ? supplied : "/" + supplied;
        RoutePathResult parsed = NormalizeRoutePath(path, Array.Empty<string>(), 0);
        if (!parsed.Success)
        {
            RouteUrlResult failed = CreateRouteUrlFailure("route.invalid-base-path");
            failed.Code = parsed.Code == "route.path-escapes-base" ? "route.invalid-base-path" : parsed.Code;
            return failed;
        }

        string canonical = EncodeRoutePath(parsed.Segments);
        if (canonical.Length > 1)
        {
            canonical += "/";
        }

        RouteUrlResult result = new RouteUrlResult();
        result.Success = true;
        result.BasePath = canonical;
        result.CanonicalUrl = canonical;
        result.Path = "/";
        result.PathSegments = CopyRouteSegments(parsed.Segments);
        return result;
    }

    internal RouteValueResult FormatInvariantRouteValue(UiValue? value)
    {
        if (value == null)
        {
            return CreateRouteValueFailure("route.null-value");
        }

        RouteValueResult result = new RouteValueResult();
        result.Success = true;
        switch (value.Kind)
        {
            case UiValueKind.Null:
                result.Text = string.Empty;
                break;
            case UiValueKind.Boolean:
                result.Text = InvariantFormatter.FormatBoolean(value.BooleanValue);
                break;
            case UiValueKind.Integer:
                result.Text = InvariantFormatter.FormatInt64(value.IntegerValue);
                break;
            case UiValueKind.FloatingPoint:
                if (double.IsNaN(value.FloatingPointValue) || double.IsInfinity(value.FloatingPointValue))
                {
                    return CreateRouteValueFailure("route.non-finite-value");
                }

                result.Text = InvariantFormatter.FormatDouble(value.FloatingPointValue);
                break;
            case UiValueKind.Decimal:
                result.Text = InvariantFormatter.FormatDecimal(value.DecimalValue);
                break;
            case UiValueKind.String:
                if (value.StringValue == null)
                {
                    return CreateRouteValueFailure("route.null-string-value");
                }

                result.Text = value.StringValue;
                break;
            case UiValueKind.DateTime:
                if (value.DateTimeValue == null)
                {
                    return CreateRouteValueFailure("route.null-date-time-value");
                }

                result.Text = value.DateTimeValue;
                break;
            default:
                return CreateRouteValueFailure("route.unsupported-value-kind");
        }

        return result;
    }

    internal RouteUrlResult ParseInternalRouteUrl(string url, RouteUrlResult normalizedBase)
    {
        int fragmentIndex = url.IndexOf('#');
        string beforeFragment = fragmentIndex < 0 ? url : url.Substring(0, fragmentIndex);
        string? rawFragment = fragmentIndex < 0 ? null : url.Substring(fragmentIndex + 1);
        int queryIndex = beforeFragment.IndexOf('?');
        string rawPath = queryIndex < 0 ? beforeFragment : beforeFragment.Substring(0, queryIndex);
        string? rawQuery = queryIndex < 0 ? null : beforeFragment.Substring(queryIndex + 1);
        RouteQueryResult query = ParseRouteQuery(rawQuery);
        if (!query.Success)
        {
            return CreateRouteUrlFailure(query.Code);
        }

        RouteSegmentResult fragment = DecodeRouteComponent(rawFragment, false, false);
        if (!fragment.Success)
        {
            return CreateRouteUrlFailure(fragment.Code);
        }

        bool absolutePath = rawPath.Length > 0 && rawPath[0] == '/';
        RoutePathResult physical;
        if (absolutePath)
        {
            physical = NormalizeRoutePath(rawPath, Array.Empty<string>(), 0);
        }
        else
        {
            physical = NormalizeRoutePath(rawPath, normalizedBase.PathSegments, normalizedBase.PathSegments.Length);
        }

        if (!physical.Success)
        {
            return CreateRouteUrlFailure(physical.Code);
        }

        if (!RoutePathStartsWith(physical.Segments, normalizedBase.PathSegments))
        {
            return CreateRouteUrlFailure("route.outside-base-path");
        }

        string[] relativeSegments = SliceRouteSegments(physical.Segments, normalizedBase.PathSegments.Length);
        string canonicalPath = EncodeRoutePath(physical.Segments);
        if (relativeSegments.Length == 0 && normalizedBase.BasePath.Length > 1)
        {
            canonicalPath += "/";
        }

        RouteUrlResult result = new RouteUrlResult();
        result.Success = true;
        result.BasePath = normalizedBase.BasePath;
        result.PathSegments = relativeSegments;
        result.Path = JoinDecodedRoutePath(relativeSegments);
        result.Query = query.Entries;
        result.Fragment = rawFragment == null ? null : fragment.Text;
        result.CanonicalUrl = canonicalPath + EncodeRouteQuery(query.Entries) + EncodeRouteFragment(result.Fragment);
        return result;
    }

    internal RouteUrlResult ParseExternalRouteUrl(string url, int schemeSeparator, string basePath)
    {
        string scheme = url.Substring(0, schemeSeparator);
        if (!IsValidRouteScheme(scheme))
        {
            return CreateRouteUrlFailure("route.invalid-scheme");
        }

        string normalizedScheme = scheme.ToLowerInvariant();
        if (!IsSafeExternalRouteScheme(normalizedScheme))
        {
            return CreateRouteUrlFailure("route.unsafe-scheme");
        }

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return CreateRouteUrlFailure("route.invalid-external-url");
        }

        Uri parsed = new Uri(url, UriKind.Absolute);
        RouteUrlResult result = new RouteUrlResult();
        result.Success = true;
        result.BasePath = basePath;
        result.CanonicalUrl = parsed.AbsoluteUri;
        result.Scheme = normalizedScheme;
        result.IsExternal = true;
        return result;
    }

    internal RouteUrlResult ParseSchemeRelativeRouteUrl(string url, string basePath)
    {
        string absolute = "https:" + url;
        if (!Uri.IsWellFormedUriString(absolute, UriKind.Absolute))
        {
            return CreateRouteUrlFailure("route.invalid-external-url");
        }

        Uri parsed = new Uri(absolute, UriKind.Absolute);
        RouteUrlResult result = new RouteUrlResult();
        result.Success = true;
        result.BasePath = basePath;
        result.CanonicalUrl = parsed.AbsoluteUri.Substring("https:".Length);
        result.IsExternal = true;
        return result;
    }

    internal RoutePathResult NormalizeRoutePath(string rawPath, string[] prefixSegments, int minimumDepth)
    {
        List<string> segments = new List<string>();
        int prefixIndex = 0;
        while (prefixIndex < prefixSegments.Length)
        {
            segments.Add(prefixSegments[prefixIndex]);
            prefixIndex++;
        }

        string[] rawSegments = rawPath.Split('/');
        int index = 0;
        while (index < rawSegments.Length)
        {
            string rawSegment = rawSegments[index];
            if (rawSegment.Length > 0)
            {
                RouteSegmentResult decoded = DecodeRouteComponent(rawSegment, false, true);
                if (!decoded.Success)
                {
                    return CreateRoutePathFailure(decoded.Code);
                }

                if (ValueComparer.TextEquals(decoded.Text, ".."))
                {
                    if (segments.Count <= minimumDepth)
                    {
                        return CreateRoutePathFailure("route.path-escapes-base");
                    }

                    segments.RemoveAt(segments.Count - 1);
                }
                else if (!ValueComparer.TextEquals(decoded.Text, "."))
                {
                    if (segments.Count == MaximumRouteSegmentCount)
                    {
                        return CreateRoutePathFailure("route.too-many-segments");
                    }

                    segments.Add(decoded.Text);
                }
            }

            index++;
        }

        RoutePathResult result = new RoutePathResult();
        result.Success = true;
        result.Segments = segments.ToArray();
        return result;
    }

    internal RouteQueryResult ParseRouteQuery(string? rawQuery)
    {
        RouteQueryResult result = new RouteQueryResult();
        result.Success = true;
        if (rawQuery == null || rawQuery.Length == 0)
        {
            return result;
        }

        List<UiValueEntry> entries = new List<UiValueEntry>();
        string[] pairs = rawQuery.Split('&');
        int pairIndex = 0;
        while (pairIndex < pairs.Length)
        {
            string pair = pairs[pairIndex];
            if (pair.Length > 0)
            {
                int equalsIndex = pair.IndexOf('=');
                string rawKey = equalsIndex < 0 ? pair : pair.Substring(0, equalsIndex);
                string rawValue = equalsIndex < 0 ? string.Empty : pair.Substring(equalsIndex + 1);
                RouteSegmentResult key = DecodeRouteComponent(rawKey, true, false);
                RouteSegmentResult value = DecodeRouteComponent(rawValue, true, false);
                if (!key.Success)
                {
                    return CreateRouteQueryFailure(key.Code);
                }

                if (!value.Success)
                {
                    return CreateRouteQueryFailure(value.Code);
                }

                if (key.Text.Length == 0)
                {
                    return CreateRouteQueryFailure("route.invalid-query-key");
                }

                StoreRouteQueryEntry(entries, key.Text, value.Text);
            }

            pairIndex++;
        }

        SortRouteQueryEntries(entries);
        result.Entries = entries.ToArray();
        return result;
    }

    internal RouteSegmentResult DecodeRouteComponent(string? raw, bool plusAsSpace, bool pathSegment)
    {
        RouteSegmentResult result = new RouteSegmentResult();
        result.Success = true;
        if (raw == null)
        {
            return result;
        }

        if (raw.Length > MaximumRouteComponentLength * 3)
        {
            return CreateRouteSegmentFailure("route.component-too-long");
        }

        StringBuilder decoded = new StringBuilder();
        int index = 0;
        while (index < raw.Length)
        {
            char character = raw[index];
            if (character == '%')
            {
                int firstByte = ReadRoutePercentByte(raw, index);
                if (firstByte < 0)
                {
                    return CreateRouteSegmentFailure("route.invalid-percent-encoding");
                }

                int byteCount = GetRouteUtf8ByteCount(firstByte);
                if (byteCount == 0)
                {
                    return CreateRouteSegmentFailure("route.invalid-utf8");
                }

                int codePoint = GetRouteUtf8InitialValue(firstByte, byteCount);
                int byteIndex = 1;
                while (byteIndex < byteCount)
                {
                    int byteOffset = index + byteIndex * 3;
                    int continuation = ReadRoutePercentByte(raw, byteOffset);
                    if (continuation < 128 || continuation > 191)
                    {
                        return CreateRouteSegmentFailure("route.invalid-utf8");
                    }

                    codePoint = codePoint * 64 + continuation - 128;
                    byteIndex++;
                }

                if (!IsValidRouteCodePoint(codePoint, byteCount))
                {
                    return CreateRouteSegmentFailure("route.invalid-utf8");
                }

                if (!AppendDecodedRouteCodePoint(decoded, codePoint, pathSegment))
                {
                    return CreateRouteSegmentFailure("route.invalid-decoded-character");
                }

                index += byteCount * 3;
            }
            else
            {
                int codePoint = character;
                if (char.IsHighSurrogate(character))
                {
                    if (index + 1 >= raw.Length || !char.IsLowSurrogate(raw[index + 1]))
                    {
                        return CreateRouteSegmentFailure("route.invalid-utf16");
                    }

                    codePoint = char.ConvertToUtf32(character, raw[index + 1]);
                    index++;
                }
                else if (char.IsLowSurrogate(character))
                {
                    return CreateRouteSegmentFailure("route.invalid-utf16");
                }

                if (plusAsSpace && codePoint == '+')
                {
                    codePoint = ' ';
                }

                if (!AppendDecodedRouteCodePoint(decoded, codePoint, pathSegment))
                {
                    return CreateRouteSegmentFailure("route.invalid-decoded-character");
                }

                index++;
            }

            if (decoded.Length > MaximumRouteComponentLength)
            {
                return CreateRouteSegmentFailure("route.component-too-long");
            }
        }

        result.Text = decoded.ToString();
        return result;
    }

    internal bool AppendDecodedRouteCodePoint(StringBuilder output, int codePoint, bool pathSegment)
    {
        if (codePoint < 32 || codePoint == 127 || pathSegment && (codePoint == '/' || codePoint == '\\'))
        {
            return false;
        }

        output.Append(char.ConvertFromUtf32(codePoint));
        return true;
    }

    internal int ReadRoutePercentByte(string value, int index)
    {
        if (index < 0 || index + 2 >= value.Length || value[index] != '%')
        {
            return -1;
        }

        int high = ParseRouteHexDigit(value[index + 1]);
        int low = ParseRouteHexDigit(value[index + 2]);
        return high < 0 || low < 0 ? -1 : high * 16 + low;
    }

    internal int ParseRouteHexDigit(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        return -1;
    }

    internal int GetRouteUtf8ByteCount(int firstByte)
    {
        if (firstByte <= 127)
        {
            return 1;
        }

        if (firstByte >= 194 && firstByte <= 223)
        {
            return 2;
        }

        if (firstByte >= 224 && firstByte <= 239)
        {
            return 3;
        }

        return firstByte >= 240 && firstByte <= 244 ? 4 : 0;
    }

    internal int GetRouteUtf8InitialValue(int firstByte, int byteCount)
    {
        if (byteCount == 1)
        {
            return firstByte;
        }

        if (byteCount == 2)
        {
            return firstByte - 192;
        }

        if (byteCount == 3)
        {
            return firstByte - 224;
        }

        return firstByte - 240;
    }

    internal bool IsValidRouteCodePoint(int codePoint, int byteCount)
    {
        if (byteCount == 1)
        {
            return codePoint <= 127;
        }

        if (byteCount == 2)
        {
            return codePoint >= 128 && codePoint <= 2047;
        }

        if (byteCount == 3)
        {
            return codePoint >= 2048 && (codePoint < 55296 || codePoint > 57343);
        }

        return codePoint >= 65536 && codePoint <= 1114111;
    }

    internal int FindRouteSchemeSeparator(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == ':')
            {
                return index;
            }

            if (character == '/' || character == '?' || character == '#')
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    internal bool IsValidRouteScheme(string scheme)
    {
        if (scheme.Length == 0 || !IsAsciiRouteLetter(scheme[0]))
        {
            return false;
        }

        int index = 1;
        while (index < scheme.Length)
        {
            char character = scheme[index];
            if (!IsAsciiRouteLetter(character) && !IsAsciiRouteDigit(character) && character != '+' && character != '-' && character != '.')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsSafeExternalRouteScheme(string scheme)
    {
        return ValueComparer.TextEquals(scheme, "http") || ValueComparer.TextEquals(scheme, "https") || ValueComparer.TextEquals(scheme, "mailto") || ValueComparer.TextEquals(scheme, "tel");
    }

    internal bool IsAsciiRouteLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    internal bool IsAsciiRouteDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal bool ContainsInvalidRouteText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 32 || character == 127)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool RoutePathStartsWith(string[] path, string[] prefix)
    {
        if (path.Length < prefix.Length)
        {
            return false;
        }

        int index = 0;
        while (index < prefix.Length)
        {
            if (!ValueComparer.TextEquals(path[index], prefix[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal void StoreRouteQueryEntry(List<UiValueEntry> entries, string key, string value)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (ValueComparer.TextEquals(entries[index].Key, key))
            {
                UiValue existing = entries[index].Value;
                if (existing.Kind == UiValueKind.String)
                {
                    UiValue first = new UiValue { Kind = UiValueKind.String, StringValue = existing.StringValue ?? string.Empty };
                    UiValue second = new UiValue { Kind = UiValueKind.String, StringValue = value };
                    existing.Kind = UiValueKind.Array;
                    existing.StringValue = null;
                    existing.ArrayValue = new UiValue[] { first, second };
                }
                else if (existing.Kind == UiValueKind.Array)
                {
                    UiValue[] current = existing.ArrayValue ?? Array.Empty<UiValue>();
                    UiValue[] expanded = new UiValue[current.Length + 1];
                    Array.Copy(current, expanded, current.Length);
                    expanded[expanded.Length - 1] = new UiValue { Kind = UiValueKind.String, StringValue = value };
                    existing.ArrayValue = expanded;
                }

                return;
            }

            index++;
        }

        UiValueEntry entry = new UiValueEntry();
        entry.Key = key;
        entry.Value.Kind = UiValueKind.String;
        entry.Value.StringValue = value;
        entries.Add(entry);
    }

    internal void SortRouteQueryEntries(List<UiValueEntry> entries)
    {
        int index = 1;
        while (index < entries.Count)
        {
            UiValueEntry candidate = entries[index];
            int insertion = index;
            while (insertion > 0 && ValueComparer.CompareText(candidate.Key, entries[insertion - 1].Key) < 0)
            {
                entries[insertion] = entries[insertion - 1];
                insertion--;
            }

            entries[insertion] = candidate;
            index++;
        }
    }

    internal string EncodeRoutePath(string[] segments)
    {
        if (segments.Length == 0)
        {
            return "/";
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < segments.Length)
        {
            result.Append('/');
            result.Append(EncodeRouteComponent(segments[index]));
            index++;
        }

        return result.ToString();
    }

    internal string JoinDecodedRoutePath(string[] segments)
    {
        if (segments.Length == 0)
        {
            return "/";
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < segments.Length)
        {
            result.Append('/');
            result.Append(segments[index]);
            index++;
        }

        return result.ToString();
    }

    internal string EncodeRouteQuery(UiValueEntry[] entries)
    {
        if (entries.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder result = new StringBuilder();
        result.Append('?');
        int index = 0;
        bool hasPair = false;
        while (index < entries.Length)
        {
            UiValue value = entries[index].Value;
            if (value.Kind == UiValueKind.Array)
            {
                UiValue[] items = value.ArrayValue ?? Array.Empty<UiValue>();
                int itemIndex = 0;
                while (itemIndex < items.Length)
                {
                    if (hasPair)
                    {
                        result.Append('&');
                    }

                    result.Append(EncodeRouteComponent(entries[index].Key));
                    result.Append('=');
                    result.Append(EncodeRouteComponent(items[itemIndex].StringValue ?? string.Empty));
                    hasPair = true;
                    itemIndex++;
                }
            }
            else
            {
                if (hasPair)
                {
                    result.Append('&');
                }

                result.Append(EncodeRouteComponent(entries[index].Key));
                result.Append('=');
                result.Append(EncodeRouteComponent(value.StringValue ?? string.Empty));
                hasPair = true;
            }

            index++;
        }

        return result.ToString();
    }

    internal string EncodeRouteFragment(string? fragment)
    {
        return fragment == null ? string.Empty : "#" + EncodeRouteComponent(fragment);
    }

    internal string EncodeRouteComponent(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < bytes.Length)
        {
            byte valueByte = bytes[index];
            if (IsUnreservedRouteByte(valueByte))
            {
                result.Append((char)valueByte);
            }
            else
            {
                result.Append('%');
                result.Append(RouteHexDigit(valueByte / 16));
                result.Append(RouteHexDigit(valueByte % 16));
            }

            index++;
        }

        return result.ToString();
    }

    internal bool IsUnreservedRouteByte(byte value)
    {
        return value >= 65 && value <= 90 || value >= 97 && value <= 122 || value >= 48 && value <= 57 || value == '-' || value == '.' || value == '_' || value == '~';
    }

    internal char RouteHexDigit(int value)
    {
        return (char)(value < 10 ? '0' + value : 'A' + value - 10);
    }

    internal string[] SliceRouteSegments(string[] source, int start)
    {
        string[] result = new string[source.Length - start];
        int index = start;
        while (index < source.Length)
        {
            result[index - start] = source[index];
            index++;
        }

        return result;
    }

    internal string[] CopyRouteSegments(string[] source)
    {
        string[] result = new string[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    internal RouteUrlResult CreateRouteUrlFailure(string code)
    {
        return new RouteUrlResult { Code = code };
    }

    internal RouteSegmentResult CreateRouteSegmentFailure(string code)
    {
        return new RouteSegmentResult { Code = code };
    }

    internal RoutePathResult CreateRoutePathFailure(string code)
    {
        return new RoutePathResult { Code = code };
    }

    internal RouteQueryResult CreateRouteQueryFailure(string code)
    {
        return new RouteQueryResult { Code = code };
    }

    internal RouteValueResult CreateRouteValueFailure(string code)
    {
        return new RouteValueResult { Code = code };
    }
}
