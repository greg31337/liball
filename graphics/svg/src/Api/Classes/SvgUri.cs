namespace svg;

/// <summary>Normalizes and resolves bounded RFC 3986 URI references and RFC 3987 IRIs.</summary>
public class SvgUri : ISvgUri
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgUri()
    {
    }

    /// <summary>The default maximum input and normalized character count.</summary>
    internal const int DefaultMaxCharacters = 8192;

    /// <summary>Normalizes one URI or Unicode IRI reference.</summary>
    /// <param name="value">The URI or IRI reference, which may be empty.</param>
    /// <returns>The normalized forms and resource cache key.</returns>
    public SvgUriResult NormalizeWithValue(string value)
    {
        return new SvgUri().NormalizeWithValueAndMaxCharacters(value, DefaultMaxCharacters);
    }

    /// <summary>Normalizes one URI or Unicode IRI reference with an explicit character bound.</summary>
    /// <param name="value">The URI or IRI reference, which may be empty.</param>
    /// <param name="maxCharacters">The maximum input and normalized character count.</param>
    /// <returns>The normalized forms or a stable failure.</returns>
    public SvgUriResult NormalizeWithValueAndMaxCharacters(string? value, int maxCharacters)
    {
        if (value == null || maxCharacters < 1) return new SvgUri().Failure(SvgUriError.InvalidArgument);
        if (value.Length > maxCharacters) return new SvgUri().Failure(SvgUriError.TooLong);
        SvgUriComponents components = new SvgUri().Parse(value);
        if (!components.Valid) return new SvgUri().Failure(components.Error);
        return new SvgUri().Finish(components, maxCharacters);
    }

    /// <summary>Resolves one URI or IRI reference against a base and normalizes the target.</summary>
    /// <param name="baseUri">The base URI or IRI, or an empty string when no base is available.</param>
    /// <param name="reference">The relative or absolute reference.</param>
    /// <returns>The normalized target and resource cache key.</returns>
    public SvgUriResult ResolveWithBaseUriAndReference(string baseUri, string reference)
    {
        return new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters(baseUri, reference, DefaultMaxCharacters);
    }

    /// <summary>Resolves one URI or IRI reference with an explicit character bound.</summary>
    /// <param name="baseUri">The base URI or IRI, or an empty string when no base is available.</param>
    /// <param name="reference">The relative or absolute reference.</param>
    /// <param name="maxCharacters">The maximum input and normalized character count.</param>
    /// <returns>The normalized target or a stable failure.</returns>
    public SvgUriResult ResolveWithBaseUriAndReferenceAndMaxCharacters(string? baseUri, string? reference, int maxCharacters)
    {
        if (baseUri == null || reference == null || maxCharacters < 1) return new SvgUri().Failure(SvgUriError.InvalidArgument);
        if (baseUri.Length > maxCharacters || reference.Length > maxCharacters) return new SvgUri().Failure(SvgUriError.TooLong);
        SvgUriComponents referenceParts = new SvgUri().Parse(reference);
        if (!referenceParts.Valid) return new SvgUri().Failure(referenceParts.Error);
        if (baseUri.Length == 0) return new SvgUri().Finish(referenceParts, maxCharacters);
        SvgUriComponents baseParts = new SvgUri().Parse(baseUri);
        if (!baseParts.Valid) return new SvgUri().Failure(SvgUriError.InvalidBaseUri);
        SvgUriComponents target = new SvgUri().Transform(baseParts, referenceParts);
        return new SvgUri().Finish(target, maxCharacters);
    }

    /// <summary>Escapes an IRI as an ASCII URI using UTF-8 percent encoding.</summary>
    /// <param name="value">The URI or IRI reference.</param>
    /// <returns>The normalized ASCII URI, or an empty string for invalid input.</returns>
    public string EscapeIri(string value)
    {
        SvgUriResult result = new SvgUri().NormalizeWithValueAndMaxCharacters(value, DefaultMaxCharacters);
        return result.Success ? result.NormalizedUri : "";
    }

    /// <summary>Decodes a valid percent-encoded fragment as UTF-8.</summary>
    /// <param name="fragment">Fragment text with or without a leading number sign.</param>
    /// <returns>The decoded fragment, or an empty string for malformed input.</returns>
    public string DecodeFragment(string? fragment)
    {
        if (fragment == null) return "";
        string value = fragment.Length > 0 && fragment[0] == '#' ? fragment.Substring(1) : fragment;
        SvgUriResult parsed = new SvgUri().NormalizeWithValueAndMaxCharacters("#" + value, DefaultMaxCharacters);
        if (!parsed.Success) return "";
        return System.Uri.UnescapeDataString(parsed.Fragment);
    }

    /// <summary>Creates a fragment-free normalized ASCII cache key for a resolved resource.</summary>
    /// <param name="baseUri">The base URI or IRI, or an empty string.</param>
    /// <param name="reference">The resource reference.</param>
    /// <returns>The cache key, or an empty string for invalid input.</returns>
    public string GetCacheKey(string baseUri, string reference)
    {
        SvgUriResult result = new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters(baseUri, reference, DefaultMaxCharacters);
        return result.Success ? result.CacheKey : "";
    }
