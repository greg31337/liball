namespace sip;

internal class SipXmlScanner
{
    string text;
    int maximumDepth;
    int index;
    string? errorMessage;
    int errorOffset = -1;
    bool rootSeen;
    bool rootClosed;
    bool hasDeclaration;
    string? rootName;
    string? rootNamespace;
    System.Collections.Generic.List<string> elementNames;
    System.Collections.Generic.List<string> namespacePrefixes;
    System.Collections.Generic.List<string> namespaceValues;
    System.Collections.Generic.List<int> namespaceDepths;
    System.Collections.Generic.List<string> rootAttributeNames;
    System.Collections.Generic.List<string> rootAttributeValues;
    System.Collections.Generic.List<string> childNames;
    System.Collections.Generic.List<string> childNamespaces;

    internal SipXmlScanner(string textValue, int depthLimit)
    {
        text = textValue;
        maximumDepth = depthLimit;
        elementNames = new System.Collections.Generic.List<string>();
        namespacePrefixes = new System.Collections.Generic.List<string>();
        namespaceValues = new System.Collections.Generic.List<string>();
        namespaceDepths = new System.Collections.Generic.List<int>();
        rootAttributeNames = new System.Collections.Generic.List<string>();
        rootAttributeValues = new System.Collections.Generic.List<string>();
        childNames = new System.Collections.Generic.List<string>();
        childNamespaces = new System.Collections.Generic.List<string>();
        namespacePrefixes.Add("xml");
        namespaceValues.Add("http://www.w3.org/XML/1998/namespace");
        namespaceDepths.Add(0);
    }

    internal SipXmlScanResult ScanDocument()
    {
        if (!ValidateCharacters()) return BuildResult();
        if (text.Length > 0 && text[0] == '\uFEFF') index = 1;
        if (StartsWithText("<?xml") && IsXmlDeclarationBoundary(index + 5))
        {
            if (!ScanXmlDeclaration()) return BuildResult();
            hasDeclaration = true;
        }
        while (index < text.Length)
        {
            if (text[index] != '<')
            {
                int textStart = index;
                while (index < text.Length && text[index] != '<') index = index + 1;
                if (!ScanCharacterData(textStart, index, elementNames.Count > 0)) return BuildResult();
                continue;
            }
            if (StartsWithText("<!--"))
            {
                if (!ScanComment()) return BuildResult();
                continue;
            }
            if (StartsWithText("<![CDATA["))
            {
                if (elementNames.Count == 0) return FailResult("CDATA is only allowed inside the root element.");
                if (!ScanCdata()) return BuildResult();
                continue;
            }
            if (StartsWithText("<?"))
            {
                if (!ScanProcessingInstruction()) return BuildResult();
                continue;
            }
            if (StartsWithText("</"))
            {
                if (!ScanEndElement()) return BuildResult();
                continue;
            }
            if (StartsWithText("<!"))
                return FailResult("DTD and other XML declarations are prohibited.");
            if (!ScanStartElement()) return BuildResult();
        }
        if (!rootSeen) return FailResult("An XML document requires a root element.");
        if (!rootClosed || elementNames.Count != 0) return FailResult("An XML element is not closed.");
        SipXmlScanResult result = BuildResult();
        result.Success = true;
        return result;
    }

    bool ScanXmlDeclaration()
    {
        int start = index;
        int close = text.IndexOf("?>", index + 5, System.StringComparison.Ordinal);
        if (close < 0) return Fail("The XML declaration is not terminated.", start);
        string declaration = text.Substring(index + 5, close - index - 5);
        SipXmlScanResult attributes = new SipXmlScanner(
            "<declaration" + declaration + "/>", 1).ScanDocument();
        if (!attributes.Success || !ValidateDeclarationAttributes(attributes))
            return Fail("The XML declaration is invalid or specifies a non-UTF-8 encoding.", start);
        index = close + 2;
        return true;
    }

