namespace xml;

internal class XsltStaticFunction : IXPathFunctionResolver, IXPathFunction
{
    internal XsltStylesheetLoader Loader;
    internal XmlNamespaceBinding[] Namespaces;
    internal string? StaticBaseUri;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmFactory Factory;

    internal XsltStaticFunction(XsltStylesheetLoader loader, XmlNamespaceBinding[] namespaces, string? staticBaseUri)
    {
        Loader = loader;
        Namespaces = namespaces;
        StaticBaseUri = staticBaseUri;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Factory = new XdmFactory();
    }

    public IXPathFunction? ResolveXPathFunction(XmlName name, int arity, XPathStaticContext context)
    {
        string localName = name.LocalName == null ? "" : name.LocalName;
        if (IsXsltStylesheetFunctionName(name, arity) || name.NamespaceUri == XsltNamespaces.Xslt && localName == "original")
        {
            return this;
        }

        if (name.NamespaceUri == XPathNamespaces.Functions && IsXsltLibraryFunctionName(localName, arity))
        {
            return this;
        }

        return null;
    }

    internal bool IsXsltLibraryFunctionName(string localName, int arity)
    {
        if (localName == "available-system-properties" || localName == "current" || localName == "current-output-uri") return arity == 0;
        if (localName == "copy-of" || localName == "snapshot") return arity == 0 || arity == 1;
        if (localName == "document" || localName == "function-available" || localName == "unparsed-entity-public-id" || localName == "unparsed-entity-uri") return arity == 1 || arity == 2;
        if (localName == "system-property" || localName == "element-available" || localName == "type-available" || localName == "regex-group" || localName == "stream-available") return arity == 1;
        if (localName == "current-group" || localName == "current-grouping-key" || localName == "current-merge-key") return arity == 0;
        if (localName == "current-merge-group") return arity == 0 || arity == 1;
        if (localName == "key") return arity == 2 || arity == 3;
        return (localName == "accumulator-before" || localName == "accumulator-after") && arity == 1;
    }

