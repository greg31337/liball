namespace xml;

/// <summary>Provides Serialization 3.1 output for XML trees, events, and XDM sequences.</summary>
public class XmlSerializer : IXmlSerializer
{
    /// <summary>Creates an XML serializer.</summary>
    public XmlSerializer()
    {
    }

    /// <summary>Serializes an XML document.</summary>
    public void SerializeXmlDocument(XmlDocument document, XmlOutput output, XmlSerializationOptions options)
    {
        Guard.NotNull(document, "document");
        Guard.NotNull(output, "output");
        Guard.NotNull(options, "options");
        IXmlSerializationMethod? extension = FindXmlSerializationMethod(options);
        if (extension != null)
        {
            new SerializationParameterValidator().ValidateSerializationParameters(options);
            XmlNode? extensionDocumentNode = document.DocumentNode;
            if (extensionDocumentNode == null)
            {
                throw new System.ArgumentException("The XML document has no document node.", "document");
            }

            InvokeXmlSerializationMethod(extension, CreateNodeJsonSerializationSequence(extensionDocumentNode), output, options);
            return;
        }

        options = ResolveExtensionXmlSerializationOptions(options);
        new SerializationParameterValidator().ValidateSerializationParameters(options);
        if (options.Method == XmlSerializationMethod.Adaptive)
        {
            XmlNode? adaptiveDocumentNode = document.DocumentNode;
            if (adaptiveDocumentNode == null)
            {
                throw new System.ArgumentException("The XML document has no document node.", "document");
            }

            WriteAdaptiveSerialization(CreateNodeJsonSerializationSequence(adaptiveDocumentNode), output, options);
            return;
        }

        if (options.Method == XmlSerializationMethod.Json)
        {
            XmlNode? documentNode = document.DocumentNode;
            if (documentNode == null)
            {
                throw new System.ArgumentException("The XML document has no document node.", "document");
            }

            WriteJsonSerialization(CreateNodeJsonSerializationSequence(documentNode), output, options);
            return;
        }

        SequenceNormalizer normalizer = new SequenceNormalizer();
        XmlDocument normalized = normalizer.NormalizeXmlSerializationDocument(document, options.ItemSeparator);
        normalized.Version = document.Version;
        normalized.Standalone = document.Standalone;
        normalized.DocumentType = document.DocumentType;
        WriteNormalizedXmlSerialization(normalized, output, options);
    }

    /// <summary>Serializes an XML node.</summary>
    public void SerializeXmlNode(XmlNode node, XmlOutput output, XmlSerializationOptions options)
    {
        Guard.NotNull(node, "node");
        Guard.NotNull(output, "output");
        Guard.NotNull(options, "options");
        IXmlSerializationMethod? extension = FindXmlSerializationMethod(options);
        if (extension != null)
        {
            new SerializationParameterValidator().ValidateSerializationParameters(options);
            InvokeXmlSerializationMethod(extension, CreateNodeJsonSerializationSequence(node), output, options);
            return;
        }

        options = ResolveExtensionXmlSerializationOptions(options);
        new SerializationParameterValidator().ValidateSerializationParameters(options);
        if (options.Method == XmlSerializationMethod.Adaptive)
        {
            WriteAdaptiveSerialization(CreateNodeJsonSerializationSequence(node), output, options);
            return;
        }

        if (options.Method == XmlSerializationMethod.Json)
        {
            WriteJsonSerialization(CreateNodeJsonSerializationSequence(node), output, options);
            return;
        }

        SequenceNormalizer normalizer = new SequenceNormalizer();
        XmlDocument normalized = normalizer.NormalizeXmlSerializationNode(node, options.ItemSeparator);
        if (node.OwnerDocument != null)
        {
            normalized.Version = node.OwnerDocument.Version;
            normalized.Standalone = node.OwnerDocument.Standalone;
            normalized.DocumentType = node.Kind == XmlNodeKind.Document ? node.OwnerDocument.DocumentType : null;
        }

        WriteNormalizedXmlSerialization(normalized, output, options);
    }