SvgUriResult Finish(SvgUriComponents components, int maxCharacters)
    {
        new SvgUri().NormalizeComponents(components);
        if (!new SvgUri().ValidateFile(components)) return new SvgUri().Failure(SvgUriError.InvalidFileUri);
        string iri = new SvgUri().Compose(components);
        string uri = new SvgUri().EscapeUnicode(iri);
        if (iri.Length > maxCharacters || uri.Length > maxCharacters) return new SvgUri().Failure(SvgUriError.TooLong);
        SvgUriResult result = new SvgUriResult();
        result.Success = true;
        result.Error = SvgUriError.None;
        result.NormalizedIri = iri;
        result.NormalizedUri = uri;
        result.HasFragment = components.HasFragment;
        result.Fragment = components.Fragment;
        result.ResourceIri = new SvgUri().RemoveFragment(iri);
        result.CacheKey = new SvgUri().RemoveFragment(uri);
        result.IsAbsolute = components.Scheme.Length != 0;
        result.IsFile = components.Scheme == "file";
        result.IsLocalFile = result.IsFile && (!components.HasAuthority || components.Host.Length == 0);
        return result;
    }
SvgUriResult Failure(SvgUriError error)
    {
        SvgUriResult result = new SvgUriResult();
        result.Error = error;
        return result;
    }
SvgUriComponents Transform(SvgUriComponents baseParts, SvgUriComponents reference)
    {
        SvgUriComponents target = new SvgUriComponents();
        target.Valid = true;
        target.Error = SvgUriError.None;
        if (reference.Scheme.Length != 0)
        {
            new SvgUri().CopyComponents(reference, target);
            target.Path = new SvgUri().RemoveDotSegments(reference.Path);
            return target;
        }
        target.Scheme = baseParts.Scheme;
        if (reference.HasAuthority)
        {
            new SvgUri().CopyAuthority(reference, target);
            target.Path = new SvgUri().RemoveDotSegments(reference.Path);
            target.HasQuery = reference.HasQuery;
            target.Query = reference.Query;
        }
        else
        {
            new SvgUri().CopyAuthority(baseParts, target);
            if (reference.Path.Length == 0)
            {
                target.Path = baseParts.Path;
                if (reference.HasQuery)
                {
                    target.HasQuery = true;
                    target.Query = reference.Query;
                }
                else
                {
                    target.HasQuery = baseParts.HasQuery;
                    target.Query = baseParts.Query;
                }
            }
            else
            {
                if (reference.Path[0] == '/') target.Path = new SvgUri().RemoveDotSegments(reference.Path);
                else target.Path = new SvgUri().RemoveDotSegments(new SvgUri().MergePath(baseParts, reference.Path));
                target.HasQuery = reference.HasQuery;
                target.Query = reference.Query;
            }
        }
        target.HasFragment = reference.HasFragment;
        target.Fragment = reference.Fragment;
        return target;
    }
void CopyComponents(SvgUriComponents source, SvgUriComponents destination)
    {
        destination.Scheme = source.Scheme;
        new SvgUri().CopyAuthority(source, destination);
        destination.Path = source.Path;
        destination.HasQuery = source.HasQuery;
        destination.Query = source.Query;
        destination.HasFragment = source.HasFragment;
        destination.Fragment = source.Fragment;
        destination.Valid = source.Valid;
        destination.Error = source.Error;
    }
void CopyAuthority(SvgUriComponents source, SvgUriComponents destination)
    {
        destination.HasAuthority = source.HasAuthority;
        destination.HasUserInfo = source.HasUserInfo;
        destination.UserInfo = source.UserInfo;
        destination.Host = source.Host;
        destination.HasPort = source.HasPort;
        destination.Port = source.Port;
        destination.HostIsLiteral = source.HostIsLiteral;
    }
string MergePath(SvgUriComponents baseParts, string path)
    {
        if (baseParts.HasAuthority && baseParts.Path.Length == 0) return "/" + path;
        int slash = baseParts.Path.LastIndexOf('/');
        return slash < 0 ? path : baseParts.Path.Substring(0, slash + 1) + path;
    }
