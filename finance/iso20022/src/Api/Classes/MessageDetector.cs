using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace iso20022;

/// <summary>Detects standalone, sibling, envelope, file, and configured-wrapper message layouts.</summary>
public class MessageDetector : IMessageDetector
{
    /// <summary>Initializes a message detector.</summary>
    public MessageDetector()
    {
    }

    /// <summary>Detects a message from a byte array.</summary>
    /// <param name="input">The complete encoded input.</param>
    /// <param name="options">The read and wrapper options.</param>
    /// <returns>The detection result and replayable input.</returns>
    public MessageDetectionResult DetectBytes(byte[]? input, MessageReadOptions? options)
    {
        if (input == null)
        {
            return CreateFailure("detection.input_null", "Message input is required.");
        }

        if (options == null)
        {
            return CreateFailure("detection.options_null", "Message read options are required.");
        }

        byte[] preservedBytes = CopyBytes(input);
        MessageDetectionResult result = DetectBufferedBytes(preservedBytes, options);
        result.PreservedBytes = preservedBytes;
        result.ReplayStream = new MemoryStream(preservedBytes, false);
        return result;
    }

    /// <summary>Detects a message from the current position of a stream.</summary>
    /// <param name="input">The input stream.</param>
    /// <param name="options">The read and wrapper options.</param>
    /// <returns>The detection result and original or replacement replay stream.</returns>
    public MessageDetectionResult DetectStream(Stream? input, MessageReadOptions? options)
    {
        if (input == null)
        {
            return CreateFailure("detection.input_null", "Message input is required.");
        }

        if (options == null)
        {
            return CreateFailure("detection.options_null", "Message read options are required.");
        }

        long originalPosition = 0;
        if (input.CanSeek)
        {
            originalPosition = input.Position;
        }

        byte[] preservedBytes = ReadStreamBytes(input, options.Limits);
        if (input.CanSeek)
        {
            input.Position = originalPosition;
        }

        MessageDetectionResult result = DetectBufferedBytes(preservedBytes, options);
        result.InputWasSeekable = input.CanSeek;
        result.OriginalStreamPosition = originalPosition;
        result.PreservedBytes = preservedBytes;
        if (input.CanSeek)
        {
            result.ReplayStream = input;
        }
        else
        {
            result.ReplayStream = new MemoryStream(preservedBytes, false);
        }

        return result;
    }

    /// <summary>Detects a message from text encoded to UTF-8 for replay.</summary>
    /// <param name="input">The XML text.</param>
    /// <param name="options">The read and wrapper options.</param>
    /// <returns>The detection result and replayable UTF-8 input.</returns>
    public MessageDetectionResult DetectText(string? input, MessageReadOptions? options)
    {
        if (input == null)
        {
            return CreateFailure("detection.input_null", "Message input is required.");
        }

        if (options == null)
        {
            return CreateFailure("detection.options_null", "Message read options are required.");
        }

        byte[] preservedBytes = Encoding.UTF8.GetBytes(input);
        MessageDetectionResult result = DetectBufferedText(input, options);
        result.PreservedBytes = preservedBytes;
        result.ReplayStream = new MemoryStream(preservedBytes, false);
        return result;
    }

    internal static MessageDetectionResult DetectBufferedBytes(byte[] bytes, MessageReadOptions options)
    {
        GenericXmlNodeReader reader = new GenericXmlNodeReader(options);
        DynamicXmlNode document = reader.ReadDocumentFromBytes(bytes, null);
        MessageDetectionResult result = DetectDocument(document, options);
        MessageRepairFactory.PrependCopies(result.Repairs, reader.Repairs, options.Limits);
        return result;
    }

    internal static MessageDetectionResult DetectBufferedText(string text, MessageReadOptions options)
    {
        GenericXmlNodeReader reader = new GenericXmlNodeReader(options);
        DynamicXmlNode document = reader.ReadDocumentFromText(text, null);
        MessageDetectionResult result = DetectDocument(document, options);
        MessageRepairFactory.PrependCopies(result.Repairs, reader.Repairs, options.Limits);
        return result;
    }