    /// <summary>Serializes an XDM sequence.</summary>
    public void SerializeXdmSequence(XdmSequence sequence, XmlOutput output, XmlSerializationOptions options)
    {
        Guard.NotNull(sequence, "sequence");
        Guard.NotNull(output, "output");
        Guard.NotNull(options, "options");
        IXmlSerializationMethod? extension = FindXmlSerializationMethod(options);
        if (extension != null)
        {
            new SerializationParameterValidator().ValidateSerializationParameters(options);
            InvokeXmlSerializationMethod(extension, sequence, output, options);
            return;
        }

        options = ResolveExtensionXmlSerializationOptions(options);
        new SerializationParameterValidator().ValidateSerializationParameters(options);
        if (options.Method == XmlSerializationMethod.Adaptive)
        {
            WriteAdaptiveSerialization(sequence, output, options);
            return;
        }

        if (options.Method == XmlSerializationMethod.Json)
        {
            WriteJsonSerialization(sequence, output, options);
            return;
        }

        SequenceNormalizer normalizer = new SequenceNormalizer();
        XmlDocument normalized = normalizer.NormalizeXdmSerializationSequence(sequence, options.ItemSeparator);
        WriteNormalizedXmlSerialization(normalized, output, options);
    }

    /// <summary>Serializes normalized XML events.</summary>
    public void SerializeXmlEvents(XmlEventBatch events, XmlOutput output, XmlSerializationOptions options)
    {
        Guard.NotNull(events, "events");
        Guard.NotNull(output, "output");
        Guard.NotNull(options, "options");
        IXmlSerializationMethod? extension = FindXmlSerializationMethod(options);
        if (extension != null)
        {
            new SerializationParameterValidator().ValidateSerializationParameters(options);
            XmlEvent[] extensionSource = events.Events == null ? [] : events.Events;
            if (events.Offset < 0 || events.Count < 0 || events.Offset > extensionSource.Length - events.Count)
            {
                throw new System.ArgumentOutOfRangeException("events");
            }

            XmlEvent[] extensionEvents = new XmlEvent[events.Count];
            System.Array.Copy(extensionSource, events.Offset, extensionEvents, 0, events.Count);
            XmlDocument extensionMetadata = new XmlDocument();
            XmlDocument extensionDocument = new TreeBuilder(extensionMetadata, false).BuildXmlEventTree(extensionEvents);
            XmlNode? extensionDocumentNode = extensionDocument.DocumentNode;
            if (extensionDocumentNode == null)
            {
                throw new System.ArgumentException("The XML event tree has no document node.", "events");
            }

            InvokeXmlSerializationMethod(extension, CreateNodeJsonSerializationSequence(extensionDocumentNode), output, options);
            return;
        }

        options = ResolveExtensionXmlSerializationOptions(options);
        new SerializationParameterValidator().ValidateSerializationParameters(options);
        XmlEvent[] source = events.Events == null ? [] : events.Events;
        if (events.Offset < 0 || events.Count < 0 || events.Offset > source.Length - events.Count)
        {
            throw new System.ArgumentOutOfRangeException("events");
        }

        if (CanStreamXmlSerializationEvents(events, options))
        {
            XmlSerializationEventSink sink = new XmlSerializationEventSink(output, options);
            XmlEventPipeline pipeline = new XmlEventPipeline([], sink);
            pipeline.ProcessXmlEventBatch(events);
            pipeline.CompleteXmlEventPipeline();
            return;
        }

        XmlEvent[] selected = new XmlEvent[events.Count];
        System.Array.Copy(source, events.Offset, selected, 0, events.Count);
        XmlDocument metadata = new XmlDocument();
        metadata.Version = options.Version != null && string.Equals(options.Version, "1.1", System.StringComparison.Ordinal) ? XmlVersion.Xml11 : XmlVersion.Xml10;
        XmlDocument document = new TreeBuilder(metadata, false).BuildXmlEventTree(selected);
        if (options.Method == XmlSerializationMethod.Adaptive)
        {
            XmlNode? adaptiveDocumentNode = document.DocumentNode;
            if (adaptiveDocumentNode == null)
            {
                throw new System.ArgumentException("The XML event tree has no document node.", "events");
            }

            WriteAdaptiveSerialization(CreateNodeJsonSerializationSequence(adaptiveDocumentNode), output, options);
            return;
        }

        if (options.Method == XmlSerializationMethod.Json)
        {
            XmlNode? documentNode = document.DocumentNode;
            if (documentNode == null)
            {
                throw new System.ArgumentException("The XML event tree has no document node.", "events");
            }

            WriteJsonSerialization(CreateNodeJsonSerializationSequence(documentNode), output, options);
            return;
        }

        SequenceNormalizer normalizer = new SequenceNormalizer();
        XmlDocument normalized = normalizer.NormalizeXmlSerializationDocument(document, options.ItemSeparator);
        normalized.Version = document.Version;
        normalized.DocumentType = document.DocumentType;
        WriteNormalizedXmlSerialization(normalized, output, options);
    }

