namespace svg;

/// <summary>Maintains bounded token-to-document construction state.</summary>
internal class SvgXmlBuildState
{
    string source;
    System.Collections.Generic.List<SvgXmlToken> tokens;
    SvgLoadOptions options;
    SvgSourceUri sourceUri;
    SvgDocument document;
    System.Collections.Generic.List<SvgNode> openElements;
    int nodeCount;
    int attributeCount;
    long textCharacters;
    bool sawDoctype;
    SvgXmlBuildError error;
    int errorOffset;
    int errorLine;
    int errorColumn;
    string message;

    /// <summary>Creates token construction state.</summary>
    /// <param name="sourceText">The exact decoded source.</param>
    /// <param name="sourceTokens">The validated tokens.</param>
    /// <param name="loadOptions">The construction options.</param>
    /// <param name="sourceUriText">The source URI text.</param>
    internal SvgXmlBuildState(string sourceText, System.Collections.Generic.List<SvgXmlToken> sourceTokens, SvgLoadOptions loadOptions, string sourceUriText)
    {
        source = sourceText;
        tokens = sourceTokens;
        options = loadOptions;
        sourceUri = new SvgSourceUri(sourceUriText);
        document = CreateDocument(sourceUriText);
        openElements = new System.Collections.Generic.List<SvgNode>();
        nodeCount = 0;
        attributeCount = 0;
        textCharacters = 0;
        sawDoctype = false;
        error = SvgXmlBuildError.None;
        errorOffset = -1;
        errorLine = 0;
        errorColumn = 0;
        message = "";
    }

    /// <summary>Constructs the complete token stream.</summary>
    /// <returns>A complete or partial construction result.</returns>
    internal SvgXmlBuildResult Build()
    {
        if (options.CancellationToken.IsCancellationRequested)
        {
            SetErrorAtEnd(SvgXmlBuildError.Cancelled, "XML construction was cancelled.");
        }
        int index = 0;
        while (index < tokens.Count && error == SvgXmlBuildError.None)
        {
            if (options.CancellationToken.IsCancellationRequested) SetError(SvgXmlBuildError.Cancelled, tokens[index], "XML construction was cancelled.");
            else ProcessToken(tokens[index]);
            index = index + 1;
        }
        if (error == SvgXmlBuildError.None && openElements.Count != 0)
        {
            SetErrorAtEnd(SvgXmlBuildError.UnclosedElement, "An element is not closed at the end of the XML source.");
        }
        if (error == SvgXmlBuildError.None && document.DocumentElement == null)
        {
            SetErrorAtEnd(SvgXmlBuildError.MissingDocumentElement, "The XML source does not contain a document element.");
        }
        if (error != SvgXmlBuildError.None)
        {
            return new SvgXmlBuildResult(false, error, document, errorOffset, errorLine, errorColumn, message);
        }
        return new SvgXmlBuildResult(true, SvgXmlBuildError.None, document, -1, 0, 0, "");
    }

    SvgDocument CreateDocument(string sourceUriText)
    {
        SvgDocument created = new SvgDocument();
        created.BaseUri = options.BaseUri.Length == 0 ? sourceUriText : options.BaseUri;
        created.Source = sourceUri;
        created.DocumentNode.SourceMapKind = SvgSourceMapKind.Original;
        if (options.PreserveSource)
        {
            int[] end = new int[] { 1, 1 };
            FindEndLocation(end);
            created.SourceText = source;
            created.SourceSpan = new SvgSourceSpan(sourceUri, 0, 1, 1, source.Length, end[0], end[1]);
            created.DocumentNode.SourceSpan = created.SourceSpan;
        }
        return created;
    }

