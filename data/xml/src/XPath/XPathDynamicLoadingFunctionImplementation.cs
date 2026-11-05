using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace xml;

internal class XPathDynamicLoadingFunctionImplementation : IXPathFunction
{
    internal string LocalName;
    internal SourceRange Range;
    internal XPathStaticContext StaticContext;
    internal XdmSequenceOperations Sequences;
    internal XdmTypeConverter Converter;
    internal XdmMapOperations Maps;
    internal XdmFactory Factory;

    internal XPathDynamicLoadingFunctionImplementation(string localName, SourceRange range, XPathStaticContext staticContext)
    {
        LocalName = localName;
        Range = range;
        StaticContext = staticContext;
        Sequences = new XdmSequenceOperations();
        Converter = new XdmTypeConverter();
        Maps = new XdmMapOperations();
        Factory = new XdmFactory();
    }

    public XdmSequence InvokeXPathFunction(XPathFunctionBinding binding, XdmSequence[] arguments, XPathDynamicContext context)
    {
        if (LocalName == "load-xquery-module")
        {
            ValidateXPathModuleArguments(arguments);
            throw CreateXPathDynamicLoadingError("No suitable XQuery processor is available.", XPathErrorCodes.FOQM0006);
        }

        ValidateXPathTransformArguments(arguments);
        Task<XdmSequence> pending = Task.Run(() => TransformXPathStylesheet(arguments[0], context));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status == TaskStatus.RanToCompletion) return pending.Result;
        Exception failure = ReadXPathTransformFailure(pending.Exception);
        string? typeName = failure.GetType().FullName;
        if (typeName == "xml.XPathDynamicException" || typeName == "xml.XPathTypeException") throw failure;
        XmlDiagnostic? diagnostic = ReadXPathTransformDiagnostic(failure);
        if (diagnostic != null)
        {
            diagnostic.Category = XmlErrorCategory.XPathDynamic;
            diagnostic.Range = Range;
            throw new XPathDynamicException(diagnostic.Message == null ? failure.Message : diagnostic.Message, diagnostic, Range.Start);
        }

