namespace asn1;

internal class XerXmlTokenWriter
{
    internal System.IO.Stream Output;
    internal AsnResourceLimits Limits;
    internal ResourceCounter Resources;
    internal XerXmlCharacters Characters = new XerXmlCharacters();
    internal int WrittenCount;
    internal int TokenCount;
    internal bool Terminal;
    internal XerXmlWriteResult TerminalResult = new XerXmlWriteResult();
    internal XerXmlNamespaceResolver NamespaceResolver =
        new XerXmlNamespaceResolver();
    internal System.Collections.Generic.List<XerXmlElementFrame> ElementStack =
        new System.Collections.Generic.List<XerXmlElementFrame>();
    internal System.Collections.Generic.List<XerXmlNamespaceBinding> AssignedPrefixes =
        new System.Collections.Generic.List<XerXmlNamespaceBinding>();
    internal int NextPrefix = 1;
    internal bool RootSeen;
    internal bool RootClosed;
    internal bool DeclarationWritten;

    internal XerXmlTokenWriter(
        System.IO.Stream output,
        AsnResourceLimits limits)
    {
        this.Output = output == null
            ? throw new System.ArgumentNullException("output")
            : output;
        this.Limits = limits == null
            ? throw new System.ArgumentNullException("limits")
            : limits;
        this.Resources = new ResourceCounter(limits);
    }

    internal XerXmlWriteResult WriteToken(XerXmlToken token)
    {
        if (token == null)
        {
            throw new System.ArgumentNullException("token");
        }

        if (this.Terminal)
        {
            return this.CreateWriteError(
                "ASNXER0030",
                "The XML token writer is terminal until ResetWriter is called.",
                false);
        }

        if (!this.Resources.AllowsCollectionElements(this.TokenCount + 1))
        {
            return this.SetTerminalWriteError(
                "ASNR0011",
                "The XML token count exceeds the configured limit.",
                true);
        }

        string encoded;
        if (token.Kind == XerXmlTokenKind.StartElement
            || token.Kind == XerXmlTokenKind.EmptyElement)
        {
            encoded = this.EncodeStartToken(token);
            if (this.Terminal)
            {
                return this.CopyTerminalWriteResult();
            }

            if (token.Kind == XerXmlTokenKind.StartElement
                && !this.Resources.TryEnterNesting())
            {
                return this.SetTerminalWriteError(
                    "ASNR0001",
                    "The XML nesting depth exceeds the configured limit.",
                    true);
            }
        }
        else if (token.Kind == XerXmlTokenKind.EndElement)
        {
            encoded = this.EncodeEndToken(token);
            if (this.Terminal)
            {
                return this.CopyTerminalWriteResult();
            }

            this.Resources.LeaveNesting();
        }
        else if (token.Kind == XerXmlTokenKind.Text)
        {
            encoded = this.EncodeTextToken(token);
        }
        else if (token.Kind == XerXmlTokenKind.CData)
        {
            encoded = this.EncodeCDataToken(token);
        }
        else if (token.Kind == XerXmlTokenKind.Comment)
        {
            encoded = this.EncodeCommentToken(token);
        }
        else if (token.Kind == XerXmlTokenKind.ProcessingInstruction)
        {
            encoded = this.EncodeProcessingInstructionToken(token);
        }
        else if (token.Kind == XerXmlTokenKind.XmlDeclaration)
        {
            encoded = this.EncodeXmlDeclarationToken(token);
        }
        else
        {
            return this.SetTerminalWriteError(
                "ASNXER0031",
                "The XML token kind is not supported.",
                false);
        }

        if (this.Terminal)
        {
            return this.CopyTerminalWriteResult();
        }

        if (!this.Characters.IsValidText(encoded))
        {
            return this.SetTerminalWriteError(
                "ASNXER0032",
                "The encoded XML token contains a forbidden character.",
                false);
        }

        if (!this.Resources.AllowsTextCharacters(encoded.Length))
        {
            return this.SetTerminalWriteError(
                "ASNR0009",
                "The encoded XML token exceeds the configured text-character limit.",
                true);
        }

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(encoded);
        if (!this.Resources.TryChargeAllocationBytes(bytes.Length))
        {
            return this.SetTerminalWriteError(
                "ASNR0005",
                "Encoding the XML token exceeds the configured allocation limit.",
                true);
        }

        int index = 0;
        while (index < bytes.Length)
        {
            this.Output.Write(bytes, index, 1);
            index = index + 1;
        }

        this.WrittenCount = this.WrittenCount + bytes.Length;
        this.TokenCount = this.TokenCount + 1;
        return new XerXmlWriteResult
        {
            Success = true,
            WrittenCount = bytes.Length
        };
    }