    internal bool IsXsltStylesheetFunctionName(XmlName name, int arity)
    {
        XPathFunctionBinding[] bindings = Loader.StylesheetFunctions;
        int index = 0;
        while (index < bindings.Length)
        {
            XmlName? candidate = bindings[index].Name;
            if (candidate != null && candidate.LocalName == name.LocalName && (candidate.NamespaceUri == null ? "" : candidate.NamespaceUri) == (name.NamespaceUri == null ? "" : name.NamespaceUri) && bindings[index].Arity == arity)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        string localName = binding.Name == null || binding.Name.LocalName == null ? "" : binding.Name.LocalName;
        string? bindingKey = binding.BindingKey;
        if (bindingKey != null && (bindingKey.StartsWith("xslt-function:", System.StringComparison.Ordinal) || bindingKey.StartsWith("xslt-original-function:", System.StringComparison.Ordinal)))
        {
            if (context.XsltFunctions == null)
            {
                throw CreateXsltContextFunctionError("The stylesheet function runtime is absent.", XPathErrorCodes.XPDY0002);
            }

            return context.XsltFunctions.InvokeXsltFunction(binding, arguments, context);
        }

        if (localName == "current")
        {
            XdmSequence? current = context.XsltCurrentItem;
            if (current == null || current.Count == 0) throw CreateXsltContextFunctionError("The current item is absent.", XPathErrorCodes.XPDY0002);
            return current;
        }

        if (localName == "current-output-uri")
        {
            return context.XsltCurrentOutputUri == null ? Sequences.CreateEmptyXdmSequence() : Sequences.CreateXdmAtomicSequence(Factory.CreateXdmAnyUriAtomicValue(context.XsltCurrentOutputUri));
        }

        if (localName == "available-system-properties")
        {
            return CreateXsltAvailableSystemProperties();
        }

        if (localName == "copy-of" || localName == "snapshot")
        {
            XdmSequence? supplied = arguments.Length == 0 ? context.XsltCurrentItem : arguments[0];
            if (supplied == null) throw CreateXsltContextFunctionError("The context item is absent.", XPathErrorCodes.XPDY0002);
            return CopyXsltFunctionSequence(supplied, localName == "snapshot", context);
        }

        if (localName == "stream-available")
        {
            return CreateXsltStaticFunctionBoolean(false);
        }

        if (localName == "unparsed-entity-public-id" || localName == "unparsed-entity-uri")
        {
            return EvaluateXsltUnparsedEntity(localName, arguments, context);
        }

        if (localName == "document")
        {
            return EvaluateXsltDocumentFunction(arguments, context);
        }

        if (localName == "key")
        {
            if (context.XsltKeys == null)
            {
                throw CreateXsltContextFunctionError("A dynamic function call has no named-key context.", XsltErrorCodes.XTDE1260);
            }

            return context.XsltKeys.EvaluateXsltKey(arguments, context, Namespaces);
        }

        if (localName == "accumulator-before" || localName == "accumulator-after")
        {
            if (context.XsltAccumulators == null)
            {
                throw CreateXsltContextFunctionError("The accumulator dynamic context is absent.", XsltErrorCodes.XTDE3350);
            }

            return context.XsltAccumulators.EvaluateXsltAccumulator(arguments[0], context, Namespaces, localName == "accumulator-after");
        }

        if (localName == "current-group")
        {
            if (!context.XsltCurrentGroupAvailable || context.XsltCurrentGroup == null)
            {
                throw CreateXsltContextFunctionError("The current group is absent.", XsltErrorCodes.XTDE1061);
            }

            return context.XsltCurrentGroup;
        }

        if (localName == "current-grouping-key")
        {
            if (!context.XsltCurrentGroupingKeyAvailable || context.XsltCurrentGroupingKey == null)
            {
                throw CreateXsltContextFunctionError("The current grouping key is absent.", XsltErrorCodes.XTDE1071);
            }

            return context.XsltCurrentGroupingKey;
        }

        if (localName == "current-merge-group")
        {
            return EvaluateXsltCurrentMergeGroup(arguments, context);
        }

        if (localName == "current-merge-key")
        {
            if (!context.XsltCurrentMergeKeyAvailable || context.XsltCurrentMergeKey == null)
            {
                throw CreateXsltContextFunctionError("The current merge key is absent.", XsltErrorCodes.XTDE3510);
            }

            return context.XsltCurrentMergeKey;
        }

        if (localName == "regex-group")
        {
            return EvaluateXsltRegexGroup(arguments[0], context);
        }

        string lexicalName = ReadXsltStaticFunctionString(arguments[0]);
        XmlName? name = ResolveXsltStaticFunctionName(lexicalName);
        if (localName == "system-property")
        {
            return CreateXsltStaticFunctionString(ReadXsltSystemProperty(name));
        }

        if (localName == "element-available")
        {
            return CreateXsltStaticFunctionBoolean(IsXsltElementAvailable(name));
        }

        if (localName == "type-available")
        {
            return CreateXsltStaticFunctionBoolean(IsXsltTypeAvailable(name));
        }

        int arity = arguments.Length == 2 ? ReadXsltStaticFunctionArity(arguments[1]) : -1;
        return CreateXsltStaticFunctionBoolean(IsXsltFunctionAvailable(name, arity));
    }

    internal XdmSequence EvaluateXsltCurrentMergeGroup(XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (!context.XsltCurrentMergeGroupAvailable || context.XsltCurrentMergeGroup == null)
        {
            throw CreateXsltContextFunctionError("The current merge group is absent.", XsltErrorCodes.XTDE3480);
        }

        if (arguments.Length == 0)
        {
            return context.XsltCurrentMergeGroup;
        }

        string source = ReadXsltStaticFunctionString(arguments[0]);
        string[] names = context.XsltCurrentMergeSourceNames == null ? [] : context.XsltCurrentMergeSourceNames;
        XdmSequence[] groups = context.XsltCurrentMergeSourceGroups == null ? [] : context.XsltCurrentMergeSourceGroups;
        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == source)
            {
                return groups[index];
            }

            index = index + 1;
        }