        throw CreateXPathDynamicLoadingError(failure.Message.Length == 0 ? "The XSLT transformation failed." : failure.Message, XPathErrorCodes.FOXT0003);
    }

    internal XdmSequence TransformXPathStylesheet(XdmSequence supplied, XPathDynamicContext context)
    {
        XdmMapValue map = ReadXPathTransformMap(supplied);
        ValidateXPathTransformInvocationOptions(map);
        ValidateXPathTransformVersion(map);
        XsltCompileOptions compileOptions = CreateXPathTransformCompileOptions(map, context);
        IXmlResourceResolver resolver = context.ResourceResolver == null ? new XmlDefaultResolver() : context.ResourceResolver;
        XsltExecutable executable = CompileXPathTransformStylesheet(map, compileOptions, resolver, context);
        XsltTransformOptions options = CreateXPathTransformOptions(map, executable, context);
        XsltTransformationState state = new XsltTransformationEngine().InitializeXsltTransformation(executable, options);
        if (state.TemplateRuntime == null) throw new InvalidOperationException("The XSLT template runtime is unavailable.");
        state.TemplateRuntime.ExecuteXsltInitialTemplateInvocation();
        return CreateXPathTransformResultMap(state, ReadXPathTransformDeliveryFormat(map));
    }

    internal XsltCompileOptions CreateXPathTransformCompileOptions(XdmMapValue map, XPathDynamicContext context)
    {
        XPathRuntimeContext runtime = new XPathRuntimeContext(context);
        XsltCompileOptions result = new XsltCompileOptions();
        result.BaseUri = ReadXPathTransformOptionalUri(map, "stylesheet-base-uri", StaticContext.BaseUri);
        result.StaticParameters = ReadXPathTransformParameters(map, "static-params", true, false);
        result.ResolverPolicy = runtime.ResourcePolicy;
        result.SecurityLimits = runtime.Limits;
        return result;
    }

    internal XsltExecutable CompileXPathTransformStylesheet(XdmMapValue map, XsltCompileOptions options, IXmlResourceResolver resolver, XPathDynamicContext context)
    {
        string[] names = ["stylesheet-location", "stylesheet-node", "stylesheet-text", "stylesheet-executable"];
        int suppliedCount = 0;
        string selected = "";
        int index = 0;
        while (index < names.Length)
        {
            if (FindXPathTransformOption(map, names[index]) != null)
            {
                suppliedCount = suppliedCount + 1;
                selected = names[index];
            }

            index = index + 1;
        }

        if (suppliedCount != 1) throw CreateXPathDynamicLoadingError("Exactly one stylesheet input option must be supplied.", XPathErrorCodes.FOXT0002);
        XmlInput input;
        if (selected == "stylesheet-location")
        {
            string location = ReadXPathTransformString(map, selected);
            string absolute = ResolveXPathTransformUri(location, StaticContext.BaseUri, selected);
            XmlResolverPolicy policy = new XPathRuntimeContext(context).ResourcePolicy;
            XmlResolvedResource resource = resolver.ResolveXmlResource(XmlResourceKind.Stylesheet, null, absolute, StaticContext.BaseUri, policy);
            if (resource == null || resource.Access != XmlResourceAccess.Allowed || resource.Input == null) throw CreateXPathDynamicLoadingError("The stylesheet location could not be resolved.", XPathErrorCodes.FOXT0002);
            input = resource.Input;
            input.BaseUri = resource.AbsoluteUri == null ? absolute : resource.AbsoluteUri;
            options.BaseUri = input.BaseUri;
        }
        else if (selected == "stylesheet-node")
        {
            XmlNode node = ReadXPathTransformNode(map, selected);
            input = CreateXPathTransformNodeInput(node);
            if (options.BaseUri == null) options.BaseUri = input.BaseUri;
        }
        else
        {
            input = new XmlInput();
            input.Kind = XmlInputKind.String;
            input.Text = ReadXPathTransformString(map, selected);
            input.BaseUri = options.BaseUri == null ? StaticContext.BaseUri : options.BaseUri;
        }

        return new XsltCompiler().CompileXslt(input, options, resolver);
    }

    internal XmlInput CreateXPathTransformNodeInput(XmlNode node)
    {
        if (node.Kind != XmlNodeKind.Document && node.Kind != XmlNodeKind.Element) throw CreateXPathDynamicLoadingError("stylesheet-node must be a document or element node.", XPathErrorCodes.FOXT0002);
        XmlDocument document = node.OwnerDocument == null ? throw CreateXPathDynamicLoadingError("stylesheet-node must belong to a document.", XPathErrorCodes.FOXT0002) : node.OwnerDocument;
        StringBuilder builder = new StringBuilder();
        AppendXPathTransformXmlNode(builder, node, true);
        XmlInput result = new XmlInput();
        result.Kind = XmlInputKind.String;
        result.Text = builder.ToString();
        result.BaseUri = document.BaseUri;
        return result;
    }

    internal void AppendXPathTransformXmlNode(StringBuilder builder, XmlNode node, bool root)
    {
        if (node.Kind == XmlNodeKind.Document || node.Kind == XmlNodeKind.DocumentFragment)
        {
            AppendXPathTransformXmlChildren(builder, node.Children, root);
            return;
        }

        if (node.Kind == XmlNodeKind.Element)
        {
            string name = ReadXPathTransformXmlName(node);
            builder.Append('<');
            builder.Append(name);
            XmlNamespaceBinding[] namespaces = root ? new TreeNavigation().SelectXmlNamespacesOperation(node) : node.NamespaceBindings == null ? [] : node.NamespaceBindings;
            int namespaceIndex = 0;
            while (namespaceIndex < namespaces.Length)
            {
                string? optionalPrefix = namespaces[namespaceIndex].Prefix;
                string? optionalUri = namespaces[namespaceIndex].NamespaceUri;
                string prefix = optionalPrefix == null ? "" : optionalPrefix;
                string uri = optionalUri == null ? "" : optionalUri;
                builder.Append(prefix.Length == 0 ? " xmlns=\"" : " xmlns:" + prefix + "=\"");
                builder.Append(EscapeXPathTransformXmlValue(uri, true));
                builder.Append('"');
                namespaceIndex = namespaceIndex + 1;
            }

            XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                if (!IsXPathTransformNamespaceDeclaration(attributes[attributeIndex]))
                {
                    builder.Append(' ');
                    builder.Append(ReadXPathTransformXmlName(attributes[attributeIndex]));
                    builder.Append("=\"");
                    string? optionalValue = attributes[attributeIndex].Value;
                    builder.Append(EscapeXPathTransformXmlValue(optionalValue == null ? "" : optionalValue, true));
                    builder.Append('"');
                }

                attributeIndex = attributeIndex + 1;
            }

            builder.Append('>');
            AppendXPathTransformXmlChildren(builder, node.Children, false);
            builder.Append("</");
            builder.Append(name);
            builder.Append('>');
            return;
        }

        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            builder.Append(EscapeXPathTransformXmlValue(node.Value == null ? "" : node.Value, false));
        }
        else if (node.Kind == XmlNodeKind.Comment)
        {
            builder.Append("<!--");
            builder.Append(node.Value);
            builder.Append("-->");
        }
        else if (node.Kind == XmlNodeKind.ProcessingInstruction)
        {
            builder.Append("<?");
            builder.Append(ReadXPathTransformXmlName(node));
            if (node.Value != null && node.Value.Length != 0)
            {
                builder.Append(' ');
                builder.Append(node.Value);
            }

            builder.Append("?>");
        }
    }

    internal void AppendXPathTransformXmlChildren(StringBuilder builder, XmlNode[]? children, bool root)
    {
        XmlNode[] values = children == null ? [] : children;
        int index = 0;
        while (index < values.Length)
        {
            AppendXPathTransformXmlNode(builder, values[index], root);
            index = index + 1;
        }
    }

    internal string ReadXPathTransformXmlName(XmlNode node)
    {
        XmlName? name = node.Name;
        if (name == null || name.LocalName == null) return "";
        if (node.Kind == XmlNodeKind.Namespace) return name.LocalName.Length == 0 || name.LocalName == "xmlns" ? "xmlns" : "xmlns:" + name.LocalName;
        if (name.NamespaceUri == XmlNamespaces.Xmlns) return name.LocalName == "xmlns" ? "xmlns" : "xmlns:" + name.LocalName;
        if (node.Kind == XmlNodeKind.Attribute && IsXPathTransformNamespaceDeclaration(node)) return name.LocalName == "xmlns" ? "xmlns" : "xmlns:" + name.LocalName;
        if (name.QualifiedName != null && name.QualifiedName.IndexOf(':') >= 0) return name.QualifiedName;
        string? optionalPrefix = name.Prefix;
        string prefix = optionalPrefix == null ? "" : optionalPrefix;
        if (prefix.Length == 0 && name.NamespaceUri != null && name.NamespaceUri.Length != 0) prefix = FindXPathTransformXmlPrefix(node, name.NamespaceUri);
        return prefix.Length == 0 ? name.LocalName : prefix + ":" + name.LocalName;
    }

    internal string FindXPathTransformXmlPrefix(XmlNode node, string namespaceUri)
    {
        XmlNode? current = node.Kind == XmlNodeKind.Attribute ? node.Parent : node;
        while (current != null)
        {
            XmlNamespaceBinding[] bindings = current.NamespaceBindings == null ? [] : current.NamespaceBindings;
            int bindingIndex = 0;
            while (bindingIndex < bindings.Length)
            {
                string? bindingUri = bindings[bindingIndex].NamespaceUri;
                string? bindingPrefix = bindings[bindingIndex].Prefix;
                if (bindingUri == namespaceUri) return bindingPrefix == null ? "" : bindingPrefix;
                bindingIndex = bindingIndex + 1;
            }

            XmlNode[] attributes = current.Attributes == null ? [] : current.Attributes;
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                XmlName? name = attributes[attributeIndex].Name;
                if (name != null && IsXPathTransformNamespaceDeclaration(attributes[attributeIndex]) && attributes[attributeIndex].Value == namespaceUri) return name.LocalName == null || name.LocalName == "xmlns" ? "" : name.LocalName;
                attributeIndex = attributeIndex + 1;
            }

            current = current.Parent;
        }

        return "";
    }

    internal bool IsXPathTransformNamespaceDeclaration(XmlNode node)
    {
        XmlName? name = node.Name;
        if (node.Kind == XmlNodeKind.Namespace || name != null && name.NamespaceUri == XmlNamespaces.Xmlns) return true;
        if (name == null || name.LocalName == null || node.Value == null || node.Parent == null || !UriTools.HasUriScheme(node.Value)) return false;
        if (name.LocalName == "xsl" && node.Value == XsltNamespaces.Xslt) return true;
        string? prefix = name.Prefix;
        string? namespaceUri = name.NamespaceUri;
        if (node.Parent.Parent != null && node.Parent.Parent.Kind == XmlNodeKind.Document && (prefix == null || prefix.Length == 0) && (namespaceUri == null || namespaceUri.Length == 0)) return true;
        return UsesXPathTransformNamespace(node.Parent, name.LocalName, node.Value);
    }

    internal bool UsesXPathTransformNamespace(XmlNode node, string prefix, string namespaceUri)
    {
        XmlName? name = node.Name;
        if (name != null && name.NamespaceUri == namespaceUri) return true;
        XmlNode[] attributes = node.Attributes == null ? [] : node.Attributes;
        int attributeIndex = 0;
        while (attributeIndex < attributes.Length)
        {
            string? value = attributes[attributeIndex].Value;
            if (!object.ReferenceEquals(attributes[attributeIndex], node) && value != null && value.IndexOf(prefix + ":", StringComparison.Ordinal) >= 0) return true;
            attributeIndex = attributeIndex + 1;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            if (UsesXPathTransformNamespace(children[childIndex], prefix, namespaceUri)) return true;
            childIndex = childIndex + 1;
        }

        return false;
    }

    internal string EscapeXPathTransformXmlValue(string value, bool attribute)
    {
        return new XmlEscaper().EscapeXmlValue(value, XmlVersion.Xml10, attribute ? XmlCharacterEscapeContext.AttributeDoubleQuoted : XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, attribute ? '"' : '\0');
    }

    internal XsltTransformOptions CreateXPathTransformOptions(XdmMapValue map, XsltExecutable executable, XPathDynamicContext context)
    {
        XsltTransformOptions result = new XsltTransformOptions();
        result.Executable = executable;
        result.ResourceResolver = context.ResourceResolver;
        result.SecurityLimits = context.SecurityLimits;
        result.EnableAssertions = ReadXPathTransformBoolean(map, "enable-assertions", false);
        result.EnableTracing = ReadXPathTransformBoolean(map, "enable-trace", false);
        result.BaseOutputUri = ReadXPathTransformOptionalUri(map, "base-output-uri", StaticContext.BaseUri);
        result.ResultDocumentResolver = new XsltResultDocumentCollector();
        XdmSequence? source = FindXPathTransformOption(map, "source-node");
        XdmSequence? selection = FindXPathTransformOption(map, "initial-match-selection");
        if (source != null && selection != null) throw CreateXPathDynamicLoadingError("source-node and initial-match-selection are mutually exclusive.", XPathErrorCodes.FOXT0002);
        if (source != null)
        {
            XmlNode node = ReadXPathTransformNode(map, "source-node");
            result.InitialMatchSelection = Sequences.CreateXdmNodeSequence(node);
            if (node.OwnerDocument != null && node.OwnerDocument.DocumentNode != null) result.GlobalContextItem = Sequences.CreateXdmNodeSequence(node.OwnerDocument.DocumentNode);
        }
        else if (selection != null)
        {
            result.InitialMatchSelection = selection;
        }

        XdmSequence? global = FindXPathTransformOption(map, "global-context-item");
        if (global != null)
        {
            if (global.Count != 1) throw CreateXPathDynamicLoadingError("global-context-item must contain exactly one item.", XPathErrorCodes.FOXT0002);
            result.GlobalContextItem = global;
        }

        result.InitialMode = ReadXPathTransformOptionalQName(map, "initial-mode");
        result.InitialTemplate = ReadXPathTransformOptionalQName(map, "initial-template");
        result.InitialFunction = ReadXPathTransformOptionalQName(map, "initial-function");
        result.FunctionParameters = ReadXPathTransformFunctionParameters(map);
        if (result.InitialFunction == null && result.FunctionParameters.Length != 0) throw CreateXPathDynamicLoadingError("function-params requires initial-function.", XPathErrorCodes.FOXT0002);
        result.Parameters = ReadXPathTransformParameters(map, "stylesheet-params", false, false);
        List<XsltParameter> initialTemplateParameters = new List<XsltParameter>();
        AppendXPathTransformParameters(initialTemplateParameters, ReadXPathTransformParameters(map, "template-params", false, false));
        AppendXPathTransformParameters(initialTemplateParameters, ReadXPathTransformParameters(map, "tunnel-params", false, true));
        result.InitialTemplateParameters = initialTemplateParameters.ToArray();
        XdmSequence? serialization = FindXPathTransformOption(map, "serialization-params");
        if (serialization != null)
        {
            XsltStylesheetPlan plan = executable.CompiledPlan == null ? executable.Plan == null ? throw new InvalidOperationException("The XSLT executable plan is absent.") : (XsltStylesheetPlan)executable.Plan : executable.CompiledPlan;
            XmlSerializationOptions options = new XsltTransformationEngine().CopyXsltRuntimeSerializationOptions(new XsltTransformationEngine().FindXsltDefaultSerializationOptions(plan));
            new XPathParsingSerializationFunctionImplementation("serialize", Range, StaticContext).ApplyXPathSerializationParameters(options, serialization);
            result.SerializationOptions = options;
        }

        return result;
    }

    internal XdmSequence CreateXPathTransformResultMap(XsltTransformationState state, string delivery)
    {
        List<XdmMapEntry> entries = new List<XdmMapEntry>();
        string principalKey = state.PrincipalDestination.BaseUri == null ? "output" : state.PrincipalDestination.BaseUri;
        XdmSequence principal = state.Result.PrincipalValue == null ? Sequences.CreateEmptyXdmSequence() : state.Result.PrincipalValue;
        entries.Add(Maps.CreateXdmMapEntry(Factory.CreateXdmStringAtomicValue(principalKey, XdmAtomicType.String), DeliverXPathTransformValue(principal, state.PrincipalDestination.SerializationOptions, delivery, principalKey)));
        XsltSecondaryResult[] secondary = state.Result.SecondaryResults == null ? [] : state.Result.SecondaryResults;
        int index = 0;
        while (index < secondary.Length)
        {
            XsltSecondaryResult result = secondary[index];
            string key = result.Uri == null ? "" : result.Uri;
            XdmSequence value = result.Value == null ? Sequences.CreateEmptyXdmSequence() : result.Value;
            XmlSerializationOptions options = result.SerializationOptions == null ? state.PrincipalDestination.SerializationOptions : result.SerializationOptions;
            entries.Add(Maps.CreateXdmMapEntry(Factory.CreateXdmStringAtomicValue(key, XdmAtomicType.String), DeliverXPathTransformValue(value, options, delivery, key)));
            index = index + 1;
        }

        return Sequences.CreateXdmMapSequence(Maps.CreateXdmMap(entries.ToArray()));
    }

    internal XdmSequence DeliverXPathTransformValue(XdmSequence value, XmlSerializationOptions options, string delivery, string baseUri)
    {
        if (delivery == "raw") return Sequences.FreezeXdmSequenceStorage(value);
        if (delivery == "document")
        {
            XmlDocument document = new SequenceNormalizer().NormalizeXdmSerializationSequence(value, options.ItemSeparator);
            document.BaseUri = baseUri == "output" ? null : baseUri;
            document.DocumentUri = baseUri == "output" ? null : baseUri;
            return Sequences.CreateXdmNodeSequence(document.DocumentNode == null ? throw new InvalidOperationException("Sequence normalization did not create a document node.") : document.DocumentNode);
        }

        MemoryStream stream = new MemoryStream();
        XmlOutput output = new XmlOutput();
        output.Kind = XmlOutputKind.Stream;
        output.ByteStream = stream;
        output.EncodingName = options.Encoding;
        new XmlSerializer().SerializeXdmSequence(value, output, options);
        Encoding encoding = EncodingDetector.ResolveXmlEncodingName(options.Encoding == null ? "utf-8" : options.Encoding, EncodingPolicyKind.BuiltInText);
        byte[] bytes = stream.ToArray();
        int preamble = EncodingDetector.GetByteOrderMarkLength(EncodingDetector.DetectByteOrderMark(bytes));
        string serialized = encoding.GetString(bytes, preamble, bytes.Length - preamble);
        return Sequences.CreateXdmAtomicSequence(Factory.CreateXdmStringAtomicValue(serialized, XdmAtomicType.String));
    }

    internal string ReadXPathTransformDeliveryFormat(XdmMapValue map)
    {
        XdmSequence? supplied = FindXPathTransformOption(map, "delivery-format");
        string value = supplied == null ? "document" : ReadXPathTransformStringValue(supplied, "delivery-format");
        if (value != "document" && value != "serialized" && value != "raw") throw CreateXPathDynamicLoadingError("delivery-format must be document, serialized, or raw.", XPathErrorCodes.FOXT0002);
        return value;
    }

    internal XsltParameter[] ReadXPathTransformParameters(XdmMapValue map, string optionName, bool isStatic, bool isTunnel)
    {
        XdmSequence? supplied = FindXPathTransformOption(map, optionName);
        if (supplied == null) return [];
        XdmMapValue values = ReadXPathTransformNestedMap(supplied, optionName);
        XdmMapEntry[] entries = values.Entries == null ? [] : values.Entries;
        XsltParameter[] result = new XsltParameter[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            XdmAtomicValue? key = entries[index].Key;
            if (key == null || key.Type != XdmAtomicType.QName || key.NameValue == null || entries[index].Value == null) throw CreateXPathDynamicLoadingError(optionName + " must use QName keys.", XPathErrorCodes.FOXT0002);
            XsltParameter parameter = new XsltParameter();
            parameter.Name = key.NameValue;
            parameter.Value = entries[index].Value;
            parameter.IsStatic = isStatic;
            parameter.IsTunnel = isTunnel;
            result[index] = parameter;
            index = index + 1;
        }

        return result;
    }

    internal void AppendXPathTransformParameters(List<XsltParameter> target, XsltParameter[] supplied)
    {
        int index = 0;
        while (index < supplied.Length)
        {
            target.Add(supplied[index]);
            index = index + 1;
        }
    }

    internal XdmSequence[] ReadXPathTransformFunctionParameters(XdmMapValue map)
    {
        XdmSequence? supplied = FindXPathTransformOption(map, "function-params");
        if (supplied == null) return [];
        if (supplied.Count != 1) throw CreateXPathDynamicLoadingError("function-params must be one array.", XPathErrorCodes.FOXT0002);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Array == null) throw CreateXPathDynamicLoadingError("function-params must be one array.", XPathErrorCodes.FOXT0002);
        XdmArrayValue array = item.Array;
        if (array.Members == null || array.Count != array.Members.Length) throw CreateXPathDynamicLoadingError("function-params contains invalid array storage.", XPathErrorCodes.FOXT0002);
        return (XdmSequence[])array.Members.Clone();
    }

    internal XdmMapValue ReadXPathTransformMap(XdmSequence supplied)
    {
        RequireXPathDynamicLoadingMap(supplied, "The transformation options must be one map.");
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        return item.Map == null ? throw CreateXPathDynamicLoadingTypeError("The transformation options must be one map.") : item.Map;
    }

    internal XdmMapValue ReadXPathTransformNestedMap(XdmSequence supplied, string optionName)
    {
        if (supplied.Count != 1) throw CreateXPathDynamicLoadingError(optionName + " must be one map.", XPathErrorCodes.FOXT0002);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Map == null) throw CreateXPathDynamicLoadingError(optionName + " must be one map.", XPathErrorCodes.FOXT0002);
        return item.Map;
    }

    internal XdmSequence? FindXPathTransformOption(XdmMapValue map, string name)
    {
        Maps.ValidateXdmMapStorage(map);
        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        int index = 0;
        while (index < entries.Length)
        {
            XdmAtomicValue? key = entries[index].Key;
            if (key == null || entries[index].Value == null || !Maps.IsXdmStringMapKey(key.Type)) throw CreateXPathDynamicLoadingError("Transformation option keys must be strings.", XPathErrorCodes.FOXT0002);
            if (Converter.ReadXdmAtomicStringValue(key) == name) return entries[index].Value;
            index = index + 1;
        }

        return null;
    }

    internal string ReadXPathTransformString(XdmMapValue map, string name)
    {
        XdmSequence? supplied = FindXPathTransformOption(map, name);
        return supplied == null ? throw CreateXPathDynamicLoadingError(name + " is required.", XPathErrorCodes.FOXT0002) : ReadXPathTransformStringValue(supplied, name);
    }

    internal string ReadXPathTransformStringValue(XdmSequence supplied, string name)
    {
        if (supplied.Count != 1) throw CreateXPathDynamicLoadingError(name + " must contain one string.", XPathErrorCodes.FOXT0002);
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        if (item.AtomicValue == null || !Maps.IsXdmStringMapKey(item.AtomicValue.Type)) throw CreateXPathDynamicLoadingError(name + " must contain one string.", XPathErrorCodes.FOXT0002);
        return Converter.ReadXdmAtomicStringValue(item.AtomicValue);
    }

    internal XmlNode ReadXPathTransformNode(XdmMapValue map, string name)
    {
        XdmSequence? supplied = FindXPathTransformOption(map, name);
        if (supplied == null || supplied.Count != 1) throw CreateXPathDynamicLoadingError(name + " must contain one node.", XPathErrorCodes.FOXT0002);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Node == null) throw CreateXPathDynamicLoadingError(name + " must contain one node.", XPathErrorCodes.FOXT0002);
        return item.Node;
    }

    internal XmlName? ReadXPathTransformOptionalQName(XdmMapValue map, string name)
    {
        XdmSequence? supplied = FindXPathTransformOption(map, name);
        if (supplied == null) return null;
        if (supplied.Count != 1) throw CreateXPathDynamicLoadingError(name + " must contain one QName.", XPathErrorCodes.FOXT0002);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.AtomicValue == null || item.AtomicValue.Type != XdmAtomicType.QName || item.AtomicValue.NameValue == null) throw CreateXPathDynamicLoadingError(name + " must contain one QName.", XPathErrorCodes.FOXT0002);
        return item.AtomicValue.NameValue;
    }

    internal bool ReadXPathTransformBoolean(XdmMapValue map, string name, bool defaultValue)
    {
        XdmSequence? supplied = FindXPathTransformOption(map, name);
        if (supplied == null) return defaultValue;
        if (supplied.Count != 1) throw CreateXPathDynamicLoadingError(name + " must contain one boolean.", XPathErrorCodes.FOXT0002);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.AtomicValue == null || item.AtomicValue.Type != XdmAtomicType.Boolean) throw CreateXPathDynamicLoadingError(name + " must contain one boolean.", XPathErrorCodes.FOXT0002);
        return item.AtomicValue.BooleanValue;
    }

    internal string? ReadXPathTransformOptionalUri(XdmMapValue map, string name, string? basis)
    {
        XdmSequence? supplied = FindXPathTransformOption(map, name);
        return supplied == null ? null : ResolveXPathTransformUri(ReadXPathTransformStringValue(supplied, name), basis, name);
    }

    internal string ResolveXPathTransformUri(string reference, string? basis, string name)
    {
        Task<string> pending = Task.Run(() => UriTools.HasUriScheme(reference) ? UriTools.CanonicalAbsoluteUri(UriTools.CreateAbsolute(reference)) : basis == null ? throw new UriFormatException("A relative URI has no base URI.") : UriTools.CanonicalAbsoluteUri(UriTools.ResolveReference(UriTools.CreateAbsolute(basis), reference)));
        ((IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        if (pending.Status != TaskStatus.RanToCompletion) throw CreateXPathDynamicLoadingError(name + " is not a resolvable URI reference.", XPathErrorCodes.FOXT0002);
        return pending.Result;
    }

    internal void ValidateXPathTransformInvocationOptions(XdmMapValue map)
    {
        bool function = FindXPathTransformOption(map, "initial-function") != null;
        bool template = FindXPathTransformOption(map, "initial-template") != null;
        bool source = FindXPathTransformOption(map, "source-node") != null;
        bool selection = FindXPathTransformOption(map, "initial-match-selection") != null;
        bool mode = FindXPathTransformOption(map, "initial-mode") != null;
        bool functionParameters = FindXPathTransformOption(map, "function-params") != null;
        bool templateParameters = FindXPathTransformOption(map, "template-params") != null;
        bool tunnelParameters = FindXPathTransformOption(map, "tunnel-params") != null;
        if (source && selection) throw CreateXPathDynamicLoadingError("source-node and initial-match-selection are mutually exclusive.", XPathErrorCodes.FOXT0002);
        if (function && (template || source || selection || mode || templateParameters || tunnelParameters)) throw CreateXPathDynamicLoadingError("initial-function cannot be combined with template invocation options.", XPathErrorCodes.FOXT0002);
        if (template && (selection || mode)) throw CreateXPathDynamicLoadingError("initial-template cannot be combined with initial-match-selection or initial-mode.", XPathErrorCodes.FOXT0002);
        if (!function && functionParameters) throw CreateXPathDynamicLoadingError("function-params requires initial-function.", XPathErrorCodes.FOXT0002);
        if (!function && !template && !source && !selection && mode) throw CreateXPathDynamicLoadingError("initial-mode requires an apply-templates invocation.", XPathErrorCodes.FOXT0002);
    }

    internal void ValidateXPathTransformVersion(XdmMapValue map)
    {
        XdmSequence? supplied = FindXPathTransformOption(map, "xslt-version");
        if (supplied == null) return;
        if (supplied.Count != 1) throw CreateXPathDynamicLoadingError("xslt-version must contain one decimal.", XPathErrorCodes.FOXT0002);
        XdmSequence atomized = Converter.AtomizeXdmSequence(supplied);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(atomized, 0);
        if (item.AtomicValue == null) throw CreateXPathDynamicLoadingError("xslt-version must contain one decimal.", XPathErrorCodes.FOXT0002);
        XdmAtomicValue converted = Converter.CastXdmAtomic(item.AtomicValue, XdmAtomicType.Decimal, StaticContext);
        if (converted.DecimalValue > 3M) throw CreateXPathDynamicLoadingError("No processor is available for the requested XSLT version.", XPathErrorCodes.FOXT0001);
    }

    internal void ValidateXPathModuleArguments(XdmSequence[] arguments)
    {
        XdmSequenceType uriType = new XdmSequenceType();
        uriType.ItemKind = XdmItemKind.Atomic;
        uriType.AtomicType = XdmAtomicType.String;
        uriType.Occurrence = XdmOccurrence.ExactlyOne;
        XdmSequence uri = Converter.ApplyXdmFunctionConversion(arguments[0], uriType, StaticContext);
        XdmAtomicValue? value = Sequences.ReadXdmSequenceItem(uri, 0).AtomicValue;
        if (value != null && Converter.ReadXdmAtomicStringValue(value).Length == 0) throw CreateXPathDynamicLoadingError("The module URI cannot be empty.", XPathErrorCodes.FOQM0001);
        if (arguments.Length == 2) RequireXPathDynamicLoadingMap(arguments[1], "The XQuery loading options must be one map.");
    }

    internal void ValidateXPathTransformArguments(XdmSequence[] arguments)
    {
        RequireXPathDynamicLoadingMap(arguments[0], "The transformation options must be one map.");
    }

    internal void RequireXPathDynamicLoadingMap(XdmSequence supplied, string message)
    {
        if (supplied.Count != 1) throw CreateXPathDynamicLoadingTypeError(message);
        XdmSequenceItem item = Sequences.ReadXdmSequenceItem(supplied, 0);
        if (item.Kind != XdmItemKind.Map || item.Map == null) throw CreateXPathDynamicLoadingTypeError(message);
    }

    internal Exception ReadXPathTransformFailure(AggregateException? aggregate)
    {
        Exception result = aggregate == null ? new InvalidOperationException("The transformation failed without an exception.") : aggregate;
        while (result.GetType().FullName == "System.AggregateException")
        {
            AggregateException current = (AggregateException)result;
            if (current.InnerExceptions.Count == 0) break;
            result = current.InnerExceptions[0];
        }

        return result;
    }

    internal XmlDiagnostic? ReadXPathTransformDiagnostic(Exception failure)
    {
        string? typeName = failure.GetType().FullName;
        if (typeName == "xml.XsltStaticException") return ((XsltStaticException)failure).Diagnostic;
        if (typeName == "xml.XsltDynamicException") return ((XsltDynamicException)failure).Diagnostic;
        if (typeName == "xml.XsltTypeException") return ((XsltTypeException)failure).Diagnostic;
        if (typeName == "xml.XsltException") return ((XsltException)failure).Diagnostic;
        return null;
    }

    internal XPathDynamicException CreateXPathDynamicLoadingError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathDynamicException(message, diagnostic, Range.Start);
    }

    internal XPathTypeException CreateXPathDynamicLoadingTypeError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XPathErrorCodes.XPTY0004;
        diagnostic.Category = XmlErrorCategory.XPathType;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = Range;
        return new XPathTypeException(message, diagnostic, Range.Start);
    }
}