    internal XerXmlWriteResult CompleteWriter()
    {
        if (this.Terminal)
        {
            return this.CopyTerminalWriteResult();
        }

        if (!this.RootSeen)
        {
            return this.SetTerminalWriteError(
                "ASNXER0052",
                "An XML document requires a document element.",
                false);
        }

        if (this.ElementStack.Count > 0 || !this.RootClosed)
        {
            return this.SetTerminalWriteError(
                "ASNXER0053",
                "The XML document ends before all elements are closed.",
                false);
        }

        XerXmlWriteResult result = new XerXmlWriteResult
        {
            Success = true,
            WrittenCount = this.WrittenCount,
            Complete = true
        };
        this.Terminal = true;
        this.TerminalResult = result;
        return result;
    }

    internal void ResetWriter()
    {
        this.Resources = new ResourceCounter(this.Limits);
        this.WrittenCount = 0;
        this.TokenCount = 0;
        this.Terminal = false;
        this.TerminalResult = new XerXmlWriteResult();
        this.ElementStack.Clear();
        this.AssignedPrefixes.Clear();
        this.NextPrefix = 1;
        this.RootSeen = false;
        this.RootClosed = false;
        this.DeclarationWritten = false;
    }

    internal string EncodeStartToken(XerXmlToken token)
    {
        if (!this.Resources.AllowsCollectionElements(token.Attributes.Length))
        {
            this.SetTerminalWriteError(
                "ASNR0011",
                "The XML attribute count exceeds the configured limit.",
                true);
            return "";
        }

        bool documentLevel = this.ElementStack.Count == 0;
        if (documentLevel && this.RootSeen)
        {
            this.SetTerminalWriteError(
                "ASNXER0050",
                "An XML document must contain exactly one document element.",
                false);
            return "";
        }

        XerXmlNamespaceBinding[] inherited = [];
        if (!documentLevel)
        {
            inherited = this.ElementStack[
                this.ElementStack.Count - 1].NamespaceBindings;
        }

        XerXmlNamespaceContext context =
            new XerXmlNamespaceContext(inherited);
        System.Collections.Generic.List<string> declaredPrefixes =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < token.Attributes.Length)
        {
            XerXmlAttribute attribute = token.Attributes[index];
            if (attribute == null
                || !this.Characters.IsValidText(attribute.Value))
            {
                this.SetTerminalWriteError(
                    "ASNXER0034",
                    "The XML attribute name or value is invalid.",
                    false);
                return "";
            }

            if (this.IsNamespaceDeclaration(attribute))
            {
                string prefix = this.GetDeclaredPrefix(attribute);
                if (!this.ValidateWriterNamespaceDeclaration(
                    prefix,
                    attribute.Value,
                    declaredPrefixes))
                {
                    return "";
                }

                declaredPrefixes.Add(prefix);
                context.SetBinding(prefix, attribute.Value);
                attribute.NamespaceDeclaration = true;
                attribute.NamespacePrefix = prefix;
                attribute.NamespaceUri = XerXmlNamespaceContext.XmlnsNamespace;
            }

            index = index + 1;
        }

        XerXmlQualifiedName element = this.ResolveWriterElement(token, context);
        if (!element.Success)
        {
            this.SetTerminalWriteError(
                "ASNXER0033",
                element.ErrorMessage.Length == 0
                    ? "The XML element name is invalid."
                    : element.ErrorMessage,
                false);
            return "";
        }

