using System;

namespace xml;

internal class SerializationParameterValidator
{
    internal void ValidateSerializationParameters(XmlSerializationOptions options)
    {
        Guard.NotNull(options, "options");
        if (options.Method < XmlSerializationMethod.Xml || options.Method > XmlSerializationMethod.Extension)
        {
            throw CreateSerializationParameterError("The serialization method is not supported.", XmlErrorCodes.SEPM0016);
        }

        if (options.Standalone < XmlStandalone.Unspecified || options.Standalone > XmlStandalone.No)
        {
            throw CreateSerializationParameterError("The standalone parameter is invalid.", XmlErrorCodes.SEPM0016);
        }

        if (options.InvalidCharacterPolicy < XmlInvalidCharacterPolicy.Error || options.InvalidCharacterPolicy > XmlInvalidCharacterPolicy.Replace)
        {
            throw CreateSerializationParameterError("The invalid-character policy is invalid.", XmlErrorCodes.SEPM0016);
        }

        ValidateSerializationUnicodeString(options.ItemSeparator, "item-separator");
        ValidateSerializationUnicodeString(options.Version, "version");
        ValidateSerializationUnicodeString(options.MediaType, "media-type");
        ValidateSerializationUnicodeString(options.DoctypeSystem, "doctype-system");
        ValidateSerializationUnicodeString(options.InvalidCharacterReplacement, "invalid-character replacement");
        ValidateSerializationUnicodeString(options.NewLine, "newline");
        ValidateSerializationEncoding(options.Encoding);
        ValidateSerializationNormalization(options.NormalizationForm);
        ValidateSerializationHtmlVersion(options.HtmlVersion);
        ValidateSerializationMediaType(options.MediaType);
        ValidateSerializationDoctype(options.DoctypePublic, options.DoctypeSystem);
        ValidateSerializationNames(options.CDataSectionElements, "cdata-section-elements");
        ValidateSerializationNames(options.SuppressIndentation, "suppress-indentation");
        new OutputCharacterMap(options.UseCharacterMaps);

        if (options.Method == XmlSerializationMethod.Xml || options.Method == XmlSerializationMethod.Xhtml)
        {
            if (options.Version != null && !string.Equals(options.Version, "1.0", StringComparison.Ordinal) && !string.Equals(options.Version, "1.1", StringComparison.Ordinal))
            {
                throw CreateSerializationParameterError("The requested XML version is not supported.", XmlErrorCodes.SESU0013);
            }

            if (options.UndeclarePrefixes && string.Equals(options.Version, "1.0", StringComparison.Ordinal))
            {
                throw CreateSerializationParameterError("Prefix undeclarations require XML 1.1 output.", XmlErrorCodes.SEPM0010);
            }
        }

        if (options.Method == XmlSerializationMethod.Html)
        {
            string? requestedHtmlVersion = options.HtmlVersion == null ? options.Version : options.HtmlVersion;
            if (requestedHtmlVersion != null && !IsSupportedSerializationHtmlVersion(requestedHtmlVersion))
            {
                throw CreateSerializationParameterError("The requested HTML version is not supported.", XmlErrorCodes.SESU0013);
            }
        }

        if (options.Method == XmlSerializationMethod.Json && options.JsonNodeOutputMethod != XmlSerializationMethod.Xml && options.JsonNodeOutputMethod != XmlSerializationMethod.Xhtml && options.JsonNodeOutputMethod != XmlSerializationMethod.Html && options.JsonNodeOutputMethod != XmlSerializationMethod.Text)
        {
            throw CreateSerializationParameterError("The json-node-output-method must be xml, xhtml, html, or text.", XmlErrorCodes.SEPM0004);
        }
    }