    internal bool CanStreamXmlSerializationEvents(XmlEventBatch events, XmlSerializationOptions options)
    {
        if (options.Method != XmlSerializationMethod.Xml && options.Method != XmlSerializationMethod.Text)
        {
            return false;
        }

        if (options.Indent || options.DoctypePublic != null || options.DoctypeSystem != null)
        {
            return false;
        }

        if (options.NormalizationForm != null && !string.Equals(options.NormalizationForm, "none", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        XmlEvent[] source = events.Events == null ? [] : events.Events;
        int end = events.Offset + events.Count;
        int index = events.Offset;
        while (index < end)
        {
            XmlEventKind kind = source[index].Kind;
            if (kind == XmlEventKind.StartDtd || kind == XmlEventKind.EndDtd || kind == XmlEventKind.ElementDeclaration || kind == XmlEventKind.AttributeDeclaration || kind == XmlEventKind.EntityDeclaration || kind == XmlEventKind.NotationDeclaration)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void WriteNormalizedXmlSerialization(XmlDocument document, XmlOutput output, XmlSerializationOptions options)
    {
        new SerializationParameterValidator().ValidateNormalizedSerializationDocument(document, options);
        XmlOutputWriter outputWriter = new XmlOutputWriter(output, options.Encoding, options.ByteOrderMark, 4096);
        if (output.Kind != XmlOutputKind.TextWriter && (options.Method == XmlSerializationMethod.Xml || options.Method == XmlSerializationMethod.Xhtml) && string.Equals(outputWriter.InspectXmlOutputEncodingName(), "utf-16", System.StringComparison.OrdinalIgnoreCase))
        {
            outputWriter.EmitByteOrderMark = true;
        }

        string value;
        if (options.Method == XmlSerializationMethod.Text)
        {
            TextSerializationWriter textSerializer = new TextSerializationWriter(options);
            value = textSerializer.SerializeTextDocument(document);
        }
        else
        {
            XmlSerializationWriter serializer = new XmlSerializationWriter(options, document.Version, outputWriter.InspectXmlOutputEncodingName());
            value = serializer.SerializeXmlDocumentTree(document);
        }

        outputWriter.WriteXmlOutputText(value);
        outputWriter.CompleteXmlOutput();
    }

    internal XdmSequence CreateNodeJsonSerializationSequence(XmlNode node)
    {
        return new XdmSequenceOperations().CreateXdmNodeSequence(node);
    }

    internal void WriteJsonSerialization(XdmSequence sequence, XmlOutput output, XmlSerializationOptions options)
    {
        XmlOutputWriter outputWriter = new XmlOutputWriter(output, options.Encoding, options.ByteOrderMark, 4096);
        JsonSerializationWriter serializer = new JsonSerializationWriter(options, outputWriter.InspectXmlOutputEncodingName());
        string value = serializer.SerializeJsonSequence(sequence);
        outputWriter.WriteXmlOutputText(value);
        outputWriter.CompleteXmlOutput();
    }

    internal void WriteAdaptiveSerialization(XdmSequence sequence, XmlOutput output, XmlSerializationOptions options)
    {
        XmlOutputWriter outputWriter = new XmlOutputWriter(output, options.Encoding, options.ByteOrderMark, 4096);
        AdaptiveSerializationWriter serializer = new AdaptiveSerializationWriter(options, outputWriter.InspectXmlOutputEncodingName());
        string value = serializer.SerializeAdaptiveSequence(sequence);
        outputWriter.WriteXmlOutputText(value);
        outputWriter.CompleteXmlOutput();
    }

    internal XmlSerializationOptions ResolveExtensionXmlSerializationOptions(XmlSerializationOptions options)
    {
        if (options.Method != XmlSerializationMethod.Extension)
        {
            return options;
        }

        XmlName? name = options.ExtensionMethodName;
        if (name == null || name.LocalName == null || name.LocalName.Length == 0)
        {
            throw CreateExtensionXmlSerializationError("An extension serialization method requires a name.", XmlErrorCodes.SEPM0004);
        }

        if (name.NamespaceUri != null && name.NamespaceUri.Length > 0)
        {
            throw CreateExtensionXmlSerializationError("No implementation is registered for the extension serialization method.", XmlErrorCodes.SESU0013);
        }

        XmlSerializationMethod method;
        if (string.Equals(name.LocalName, "xml", System.StringComparison.Ordinal))
        {
            method = XmlSerializationMethod.Xml;
        }
        else if (string.Equals(name.LocalName, "xhtml", System.StringComparison.Ordinal))
        {
            method = XmlSerializationMethod.Xhtml;
        }
        else if (string.Equals(name.LocalName, "html", System.StringComparison.Ordinal))
        {
            method = XmlSerializationMethod.Html;
        }
        else if (string.Equals(name.LocalName, "text", System.StringComparison.Ordinal))
        {
            method = XmlSerializationMethod.Text;
        }
        else if (string.Equals(name.LocalName, "json", System.StringComparison.Ordinal))
        {
            method = XmlSerializationMethod.Json;
        }
        else if (string.Equals(name.LocalName, "adaptive", System.StringComparison.Ordinal))
        {
            method = XmlSerializationMethod.Adaptive;
        }
        else
        {
            throw CreateExtensionXmlSerializationError("The unqualified serialization method name is not recognized.", XmlErrorCodes.SEPM0004);
        }

        return CopyResolvedXmlSerializationOptions(options, method);
    }

    internal IXmlSerializationMethod? FindXmlSerializationMethod(XmlSerializationOptions options)
    {
        if (options.Method != XmlSerializationMethod.Extension || options.ExtensionMethodName == null)
        {
            return null;
        }

        XmlExtensionPolicy policy = new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(options.ExtensionPolicy);
        XmlSerializationMethodBinding[] bindings = options.ExtensionMethods == null ? [] : options.ExtensionMethods;
        int index = 0;
        while (index < bindings.Length)
        {
            if (bindings[index] == null || bindings[index].Implementation == null)
            {
                throw new System.ArgumentException("An extension serialization method requires an implementation.", "options");
            }

            XmlName? candidate = bindings[index].Name;
            new XmlExtensionPolicyEvaluator().RequireXmlExtensionName(policy, candidate, "options");
            if (candidate != null && new XmlExtensionPolicyEvaluator().IsXmlExtensionReservedNamespace(candidate.NamespaceUri == null ? "" : candidate.NamespaceUri))
            {
                throw new System.ArgumentException("An extension serialization method cannot use a reserved namespace.", "options");
            }
            int earlier = 0;
            while (earlier < index)
            {
                XmlName? previous = bindings[earlier].Name;
                if (previous != null && candidate != null && previous.LocalName == candidate.LocalName && (previous.NamespaceUri == null ? "" : previous.NamespaceUri) == (candidate.NamespaceUri == null ? "" : candidate.NamespaceUri))
                {
                    throw new System.ArgumentException("Extension serialization method names must be unique.", "options");
                }

                earlier = earlier + 1;
            }

            XmlName requested = options.ExtensionMethodName;
            if (candidate != null && candidate.LocalName == requested.LocalName && (candidate.NamespaceUri == null ? "" : candidate.NamespaceUri) == (requested.NamespaceUri == null ? "" : requested.NamespaceUri))
            {
                return bindings[index].Implementation;
            }

            index = index + 1;
        }

        return null;
    }

    internal void InvokeXmlSerializationMethod(IXmlSerializationMethod implementation, XdmSequence sequence, XmlOutput output, XmlSerializationOptions source)
    {
        XmlSerializationOptions options = CopyResolvedXmlSerializationOptions(source, XmlSerializationMethod.Extension);
        XmlExtensionPolicy policy = options.ExtensionPolicy == null ? new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(null) : options.ExtensionPolicy;
        XmlExtensionRuntimeState state = options.ExtensionState == null ? new XmlExtensionRuntimeState() : options.ExtensionState;
        options.ExtensionState = state;
        if (!policy.AllowReentrancy && state.Depth > 0)
        {
            throw CreateExtensionXmlSerializationError("Reentrant extension serialization is denied by policy.", XmlErrorCodes.SESU0013);
        }

        if (state.InvocationCount >= policy.MaximumInvocations)
        {
            throw CreateExtensionXmlSerializationError("The extension serialization invocation limit was exceeded.", XmlErrorCodes.SESU0013);
        }

        state.InvocationCount = state.InvocationCount + 1;
        state.Depth = state.Depth + 1;
        System.Threading.Tasks.Task pending = System.Threading.Tasks.Task.Run(() => implementation.SerializeXmlSequence(sequence, output, options));
        ((System.IAsyncResult)pending).AsyncWaitHandle.WaitOne();
        state.Depth = state.Depth - 1;
        if (pending.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
        {
            return;
        }

        string detail = new XmlExtensionPolicyEvaluator().ReadXmlExtensionFailureMessage(pending.Exception);
        throw CreateExtensionXmlSerializationError("The extension serialization method failed: " + detail, XmlErrorCodes.SESU0013);
    }

    internal XmlSerializationOptions CopyResolvedXmlSerializationOptions(XmlSerializationOptions source, XmlSerializationMethod method)
    {
        XmlSerializationOptions result = new XmlSerializationOptions();
        result.Method = method;
        result.ExtensionMethodName = source.ExtensionMethodName;
        result.ExtensionMethods = CopyXmlSerializationMethodBindings(source.ExtensionMethods);
        result.ExtensionPolicy = new XmlExtensionPolicyEvaluator().CopyXmlExtensionPolicy(source.ExtensionPolicy);
        result.ExtensionState = source.ExtensionState;
        result.AllowDuplicateNames = source.AllowDuplicateNames;
        result.ByteOrderMark = source.ByteOrderMark;
        result.CDataSectionElements = source.CDataSectionElements;
        result.DoctypePublic = source.DoctypePublic;
        result.DoctypeSystem = source.DoctypeSystem;
        result.Encoding = source.Encoding;
        result.EscapeUriAttributes = source.EscapeUriAttributes;
        result.HtmlVersion = source.HtmlVersion;
        result.IncludeContentType = source.IncludeContentType;
        result.Indent = source.Indent;
        result.ItemSeparator = source.ItemSeparator;
        result.JsonNodeOutputMethod = source.JsonNodeOutputMethod;
        result.MediaType = source.MediaType;
        result.NormalizationForm = source.NormalizationForm;
        result.OmitXmlDeclaration = source.OmitXmlDeclaration;
        result.Standalone = source.Standalone;
        result.SuppressIndentation = source.SuppressIndentation;
        result.UndeclarePrefixes = source.UndeclarePrefixes;
        result.UseCharacterMaps = source.UseCharacterMaps;
        result.Version = source.Version;
        result.InvalidCharacterPolicy = source.InvalidCharacterPolicy;
        result.InvalidCharacterReplacement = source.InvalidCharacterReplacement;
        result.NewLine = source.NewLine;
        return result;
    }

    internal XmlSerializationMethodBinding[]? CopyXmlSerializationMethodBindings(XmlSerializationMethodBinding[]? source)
    {
        if (source == null)
        {
            return null;
        }

        XmlSerializationMethodBinding[] result = new XmlSerializationMethodBinding[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            XmlSerializationMethodBinding item = source[index];
            XmlSerializationMethodBinding copy = new XmlSerializationMethodBinding();
            copy.Name = item.Name == null ? null : new XmlName();
            if (copy.Name != null && item.Name != null)
            {
                copy.Name.Prefix = item.Name.Prefix;
                copy.Name.LocalName = item.Name.LocalName;
                copy.Name.NamespaceUri = item.Name.NamespaceUri;
                copy.Name.QualifiedName = item.Name.QualifiedName;
            }

            copy.Implementation = item.Implementation;
            result[index] = copy;
            index = index + 1;
        }

        return result;
    }

    internal XmlSerializationException CreateExtensionXmlSerializationError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }
}