SvgUriComponents Parse(string source)
    {
        SvgUriComponents result = new SvgUriComponents();
        if (!new SvgUri().ValidUnicode(source))
        {
            result.Error = SvgUriError.InvalidSyntax;
            return result;
        }
        string value = source.Normalize(System.Text.NormalizationForm.FormC);
        int length = value.Length;
        int fragment = value.IndexOf('#');
        if (fragment >= 0 && value.IndexOf('#', fragment + 1) >= 0)
        {
            result.Error = SvgUriError.InvalidSyntax;
            return result;
        }
        int query = value.IndexOf('?');
        if (fragment >= 0 && query > fragment) query = -1;
        int hierarchyEnd = length;
        if (query >= 0) hierarchyEnd = query;
        if (fragment >= 0 && fragment < hierarchyEnd) hierarchyEnd = fragment;
        int colon = value.IndexOf(':');
        int slash = value.IndexOf('/');
        int schemeBoundary = hierarchyEnd;
        if (slash >= 0 && slash < schemeBoundary) schemeBoundary = slash;
        int position = 0;
        if (colon >= 0 && colon < schemeBoundary)
        {
            if (!new SvgUri().ValidScheme(value, colon))
            {
                result.Error = SvgUriError.InvalidSyntax;
                return result;
            }
            result.Scheme = value.Substring(0, colon);
            position = colon + 1;
        }
        if (position + 1 < hierarchyEnd && value[position] == '/' && value[position + 1] == '/')
        {
            result.HasAuthority = true;
            int authorityStart = position + 2;
            int authorityEnd = authorityStart;
            while (authorityEnd < hierarchyEnd && value[authorityEnd] != '/') authorityEnd = authorityEnd + 1;
            if (!new SvgUri().ParseAuthority(value.Substring(authorityStart, authorityEnd - authorityStart), result)) return result;
            position = authorityEnd;
        }
        result.Path = value.Substring(position, hierarchyEnd - position);
        if (result.HasAuthority && result.Path.Length != 0 && result.Path[0] != '/')
        {
            result.Error = SvgUriError.InvalidSyntax;
            return result;
        }
        if (result.Scheme.Length == 0 && !result.HasAuthority && result.Path.Length != 0)
        {
            int firstSlash = result.Path.IndexOf('/');
            int pathColon = result.Path.IndexOf(':');
            if (pathColon >= 0 && (firstSlash < 0 || pathColon < firstSlash))
            {
                result.Error = SvgUriError.InvalidSyntax;
                return result;
            }
        }
        if (!new SvgUri().ValidComponent(result.Path, 0))
        {
            result.Error = new SvgUri().PercentError(result.Path);
            return result;
        }
        if (query >= 0)
        {
            int queryEnd = fragment < 0 ? length : fragment;
            result.HasQuery = true;
            result.Query = value.Substring(query + 1, queryEnd - query - 1);
            if (!new SvgUri().ValidComponent(result.Query, 1))
            {
                result.Error = new SvgUri().PercentError(result.Query);
                return result;
            }
        }
        if (fragment >= 0)
        {
            result.HasFragment = true;
            result.Fragment = value.Substring(fragment + 1);
            if (!new SvgUri().ValidComponent(result.Fragment, 1))
            {
                result.Error = new SvgUri().PercentError(result.Fragment);
                return result;
            }
        }
        result.Valid = true;
        result.Error = SvgUriError.None;
        return result;
    }
bool ParseAuthority(string authority, SvgUriComponents result)
    {
        int at = authority.LastIndexOf('@');
        if (at >= 0)
        {
            if (authority.IndexOf('@') != at || !new SvgUri().ValidComponent(authority.Substring(0, at), 2))
            {
                result.Error = new SvgUri().PercentError(authority);
                return false;
            }
            result.HasUserInfo = true;
            result.UserInfo = authority.Substring(0, at);
        }
        string hostPort = authority.Substring(at + 1);
        if (hostPort.StartsWith("[", System.StringComparison.Ordinal))
        {
            int close = hostPort.IndexOf(']');
            if (close < 0 || !new SvgUri().ValidHostLiteral(hostPort.Substring(1, close - 1)))
            {
                result.Error = SvgUriError.InvalidSyntax;
                return false;
            }
            result.HostIsLiteral = true;
            result.Host = hostPort.Substring(1, close - 1);
            if (close + 1 < hostPort.Length)
            {
                if (hostPort[close + 1] != ':' || !new SvgUri().ValidPort(hostPort.Substring(close + 2)))
                {
                    result.Error = SvgUriError.InvalidSyntax;
                    return false;
                }
                result.HasPort = true;
                result.Port = hostPort.Substring(close + 2);
            }
        }
        else
        {
            int port = hostPort.LastIndexOf(':');
            if (port >= 0)
            {
                if (hostPort.IndexOf(':') != port || !new SvgUri().ValidPort(hostPort.Substring(port + 1)))
                {
                    result.Error = SvgUriError.InvalidSyntax;
                    return false;
                }
                result.HasPort = true;
                result.Port = hostPort.Substring(port + 1);
                result.Host = hostPort.Substring(0, port);
            }
            else result.Host = hostPort;
            if (!new SvgUri().ValidComponent(result.Host, 3))
            {
                result.Error = new SvgUri().PercentError(result.Host);
                return false;
            }
        }
        return true;
    }