    internal void ValidateNormalizedSerializationDocument(XmlDocument document, XmlSerializationOptions options)
    {
        if (options.Method != XmlSerializationMethod.Xml && options.Method != XmlSerializationMethod.Xhtml)
        {
            return;
        }

        XmlNode? root = document.DocumentNode;
        XmlNode[] children = root == null || root.Children == null ? [] : root.Children;
        int elementCount = 0;
        bool containsText = false;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                elementCount = elementCount + 1;
            }
            else if (children[index].Kind == XmlNodeKind.Text || children[index].Kind == XmlNodeKind.CData)
            {
                containsText = true;
            }

            index = index + 1;
        }

        bool constrained = options.DoctypeSystem != null || options.Standalone != XmlStandalone.Unspecified;
        if (constrained && (containsText || elementCount > 1))
        {
            throw CreateSerializationParameterError("The doctype-system or standalone parameter cannot be used with top-level text or multiple top-level elements.", XmlErrorCodes.SEPM0004);
        }

        XmlVersion effectiveVersion = options.Version == null ? document.Version : string.Equals(options.Version, "1.1", StringComparison.Ordinal) ? XmlVersion.Xml11 : XmlVersion.Xml10;
        if (options.OmitXmlDeclaration && options.DoctypeSystem != null && effectiveVersion != XmlVersion.Xml10)
        {
            throw CreateSerializationParameterError("An omitted XML declaration cannot be combined with a doctype-system parameter for this XML version.", XmlErrorCodes.SEPM0009);
        }

        if (root != null)
        {
            ValidateSerializationNodeNames(root, effectiveVersion);
        }
    }

    internal void ValidateSerializationNodeNames(XmlNode node, XmlVersion version)
    {
        XmlNamespaceHandling handling = version == XmlVersion.Xml11 ? XmlNamespaceHandling.Namespaces11 : XmlNamespaceHandling.Namespaces10;
        QualifiedNameParser parser = new QualifiedNameParser(handling, null);
        if (node.Kind == XmlNodeKind.Element || node.Kind == XmlNodeKind.Attribute || node.Kind == XmlNodeKind.ProcessingInstruction || node.Kind == XmlNodeKind.DocumentType)
        {
            XmlName? name = node.Name;
            string? localName = name == null ? null : name.LocalName;
            string? prefix = name == null ? null : name.Prefix;
            string expectedName = prefix == null || prefix.Length == 0 ? localName == null ? "" : localName : prefix + ":" + localName;
            if (name == null || localName == null || !parser.IsValidXmlNcName(localName) || prefix != null && prefix.Length > 0 && !parser.IsValidXmlNcName(prefix) || name.QualifiedName != null && name.QualifiedName.Length > 0 && !string.Equals(name.QualifiedName, expectedName, StringComparison.Ordinal))
            {
                throw CreateSerializationParameterError("The serialized result contains a name that is not permitted by the selected XML version.", XmlErrorCodes.SERE0005);
            }
        }

        XmlNamespaceBinding[] bindings = node.NamespaceBindings == null ? [] : node.NamespaceBindings;
        int bindingIndex = 0;
        while (bindingIndex < bindings.Length)
        {
            XmlNamespaceBinding? binding = bindings[bindingIndex];
            string? prefix = binding == null ? null : binding.Prefix;
            if (binding == null || prefix == null || prefix.Length > 0 && !parser.IsValidXmlNcName(prefix))
            {
                throw CreateSerializationParameterError("The serialized result contains a namespace prefix that is not permitted by the selected XML version.", XmlErrorCodes.SERE0005);
            }

            bindingIndex = bindingIndex + 1;
        }

        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            ValidateSerializationNodeNames(attributes[attributeIndex], version);
            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            ValidateSerializationNodeNames(children[childIndex], version);
            childIndex = childIndex + 1;
        }
    }

    internal void ValidateSerializationEncoding(string? encoding)
    {
        if (encoding == null)
        {
            return;
        }

        if (encoding.Length == 0)
        {
            throw CreateSerializationParameterError("The encoding parameter must not be empty.", XmlErrorCodes.SEPM0016);
        }

        int index = 0;
        while (index < encoding.Length)
        {
            int character = encoding[index];
            if (character < 0x21 || character > 0x7E)
            {
                throw CreateSerializationParameterError("The encoding parameter must contain printable ASCII characters only.", XmlErrorCodes.SEPM0016);
            }

            index = index + 1;
        }

        if (!IsSupportedSerializationEncoding(encoding))
        {
            throw CreateSerializationParameterError("The requested output encoding is not supported.", XmlErrorCodes.SESU0007);
        }
    }

    internal bool IsSupportedSerializationEncoding(string encoding)
    {
        return string.Equals(encoding, "utf-8", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf8", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf-16", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf16", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "unicode", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf-16le", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf16le", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf-16be", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf16be", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "unicodefffe", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf-32", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf32", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf-32le", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf32le", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf-32be", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "utf32be", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "us-ascii", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "ascii", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "iso-8859-1", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "iso8859-1", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "latin1", StringComparison.OrdinalIgnoreCase) || string.Equals(encoding, "latin-1", StringComparison.OrdinalIgnoreCase);
    }

    internal void ValidateSerializationNormalization(string? form)
    {
        if (form == null)
        {
            return;
        }

        if (form.Length == 0)
        {
            throw CreateSerializationParameterError("The normalization-form parameter must not be empty.", XmlErrorCodes.SEPM0016);
        }

        if (string.Equals(form, "none", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFC", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFD", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFKC", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFKD", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "fully-normalized", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!new QualifiedNameParser(XmlNamespaceHandling.Namespaces10, null).IsValidXmlNcName(form))
        {
            throw CreateSerializationParameterError("The normalization-form parameter is outside its defined domain.", XmlErrorCodes.SEPM0016);
        }

        throw CreateSerializationParameterError("The normalization form is not supported.", XmlErrorCodes.SESU0011);
    }

    internal void ValidateSerializationHtmlVersion(string? version)
    {
        if (version != null && !IsSerializationDecimal(version))
        {
            throw CreateSerializationParameterError("The html-version parameter must be a decimal value.", XmlErrorCodes.SEPM0016);
        }
    }

    internal bool IsSerializationDecimal(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = value[0] == '+' ? 1 : 0;
        int digits = 0;
        int points = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character == '.')
            {
                points = points + 1;
                if (points > 1)
                {
                    return false;
                }
            }
            else if (character >= '0' && character <= '9')
            {
                digits = digits + 1;
            }
            else
            {
                return false;
            }

            index = index + 1;
        }

        return digits > 0;
    }

    internal bool IsSupportedSerializationHtmlVersion(string value)
    {
        if (!IsSerializationDecimal(value))
        {
            return false;
        }

        double number = 0;
        double fraction = 0.1;
        bool afterPoint = false;
        int index = value[0] == '+' ? 1 : 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character == '.')
            {
                afterPoint = true;
            }
            else if (!afterPoint)
            {
                number = number * 10 + character - '0';
            }
            else
            {
                number = number + (character - '0') * fraction;
                fraction = fraction / 10;
            }

            if (number > 5)
            {
                return false;
            }

            index = index + 1;
        }

        return number >= 1 && number <= 5;
    }

    internal void ValidateSerializationMediaType(string? mediaType)
    {
        if (mediaType == null)
        {
            return;
        }

        int semicolon = mediaType.IndexOf(';');
        string essence = semicolon < 0 ? mediaType : mediaType.Substring(0, semicolon);
        int slash = essence.IndexOf('/');
        if (slash <= 0 || slash == essence.Length - 1 || essence.IndexOf('/', slash + 1) >= 0 || !IsSerializationMediaTypeToken(essence.Substring(0, slash)) || !IsSerializationMediaTypeToken(essence.Substring(slash + 1)))
        {
            throw CreateSerializationParameterError("The media-type parameter is not a valid media type.", XmlErrorCodes.SEPM0016);
        }

        int parameterStart = semicolon;
        while (parameterStart >= 0 && parameterStart < mediaType.Length)
        {
            parameterStart = parameterStart + 1;
            while (parameterStart < mediaType.Length && (mediaType[parameterStart] == ' ' || mediaType[parameterStart] == '\t'))
            {
                parameterStart = parameterStart + 1;
            }

            if (parameterStart + 7 <= mediaType.Length && string.Equals(mediaType.Substring(parameterStart, 7), "charset", StringComparison.OrdinalIgnoreCase))
            {
                int afterName = parameterStart + 7;
                while (afterName < mediaType.Length && (mediaType[afterName] == ' ' || mediaType[afterName] == '\t'))
                {
                    afterName = afterName + 1;
                }

                if (afterName < mediaType.Length && mediaType[afterName] == '=')
                {
                    throw CreateSerializationParameterError("The media-type parameter must not specify a charset parameter.", XmlErrorCodes.SEPM0016);
                }
            }

            parameterStart = mediaType.IndexOf(';', parameterStart);
        }
    }

    internal bool IsSerializationMediaTypeToken(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        string punctuation = "!#$%&'*+-.^_`|~";
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            bool alphaNumeric = character >= '0' && character <= '9' || character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
            if (!alphaNumeric && !punctuation.Contains((char)character))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void ValidateSerializationDoctype(string? publicIdentifier, string? systemIdentifier)
    {
        if (publicIdentifier != null)
        {
            string punctuation = "-'()+,./:=?;!*#@$_%";
            int index = 0;
            while (index < publicIdentifier.Length)
            {
                int character = publicIdentifier[index];
                bool permitted = character == 0x20 || character == 0xD || character == 0xA || character >= '0' && character <= '9' || character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || punctuation.Contains((char)character);
                if (!permitted)
                {
                    throw CreateSerializationParameterError("The doctype-public parameter contains a character outside PubidChar.", XmlErrorCodes.SEPM0016);
                }

                index = index + 1;
            }
        }

        if (systemIdentifier != null && systemIdentifier.Contains("\"", StringComparison.Ordinal) && systemIdentifier.Contains("'", StringComparison.Ordinal))
        {
            throw CreateSerializationParameterError("The doctype-system parameter contains both quote delimiters.", XmlErrorCodes.SEPM0016);
        }
    }

    internal void ValidateSerializationNames(XmlName[]? names, string parameterName)
    {
        if (names == null)
        {
            return;
        }

        QualifiedNameParser parser = new QualifiedNameParser(XmlNamespaceHandling.Namespaces10, null);
        int index = 0;
        while (index < names.Length)
        {
            XmlName? name = names[index];
            if (name == null || name.LocalName == null || !parser.IsValidXmlNcName(name.LocalName))
            {
                throw CreateSerializationParameterError("The " + parameterName + " parameter contains an invalid expanded QName.", XmlErrorCodes.SEPM0016);
            }

            ValidateSerializationUnicodeString(name.NamespaceUri, parameterName);
            index = index + 1;
        }
    }

    internal void ValidateSerializationUnicodeString(string? value, string parameterName)
    {
        if (value == null)
        {
            return;
        }

        int position = 0;
        while (position < value.Length)
        {
            int first = value[position];
            if (first >= 0xD800 && first <= 0xDBFF)
            {
                if (position + 1 >= value.Length || value[position + 1] < 0xDC00 || value[position + 1] > 0xDFFF)
                {
                    throw CreateSerializationParameterError("The " + parameterName + " parameter contains malformed Unicode.", XmlErrorCodes.SEPM0016);
                }

                position = position + 2;
                continue;
            }

            if (first >= 0xDC00 && first <= 0xDFFF)
            {
                throw CreateSerializationParameterError("The " + parameterName + " parameter contains malformed Unicode.", XmlErrorCodes.SEPM0016);
            }

            position = position + 1;
        }
    }

    internal XmlSerializationException CreateSerializationParameterError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }
}
