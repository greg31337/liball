namespace svg;

internal class SvgXmlWriteNamespaceDeclaration
{
    internal string Prefix;
    internal string NamespaceUri;

    internal SvgXmlWriteNamespaceDeclaration(string prefix, string namespaceUri)
    {
        Prefix = prefix;
        NamespaceUri = namespaceUri;
    }
}

internal class SvgXmlWrittenAttribute
{
    internal string QualifiedName;
    internal string LocalName;
    internal string NamespaceUri;
    internal string Value;
    internal int SourceOrder;

    internal SvgXmlWrittenAttribute(string qualifiedName, string localName, string namespaceUri, string value, int sourceOrder)
    {
        QualifiedName = qualifiedName;
        LocalName = localName;
        NamespaceUri = namespaceUri;
        Value = value;
        SourceOrder = sourceOrder;
    }
}

internal class SvgXmlWriteNamespaceHint
{
    internal string Prefix;
    internal string NamespaceUri;

    internal SvgXmlWriteNamespaceHint(string prefix, string namespaceUri)
    {
        Prefix = prefix;
        NamespaceUri = namespaceUri;
    }
}

internal class SvgXmlWriteScope
{
    internal System.Collections.Generic.Dictionary<string, string> Bindings;

    internal SvgXmlWriteScope()
    {
        Bindings = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal);
        Bindings.Add("xml", SvgNamespaces.Xml);
    }

    internal SvgXmlWriteScope Copy()
    {
        SvgXmlWriteScope result = new SvgXmlWriteScope();
        result.Bindings = new System.Collections.Generic.Dictionary<string, string>(Bindings, System.StringComparer.Ordinal);
        return result;
    }
}

internal class SvgXmlElementWritePlan
{
    internal string QualifiedName;
    internal SvgXmlWriteScope? Scope;
    internal System.Collections.Generic.List<SvgXmlWriteNamespaceDeclaration> Declarations;
    internal System.Collections.Generic.List<SvgXmlWrittenAttribute> Attributes;
    internal bool IndentChildren;
    internal bool ReuseStart;
    internal bool ReuseEnd;
    internal bool ReuseEmpty;

    internal SvgXmlElementWritePlan()
    {
        QualifiedName = "";
        Scope = null;
        Declarations = new System.Collections.Generic.List<SvgXmlWriteNamespaceDeclaration>();
        Attributes = new System.Collections.Generic.List<SvgXmlWrittenAttribute>();
        IndentChildren = false;
        ReuseStart = false;
        ReuseEnd = false;
        ReuseEmpty = false;
    }
}

internal class SvgXmlWriteFrame
{
    internal SvgNode Node;
    internal SvgXmlWriteScope? ParentScope;
    internal SvgXmlElementWritePlan? Plan;
    internal int Depth;
    internal int ChildIndex;
    internal bool Entered;

    internal SvgXmlWriteFrame(SvgNode node, SvgXmlWriteScope? parentScope, int depth)
    {
        Node = node;
        ParentScope = parentScope;
        Plan = null;
        Depth = depth;
        ChildIndex = 0;
        Entered = false;
    }
}

internal static class SvgXmlWriter
{
    internal static SvgWriteResult WriteDocument(SvgDocument? document, SvgWriteOptions? options)
    {
        SvgWriteResult result = new SvgWriteResult();
        result.SourceVersion = document == null ? 0 : document.Version;
        if (document == null || document.DocumentNode == null || options == null)
        {
            return Invalid(result, SvgWriteError.InvalidArgument, "A document and write options are required.");
        }
        SvgXmlWriterState state = new SvgXmlWriterState(options, result);
        return state.WriteDocument(document);
    }

    internal static SvgWriteResult WriteNode(SvgNode? node, SvgWriteOptions? options)
    {
        SvgWriteResult result = new SvgWriteResult();
        result.SourceVersion = node == null || node.OwnerDocument == null ? 0 : node.OwnerDocument.Version;
        if (node == null || options == null || node.Kind == SvgNodeKind.Document)
        {
            return Invalid(result, SvgWriteError.InvalidArgument, "A non-document node and write options are required.");
        }
        SvgXmlWriterState state = new SvgXmlWriterState(options, result);
        return state.WriteFragment(node);
    }

    static SvgWriteResult Invalid(SvgWriteResult result, SvgWriteError error, string message)
    {
        result.Error = error;
        result.Message = message;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, message, null));
        result.Success = false;
        return result;
    }
}

internal class SvgXmlWriterState
{
    SvgWriteOptions options;
    SvgWriteResult result;
    System.Text.StringBuilder output;
    SvgWriteError error;
    string message;
    int nodeCount;
    int attributeCount;
    int generatedPrefix;
    SvgDocument? sourceDocument;
    SvgCanonicalizeOptions? valueOptions;
    int reusedRegions;
    int reserializedRegions;

    internal SvgXmlWriterState(SvgWriteOptions writeOptions, SvgWriteResult writeResult)
    {
        options = writeOptions;
        result = writeResult;
        output = new System.Text.StringBuilder();
        error = SvgWriteError.None;
        message = "";
        nodeCount = 0;
        attributeCount = 0;
        generatedPrefix = 1;
        sourceDocument = null;
        valueOptions = null;
        reusedRegions = 0;
        reserializedRegions = 0;
    }

    internal SvgWriteResult WriteDocument(SvgDocument document)
    {
        sourceDocument = document;
        if (!ValidOptions()) return CompleteFailure(SvgWriteError.InvalidArgument, "XML write options are invalid.");
        valueOptions = PrepareValueOptions();
        if (!ValidateDocument(document)) return Complete();
        if (options.DeclarationPolicy == SvgXmlDeclarationPolicy.Include)
        {
            char quote = DeclarationQuote();
            if (!Append("<?xml version=" + quote + "1.0" + quote)) return Complete();
            if (options.DeclarationEncoding.Length != 0 && !Append(" encoding=" + quote + EscapeAttribute(options.DeclarationEncoding, quote) + quote)) return Complete();
            if (options.DeclarationStandalone.Length != 0 && !Append(" standalone=" + quote + options.DeclarationStandalone + quote)) return Complete();
            if (!Append("?>")) return Complete();
            if (document.DocumentNode.Children.Count != 0 && !Append(options.NewLine)) return Complete();
        }
        else if (options.DeclarationPolicy == SvgXmlDeclarationPolicy.Preserve && !WritePreservedDeclaration(document)) return Complete();
        SvgXmlWriteScope scope = new SvgXmlWriteScope();
        int index = 0;
        bool wroteTopLevel = false;
        while (index < document.DocumentNode.Children.Count && error == SvgWriteError.None)
        {
            SvgNode node = document.DocumentNode.Children[index];
            bool omitted = node.Kind == SvgNodeKind.Doctype && options.DoctypePolicy == SvgWriteDoctypePolicy.Omit;
            if (!omitted)
            {
                if (IndentEnabled() && wroteTopLevel && Structural(node) && !EndsWithNewLine() && !Append(options.NewLine)) return Complete();
                WriteTree(node, scope, 0);
                wroteTopLevel = true;
            }
            index = index + 1;
        }
        return Complete();
    }

