namespace gltf;

internal class UriReference
{
    internal UriPercentCodec PercentCodec = new UriPercentCodec();

    internal GltfUriKind ClassifyReference(string? reference)
    {
        if (reference == null)
        {
            return GltfUriKind.Absent;
        }

        UriReferenceParts parts = ParseReference(reference);
        if (parts.Scheme == null)
        {
            return GltfUriKind.Relative;
        }

        string scheme = parts.Scheme.ToLowerInvariant();
        switch (scheme)
        {
            case "file":
                return GltfUriKind.File;
            case "data":
                return GltfUriKind.Data;
            case "http":
            case "https":
            case "ftp":
            case "ftps":
            case "ws":
            case "wss":
                return GltfUriKind.Network;
            case "urn":
            case "mailto":
            case "tel":
            case "news":
            case "ldap":
                return GltfUriKind.Absolute;
            default:
                return GltfUriKind.Custom;
        }
    }

    internal string ResolveReference(string baseUri, string reference)
    {
        if (baseUri == null)
        {
            throw new System.ArgumentNullException("baseUri");
        }

        if (reference == null)
        {
            throw new System.ArgumentNullException("reference");
        }

        UriReferenceParts baseParts = ParseReference(baseUri);
        if (baseParts.Scheme == null)
        {
            throw new GltfResourceException("The base URI must be absolute and include a scheme.");
        }

        UriReferenceParts referenceParts = ParseReference(reference);
        UriReferenceParts target = new UriReferenceParts();
        if (referenceParts.Scheme != null)
        {
            target.Scheme = referenceParts.Scheme;
            target.HasAuthority = referenceParts.HasAuthority;
            target.Authority = referenceParts.Authority;
            target.Path = RemoveDotSegments(referenceParts.Path);
            target.HasQuery = referenceParts.HasQuery;
            target.Query = referenceParts.Query;
        }
        else
        {
            target.Scheme = baseParts.Scheme;
            if (referenceParts.HasAuthority)
            {
                target.HasAuthority = true;
                target.Authority = referenceParts.Authority;
                target.Path = RemoveDotSegments(referenceParts.Path);
                target.HasQuery = referenceParts.HasQuery;
                target.Query = referenceParts.Query;
            }
            else
            {
                target.HasAuthority = baseParts.HasAuthority;
                target.Authority = baseParts.Authority;
                if (referenceParts.Path.Length == 0)
                {
                    target.Path = baseParts.Path;
                    if (referenceParts.HasQuery)
                    {
                        target.HasQuery = true;
                        target.Query = referenceParts.Query;
                    }
                    else
                    {
                        target.HasQuery = baseParts.HasQuery;
                        target.Query = baseParts.Query;
                    }
                }
                else
                {
                    if (referenceParts.Path.StartsWith("/", System.StringComparison.Ordinal))
                    {
                        target.Path = RemoveDotSegments(referenceParts.Path);
                    }
                    else
                    {
                        target.Path = RemoveDotSegments(MergePaths(baseParts, referenceParts.Path));
                    }

                    target.HasQuery = referenceParts.HasQuery;
                    target.Query = referenceParts.Query;
                }
            }
        }

        target.HasFragment = referenceParts.HasFragment;
        target.Fragment = referenceParts.Fragment;
        return ComposeReference(target);
    }

    internal UriReferenceParts ParseReference(string reference)
    {
        if (reference == null)
        {
            throw new System.ArgumentNullException("reference");
        }

        PercentCodec.ValidateEscapes(reference);
        UriReferenceParts result = new UriReferenceParts();
        string remaining = reference;
        int fragmentIndex = remaining.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            result.HasFragment = true;
            result.Fragment = remaining.Substring(fragmentIndex + 1);
            remaining = remaining.Substring(0, fragmentIndex);
        }

        int queryIndex = remaining.IndexOf('?');
        if (queryIndex >= 0)
        {
            result.HasQuery = true;
            result.Query = remaining.Substring(queryIndex + 1);
            remaining = remaining.Substring(0, queryIndex);
        }

        int colonIndex = remaining.IndexOf(':');
        int slashIndex = remaining.IndexOf('/');
        if (colonIndex >= 0 && (slashIndex < 0 || colonIndex < slashIndex))
        {
            string scheme = remaining.Substring(0, colonIndex);
            if (!IsValidScheme(scheme))
            {
                throw new GltfResourceException($"URI reference '{reference}' has an invalid scheme.");
            }

            result.Scheme = scheme;
            remaining = remaining.Substring(colonIndex + 1);
        }

