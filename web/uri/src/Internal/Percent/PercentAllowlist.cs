namespace uri;

internal static class PercentAllowlist
{
    internal const string WebLinkExtendedValueCharacters = "!#$&+-.^_`|~";

    internal static bool IsAllowed(char value, PercentEncodeSet encodeSet)
    {
        switch (encodeSet)
        {
            case PercentEncodeSet.Unreserved:
                return Ascii.IsUnreserved(value);
            case PercentEncodeSet.UserInfo:
                return IsUserInfoCharacter(value);
            case PercentEncodeSet.RegisteredName:
                return IsRegisteredNameCharacter(value);
            case PercentEncodeSet.PathSegment:
                return IsPathSegmentCharacter(value);
            case PercentEncodeSet.Path:
                return IsPathCharacter(value);
            case PercentEncodeSet.Query:
                return IsQueryCharacter(value);
            case PercentEncodeSet.Fragment:
                return IsFragmentCharacter(value);
            case PercentEncodeSet.UrnNamespaceSpecificString:
                return IsUrnNamespaceSpecificStringCharacter(value);
            case PercentEncodeSet.UrnRComponent:
                return IsUrnRComponentCharacter(value);
            case PercentEncodeSet.UrnQComponent:
                return IsUrnQComponentCharacter(value);
            case PercentEncodeSet.UriTemplateSimpleExpansion:
                return IsUriTemplateSimpleExpansionCharacter(value);
            case PercentEncodeSet.UriTemplateReservedExpansion:
                return IsUriTemplateReservedExpansionCharacter(value);
            case PercentEncodeSet.WebLinkExtendedValue:
                return IsWebLinkExtendedValueCharacter(value);
            default:
                return false;
        }
    }

    internal static bool IsUserInfoCharacter(char value)
    {
        return Ascii.IsUnreserved(value) || Ascii.IsSubDelimiter(value) || value == ':';
    }

    internal static bool IsRegisteredNameCharacter(char value)
    {
        return Ascii.IsUnreserved(value) || Ascii.IsSubDelimiter(value);
    }

    internal static bool IsPathSegmentCharacter(char value)
    {
        return Ascii.IsUnreserved(value) ||
            Ascii.IsSubDelimiter(value) ||
            value == ':' ||
            value == '@';
    }

    internal static bool IsPathCharacter(char value)
    {
        return IsPathSegmentCharacter(value) || value == '/';
    }

    internal static bool IsQueryCharacter(char value)
    {
        return IsPathSegmentCharacter(value) || value == '/' || value == '?';
    }

    internal static bool IsFragmentCharacter(char value)
    {
        return IsQueryCharacter(value);
    }

    internal static bool IsUrnNamespaceSpecificStringCharacter(char value)
    {
        return IsPathCharacter(value);
    }

    internal static bool IsUrnRComponentCharacter(char value)
    {
        return IsQueryCharacter(value);
    }

    internal static bool IsUrnQComponentCharacter(char value)
    {
        return IsQueryCharacter(value);
    }

    internal static bool IsUriTemplateSimpleExpansionCharacter(char value)
    {
        return Ascii.IsUnreserved(value);
    }

    internal static bool IsUriTemplateReservedExpansionCharacter(char value)
    {
        return Ascii.IsUnreserved(value) || Ascii.IsReserved(value);
    }

    internal static bool IsWebLinkExtendedValueCharacter(char value)
    {
        return Ascii.IsAlphaNumeric(value) || WebLinkExtendedValueCharacters.IndexOf(value) >= 0;
    }
}