        System.Collections.Generic.List<XerXmlAttribute> automaticDeclarations =
            new System.Collections.Generic.List<XerXmlAttribute>();
        if (element.NamespaceUri.Length > 0)
        {
            if (element.Prefix.Length == 0)
            {
                XerXmlNamespaceBinding defaultBinding =
                    context.FindBinding("");
                if (!defaultBinding.Found
                    || defaultBinding.NamespaceUri != element.NamespaceUri)
                {
                    element.Prefix = this.GetWriterPrefix(
                        element.NamespaceUri,
                        context);
                }
            }

            XerXmlNamespaceBinding elementBinding =
                context.FindBinding(element.Prefix);
            if (declaredPrefixes.Contains(element.Prefix)
                && (!elementBinding.Found
                    || elementBinding.NamespaceUri != element.NamespaceUri))
            {
                element.Prefix = this.GetWriterPrefix(
                    element.NamespaceUri,
                    context);
            }

            if (element.Prefix.Length > 0
                && !this.EnsureWriterBinding(
                    element.Prefix,
                    element.NamespaceUri,
                    context,
                    automaticDeclarations))
            {
                return "";
            }
        }
        else if (element.Prefix.Length == 0)
        {
            XerXmlNamespaceBinding defaultBinding = context.FindBinding("");
            if (defaultBinding.Found
                && defaultBinding.NamespaceUri.Length > 0)
            {
                context.SetBinding("", "");
                automaticDeclarations.Add(new XerXmlAttribute
                {
                    NamespaceDeclaration = true,
                    Value = ""
                });
            }
        }

        element.Name = element.Prefix.Length == 0
            ? element.LocalName
            : element.Prefix + ":" + element.LocalName;
        System.Collections.Generic.List<string> attributeNames =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> encodedAttributes =
            new System.Collections.Generic.List<string>();
        index = 0;
        while (index < token.Attributes.Length)
        {
            XerXmlAttribute attribute = token.Attributes[index];
            if (!attribute.NamespaceDeclaration)
            {
                XerXmlQualifiedName resolved =
                    this.ResolveWriterAttribute(attribute, context);
                if (!resolved.Success)
                {
                    this.SetTerminalWriteError(
                        "ASNXER0034",
                        resolved.ErrorMessage.Length == 0
                            ? "The XML attribute name or value is invalid."
                            : resolved.ErrorMessage,
                        false);
                    return "";
                }

                if (resolved.NamespaceUri.Length > 0)
                {
                    if (resolved.Prefix.Length == 0)
                    {
                        resolved.Prefix = this.GetWriterPrefix(
                            resolved.NamespaceUri,
                            context);
                    }

                    XerXmlNamespaceBinding attributeBinding =
                        context.FindBinding(resolved.Prefix);
                    if (declaredPrefixes.Contains(resolved.Prefix)
                        && (!attributeBinding.Found
                            || attributeBinding.NamespaceUri
                                != resolved.NamespaceUri))
                    {
                        resolved.Prefix = this.GetWriterPrefix(
                            resolved.NamespaceUri,
                            context);
                    }

                    if (!this.EnsureWriterBinding(
                        resolved.Prefix,
                        resolved.NamespaceUri,
                        context,
                        automaticDeclarations))
                    {
                        return "";
                    }
                }

                resolved.Name = resolved.Prefix.Length == 0
                    ? resolved.LocalName
                    : resolved.Prefix + ":" + resolved.LocalName;
                string key = resolved.NamespaceUri
                    + "\u0001" + resolved.LocalName;
                if (attributeNames.Contains(key))
                {
                    this.SetTerminalWriteError(
                        "ASNXER0049",
                        "The XML start element contains duplicate attributes.",
                        false);
                    return "";
                }

                attributeNames.Add(key);
                encodedAttributes.Add(
                    resolved.Name + "=\""
                    + this.EscapeAttributeValue(attribute.Value) + "\"");
            }

            index = index + 1;
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('<');
        result.Append(element.Name);
        index = 0;
        while (index < token.Attributes.Length)
        {
            XerXmlAttribute attribute = token.Attributes[index];
            if (attribute.NamespaceDeclaration)
            {
                result.Append(' ');
                result.Append(attribute.NamespacePrefix.Length == 0
                    ? "xmlns"
                    : "xmlns:" + attribute.NamespacePrefix);
                result.Append("=\"");
                result.Append(this.EscapeAttributeValue(attribute.Value));
                result.Append('"');
            }

            index = index + 1;
        }

        index = 0;
        while (index < automaticDeclarations.Count)
        {
            XerXmlAttribute declaration = automaticDeclarations[index];
            result.Append(' ');
            result.Append(declaration.NamespacePrefix.Length == 0
                ? "xmlns"
                : "xmlns:" + declaration.NamespacePrefix);
            result.Append("=\"");
            result.Append(this.EscapeAttributeValue(declaration.Value));
            result.Append('"');
            index = index + 1;
        }

        index = 0;
        while (index < encodedAttributes.Count)
        {
            result.Append(' ');
            result.Append(encodedAttributes[index]);
            index = index + 1;
        }

        result.Append(token.Kind == XerXmlTokenKind.EmptyElement
            ? "/>"
            : ">");
        token.Name = element.Name;
        token.Prefix = element.Prefix;
        token.LocalName = element.LocalName;
        token.NamespaceUri = element.NamespaceUri;
        token.NamespaceBindings = context.CopyBindings();
        if (documentLevel)
        {
            this.RootSeen = true;
        }

        if (token.Kind == XerXmlTokenKind.StartElement)
        {
            this.ElementStack.Add(new XerXmlElementFrame
            {
                Name = element.Name,
                Prefix = element.Prefix,
                LocalName = element.LocalName,
                NamespaceUri = element.NamespaceUri,
                NamespaceBindings = token.NamespaceBindings
            });
        }
        else if (documentLevel)
        {
            this.RootClosed = true;
        }

        return result.ToString();
    }