    void ProcessToken(SvgXmlToken token)
    {
        switch (token.Kind)
        {
            case SvgXmlTokenKind.Declaration:
                break;
            case SvgXmlTokenKind.StartElement:
                AddElement(token, false);
                break;
            case SvgXmlTokenKind.EmptyElement:
                AddElement(token, true);
                break;
            case SvgXmlTokenKind.EndElement:
                CloseElement(token);
                break;
            case SvgXmlTokenKind.Text:
                AddText(token, token.NormalizedValue, false);
                break;
            case SvgXmlTokenKind.Reference:
                AddText(token, token.NormalizedValue, true);
                break;
            case SvgXmlTokenKind.CData:
                AddValueNode(token, SvgNodeKind.CData, "", "", "", "", token.NormalizedValue, true);
                break;
            case SvgXmlTokenKind.Comment:
                AddValueNode(token, SvgNodeKind.Comment, "", "", "", "", token.NormalizedValue, false);
                break;
            case SvgXmlTokenKind.ProcessingInstruction:
                AddValueNode(token, SvgNodeKind.ProcessingInstruction, token.QualifiedName, token.Prefix, token.LocalName, "", token.NormalizedValue, false);
                break;
            case SvgXmlTokenKind.Doctype:
                AddDoctype(token);
                break;
            default:
                SetError(SvgXmlBuildError.InvalidArgument, token, "The XML token kind is invalid for document construction.");
                break;
        }
    }

    void AddElement(SvgXmlToken token, bool empty)
    {
        if (token.NamespaceUri == null)
        {
            SetError(SvgXmlBuildError.InvalidArgument, token, "Element tokens require resolved namespace URIs.");
            return;
        }
        int depth = openElements.Count + 1;
        if (depth > options.Limits.Tree.MaxDepth)
        {
            SetError(SvgXmlBuildError.DepthLimitExceeded, token, "The XML element depth exceeded MaxDepth.");
            return;
        }
        if (openElements.Count == 0 && document.DocumentElement != null)
        {
            SetError(SvgXmlBuildError.MultipleDocumentElements, token, "The XML source contains more than one document element.");
            return;
        }
        if (!ReserveNode(token)) return;
        if (!ReserveAttributes(token)) return;
        SvgNode element = new SvgNode(SvgNodeKind.Element, token.QualifiedName, token.Prefix, token.LocalName, token.NamespaceUri, "");
        RetainToken(element, token);
        SvgNode parent = CurrentParent();
        document.AttachInitialChild(parent, element);
        int index = 0;
        while (index < token.Attributes.Count)
        {
            SvgXmlAttributeToken raw = token.Attributes[index];
            if (raw.NamespaceUri == null || raw.NormalizedValue == null)
            {
                SetError(SvgXmlBuildError.InvalidArgument, token, "Attribute tokens require normalized values and resolved namespace URIs.");
                return;
            }
            SvgAttribute attribute = new SvgAttribute(raw.QualifiedName, raw.Prefix, raw.LocalName, raw.NamespaceUri, raw.NormalizedValue);
            attribute.SourceMapKind = SvgSourceMapKind.Original;
            if (options.PreserveSource)
            {
                attribute.RawToken = raw;
                attribute.SourceSpan = new SvgSourceSpan(sourceUri, raw.StartOffset, raw.StartLine, raw.StartColumn, raw.EndOffset, raw.EndLine, raw.EndColumn);
            }
            document.AttachInitialAttribute(element, attribute);
            index = index + 1;
        }
        attributeCount = attributeCount + token.Attributes.Count;
        if (!empty) openElements.Add(element);
    }

    void CloseElement(SvgXmlToken token)
    {
        if (openElements.Count == 0)
        {
            SetError(SvgXmlBuildError.UnexpectedEndElement, token, "The end tag has no open element.");
            return;
        }
        SvgNode element = openElements[openElements.Count - 1];
        if (element.QualifiedName != token.QualifiedName)
        {
            SetError(SvgXmlBuildError.MismatchedEndElement, token, "The end tag does not match the current open element.");
            return;
        }
        openElements.RemoveAt(openElements.Count - 1);
        if (options.PreserveSource)
        {
            element.EndToken = token;
            SvgXmlToken? start = element.RawToken;
            element.SourceSpan = new SvgSourceSpan(sourceUri, (start ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).StartOffset, start.StartLine, start.StartColumn, token.EndOffset, token.EndLine, token.EndColumn);
        }
    }

    void AddText(SvgXmlToken token, string? value, bool reference)
    {
        if (value == null)
        {
            SetError(SvgXmlBuildError.InvalidArgument, token, "Text tokens require normalized values.");
            return;
        }
        if (openElements.Count == 0 && (reference || !IsXmlWhitespace(value)))
        {
            SetError(SvgXmlBuildError.CharacterDataOutsideElement, token, "Non-whitespace character data is not allowed outside the document element.");
            return;
        }
        AddValueNode(token, SvgNodeKind.Text, "", "", "", "", value, true);
    }

