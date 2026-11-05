namespace svg;

/// <summary>Records one reversible namespace binding update.</summary>
internal class SvgXmlNamespaceChange
{
    internal string Prefix;
    internal bool HadPrevious;
    internal string PreviousUri;

    /// <summary>Creates one reversible binding update.</summary>
    /// <param name="prefix">The changed prefix.</param>
    /// <param name="hadPrevious">Whether a binding previously existed.</param>
    /// <param name="previousUri">The previous namespace URI.</param>
    internal SvgXmlNamespaceChange(string prefix, bool hadPrevious, string previousUri)
    {
        Prefix = prefix;
        HadPrevious = hadPrevious;
        PreviousUri = previousUri;
    }
}

/// <summary>Groups namespace changes introduced by one start tag.</summary>
internal class SvgXmlNamespaceFrame
{
    internal System.Collections.Generic.List<SvgXmlNamespaceChange> Changes;

    /// <summary>Creates an empty namespace frame.</summary>
    internal SvgXmlNamespaceFrame()
    {
        Changes = new System.Collections.Generic.List<SvgXmlNamespaceChange>();
    }
}

/// <summary>Maintains scoped namespace bindings while processing an ordered XML token stream.</summary>
internal class SvgXmlNamespaceState
{
    string source;
    System.Collections.Generic.List<SvgXmlToken> tokens;
    System.Collections.Generic.Dictionary<string, string> bindings;
    System.Collections.Generic.List<SvgXmlNamespaceFrame> frames;
    SvgXmlNamespaceError error;
    int errorOffset;
    string message;