    internal static MessageDetectionResult DetectDocument(DynamicXmlNode document, MessageReadOptions options)
    {
        List<DynamicXmlNode> roots = GetElementChildren(document);
        if (roots.Count == 0)
        {
            return CreateFailure("detection.no_root", "The input does not contain an XML element root.");
        }

        if (roots.Count == 2 && IsSiblingPair(roots[0], roots[1]))
        {
            MessageDetectionResult siblingResult = DetectSiblingLayout(roots[0], roots[1], options);
            if (siblingResult.Success && options.NamespaceMode == NamespaceMode.Lenient)
            {
                MessageRepair siblingRepair = MessageRepairFactory.Create(
                    "xml.sibling_roots_wrapped",
                    "The recognized AppHdr and Document sibling roots were interpreted as one logical message fragment.",
                    roots[0].Name.LocalName + "," + roots[1].Name.LocalName,
                    "MessageFragment",
                    roots[0]);
                AddMessageRepair(siblingResult, siblingRepair, options.Limits);
            }

            return siblingResult;
        }

        if (roots.Count != 1)
        {
            return CreateFailure("detection.root_count", "The input contains an unsupported number or order of top-level elements.");
        }

        return DetectSingleRoot(roots[0], options);
    }

    internal static MessageDetectionResult DetectSingleRoot(DynamicXmlNode root, MessageReadOptions options)
    {
        NamespaceService service = new NamespaceService();
        NamespaceServiceResult namespaceResult = service.ParseNamespace(root.Name.NamespaceUri, options.NamespaceMode);
        bool customWrapper = IsConfiguredWrapper(root.Name, options.CustomWrapperNames);
        if (namespaceResult.Success && namespaceResult.Descriptor != null)
        {
            NamespaceDescriptor descriptor = namespaceResult.Descriptor;
            if (root.Name.LocalName == "Document" && descriptor.Kind == NamespaceKind.Message)
            {
                MessageDetectionResult result = CreateSuccess(EnvelopeKind.StandaloneDocument, root.Name);
                AddNamespaceRepairs(result, namespaceResult.Repairs, root, options.Limits);
                AddPayloadIdentity(result, descriptor);
                SetPrimaryIdentities(result);
                return result;
            }

            if (root.Name.LocalName == "AppHdr" && descriptor.Kind == NamespaceKind.Header)
            {
                MessageDetectionResult result = CreateSuccess(EnvelopeKind.StandaloneHeader, root.Name);
                result.HeaderNamespace = descriptor;
                AddNamespaceRepairs(result, namespaceResult.Repairs, root, options.Limits);
                CollectDeclaredIdentifiers(root, result);
                SetPrimaryIdentities(result);
                ValidateIdentifierCoherence(result);
                return result;
            }

            if (descriptor.Kind == NamespaceKind.Envelope)
            {
                return DetectStandardEnvelope(root, descriptor, namespaceResult.Repairs, options);
            }
        }

        if (customWrapper)
        {
            MessageDetectionResult result = CreateSuccess(EnvelopeKind.CustomWrapper, root.Name);
            result.WrapperName = XmlPathTracker.CopyName(root.Name);
            CollectContainedIdentities(root, result, options);
            if (result.ErrorCode.Length > 0)
            {
                result.Success = false;
                return result;
            }

            SetPrimaryIdentities(result);
            if (result.PayloadIdentifiers.Count == 0 && result.HeaderNamespace == null)
            {
                SetFailure(result, "detection.wrapper_empty", "The configured wrapper does not contain a detectable message or header.");
            }

            ValidateIdentifierCoherence(result);
            return result;
        }

        if (options.NamespaceMode == NamespaceMode.Lenient)
        {
            QualifiedNameData? localWrapper = FindConfiguredWrapperByLocalName(root.Name, options.CustomWrapperNames);
            MessageDetectionResult inferredWrapper = CreateSuccess(EnvelopeKind.CustomWrapper, root.Name);
            inferredWrapper.WrapperName = XmlPathTracker.CopyName(root.Name);
            CollectContainedIdentities(root, inferredWrapper, options);
            if (inferredWrapper.ErrorCode.Length == 0 && (inferredWrapper.PayloadIdentifiers.Count > 0 || inferredWrapper.HeaderNamespace != null))
            {
                SetPrimaryIdentities(inferredWrapper);
                ValidateIdentifierCoherence(inferredWrapper);
                if (inferredWrapper.Success)
                {
                    MessageRepair wrapperRepair;
                    if (localWrapper != null)
                    {
                        wrapperRepair = MessageRepairFactory.Create(
                            "wrapper.namespace_repaired",
                            "A configured wrapper local name was accepted with its source namespace and recorded as a lenient repair.",
                            root.Name.NamespaceUri,
                            localWrapper.NamespaceUri,
                            root);
                    }
                    else
                    {
                        wrapperRepair = MessageRepairFactory.Create(
                            "wrapper.inferred",
                            "An unconfigured outer element containing a detectable ISO 20022 message was interpreted as a wrapper.",
                            root.Name.NamespaceUri + "#" + root.Name.LocalName,
                            "CustomWrapper",
                            root);
                    }

                    AddMessageRepair(inferredWrapper, wrapperRepair, options.Limits);
                }

                return inferredWrapper;
            }
        }

        if (!namespaceResult.Success)
        {
            return CreateFailure("detection.namespace", namespaceResult.ErrorMessage);
        }

        return CreateFailure("detection.root", "The XML root is not a supported ISO 20022 message, header, or envelope.");
    }