    void AddValueNode(SvgXmlToken token, SvgNodeKind kind, string qualifiedName, string prefix, string localName, string namespaceUri, string? value, bool countsAsText)
    {
        if (value == null)
        {
            SetError(SvgXmlBuildError.InvalidArgument, token, "XML content tokens require normalized values.");
            return;
        }
        if (kind == SvgNodeKind.CData && openElements.Count == 0)
        {
            SetError(SvgXmlBuildError.CharacterDataOutsideElement, token, "CDATA is not allowed outside the document element.");
            return;
        }
        if (countsAsText && !ReserveText(token, value.Length)) return;
        if (!ReserveNode(token)) return;
        SvgNode node = new SvgNode(kind, qualifiedName, prefix, localName, namespaceUri, value);
        RetainToken(node, token);
        document.AttachInitialChild(CurrentParent(), node);
    }

    void AddDoctype(SvgXmlToken token)
    {
        if (options.DtdPolicy == SvgDtdPolicy.Prohibit)
        {
            SetError(SvgXmlBuildError.DtdProhibited, token, "Document type declarations are prohibited by the configured DTD policy.");
            return;
        }
        if (sawDoctype)
        {
            SetError(SvgXmlBuildError.MultipleDoctypes, token, "The XML source contains more than one document type declaration.");
            return;
        }
        if (document.DocumentElement != null || openElements.Count != 0)
        {
            SetError(SvgXmlBuildError.MisplacedDoctype, token, "A document type declaration must precede the document element.");
            return;
        }
        if (!ReserveNode(token)) return;
        if (token.NormalizedValue == null)
        {
            SetError(SvgXmlBuildError.InvalidArgument, token, "Document type tokens require normalized values.");
            return;
        }
        sawDoctype = true;
        SvgNode node = new SvgNode(SvgNodeKind.Doctype, ReadDoctypeName(token.Value), "", ReadDoctypeName(token.Value), "", token.NormalizedValue);
        RetainToken(node, token);
        document.AttachInitialChild(document.DocumentNode, node);
    }

    bool ReserveNode(SvgXmlToken token)
    {
        if (nodeCount >= options.Limits.Tree.MaxNodes)
        {
            SetError(SvgXmlBuildError.NodeLimitExceeded, token, "The XML node count exceeded MaxNodes.");
            return false;
        }
        nodeCount = nodeCount + 1;
        return true;
    }

    bool ReserveAttributes(SvgXmlToken token)
    {
        int count = token.Attributes.Count;
        if (count > options.Limits.Tree.MaxAttributesPerElement)
        {
            SvgXmlAttributeToken failure = token.Attributes[options.Limits.Tree.MaxAttributesPerElement];
            SetErrorAtLocation(SvgXmlBuildError.ElementAttributeLimitExceeded, failure.StartOffset, failure.StartLine, failure.StartColumn, "The element attribute count exceeded MaxAttributesPerElement.");
            return false;
        }
        if (attributeCount > options.Limits.Tree.MaxTotalAttributes - count)
        {
            int available = options.Limits.Tree.MaxTotalAttributes - attributeCount;
            SvgXmlAttributeToken failure = token.Attributes[available < 0 ? 0 : available];
            SetErrorAtLocation(SvgXmlBuildError.TotalAttributeLimitExceeded, failure.StartOffset, failure.StartLine, failure.StartColumn, "The total attribute count exceeded MaxTotalAttributes.");
            return false;
        }
        return true;
    }

    bool ReserveText(SvgXmlToken token, int length)
    {
        if (textCharacters > options.Limits.Tree.MaxTextCharacters - length)
        {
            SetError(SvgXmlBuildError.TextLimitExceeded, token, "The cumulative XML text count exceeded MaxTextCharacters.");
            return false;
        }
        textCharacters = textCharacters + length;
        return true;
    }

    SvgNode CurrentParent()
    {
        return openElements.Count == 0 ? document.DocumentNode : openElements[openElements.Count - 1];
    }

    void RetainToken(SvgNode node, SvgXmlToken token)
    {
        node.SourceMapKind = SvgSourceMapKind.Original;
        if (!options.PreserveSource) return;
        node.RawToken = token;
        node.SourceSpan = new SvgSourceSpan(sourceUri, token.StartOffset, token.StartLine, token.StartColumn, token.EndOffset, token.EndLine, token.EndColumn);
    }