    internal string EncodeEndToken(XerXmlToken token)
    {
        if (this.ElementStack.Count == 0)
        {
            this.SetTerminalWriteError(
                "ASNXER0042",
                "The XML end element has no matching start element.",
                false);
            return "";
        }

        XerXmlElementFrame frame =
            this.ElementStack[this.ElementStack.Count - 1];
        if (token.Name.Length > 0 || token.LocalName.Length > 0)
        {
            XerXmlQualifiedName resolved;
            if (token.LocalName.Length > 0)
            {
                resolved = new XerXmlQualifiedName
                {
                    Success = this.Characters.IsValidNcName(token.LocalName),
                    Prefix = token.Prefix,
                    LocalName = token.LocalName,
                    NamespaceUri = token.NamespaceUri
                };
            }
            else
            {
                resolved = this.NamespaceResolver.ResolveElementName(
                    token.Name,
                    frame.NamespaceBindings);
            }

            if (!resolved.Success
                || resolved.LocalName != frame.LocalName
                || resolved.NamespaceUri != frame.NamespaceUri)
            {
                this.SetTerminalWriteError(
                    "ASNXER0043",
                    "The XML end element does not match its start element.",
                    false);
                return "";
            }
        }

        this.ElementStack.RemoveAt(this.ElementStack.Count - 1);
        if (this.ElementStack.Count == 0)
        {
            this.RootClosed = true;
        }

        return "</" + frame.Name + ">";
    }

    internal string EncodeTextToken(XerXmlToken token)
    {
        if (!this.Characters.IsValidText(token.Value))
        {
            this.SetTerminalWriteError(
                "ASNXER0035",
                "The XML character data contains a forbidden character.",
                false);
            return "";
        }

        string encoded = this.EscapeTextValue(token.Value);
        if (!this.Resources.AllowsTextCharacters(encoded.Length))
        {
            this.SetTerminalWriteError(
                "ASNR0009",
                "The encoded XML token exceeds the configured text-character limit.",
                true);
            return "";
        }

        if (this.ElementStack.Count == 0
            && !this.IsOnlyXmlWhitespace(token.Value))
        {
            this.SetTerminalWriteError(
                "ASNXER0040",
                "XML character data is not permitted outside the document element.",
                false);
            return "";
        }

        return encoded;
    }