    bool ValidateDeclarationAttributes(SipXmlScanResult attributes)
    {
        if (attributes.RootAttributeNames.Length == 0
            || attributes.RootAttributeNames[0] != "version"
            || attributes.RootAttributeValues[0] != "1.0") return false;
        int declarationIndex = 1;
        if (declarationIndex < attributes.RootAttributeNames.Length
            && attributes.RootAttributeNames[declarationIndex] == "encoding")
        {
            if (!SipAscii.EqualsIgnoreCase(
                attributes.RootAttributeValues[declarationIndex], "UTF-8")) return false;
            declarationIndex = declarationIndex + 1;
        }
        if (declarationIndex < attributes.RootAttributeNames.Length
            && attributes.RootAttributeNames[declarationIndex] == "standalone")
        {
            string value = attributes.RootAttributeValues[declarationIndex];
            if (value != "yes" && value != "no") return false;
            declarationIndex = declarationIndex + 1;
        }
        return declarationIndex == attributes.RootAttributeNames.Length;
    }

    bool ScanStartElement()
    {
        if (rootClosed) return Fail("An XML document can contain only one root element.", index);
        int start = index;
        index = index + 1;
        string? elementName = ReadName();
        if (elementName == null) return Fail("An XML start tag has an invalid name.", start);
        int depth = elementNames.Count + 1;
        if (depth > maximumDepth) return Fail("The XML element depth exceeds the configured limit.", start);
        System.Collections.Generic.List<string> attributeNames = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> attributeValues = new System.Collections.Generic.List<string>();
        bool empty = false;
        while (index < text.Length)
        {
            int whitespaceStart = index;
            SkipWhitespace();
            bool separated = index > whitespaceStart;
            if (index >= text.Length) return Fail("An XML start tag is not terminated.", start);
            if (text[index] == '>')
            {
                index = index + 1;
                break;
            }
            if (text[index] == '/' && index + 1 < text.Length && text[index + 1] == '>')
            {
                index = index + 2;
                empty = true;
                break;
            }
            if (!separated) return Fail("XML attributes must be separated by whitespace.", index);
            string? attributeName = ReadName();
            if (attributeName == null) return Fail("An XML attribute has an invalid name.", index);
            if (ContainsExact(attributeNames, attributeName))
                return Fail("An XML attribute cannot be repeated.", index);
            SkipWhitespace();
            if (index >= text.Length || text[index] != '=')
                return Fail("An XML attribute requires an equals sign.", index);
            index = index + 1;
            SkipWhitespace();
            if (index >= text.Length || text[index] != '\'' && text[index] != '"')
                return Fail("An XML attribute value must be quoted.", index);
            int quote = text[index];
            index = index + 1;
            int valueStart = index;
            while (index < text.Length && text[index] != quote)
            {
                if (text[index] == '<') return Fail("An XML attribute value cannot contain '<'.", index);
                index = index + 1;
            }
            if (index >= text.Length) return Fail("An XML attribute value is not terminated.", valueStart);
            string rawValue = text.Substring(valueStart, index - valueStart);
            if (!ValidateReferences(rawValue, valueStart)) return false;
            attributeNames.Add(attributeName);
            attributeValues.Add(DecodePredefinedReferences(rawValue));
            index = index + 1;
        }
        AddNamespaceDeclarations(attributeNames, attributeValues, depth);
        string? namespaceUri = ResolveElementNamespace(elementName);
        if (namespaceUri == null) return Fail("An XML namespace prefix is not declared.", start);
        if (!ValidateAttributePrefixes(attributeNames))
            return Fail("An XML attribute namespace prefix is not declared.", start);
        if (!HasUniqueExpandedAttributes(attributeNames))
            return Fail("An XML attribute expanded name cannot be repeated.", start);
        string localName = LocalName(elementName);
        if (!rootSeen)
        {
            rootSeen = true;
            rootName = localName;
            rootNamespace = namespaceUri;
            CopyRootAttributes(attributeNames, attributeValues);
        }
        else if (elementNames.Count == 1)
        {
            childNames.Add(localName);
            childNamespaces.Add(namespaceUri);
        }
        if (empty)
        {
            if (depth == 1) rootClosed = true;
            RemoveNamespacesAtDepth(depth);
        }
        else elementNames.Add(elementName);
        return true;
    }