    static bool IsXmlWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current != ' ' && current != '\t' && current != '\r' && current != '\n') return false;
            index = index + 1;
        }
        return true;
    }

    static string ReadDoctypeName(string value)
    {
        int index = 0;
        while (index < value.Length && (value[index] == ' ' || value[index] == '\t' || value[index] == '\r' || value[index] == '\n')) index = index + 1;
        int start = index;
        while (index < value.Length && value[index] != ' ' && value[index] != '\t' && value[index] != '\r' && value[index] != '\n' && value[index] != '[') index = index + 1;
        return value.Substring(start, index - start);
    }

    void SetError(SvgXmlBuildError value, SvgXmlToken token, string errorMessage)
    {
        SetErrorAtLocation(value, token.StartOffset, token.StartLine, token.StartColumn, errorMessage);
    }

    void SetErrorAtLocation(SvgXmlBuildError value, int offset, int line, int column, string errorMessage)
    {
        if (error != SvgXmlBuildError.None) return;
        error = value;
        errorOffset = offset;
        errorLine = line;
        errorColumn = column;
        message = errorMessage;
    }

    void SetErrorAtEnd(SvgXmlBuildError value, string errorMessage)
    {
        int[] end = new int[] { 1, 1 };
        FindEndLocation(end);
        SetErrorAtLocation(value, source.Length, end[0], end[1], errorMessage);
    }

    void FindEndLocation(int[] location)
    {
        int line = 1;
        int column = 1;
        int index = 0;
        while (index < source.Length)
        {
            char current = source[index];
            index = index + 1;
            if (current == '\r')
            {
                line = line + 1;
                column = 1;
            }
            else if (current == '\n')
            {
                if (index < 2 || source[index - 2] != '\r')
                {
                    line = line + 1;
                    column = 1;
                }
            }
            else column = column + 1;
        }
        location[0] = line;
        location[1] = column;
    }
}

/// <summary>Maintains bounded construction state for a caller-owned XML reader.</summary>
internal class SvgXmlReaderBuildState
{
    System.Xml.XmlReader reader;
    SvgLoadOptions options;
    SvgDocument document;
    System.Collections.Generic.List<SvgNode> openElements;
    int nodeCount;
    int attributeCount;
    long textCharacters;
    bool sawDoctype;
    SvgXmlBuildError error;
    string message;

    /// <summary>Creates reader construction state.</summary>
    /// <param name="xmlReader">The caller-owned reader.</param>
    /// <param name="loadOptions">The construction options.</param>
    /// <param name="sourceUri">The source URI text.</param>
    internal SvgXmlReaderBuildState(System.Xml.XmlReader xmlReader, SvgLoadOptions loadOptions, string sourceUri)
    {
        reader = xmlReader;
        options = loadOptions;
        document = new SvgDocument();
        document.BaseUri = options.BaseUri.Length == 0 ? sourceUri : options.BaseUri;
        document.Source = new SvgSourceUri(sourceUri);
        document.DocumentNode.SourceMapKind = SvgSourceMapKind.Original;
        openElements = new System.Collections.Generic.List<SvgNode>();
        nodeCount = 0;
        attributeCount = 0;
        textCharacters = 0;
        sawDoctype = false;
        error = SvgXmlBuildError.None;
        message = "";
    }