    /// <summary>Creates namespace-processing state.</summary>
    /// <param name="sourceText">The exact decoded source.</param>
    /// <param name="sourceTokens">The validated token stream.</param>
    internal SvgXmlNamespaceState(string sourceText, System.Collections.Generic.List<SvgXmlToken> sourceTokens)
    {
        source = sourceText;
        tokens = sourceTokens;
        bindings = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal);
        bindings["xml"] = SvgNamespaces.Xml;
        frames = new System.Collections.Generic.List<SvgXmlNamespaceFrame>();
        error = SvgXmlNamespaceError.None;
        errorOffset = -1;
        message = "";
    }

    /// <summary>Processes every namespace-bearing token.</summary>
    /// <returns>The earliest deterministic namespace result.</returns>
    internal SvgXmlNamespaceResult Resolve()
    {
        int index = 0;
        while (index < tokens.Count)
        {
            SvgXmlToken token = tokens[index];
            if (token.Kind == SvgXmlTokenKind.StartElement || token.Kind == SvgXmlTokenKind.EmptyElement) ResolveStart(token);
            else if (token.Kind == SvgXmlTokenKind.EndElement) ResolveEnd(token);
            index = index + 1;
        }
        if (error == SvgXmlNamespaceError.None)
        {
            return new SvgXmlNamespaceResult(true, SvgXmlNamespaceError.None, -1, 0, 0, "");
        }
        int[] location = new int[] { 1, 1 };
        FindLocation(errorOffset, location);
        return new SvgXmlNamespaceResult(false, error, errorOffset, location[0], location[1], message);
    }

    void ResolveStart(SvgXmlToken token)
    {
        int nameOffset = token.StartOffset + 1;
        ValidateQualifiedName(token.QualifiedName, nameOffset);
        int index = 0;
        while (index < token.Attributes.Count)
        {
            ValidateQualifiedName(token.Attributes[index].QualifiedName, token.Attributes[index].StartOffset);
            index = index + 1;
        }
        SvgXmlNamespaceFrame frame = new SvgXmlNamespaceFrame();
        index = 0;
        while (index < token.Attributes.Count)
        {
            SvgXmlAttributeToken attribute = token.Attributes[index];
            if (IsValidQualifiedName(attribute.QualifiedName) && IsNamespaceDeclaration(attribute)) ApplyDeclaration(attribute, frame);
            index = index + 1;
        }
        ResolveElementName(token, nameOffset);
        ResolveAttributes(token.Attributes);
        ValidateDuplicateAttributes(token.Attributes);
        if (token.Kind == SvgXmlTokenKind.StartElement) frames.Add(frame);
        else RestoreFrame(frame);
    }

    void ResolveEnd(SvgXmlToken token)
    {
        int nameOffset = token.StartOffset + 2;
        ValidateQualifiedName(token.QualifiedName, nameOffset);
        ResolveElementName(token, nameOffset);
        if (frames.Count == 0) return;
        SvgXmlNamespaceFrame frame = frames[frames.Count - 1];
        frames.RemoveAt(frames.Count - 1);
        RestoreFrame(frame);
    }

    void ApplyDeclaration(SvgXmlAttributeToken attribute, SvgXmlNamespaceFrame frame)
    {
        attribute.NamespaceUri = SvgNamespaces.Xmlns;
        if (attribute.NormalizedValue == null)
        {
            SetError(SvgXmlNamespaceError.InvalidArgument, attribute.StartOffset, "Namespace declarations require XML-normalized values.");
            return;
        }
        string declaredPrefix = attribute.QualifiedName == "xmlns" ? "" : attribute.LocalName;
        string uri = attribute.NormalizedValue;
        if (declaredPrefix == "xmlns")
        {
            SetError(SvgXmlNamespaceError.ReservedPrefix, attribute.StartOffset + 6, "The reserved xmlns prefix must not be declared.");
        }
        if (declaredPrefix == "xml" && uri != SvgNamespaces.Xml)
        {
            SetError(SvgXmlNamespaceError.ReservedPrefix, AttributeValueOffset(attribute), "The xml prefix must be bound only to its reserved namespace URI.");
        }
        if (declaredPrefix != "xml" && uri == SvgNamespaces.Xml)
        {
            SetError(SvgXmlNamespaceError.ReservedNamespace, AttributeValueOffset(attribute), "Only the xml prefix may be bound to the XML namespace URI.");
        }
        if (uri == SvgNamespaces.Xmlns)
        {
            SetError(SvgXmlNamespaceError.ReservedNamespace, AttributeValueOffset(attribute), "The XMLNS namespace URI must not be declared.");
        }
        if (declaredPrefix.Length != 0 && uri.Length == 0)
        {
            SetError(SvgXmlNamespaceError.EmptyPrefixedNamespace, AttributeValueOffset(attribute), "A prefixed namespace declaration must not have an empty namespace name.");
        }
        bool hadPrevious = bindings.ContainsKey(declaredPrefix);
        string previousUri = hadPrevious ? bindings[declaredPrefix] : "";
        frame.Changes.Add(new SvgXmlNamespaceChange(declaredPrefix, hadPrevious, previousUri));
        bindings[declaredPrefix] = uri;
    }

    void ResolveElementName(SvgXmlToken token, int nameOffset)
    {
        if (!IsValidQualifiedName(token.QualifiedName)) return;
        if (token.Prefix == "xmlns")
        {
            SetError(SvgXmlNamespaceError.ReservedPrefix, nameOffset, "Element names must not use the reserved xmlns prefix.");
            return;
        }
        if (token.Prefix.Length == 0)
        {
            token.NamespaceUri = bindings.ContainsKey("") ? bindings[""] : "";
            return;
        }
        if (!bindings.ContainsKey(token.Prefix))
        {
            SetError(SvgXmlNamespaceError.UndeclaredPrefix, nameOffset, "The element namespace prefix is not declared in scope.");
            return;
        }
        token.NamespaceUri = bindings[token.Prefix];
    }

    void ResolveAttributes(System.Collections.Generic.List<SvgXmlAttributeToken> attributes)
    {
        int index = 0;
        while (index < attributes.Count)
        {
            SvgXmlAttributeToken attribute = attributes[index];
            if (!IsValidQualifiedName(attribute.QualifiedName))
            {
                index = index + 1;
                continue;
            }
            if (IsNamespaceDeclaration(attribute)) attribute.NamespaceUri = SvgNamespaces.Xmlns;
            else if (attribute.Prefix.Length == 0) attribute.NamespaceUri = "";
            else if (!bindings.ContainsKey(attribute.Prefix))
            {
                SetError(SvgXmlNamespaceError.UndeclaredPrefix, attribute.StartOffset, "The attribute namespace prefix is not declared in scope.");
            }
            else attribute.NamespaceUri = bindings[attribute.Prefix];
            index = index + 1;
        }
    }

    void ValidateDuplicateAttributes(System.Collections.Generic.List<SvgXmlAttributeToken> attributes)
    {
        System.Collections.Generic.Dictionary<string, bool> expandedNames = new System.Collections.Generic.Dictionary<string, bool>(System.StringComparer.Ordinal);
        int index = 0;
        while (index < attributes.Count)
        {
            SvgXmlAttributeToken current = attributes[index];
            string namespaceUri = current.NamespaceUri == null ? "" : current.NamespaceUri;
            string expandedName = namespaceUri + "\0" + current.LocalName;
            if (expandedNames.ContainsKey(expandedName))
            {
                SetError(SvgXmlNamespaceError.DuplicateAttribute, current.StartOffset, "Attributes must not have duplicate expanded names.");
            }
            else expandedNames[expandedName] = true;
            index = index + 1;
        }
    }

    void ValidateQualifiedName(string value, int offset)
    {
        int firstColon = value.IndexOf(':');
        if (firstColon < 0) return;
        if (firstColon == 0)
        {
            SetError(SvgXmlNamespaceError.InvalidQualifiedName, offset, "A qualified name prefix must not be empty.");
            return;
        }
        if (firstColon == value.Length - 1)
        {
            SetError(SvgXmlNamespaceError.InvalidQualifiedName, offset + firstColon, "A qualified name local part must not be empty.");
            return;
        }
        int secondColon = value.IndexOf(':', firstColon + 1);
        if (secondColon >= 0)
        {
            SetError(SvgXmlNamespaceError.InvalidQualifiedName, offset + secondColon, "A qualified name must not contain more than one colon.");
            return;
        }
        if (!IsNcNameStart(value, firstColon + 1))
        {
            SetError(SvgXmlNamespaceError.InvalidQualifiedName, offset + firstColon + 1, "A qualified name local part must begin with an NCName start character.");
        }
    }

    static bool IsValidQualifiedName(string value)
    {
        int firstColon = value.IndexOf(':');
        if (firstColon < 0) return true;
        if (firstColon == 0 || firstColon == value.Length - 1) return false;
        if (value.IndexOf(':', firstColon + 1) >= 0) return false;
        return IsNcNameStart(value, firstColon + 1);
    }

    static bool IsNcNameStart(string value, int index)
    {
        int codePoint = value[index];
        if (codePoint >= 55296 && codePoint <= 56319 && index + 1 < value.Length)
        {
            int low = value[index + 1];
            if (low >= 56320 && low <= 57343) codePoint = 65536 + (codePoint - 55296) * 1024 + low - 56320;
        }
        return codePoint == '_' || codePoint >= 'A' && codePoint <= 'Z' || codePoint >= 'a' && codePoint <= 'z' ||
            codePoint >= 192 && codePoint <= 214 || codePoint >= 216 && codePoint <= 246 || codePoint >= 248 && codePoint <= 767 ||
            codePoint >= 880 && codePoint <= 893 || codePoint >= 895 && codePoint <= 8191 || codePoint >= 8204 && codePoint <= 8205 ||
            codePoint >= 8304 && codePoint <= 8591 || codePoint >= 11264 && codePoint <= 12271 || codePoint >= 12289 && codePoint <= 55295 ||
            codePoint >= 63744 && codePoint <= 64975 || codePoint >= 65008 && codePoint <= 65533 || codePoint >= 65536 && codePoint <= 983039;
    }

    static bool IsNamespaceDeclaration(SvgXmlAttributeToken attribute)
    {
        return attribute.QualifiedName == "xmlns" || attribute.Prefix == "xmlns";
    }

    static int AttributeValueOffset(SvgXmlAttributeToken attribute)
    {
        int quote = attribute.RawText.IndexOf(attribute.Quote);
        return attribute.StartOffset + quote + 1;
    }

    void RestoreFrame(SvgXmlNamespaceFrame frame)
    {
        int index = frame.Changes.Count - 1;
        while (index >= 0)
        {
            SvgXmlNamespaceChange change = frame.Changes[index];
            if (change.HadPrevious) bindings[change.Prefix] = change.PreviousUri;
            else bindings.Remove(change.Prefix);
            index = index - 1;
        }
    }

    void SetError(SvgXmlNamespaceError value, int offset, string errorMessage)
    {
        if (error != SvgXmlNamespaceError.None && offset >= errorOffset) return;
        error = value;
        errorOffset = offset;
        message = errorMessage;
    }

    void FindLocation(int offset, int[] location)
    {
        int line = 1;
        int column = 1;
        int index = 0;
        while (index < offset && index < source.Length)
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