    bool HasUniqueExpandedAttributes(System.Collections.Generic.List<string> names)
    {
        int indexValue = 0;
        while (indexValue < names.Count)
        {
            string name = names[indexValue];
            if (name != "xmlns" && !name.StartsWith("xmlns:", System.StringComparison.Ordinal))
            {
                int colon = name.IndexOf(':');
                string localName = colon < 0 ? name : name.Substring(colon + 1);
                string namespaceUri = colon < 0 ? "" : LookupNamespace(name.Substring(0, colon)) ?? "";
                int previous = 0;
                while (previous < indexValue)
                {
                    string previousName = names[previous];
                    if (previousName != "xmlns"
                        && !previousName.StartsWith("xmlns:", System.StringComparison.Ordinal))
                    {
                        int previousColon = previousName.IndexOf(':');
                        string previousLocal = previousColon < 0 ? previousName
                            : previousName.Substring(previousColon + 1);
                        string previousNamespace = previousColon < 0 ? ""
                            : LookupNamespace(previousName.Substring(0, previousColon)) ?? "";
                        if (localName == previousLocal && namespaceUri == previousNamespace) return false;
                    }
                    previous = previous + 1;
                }
            }
            indexValue = indexValue + 1;
        }
        return true;
    }

    bool ScanEndElement()
    {
        int start = index;
        index = index + 2;
        string? name = ReadName();
        if (name == null) return Fail("An XML end tag has an invalid name.", start);
        SkipWhitespace();
        if (index >= text.Length || text[index] != '>')
            return Fail("An XML end tag is not terminated.", start);
        index = index + 1;
        if (elementNames.Count == 0 || elementNames[elementNames.Count - 1] != name)
            return Fail("An XML end tag does not match its start tag.", start);
        int depth = elementNames.Count;
        elementNames.RemoveAt(elementNames.Count - 1);
        RemoveNamespacesAtDepth(depth);
        if (elementNames.Count == 0) rootClosed = true;
        return true;
    }

    bool ScanComment()
    {
        int start = index;
        int close = text.IndexOf("-->", index + 4, System.StringComparison.Ordinal);
        if (close < 0) return Fail("An XML comment is not terminated.", start);
        string content = text.Substring(index + 4, close - index - 4);
        if (content.IndexOf("--", System.StringComparison.Ordinal) >= 0
            || content.Length > 0 && content[content.Length - 1] == '-')
            return Fail("An XML comment cannot contain '--'.", start);
        index = close + 3;
        return true;
    }

    bool ScanCdata()
    {
        int start = index;
        int close = text.IndexOf("]]>", index + 9, System.StringComparison.Ordinal);
        if (close < 0) return Fail("An XML CDATA section is not terminated.", start);
        index = close + 3;
        return true;
    }

    bool ScanProcessingInstruction()
    {
        int start = index;
        index = index + 2;
        string? target = ReadName();
        if (target == null) return Fail("An XML processing instruction has an invalid target.", start);
        if (SipAscii.EqualsIgnoreCase(target, "xml"))
            return Fail("The XML declaration may appear only at the start of the document.", start);
        int close = text.IndexOf("?>", index, System.StringComparison.Ordinal);
        if (close < 0) return Fail("An XML processing instruction is not terminated.", start);
        index = close + 2;
        return true;
    }

    bool ScanCharacterData(int start, int end, bool insideRoot)
    {
        string value = text.Substring(start, end - start);
        if (!insideRoot && !IsOnlyWhitespace(value))
            return Fail("Non-whitespace text is not allowed outside the XML root element.", start);
        if (value.IndexOf("]]>", System.StringComparison.Ordinal) >= 0)
            return Fail("Character data cannot contain ']]>'.", start);
        return ValidateReferences(value, start);
    }

    bool ValidateReferences(string value, int sourceOffset)
    {
        int valueIndex = 0;
        while (valueIndex < value.Length)
        {
            if (value[valueIndex] != '&')
            {
                valueIndex = valueIndex + 1;
                continue;
            }
            int semicolon = value.IndexOf(';', valueIndex + 1);
            if (semicolon < 0) return Fail("An XML entity reference is not terminated.", sourceOffset + valueIndex);
            string entity = value.Substring(valueIndex + 1, semicolon - valueIndex - 1);
            if (!IsEntityReference(entity))
                return Fail("An XML entity reference is not predefined or numeric.", sourceOffset + valueIndex);
            valueIndex = semicolon + 1;
        }
        return true;
    }