    internal string EncodeCDataToken(XerXmlToken token)
    {
        if (!this.Characters.IsValidText(token.Value))
        {
            this.SetTerminalWriteError(
                "ASNXER0036",
                "The XML CDATA value contains a forbidden character.",
                false);
            return "";
        }

        if (this.ElementStack.Count == 0)
        {
            this.SetTerminalWriteError(
                "ASNXER0040",
                "An XML CDATA section is not permitted outside the document element.",
                false);
            return "";
        }

        return "<![CDATA["
            + token.Value.Replace(
                "]]>",
                "]]]]><![CDATA[>",
                System.StringComparison.Ordinal)
            + "]]>";
    }

    internal string EncodeCommentToken(XerXmlToken token)
    {
        if (!this.Characters.IsValidText(token.Value)
            || token.Value.Contains("--", System.StringComparison.Ordinal)
            || token.Value.EndsWith("-", System.StringComparison.Ordinal))
        {
            this.SetTerminalWriteError(
                "ASNXER0037",
                "The XML comment value is invalid.",
                false);
            return "";
        }

        return "<!--" + token.Value + "-->";
    }

    internal string EncodeProcessingInstructionToken(XerXmlToken token)
    {
        if (!this.Characters.IsValidName(token.Name)
            || !this.Characters.IsValidText(token.Value)
            || token.Value.Contains("?>", System.StringComparison.Ordinal)
            || string.Equals(
                token.Name,
                "xml",
                System.StringComparison.OrdinalIgnoreCase))
        {
            this.SetTerminalWriteError(
                "ASNXER0038",
                "The XML processing instruction is invalid.",
                false);
            return "";
        }

        return token.Value.Length == 0
            ? "<?" + token.Name + "?>"
            : "<?" + token.Name + " " + token.Value + "?>";
    }