void NormalizeComponents(SvgUriComponents value)
    {
        value.Scheme = value.Scheme.ToLowerInvariant();
        value.UserInfo = new SvgUri().NormalizePercent(value.UserInfo);
        value.Host = new SvgUri().NormalizePercent(value.Host).ToLowerInvariant();
        value.Path = new SvgUri().NormalizePercent(value.Path);
        if (value.Scheme.Length != 0 || value.HasAuthority || value.Path.StartsWith("/", System.StringComparison.Ordinal)) value.Path = new SvgUri().RemoveDotSegments(value.Path);
        value.Query = new SvgUri().NormalizePercent(value.Query);
        value.Fragment = new SvgUri().NormalizePercent(value.Fragment);
        if (value.HasPort && (value.Port.Length == 0 || value.Scheme == "http" && value.Port == "80" || value.Scheme == "https" && value.Port == "443"))
        {
            value.HasPort = false;
            value.Port = "";
        }
        if (value.HasAuthority && value.Path.Length == 0 && value.Scheme != "file") value.Path = "/";
        if (value.Scheme == "file") new SvgUri().NormalizeFile(value);
    }
void NormalizeFile(SvgUriComponents value)
    {
        if (value.Host == "localhost") value.Host = "";
        value.HasAuthority = true;
        if (value.Path.Length >= 3 && value.Path[0] == '/' && new SvgUri().IsAsciiLetter(value.Path[1]) && value.Path[2] == ':')
        {
            value.Path = "/" + char.ToUpperInvariant(value.Path[1]) + value.Path.Substring(2);
        }
    }
bool ValidateFile(SvgUriComponents value)
    {
        if (value.Scheme != "file") return true;
        if (value.HasUserInfo || value.HasPort) return false;
        return value.Path.Length > 0 && value.Path[0] == '/';
    }
string Compose(SvgUriComponents value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        if (value.Scheme.Length != 0) result.Append(value.Scheme).Append(':');
        if (value.HasAuthority)
        {
            result.Append("//");
            if (value.HasUserInfo) result.Append(value.UserInfo).Append('@');
            if (value.HostIsLiteral) result.Append('[').Append(value.Host).Append(']');
            else result.Append(value.Host);
            if (value.HasPort) result.Append(':').Append(value.Port);
        }
        result.Append(value.Path);
        if (value.HasQuery) result.Append('?').Append(value.Query);
        if (value.HasFragment) result.Append('#').Append(value.Fragment);
        return result.ToString();
    }
string NormalizePercent(string value)
    {
        if (value.Length == 0) return value;
        System.Text.StringBuilder result = new System.Text.StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '%')
            {
                int octet = new SvgUri().Hex(value[index + 1]) * 16 + new SvgUri().Hex(value[index + 2]);
                char decoded = (char)octet;
                if (new SvgUri().IsUnreserved(decoded)) result.Append(decoded);
                else result.Append('%').Append(value[index + 1].ToString().ToUpperInvariant()).Append(value[index + 2].ToString().ToUpperInvariant());
                index = index + 3;
            }
            else
            {
                result.Append(value[index]);
                index = index + 1;
            }
        }
        return result.ToString();
    }
string EscapeUnicode(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < 128)
            {
                result.Append(character);
                index = index + 1;
                continue;
            }
            int count = char.IsHighSurrogate(character) ? 2 : 1;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value.Substring(index, count));
            int byteIndex = 0;
            while (byteIndex < bytes.Length)
            {
                result.Append('%').Append(bytes[byteIndex].ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
                byteIndex = byteIndex + 1;
            }
            index = index + count;
        }
        return result.ToString();
    }
