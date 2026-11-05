using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class XmlSerializationWriter
{
    internal XmlSerializationOptions Options;
    internal XmlVersion Version;
    internal string EncodingName;
    internal string NewLine;
    internal XmlEscaper Escaper;
    internal StringBuilder Result;
    internal OutputCharacterMap CharacterMap;
    internal bool XhtmlOutput;
    internal bool HtmlOutput;
    internal bool Html5Output;
    internal int UnescapedHtmlDepth;

    internal XmlSerializationWriter(XmlSerializationOptions options, XmlVersion sourceVersion, string encodingName)
    {
        Guard.NotNull(options, "options");
        Guard.NotNull(encodingName, "encodingName");
        Options = options;
        XhtmlOutput = options.Method == XmlSerializationMethod.Xhtml;
        HtmlOutput = options.Method == XmlSerializationMethod.Html;
        string? requestedHtmlVersion = options.HtmlVersion == null ? options.Version : options.HtmlVersion;
        Html5Output = (XhtmlOutput && string.Equals(options.HtmlVersion, "5.0", StringComparison.Ordinal)) || (HtmlOutput && IsHtmlSerializationVersionFive(requestedHtmlVersion == null ? "5.0" : requestedHtmlVersion));
        Version = ResolveXmlSerializationVersion(sourceVersion);
        EncodingName = encodingName;
        NewLine = options.NewLine == null ? "\n" : options.NewLine;
        Escaper = new XmlEscaper();
        Escaper.ConfigureXmlSerializationErrorCodes(XmlErrorCodes.SERE0006, XmlErrorCodes.SERE0003);
        Escaper.ConfigureXmlSerializationInvalidCharacterReplacement(options.InvalidCharacterReplacement);
        Result = new StringBuilder();
        UnescapedHtmlDepth = 0;
        ValidateXmlSerializationOptions();
        CharacterMap = new OutputCharacterMap(options.UseCharacterMaps);
    }

    internal string SerializeXmlDocumentTree(XmlDocument document)
    {
        Guard.NotNull(document, "document");
        new SerializationParameterValidator().ValidateNormalizedSerializationDocument(document, Options);
        XmlNode? documentNode = document.DocumentNode;
        if (documentNode == null)
        {
            throw CreateXmlSerializationError("The XML document has no document node.", XmlErrorCodes.SENR0001);
        }

        if (Html5Output)
        {
            NormalizeXhtmlSerializationPrefixes(documentNode);
        }

        FixupXmlSerializationNamespaces(documentNode);
        WriteXmlSerializationDeclaration();
        XmlNode[] children = documentNode.Children == null ? [] : documentNode.Children;
        bool wroteDoctype = false;
        bool automaticDoctypeAllowed = true;
        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (child.Kind == XmlNodeKind.DocumentType)
            {
                if (!wroteDoctype)
                {
                    WriteXmlSerializationDoctype(document, HtmlOutput ? "html" : FindXmlSerializationRootName(children));
                    wroteDoctype = true;
                }
            }
            else
            {
                bool parameterDoctype = Options.DoctypeSystem != null || HtmlOutput && Options.DoctypePublic != null;
                bool automaticXhtmlDoctype = XhtmlOutput && Html5Output && automaticDoctypeAllowed && child.Kind == XmlNodeKind.Element && IsRecognizedXhtmlElement(child) && string.Equals(XmlSerializationLocalName(child), "html", StringComparison.OrdinalIgnoreCase);
                bool automaticHtmlDoctype = HtmlOutput && Html5Output && automaticDoctypeAllowed && child.Kind == XmlNodeKind.Element && IsHtmlSerializationElement(child) && string.Equals(XmlSerializationLocalName(child), "html", StringComparison.OrdinalIgnoreCase);
                if (!wroteDoctype && child.Kind == XmlNodeKind.Element && (parameterDoctype || automaticXhtmlDoctype || automaticHtmlDoctype))
                {
                    WriteXmlSerializationDoctype(document, HtmlOutput ? "html" : XmlSerializationNodeName(child));
                    wroteDoctype = true;
                }

                WriteXmlSerializationNode(child, 0, false, false);
                if (child.Kind == XmlNodeKind.Text && !IsXmlSerializationWhitespace(child.Value == null ? "" : child.Value))
                {
                    automaticDoctypeAllowed = false;
                }
            }

            index = index + 1;
        }

        if (!wroteDoctype && (Options.DoctypeSystem != null || HtmlOutput && Options.DoctypePublic != null))
        {
            throw CreateXmlSerializationError("A doctype serialization parameter requires a document element.", XmlErrorCodes.SERE0003);
        }

        return Result.ToString();
    }

    internal XmlVersion ResolveXmlSerializationVersion(XmlVersion sourceVersion)
    {
        if (HtmlOutput)
        {
            return XmlVersion.Xml10;
        }

        if (Options.Version == null || Options.Version.Length == 0)
        {
            return sourceVersion == XmlVersion.Xml11 ? XmlVersion.Xml11 : XmlVersion.Xml10;
        }

        if (string.Equals(Options.Version, "1.0", StringComparison.Ordinal))
        {
            return XmlVersion.Xml10;
        }

        if (string.Equals(Options.Version, "1.1", StringComparison.Ordinal))
        {
            return XmlVersion.Xml11;
        }

        throw CreateXmlSerializationError("The requested XML version is not supported.", XmlErrorCodes.SESU0013);
    }

    internal void ValidateXmlSerializationOptions()
    {
        if (Options.Method != XmlSerializationMethod.Xml && Options.Method != XmlSerializationMethod.Xhtml && Options.Method != XmlSerializationMethod.Html)
        {
            throw CreateXmlSerializationError("This serialization writer requires the XML or XHTML output method.", XmlErrorCodes.SEPM0004);
        }

        if (Options.Standalone < XmlStandalone.Unspecified || Options.Standalone > XmlStandalone.No)
        {
            throw CreateXmlSerializationError("The standalone parameter is invalid.", XmlErrorCodes.SEPM0016);
        }

        if (Options.InvalidCharacterPolicy < XmlInvalidCharacterPolicy.Error || Options.InvalidCharacterPolicy > XmlInvalidCharacterPolicy.Replace)
        {
            throw CreateXmlSerializationError("The invalid-character policy is invalid.", XmlErrorCodes.SEPM0016);
        }

        if (!HtmlOutput && Options.OmitXmlDeclaration && Options.Standalone != XmlStandalone.Unspecified)
        {
            throw CreateXmlSerializationError("A standalone value cannot be emitted when the XML declaration is omitted.", XmlErrorCodes.SEPM0009);
        }

        if (!HtmlOutput && Options.OmitXmlDeclaration && Options.DoctypeSystem != null && Version != XmlVersion.Xml10)
        {
            throw CreateXmlSerializationError("An omitted XML declaration cannot be combined with a doctype-system parameter for this XML version.", XmlErrorCodes.SEPM0009);
        }

        if (!HtmlOutput && Options.UndeclarePrefixes && Version == XmlVersion.Xml10)
        {
            throw CreateXmlSerializationError("Prefix undeclarations require XML 1.1 output.", XmlErrorCodes.SEPM0010);
        }

        if (XhtmlOutput && Options.HtmlVersion != null && !IsValidXhtmlSerializationHtmlVersion(Options.HtmlVersion))
        {
            throw CreateXmlSerializationError("The XHTML html-version parameter must be a non-negative decimal value.", XmlErrorCodes.SEPM0016);
        }

        if (HtmlOutput)
        {
            string? requested = Options.HtmlVersion == null ? Options.Version : Options.HtmlVersion;
            if (requested != null && !IsValidHtmlSerializationVersion(requested))
            {
                throw CreateXmlSerializationError("The requested HTML version must be between 1.0 and 5.0.", XmlErrorCodes.SESU0013);
            }
        }

        ValidateXmlSerializationNormalizationForm();
    }

    internal void ValidateXmlSerializationNormalizationForm()
    {
        string? form = Options.NormalizationForm;
        if (form == null || form.Length == 0 || string.Equals(form, "none", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFC", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFD", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFKC", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFKD", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "fully-normalized", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw CreateXmlSerializationError("The normalization-form parameter is not supported.", XmlErrorCodes.SESU0011);
    }

    internal void FixupXmlSerializationNamespaces(XmlNode documentNode)
    {
        XmlNamespaceHandling handling = Version == XmlVersion.Xml11 ? XmlNamespaceHandling.Namespaces11 : XmlNamespaceHandling.Namespaces10;
        NamespaceFixup fixup = new NamespaceFixup(handling, null);
        NamespaceContext root = new NamespaceContext(null, []);
        XmlNode[] children = documentNode.Children == null ? [] : documentNode.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                fixup.FixupXmlNamespaceSubtree(children[index], root);
            }

            index = index + 1;
        }
    }

    internal void NormalizeXhtmlSerializationPrefixes(XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Element && node.Name != null && IsXhtmlSerializationSpecialNamespace(node.Name.NamespaceUri))
        {
            string? removedPrefix = node.Name.Prefix;
            node.Name.Prefix = null;
            node.Name.QualifiedName = node.Name.LocalName;
            XmlNamespaceBinding[] bindings = node.NamespaceBindings == null ? [] : node.NamespaceBindings;
            List<XmlNamespaceBinding> retained = new List<XmlNamespaceBinding>();
            int bindingIndex = 0;
            while (bindingIndex < bindings.Length)
            {
                XmlNamespaceBinding binding = bindings[bindingIndex];
                bool specialBinding = IsXhtmlSerializationSpecialNamespace(binding.NamespaceUri);
                bool attributeUsesPrefix = removedPrefix != null && XmlSerializationAttributeUsesPrefix(node, binding.Prefix == null ? "" : binding.Prefix);
                if (!specialBinding || attributeUsesPrefix)
                {
                    retained.Add(binding);
                }

                bindingIndex = bindingIndex + 1;
            }

            node.NamespaceBindings = retained.ToArray();
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            NormalizeXhtmlSerializationPrefixes(children[index]);
            index = index + 1;
        }
    }

    internal bool XmlSerializationAttributeUsesPrefix(XmlNode element, string prefix)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlName? name = attributes[index].Name;
            if (name != null && string.Equals(name.Prefix, prefix, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void WriteXmlSerializationDeclaration()
    {
        if (HtmlOutput || Options.OmitXmlDeclaration)
        {
            return;
        }

        Result.Append("<?xml version=\"");
        Result.Append(Version == XmlVersion.Xml11 ? "1.1" : "1.0");
        Result.Append("\" encoding=\"");
        Result.Append(EncodingName);
        Result.Append('"');
        if (Options.Standalone != XmlStandalone.Unspecified)
        {
            Result.Append(" standalone=\"");
            Result.Append(Options.Standalone == XmlStandalone.Yes ? "yes" : "no");
            Result.Append('"');
        }

        Result.Append("?>");
    }

    internal void WriteXmlSerializationDoctype(XmlDocument document, string rootName)
    {
        XmlDocumentType? retained = document.DocumentType;
        bool parameterDoctype = Options.DoctypeSystem != null || HtmlOutput && Options.DoctypePublic != null;
        string? publicIdentifier = parameterDoctype ? Options.DoctypePublic : retained == null ? null : retained.PublicIdentifier;
        string? systemIdentifier = parameterDoctype ? Options.DoctypeSystem : retained == null ? null : retained.SystemIdentifier;
        string? internalSubset = !parameterDoctype && retained != null ? retained.InternalSubset : null;
        string declaredName = rootName;
        if (declaredName.Length == 0 && retained != null && retained.Name != null)
        {
            declaredName = XmlSerializationName(retained.Name);
        }

        if (declaredName.Length == 0)
        {
            throw CreateXmlSerializationError("A document type requires an element name.", XmlErrorCodes.SERE0003);
        }

        Result.Append("<!DOCTYPE ");
        Result.Append(declaredName);
        if (publicIdentifier != null)
        {
            char publicQuote = publicIdentifier.Contains("\"", StringComparison.Ordinal) ? '\'' : '"';
            XmlCharacterEscapeContext publicContext = XmlCharacterEscapeContext.PublicIdentifier;
            Result.Append(" PUBLIC ");
            Result.Append(publicQuote);
            Result.Append(Escaper.EscapeXmlValue(publicIdentifier, Version, publicContext, Options.InvalidCharacterPolicy, publicQuote));
            Result.Append(publicQuote);
            if (systemIdentifier != null)
            {
                Result.Append(' ');
                WriteXmlSerializationSystemLiteral(systemIdentifier);
            }
        }
        else if (systemIdentifier != null)
        {
            Result.Append(" SYSTEM ");
            WriteXmlSerializationSystemLiteral(systemIdentifier);
        }

        if (internalSubset != null && internalSubset.Length > 0)
        {
            Result.Append(" [");
            Result.Append(internalSubset);
            Result.Append(']');
        }

        Result.Append('>');
    }

    internal void WriteXmlSerializationSystemLiteral(string value)
    {
        char quote = value.Contains("\"", StringComparison.Ordinal) ? '\'' : '"';
        if (value.Contains("\"", StringComparison.Ordinal) && value.Contains("'", StringComparison.Ordinal))
        {
            throw CreateXmlSerializationError("A system identifier contains both quote delimiters.", XmlErrorCodes.SERE0003);
        }

        XmlCharacterEscapeContext context = quote == '"' ? XmlCharacterEscapeContext.SystemLiteralDoubleQuoted : XmlCharacterEscapeContext.SystemLiteralSingleQuoted;
        Result.Append(quote);
        Result.Append(Escaper.EscapeXmlValue(value, Version, context, Options.InvalidCharacterPolicy, quote));
        Result.Append(quote);
    }

    internal void WriteXmlSerializationNode(XmlNode node, int depth, bool parentIndented, bool indentationSuppressed)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            WriteXmlSerializationElement(node, depth, indentationSuppressed);
        }
        else if (node.Kind == XmlNodeKind.Text)
        {
            string value = node.Value == null ? "" : node.Value;
            if (HtmlOutput && UnescapedHtmlDepth > 0)
            {
                ValidateHtmlSerializationValue(value);
                Result.Append(NormalizeXmlSerializationValue(value));
            }
            else
            {
                WriteXmlSerializationMappedValue(value, XmlCharacterEscapeContext.Text, false);
            }
        }
        else if (node.Kind == XmlNodeKind.CData)
        {
            WriteXmlSerializationMappedValue(node.Value == null ? "" : node.Value, XmlCharacterEscapeContext.CData, true);
        }
        else if (node.Kind == XmlNodeKind.Comment)
        {
            if (parentIndented)
            {
                WriteXmlSerializationIndent(depth);
            }

            Result.Append("<!--");
            Result.Append(Escaper.EscapeXmlValue(NormalizeXmlSerializationValue(node.Value == null ? "" : node.Value), Version, XmlCharacterEscapeContext.Comment, Options.InvalidCharacterPolicy, '\0'));
            Result.Append("-->");
        }
        else if (node.Kind == XmlNodeKind.ProcessingInstruction)
        {
            if (parentIndented)
            {
                WriteXmlSerializationIndent(depth);
            }

            Result.Append("<?");
            Result.Append(XmlSerializationNodeName(node));
            string value = node.Value == null ? "" : node.Value;
            if (value.Length > 0)
            {
                Result.Append(' ');
                string normalized = NormalizeXmlSerializationValue(value);
                if (HtmlOutput && normalized.Contains(">", StringComparison.Ordinal))
                {
                    throw CreateXmlSerializationError("An HTML processing instruction cannot contain '>'.", XmlErrorCodes.SERE0015);
                }

                Result.Append(Escaper.EscapeXmlValue(normalized, Version, XmlCharacterEscapeContext.ProcessingInstruction, Options.InvalidCharacterPolicy, '\0'));
            }

            Result.Append(HtmlOutput ? ">" : "?>");
        }
        else if (node.Kind == XmlNodeKind.EntityReference)
        {
            Result.Append('&');
            Result.Append(XmlSerializationNodeName(node));
            Result.Append(';');
        }
        else if (node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment)
        {
            XmlNode[] children = node.Children == null ? [] : node.Children;
            int index = 0;
            while (index < children.Length)
            {
                WriteXmlSerializationNode(children[index], depth, false, indentationSuppressed);
                index = index + 1;
            }
        }
        else if (node.Kind != XmlNodeKind.DocumentType)
        {
            throw CreateXmlSerializationError("The XML output method cannot serialize this node kind.", XmlErrorCodes.SENR0001);
        }
    }

    internal void WriteXmlSerializationElement(XmlNode element, int depth, bool indentationSuppressed)
    {
        bool htmlElement = HtmlOutput && IsHtmlSerializationElement(element);
        bool suppressHere = indentationSuppressed || MatchesXmlSerializationName(element.Name, Options.SuppressIndentation) || IsFormattedXhtmlElement(element) || IsFormattedHtmlElement(element);
        bool indentChildren = Options.Indent && !suppressHere && HasXmlSerializationMarkupOnlyChildren(element);
        Result.Append('<');
        Result.Append(XmlSerializationNodeName(element));
        WriteXmlSerializationNamespaces(element);
        WriteXmlSerializationAttributes(element);
        XmlNode[] children = element.Children == null ? [] : element.Children;
        bool insertsContentType = Options.IncludeContentType && (XhtmlOutput && IsXhtmlElementWithLocalName(element, "head") || HtmlOutput && IsHtmlElementWithLocalName(element, "head"));
        if (children.Length == 0 && !insertsContentType)
        {
            if (HtmlOutput && htmlElement)
            {
                if (!IsVoidHtmlSerializationElement(element))
                {
                    Result.Append("></");
                    Result.Append(XmlSerializationNodeName(element));
                    Result.Append('>');
                }
                else
                {
                    Result.Append('>');
                }
            }
            else if (XhtmlOutput && IsRecognizedXhtmlElement(element) && !IsExpectedEmptyXhtmlElement(element))
            {
                Result.Append("></");
                Result.Append(XmlSerializationNodeName(element));
                Result.Append('>');
            }
            else
            {
                Result.Append(XhtmlOutput && IsExpectedEmptyXhtmlElement(element) && !Html5Output ? " />" : "/>");
            }

            return;
        }

        Result.Append('>');
        if (insertsContentType)
        {
            WriteXhtmlSerializationContentTypeMeta(element);
        }

        bool cdata = !htmlElement && MatchesXmlSerializationName(element.Name, Options.CDataSectionElements);
        bool rawHtml = HtmlOutput && htmlElement && (string.Equals(XmlSerializationLocalName(element), "script", StringComparison.OrdinalIgnoreCase) || string.Equals(XmlSerializationLocalName(element), "style", StringComparison.OrdinalIgnoreCase));
        if (rawHtml)
        {
            UnescapedHtmlDepth = UnescapedHtmlDepth + 1;
        }

        int index = 0;
        while (index < children.Length)
        {
            XmlNode child = children[index];
            if (Options.IncludeContentType && (XhtmlOutput && IsExistingXhtmlContentTypeMeta(child) || HtmlOutput && IsExistingHtmlContentTypeMeta(child)))
            {
                index = index + 1;
                continue;
            }

            if (indentChildren)
            {
                WriteXmlSerializationIndent(depth + 1);
            }

            if (cdata && (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData))
            {
                WriteXmlSerializationPlainValue(child.Value == null ? "" : child.Value, XmlCharacterEscapeContext.CData, true);
            }
            else
            {
                WriteXmlSerializationNode(child, depth + 1, false, suppressHere);
            }

            index = index + 1;
        }

        if (indentChildren)
        {
            WriteXmlSerializationIndent(depth);
        }

        if (rawHtml)
        {
            UnescapedHtmlDepth = UnescapedHtmlDepth - 1;
        }

        Result.Append("</");
        Result.Append(XmlSerializationNodeName(element));
        Result.Append('>');
    }

    internal void WriteXmlSerializationNamespaces(XmlNode element)
    {
        XmlNamespaceBinding[] bindings = element.NamespaceBindings == null ? [] : element.NamespaceBindings;
        int index = 0;
        while (index < bindings.Length)
        {
            XmlNamespaceBinding? binding = bindings[index];
            if (binding == null || binding.Prefix == null || binding.NamespaceUri == null)
            {
                throw CreateXmlSerializationError("An element contains an incomplete namespace binding.", XmlErrorCodes.SENR0001);
            }

            bool ignoredHtmlXmlNamespace = HtmlOutput && string.Equals(binding.Prefix, "xml", StringComparison.Ordinal);
            if (!ignoredHtmlXmlNamespace && ((!binding.IsUndeclaration && binding.NamespaceUri.Length > 0) || binding.Prefix.Length == 0 || Options.UndeclarePrefixes && !HtmlOutput))
            {
                Result.Append(" xmlns");
                if (binding.Prefix.Length > 0)
                {
                    Result.Append(':');
                    Result.Append(binding.Prefix);
                }

                Result.Append("=\"");
                string namespaceUri = binding.IsUndeclaration ? "" : binding.NamespaceUri;
                WriteXmlSerializationPlainValue(namespaceUri, XmlCharacterEscapeContext.AttributeDoubleQuoted, false);
                Result.Append('"');
            }

            index = index + 1;
        }
    }

    internal void WriteXmlSerializationAttributes(XmlNode element)
    {
        XmlNode[] attributes = element.Attributes == null ? [] : element.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            if (!IsXmlSerializationNamespaceAttribute(attribute))
            {
                Result.Append(' ');
                Result.Append(XmlSerializationNodeName(attribute));
                if (HtmlOutput && IsMinimizedHtmlSerializationAttribute(element, attribute))
                {
                    index = index + 1;
                    continue;
                }

                Result.Append("=\"");
                string value = attribute.Value == null ? "" : attribute.Value;
                bool uriEscaped = (XhtmlOutput || HtmlOutput) && Options.EscapeUriAttributes && IsXhtmlSerializationUriAttribute(element, attribute);
                if (uriEscaped)
                {
                    value = EscapeXhtmlSerializationUri(value);
                }

                if (HtmlOutput)
                {
                    WriteHtmlSerializationAttributeValue(value, UnescapedHtmlDepth > 0, !uriEscaped);
                }
                else if (uriEscaped)
                {
                    WriteXmlSerializationPlainValue(value, XmlCharacterEscapeContext.AttributeDoubleQuoted, false);
                }
                else
                {
                    WriteXmlSerializationMappedValue(value, XmlCharacterEscapeContext.AttributeDoubleQuoted, false);
                }
                Result.Append('"');
            }

            index = index + 1;
        }
    }

    internal bool IsXmlSerializationNamespaceAttribute(XmlNode attribute)
    {
        XmlName? name = attribute.Name;
        return name != null && (string.Equals(name.NamespaceUri, XmlNamespaces.Xmlns, StringComparison.Ordinal) || string.Equals(name.Prefix, "xmlns", StringComparison.Ordinal) || name.Prefix == null && string.Equals(name.LocalName, "xmlns", StringComparison.Ordinal));
    }

    internal void WriteXmlSerializationMappedValue(string value, XmlCharacterEscapeContext context, bool cdata)
    {
        int position = 0;
        int plainStart = 0;
        while (position < value.Length)
        {
            int scalar = value[position];
            int characterCount = 1;
            if (scalar >= 0xD800 && scalar <= 0xDBFF && position + 1 < value.Length && value[position + 1] >= 0xDC00 && value[position + 1] <= 0xDFFF)
            {
                int second = value[position + 1];
                scalar = 0x10000 + (scalar - 0xD800) * 0x400 + second - 0xDC00;
                characterCount = 2;
            }

            int mapIndex = CharacterMap.FindOutputCharacterMap(scalar);
            if (mapIndex >= 0)
            {
                WriteXmlSerializationPlainValue(value.Substring(plainStart, position - plainStart), context, cdata);
                Result.Append(CharacterMap.Replacements[mapIndex]);
                position = position + characterCount;
                plainStart = position;
            }
            else
            {
                position = position + characterCount;
            }
        }

        WriteXmlSerializationPlainValue(value.Substring(plainStart), context, cdata);
    }

    internal void WriteXmlSerializationPlainValue(string value, XmlCharacterEscapeContext context, bool cdata)
    {
        if (value.Length == 0)
        {
            return;
        }

        string normalized = NormalizeXmlSerializationValue(value);
        if (cdata)
        {
            Result.Append("<![CDATA[");
            Result.Append(Escaper.EscapeXmlValue(normalized, Version, XmlCharacterEscapeContext.CData, Options.InvalidCharacterPolicy, '\0'));
            Result.Append("]]>");
            return;
        }

        Result.Append(Escaper.EscapeXmlValue(normalized, Version, context, Options.InvalidCharacterPolicy, context == XmlCharacterEscapeContext.AttributeDoubleQuoted ? '"' : '\0'));
    }

    internal bool HasXmlSerializationMarkupOnlyChildren(XmlNode element)
    {
        XmlNode[] children = element.Children == null ? [] : element.Children;
        if (children.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < children.Length)
        {
            XmlNodeKind kind = children[index].Kind;
            if (kind == XmlNodeKind.Text || kind == XmlNodeKind.CData || kind == XmlNodeKind.EntityReference)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool MatchesXmlSerializationName(XmlName? candidate, XmlName[]? names)
    {
        if (candidate == null || names == null)
        {
            return false;
        }

        int index = 0;
        while (index < names.Length)
        {
            XmlName? name = names[index];
            bool exact = name != null && string.Equals(candidate.LocalName, name.LocalName, StringComparison.Ordinal) && string.Equals(candidate.NamespaceUri == null ? "" : candidate.NamespaceUri, name.NamespaceUri == null ? "" : name.NamespaceUri, StringComparison.Ordinal);
            bool nullNamespaceCaseMatch = (XhtmlOutput || HtmlOutput) && name != null && (candidate.NamespaceUri == null || candidate.NamespaceUri.Length == 0) && (name.NamespaceUri == null || name.NamespaceUri.Length == 0) && string.Equals(candidate.LocalName, name.LocalName, StringComparison.OrdinalIgnoreCase);
            bool html5NamespaceMatch = Html5Output && name != null && string.Equals(candidate.LocalName, name.LocalName, StringComparison.OrdinalIgnoreCase) && ((candidate.NamespaceUri == null || candidate.NamespaceUri.Length == 0) && string.Equals(name.NamespaceUri, "http://www.w3.org/1999/xhtml", StringComparison.Ordinal) || (name.NamespaceUri == null || name.NamespaceUri.Length == 0) && string.Equals(candidate.NamespaceUri, "http://www.w3.org/1999/xhtml", StringComparison.Ordinal));
            if (exact || nullNamespaceCaseMatch || html5NamespaceMatch)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void WriteXmlSerializationIndent(int depth)
    {
        Result.Append(NewLine);
        int index = 0;
        while (index < depth * 2)
        {
            Result.Append(' ');
            index = index + 1;
        }
    }

    internal string NormalizeXmlSerializationValue(string value)
    {
        if (HtmlOutput)
        {
            ValidateHtmlSerializationValue(value);
        }

        string? form = Options.NormalizationForm;
        SerializationNormalization.ValidateFullyNormalizedValue(value, form);
        if (form == null || form.Length == 0 || string.Equals(form, "none", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (string.Equals(form, "NFD", StringComparison.OrdinalIgnoreCase))
        {
            return value.Normalize(global::System.Text.NormalizationForm.FormD);
        }

        if (string.Equals(form, "NFKC", StringComparison.OrdinalIgnoreCase))
        {
            return value.Normalize(global::System.Text.NormalizationForm.FormKC);
        }

        if (string.Equals(form, "NFKD", StringComparison.OrdinalIgnoreCase))
        {
            return value.Normalize(global::System.Text.NormalizationForm.FormKD);
        }

        return value.Normalize(global::System.Text.NormalizationForm.FormC);
    }

    internal string FindXmlSerializationRootName(XmlNode[] children)
    {
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                return XmlSerializationNodeName(children[index]);
            }

            index = index + 1;
        }

        return "";
    }

    internal string XmlSerializationNodeName(XmlNode node)
    {
        XmlName? name = node.Name;
        if (name == null)
        {
            throw CreateXmlSerializationError("A named XML node has no name.", XmlErrorCodes.SENR0001);
        }

        return XmlSerializationName(name);
    }

    internal string XmlSerializationName(XmlName name)
    {
        if (name.QualifiedName != null && name.QualifiedName.Length > 0)
        {
            return name.QualifiedName;
        }

        string localName = name.LocalName == null ? "" : name.LocalName;
        return name.Prefix == null || name.Prefix.Length == 0 ? localName : name.Prefix + ":" + localName;
    }

    internal bool IsValidXhtmlSerializationHtmlVersion(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int decimalPoints = 0;
        int digits = 0;
        int index = value[0] == '+' ? 1 : 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.')
            {
                decimalPoints = decimalPoints + 1;
                if (decimalPoints > 1)
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

    internal bool IsXhtmlSerializationSpecialNamespace(string? namespaceUri)
    {
        return string.Equals(namespaceUri, "http://www.w3.org/1999/xhtml", StringComparison.Ordinal) || string.Equals(namespaceUri, "http://www.w3.org/2000/svg", StringComparison.Ordinal) || string.Equals(namespaceUri, "http://www.w3.org/1998/Math/MathML", StringComparison.Ordinal);
    }

    internal bool IsRecognizedXhtmlElement(XmlNode element)
    {
        XmlName? name = element.Name;
        if (name == null)
        {
            return false;
        }

        if (string.Equals(name.NamespaceUri, "http://www.w3.org/1999/xhtml", StringComparison.Ordinal))
        {
            return true;
        }

        return Html5Output && (name.NamespaceUri == null || name.NamespaceUri.Length == 0) && IsKnownXhtmlSerializationElement(name.LocalName == null ? "" : name.LocalName);
    }

    internal bool IsKnownXhtmlSerializationElement(string localName)
    {
        string names = "|a|abbr|address|area|article|aside|audio|b|base|bdi|bdo|blockquote|body|br|button|canvas|caption|cite|code|col|colgroup|data|datalist|dd|del|details|dfn|dialog|div|dl|dt|em|embed|fieldset|figcaption|figure|footer|form|h1|h2|h3|h4|h5|h6|head|header|hgroup|hr|html|i|iframe|img|input|ins|kbd|label|legend|li|link|main|map|mark|menu|meta|meter|nav|noscript|object|ol|optgroup|option|output|p|picture|pre|progress|q|rp|rt|ruby|s|samp|script|search|section|select|slot|small|source|span|strong|style|sub|summary|sup|table|tbody|td|template|textarea|tfoot|th|thead|time|title|tr|track|u|ul|var|video|wbr|";
        return names.Contains("|" + localName.ToLowerInvariant() + "|", StringComparison.Ordinal);
    }

    internal bool IsExpectedEmptyXhtmlElement(XmlNode element)
    {
        if (!IsRecognizedXhtmlElement(element))
        {
            return false;
        }

        string localName = XmlSerializationLocalName(element).ToLowerInvariant();
        string legacy = "|area|base|br|col|embed|hr|img|input|link|meta|basefont|frame|isindex|param|";
        string html5 = "|area|base|br|col|embed|hr|img|input|keygen|link|meta|param|source|track|wbr|";
        return (Html5Output ? html5 : legacy).Contains("|" + localName + "|", StringComparison.Ordinal);
    }

    internal bool IsFormattedXhtmlElement(XmlNode element)
    {
        if (!XhtmlOutput || !IsRecognizedXhtmlElement(element))
        {
            return false;
        }

        string localName = XmlSerializationLocalName(element).ToLowerInvariant();
        return string.Equals(localName, "pre", StringComparison.Ordinal) || string.Equals(localName, "script", StringComparison.Ordinal) || string.Equals(localName, "style", StringComparison.Ordinal) || string.Equals(localName, "title", StringComparison.Ordinal) || string.Equals(localName, "textarea", StringComparison.Ordinal);
    }

    internal bool IsXhtmlElementWithLocalName(XmlNode element, string localName)
    {
        return IsRecognizedXhtmlElement(element) && string.Equals(XmlSerializationLocalName(element), localName, StringComparison.OrdinalIgnoreCase);
    }

    internal string XmlSerializationLocalName(XmlNode node)
    {
        return node.Name == null || node.Name.LocalName == null ? "" : node.Name.LocalName;
    }

    internal void WriteXhtmlSerializationContentTypeMeta(XmlNode head)
    {
        string prefix = head.Name == null || head.Name.Prefix == null || head.Name.Prefix.Length == 0 ? "" : head.Name.Prefix + ":";
        string mediaType = Options.MediaType == null || Options.MediaType.Length == 0 ? (HtmlOutput ? "text/html" : "application/xhtml+xml") : Options.MediaType;
        Result.Append('<');
        Result.Append(prefix);
        Result.Append("meta http-equiv=\"Content-Type\" content=\"");
        WriteXmlSerializationPlainValue(mediaType + "; charset=" + EncodingName, XmlCharacterEscapeContext.AttributeDoubleQuoted, false);
        if (HtmlOutput)
        {
            Result.Append("\">");
        }
        else
        {
            Result.Append(Html5Output ? "\"/>" : "\" />");
        }
    }

    internal bool IsExistingXhtmlContentTypeMeta(XmlNode node)
    {
        if (node.Kind != XmlNodeKind.Element || !IsXhtmlElementWithLocalName(node, "meta"))
        {
            return false;
        }

        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            XmlName? name = attribute.Name;
            if (name != null && (name.NamespaceUri == null || name.NamespaceUri.Length == 0) && string.Equals(name.LocalName, "http-equiv", StringComparison.OrdinalIgnoreCase) && string.Equals((attribute.Value == null ? "" : attribute.Value).Trim(), "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXhtmlSerializationUriAttribute(XmlNode element, XmlNode attribute)
    {
        bool applicableElement = XhtmlOutput && IsRecognizedXhtmlElement(element) || HtmlOutput && IsHtmlSerializationElement(element);
        if (!applicableElement || attribute.Name == null || attribute.Name.NamespaceUri != null && attribute.Name.NamespaceUri.Length > 0)
        {
            return false;
        }

        string attributeName = attribute.Name.LocalName == null ? "" : attribute.Name.LocalName.ToLowerInvariant();
        string elementName = XmlSerializationLocalName(element).ToLowerInvariant();
        if (string.Equals(attributeName, "action", StringComparison.Ordinal)) return string.Equals(elementName, "form", StringComparison.Ordinal);
        if (string.Equals(attributeName, "archive", StringComparison.Ordinal) || string.Equals(attributeName, "classid", StringComparison.Ordinal) || string.Equals(attributeName, "data", StringComparison.Ordinal)) return string.Equals(elementName, "object", StringComparison.Ordinal);
        if (string.Equals(attributeName, "background", StringComparison.Ordinal)) return string.Equals(elementName, "body", StringComparison.Ordinal);
        if (string.Equals(attributeName, "href", StringComparison.Ordinal)) return "|a|area|base|link|".Contains("|" + elementName + "|", StringComparison.Ordinal);
        if (string.Equals(attributeName, "src", StringComparison.Ordinal)) return "|audio|embed|frame|iframe|img|input|script|source|track|video|".Contains("|" + elementName + "|", StringComparison.Ordinal);
        if (string.Equals(attributeName, "cite", StringComparison.Ordinal)) return "|blockquote|del|ins|q|".Contains("|" + elementName + "|", StringComparison.Ordinal);
        if (string.Equals(attributeName, "codebase", StringComparison.Ordinal)) return "|applet|object|".Contains("|" + elementName + "|", StringComparison.Ordinal);
        if (string.Equals(attributeName, "datasrc", StringComparison.Ordinal)) return "|button|div|input|object|select|span|table|textarea|".Contains("|" + elementName + "|", StringComparison.Ordinal);
        if (string.Equals(attributeName, "formaction", StringComparison.Ordinal)) return string.Equals(elementName, "button", StringComparison.Ordinal) || string.Equals(elementName, "input", StringComparison.Ordinal);
        if (string.Equals(attributeName, "longdesc", StringComparison.Ordinal)) return "|frame|iframe|img|".Contains("|" + elementName + "|", StringComparison.Ordinal);
        if (string.Equals(attributeName, "usemap", StringComparison.Ordinal)) return "|img|input|object|".Contains("|" + elementName + "|", StringComparison.Ordinal);
        if (string.Equals(attributeName, "manifest", StringComparison.Ordinal)) return string.Equals(elementName, "html", StringComparison.Ordinal);
        if (string.Equals(attributeName, "profile", StringComparison.Ordinal)) return string.Equals(elementName, "head", StringComparison.Ordinal);
        if (string.Equals(attributeName, "poster", StringComparison.Ordinal)) return string.Equals(elementName, "video", StringComparison.Ordinal);
        if (string.Equals(attributeName, "name", StringComparison.Ordinal)) return string.Equals(elementName, "a", StringComparison.Ordinal);
        if (string.Equals(attributeName, "for", StringComparison.Ordinal)) return string.Equals(elementName, "script", StringComparison.Ordinal);
        if (string.Equals(attributeName, "icon", StringComparison.Ordinal)) return string.Equals(elementName, "command", StringComparison.Ordinal);
        return string.Equals(attributeName, "value", StringComparison.Ordinal) && string.Equals(elementName, "input", StringComparison.Ordinal);
    }

    internal string EscapeXhtmlSerializationUri(string value)
    {
        StringBuilder escaped = new StringBuilder(value.Length);
        int position = 0;
        while (position < value.Length)
        {
            int scalar = value[position];
            int characterCount = 1;
            if (scalar >= 0xD800 && scalar <= 0xDBFF && position + 1 < value.Length && value[position + 1] >= 0xDC00 && value[position + 1] <= 0xDFFF)
            {
                int second = value[position + 1];
                scalar = 0x10000 + (scalar - 0xD800) * 0x400 + second - 0xDC00;
                characterCount = 2;
            }

            if (scalar <= 0x7F)
            {
                escaped.Append(value, position, characterCount);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value.Substring(position, characterCount));
                int byteIndex = 0;
                while (byteIndex < bytes.Length)
                {
                    escaped.Append('%');
                    escaped.Append(bytes[byteIndex].ToString("X2"));
                    byteIndex = byteIndex + 1;
                }
            }

            position = position + characterCount;
        }

        return escaped.ToString();
    }

    internal bool IsXmlSerializationWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != ' ' && character != '\t' && character != '\r' && character != '\n')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsValidHtmlSerializationVersion(string value)
    {
        if (!IsValidXhtmlSerializationHtmlVersion(value))
        {
            return false;
        }

        double number = 0;
        double fraction = 0.1;
        bool afterPoint = false;
        int index = value[0] == '+' ? 1 : 0;
        while (index < value.Length)
        {
            char character = value[index];
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

    internal bool IsHtmlSerializationVersionFive(string value)
    {
        if (!IsValidXhtmlSerializationHtmlVersion(value))
        {
            return false;
        }

        int index = value[0] == '+' ? 1 : 0;
        int whole = 0;
        bool afterPoint = false;
        bool nonzeroFraction = false;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '.')
            {
                afterPoint = true;
            }
            else if (!afterPoint)
            {
                whole = whole * 10 + character - '0';
            }
            else if (character != '0')
            {
                nonzeroFraction = true;
            }

            index = index + 1;
        }

        return whole == 5 && !nonzeroFraction;
    }

    internal bool IsHtmlSerializationElement(XmlNode element)
    {
        XmlName? name = element.Name;
        if (name == null)
        {
            return false;
        }

        if (name.NamespaceUri == null || name.NamespaceUri.Length == 0)
        {
            return true;
        }

        return Html5Output && string.Equals(name.NamespaceUri, "http://www.w3.org/1999/xhtml", StringComparison.Ordinal);
    }

    internal bool IsVoidHtmlSerializationElement(XmlNode element)
    {
        if (!IsHtmlSerializationElement(element))
        {
            return false;
        }

        string localName = XmlSerializationLocalName(element).ToLowerInvariant();
        string legacy = "|area|base|basefont|br|col|embed|frame|hr|img|input|isindex|link|meta|param|";
        string html5 = "|area|base|br|col|embed|hr|img|input|keygen|link|meta|param|source|track|wbr|";
        return (Html5Output ? html5 : legacy).Contains("|" + localName + "|", StringComparison.Ordinal);
    }

    internal bool IsFormattedHtmlElement(XmlNode element)
    {
        if (!HtmlOutput || !IsHtmlSerializationElement(element))
        {
            return false;
        }

        string localName = XmlSerializationLocalName(element).ToLowerInvariant();
        return string.Equals(localName, "pre", StringComparison.Ordinal) || string.Equals(localName, "script", StringComparison.Ordinal) || string.Equals(localName, "style", StringComparison.Ordinal) || string.Equals(localName, "title", StringComparison.Ordinal) || string.Equals(localName, "textarea", StringComparison.Ordinal);
    }

    internal bool IsHtmlElementWithLocalName(XmlNode element, string localName)
    {
        return IsHtmlSerializationElement(element) && string.Equals(XmlSerializationLocalName(element), localName, StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsExistingHtmlContentTypeMeta(XmlNode node)
    {
        if (node.Kind != XmlNodeKind.Element || !IsHtmlElementWithLocalName(node, "meta"))
        {
            return false;
        }

        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int index = 0;
        while (index < attributes.Length)
        {
            XmlNode attribute = attributes[index];
            XmlName? name = attribute.Name;
            if (name != null && (name.NamespaceUri == null || name.NamespaceUri.Length == 0) && string.Equals(name.LocalName, "http-equiv", StringComparison.OrdinalIgnoreCase) && string.Equals((attribute.Value == null ? "" : attribute.Value).Trim(), "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsMinimizedHtmlSerializationAttribute(XmlNode element, XmlNode attribute)
    {
        if (!IsHtmlSerializationElement(element) || attribute.Name == null || attribute.Name.NamespaceUri != null && attribute.Name.NamespaceUri.Length > 0)
        {
            return false;
        }

        string name = attribute.Name.LocalName == null ? "" : attribute.Name.LocalName;
        string booleanNames = "|allowfullscreen|async|autofocus|autoplay|checked|compact|controls|declare|default|defer|disabled|formnovalidate|hidden|inert|ismap|itemscope|loop|multiple|muted|nohref|nomodule|noshade|novalidate|nowrap|open|playsinline|readonly|required|reversed|selected|";
        return booleanNames.Contains("|" + name.ToLowerInvariant() + "|", StringComparison.Ordinal) && string.Equals(attribute.Value, name, StringComparison.OrdinalIgnoreCase);
    }

    internal void WriteHtmlSerializationAttributeValue(string value, bool raw, bool mapCharacters)
    {
        if (!mapCharacters)
        {
            WriteHtmlSerializationAttributePlain(value, raw);
            return;
        }

        int position = 0;
        int plainStart = 0;
        while (position < value.Length)
        {
            int scalar = value[position];
            int characterCount = 1;
            if (scalar >= 0xD800 && scalar <= 0xDBFF && position + 1 < value.Length && value[position + 1] >= 0xDC00 && value[position + 1] <= 0xDFFF)
            {
                int second = value[position + 1];
                scalar = 0x10000 + (scalar - 0xD800) * 0x400 + second - 0xDC00;
                characterCount = 2;
            }

            int mapIndex = CharacterMap.FindOutputCharacterMap(scalar);
            if (mapIndex >= 0)
            {
                WriteHtmlSerializationAttributePlain(value.Substring(plainStart, position - plainStart), raw);
                Result.Append(CharacterMap.Replacements[mapIndex]);
                position = position + characterCount;
                plainStart = position;
            }
            else
            {
                position = position + characterCount;
            }
        }

        WriteHtmlSerializationAttributePlain(value.Substring(plainStart), raw);
    }

    internal void WriteHtmlSerializationAttributePlain(string value, bool raw)
    {
        if (value.Length == 0)
        {
            return;
        }

        string normalized = NormalizeXmlSerializationValue(value);
        if (raw)
        {
            Result.Append(normalized);
            return;
        }

        string escaped = Escaper.EscapeXmlValue(normalized, Version, XmlCharacterEscapeContext.AttributeDoubleQuoted, Options.InvalidCharacterPolicy, '"');
        escaped = escaped.Replace("&lt;", "<", StringComparison.Ordinal);
        escaped = escaped.Replace("&amp;{", "&{", StringComparison.Ordinal);
        Result.Append(escaped);
    }

    internal void ValidateHtmlSerializationValue(string value)
    {
        if (Html5Output)
        {
            return;
        }

        int index = 0;
        while (index < value.Length)
        {
            int scalar = value[index];
            int characterCount = 1;
            if (scalar >= 0xD800 && scalar <= 0xDBFF && index + 1 < value.Length && value[index + 1] >= 0xDC00 && value[index + 1] <= 0xDFFF)
            {
                int second = value[index + 1];
                scalar = 0x10000 + (scalar - 0xD800) * 0x400 + second - 0xDC00;
                characterCount = 2;
            }

            if (scalar >= 0x7F && scalar <= 0x9F || scalar >= 0x1 && scalar <= 0x8 || scalar == 0xB || scalar == 0xC || scalar >= 0xE && scalar <= 0x1F)
            {
                throw CreateXmlSerializationError("The value contains a control character that is not permitted in HTML before version 5.", XmlErrorCodes.SERE0014);
            }

            index = index + characterCount;
        }
    }

    internal XmlSerializationException CreateXmlSerializationError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }
}