    internal string EncodeXmlDeclarationToken(XerXmlToken token)
    {
        string version = token.XmlVersion.Length == 0
            ? "1.0"
            : token.XmlVersion;
        if (this.TokenCount != 0
            || this.DeclarationWritten
            || version != "1.0"
            || token.XmlEncoding.Length > 0
                && !string.Equals(
                    token.XmlEncoding,
                    "UTF-8",
                    System.StringComparison.OrdinalIgnoreCase))
        {
            this.SetTerminalWriteError(
                "ASNXER0045",
                "The XML declaration is invalid or is not the first token.",
                false);
            return "";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append("<?xml version=\"");
        result.Append(version);
        result.Append('"');
        if (token.XmlEncoding.Length > 0)
        {
            result.Append(" encoding=\"");
            result.Append(token.XmlEncoding);
            result.Append('"');
        }

        if (token.HasStandalone)
        {
            result.Append(" standalone=\"");
            result.Append(token.Standalone ? "yes" : "no");
            result.Append('"');
        }

        result.Append("?>");
        this.DeclarationWritten = true;
        return result.ToString();
    }

    internal XerXmlQualifiedName ResolveWriterElement(
        XerXmlToken token,
        XerXmlNamespaceContext context)
    {
        XerXmlQualifiedName result;
        if (token.Name.Length > 0)
        {
            result = this.NamespaceResolver.ResolveElementName(
                token.Name,
                context.CopyBindings());
            if (!result.Success)
            {
                return result;
            }
        }
        else
        {
            result = new XerXmlQualifiedName
            {
                Success = this.Characters.IsValidNcName(token.LocalName),
                Prefix = token.Prefix,
                LocalName = token.LocalName,
                NamespaceUri = token.NamespaceUri
            };
        }

        if (token.NamespaceUri.Length > 0)
        {
            result.NamespaceUri = token.NamespaceUri;
        }

        if (token.Prefix.Length > 0)
        {
            result.Prefix = token.Prefix;
        }

        if (!result.Success
            || !this.Characters.IsValidNcName(result.LocalName)
            || result.Prefix.Length > 0
                && !this.Characters.IsValidNcName(result.Prefix)
            || result.Prefix == "xmlns")
        {
            result.Success = false;
            result.ErrorMessage = "The XML element qualified name is invalid.";
        }

        return result;
    }

    internal XerXmlQualifiedName ResolveWriterAttribute(
        XerXmlAttribute attribute,
        XerXmlNamespaceContext context)
    {
        XerXmlQualifiedName result;
        if (attribute.Name.Length > 0)
        {
            result = this.NamespaceResolver.ResolveAttributeName(
                attribute.Name,
                context.CopyBindings());
            if (!result.Success)
            {
                return result;
            }
        }
        else
        {
            result = new XerXmlQualifiedName
            {
                Success = this.Characters.IsValidNcName(attribute.LocalName),
                Prefix = attribute.Prefix,
                LocalName = attribute.LocalName,
                NamespaceUri = attribute.NamespaceUri
            };
        }

        if (attribute.NamespaceUri.Length > 0)
        {
            result.NamespaceUri = attribute.NamespaceUri;
        }

        if (attribute.Prefix.Length > 0)
        {
            result.Prefix = attribute.Prefix;
        }

        if (!result.Success
            || !this.Characters.IsValidNcName(result.LocalName)
            || result.Prefix.Length > 0
                && !this.Characters.IsValidNcName(result.Prefix)
            || result.Prefix == "xmlns")
        {
            result.Success = false;
            result.ErrorMessage = "The XML attribute qualified name is invalid.";
        }

        return result;
    }

    internal bool IsNamespaceDeclaration(XerXmlAttribute attribute)
    {
        return attribute.NamespaceDeclaration
            || attribute.Name == "xmlns"
            || attribute.Name.StartsWith(
                "xmlns:",
                System.StringComparison.Ordinal);
    }

    internal string GetDeclaredPrefix(XerXmlAttribute attribute)
    {
        if (attribute.NamespacePrefix.Length > 0)
        {
            return attribute.NamespacePrefix;
        }

        if (attribute.Name == "xmlns")
        {
            return "";
        }

        return attribute.Name.StartsWith(
            "xmlns:",
            System.StringComparison.Ordinal)
                ? attribute.Name.Substring(6)
                : "";
    }

    internal bool ValidateWriterNamespaceDeclaration(
        string prefix,
        string namespaceUri,
        System.Collections.Generic.List<string> declaredPrefixes)
    {
        if (declaredPrefixes.Contains(prefix)
            || prefix.Length > 0 && !this.Characters.IsValidNcName(prefix)
            || prefix == "xmlns"
            || namespaceUri == XerXmlNamespaceContext.XmlnsNamespace
            || prefix == "xml"
                && namespaceUri != XerXmlNamespaceContext.XmlNamespace
            || prefix != "xml"
                && namespaceUri == XerXmlNamespaceContext.XmlNamespace
            || prefix.Length > 0 && namespaceUri.Length == 0)
        {
            this.SetTerminalWriteError(
                "ASNXER0051",
                "The XML namespace declaration violates reserved-prefix rules.",
                false);
            return false;
        }

        return true;
    }

    internal bool EnsureWriterBinding(
        string prefix,
        string namespaceUri,
        XerXmlNamespaceContext context,
        System.Collections.Generic.List<XerXmlAttribute> declarations)
    {
        if (prefix == "xml")
        {
            if (namespaceUri == XerXmlNamespaceContext.XmlNamespace)
            {
                return true;
            }

            this.SetTerminalWriteError(
                "ASNXER0051",
                "The XML namespace declaration violates reserved-prefix rules.",
                false);
            return false;
        }

        if (prefix.Length == 0
            || prefix == "xmlns"
            || namespaceUri.Length == 0
            || namespaceUri == XerXmlNamespaceContext.XmlnsNamespace
            || namespaceUri == XerXmlNamespaceContext.XmlNamespace)
        {
            this.SetTerminalWriteError(
                "ASNXER0051",
                "The XML namespace binding is invalid.",
                false);
            return false;
        }

        XerXmlNamespaceBinding current = context.FindBinding(prefix);
        if (current.Found && current.NamespaceUri == namespaceUri)
        {
            return true;
        }

        int index = 0;
        while (index < declarations.Count)
        {
            XerXmlAttribute declaration = declarations[index];
            if (declaration.NamespacePrefix == prefix
                && declaration.Value != namespaceUri)
            {
                this.SetTerminalWriteError(
                    "ASNXER0051",
                    "One prefix cannot denote two namespaces in the same element.",
                    false);
                return false;
            }

            index = index + 1;
        }

        context.SetBinding(prefix, namespaceUri);
        declarations.Add(new XerXmlAttribute
        {
            NamespaceDeclaration = true,
            NamespacePrefix = prefix,
            Value = namespaceUri
        });
        return true;
    }

    internal string GetWriterPrefix(
        string namespaceUri,
        XerXmlNamespaceContext context)
    {
        if (namespaceUri == XerXmlNamespaceContext.XmlNamespace)
        {
            return "xml";
        }

        int index = 0;
        while (index < this.AssignedPrefixes.Count)
        {
            XerXmlNamespaceBinding assigned = this.AssignedPrefixes[index];
            if (assigned.NamespaceUri == namespaceUri)
            {
                XerXmlNamespaceBinding current =
                    context.FindBinding(assigned.Prefix);
                if (!current.Found
                    || current.NamespaceUri == namespaceUri)
                {
                    return assigned.Prefix;
                }
            }

            index = index + 1;
        }

        string prefix = "";
        bool available = false;
        while (!available)
        {
            prefix = "ns" + this.NextPrefix.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            this.NextPrefix = this.NextPrefix + 1;
            available = !context.FindBinding(prefix).Found;
        }

        this.AssignedPrefixes.Add(new XerXmlNamespaceBinding
        {
            Found = true,
            Prefix = prefix,
            NamespaceUri = namespaceUri
        });
        return prefix;
    }

    internal bool IsOnlyXmlWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current != ' ' && current != '\t'
                && current != '\r' && current != '\n')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string EscapeTextValue(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '&')
            {
                result.Append("&amp;");
            }
            else if (current == '<')
            {
                result.Append("&lt;");
            }
            else if (current == '>')
            {
                result.Append("&gt;");
            }
            else
            {
                result.Append(current);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string EscapeAttributeValue(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '&')
            {
                result.Append("&amp;");
            }
            else if (current == '<')
            {
                result.Append("&lt;");
            }
            else if (current == '"')
            {
                result.Append("&quot;");
            }
            else if (current == '\t')
            {
                result.Append("&#x9;");
            }
            else if (current == '\n')
            {
                result.Append("&#xA;");
            }
            else if (current == '\r')
            {
                result.Append("&#xD;");
            }
            else
            {
                result.Append(current);
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal XerXmlWriteResult SetTerminalWriteError(
        string code,
        string message,
        bool limitExceeded)
    {
        XerXmlWriteResult result = this.CreateWriteError(
            code,
            message,
            limitExceeded);
        this.Terminal = true;
        this.TerminalResult = result;
        return result;
    }

    internal XerXmlWriteResult CreateWriteError(
        string code,
        string message,
        bool limitExceeded)
    {
        return new XerXmlWriteResult
        {
            LimitExceeded = limitExceeded,
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    internal XerXmlWriteResult CopyTerminalWriteResult()
    {
        return new XerXmlWriteResult
        {
            Success = this.TerminalResult.Success,
            WrittenCount = this.TerminalResult.WrittenCount,
            Complete = this.TerminalResult.Complete,
            LimitExceeded = this.TerminalResult.LimitExceeded,
            ErrorCode = this.TerminalResult.ErrorCode,
            ErrorMessage = this.TerminalResult.ErrorMessage
        };
    }
}