    bool IsEntityReference(string entity)
    {
        if (entity == "amp" || entity == "lt" || entity == "gt"
            || entity == "apos" || entity == "quot") return true;
        if (entity.Length < 2 || entity[0] != '#') return false;
        bool hexadecimal = entity.Length > 2 && (entity[1] == 'x' || entity[1] == 'X');
        int entityIndex = hexadecimal ? 2 : 1;
        if (entityIndex == entity.Length) return false;
        int value = 0;
        while (entityIndex < entity.Length)
        {
            int digit = XmlDigitValue(entity[entityIndex], hexadecimal);
            if (digit < 0 || value > (1114111 - digit) / (hexadecimal ? 16 : 10)) return false;
            value = value * (hexadecimal ? 16 : 10) + digit;
            entityIndex = entityIndex + 1;
        }
        return IsXmlCodePoint(value);
    }

    bool ValidateCharacters()
    {
        int characterIndex = 0;
        while (characterIndex < text.Length)
        {
            int value = text[characterIndex];
            if (value < 32 && value != 9 && value != 10 && value != 13)
                return Fail("The XML document contains a prohibited character.", characterIndex);
            if (value >= 55296 && value <= 56319)
            {
                if (characterIndex + 1 >= text.Length || text[characterIndex + 1] < 56320
                    || text[characterIndex + 1] > 57343)
                    return Fail("The XML document contains an invalid surrogate.", characterIndex);
                characterIndex = characterIndex + 2;
                continue;
            }
            if (value >= 56320 && value <= 57343)
                return Fail("The XML document contains an invalid surrogate.", characterIndex);
            if (!IsXmlCodePoint(value))
                return Fail("The XML document contains a prohibited character.", characterIndex);
            characterIndex = characterIndex + 1;
        }
        return true;
    }

    string? ReadName()
    {
        if (index >= text.Length || !IsNameStart(text[index])) return null;
        int start = index;
        int colonCount = 0;
        while (index < text.Length && IsNameCharacter(text[index]))
        {
            if (text[index] == ':') colonCount = colonCount + 1;
            index = index + 1;
        }
        if (colonCount > 1 || text[start] == ':' || text[index - 1] == ':') return null;
        return text.Substring(start, index - start);
    }

    void AddNamespaceDeclarations(System.Collections.Generic.List<string> names,
        System.Collections.Generic.List<string> values, int depth)
    {
        int attributeIndex = 0;
        while (attributeIndex < names.Count)
        {
            string name = names[attributeIndex];
            if (name == "xmlns") AddNamespace("", values[attributeIndex], depth);
            else if (name.StartsWith("xmlns:", System.StringComparison.Ordinal))
                AddNamespace(name.Substring(6), values[attributeIndex], depth);
            attributeIndex = attributeIndex + 1;
        }
    }

    void AddNamespace(string prefix, string value, int depth)
    {
        namespacePrefixes.Add(prefix);
        namespaceValues.Add(value);
        namespaceDepths.Add(depth);
    }

    string? ResolveElementNamespace(string name)
    {
        int colon = name.IndexOf(':');
        string prefix = colon < 0 ? "" : name.Substring(0, colon);
        return LookupNamespace(prefix);
    }

    string? LookupNamespace(string prefix)
    {
        int lookup = namespacePrefixes.Count - 1;
        while (lookup >= 0)
        {
            if (namespacePrefixes[lookup] == prefix) return namespaceValues[lookup];
            lookup = lookup - 1;
        }
        return prefix.Length == 0 ? "" : null;
    }

    bool ValidateAttributePrefixes(System.Collections.Generic.List<string> names)
    {
        int attributeIndex = 0;
        while (attributeIndex < names.Count)
        {
            string name = names[attributeIndex];
            int colon = name.IndexOf(':');
            if (colon >= 0 && !name.StartsWith("xmlns:", System.StringComparison.Ordinal)
                && LookupNamespace(name.Substring(0, colon)) == null) return false;
            attributeIndex = attributeIndex + 1;
        }
        return true;
    }