    /// <summary>Consumes the reader through its end state.</summary>
    /// <returns>A complete or partial reader construction result.</returns>
    internal SvgXmlBuildResult Build()
    {
        if (reader.ReadState == System.Xml.ReadState.Closed || reader.ReadState == System.Xml.ReadState.Error)
        {
            return new SvgXmlBuildResult(false, SvgXmlBuildError.InvalidArgument, document, -1, 0, 0, "The XML reader is not readable.");
        }
        if (reader.ReadState == System.Xml.ReadState.Interactive && reader.NodeType == System.Xml.XmlNodeType.Attribute) reader.MoveToElement();
        bool processCurrent = reader.ReadState == System.Xml.ReadState.Interactive;
        while (error == SvgXmlBuildError.None)
        {
            if (options.CancellationToken.IsCancellationRequested)
            {
                SetError(SvgXmlBuildError.Cancelled, "XML construction was cancelled.");
                break;
            }
            if (!processCurrent && !reader.Read()) break;
            processCurrent = false;
            ProcessCurrent();
        }
        if (error == SvgXmlBuildError.None && openElements.Count != 0) SetError(SvgXmlBuildError.UnclosedElement, "An element is not closed at the end of the XML reader.");
        if (error == SvgXmlBuildError.None && document.DocumentElement == null) SetError(SvgXmlBuildError.MissingDocumentElement, "The XML reader does not contain a document element.");
        if (error != SvgXmlBuildError.None) return new SvgXmlBuildResult(false, error, document, -1, 0, 0, message);
        return new SvgXmlBuildResult(true, SvgXmlBuildError.None, document, -1, 0, 0, "");
    }

    void ProcessCurrent()
    {
        System.Xml.XmlNodeType nodeType = reader.NodeType;
        if (nodeType == System.Xml.XmlNodeType.Element)
        {
            AddElement();
        }
        else if (nodeType == System.Xml.XmlNodeType.EndElement)
        {
            CloseElement();
        }
        else if (nodeType == System.Xml.XmlNodeType.Text || nodeType == System.Xml.XmlNodeType.Whitespace || nodeType == System.Xml.XmlNodeType.SignificantWhitespace)
        {
            AddText(reader.Value);
        }
        else if (nodeType == System.Xml.XmlNodeType.CDATA)
        {
            AddValueNode(SvgNodeKind.CData, "", "", "", "", reader.Value, true);
        }
        else if (nodeType == System.Xml.XmlNodeType.Comment)
        {
            AddValueNode(SvgNodeKind.Comment, "", "", "", "", reader.Value, false);
        }
        else if (nodeType == System.Xml.XmlNodeType.ProcessingInstruction)
        {
            AddValueNode(SvgNodeKind.ProcessingInstruction, reader.Name, reader.Prefix, reader.LocalName, "", reader.Value, false);
        }
        else if (nodeType == System.Xml.XmlNodeType.DocumentType)
        {
            AddDoctype();
        }
        else if (nodeType == System.Xml.XmlNodeType.EntityReference)
        {
            SetError(SvgXmlBuildError.UnresolvedEntity, "The XML reader exposed an unresolved entity reference.");
        }
    }

    void AddElement()
    {
        if (reader.Name.Length > options.Limits.Tree.MaxNameCharacters)
        {
            SetError(SvgXmlBuildError.NameLimitExceeded, "An XML reader element name exceeded MaxNameCharacters.");
            return;
        }
        int depth = openElements.Count + 1;
        if (depth > options.Limits.Tree.MaxDepth)
        {
            SetError(SvgXmlBuildError.DepthLimitExceeded, "The XML element depth exceeded MaxDepth.");
            return;
        }
        if (openElements.Count == 0 && document.DocumentElement != null)
        {
            SetError(SvgXmlBuildError.MultipleDocumentElements, "The XML reader contains more than one document element.");
            return;
        }
        if (!ReserveNode()) return;
        int count = reader.AttributeCount;
        if (count > options.Limits.Tree.MaxAttributesPerElement)
        {
            SetError(SvgXmlBuildError.ElementAttributeLimitExceeded, "The element attribute count exceeded MaxAttributesPerElement.");
            return;
        }
        if (attributeCount > options.Limits.Tree.MaxTotalAttributes - count)
        {
            SetError(SvgXmlBuildError.TotalAttributeLimitExceeded, "The total attribute count exceeded MaxTotalAttributes.");
            return;
        }
        SvgNode element = new SvgNode(SvgNodeKind.Element, reader.Name, reader.Prefix, reader.LocalName, reader.NamespaceURI, "");
        element.SourceMapKind = SvgSourceMapKind.Original;
        document.AttachInitialChild(CurrentParent(), element);
        int index = 0;
        while (index < count)
        {
            reader.MoveToAttribute(index);
            if (reader.Name.Length > options.Limits.Tree.MaxNameCharacters)
            {
                reader.MoveToElement();
                SetError(SvgXmlBuildError.NameLimitExceeded, "An XML reader attribute name exceeded MaxNameCharacters.");
                return;
            }
            SvgAttribute attribute = new SvgAttribute(reader.Name, reader.Prefix, reader.LocalName, reader.NamespaceURI, reader.Value);
            attribute.SourceMapKind = SvgSourceMapKind.Original;
            document.AttachInitialAttribute(element, attribute);
            index = index + 1;
        }
        if (count != 0) reader.MoveToElement();
        attributeCount = attributeCount + count;
        if (!reader.IsEmptyElement) openElements.Add(element);
    }