        if (remaining.StartsWith("//", System.StringComparison.Ordinal))
        {
            result.HasAuthority = true;
            int authorityEnd = remaining.IndexOf('/', 2);
            if (authorityEnd < 0)
            {
                result.Authority = remaining.Substring(2);
                result.Path = "";
            }
            else
            {
                result.Authority = remaining.Substring(2, authorityEnd - 2);
                result.Path = remaining.Substring(authorityEnd);
            }
        }
        else
        {
            result.Path = remaining;
        }

        return result;
    }

    internal string NormalizeReference(string reference)
    {
        UriReferenceParts parts = ParseReference(reference);
        if (parts.Scheme != null)
        {
            parts.Scheme = parts.Scheme.ToLowerInvariant();
        }

        if (parts.HasAuthority)
        {
            parts.Authority = PercentCodec.NormalizeAuthority(parts.Authority);
        }

        parts.Path = PercentCodec.NormalizePath(parts.Path);
        if (parts.HasQuery)
        {
            parts.Query = PercentCodec.NormalizeQuery(parts.Query);
        }

        if (parts.HasFragment)
        {
            parts.Fragment = PercentCodec.NormalizeFragment(parts.Fragment);
        }

        return ComposeReference(parts);
    }

    internal bool IsValidScheme(string scheme)
    {
        if (scheme.Length == 0 || !IsAsciiLetter(scheme[0]))
        {
            return false;
        }

        int index = 1;
        while (index < scheme.Length)
        {
            char character = scheme[index];
            if (!IsAsciiLetter(character)
                && !IsAsciiDigit(character)
                && character != '+'
                && character != '-'
                && character != '.')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool IsAsciiLetter(char character)
    {
        return (character >= 'A' && character <= 'Z')
            || (character >= 'a' && character <= 'z');
    }

    internal bool IsAsciiDigit(char character)
    {
        return character >= '0' && character <= '9';
    }

    internal string MergePaths(UriReferenceParts baseParts, string referencePath)
    {
        if (baseParts.HasAuthority && baseParts.Path.Length == 0)
        {
            return "/" + referencePath;
        }

        int slashIndex = baseParts.Path.LastIndexOf('/');
        if (slashIndex < 0)
        {
            return referencePath;
        }

        return baseParts.Path.Substring(0, slashIndex + 1) + referencePath;
    }

    internal string RemoveDotSegments(string path)
    {
        string input = path;
        string output = "";
        while (input.Length > 0)
        {
            if (input.StartsWith("../", System.StringComparison.Ordinal))
            {
                input = input.Substring(3);
            }
            else if (input.StartsWith("./", System.StringComparison.Ordinal))
            {
                input = input.Substring(2);
            }
            else if (input.StartsWith("/./", System.StringComparison.Ordinal))
            {
                input = "/" + input.Substring(3);
            }
            else if (input == "/.")
            {
                input = "/";
            }
            else if (input.StartsWith("/../", System.StringComparison.Ordinal))
            {
                input = "/" + input.Substring(4);
                output = RemoveLastPathSegment(output);
            }
            else if (input == "/..")
            {
                input = "/";
                output = RemoveLastPathSegment(output);
            }
            else if (input == "." || input == "..")
            {
                input = "";
            }
            else
            {
                int segmentLength = GetFirstPathSegmentLength(input);
                output += input.Substring(0, segmentLength);
                input = input.Substring(segmentLength);
            }
        }

        return output;
    }

    internal int GetFirstPathSegmentLength(string input)
    {
        int searchStart = 0;
        if (input[0] == '/')
        {
            searchStart = 1;
        }

        int slashIndex = input.IndexOf('/', searchStart);
        if (slashIndex < 0)
        {
            return input.Length;
        }

        return slashIndex;
    }

    internal string RemoveLastPathSegment(string output)
    {
        int slashIndex = output.LastIndexOf('/');
        if (slashIndex < 0)
        {
            return "";
        }

        return output.Substring(0, slashIndex);
    }

    internal string ComposeReference(UriReferenceParts parts)
    {
        string result = "";
        if (parts.Scheme != null)
        {
            result += parts.Scheme + ":";
        }

        if (parts.HasAuthority)
        {
            result += "//" + parts.Authority;
        }

        result += parts.Path;
        if (parts.HasQuery)
        {
            result += "?" + parts.Query;
        }

        if (parts.HasFragment)
        {
            result += "#" + parts.Fragment;
        }

        return result;
    }
}

internal class UriReferenceParts
{
    internal string? Scheme;
    internal bool HasAuthority;
    internal string Authority = "";
    internal string Path = "";
    internal bool HasQuery;
    internal string Query = "";
    internal bool HasFragment;
    internal string Fragment = "";
}