string RemoveDotSegments(string path)
    {
        string input = path;
        string output = "";
        while (input.Length != 0)
        {
            if (input.StartsWith("../", System.StringComparison.Ordinal)) input = input.Substring(3);
            else if (input.StartsWith("./", System.StringComparison.Ordinal)) input = input.Substring(2);
            else if (input.StartsWith("/./", System.StringComparison.Ordinal)) input = input.Substring(2);
            else if (input == "/.") input = "/";
            else if (input.StartsWith("/../", System.StringComparison.Ordinal))
            {
                input = input.Substring(3);
                output = new SvgUri().RemoveLastSegment(output);
            }
            else if (input == "/..")
            {
                input = "/";
                output = new SvgUri().RemoveLastSegment(output);
            }
            else if (input == "." || input == "..") input = "";
            else
            {
                int next = input[0] == '/' ? input.IndexOf('/', 1) : input.IndexOf('/');
                if (next < 0)
                {
                    output = output + input;
                    input = "";
                }
                else
                {
                    output = output + input.Substring(0, next);
                    input = input.Substring(next);
                }
            }
        }
        return output;
    }
string RemoveLastSegment(string value)
    {
        int slash = value.LastIndexOf('/');
        return slash < 0 ? "" : value.Substring(0, slash);
    }
string RemoveFragment(string value)
    {
        int fragment = value.IndexOf('#');
        return fragment < 0 ? value : value.Substring(0, fragment);
    }
bool ValidScheme(string value, int colon)
    {
        if (colon < 1 || !new SvgUri().IsAsciiLetter(value[0])) return false;
        int index = 1;
        while (index < colon)
        {
            char character = value[index];
            if (!new SvgUri().IsAsciiLetter(character) && !new SvgUri().IsDigit(character) && character != '+' && character != '-' && character != '.') return false;
            index = index + 1;
        }
        return true;
    }
bool ValidComponent(string value, int kind)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '%')
            {
                if (index + 2 >= value.Length || new SvgUri().Hex(value[index + 1]) < 0 || new SvgUri().Hex(value[index + 2]) < 0) return false;
                index = index + 3;
                continue;
            }
            if (character >= 128)
            {
                index = index + (char.IsHighSurrogate(character) ? 2 : 1);
                continue;
            }
            bool valid = new SvgUri().IsUnreserved(character) || new SvgUri().IsSubDelimiter(character) || character == ':' || character == '@';
            if (kind == 0) valid = valid || character == '/';
            else if (kind == 1) valid = valid || character == '/' || character == '?';
            else if (kind == 3) valid = new SvgUri().IsUnreserved(character) || new SvgUri().IsSubDelimiter(character);
            if (!valid) return false;
            index = index + 1;
        }
        return true;
    }
bool ValidHostLiteral(string value)
    {
        if (value.Length == 0) return false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (!new SvgUri().IsAsciiLetter(character) && !new SvgUri().IsDigit(character) && character != ':' && character != '.' && character != '-' && character != '_' && character != '~' && character != '%' && character != 'v') return false;
            if (character == '%' && (index + 2 >= value.Length || new SvgUri().Hex(value[index + 1]) < 0 || new SvgUri().Hex(value[index + 2]) < 0)) return false;
            index = index + (character == '%' ? 3 : 1);
        }
        return true;
    }
bool ValidPort(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!new SvgUri().IsDigit(value[index])) return false;
            index = index + 1;
        }
        return true;
    }
SvgUriError PercentError(string value)
    {
        int index = value.IndexOf('%');
        while (index >= 0)
        {
            if (index + 2 >= value.Length || new SvgUri().Hex(value[index + 1]) < 0 || new SvgUri().Hex(value[index + 2]) < 0) return SvgUriError.InvalidPercentEscape;
            index = value.IndexOf('%', index + 3);
        }
        return SvgUriError.InvalidSyntax;
    }
bool ValidUnicode(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= 0x20 || character == 0x7f || character == '\\' || character == '<' || character == '>' || character == '"' || character == '^' || character == '`' || character == '{' || character == '|' || character == '}') return false;
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1])) return false;
                index = index + 2;
                continue;
            }
            if (char.IsLowSurrogate(character)) return false;
            index = index + 1;
        }
        return true;
    }
bool IsUnreserved(char value)
    {
        return new SvgUri().IsAsciiLetter(value) || new SvgUri().IsDigit(value) || value == '-' || value == '.' || value == '_' || value == '~';
    }
bool IsSubDelimiter(char value)
    {
        return value == '!' || value == '$' || value == '&' || value == '\'' || value == '(' || value == ')' || value == '*' || value == '+' || value == ',' || value == ';' || value == '=';
    }
bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }
bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }
int Hex(char value)
    {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        if (value >= 'a' && value <= 'f') return value - 'a' + 10;
        return -1;
    }
}
