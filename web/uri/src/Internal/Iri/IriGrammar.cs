namespace uri;

internal static class IriGrammar
{
    internal static bool IsUcsCharacter(int scalarValue)
    {
        if (!UnicodeScalar.IsValid(scalarValue))
        {
            return false;
        }

        if (scalarValue >= 160 && scalarValue <= 55295)
        {
            return true;
        }

        if (scalarValue >= 63744 && scalarValue <= 64975)
        {
            return true;
        }

        if (scalarValue >= 65008 && scalarValue <= 65519)
        {
            return true;
        }

        if (scalarValue >= 65536 && scalarValue <= 917503)
        {
            int planeOffset = scalarValue & 65535;
            return planeOffset <= 65533;
        }

        if (scalarValue >= 921600 && scalarValue <= 983037)
        {
            return true;
        }

        return false;
    }

    internal static bool IsPrivateCharacter(int scalarValue)
    {
        if (!UnicodeScalar.IsValid(scalarValue))
        {
            return false;
        }

        if (scalarValue >= 57344 && scalarValue <= 63743)
        {
            return true;
        }

        if (scalarValue >= 983040 && scalarValue <= 1048573)
        {
            return true;
        }

        return scalarValue >= 1048576 && scalarValue <= 1114109;
    }

    internal static bool IsInternationalizedUnreserved(int scalarValue)
    {
        if (scalarValue >= 0 && scalarValue <= 127)
        {
            return Ascii.IsUnreserved((char)scalarValue);
        }

        return IsUcsCharacter(scalarValue);
    }

    internal static bool IsUserInfoCharacter(int scalarValue)
    {
        if (IsInternationalizedUnreserved(scalarValue))
        {
            return true;
        }

        return scalarValue >= 0 &&
            scalarValue <= 127 &&
            (Ascii.IsSubDelimiter((char)scalarValue) || scalarValue == ':');
    }

    internal static bool IsRegisteredNameCharacter(int scalarValue)
    {
        if (IsInternationalizedUnreserved(scalarValue))
        {
            return true;
        }

        return scalarValue >= 0 &&
            scalarValue <= 127 &&
            Ascii.IsSubDelimiter((char)scalarValue);
    }

    internal static bool IsPathSegmentCharacter(int scalarValue)
    {
        if (IsInternationalizedUnreserved(scalarValue))
        {
            return true;
        }

        return scalarValue >= 0 &&
            scalarValue <= 127 &&
            (Ascii.IsSubDelimiter((char)scalarValue) ||
                scalarValue == ':' ||
                scalarValue == '@');
    }

    internal static bool IsPathNoSchemeSegmentCharacter(int scalarValue)
    {
        if (IsInternationalizedUnreserved(scalarValue))
        {
            return true;
        }

        return scalarValue >= 0 &&
            scalarValue <= 127 &&
            (Ascii.IsSubDelimiter((char)scalarValue) || scalarValue == '@');
    }

    internal static bool IsPathCharacter(int scalarValue)
    {
        return IsPathSegmentCharacter(scalarValue) || scalarValue == '/';
    }

    internal static bool IsQueryCharacter(int scalarValue)
    {
        return IsPathSegmentCharacter(scalarValue) ||
            IsPrivateCharacter(scalarValue) ||
            scalarValue == '/' ||
            scalarValue == '?';
    }

    internal static bool IsFragmentCharacter(int scalarValue)
    {
        return IsPathSegmentCharacter(scalarValue) ||
            scalarValue == '/' ||
            scalarValue == '?';
    }
}
