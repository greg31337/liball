namespace odf;

internal static class OdfNameRules
{
    internal static bool IsName(string? value)
    {
        return IsNameCore(value, true);
    }

    internal static bool IsNcName(string? value)
    {
        return IsNameCore(value, false);
    }

    internal static bool IsXmlId(string? value)
    {
        return IsNameCore(value, false);
    }

    internal static bool IsQName(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int colon = value.IndexOf(':');
        if (colon < 0)
        {
            return IsNcName(value);
        }
        if (colon == 0 || colon == value.Length - 1 || colon != value.LastIndexOf(':'))
        {
            return false;
        }

        string prefix = value.Substring(0, colon);
        string localName = value.Substring(colon + 1);
        return IsNcName(prefix) && IsNcName(localName);
    }

    internal static bool IsQualifiedNameValue(OdfQualifiedName? value)
    {
        if (value == null || !IsNcName(value.LocalName))
        {
            return false;
        }
        if (value.Prefix.Length > 0 && !IsNcName(value.Prefix))
        {
            return false;
        }
        if (value.Prefix.Length > 0 && value.NamespaceName.Length == 0)
        {
            return false;
        }
        if (value.Prefix == "xmlns" || value.NamespaceName == "http://www.w3.org/2000/xmlns/")
        {
            return false;
        }

        bool isXmlPrefix = value.Prefix == "xml";
        bool isXmlNamespace = value.NamespaceName == OdfNamespaces.Xml;
        if (isXmlPrefix != isXmlNamespace)
        {
            return false;
        }
        if (value.Prefix.StartsWith("xml", global::System.StringComparison.OrdinalIgnoreCase) && !isXmlPrefix)
        {
            return false;
        }
        return true;
    }

    internal static bool IsNamespaceDeclarationValue(OdfNamespaceDeclaration? value)
    {
        if (value == null)
        {
            return false;
        }
        if (value.Prefix.Length > 0 && !IsNcName(value.Prefix))
        {
            return false;
        }
        if (value.Prefix.Length > 0 && value.NamespaceName.Length == 0)
        {
            return false;
        }
        if (value.Prefix == "xmlns" || value.NamespaceName == "http://www.w3.org/2000/xmlns/")
        {
            return false;
        }

        bool isXmlPrefix = value.Prefix == "xml";
        bool isXmlNamespace = value.NamespaceName == OdfNamespaces.Xml;
        if (isXmlPrefix != isXmlNamespace)
        {
            return false;
        }
        if (value.Prefix.StartsWith("xml", global::System.StringComparison.OrdinalIgnoreCase) && !isXmlPrefix)
        {
            return false;
        }
        return true;
    }

    static bool IsNameCore(string? value, bool allowColon)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            int character = current;
            int width = 1;
            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return false;
                }
                character = char.ConvertToUtf32(current, value[index + 1]);
                width = 2;
            }
            else if (char.IsLowSurrogate(current))
            {
                return false;
            }

            bool valid;
            if (index == 0)
            {
                valid = IsNameStartCharacter(character, allowColon);
            }
            else
            {
                valid = IsNameCharacter(character, allowColon);
            }
            if (!valid)
            {
                return false;
            }
            index = index + width;
        }
        return true;
    }

    static bool IsNameStartCharacter(int character, bool allowColon)
    {
        if (allowColon && character == 0x3A)
        {
            return true;
        }
        if (character == 0x5F)
        {
            return true;
        }
        if (character >= 0x41 && character <= 0x5A)
        {
            return true;
        }
        if (character >= 0x61 && character <= 0x7A)
        {
            return true;
        }
        if (character >= 0xC0 && character <= 0xD6)
        {
            return true;
        }
        if (character >= 0xD8 && character <= 0xF6)
        {
            return true;
        }
        if (character >= 0xF8 && character <= 0x2FF)
        {
            return true;
        }
        if (character >= 0x370 && character <= 0x37D)
        {
            return true;
        }
        if (character >= 0x37F && character <= 0x1FFF)
        {
            return true;
        }
        if (character >= 0x200C && character <= 0x200D)
        {
            return true;
        }
        if (character >= 0x2070 && character <= 0x218F)
        {
            return true;
        }
        if (character >= 0x2C00 && character <= 0x2FEF)
        {
            return true;
        }
        if (character >= 0x3001 && character <= 0xD7FF)
        {
            return true;
        }
        if (character >= 0xF900 && character <= 0xFDCF)
        {
            return true;
        }
        if (character >= 0xFDF0 && character <= 0xFFFD)
        {
            return true;
        }
        return character >= 0x10000 && character <= 0xEFFFF;
    }

    static bool IsNameCharacter(int character, bool allowColon)
    {
        if (IsNameStartCharacter(character, allowColon))
        {
            return true;
        }
        if (character == 0x2D || character == 0x2E)
        {
            return true;
        }
        if (character >= 0x30 && character <= 0x39)
        {
            return true;
        }
        if (character == 0xB7)
        {
            return true;
        }
        if (character >= 0x300 && character <= 0x36F)
        {
            return true;
        }
        return character >= 0x203F && character <= 0x2040;
    }
}