        throw CreateXsltContextFunctionError("No merge source has the name '" + source + "'.", XsltErrorCodes.XTDE3490);
    }

    internal XdmSequence EvaluateXsltRegexGroup(XdmSequence argument, XPathDynamicContext context)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(argument);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        System.Numerics.BigInteger number = item.AtomicValue == null ? new System.Numerics.BigInteger(-1) : new XdmNumericDatatype().ReadXdmIntegerValue(item.AtomicValue);
        string?[] groups = context.XsltRegexGroups == null ? [] : context.XsltRegexGroups;
        string value = number < System.Numerics.BigInteger.Zero || number >= new System.Numerics.BigInteger(groups.Length) ? "" : groups[(int)number] ?? "";
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.String));
    }

    internal XPathDynamicException CreateXsltContextFunctionError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XsltDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }

    internal string ReadXsltStaticFunctionString(XdmSequence sequence)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(sequence);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        return item.AtomicValue == null ? "" : Converter.ReadXdmAtomicStringValue(item.AtomicValue);
    }

    internal int ReadXsltStaticFunctionArity(XdmSequence sequence)
    {
        XdmSequence atomized = Converter.AtomizeXdmSequence(sequence);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        if (item.AtomicValue == null)
        {
            return -1;
        }

        string value = Converter.ReadXdmAtomicStringValue(item.AtomicValue);
        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return -1;
            }

            result = result * 10 + value[index] - '0';
            index = index + 1;
        }

        return value.Length == 0 ? -1 : result;
    }

    internal XmlName? ResolveXsltStaticFunctionName(string lexicalName)
    {
        string value = lexicalName.Trim();
        XmlName result = new XmlName();
        if (value.StartsWith("Q{", System.StringComparison.Ordinal))
        {
            int close = value.IndexOf('}');
            if (close < 2 || close == value.Length - 1)
            {
                return null;
            }

            result.NamespaceUri = value.Substring(2, close - 2);
            result.LocalName = value.Substring(close + 1);
            result.QualifiedName = value;
            return result;
        }

        int colon = value.IndexOf(':');
        if (colon < 0)
        {
            result.NamespaceUri = "";
            result.LocalName = value;
            result.QualifiedName = value;
            return result;
        }

        if (colon == 0 || colon != value.LastIndexOf(':') || colon == value.Length - 1)
        {
            return null;
        }

        string prefix = value.Substring(0, colon);
        string? namespaceUri = LookupXsltStaticFunctionNamespace(prefix);
        if (namespaceUri == null)
        {
            return null;
        }

        result.Prefix = prefix;
        result.NamespaceUri = namespaceUri;
        result.LocalName = value.Substring(colon + 1);
        result.QualifiedName = value;
        return result;
    }

    internal string? LookupXsltStaticFunctionNamespace(string prefix)
    {
        int index = 0;
        while (index < Namespaces.Length)
        {
            if (Namespaces[index].Prefix == prefix)
            {
                return Namespaces[index].NamespaceUri;
            }

            index = index + 1;
        }

        if (prefix == "xsl")
        {
            return XsltNamespaces.Xslt;
        }

        if (prefix == "fn")
        {
            return XPathNamespaces.Functions;
        }

        if (prefix == "xs")
        {
            return XPathNamespaces.XmlSchema;
        }

        return null;
    }

    internal string ReadXsltSystemProperty(XmlName? name)
    {
        if (name == null || name.NamespaceUri != XsltNamespaces.Xslt)
        {
            return "";
        }

        string localName = name.LocalName == null ? "" : name.LocalName;
        if (localName == "version")
        {
            return "3.0";
        }

        if (localName == "vendor")
        {
            return "libs22 xml";
        }

        if (localName == "vendor-url")
        {
            return "https://github.com/libs22/xml";
        }

        if (localName == "product-name")
        {
            return "xml";
        }

        if (localName == "product-version")
        {
            return "1";
        }

        if (localName == "is-schema-aware")
        {
            return Loader.Options.ConformanceLevel == XsltConformanceLevel.SchemaAware ? "yes" : "no";
        }

        if (localName == "supports-streaming")
        {
            return "no";
        }

        if (localName == "supports-serialization" || localName == "supports-backwards-compatibility")
        {
            return "yes";
        }

        if (localName == "supports-namespace-axis")
        {
            return "no";
        }

        if (localName == "supports-dynamic-evaluation" || localName == "supports-higher-order-functions")
        {
            return "yes";
        }

        if (localName == "xpath-version")
        {
            return "3.1";
        }

        if (localName == "xsd-version")
        {
            return "1.1";
        }

        return "";
    }

    internal bool IsXsltElementAvailable(XmlName? name)
    {
        if (name == null)
        {
            return false;
        }

        if (name.NamespaceUri == XsltNamespaces.Xslt)
        {
            string localName = name.LocalName == null ? "" : name.LocalName;
            return new XsltStylesheetAssembler(Loader).IsXsltKnownElementName(localName);
        }

        XsltExtensionBinding[] extensions = Loader.Options.Extensions == null ? [] : Loader.Options.Extensions;
        int index = 0;
        while (index < extensions.Length)
        {
            XsltExtensionBinding? extension = extensions[index];
            if (extension != null && extension.IsInstruction && extension.Name != null && SameXsltStaticFunctionName(extension.Name, name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXsltTypeAvailable(XmlName? name)
    {
        if (name == null || name.LocalName == null)
        {
            return false;
        }

        if (name.NamespaceUri == XPathNamespaces.XmlSchema && (name.LocalName == "anyType" || name.LocalName == "anySimpleType" || name.LocalName == "untyped" || new XdmTypeSystem().FindXdmAtomicTypeDefinition(name) != null)) return true;
        XmlSchemaSet? schemas = Loader.Options.SchemaSet;
        CompiledXmlSchemaSet? compiled = schemas == null ? null : schemas.CompiledSchemaSet == null ? schemas.CompiledSet as CompiledXmlSchemaSet : schemas.CompiledSchemaSet;
        if (compiled == null) return false;
        int simpleIndex = 0;
        while (simpleIndex < compiled.SimpleTypes.Length)
        {
            XmlName? candidate = compiled.SimpleTypes[simpleIndex].Name;
            if (candidate != null && SameXsltStaticFunctionName(candidate, name)) return true;
            simpleIndex = simpleIndex + 1;
        }

        int complexIndex = 0;
        while (complexIndex < compiled.ComplexTypes.Length)
        {
            XmlName? candidate = compiled.ComplexTypes[complexIndex].Name;
            if (candidate != null && SameXsltStaticFunctionName(candidate, name)) return true;
            complexIndex = complexIndex + 1;
        }

        return false;
    }

    internal bool IsXsltFunctionAvailable(XmlName? name, int arity)
    {
        if (name == null)
        {
            return false;
        }

        if (name.NamespaceUri == XPathNamespaces.Functions)
        {
            if (arity >= 0 && (new XPathStaticAnalyzer(new XPathStaticContext()).IsKnownXPathBuiltinFunction(name, arity) || IsXsltLibraryFunctionName(name.LocalName == null ? "" : name.LocalName, arity))) return true;
            if (arity < 0 && IsXsltFunctionAvailableAtAnyArity(name)) return true;
            string localName = name.LocalName == null ? "" : name.LocalName;
            if (localName == "true" || localName == "false" || localName == "position" || localName == "last")
            {
                return arity < 0 || arity == 0;
            }

            if (localName == "current-group" || localName == "current-grouping-key")
            {
                return arity < 0 || arity == 0;
            }

            if (localName == "system-property" || localName == "element-available" || localName == "type-available" || localName == "boolean" || localName == "not" || localName == "count" || localName == "string")
            {
                return arity < 0 || arity == 1;
            }

            if (localName == "function-available")
            {
                return arity < 0 || arity == 1 || arity == 2;
            }

            if (localName == "concat")
            {
                return arity < 0 || arity >= 2;
            }
        }

        XPathFunctionBinding[] stylesheetFunctions = Loader.StylesheetFunctions;
        int functionIndex = 0;
        while (functionIndex < stylesheetFunctions.Length)
        {
            XPathFunctionBinding function = stylesheetFunctions[functionIndex];
            if (SameXsltStaticFunctionName(function.Name == null ? new XmlName() : function.Name, name) && (arity < 0 || function.Arity == arity) && (!function.IsExtensionBinding || function.Implementation != null)) return true;
            functionIndex = functionIndex + 1;
        }

        XPathFunctionBinding[] extensions = Loader.Options.Functions == null ? [] : Loader.Options.Functions;
        int index = 0;
        while (index < extensions.Length)
        {
            XPathFunctionBinding? extension = extensions[index];
            if (extension != null && extension.Implementation != null && extension.Name != null && SameXsltStaticFunctionName(extension.Name, name) && (arity < 0 || extension.Arity == arity))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsXsltFunctionAvailableAtAnyArity(XmlName name)
    {
        XPathStaticAnalyzer analyzer = new XPathStaticAnalyzer(new XPathStaticContext());
        string localName = name.LocalName == null ? "" : name.LocalName;
        int arity = 0;
        while (arity <= 20)
        {
            if (analyzer.IsKnownXPathBuiltinFunction(name, arity) || IsXsltLibraryFunctionName(localName, arity)) return true;
            arity = arity + 1;
        }

        return false;
    }

    internal bool SameXsltStaticFunctionName(XmlName left, XmlName right)
    {
        string leftNamespace = left.NamespaceUri == null ? "" : left.NamespaceUri;
        string rightNamespace = right.NamespaceUri == null ? "" : right.NamespaceUri;
        return leftNamespace == rightNamespace && left.LocalName == right.LocalName;
    }

    internal XdmSequence CreateXsltStaticFunctionString(string value)
    {
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmStringAtomicValue(value, XdmAtomicType.String));
    }

    internal XdmSequence CreateXsltStaticFunctionBoolean(bool value)
    {
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmBooleanAtomicValue(value ? "true" : "false"));
    }

    internal XdmSequence CreateXsltAvailableSystemProperties()
    {
        string[] names = ["version", "vendor", "vendor-url", "product-name", "product-version", "is-schema-aware", "supports-serialization", "supports-backwards-compatibility", "supports-namespace-axis", "supports-streaming", "supports-dynamic-evaluation", "supports-higher-order-functions", "xpath-version", "xsd-version"];
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < names.Length)
        {
            XmlName name = new XmlName();
            name.Prefix = "xsl";
            name.LocalName = names[index];
            name.NamespaceUri = XsltNamespaces.Xslt;
            name.QualifiedName = "xsl:" + names[index];
            XdmAtomicValue value = new XdmAtomicValue();
            value.Type = XdmAtomicType.QName;
            value.NameValue = name;
            value.LexicalValue = name.QualifiedName;
            value.CanonicalValue = name.QualifiedName;
            value.StringValue = name.QualifiedName;
            value.IsImmutable = true;
            result.AddXdmAtomicValue(value);
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XdmSequence CopyXsltFunctionSequence(XdmSequence source, bool snapshot, XPathDynamicContext context)
    {
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        int index = 0;
        while (index < source.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(source, index);
            if (item.Node != null)
            {
                XsltStreamedNodeRuntime? streamed = context.XsltStreamedNodes;
                if (streamed != null && streamed.IsXsltStreamedNode(item.Node) && !streamed.IsXsltStreamedNodeAvailable(item.Node))
                {
                    throw CreateXsltContextFunctionError("A streamed node cannot be grounded after its focus lifetime has ended.", XPathErrorCodes.XPDY0130);
                }

                if (streamed != null && streamed.IsXsltStreamedNodeConsumed(item.Node))
                {
                    throw CreateXsltContextFunctionError("A streamed node cannot be grounded after its subtree has been consumed.", XPathErrorCodes.XPDY0130);
                }

                if (streamed != null && !streamed.ReserveXsltStreamGrounding(item.Node, snapshot))
                {
                    throw CreateXsltContextFunctionError("Grounding the streamed node would exceed the configured node limit.", XPathErrorCodes.XPDY0130);
                }

                XmlNode copy = CopyXsltFunctionNode(item.Node, snapshot);
                result.AddXdmNode(copy);
                if (context.XsltFunctions != null) RecordXsltFunctionAccumulatorCopies(item.Node, copy, context.XsltFunctions.State);
            }
            else if (item.AtomicValue != null) result.AddXdmAtomicValue(item.AtomicValue);
            else if (item.Function != null) result.AddXdmFunction(item.Function);
            else if (item.Map != null) result.AddXdmMap(item.Map);
            else if (item.Array != null) result.AddXdmArray(item.Array);
            else throw new System.InvalidOperationException("An XDM item to copy is incomplete.");
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal XmlNode CopyXsltFunctionNode(XmlNode source, bool snapshot)
    {
        XmlDocument originalDocument = source.OwnerDocument == null ? new XmlDocument() : source.OwnerDocument;
        XmlDocument document = new XmlDocument();
        document.Version = originalDocument.Version;
        document.Standalone = originalDocument.Standalone;
        document.BaseUri = originalDocument.BaseUri;
        document.DocumentUri = originalDocument.DocumentUri;
        document.EncodingName = originalDocument.EncodingName;
        document.IsReadOnly = false;
        TreeMutation mutation = new TreeMutation();
        XmlNode selected = mutation.ImportXmlTreeNodeOperation(document, source, true);
        XmlNode outer = selected;
        XmlNode? ancestor = snapshot ? source.Parent : null;
        if (snapshot && source.Kind == XmlNodeKind.Attribute && ancestor != null)
        {
            XmlNode parentCopy = mutation.ImportXmlTreeNodeOperation(document, ancestor, false);
            XmlNode[] sourceAttributes = ancestor.Attributes == null ? [] : ancestor.Attributes;
            XmlNode[] copiedAttributes = parentCopy.Attributes == null ? [] : parentCopy.Attributes;
            int attributeIndex = 0;
            while (attributeIndex < sourceAttributes.Length && attributeIndex < copiedAttributes.Length)
            {
                if (sourceAttributes[attributeIndex] == source)
                {
                    selected = copiedAttributes[attributeIndex];
                    break;
                }

                attributeIndex = attributeIndex + 1;
            }

            outer = parentCopy;
            ancestor = ancestor.Parent;
        }

        while (ancestor != null && source.Kind != XmlNodeKind.Namespace)
        {
            XmlNode parentCopy = mutation.ImportXmlTreeNodeOperation(document, ancestor, false);
            mutation.AppendXmlTreeChildOperation(parentCopy, outer);
            outer = parentCopy;
            ancestor = ancestor.Parent;
        }

        if (outer.Kind == XmlNodeKind.Document || outer.Kind == XmlNodeKind.DocumentFragment)
        {
            document.DocumentNode = outer;
            XmlNode[] children = outer.Children == null ? [] : outer.Children;
            int childIndex = 0;
            while (childIndex < children.Length)
            {
                if (children[childIndex].Kind == XmlNodeKind.Element)
                {
                    document.DocumentElement = children[childIndex];
                    break;
                }

                childIndex = childIndex + 1;
            }
        }

        return selected;
    }

    internal void RecordXsltFunctionAccumulatorCopies(XmlNode source, XmlNode copy, XsltTransformationState state)
    {
        if (source.Kind != XmlNodeKind.Attribute && source.Kind != XmlNodeKind.Namespace) state.AccumulatorCopies.Add(new XsltAccumulatorCopyMapping(source, copy));
        XmlNode[] sourceChildren = source.Children == null ? [] : source.Children;
        XmlNode[] copiedChildren = copy.Children == null ? [] : copy.Children;
        int index = 0;
        while (index < sourceChildren.Length && index < copiedChildren.Length)
        {
            RecordXsltFunctionAccumulatorCopies(sourceChildren[index], copiedChildren[index], state);
            index = index + 1;
        }
    }

    internal XdmSequence EvaluateXsltUnparsedEntity(string localName, XdmSequence[] arguments, XPathDynamicContext context)
    {
        string entityName = ReadXsltStaticFunctionString(arguments[0]);
        XmlNode? node = arguments.Length == 1 ? ReadXsltContextNode(context) : ReadXsltSuppliedNode(arguments[1]);
        if (node == null || node.OwnerDocument == null) throw CreateXsltContextFunctionError("The unparsed entity function requires a node in a document.", XsltErrorCodes.XTDE1370);
        XmlDocumentType? documentType = node.OwnerDocument.DocumentType;
        XmlEntityDeclaration[] declarations = documentType == null || documentType.EntityDeclarations == null ? [] : documentType.EntityDeclarations;
        int index = 0;
        while (index < declarations.Length)
        {
            XmlEntityDeclaration declaration = declarations[index];
            if (declaration.Name == entityName && declaration.IsExternal && !declaration.IsParsed && declaration.NotationName != null)
            {
                string value = localName == "unparsed-entity-public-id" ? declaration.PublicIdentifier == null ? "" : declaration.PublicIdentifier : ResolveXsltEntitySystemIdentifier(declaration);
                return localName == "unparsed-entity-uri" ? Sequences.CreateXdmAtomicSequence(Factory.CreateXdmAnyUriAtomicValue(value)) : CreateXsltStaticFunctionString(value);
            }

            index = index + 1;
        }

        return localName == "unparsed-entity-uri" ? Sequences.CreateXdmAtomicSequence(Factory.CreateXdmAnyUriAtomicValue("")) : CreateXsltStaticFunctionString("");
    }

    internal XdmSequence EvaluateXsltDocumentFunction(XdmSequence[] arguments, XPathDynamicContext context)
    {
        XPathResourceRegistry? documents = context.XsltDocuments;
        if (documents == null) throw CreateXsltContextFunctionError("The XSLT document pool is unavailable.", XPathErrorCodes.FODC0002);
        string? suppliedBaseUri = arguments.Length == 2 ? ReadXsltDocumentBaseUri(arguments[1]) : null;
        XdmSequence values = arguments[0];
        XdmSequenceBuilder result = new XdmSequenceBuilder();
        System.Collections.Generic.HashSet<XmlDocument> added = new System.Collections.Generic.HashSet<XmlDocument>();
        int index = 0;
        while (index < values.Count)
        {
            XdmSequenceItem item = Sequences.ReadXdmSequenceItem(values, index);
            if (item.Node == null && item.AtomicValue == null) throw CreateXsltContextFunctionError("The document URI sequence must contain nodes or string-like atomic values.", XPathErrorCodes.XPTY0004);
            XdmSequence atomized = item.Node == null ? Sequences.CreateXdmAtomicSequence(item.AtomicValue == null ? throw CreateXsltContextFunctionError("The document URI item is incomplete.", XPathErrorCodes.XPTY0004) : item.AtomicValue) : Converter.AtomizeXdmSequence(Sequences.CreateXdmNodeSequence(item.Node));
            string? baseUri = suppliedBaseUri == null ? item.Node == null ? StaticBaseUri : new TreeNavigation().GetXmlNodeBaseUriOperation(item.Node) : suppliedBaseUri;
            int atomizedIndex = 0;
            while (atomizedIndex < atomized.Count)
            {
                XdmSequenceItem atomicItem = Sequences.ReadXdmSequenceItem(atomized, atomizedIndex);
                XdmAtomicValue? atomic = atomicItem.AtomicValue;
                if (atomic == null || atomic.Type != XdmAtomicType.String && atomic.Type != XdmAtomicType.AnyUri && atomic.Type != XdmAtomicType.UntypedAtomic) throw CreateXsltContextFunctionError("The document URI sequence must atomize to strings or URI values.", XPathErrorCodes.XPTY0004);
                string reference = Converter.ReadXdmAtomicStringValue(atomic);
                string? uri = ResolveXsltDocumentFunctionUri(reference, baseUri);
                if (uri == null) throw CreateXsltContextFunctionError("The document URI is invalid or cannot be resolved.", XsltErrorCodes.XTDE1162);
                XmlDocument? stylesheetDocument = FindXsltStylesheetDocument(uri);
                XmlDocument document = stylesheetDocument == null ? LoadXsltFunctionDocument(documents, uri, context) : stylesheetDocument;
                if (document.DocumentNode != null && added.Add(document)) result.AddXdmNode(document.DocumentNode);
                atomizedIndex = atomizedIndex + 1;
            }
            index = index + 1;
        }

        return result.BuildImmutableXdmSequence();
    }

    internal string? ReadXsltDocumentBaseUri(XdmSequence supplied)
    {
        XmlNode? node = ReadXsltSuppliedNode(supplied);
        if (node == null) throw CreateXsltContextFunctionError("The second argument of document() must be one node.", XPathErrorCodes.XPTY0004);
        return new TreeNavigation().GetXmlNodeBaseUriOperation(node);
    }

    internal XmlDocument? FindXsltStylesheetDocument(string uri)
    {
        XsltStylesheetModule[] modules = Loader.Modules.ToArray();
        int index = 0;
        while (index < modules.Length)
        {
            string? baseUri = modules[index].BaseUri;
            if (baseUri != null && baseUri == uri) return modules[index].Document;
            index = index + 1;
        }

        return null;
    }

    internal string? ResolveXsltDocumentFunctionUri(string reference, string? baseUri)
    {
        System.Threading.Tasks.Task<string> pending = System.Threading.Tasks.Task.Run(() => UriTools.HasUriScheme(reference) ? UriTools.CanonicalAbsoluteUri(UriTools.CreateAbsolute(reference)) : baseUri == null ? throw new System.UriFormatException("A relative document URI has no base URI.") : UriTools.CanonicalAbsoluteUri(UriTools.ResolveReference(UriTools.CreateAbsolute(baseUri), reference)));
        ((System.IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        return pending.Status == System.Threading.Tasks.TaskStatus.RanToCompletion ? pending.Result : null;
    }

    internal XmlDocument LoadXsltFunctionDocument(XPathResourceRegistry documents, string uri, XPathDynamicContext context)
    {
        XmlDocument? existing = documents.FindXPathDocumentResource(uri);
        if (existing != null) return existing;
        IXmlResourceResolver? optionalResolver = documents.ResourceResolver;
        if (optionalResolver == null) throw CreateXsltContextFunctionError("No XML resource resolver is available for the document function.", XPathErrorCodes.FODC0002);
        IXmlResourceResolver resolver = optionalResolver;
        System.Threading.Tasks.Task<XmlResolvedResource> resolving = System.Threading.Tasks.Task.Run(() => ResolveXsltFunctionDocumentResource(resolver, uri, documents.Policy));
        ((System.IAsyncResult)resolving).AsyncWaitHandle.WaitOne();
        if (resolving.Status != System.Threading.Tasks.TaskStatus.RanToCompletion || resolving.Result == null || resolving.Result.Access != XmlResourceAccess.Allowed || resolving.Result.Input == null) throw CreateXsltContextFunctionError("The requested document is unavailable.", XPathErrorCodes.FODC0002);
        XmlResolvedResource resource = resolving.Result;
        XmlInput source = resource.Input;
        XmlInput input = new XmlInput();
        input.Kind = source.Kind;
        input.Text = source.Text;
        input.Characters = source.Characters;
        input.Bytes = source.Bytes;
        input.ByteStream = source.ByteStream;
        input.TextReader = source.TextReader;
        input.BaseUri = resource.AbsoluteUri == null ? uri : resource.AbsoluteUri;
        input.MediaType = resource.MediaType == null ? source.MediaType : resource.MediaType;
        input.EncodingName = resource.EncodingName == null ? source.EncodingName : resource.EncodingName;
        input.Encoding = source.Encoding;
        input.OwnsResource = resource.OwnsResource || source.OwnsResource;
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.DtdProcessing = XmlDtdProcessing.Prohibit;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.PreserveComments = true;
        options.PreserveProcessingInstructions = true;
        options.PreservePrefixes = true;
        options.PreserveSourceLocations = true;
        options.RetainNamespaceDeclarations = true;
        options.SecurityLimits = context.SecurityLimits;
        options.ResolverPolicy = context.ResolverPolicy;
        System.Threading.Tasks.Task<XmlDocument> parsing = System.Threading.Tasks.Task.Run(() => new XmlParser().ParseXmlDocument(input, options, resolver, null, context.Cancellation));
        ((System.IAsyncResult)parsing).AsyncWaitHandle.WaitOne();
        if (parsing.Status != System.Threading.Tasks.TaskStatus.RanToCompletion || parsing.Result == null)
        {
            System.Exception? failure = parsing.Exception == null ? null : parsing.Exception.InnerException;
            throw CreateXsltContextFunctionError("The requested document could not be parsed" + (failure == null ? "." : ": " + failure.Message), XPathErrorCodes.FODC0002);
        }
        XmlDocument result = parsing.Result;
        result.BaseUri = uri;
        result.DocumentUri = uri;
        documents.RegisterXPathDocumentResource(uri, result);
        return result;
    }

    internal XmlResolvedResource ResolveXsltFunctionDocumentResource(IXmlResourceResolver resolver, string uri, XmlResolverPolicy policy)
    {
        ResolverGuard guard = new ResolverGuard(policy, null);
        using (ResolverGuardScope scope = guard.BeginXmlResource(uri, null))
        {
            XmlResolvedResource resource = resolver.ResolveXmlResource(XmlResourceKind.Document, null, uri, null, policy);
            guard.ValidateXmlResolvedResource(scope, XmlResourceKind.Document, resource);
            return resource;
        }
    }

    internal XmlNode? ReadXsltContextNode(XPathDynamicContext context)
    {
        return context.ContextItem == null ? null : ReadXsltSuppliedNode(context.ContextItem);
    }

    internal XmlNode? ReadXsltSuppliedNode(XdmSequence supplied)
    {
        if (supplied.Count != 1) return null;
        return Sequences.ReadXdmSequenceItem(supplied, 0).Node;
    }

    internal string ResolveXsltEntitySystemIdentifier(XmlEntityDeclaration declaration)
    {
        string systemIdentifier = declaration.SystemIdentifier == null ? "" : declaration.SystemIdentifier;
        if (systemIdentifier.Length == 0 || UriTools.HasUriScheme(systemIdentifier) || declaration.BaseUri == null || declaration.BaseUri.Length == 0) return systemIdentifier;
        System.Threading.Tasks.Task<string> pending = System.Threading.Tasks.Task.Run(() => UriTools.CanonicalAbsoluteUri(UriTools.ResolveReference(UriTools.CreateAbsolute(declaration.BaseUri), systemIdentifier)));
        ((System.IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        return pending.Status == System.Threading.Tasks.TaskStatus.RanToCompletion ? pending.Result : systemIdentifier;
    }
}
