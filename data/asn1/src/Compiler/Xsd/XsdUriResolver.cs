namespace asn1;

internal class XsdUriResolver
{
    internal bool IsValidAbsoluteUri(string value)
    {
        if (!this.IsValidUriReference(value))
        {
            return false;
        }
        int colon = value.IndexOf(':');
        if (colon <= 0)
        {
            return false;
        }
        int index = 0;
        while (index < colon)
        {
            char character = value[index];
            bool valid = index == 0
                ? character >= 'A' && character <= 'Z'
                    || character >= 'a' && character <= 'z'
                : character >= 'A' && character <= 'Z'
                    || character >= 'a' && character <= 'z'
                    || character >= '0' && character <= '9'
                    || character == '+'
                    || character == '-'
                    || character == '.';
            if (!valid)
            {
                return false;
            }
            index = index + 1;
        }
        return colon + 1 < value.Length;
    }

    internal bool IsValidUriReference(string value)
    {
        if (value == null)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= ' ' || character == '\\')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal string ResolveUri(string baseUri, string reference)
    {
        if (this.IsValidAbsoluteUri(reference))
        {
            return this.NormalizeUriPath(reference);
        }
        if (baseUri.Length == 0)
        {
            return this.NormalizeRelativePath(reference);
        }
        if (reference.Length == 0)
        {
            return this.RemoveFragment(baseUri);
        }
        if (reference[0] == '#')
        {
            return this.RemoveFragment(baseUri) + reference;
        }
        if (reference[0] == '?')
        {
            return this.RemoveQueryAndFragment(baseUri) + reference;
        }
        int authority = baseUri.IndexOf("://", System.StringComparison.Ordinal);
        if (reference[0] == '/' && authority >= 0)
        {
            int pathStart = baseUri.IndexOf('/', authority + 3);
            string origin = pathStart < 0
                ? baseUri
                : baseUri.Substring(0, pathStart);
            return origin + this.NormalizePath(reference, true);
        }
        string cleanBase = this.RemoveQueryAndFragment(baseUri);
        int slash = cleanBase.LastIndexOf('/');
        if (slash < 0)
        {
            return this.NormalizeRelativePath(reference);
        }
        string combined = cleanBase.Substring(0, slash + 1) + reference;
        return this.NormalizeUriPath(combined);
    }

    internal string NormalizeUriPath(string value)
    {
        int authority = value.IndexOf("://", System.StringComparison.Ordinal);
        if (authority < 0)
        {
            return this.NormalizeRelativePath(value);
        }
        int pathStart = value.IndexOf('/', authority + 3);
        if (pathStart < 0)
        {
            return value;
        }
        string prefix = value.Substring(0, pathStart);
        return prefix + this.NormalizePath(
            value.Substring(pathStart),
            true);
    }

    internal string NormalizeRelativePath(string value)
    {
        return this.NormalizePath(
            value,
            value.Length > 0 && value[0] == '/');
    }

    internal string NormalizePath(string value, bool rooted)
    {
        int suffixIndex = value.Length;
        int query = value.IndexOf('?');
        int fragment = value.IndexOf('#');
        if (query >= 0 && query < suffixIndex)
        {
            suffixIndex = query;
        }
        if (fragment >= 0 && fragment < suffixIndex)
        {
            suffixIndex = fragment;
        }
        string path = value.Substring(0, suffixIndex);
        string suffix = value.Substring(suffixIndex);
        bool trailingSlash = path.EndsWith(
            "/",
            System.StringComparison.Ordinal);
        string[] segments = path.Split('/');
        System.Collections.Generic.List<string> result =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < segments.Length)
        {
            string segment = segments[index];
            if (segment.Length == 0 || segment == ".")
            {
                index = index + 1;
                continue;
            }
            if (segment == "..")
            {
                if (result.Count > 0
                    && result[result.Count - 1] != "..")
                {
                    result.RemoveAt(result.Count - 1);
                }
                else if (!rooted)
                {
                    result.Add(segment);
                }
            }
            else
            {
                result.Add(segment);
            }
            index = index + 1;
        }
        System.Text.StringBuilder normalized =
            new System.Text.StringBuilder();
        if (rooted)
        {
            normalized.Append('/');
        }
        index = 0;
        while (index < result.Count)
        {
            if (index > 0)
            {
                normalized.Append('/');
            }
            normalized.Append(result[index]);
            index = index + 1;
        }
        if (trailingSlash && normalized.Length > 0
            && normalized[normalized.Length - 1] != '/')
        {
            normalized.Append('/');
        }
        return normalized.ToString() + suffix;
    }

    internal string RemoveFragment(string value)
    {
        int fragment = value.IndexOf('#');
        return fragment < 0 ? value : value.Substring(0, fragment);
    }

    internal string RemoveQueryAndFragment(string value)
    {
        string result = this.RemoveFragment(value);
        int query = result.IndexOf('?');
        return query < 0 ? result : result.Substring(0, query);
    }
}