    internal SvgWriteResult WriteFragment(SvgNode node)
    {
        sourceDocument = node.OwnerDocument;
        if (!ValidOptions()) return CompleteFailure(SvgWriteError.InvalidArgument, "XML write options are invalid.");
        valueOptions = PrepareValueOptions();
        if (node.Kind == SvgNodeKind.Doctype && options.DoctypePolicy == SvgWriteDoctypePolicy.Reject) return CompleteFailure(SvgWriteError.DoctypeRejected, "The document type policy rejected the fragment.");
        if (node.Kind == SvgNodeKind.Doctype && options.DoctypePolicy == SvgWriteDoctypePolicy.Omit) return CompleteSuccess();
        WriteTree(node, new SvgXmlWriteScope(), 0);
        return Complete();
    }

    void WriteTree(SvgNode root, SvgXmlWriteScope rootScope, int rootDepth)
    {
        System.Collections.Generic.List<SvgXmlWriteFrame> stack = new System.Collections.Generic.List<SvgXmlWriteFrame>();
        stack.Add(new SvgXmlWriteFrame(root, rootScope, rootDepth));
        while (stack.Count != 0 && error == SvgWriteError.None)
        {
            if (options.CancellationToken.IsCancellationRequested)
            {
                SetError(SvgWriteError.Cancelled, "XML writing was cancelled.");
                break;
            }
            SvgXmlWriteFrame frame = stack[stack.Count - 1];
            if (!frame.Entered)
            {
                frame.Entered = true;
                if (!ReserveNode(frame.Depth)) break;
                if (frame.Node.Kind != SvgNodeKind.Element)
                {
                    WriteScalar(frame.Node);
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }
                frame.Plan = PlanElement(frame.Node, frame.ParentScope);
                if (frame.Plan == null) break;
                if (!WriteStartElement(frame.Node, frame.Plan)) break;
                if (frame.Node.Children.Count == 0)
                {
                    if (!frame.Plan.ReuseEmpty && (options.EmptyElementPolicy == SvgEmptyElementPolicy.ExplicitEndTag || frame.Plan.ReuseStart) && !WriteEndElement(frame.Node, frame.Plan)) break;
                    stack.RemoveAt(stack.Count - 1);
                }
                continue;
            }
            if (frame.ChildIndex < frame.Node.Children.Count)
            {
                SvgNode child = frame.Node.Children[frame.ChildIndex];
                frame.ChildIndex = frame.ChildIndex + 1;
                if ((frame.Plan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IndentChildren && !AppendIndent(frame.Depth + 1)) break;
                stack.Add(new SvgXmlWriteFrame(child, frame.Plan.Scope, frame.Depth + 1));
                continue;
            }
            if ((frame.Plan ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IndentChildren && frame.Node.Children.Count != 0 && !AppendIndent(frame.Depth)) break;
            if (!WriteEndElement(frame.Node, frame.Plan)) break;
            stack.RemoveAt(stack.Count - 1);
        }
    }

    SvgXmlElementWritePlan? PlanElement(SvgNode element, SvgXmlWriteScope? parentScope)
    {
        if (options.Mode == SvgWriteMode.SourcePreserving)
        {
            SvgXmlElementWritePlan? preserved = PlanPreservedElement(element, parentScope);
            if (preserved != null) return preserved;
        }
        if (!ValidLocalName(element.LocalName))
        {
            SetError(SvgWriteError.InvalidName, "An element local name is not a valid XML NCName.");
            return null;
        }
        SvgXmlElementWritePlan plan = new SvgXmlElementWritePlan();
        plan.Scope = (parentScope ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Copy();
        System.Collections.Generic.Dictionary<string, string> parentBindings = parentScope.Bindings;
        System.Collections.Generic.Dictionary<string, string> usedPrefixes = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal);
        System.Collections.Generic.List<SvgXmlWriteNamespaceHint> hints = CollectNamespaceHints(element);
        ApplyHints(plan.Scope, hints);
        string elementPrefix = ElementPrefix(element, plan.Scope, usedPrefixes, parentBindings, plan.Declarations);
        if (error != SvgWriteError.None) return null;
        plan.QualifiedName = elementPrefix.Length == 0 ? element.LocalName : elementPrefix + ":" + element.LocalName;
        plan.ReuseEnd = CanReuseEnd(element, plan.QualifiedName);
        System.Collections.Generic.HashSet<string> expandedNames = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        int index = 0;
        int sourceOrder = 0;
        while (index < element.Attributes.Count && error == SvgWriteError.None)
        {
            SvgAttribute attribute = element.Attributes[index];
            if (!IsNamespaceDeclaration(attribute))
            {
                if (!ValidLocalName(attribute.LocalName))
                {
                    SetError(SvgWriteError.InvalidName, "An attribute local name is not a valid XML NCName.");
                    return null;
                }
                string expanded = attribute.NamespaceUri + "\0" + attribute.LocalName;
                if (expandedNames.Contains(expanded))
                {
                    SetError(SvgWriteError.DuplicateAttribute, "An element contains duplicate expanded attribute names.");
                    return null;
                }
                expandedNames.Add(expanded);
                string prefix = AttributePrefix(attribute, plan.Scope, usedPrefixes, parentBindings, plan.Declarations);
                if (error != SvgWriteError.None) return null;
                string qualifiedName = prefix.Length == 0 ? attribute.LocalName : prefix + ":" + attribute.LocalName;
                plan.Attributes.Add(new SvgXmlWrittenAttribute(qualifiedName, attribute.LocalName, attribute.NamespaceUri, FormatAttributeValue(element, attribute), sourceOrder));
                sourceOrder = sourceOrder + 1;
            }
            index = index + 1;
        }
        PreserveHints(hints, plan.Scope, usedPrefixes, parentBindings, plan.Declarations);
        SortDeclarations(plan.Declarations);
        if (options.AttributeOrder == SvgWriteAttributeOrder.NamespaceThenExpandedName) SortAttributes(plan.Attributes);
        plan.IndentChildren = IndentEnabled() && ElementOnlyContent(element);
        return plan;
    }

    SvgXmlElementWritePlan? PlanPreservedElement(SvgNode element, SvgXmlWriteScope? parentScope)
    {
        SvgXmlToken? token = element.RawToken;
        if (token == null || token.RawText == null || token.Kind != SvgXmlTokenKind.StartElement && token.Kind != SvgXmlTokenKind.EmptyElement || token.QualifiedName != element.QualifiedName || token.Prefix != element.Prefix || token.LocalName != element.LocalName || token.NamespaceUri != element.NamespaceUri || !ValidLocalName(element.LocalName) || token.Attributes.Count != element.Attributes.Count) return null;
        if (token.Kind == SvgXmlTokenKind.EmptyElement && element.Children.Count != 0) return null;
        if (token.Kind == SvgXmlTokenKind.StartElement && (element.EndToken == null || element.EndToken.Kind != SvgXmlTokenKind.EndElement || element.EndToken.QualifiedName != element.QualifiedName)) return null;
        int index = 0;
        while (index < element.Attributes.Count)
        {
            SvgAttribute attribute = element.Attributes[index];
            SvgXmlAttributeToken? raw = attribute.RawToken;
            SvgXmlAttributeToken tokenAttribute = token.Attributes[index];
            if (raw == null || attribute.SourceMapKind != SvgSourceMapKind.Original || attribute.IsDirty || !object.ReferenceEquals(raw, tokenAttribute) || raw.QualifiedName != attribute.QualifiedName || raw.Prefix != attribute.Prefix || raw.LocalName != attribute.LocalName || raw.NamespaceUri != attribute.NamespaceUri || raw.NormalizedValue != attribute.Value) return null;
            index = index + 1;
        }
        SvgXmlElementWritePlan plan = new SvgXmlElementWritePlan();
        plan.QualifiedName = token.QualifiedName;
        plan.Scope = (parentScope ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Copy();
        System.Collections.Generic.List<SvgXmlWriteNamespaceHint> hints = CollectNamespaceHints(element);
        index = 0;
        while (index < hints.Count)
        {
            SvgXmlWriteNamespaceHint hint = hints[index];
            if (!ValidNamespaceHint(hint.Prefix, hint.NamespaceUri)) return null;
            plan.Scope.Bindings[hint.Prefix] = hint.NamespaceUri;
            index = index + 1;
        }
        if (!RawElementNamespaceMatches(element, plan.Scope)) return null;
        index = 0;
        while (index < element.Attributes.Count)
        {
            SvgAttribute attribute = element.Attributes[index];
            if (!IsNamespaceDeclaration(attribute) && !RawAttributeNamespaceMatches(attribute, plan.Scope)) return null;
            index = index + 1;
        }
        plan.ReuseStart = true;
        plan.ReuseEmpty = token.Kind == SvgXmlTokenKind.EmptyElement;
        plan.ReuseEnd = token.Kind == SvgXmlTokenKind.StartElement;
        plan.IndentChildren = false;
        return plan;
    }

    bool RawElementNamespaceMatches(SvgNode element, SvgXmlWriteScope scope)
    {
        if (element.Prefix.Length == 0)
        {
            string defaultUri = scope.Bindings.ContainsKey("") ? scope.Bindings[""] : "";
            return defaultUri == element.NamespaceUri;
        }
        return scope.Bindings.ContainsKey(element.Prefix) && scope.Bindings[element.Prefix] == element.NamespaceUri;
    }

    bool RawAttributeNamespaceMatches(SvgAttribute attribute, SvgXmlWriteScope scope)
    {
        if (attribute.Prefix.Length == 0) return attribute.NamespaceUri.Length == 0;
        return scope.Bindings.ContainsKey(attribute.Prefix) && scope.Bindings[attribute.Prefix] == attribute.NamespaceUri;
    }

    bool CanReuseEnd(SvgNode element, string qualifiedName)
    {
        if (options.Mode != SvgWriteMode.SourcePreserving || element.EndToken == null || element.EndToken.RawText == null) return false;
        return element.EndToken.Kind == SvgXmlTokenKind.EndElement && element.EndToken.QualifiedName == qualifiedName;
    }

    System.Collections.Generic.List<SvgXmlWriteNamespaceHint> CollectNamespaceHints(SvgNode element)
    {
        System.Collections.Generic.List<SvgXmlWriteNamespaceHint> resultHints = new System.Collections.Generic.List<SvgXmlWriteNamespaceHint>();
        int index = 0;
        while (index < element.Attributes.Count)
        {
            SvgAttribute attribute = element.Attributes[index];
            if (IsNamespaceDeclaration(attribute))
            {
                string prefix = attribute.Prefix == "xmlns" ? attribute.LocalName : "";
                if (ValidNamespaceHint(prefix, attribute.Value)) resultHints.Add(new SvgXmlWriteNamespaceHint(prefix, attribute.Value));
            }
            index = index + 1;
        }
        return resultHints;
    }

    void ApplyHints(SvgXmlWriteScope scope, System.Collections.Generic.List<SvgXmlWriteNamespaceHint> hints)
    {
        int index = 0;
        while (index < hints.Count)
        {
            SvgXmlWriteNamespaceHint hint = hints[index];
            scope.Bindings[hint.Prefix] = hint.NamespaceUri;
            index = index + 1;
        }
    }

    string ElementPrefix(SvgNode element, SvgXmlWriteScope scope, System.Collections.Generic.Dictionary<string, string> used, System.Collections.Generic.Dictionary<string, string> parent, System.Collections.Generic.List<SvgXmlWriteNamespaceDeclaration> declarations)
    {
        string uri = element.NamespaceUri;
        if (uri == SvgNamespaces.Xml)
        {
            used["xml"] = uri;
            return "xml";
        }
        if (uri == SvgNamespaces.Xmlns)
        {
            SetError(SvgWriteError.InvalidName, "Elements cannot use the namespace-declaration namespace.");
            return "";
        }
        string prefix = ValidPrefix(element.Prefix) && element.Prefix != "xmlns" && element.Prefix != "xml" ? element.Prefix : "";
        if (uri.Length == 0) prefix = "";
        return BindPrefix(prefix, uri, false, scope, used, parent, declarations);
    }

    string AttributePrefix(SvgAttribute attribute, SvgXmlWriteScope scope, System.Collections.Generic.Dictionary<string, string> used, System.Collections.Generic.Dictionary<string, string> parent, System.Collections.Generic.List<SvgXmlWriteNamespaceDeclaration> declarations)
    {
        string uri = attribute.NamespaceUri;
        if (uri.Length == 0) return "";
        if (uri == SvgNamespaces.Xml)
        {
            used["xml"] = uri;
            return "xml";
        }
        if (uri == SvgNamespaces.Xmlns)
        {
            SetError(SvgWriteError.InvalidName, "Namespace declaration attributes must use namespace declaration names.");
            return "";
        }
        string preferred = ValidPrefix(attribute.Prefix) && attribute.Prefix != "xml" && attribute.Prefix != "xmlns" ? attribute.Prefix : "";
        if (preferred.Length == 0 && uri == SvgNamespaces.XLink) preferred = "xlink";
        if (preferred.Length == 0) preferred = FindPrefix(scope, used, uri);
        if (preferred.Length == 0) preferred = GeneratePrefix(scope, used);
        return BindPrefix(preferred, uri, true, scope, used, parent, declarations);
    }

    string BindPrefix(string requested, string uri, bool attribute, SvgXmlWriteScope scope, System.Collections.Generic.Dictionary<string, string> used, System.Collections.Generic.Dictionary<string, string> parent, System.Collections.Generic.List<SvgXmlWriteNamespaceDeclaration> declarations)
    {
        string prefix = requested;
        if (attribute && prefix.Length == 0) prefix = GeneratePrefix(scope, used);
        if (used.ContainsKey(prefix) && used[prefix] != uri) prefix = GeneratePrefix(scope, used);
        used[prefix] = uri;
        string? current = scope.Bindings.ContainsKey(prefix) ? scope.Bindings[prefix] : null;
        if (current != uri)
        {
            scope.Bindings[prefix] = uri;
            SetDeclaration(declarations, prefix, uri);
        }
        else
        {
            string? inherited = parent.ContainsKey(prefix) ? parent[prefix] : null;
            if (inherited != uri) SetDeclaration(declarations, prefix, uri);
        }
        return prefix;
    }

    void PreserveHints(System.Collections.Generic.List<SvgXmlWriteNamespaceHint> hints, SvgXmlWriteScope scope, System.Collections.Generic.Dictionary<string, string> used, System.Collections.Generic.Dictionary<string, string> parent, System.Collections.Generic.List<SvgXmlWriteNamespaceDeclaration> declarations)
    {
        if (!options.PreserveNamespaceDeclarations)
        {
            int restoreIndex = 0;
            while (restoreIndex < hints.Count)
            {
                SvgXmlWriteNamespaceHint hint = hints[restoreIndex];
                if (!used.ContainsKey(hint.Prefix))
                {
                    if (parent.ContainsKey(hint.Prefix)) scope.Bindings[hint.Prefix] = parent[hint.Prefix];
                    else scope.Bindings.Remove(hint.Prefix);
                }
                restoreIndex = restoreIndex + 1;
            }
            return;
        }
        int index = 0;
        while (index < hints.Count)
        {
            SvgXmlWriteNamespaceHint hint = hints[index];
            bool conflict = used.ContainsKey(hint.Prefix) && used[hint.Prefix] != hint.NamespaceUri;
            if (!conflict)
            {
                scope.Bindings[hint.Prefix] = hint.NamespaceUri;
                SetDeclaration(declarations, hint.Prefix, hint.NamespaceUri);
            }
            index = index + 1;
        }
    }

    string FindPrefix(SvgXmlWriteScope scope, System.Collections.Generic.Dictionary<string, string> used, string uri)
    {
        string resultPrefix = "";
        foreach (System.Collections.Generic.KeyValuePair<string, string> pair in scope.Bindings)
        {
            if (pair.Key.Length != 0 && pair.Key != "xml" && pair.Key != "xmlns" && pair.Value == uri && (!used.ContainsKey(pair.Key) || used[pair.Key] == uri))
            {
                if (resultPrefix.Length == 0 || string.CompareOrdinal(pair.Key, resultPrefix) < 0) resultPrefix = pair.Key;
            }
        }
        return resultPrefix;
    }

    string GeneratePrefix(SvgXmlWriteScope scope, System.Collections.Generic.Dictionary<string, string> used)
    {
        string prefix = "ns" + generatedPrefix.ToString(System.Globalization.CultureInfo.InvariantCulture);
        generatedPrefix = generatedPrefix + 1;
        while (scope.Bindings.ContainsKey(prefix) || used.ContainsKey(prefix))
        {
            prefix = "ns" + generatedPrefix.ToString(System.Globalization.CultureInfo.InvariantCulture);
            generatedPrefix = generatedPrefix + 1;
        }
        return prefix;
    }

    void SetDeclaration(System.Collections.Generic.List<SvgXmlWriteNamespaceDeclaration> declarations, string prefix, string uri)
    {
        int index = 0;
        while (index < declarations.Count)
        {
            if (declarations[index].Prefix == prefix)
            {
                declarations[index].NamespaceUri = uri;
                return;
            }
            index = index + 1;
        }
        declarations.Add(new SvgXmlWriteNamespaceDeclaration(prefix, uri));
    }

    bool WriteStartElement(SvgNode element, SvgXmlElementWritePlan plan)
    {
        if (plan.ReuseStart)
        {
            if (!ReserveAttributes(element.Attributes.Count)) return false;
            return AppendReused(element, null, (element.RawToken ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).RawText, TokenSpan(element, element.RawToken));
        }
        int outputStart = output.Length;
        if (!Append("<" + plan.QualifiedName)) return false;
        int declarationIndex = 0;
        while (declarationIndex < plan.Declarations.Count)
        {
            SvgXmlWriteNamespaceDeclaration declaration = plan.Declarations[declarationIndex];
            string name = declaration.Prefix.Length == 0 ? "xmlns" : "xmlns:" + declaration.Prefix;
            if (!WriteAttribute(name, declaration.NamespaceUri)) return false;
            declarationIndex = declarationIndex + 1;
        }
        int attributeIndex = 0;
        while (attributeIndex < plan.Attributes.Count)
        {
            SvgXmlWrittenAttribute attribute = plan.Attributes[attributeIndex];
            if (!WriteAttribute(attribute.QualifiedName, attribute.Value)) return false;
            attributeIndex = attributeIndex + 1;
        }
        if (element.Children.Count == 0 && options.EmptyElementPolicy == SvgEmptyElementPolicy.SelfClosing)
        {
            if (!Append(options.SpaceBeforeEmptyElementSlash ? " />" : "/>")) return false;
            AddReserialized(element, null, outputStart, TokenSpan(element, element.RawToken));
            return true;
        }
        if (!Append(">")) return false;
        AddReserialized(element, null, outputStart, TokenSpan(element, element.RawToken));
        return true;
    }

    bool WriteAttribute(string qualifiedName, string value)
    {
        if (!ValidXmlCharacters(value))
        {
            SetError(SvgWriteError.InvalidXml, "An attribute contains a character not permitted by XML 1.0.");
            return false;
        }
        if (!ReserveAttributes(1)) return false;
        char quote = SelectQuote(value);
        string escaped = EscapeAttribute(value, quote);
        return Append(" " + qualifiedName + "=" + quote + escaped + quote);
    }

    bool ReserveAttributes(int count)
    {
        if (count < 0 || attributeCount > options.MaximumAttributes - count)
        {
            SetError(SvgWriteError.LimitExceeded, "XML output exceeded MaximumAttributes.");
            return false;
        }
        attributeCount = attributeCount + count;
        return true;
    }

    bool WriteEndElement(SvgNode element, SvgXmlElementWritePlan plan)
    {
        if (plan.ReuseEnd) return AppendReused(element, null, (element.EndToken ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).RawText, TokenSpan(element, element.EndToken));
        int outputStart = output.Length;
        if (!Append("</" + plan.QualifiedName + ">")) return false;
        AddReserialized(element, null, outputStart, TokenSpan(element, element.EndToken));
        return true;
    }

    void WriteScalar(SvgNode node)
    {
        if (CanReuseScalar(node))
        {
            AppendReused(node, null, (node.RawToken ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).RawText, node.SourceSpan);
            return;
        }
        int outputStart = output.Length;
        if (node.Kind == SvgNodeKind.Text)
        {
            if (!ValidXmlCharacters(node.Value)) SetError(SvgWriteError.InvalidXml, "Text contains a character not permitted by XML 1.0.");
            else
            {
                string text = FormatTextValue(node);
                Append(IsStyleText(node) ? EscapeCssText(text) : EscapeText(text));
            }
        }
        else if (node.Kind == SvgNodeKind.CData)
        {
            if (!ValidXmlCharacters(node.Value)) SetError(SvgWriteError.InvalidXml, "CDATA contains a character not permitted by XML 1.0.");
            else Append("<![CDATA[" + node.Value.Replace("]]>", "]]]]><![CDATA[>") + "]]>");
        }
        else if (node.Kind == SvgNodeKind.Comment)
        {
            if (!ValidXmlCharacters(node.Value) || node.Value.IndexOf("--", System.StringComparison.Ordinal) >= 0 || node.Value.EndsWith("-", System.StringComparison.Ordinal)) SetError(SvgWriteError.InvalidXml, "A comment is not valid XML 1.0 content.");
            else Append("<!--" + node.Value + "-->");
        }
        else if (node.Kind == SvgNodeKind.ProcessingInstruction)
        {
            string target = node.QualifiedName.Length == 0 ? node.LocalName : node.QualifiedName;
            if (!ValidQualifiedName(target) || string.Equals(target, "xml", System.StringComparison.OrdinalIgnoreCase) || !ValidXmlCharacters(node.Value) || node.Value.IndexOf("?>", System.StringComparison.Ordinal) >= 0) SetError(SvgWriteError.InvalidXml, "A processing instruction is not valid XML 1.0 content.");
            else Append("<?" + target + (node.Value.Length == 0 ? "" : " " + node.Value) + "?>");
        }
        else if (node.Kind == SvgNodeKind.Doctype)
        {
            if (options.DoctypePolicy == SvgWriteDoctypePolicy.Reject) SetError(SvgWriteError.DoctypeRejected, "The configured policy rejects document type output.");
            else if (options.DoctypePolicy == SvgWriteDoctypePolicy.Preserve) WriteDoctype(node);
        }
        else SetError(SvgWriteError.InvalidArgument, "The XML tree contains an unsupported node kind.");
        if (error == SvgWriteError.None && output.Length != outputStart) AddReserialized(node, null, outputStart, node.SourceSpan);
    }

    string FormatAttributeValue(SvgNode element, SvgAttribute attribute)
    {
        if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == "style") return SvgCssWriter.FormatDeclarationList(attribute.Value, options.Mode, (valueOptions ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Numbers, valueOptions.Colors, options.MaximumNodes, options.MaximumCharacters);
        string? formatted = SvgValueCanonicalizer.FormatAttribute(element, attribute, valueOptions);
        return formatted == null ? attribute.Value : formatted;
    }

    string FormatTextValue(SvgNode node)
    {
        if (IsStyleText(node)) return SvgCssWriter.FormatStyleSheet(node.Value, options.Mode, (valueOptions ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Numbers, valueOptions.Colors, options.IndentText, options.NewLine, options.MaximumNodes, options.MaximumCharacters);
        return node.Value;
    }

    bool IsStyleText(SvgNode node)
    {
        return node.Parent != null && node.Parent.Kind == SvgNodeKind.Element && node.Parent.NamespaceUri == SvgNamespaces.Svg && node.Parent.LocalName == "style";
    }

    SvgCanonicalizeOptions PrepareValueOptions()
    {
        SvgCanonicalizeOptions prepared = options.Values.Copy();
        SvgValueFormatMode mode = options.Mode == SvgWriteMode.Compact ? SvgValueFormatMode.Compact : SvgValueFormatMode.Canonical;
        prepared.Numbers.Mode = mode;
        prepared.Colors.Mode = mode;
        prepared.Lists.Mode = mode;
        prepared.Paths.Mode = mode;
        prepared.Transforms.Mode = mode;
        return prepared;
    }

    bool CanReuseScalar(SvgNode node)
    {
        if (options.Mode != SvgWriteMode.SourcePreserving || node.SourceMapKind != SvgSourceMapKind.Original || node.IsDirty || node.RawToken == null || node.RawToken.RawText == null || node.RawToken.NormalizedValue != node.Value) return false;
        SvgXmlToken token = node.RawToken;
        if (node.Kind == SvgNodeKind.Text) return token.Kind == SvgXmlTokenKind.Text || token.Kind == SvgXmlTokenKind.Reference;
        if (node.Kind == SvgNodeKind.CData) return token.Kind == SvgXmlTokenKind.CData;
        if (node.Kind == SvgNodeKind.Comment) return token.Kind == SvgXmlTokenKind.Comment;
        if (node.Kind == SvgNodeKind.ProcessingInstruction) return token.Kind == SvgXmlTokenKind.ProcessingInstruction && token.QualifiedName == node.QualifiedName && token.Prefix == node.Prefix && token.LocalName == node.LocalName;
        if (node.Kind == SvgNodeKind.Doctype) return options.DoctypePolicy == SvgWriteDoctypePolicy.Preserve && token.Kind == SvgXmlTokenKind.Doctype && DoctypeName(token.Value) == node.LocalName;
        return false;
    }

    string DoctypeName(string value)
    {
        int index = 0;
        while (index < value.Length && (value[index] == ' ' || value[index] == '\t' || value[index] == '\r' || value[index] == '\n')) index = index + 1;
        int start = index;
        while (index < value.Length && value[index] != ' ' && value[index] != '\t' && value[index] != '\r' && value[index] != '\n' && value[index] != '[') index = index + 1;
        return value.Substring(start, index - start);
    }

    void WriteDoctype(SvgNode node)
    {
        if (!ValidLocalName(node.LocalName) || !ValidXmlCharacters(node.Value) || !ValidDoctypeValue(node.Value))
        {
            SetError(SvgWriteError.InvalidXml, "A document type node is not valid bounded XML content.");
            return;
        }
        string trimmed = node.Value.Trim();
        string content;
        if (trimmed.Length == 0) content = node.LocalName;
        else if (trimmed == node.LocalName || trimmed.StartsWith(node.LocalName + " ", System.StringComparison.Ordinal) || trimmed.StartsWith(node.LocalName + "\t", System.StringComparison.Ordinal) || trimmed.StartsWith(node.LocalName + "\r", System.StringComparison.Ordinal) || trimmed.StartsWith(node.LocalName + "\n", System.StringComparison.Ordinal)) content = trimmed;
        else content = node.LocalName + " " + trimmed;
        Append("<!DOCTYPE " + content + ">");
    }

    bool ValidDoctypeValue(string value)
    {
        int depth = 0;
        char quote = '\0';
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (quote != '\0')
            {
                if (character == quote) quote = '\0';
            }
            else if (character == '\'' || character == '"') quote = character;
            else if (character == '[') depth = depth + 1;
            else if (character == ']')
            {
                if (depth == 0) return false;
                depth = depth - 1;
            }
            else if (character == '>' && depth == 0) return false;
            index = index + 1;
        }
        return depth == 0 && quote == '\0';
    }

    bool ValidateDocument(SvgDocument document)
    {
        int elements = 0;
        int doctypes = 0;
        bool sawElement = false;
        int index = 0;
        while (index < document.DocumentNode.Children.Count)
        {
            SvgNode node = document.DocumentNode.Children[index];
            if (node.Kind == SvgNodeKind.Element)
            {
                elements = elements + 1;
                sawElement = true;
            }
            else if (node.Kind == SvgNodeKind.Doctype)
            {
                doctypes = doctypes + 1;
                if (sawElement) return FailValidation(SvgWriteError.InvalidXml, "A document type must precede the document element.");
                if (options.DoctypePolicy == SvgWriteDoctypePolicy.Reject) return FailValidation(SvgWriteError.DoctypeRejected, "The configured policy rejects document type output.");
            }
            else if (node.Kind == SvgNodeKind.Text && !XmlWhitespace(node.Value) || node.Kind == SvgNodeKind.CData) return FailValidation(SvgWriteError.InvalidXml, "Character content is not permitted outside the document element.");
            index = index + 1;
        }
        if (elements != 1 || document.DocumentElement == null || doctypes > 1) return FailValidation(SvgWriteError.InvalidXml, "A writable XML document requires one document element and at most one document type.");
        return true;
    }

    bool FailValidation(SvgWriteError value, string failureMessage)
    {
        SetError(value, failureMessage);
        return false;
    }

    bool ReserveNode(int depth)
    {
        if (depth >= options.MaximumDepth || nodeCount >= options.MaximumNodes)
        {
            SetError(SvgWriteError.LimitExceeded, "XML output exceeded a node or depth bound.");
            return false;
        }
        nodeCount = nodeCount + 1;
        return true;
    }

    bool AppendIndent(int depth)
    {
        if (!Append(options.NewLine)) return false;
        int index = 0;
        while (index < depth)
        {
            if (!Append(options.IndentText)) return false;
            index = index + 1;
        }
        return true;
    }

    bool Append(string value)
    {
        if (error != SvgWriteError.None) return false;
        if (value == null || output.Length > options.MaximumCharacters - value.Length)
        {
            SetError(SvgWriteError.LimitExceeded, "XML output exceeded MaximumCharacters.");
            return false;
        }
        output.Append(value);
        return true;
    }

    bool AppendReused(SvgNode? node, SvgAttribute? attribute, string value, SvgSourceSpan? sourceSpan)
    {
        int outputStart = output.Length;
        if (!Append(value)) return false;
        result.SourceMap.Add(new SvgWriteSourceMapEntry(SvgSourceMapKind.Original, node, attribute, outputStart, output.Length, sourceSpan));
        reusedRegions = reusedRegions + 1;
        return true;
    }

    void AddReserialized(SvgNode node, SvgAttribute? attribute, int outputStart, SvgSourceSpan? sourceSpan)
    {
        SvgSourceMapKind kind = SvgSourceMapKind.Rewritten;
        if (node != null && node.SourceMapKind == SvgSourceMapKind.Generated || attribute != null && attribute.SourceMapKind == SvgSourceMapKind.Generated) kind = SvgSourceMapKind.Generated;
        result.SourceMap.Add(new SvgWriteSourceMapEntry(kind, node, attribute, outputStart, output.Length, sourceSpan));
        reserializedRegions = reserializedRegions + 1;
    }

    bool WritePreservedDeclaration(SvgDocument document)
    {
        if (options.Mode != SvgWriteMode.SourcePreserving || document.SourceText == null || !document.SourceText.StartsWith("<?xml", System.StringComparison.Ordinal)) return true;
        int end = document.SourceText.IndexOf("?>", System.StringComparison.Ordinal);
        if (end < 0) return true;
        return AppendReused(null, null, document.SourceText.Substring(0, end + 2), SourceTextSpan(document, end + 2));
    }

    SvgSourceSpan? TokenSpan(SvgNode node, SvgXmlToken? token)
    {
        if (token == null) return node == null ? null : node.SourceSpan;
        SvgSourceUri? source = null;
        if (node != null && node.SourceSpan != null) source = node.SourceSpan.Source;
        else if (sourceDocument != null) source = sourceDocument.Source;
        if (source == null) return node == null ? null : node.SourceSpan;
        return new SvgSourceSpan(source, token.StartOffset, token.StartLine, token.StartColumn, token.EndOffset, token.EndLine, token.EndColumn);
    }

    SvgSourceSpan? SourceTextSpan(SvgDocument document, int endOffset)
    {
        if (document.Source == null) return null;
        int line = 1;
        int column = 1;
        int index = 0;
        while (index < endOffset)
        {
            char current = (document.SourceText ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[index];
            index = index + 1;
            if (current == '\r')
            {
                line = line + 1;
                column = 1;
            }
            else if (current == '\n')
            {
                if (index < 2 || document.SourceText[index - 2] != '\r')
                {
                    line = line + 1;
                    column = 1;
                }
            }
            else column = column + 1;
        }
        return new SvgSourceSpan(document.Source, 0, 1, 1, endOffset, line, column);
    }

    string EscapeText(string value)
    {
        System.Text.StringBuilder escaped = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '&') escaped.Append("&amp;");
            else if (character == '<') escaped.Append("&lt;");
            else if (character == '>') escaped.Append("&gt;");
            else if (character == '\r') escaped.Append("&#xD;");
            else escaped.Append(character);
            index = index + 1;
        }
        return escaped.ToString();
    }

    string EscapeCssText(string value)
    {
        System.Text.StringBuilder escaped = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '&') escaped.Append("&amp;");
            else if (character == '<') escaped.Append("&lt;");
            else if (character == '>') escaped.Append("&gt;");
            else escaped.Append(character);
            index = index + 1;
        }
        return escaped.ToString();
    }