    void CloseElement()
    {
        if (openElements.Count == 0)
        {
            SetError(SvgXmlBuildError.UnexpectedEndElement, "The end element has no open element.");
            return;
        }
        SvgNode element = openElements[openElements.Count - 1];
        if (element.QualifiedName != reader.Name)
        {
            SetError(SvgXmlBuildError.MismatchedEndElement, "The end element does not match the current open element.");
            return;
        }
        openElements.RemoveAt(openElements.Count - 1);
    }

    void AddText(string value)
    {
        if (openElements.Count == 0 && !IsXmlWhitespace(value))
        {
            SetError(SvgXmlBuildError.CharacterDataOutsideElement, "Non-whitespace character data is not allowed outside the document element.");
            return;
        }
        AddValueNode(SvgNodeKind.Text, "", "", "", "", value, true);
    }

    void AddValueNode(SvgNodeKind kind, string qualifiedName, string prefix, string localName, string namespaceUri, string value, bool countsAsText)
    {
        if (kind == SvgNodeKind.CData && openElements.Count == 0)
        {
            SetError(SvgXmlBuildError.CharacterDataOutsideElement, "CDATA is not allowed outside the document element.");
            return;
        }
        if (countsAsText && !ReserveText(value.Length)) return;
        if (!ReserveNode()) return;
        SvgNode node = new SvgNode(kind, qualifiedName, prefix, localName, namespaceUri, value);
        node.SourceMapKind = SvgSourceMapKind.Original;
        document.AttachInitialChild(CurrentParent(), node);
    }

    void AddDoctype()
    {
        if (options.DtdPolicy == SvgDtdPolicy.Prohibit)
        {
            SetError(SvgXmlBuildError.DtdProhibited, "Document type declarations are prohibited by the configured DTD policy.");
            return;
        }
        if (sawDoctype)
        {
            SetError(SvgXmlBuildError.MultipleDoctypes, "The XML reader contains more than one document type declaration.");
            return;
        }
        if (document.DocumentElement != null)
        {
            SetError(SvgXmlBuildError.MisplacedDoctype, "A document type declaration must precede the document element.");
            return;
        }
        if (!ReserveNode()) return;
        if (reader.Name.Length > options.Limits.Tree.MaxNameCharacters)
        {
            SetError(SvgXmlBuildError.NameLimitExceeded, "An XML reader doctype name exceeded MaxNameCharacters.");
            return;
        }
        sawDoctype = true;
        SvgNode node = new SvgNode(SvgNodeKind.Doctype, reader.Name, "", reader.Name, "", reader.Value);
        node.SourceMapKind = SvgSourceMapKind.Original;
        document.AttachInitialChild(document.DocumentNode, node);
    }

    bool ReserveNode()
    {
        if (nodeCount >= options.Limits.Tree.MaxNodes)
        {
            SetError(SvgXmlBuildError.NodeLimitExceeded, "The XML node count exceeded MaxNodes.");
            return false;
        }
        nodeCount = nodeCount + 1;
        return true;
    }

    bool ReserveText(int length)
    {
        if (textCharacters > options.Limits.Tree.MaxTextCharacters - length)
        {
            SetError(SvgXmlBuildError.TextLimitExceeded, "The cumulative XML text count exceeded MaxTextCharacters.");
            return false;
        }
        textCharacters = textCharacters + length;
        return true;
    }

    SvgNode CurrentParent()
    {
        return openElements.Count == 0 ? document.DocumentNode : openElements[openElements.Count - 1];
    }

    static bool IsXmlWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current != ' ' && current != '\t' && current != '\r' && current != '\n') return false;
            index = index + 1;
        }
        return true;
    }

    void SetError(SvgXmlBuildError value, string errorMessage)
    {
        if (error != SvgXmlBuildError.None) return;
        error = value;
        message = errorMessage;
    }
}
