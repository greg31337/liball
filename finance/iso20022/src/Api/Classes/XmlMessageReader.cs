using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace iso20022;

/// <summary>Reads generic, detected, exact-version, header-only, and metadata-only XML messages.</summary>
public class XmlMessageReader : IXmlMessageReader
{
    /// <summary>Initializes an XML message reader.</summary>
    public XmlMessageReader()
    {
    }

    /// <summary>Reads generic XML from bytes without requiring a known message identity.</summary>
    /// <param name="input">The encoded XML input.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadGenericBytes(byte[]? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromBytes(input, null);
        MessageReadResult result = BuildGenericResult(tree, input.LongLength, options);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Reads generic XML from a stream without requiring a known message identity.</summary>
    /// <param name="input">The input stream.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadGenericStream(Stream? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        byte[] bytes = MessageDetector.ReadStreamBytes(input, options.Limits);
        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromBytes(bytes, null);
        MessageReadResult result = BuildGenericResult(tree, bytes.LongLength, options);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Reads generic XML from text without requiring a known message identity.</summary>
    /// <param name="input">The XML text.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadGenericText(string? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromText(input, null);
        MessageReadResult result = BuildGenericResult(tree, Encoding.UTF8.GetByteCount(input), options);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Detects and reads an ISO 20022 message from bytes.</summary>
    /// <param name="input">The encoded XML input.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadAutoBytes(byte[]? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromBytes(input, null);
        MessageReadResult result = BuildDetectedResult(tree, input.LongLength, options, true, true, true);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Detects and reads an ISO 20022 message from a stream.</summary>
    /// <param name="input">The input stream.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadAutoStream(Stream? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        byte[] bytes = MessageDetector.ReadStreamBytes(input, options.Limits);
        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromBytes(bytes, null);
        MessageReadResult result = BuildDetectedResult(tree, bytes.LongLength, options, true, true, true);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Detects and reads an ISO 20022 message from text.</summary>
    /// <param name="input">The XML text.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadAutoText(string? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromText(input, null);
        MessageReadResult result = BuildDetectedResult(tree, Encoding.UTF8.GetByteCount(input), options, true, true, true);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Reads bytes only when the detected payload has the exact requested identifier.</summary>
    /// <param name="input">The encoded XML input.</param><param name="identifier">The required identifier.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadExactBytes(byte[]? input, MessageIdentifier? identifier, MessageReadOptions? options)
    {
        if (input == null || options == null || identifier == null)
        {
            return CreateExactInputFailure(input == null, options == null, identifier == null);
        }

        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromBytes(input, null);
        MessageReadResult result = BuildExactResult(tree, input.LongLength, identifier, options);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Reads a stream only when the detected payload has the exact requested identifier.</summary>
    /// <param name="input">The input stream.</param><param name="identifier">The required identifier.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadExactStream(Stream? input, MessageIdentifier? identifier, MessageReadOptions? options)
    {
        if (input == null || options == null || identifier == null)
        {
            return CreateExactInputFailure(input == null, options == null, identifier == null);
        }

        byte[] bytes = MessageDetector.ReadStreamBytes(input, options.Limits);
        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromBytes(bytes, null);
        MessageReadResult result = BuildExactResult(tree, bytes.LongLength, identifier, options);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Reads text only when the detected payload has the exact requested identifier.</summary>
    /// <param name="input">The XML text.</param><param name="identifier">The required identifier.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadExactText(string? input, MessageIdentifier? identifier, MessageReadOptions? options)
    {
        if (input == null || options == null || identifier == null)
        {
            return CreateExactInputFailure(input == null, options == null, identifier == null);
        }

        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromText(input, null);
        MessageReadResult result = BuildExactResult(tree, Encoding.UTF8.GetByteCount(input), identifier, options);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Reads only the application header from bytes.</summary>
    /// <param name="input">The encoded XML input.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadHeaderBytes(byte[]? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromBytes(input, null);
        MessageReadResult result = BuildDetectedResult(tree, input.LongLength, options, true, false, false);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Reads only the application header from a stream.</summary>
    /// <param name="input">The input stream.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadHeaderStream(Stream? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        byte[] bytes = MessageDetector.ReadStreamBytes(input, options.Limits);
        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromBytes(bytes, null);
        MessageReadResult result = BuildDetectedResult(tree, bytes.LongLength, options, true, false, false);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Reads only the application header from text.</summary>
    /// <param name="input">The XML text.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadHeaderText(string? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromText(input, null);
        MessageReadResult result = BuildDetectedResult(tree, Encoding.UTF8.GetByteCount(input), options, true, false, false);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Reads only metadata from bytes.</summary>
    /// <param name="input">The encoded XML input.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadMetadataBytes(byte[]? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromBytes(input, null);
        MessageReadResult result = BuildDetectedResult(tree, input.LongLength, options, false, false, false);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Reads only metadata from a stream.</summary>
    /// <param name="input">The input stream.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadMetadataStream(Stream? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        byte[] bytes = MessageDetector.ReadStreamBytes(input, options.Limits);
        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromBytes(bytes, null);
        MessageReadResult result = BuildDetectedResult(tree, bytes.LongLength, options, false, false, false);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    /// <summary>Reads only metadata from text.</summary>
    /// <param name="input">The XML text.</param><param name="options">The read options.</param><returns>The read result.</returns>
    public MessageReadResult ReadMetadataText(string? input, MessageReadOptions? options)
    {
        if (input == null || options == null)
        {
            return CreateInputFailure(input == null, options == null);
        }

        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(options);
        DynamicXmlNode tree = nodeReader.ReadDocumentFromText(input, null);
        MessageReadResult result = BuildDetectedResult(tree, Encoding.UTF8.GetByteCount(input), options, false, false, false);
        PrependInputRepairs(result, nodeReader.Repairs, options.Limits);
        return result;
    }

    internal static MessageReadResult BuildGenericResult(DynamicXmlNode tree, long byteCount, MessageReadOptions options)
    {
        MessageDocument document = new MessageDocument();
        document.Layout = EnvelopeKind.Unknown;
        List<MessageRepair> repairs = new List<MessageRepair>();
        List<DynamicXmlNode> roots = MessageDetector.GetElementChildren(tree);
        int index = 0;
        while (index < roots.Count)
        {
            DynamicXmlNode root = roots[index];
            MessagePayload payload = new MessagePayload();
            payload.GenericContent = root;
            NamespaceServiceResult namespaceResult = new NamespaceService().ParseNamespace(root.Name.NamespaceUri, options.NamespaceMode);
            if (namespaceResult.Success && namespaceResult.Descriptor != null)
            {
                payload.Namespace = namespaceResult.Descriptor;
                payload.Identifier = namespaceResult.Descriptor.MessageIdentifier;
                int repairIndex = 0;
                while (repairIndex < namespaceResult.Repairs.Count)
                {
                    repairs.Add(MessageRepairFactory.CreateNamespace(namespaceResult.Repairs[repairIndex], root));
                    repairIndex++;
                }
            }

            document.Payloads.Add(payload);
            if (index == 0)
            {
                document.NamespaceDeclarations = root.NamespaceDeclarations;
                document.Location = root.Location;
                document.Metadata.MessageIdentifier = payload.Identifier;
                document.Metadata.NamespaceUri = root.Name.NamespaceUri;
            }

            index++;
        }

        if (roots.Count == 0)
        {
            return CreateFailure("reader.no_root", "The generic XML input does not contain an element root.", byteCount);
        }

        MessageReadResult result = CreateSuccess(document, byteCount);
        AddMessageRepairs(result, repairs, options.Limits);
        if (roots.Count > 1 && options.NamespaceMode == NamespaceMode.Lenient)
        {
            MessageRepair siblingRepair = MessageRepairFactory.Create(
                "xml.multiple_roots_wrapped",
                "Multiple generic XML roots were interpreted as one logical XML fragment.",
                roots.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "XmlFragment",
                roots[0]);
            AddSingleRepair(result, siblingRepair, options.Limits);
        }

        return result;
    }

    internal static MessageReadResult BuildDetectedResult(DynamicXmlNode tree, long byteCount, MessageReadOptions options, bool includeHeader, bool includePayloads, bool includeEnvelope)
    {
        MessageDetectionResult detection = MessageDetector.DetectDocument(tree, options);
        if (!detection.Success)
        {
            MessageReadResult failure = CreateFailure(detection.ErrorCode, detection.ErrorMessage, byteCount);
            AddMessageRepairs(failure, detection.Repairs, options.Limits);
            return failure;
        }

        MessageDocument document = MaterializeDocument(tree, detection, options, includeHeader, includePayloads, includeEnvelope);
        MessageReadResult result = CreateSuccess(document, byteCount);
        AddMessageRepairs(result, detection.Repairs, options.Limits);
        return result;
    }

    internal static MessageReadResult BuildExactResult(DynamicXmlNode tree, long byteCount, MessageIdentifier expected, MessageReadOptions options)
    {
        MessageIdentifier? normalizedExpected = NormalizeExpectedIdentifier(expected);
        if (normalizedExpected == null)
        {
            return CreateFailure("reader.exact_identifier_invalid", "The requested exact message identifier is invalid or internally inconsistent.", byteCount);
        }

        MessageReadResult result = BuildDetectedResult(tree, byteCount, options, true, true, true);
        if (!result.Success || result.Document == null)
        {
            return result;
        }

        MessageIdentifier? actual = result.Document.Metadata.MessageIdentifier;
        if (actual == null || actual.Value != normalizedExpected.Value)
        {
            return CreateFailure("reader.exact_identifier_mismatch", "The detected payload does not have the requested exact message identifier.", byteCount);
        }

        return result;
    }

    internal static MessageDocument MaterializeDocument(DynamicXmlNode tree, MessageDetectionResult detection, MessageReadOptions options, bool includeHeader, bool includePayloads, bool includeEnvelope)
    {
        MessageDocument document = new MessageDocument();
        document.Layout = detection.Layout;
        document.HeaderPrecedesPayload = detection.HeaderPrecedesPayload;
        List<DynamicXmlNode> roots = MessageDetector.GetElementChildren(tree);
        if (roots.Count > 0)
        {
            document.NamespaceDeclarations = roots[0].NamespaceDeclarations;
            document.Location = roots[0].Location;
        }

        DynamicXmlNode? headerNode = FindFirstElement(tree, "AppHdr");
        if (includeHeader && headerNode != null)
        {
            document.Header = CreateHeader(headerNode, detection);
        }

        List<DynamicXmlNode> payloadNodes = new List<DynamicXmlNode>();
        CollectPayloadElements(tree, payloadNodes);
        int payloadIndex = 0;
        while (includePayloads && payloadIndex < payloadNodes.Count)
        {
            MessagePayload payload = CreatePayload(payloadNodes[payloadIndex], options);
            document.Payloads.Add(payload);
            payloadIndex++;
        }

        if (includeEnvelope && (detection.Layout == EnvelopeKind.BusinessMessageEnvelope || detection.Layout == EnvelopeKind.BusinessFileEnvelope || detection.Layout == EnvelopeKind.CustomWrapper) && roots.Count > 0)
        {
            MessageEnvelope envelope = new MessageEnvelope();
            envelope.Kind = detection.Layout;
            envelope.Name = XmlPathTracker.CopyName(roots[0].Name);
            envelope.Namespace = detection.EnvelopeNamespace;
            envelope.Header = document.Header;
            envelope.Payloads = document.Payloads;
            envelope.NamespaceDeclarations = roots[0].NamespaceDeclarations;
            envelope.GenericContent = roots[0];
            document.Envelope = envelope;
        }

        PopulateMetadata(document.Metadata, tree, detection);
        return document;
    }

    internal static MessageHeader CreateHeader(DynamicXmlNode node, MessageDetectionResult detection)
    {
        MessageHeader header = new MessageHeader();
        header.Identifier = detection.HeaderNamespace?.MessageIdentifier;
        header.Namespace = detection.HeaderNamespace;
        header.BusinessMessageIdentifier = FindFirstText(node, "BizMsgIdr") ?? string.Empty;
        header.BusinessService = FindFirstText(node, "BizSvc") ?? string.Empty;
        header.GenericContent = node;
        return header;
    }

    internal static MessagePayload CreatePayload(DynamicXmlNode node, MessageReadOptions options)
    {
        MessagePayload payload = new MessagePayload();
        NamespaceServiceResult namespaceResult = new NamespaceService().ParseNamespace(node.Name.NamespaceUri, options.NamespaceMode);
        if (namespaceResult.Success)
        {
            NamespaceDescriptor descriptor = RequireNamespaceDescriptor(namespaceResult);
            payload.Namespace = descriptor;
            payload.Identifier = descriptor.MessageIdentifier;
        }

        payload.GenericContent = node;
        return payload;
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

    internal static void PopulateMetadata(MessageMetadata metadata, DynamicXmlNode tree, MessageDetectionResult detection)
    {
        metadata.MessageIdentifier = detection.MessageIdentifier;
        metadata.NamespaceUri = detection.MessageNamespace?.Uri ?? string.Empty;
        metadata.BusinessMessageIdentifier = FindFirstText(tree, "BizMsgIdr") ?? string.Empty;
        metadata.BusinessService = FindFirstText(tree, "BizSvc") ?? string.Empty;
        metadata.Sender = FindFirstText(tree, "Fr") ?? string.Empty;
        metadata.Receiver = FindFirstText(tree, "To") ?? string.Empty;
        string? creationText = FindFirstText(tree, "CreDt");
        if (creationText != null)
        {
            metadata.CreationDateTime = DateTimeLexical.ParseDateTime(creationText.AsSpan());
        }

        string? duplicateText = FindFirstText(tree, "PssblDplct");
        if (duplicateText != null)
        {
            IsoBooleanValue? duplicate = BooleanLexical.ParseBoolean(duplicateText.AsSpan());
            if (duplicate != null)
            {
                metadata.HasPossibleDuplicate = true;
                metadata.PossibleDuplicate = duplicate.Value;
            }
        }

        metadata.Priority = FindFirstText(tree, "Prty") ?? string.Empty;
        metadata.Uetr = FindFirstText(tree, "UETR") ?? string.Empty;
        DynamicXmlNode? amountNode = FindFirstElement(tree, "IntrBkSttlmAmt");
        if (amountNode != null)
        {
            string amountText = CollectElementText(amountNode);
            IsoDecimalValue? amount = DecimalLexical.ParseDecimal(amountText.AsSpan());
            string? currency = FindAttributeValue(amountNode, "Ccy");
            if (amount != null && currency != null)
            {
                metadata.PrimaryAmount = new IsoAmountValue();
                metadata.PrimaryAmount.Amount = amount;
                metadata.PrimaryAmount.Currency = currency;
            }
        }

        string? valueDateText = FindFirstText(tree, "IntrBkSttlmDt");
        if (valueDateText != null)
        {
            metadata.ValueDate = DateTimeLexical.ParseDate(valueDateText.AsSpan());
        }

        AddReference(metadata, tree, "MsgId");
        AddReference(metadata, tree, "InstrId");
        AddReference(metadata, tree, "EndToEndId");
        AddReference(metadata, tree, "TxId");
    }

    internal static void AddReference(MessageMetadata metadata, DynamicXmlNode tree, string localName)
    {
        string? value = FindFirstText(tree, localName);
        if (value != null && value.Length > 0)
        {
            metadata.References.Add(value);
        }
    }

    internal static DynamicXmlNode? FindFirstElement(DynamicXmlNode node, string localName)
    {
        if (node.Kind == XmlNodeKind.Element && node.Name.LocalName == localName)
        {
            return node;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            DynamicXmlNode? result = FindFirstElement(node.Children[index], localName);
            if (result != null)
            {
                return result;
            }

            index++;
        }

        return null;
    }

    internal static void CollectPayloadElements(
        DynamicXmlNode node,
        List<DynamicXmlNode> results)
    {
        if (node.Kind == XmlNodeKind.Element &&
            node.Name.LocalName == "Document")
        {
            results.Add(node);
            return;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            CollectPayloadElements(node.Children[index], results);
            index++;
        }
    }

    internal static string? FindFirstText(DynamicXmlNode node, string localName)
    {
        DynamicXmlNode? element = FindFirstElement(node, localName);
        if (element == null)
        {
            return null;
        }

        return NamespaceService.TrimXmlWhitespace(CollectElementText(element));
    }

    internal static string CollectElementText(DynamicXmlNode node)
    {
        StringBuilder text = new StringBuilder();
        CollectTextContent(node, text);
        return text.ToString();
    }

    internal static void CollectTextContent(DynamicXmlNode node, StringBuilder text)
    {
        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            text.Append(node.Value);
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            CollectTextContent(node.Children[index], text);
            index++;
        }
    }

    internal static string? FindAttributeValue(DynamicXmlNode node, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            if (node.Attributes[index].Name.LocalName == localName)
            {
                return node.Attributes[index].Value;
            }

            index++;
        }

        return null;
    }

    internal static MessageIdentifier? NormalizeExpectedIdentifier(MessageIdentifier expected)
    {
        string value = expected.Value ?? string.Empty;
        string businessArea = expected.BusinessArea ?? string.Empty;
        string functionality = expected.MessageFunctionality ?? string.Empty;
        string variant = expected.Variant ?? string.Empty;
        string version = expected.Version ?? string.Empty;
        if (value.Length > 0)
        {
            MessageIdentifier? parsed = MessageIdentifierLexical.ParseMessageIdentifier(value.AsSpan());
            if (parsed == null)
            {
                return null;
            }

            if ((businessArea.Length > 0 && businessArea != parsed.BusinessArea) ||
                (functionality.Length > 0 && functionality != parsed.MessageFunctionality) ||
                (variant.Length > 0 && variant != parsed.Variant) ||
                (version.Length > 0 && version != parsed.Version))
            {
                return null;
            }

            return parsed;
        }

        string formatted = businessArea + "." + functionality + "." + variant + "." + version;
        return MessageIdentifierLexical.ParseMessageIdentifier(formatted.AsSpan());
    }

    internal static void AddMessageRepairs(MessageReadResult result, List<MessageRepair> repairs, ResourceLimits limits)
    {
        MessageRepairFactory.AddCopies(result.Repairs, repairs, limits);
    }

    internal static void PrependInputRepairs(MessageReadResult result, List<MessageRepair> repairs, ResourceLimits limits)
    {
        MessageRepairFactory.PrependCopies(result.Repairs, repairs, limits);
    }

    internal static void AddSingleRepair(MessageReadResult result, MessageRepair repair, ResourceLimits limits)
    {
        if (result.Repairs.Count >= limits.MaxDiagnosticCount)
        {
            throw new System.Xml.XmlException("The repair diagnostic collection exceeds MaxDiagnosticCount.");
        }

        result.Repairs.Add(repair);
    }

    internal static MessageReadResult CreateSuccess(MessageDocument document, long byteCount)
    {
        MessageReadResult result = new MessageReadResult();
        result.Success = true;
        result.Document = document;
        result.BytesConsumed = byteCount;
        result.EndOfInput = true;
        return result;
    }

    internal static MessageReadResult CreateFailure(string code, string message, long byteCount)
    {
        MessageReadResult result = new MessageReadResult();
        result.ErrorCode = code;
        result.ErrorMessage = message;
        result.BytesConsumed = byteCount;
        result.EndOfInput = true;
        return result;
    }

    internal static MessageReadResult CreateInputFailure(bool inputNull, bool optionsNull)
    {
        if (inputNull)
        {
            return CreateFailure("reader.input_null", "XML message input is required.", 0);
        }

        if (optionsNull)
        {
            return CreateFailure("reader.options_null", "Message read options are required.", 0);
        }

        return CreateFailure("reader.input", "XML message input is invalid.", 0);
    }

    internal static MessageReadResult CreateExactInputFailure(bool inputNull, bool optionsNull, bool identifierNull)
    {
        if (identifierNull)
        {
            return CreateFailure("reader.exact_identifier_null", "An exact message identifier is required.", 0);
        }

        return CreateInputFailure(inputNull, optionsNull);
    }
}