    internal static MessageDetectionResult DetectSiblingLayout(DynamicXmlNode first, DynamicXmlNode second, MessageReadOptions options)
    {
        DynamicXmlNode header = first.Name.LocalName == "AppHdr" ? first : second;
        DynamicXmlNode payload = first.Name.LocalName == "Document" ? first : second;
        NamespaceService service = new NamespaceService();
        NamespaceServiceResult headerResult = service.ParseNamespace(header.Name.NamespaceUri, options.NamespaceMode);
        NamespaceServiceResult payloadResult = service.ParseNamespace(payload.Name.NamespaceUri, options.NamespaceMode);
        if (!headerResult.Success)
        {
            return CreateFailure("detection.header_namespace", "The sibling AppHdr does not use a recognized header namespace.");
        }

        NamespaceDescriptor headerDescriptor = RequireNamespaceDescriptor(headerResult);
        if (headerDescriptor.Kind != NamespaceKind.Header)
        {
            return CreateFailure("detection.header_namespace", "The sibling AppHdr does not use a recognized header namespace.");
        }

        if (!payloadResult.Success)
        {
            return CreateFailure("detection.message_namespace", "The sibling Document does not use a recognized message namespace.");
        }

        NamespaceDescriptor payloadDescriptor = RequireNamespaceDescriptor(payloadResult);
        if (payloadDescriptor.Kind != NamespaceKind.Message)
        {
            return CreateFailure("detection.message_namespace", "The sibling Document does not use a recognized message namespace.");
        }

        MessageDetectionResult result = CreateSuccess(EnvelopeKind.SiblingHeaderAndDocument, first.Name);
        result.HeaderPrecedesPayload = first.Name.LocalName == "AppHdr";
        result.HeaderNamespace = headerDescriptor;
        AddNamespaceRepairs(result, headerResult.Repairs, header, options.Limits);
        AddNamespaceRepairs(result, payloadResult.Repairs, payload, options.Limits);
        AddPayloadIdentity(result, payloadDescriptor);
        CollectDeclaredIdentifiers(header, result);
        SetPrimaryIdentities(result);
        ValidateIdentifierCoherence(result);
        return result;
    }