    string EscapeAttribute(string value, char quote)
    {
        System.Text.StringBuilder escaped = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '&') escaped.Append("&amp;");
            else if (character == '<') escaped.Append("&lt;");
            else if (character == quote && quote == '"') escaped.Append("&quot;");
            else if (character == quote) escaped.Append("&apos;");
            else if (character == '\t') escaped.Append("&#x9;");
            else if (character == '\n') escaped.Append("&#xA;");
            else if (character == '\r') escaped.Append("&#xD;");
            else escaped.Append(character);
            index = index + 1;
        }
        return escaped.ToString();
    }

    char SelectQuote(string value)
    {
        if (options.QuoteStyle == SvgAttributeQuoteStyle.Single) return '\'';
        if (options.QuoteStyle == SvgAttributeQuoteStyle.Double) return '"';
        int singles = 0;
        int doubles = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\'') singles = singles + 1;
            else if (value[index] == '"') doubles = doubles + 1;
            index = index + 1;
        }
        return singles < doubles ? '\'' : '"';
    }

    char DeclarationQuote()
    {
        return options.QuoteStyle == SvgAttributeQuoteStyle.Single ? '\'' : '"';
    }

    bool ValidXmlCharacters(string value)
    {
        if (value == null) return false;
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character >= 0xD800 && character <= 0xDBFF)
            {
                if (index + 1 >= value.Length || value[index + 1] < 0xDC00 || value[index + 1] > 0xDFFF) return false;
                index = index + 2;
                continue;
            }
            if (character >= 0xDC00 && character <= 0xDFFF) return false;
            if (character != 0x9 && character != 0xA && character != 0xD && character < 0x20 || character == 0xFFFE || character == 0xFFFF) return false;
            index = index + 1;
        }
        return true;
    }

    bool ValidLocalName(string value)
    {
        return value != null && value.Length != 0 && System.Xml.XmlConvert.EncodeLocalName(value) == value;
    }

    bool ValidPrefix(string value)
    {
        return value != null && (value.Length == 0 || ValidLocalName(value));
    }

    bool ValidQualifiedName(string value)
    {
        if (value == null || value.Length == 0) return false;
        int colon = value.IndexOf(':');
        if (colon < 0) return ValidLocalName(value);
        if (colon == 0 || colon == value.Length - 1 || value.IndexOf(':', colon + 1) >= 0) return false;
        return ValidLocalName(value.Substring(0, colon)) && ValidLocalName(value.Substring(colon + 1));
    }

    bool ValidNamespaceHint(string prefix, string uri)
    {
        if (prefix == null || uri == null || !ValidPrefix(prefix)) return false;
        if (prefix == "xmlns" || uri == SvgNamespaces.Xmlns) return false;
        if (prefix == "xml") return uri == SvgNamespaces.Xml;
        if (uri == SvgNamespaces.Xml || prefix.Length != 0 && uri.Length == 0) return false;
        return true;
    }

    bool ValidEncoding(string value)
    {
        if (value.Length == 0) return true;
        char first = value[0];
        if (!(first >= 'a' && first <= 'z' || first >= 'A' && first <= 'Z')) return false;
        int index = 1;
        while (index < value.Length)
        {
            char character = value[index];
            if (!(character >= 'a' && character <= 'z' || character >= 'A' && character <= 'Z' || character >= '0' && character <= '9' || character == '.' || character == '_' || character == '-')) return false;
            index = index + 1;
        }
        return true;
    }

    bool IsNamespaceDeclaration(SvgAttribute attribute)
    {
        return attribute.NamespaceUri == SvgNamespaces.Xmlns || attribute.QualifiedName == "xmlns" || attribute.Prefix == "xmlns";
    }

    bool ElementOnlyContent(SvgNode element)
    {
        int index = 0;
        while (index < element.Children.Count)
        {
            SvgNode child = element.Children[index];
            if (child.Kind == SvgNodeKind.Text || child.Kind == SvgNodeKind.CData) return false;
            index = index + 1;
        }
        return true;
    }

    bool XmlWhitespace(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != ' ' && character != '\t' && character != '\r' && character != '\n') return false;
            index = index + 1;
        }
        return true;
    }

    bool Structural(SvgNode node)
    {
        return node.Kind == SvgNodeKind.Element || node.Kind == SvgNodeKind.Comment || node.Kind == SvgNodeKind.ProcessingInstruction || node.Kind == SvgNodeKind.Doctype;
    }

    bool IndentEnabled()
    {
        if (options.Mode == SvgWriteMode.SourcePreserving) return false;
        return options.Mode == SvgWriteMode.Pretty || options.Mode != SvgWriteMode.Compact && options.Indent;
    }

    bool EndsWithNewLine()
    {
        if (output.Length == 0 || options.NewLine.Length == 0 || output.Length < options.NewLine.Length) return false;
        int index = 0;
        while (index < options.NewLine.Length)
        {
            if (output[output.Length - options.NewLine.Length + index] != options.NewLine[index]) return false;
            index = index + 1;
        }
        return true;
    }

    void SortDeclarations(System.Collections.Generic.List<SvgXmlWriteNamespaceDeclaration> declarations)
    {
        int index = 1;
        while (index < declarations.Count)
        {
            SvgXmlWriteNamespaceDeclaration value = declarations[index];
            int position = index;
            while (position > 0 && string.CompareOrdinal(declarations[position - 1].Prefix, value.Prefix) > 0)
            {
                declarations[position] = declarations[position - 1];
                position = position - 1;
            }
            declarations[position] = value;
            index = index + 1;
        }
    }

    void SortAttributes(System.Collections.Generic.List<SvgXmlWrittenAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            SvgXmlWrittenAttribute value = attributes[index];
            int position = index;
            while (position > 0 && CompareAttribute(attributes[position - 1], value) > 0)
            {
                attributes[position] = attributes[position - 1];
                position = position - 1;
            }
            attributes[position] = value;
            index = index + 1;
        }
    }

    int CompareAttribute(SvgXmlWrittenAttribute left, SvgXmlWrittenAttribute right)
    {
        int compared = string.CompareOrdinal(left.NamespaceUri, right.NamespaceUri);
        if (compared != 0) return compared;
        compared = string.CompareOrdinal(left.LocalName, right.LocalName);
        if (compared != 0) return compared;
        compared = string.CompareOrdinal(left.QualifiedName, right.QualifiedName);
        if (compared != 0) return compared;
        return left.SourceOrder - right.SourceOrder;
    }

    bool ValidOptions()
    {
        bool enums = options.Mode >= SvgWriteMode.SourcePreserving && options.Mode <= SvgWriteMode.Compact && options.DeclarationPolicy >= SvgXmlDeclarationPolicy.Omit && options.DeclarationPolicy <= SvgXmlDeclarationPolicy.Preserve && options.DoctypePolicy >= SvgWriteDoctypePolicy.Omit && options.DoctypePolicy <= SvgWriteDoctypePolicy.Reject && options.EmptyElementPolicy >= SvgEmptyElementPolicy.SelfClosing && options.EmptyElementPolicy <= SvgEmptyElementPolicy.ExplicitEndTag && options.QuoteStyle >= SvgAttributeQuoteStyle.Double && options.QuoteStyle <= SvgAttributeQuoteStyle.MinimizeEscapes && options.AttributeOrder >= SvgWriteAttributeOrder.Source && options.AttributeOrder <= SvgWriteAttributeOrder.NamespaceThenExpandedName;
        bool strings = options.IndentText != null && options.NewLine != null && options.DeclarationEncoding != null && options.DeclarationStandalone != null && ValidXmlCharacters(options.IndentText) && ValidXmlCharacters(options.NewLine) && ValidXmlCharacters(options.DeclarationEncoding) && XmlWhitespace(options.IndentText) && XmlWhitespace(options.NewLine) && ValidEncoding(options.DeclarationEncoding);
        bool declaration = (options.DeclarationStandalone ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length == 0 || options.DeclarationStandalone == "yes" || options.DeclarationStandalone == "no";
        bool limits = options.MaximumNodes > 0 && options.MaximumAttributes > 0 && options.MaximumDepth > 0 && options.MaximumCharacters > 0;
        return enums && strings && declaration && limits && SvgValueCanonicalizer.Valid(options.Values);
    }

    SvgWriteResult Complete()
    {
        if (error != SvgWriteError.None)
        {
            result.Text = "";
            result.SourceMap.Clear();
            result.Error = error;
            result.Message = message;
            result.WrittenNodeCount = nodeCount;
            result.WrittenAttributeCount = attributeCount;
            result.CharacterCount = 0;
            result.ReusedSourceRegionCount = 0;
            result.ReserializedRegionCount = 0;
            result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, error == SvgWriteError.InvalidArgument ? SvgDiagnosticCode.InvalidArgument : error == SvgWriteError.LimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : error == SvgWriteError.Cancelled ? SvgDiagnosticCode.Cancelled : error == SvgWriteError.InvalidName || error == SvgWriteError.DuplicateAttribute ? SvgDiagnosticCode.XmlNamespace : SvgDiagnosticCode.MalformedXml, message, null));
            result.Success = false;
            return result;
        }
        return CompleteSuccess();
    }

    SvgWriteResult CompleteSuccess()
    {
        result.Text = output.ToString();
        result.Error = SvgWriteError.None;
        result.Message = "";
        result.WrittenNodeCount = nodeCount;
        result.WrittenAttributeCount = attributeCount;
        result.CharacterCount = output.Length;
        result.ReusedSourceRegionCount = reusedRegions;
        result.ReserializedRegionCount = reserializedRegions;
        result.Success = true;
        return result;
    }

    SvgWriteResult CompleteFailure(SvgWriteError value, string failureMessage)
    {
        SetError(value, failureMessage);
        return Complete();
    }

    void SetError(SvgWriteError value, string failureMessage)
    {
        if (error != SvgWriteError.None) return;
        error = value;
        message = failureMessage;
    }
}