    void RemoveNamespacesAtDepth(int depth)
    {
        int namespaceIndex = namespaceDepths.Count - 1;
        while (namespaceIndex >= 0 && namespaceDepths[namespaceIndex] == depth)
        {
            namespacePrefixes.RemoveAt(namespaceIndex);
            namespaceValues.RemoveAt(namespaceIndex);
            namespaceDepths.RemoveAt(namespaceIndex);
            namespaceIndex = namespaceIndex - 1;
        }
    }

    void CopyRootAttributes(System.Collections.Generic.List<string> names,
        System.Collections.Generic.List<string> values)
    {
        int attributeIndex = 0;
        while (attributeIndex < names.Count)
        {
            if (names[attributeIndex] != "xmlns"
                && !names[attributeIndex].StartsWith("xmlns:", System.StringComparison.Ordinal))
            {
                rootAttributeNames.Add(names[attributeIndex]);
                rootAttributeValues.Add(values[attributeIndex]);
            }
            attributeIndex = attributeIndex + 1;
        }
    }

    string DecodePredefinedReferences(string value)
    {
        return value.Replace("&amp;", "&", System.StringComparison.Ordinal)
            .Replace("&lt;", "<", System.StringComparison.Ordinal)
            .Replace("&gt;", ">", System.StringComparison.Ordinal)
            .Replace("&apos;", "'", System.StringComparison.Ordinal)
            .Replace("&quot;", "\"", System.StringComparison.Ordinal);
    }

    void SkipWhitespace()
    {
        while (index < text.Length && IsWhitespace(text[index])) index = index + 1;
    }

    bool StartsWithText(string value)
    {
        if (value.Length > text.Length - index) return false;
        return string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
    }

    bool IsXmlDeclarationBoundary(int position)
    {
        return position < text.Length && (IsWhitespace(text[position]) || text[position] == '?');
    }

    bool Fail(string message, int offset)
    {
        errorMessage = message;
        errorOffset = offset;
        return false;
    }

    SipXmlScanResult FailResult(string message)
    {
        Fail(message, index);
        return BuildResult();
    }

    SipXmlScanResult BuildResult()
    {
        SipXmlScanResult result = new SipXmlScanResult();
        result.ErrorMessage = errorMessage;
        result.ErrorOffset = errorOffset;
        result.RootName = rootName;
        result.NamespaceUri = rootNamespace;
        result.HasXmlDeclaration = hasDeclaration;
        result.RootAttributeNames = rootAttributeNames.ToArray();
        result.RootAttributeValues = rootAttributeValues.ToArray();
        result.DirectChildNames = childNames.ToArray();
        result.DirectChildNamespaces = childNamespaces.ToArray();
        return result;
    }

    static bool ContainsExact(System.Collections.Generic.List<string> values, string value)
    {
        int valueIndex = 0;
        while (valueIndex < values.Count)
        {
            if (values[valueIndex] == value) return true;
            valueIndex = valueIndex + 1;
        }
        return false;
    }

    static string LocalName(string name)
    {
        int colon = name.IndexOf(':');
        return colon < 0 ? name : name.Substring(colon + 1);
    }

    static bool IsOnlyWhitespace(string value)
    {
        int valueIndex = 0;
        while (valueIndex < value.Length)
        {
            if (!IsWhitespace(value[valueIndex])) return false;
            valueIndex = valueIndex + 1;
        }
        return true;
    }

    static bool IsWhitespace(int value)
    {
        return value == 32 || value == 9 || value == 10 || value == 13;
    }

    static bool IsNameStart(int value)
    {
        return value == ':' || value == '_' || value >= 'A' && value <= 'Z'
            || value >= 'a' && value <= 'z' || value >= 128;
    }

    static bool IsNameCharacter(int value)
    {
        return IsNameStart(value) || value == '-' || value == '.' || value >= '0' && value <= '9';
    }

    static int XmlDigitValue(int value, bool hexadecimal)
    {
        if (value >= '0' && value <= '9') return value - '0';
        if (hexadecimal && value >= 'A' && value <= 'F') return value - 'A' + 10;
        if (hexadecimal && value >= 'a' && value <= 'f') return value - 'a' + 10;
        return -1;
    }

    static bool IsXmlCodePoint(int value)
    {
        return value == 9 || value == 10 || value == 13 || value >= 32 && value <= 55295
            || value >= 57344 && value <= 65533 || value >= 65536 && value <= 1114111;
    }
}