    internal static MessageDetectionResult DetectStandardEnvelope(DynamicXmlNode root, NamespaceDescriptor descriptor, List<NamespaceRepair> repairs, MessageReadOptions options)
    {
        EnvelopeKind layout = EnvelopeKind.Unknown;
        if (descriptor.MessageIdentifier != null && descriptor.MessageIdentifier.MessageFunctionality == "001" && IsBusinessMessageEnvelopeName(root.Name.LocalName))
        {
            layout = EnvelopeKind.BusinessMessageEnvelope;
        }
        else if (descriptor.MessageIdentifier != null && descriptor.MessageIdentifier.MessageFunctionality == "002" && IsBusinessFileEnvelopeName(root.Name.LocalName))
        {
            layout = EnvelopeKind.BusinessFileEnvelope;
        }

        if (layout == EnvelopeKind.Unknown)
        {
            return CreateFailure("detection.envelope_conflict", "The envelope root name conflicts with its namespace identifier.");
        }

        MessageDetectionResult result = CreateSuccess(layout, root.Name);
        result.EnvelopeNamespace = descriptor;
        AddNamespaceRepairs(result, repairs, root, options.Limits);
        CollectContainedIdentities(root, result, options);
        if (result.ErrorCode.Length > 0)
        {
            result.Success = false;
            return result;
        }

        SetPrimaryIdentities(result);
        if (layout == EnvelopeKind.BusinessMessageEnvelope && result.PayloadIdentifiers.Count != 1)
        {
            SetFailure(result, "detection.envelope_payload_count", "A business-message envelope must contain exactly one detectable Document payload.");
            return result;
        }

        ValidateIdentifierCoherence(result);
        return result;
    }

    internal static void CollectContainedIdentities(DynamicXmlNode node, MessageDetectionResult result, MessageReadOptions options)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            DynamicXmlNode child = node.Children[index];
            bool payloadRoot = false;
            if (child.Kind == XmlNodeKind.Element)
            {
                if (child.Name.LocalName == "Document")
                {
                    NamespaceServiceResult namespaceResult = new NamespaceService().ParseNamespace(child.Name.NamespaceUri, options.NamespaceMode);
                    if (!namespaceResult.Success)
                    {
                        SetFailure(result, "detection.message_namespace", "A contained Document does not use a recognized message namespace.");
                        return;
                    }

                    NamespaceDescriptor descriptor = RequireNamespaceDescriptor(namespaceResult);
                    if (descriptor.Kind != NamespaceKind.Message)
                    {
                        SetFailure(result, "detection.message_namespace", "A contained Document does not use a recognized message namespace.");
                        return;
                    }

                    AddNamespaceRepairs(result, namespaceResult.Repairs, child, options.Limits);
                    AddPayloadIdentity(result, descriptor);
                    payloadRoot = true;
                }
                else if (child.Name.LocalName == "AppHdr")
                {
                    NamespaceServiceResult namespaceResult = new NamespaceService().ParseNamespace(child.Name.NamespaceUri, options.NamespaceMode);
                    if (!namespaceResult.Success)
                    {
                        SetFailure(result, "detection.header_namespace", "A contained AppHdr does not use a recognized header namespace.");
                        return;
                    }

                    NamespaceDescriptor descriptor = RequireNamespaceDescriptor(namespaceResult);
                    if (descriptor.Kind != NamespaceKind.Header)
                    {
                        SetFailure(result, "detection.header_namespace", "A contained AppHdr does not use a recognized header namespace.");
                        return;
                    }

                    if (result.HeaderNamespace == null)
                    {
                        result.HeaderNamespace = descriptor;
                    }

                    AddNamespaceRepairs(result, namespaceResult.Repairs, child, options.Limits);
                    CollectDeclaredIdentifiers(child, result);
                }

                if (!payloadRoot)
                {
                    CollectContainedIdentities(child, result, options);
                }
                if (result.ErrorCode.Length > 0)
                {
                    return;
                }
            }

            index++;
        }
    }

    internal static void CollectDeclaredIdentifiers(DynamicXmlNode node, MessageDetectionResult result)
    {
        if (node.Kind == XmlNodeKind.Element && node.Name.LocalName == "MsgDefIdr")
        {
            string value = NamespaceService.TrimXmlWhitespace(CollectDirectText(node));
            MessageIdentifier? identifier = MessageIdentifierLexical.ParseMessageIdentifier(value.AsSpan());
            if (identifier != null)
            {
                result.DeclaredMessageIdentifiers.Add(identifier);
            }
            else
            {
                SetFailure(result, "detection.declared_identifier", "A header MsgDefIdr does not contain a valid message identifier.");
                return;
            }
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            CollectDeclaredIdentifiers(node.Children[index], result);
            index++;
        }
    }

    internal static string CollectDirectText(DynamicXmlNode node)
    {
        StringBuilder text = new StringBuilder();
        int index = 0;
        while (index < node.Children.Count)
        {
            DynamicXmlNode child = node.Children[index];
            if (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData)
            {
                text.Append(child.Value);
            }

            index++;
        }

        return text.ToString();
    }

    internal static void ValidateIdentifierCoherence(MessageDetectionResult result)
    {
        if (result.ErrorCode.Length > 0)
        {
            return;
        }

        if (result.PayloadIdentifiers.Count == 0 && result.DeclaredMessageIdentifiers.Count > 1)
        {
            SetFailure(result, "detection.declared_identifier_count", "A standalone header contains more than one declared message identifier.");
            return;
        }

        if (result.DeclaredMessageIdentifiers.Count > 0 && result.PayloadIdentifiers.Count > 0 && result.DeclaredMessageIdentifiers.Count != result.PayloadIdentifiers.Count)
        {
            SetFailure(result, "detection.identifier_count", "The number of header-declared identifiers does not match the number of payload namespaces.");
            return;
        }

        if (result.DeclaredMessageIdentifiers.Count == result.PayloadIdentifiers.Count && result.PayloadIdentifiers.Count > 0)
        {
            int index = 0;
            while (index < result.PayloadIdentifiers.Count)
            {
                if (result.DeclaredMessageIdentifiers[index].Value != result.PayloadIdentifiers[index].Value)
                {
                    SetFailure(result, "detection.identifier_conflict", "A header-declared message identifier conflicts with its payload namespace.");
                    return;
                }

                index++;
            }
        }
    }

    internal static void SetPrimaryIdentities(MessageDetectionResult result)
    {
        if (result.PayloadIdentifiers.Count > 0)
        {
            result.MessageIdentifier = result.PayloadIdentifiers[0];
            NamespaceServiceResult namespaceResult = new NamespaceService().BuildNamespaceFromIdentifier(result.MessageIdentifier);
            result.MessageNamespace = namespaceResult.Descriptor;
        }

        if (result.DeclaredMessageIdentifiers.Count > 0)
        {
            result.DeclaredMessageIdentifier = result.DeclaredMessageIdentifiers[0];
        }
    }

    internal static void AddPayloadIdentity(MessageDetectionResult result, NamespaceDescriptor descriptor)
    {
        if (descriptor.MessageIdentifier != null)
        {
            result.PayloadIdentifiers.Add(descriptor.MessageIdentifier);
            if (result.MessageNamespace == null)
            {
                result.MessageNamespace = descriptor;
            }
        }
    }

    internal static NamespaceDescriptor RequireNamespaceDescriptor(
        NamespaceServiceResult result)
    {
        if (result.Descriptor == null)
        {
            throw new InvalidOperationException(
                "A successful namespace result must contain a descriptor.");
        }

        return result.Descriptor;
    }

    internal static void AddNamespaceRepairs(MessageDetectionResult result, List<NamespaceRepair> repairs, DynamicXmlNode node, ResourceLimits limits)
    {
        int index = 0;
        while (index < repairs.Count)
        {
            NamespaceRepair source = repairs[index];
            NamespaceRepair copy = new NamespaceRepair();
            copy.Kind = source.Kind;
            copy.OriginalValue = source.OriginalValue;
            copy.NormalizedValue = source.NormalizedValue;
            result.NamespaceRepairs.Add(copy);
            AddMessageRepair(result, MessageRepairFactory.CreateNamespace(source, node), limits);
            index++;
        }
    }

    internal static void AddMessageRepair(MessageDetectionResult result, MessageRepair repair, ResourceLimits limits)
    {
        if (result.Repairs.Count >= limits.MaxDiagnosticCount)
        {
            throw new XmlException("The repair diagnostic collection exceeds MaxDiagnosticCount.");
        }

        result.Repairs.Add(repair);
    }

    internal static bool IsSiblingPair(DynamicXmlNode first, DynamicXmlNode second)
    {
        return (first.Name.LocalName == "AppHdr" && second.Name.LocalName == "Document") ||
            (first.Name.LocalName == "Document" && second.Name.LocalName == "AppHdr");
    }

    internal static bool IsConfiguredWrapper(QualifiedNameData rootName, List<QualifiedNameData> configuredNames)
    {
        int index = 0;
        while (index < configuredNames.Count)
        {
            QualifiedNameData configuredName = configuredNames[index];
            if (configuredName.NamespaceUri == rootName.NamespaceUri && configuredName.LocalName == rootName.LocalName)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static QualifiedNameData? FindConfiguredWrapperByLocalName(QualifiedNameData rootName, List<QualifiedNameData> configuredNames)
    {
        int index = 0;
        while (index < configuredNames.Count)
        {
            QualifiedNameData configuredName = configuredNames[index];
            if (configuredName.LocalName == rootName.LocalName && configuredName.NamespaceUri != rootName.NamespaceUri)
            {
                return configuredName;
            }

            index++;
        }

        return null;
    }

    internal static bool IsBusinessMessageEnvelopeName(string localName)
    {
        return localName == "BizMsgEnvlp" || localName == "BusinessMessageEnvelope";
    }

    internal static bool IsBusinessFileEnvelopeName(string localName)
    {
        return localName == "BizFileEnvlp" || localName == "BusinessFileEnvelope";
    }

    internal static List<DynamicXmlNode> GetElementChildren(DynamicXmlNode document)
    {
        List<DynamicXmlNode> elements = new List<DynamicXmlNode>();
        int index = 0;
        while (index < document.Children.Count)
        {
            if (document.Children[index].Kind == XmlNodeKind.Element)
            {
                elements.Add(document.Children[index]);
            }

            index++;
        }

        return elements;
    }

    internal static MessageDetectionResult CreateSuccess(EnvelopeKind layout, QualifiedNameData rootName)
    {
        MessageDetectionResult result = new MessageDetectionResult();
        result.Success = true;
        result.Layout = layout;
        result.RootName = XmlPathTracker.CopyName(rootName);
        return result;
    }

    internal static MessageDetectionResult CreateFailure(string code, string message)
    {
        MessageDetectionResult result = new MessageDetectionResult();
        SetFailure(result, code, message);
        return result;
    }

    internal static void SetFailure(MessageDetectionResult result, string code, string message)
    {
        result.Success = false;
        result.ErrorCode = code;
        result.ErrorMessage = message;
    }

    internal static byte[] ReadStreamBytes(Stream input, ResourceLimits limits)
    {
        ResourceLimitGuard guard = new ResourceLimitGuard(limits);
        byte[] buffer = new byte[8192];
        using (MemoryStream output = new MemoryStream())
        {
            int readCount = input.Read(buffer, 0, buffer.Length);
            while (readCount > 0)
            {
                guard.EnsureInputByteCount(output.Length + readCount);
                output.Write(buffer, 0, readCount);
                readCount = input.Read(buffer, 0, buffer.Length);
            }

            return output.ToArray();
        }
    }

    internal static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        Array.Copy(source, copy, source.Length);
        return copy;
    }
}
