using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Extracts bounded, prefix-independent metadata from generic message content.</summary>
public class MetadataExtractor : IMetadataExtractor
{
    const string RedactedValue = "[REDACTED]";

    /// <summary>Initializes a metadata extractor.</summary>
    public MetadataExtractor()
    {
    }

    /// <summary>Extracts bounded header, payload, identity, amount, date, and reference metadata.</summary>
    /// <param name="document">The generic message document to inspect.</param>
    /// <param name="options">The header, payload, redaction, path, and resource policies.</param>
    /// <returns>A new metadata value containing the selected fields.</returns>
    public MessageMetadata ExtractMetadata(
        MessageDocument? document,
        MetadataExtractionOptions? options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (options.Limits == null || options.AdditionalReferencePaths == null)
        {
            throw new ArgumentException(
                "Metadata limits and additional reference paths are required.",
                "options");
        }

        ResourceLimitGuard guard = new ResourceLimitGuard(options.Limits);
        guard.EnsureCollectionCount(options.AdditionalReferencePaths.Count);
        ValidateAdditionalPaths(options, guard);
        MessageMetadata metadata = new MessageMetadata();
        MetadataExtractionState state = new MetadataExtractionState(
            options,
            metadata,
            guard);
        ExtractStructuralIdentity(document, metadata);
        if (options.IncludeHeader)
        {
            ExtractHeaderMetadata(document, state);
        }

        if (options.IncludePayload)
        {
            ExtractPayloadMetadata(document, state);
        }

        if (options.RedactSensitiveValues)
        {
            RedactMetadata(metadata);
        }

        return metadata;
    }

    internal static void ValidateAdditionalPaths(
        MetadataExtractionOptions options,
        ResourceLimitGuard guard)
    {
        int index = 0;
        while (index < options.AdditionalReferencePaths.Count)
        {
            XmlPathData? path = options.AdditionalReferencePaths[index];
            if (path == null || path.Segments == null || path.Value == null ||
                (path.Segments.Count == 0 && path.Value.Length == 0))
            {
                throw new ArgumentException(
                    "Each additional reference path must identify an XML path.",
                    "options");
            }

            guard.EnsureCollectionCount(path.Segments.Count);
            if (path.Segments.Count > options.Limits.MaxDepth)
            {
                throw new ArgumentException(
                    "An additional reference path exceeds MaxDepth.",
                    "options");
            }

            index++;
        }
    }

    internal static void ExtractStructuralIdentity(
        MessageDocument document,
        MessageMetadata metadata)
    {
        int index = 0;
        while (index < document.Payloads.Count &&
            metadata.MessageIdentifier == null)
        {
            MessagePayload? payload = document.Payloads[index];
            if (payload != null)
            {
                if (payload.Identifier != null)
                {
                    metadata.MessageIdentifier = CopyIdentifier(payload.Identifier);
                }

                if (payload.Namespace != null && payload.Namespace.Uri.Length > 0)
                {
                    metadata.NamespaceUri = payload.Namespace.Uri;
                }
                else if (payload.GenericContent != null)
                {
                    DynamicXmlNode? root = FindFirstElementNode(payload.GenericContent);
                    if (root != null)
                    {
                        metadata.NamespaceUri = root.Name.NamespaceUri;
                    }
                }
                else if (payload.RawContent != null &&
                    payload.RawContent.RootName != null)
                {
                    metadata.NamespaceUri = payload.RawContent.RootName.NamespaceUri;
                }
            }

            index++;
        }

        if (metadata.MessageIdentifier == null &&
            document.Metadata.MessageIdentifier != null)
        {
            metadata.MessageIdentifier = CopyIdentifier(
                document.Metadata.MessageIdentifier);
        }

        if (metadata.NamespaceUri.Length == 0)
        {
            metadata.NamespaceUri = document.Metadata.NamespaceUri;
        }

        if (metadata.MessageIdentifier == null && metadata.NamespaceUri.Length > 0)
        {
            NamespaceServiceResult parsed = new NamespaceService().ParseNamespace(
                metadata.NamespaceUri,
                NamespaceMode.Strict);
            if (parsed.Success && parsed.Descriptor != null &&
                parsed.Descriptor.Kind == NamespaceKind.Message &&
                parsed.Descriptor.MessageIdentifier != null)
            {
                metadata.MessageIdentifier = CopyIdentifier(
                    parsed.Descriptor.MessageIdentifier);
            }
        }

        if (metadata.NamespaceUri.Length == 0 && metadata.MessageIdentifier != null)
        {
            NamespaceServiceResult built = new NamespaceService()
                .BuildNamespaceFromIdentifier(metadata.MessageIdentifier);
            if (built.Success && built.Descriptor != null)
            {
                metadata.NamespaceUri = built.Descriptor.Uri;
            }
        }
    }

    internal static void ExtractHeaderMetadata(
        MessageDocument document,
        MetadataExtractionState state)
    {
        DynamicXmlNode? tree = ResolveHeaderTree(document.Header, state.Options.Limits);
        if (tree != null)
        {
            ValidateMetadataTree(tree, state.Guard, 0);
            state.Metadata.BusinessMessageIdentifier =
                TextByLocalName(tree, "BizMsgIdr");
            state.Metadata.BusinessService = TextByLocalName(tree, "BizSvc");
            state.Metadata.Sender = PartyIdentity(tree, "Fr");
            state.Metadata.Receiver = PartyIdentity(tree, "To");
            string creationText = TextByLocalName(tree, "CreDt");
            if (creationText.Length > 0)
            {
                state.Metadata.CreationDateTime = DateTimeLexical.ParseDateTime(
                    creationText.AsSpan());
            }

            string duplicateText = TextByLocalName(tree, "PssblDplct");
            if (duplicateText.Length > 0)
            {
                IsoBooleanValue? duplicate = BooleanLexical.ParseBoolean(
                    duplicateText.AsSpan());
                if (duplicate != null)
                {
                    state.Metadata.HasPossibleDuplicate = true;
                    state.Metadata.PossibleDuplicate = duplicate.Value;
                }
            }

            state.Metadata.Priority = TextByLocalName(tree, "Prty");
        }

        ApplyHeaderFallback(document, state.Metadata);
    }

    internal static void ApplyHeaderFallback(
        MessageDocument document,
        MessageMetadata metadata)
    {
        if (document.Header != null)
        {
            if (metadata.BusinessMessageIdentifier.Length == 0)
            {
                metadata.BusinessMessageIdentifier =
                    document.Header.BusinessMessageIdentifier;
            }

            if (metadata.BusinessService.Length == 0)
            {
                metadata.BusinessService = document.Header.BusinessService;
            }
        }

        MessageMetadata source = document.Metadata;
        if (metadata.BusinessMessageIdentifier.Length == 0)
        {
            metadata.BusinessMessageIdentifier = source.BusinessMessageIdentifier;
        }

        if (metadata.BusinessService.Length == 0)
        {
            metadata.BusinessService = source.BusinessService;
        }

        if (metadata.Sender.Length == 0)
        {
            metadata.Sender = source.Sender;
        }

        if (metadata.Receiver.Length == 0)
        {
            metadata.Receiver = source.Receiver;
        }

        if (metadata.CreationDateTime == null && source.CreationDateTime != null)
        {
            metadata.CreationDateTime = CopyDateTime(source.CreationDateTime);
        }

        if (!metadata.HasPossibleDuplicate && source.HasPossibleDuplicate)
        {
            metadata.HasPossibleDuplicate = true;
            metadata.PossibleDuplicate = source.PossibleDuplicate;
        }

        if (metadata.Priority.Length == 0)
        {
            metadata.Priority = source.Priority;
        }
    }

    internal static void ExtractPayloadMetadata(
        MessageDocument document,
        MetadataExtractionState state)
    {
        state.Guard.EnsureCollectionCount(document.Payloads.Count);
        int index = 0;
        while (index < document.Payloads.Count)
        {
            MessagePayload? payload = document.Payloads[index];
            DynamicXmlNode? tree = ResolvePayloadTree(payload, state.Options.Limits);
            if (tree != null)
            {
                ValidateMetadataTree(tree, state.Guard, 0);
                if (state.Metadata.Uetr.Length == 0)
                {
                    state.Metadata.Uetr = TextByLocalName(tree, "UETR");
                }

                if (state.Metadata.PrimaryAmount == null)
                {
                    state.Metadata.PrimaryAmount = FindPrimaryAmount(tree);
                }

                if (state.Metadata.ValueDate == null)
                {
                    state.Metadata.ValueDate = FindPrimaryValueDate(tree);
                }

                CollectPayloadReferences(tree, state);
            }

            index++;
        }

        ApplyPayloadFallback(document.Metadata, state);
    }

    internal static void ApplyPayloadFallback(
        MessageMetadata source,
        MetadataExtractionState state)
    {
        if (state.Metadata.Uetr.Length == 0)
        {
            state.Metadata.Uetr = source.Uetr;
        }

        if (state.Metadata.PrimaryAmount == null && source.PrimaryAmount != null)
        {
            state.Metadata.PrimaryAmount = CopyAmount(source.PrimaryAmount);
        }

        if (state.Metadata.ValueDate == null && source.ValueDate != null)
        {
            state.Metadata.ValueDate = CopyDate(source.ValueDate);
        }

        int index = 0;
        while (index < source.References.Count)
        {
            AddReferenceValue(source.References[index], state);
            index++;
        }
    }

    internal static DynamicXmlNode? ResolveHeaderTree(
        MessageHeader? header,
        ResourceLimits limits)
    {
        if (header == null)
        {
            return null;
        }

        if (header.GenericContent != null)
        {
            return header.GenericContent;
        }

        if (header.RawContent != null)
        {
            return MaterializeRawTree(header.RawContent, limits);
        }

        return null;
    }

    internal static DynamicXmlNode? ResolvePayloadTree(
        MessagePayload? payload,
        ResourceLimits limits)
    {
        if (payload == null)
        {
            return null;
        }

        if (payload.GenericContent != null)
        {
            return payload.GenericContent;
        }

        if (payload.RawContent != null)
        {
            return MaterializeRawTree(payload.RawContent, limits);
        }

        return null;
    }

    internal static DynamicXmlNode MaterializeRawTree(
        RawXmlFragment raw,
        ResourceLimits limits)
    {
        MessageWriteOptions options = new MessageWriteOptions();
        options.Limits = limits;
        return RawXmlFragmentMaterializer.MaterializeDocument(raw, options);
    }

    internal static void ValidateMetadataTree(
        DynamicXmlNode node,
        ResourceLimitGuard guard,
        int depth)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            guard.RecordElement(depth + 1, node.Attributes.Count);
            guard.EnsureCollectionCount(node.Attributes.Count);
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count)
            {
                guard.EnsureTextLength(node.Attributes[attributeIndex].Value.Length);
                attributeIndex++;
            }
        }
        else if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            guard.EnsureTextLength(node.Value.Length);
        }

        guard.EnsureCollectionCount(node.Children.Count);
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            int childDepth = depth;
            if (node.Kind == XmlNodeKind.Element)
            {
                childDepth++;
            }

            ValidateMetadataTree(node.Children[childIndex], guard, childDepth);
            childIndex++;
        }
    }

    internal static string PartyIdentity(DynamicXmlNode tree, string partyName)
    {
        DynamicXmlNode? party = XmlMessageReader.FindFirstElement(tree, partyName);
        if (party == null)
        {
            return string.Empty;
        }

        string value = TextByLocalName(party, "BICFI");
        if (value.Length == 0)
        {
            value = TextByLocalName(party, "AnyBIC");
        }

        if (value.Length == 0)
        {
            value = TextByLocalName(party, "BIC");
        }

        if (value.Length == 0)
        {
            value = TextByLocalName(party, "LEI");
        }

        if (value.Length == 0)
        {
            value = TextByLocalName(party, "Id");
        }

        if (value.Length == 0)
        {
            value = TextByLocalName(party, "Nm");
        }

        if (value.Length == 0)
        {
            value = NamespaceService.TrimXmlWhitespace(
                XmlMessageReader.CollectElementText(party));
        }

        return value;
    }

    internal static string TextByLocalName(DynamicXmlNode tree, string localName)
    {
        string? value = XmlMessageReader.FindFirstText(tree, localName);
        return value ?? string.Empty;
    }

    internal static IsoAmountValue? FindPrimaryAmount(DynamicXmlNode tree)
    {
        string[] preferredNames =
        [
            "IntrBkSttlmAmt",
            "InstdAmt",
            "TtlIntrBkSttlmAmt",
            "TtlInstdAmt",
            "EqvtAmt"
        ];
        int index = 0;
        while (index < preferredNames.Length)
        {
            IsoAmountValue? amount = FindAmountNamed(tree, preferredNames[index]);
            if (amount != null)
            {
                return amount;
            }

            index++;
        }

        return FindAnyCurrencyAmount(tree);
    }

    internal static IsoAmountValue? FindAmountNamed(
        DynamicXmlNode node,
        string localName)
    {
        if (node.Kind == XmlNodeKind.Element && node.Name.LocalName == localName)
        {
            IsoAmountValue? amount = ParseAmountNode(node);
            if (amount != null)
            {
                return amount;
            }
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            IsoAmountValue? childAmount = FindAmountNamed(
                node.Children[index],
                localName);
            if (childAmount != null)
            {
                return childAmount;
            }

            index++;
        }

        return null;
    }

    internal static IsoAmountValue? FindAnyCurrencyAmount(DynamicXmlNode node)
    {
        if (node.Kind == XmlNodeKind.Element &&
            node.Name.LocalName.EndsWith("Amt", StringComparison.Ordinal))
        {
            IsoAmountValue? amount = ParseAmountNode(node);
            if (amount != null)
            {
                return amount;
            }
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            IsoAmountValue? childAmount = FindAnyCurrencyAmount(node.Children[index]);
            if (childAmount != null)
            {
                return childAmount;
            }

            index++;
        }

        return null;
    }

    internal static IsoAmountValue? ParseAmountNode(DynamicXmlNode node)
    {
        string? currency = XmlMessageReader.FindAttributeValue(node, "Ccy");
        if (currency == null || currency.Length == 0)
        {
            return null;
        }

        string amountText = NamespaceService.TrimXmlWhitespace(
            XmlMessageReader.CollectElementText(node));
        IsoDecimalValue? amount = DecimalLexical.ParseDecimal(amountText.AsSpan());
        if (amount == null)
        {
            return null;
        }

        IsoAmountValue result = new IsoAmountValue();
        result.Amount = amount;
        result.Currency = currency;
        return result;
    }

    internal static IsoDateValue? FindPrimaryValueDate(DynamicXmlNode tree)
    {
        string[] preferredNames =
        [
            "IntrBkSttlmDt",
            "ReqdExctnDt",
            "SttlmDt",
            "ValDt",
            "ReqdColltnDt"
        ];
        int index = 0;
        while (index < preferredNames.Length)
        {
            string value = TextByLocalName(tree, preferredNames[index]);
            if (value.Length > 0)
            {
                IsoDateValue? date = DateTimeLexical.ParseDate(value.AsSpan());
                if (date != null)
                {
                    return date;
                }
            }

            index++;
        }

        return null;
    }

    internal static void CollectPayloadReferences(
        DynamicXmlNode tree,
        MetadataExtractionState state)
    {
        state.Path.Clear();
        VisitReferenceNode(tree, state);
    }

    internal static void VisitReferenceNode(
        DynamicXmlNode node,
        MetadataExtractionState state)
    {
        bool addedSegment = false;
        if (node.Kind == XmlNodeKind.Element)
        {
            state.Path.Add(XmlPathTracker.CopyName(node.Name));
            addedSegment = true;
            if (IsStandardReferenceName(node.Name.LocalName) ||
                AdditionalPathMatches(state))
            {
                string value = NamespaceService.TrimXmlWhitespace(
                    XmlMessageReader.CollectElementText(node));
                AddReferenceValue(value, state);
            }
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            VisitReferenceNode(node.Children[index], state);
            index++;
        }

        if (addedSegment)
        {
            state.Path.RemoveAt(state.Path.Count - 1);
        }
    }

    internal static bool AdditionalPathMatches(MetadataExtractionState state)
    {
        if (state.Options.AdditionalReferencePaths.Count == 0)
        {
            return false;
        }

        XmlPathData current = new XmlPathData();
        int segmentIndex = 0;
        while (segmentIndex < state.Path.Count)
        {
            current.Segments.Add(XmlPathTracker.CopyName(state.Path[segmentIndex]));
            segmentIndex++;
        }

        int index = 0;
        while (index < state.Options.AdditionalReferencePaths.Count)
        {
            if (MessageStreamReader.PathMatches(
                state.Options.AdditionalReferencePaths[index],
                current))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static bool IsStandardReferenceName(string localName)
    {
        switch (localName)
        {
            case "MsgId":
            case "PmtInfId":
            case "InstrId":
            case "EndToEndId":
            case "TxId":
            case "MndtId":
            case "AcctSvcrRef":
            case "OrgnlMsgId":
            case "OrgnlInstrId":
            case "OrgnlEndToEndId":
            case "OrgnlTxId":
            case "RltdRef":
            case "TradId":
                return true;
            default:
                return false;
        }
    }

    internal static void AddReferenceValue(
        string value,
        MetadataExtractionState state)
    {
        if (value.Length == 0 || state.Metadata.References.Contains(value))
        {
            return;
        }

        state.Guard.EnsureCollectionCount(state.Metadata.References.Count + 1);
        state.Metadata.References.Add(value);
    }

    internal static DynamicXmlNode? FindFirstElementNode(DynamicXmlNode node)
    {
        if (node.Kind == XmlNodeKind.Element)
        {
            return node;
        }

        int index = 0;
        while (index < node.Children.Count)
        {
            DynamicXmlNode? child = FindFirstElementNode(node.Children[index]);
            if (child != null)
            {
                return child;
            }

            index++;
        }

        return null;
    }

    internal static MessageIdentifier CopyIdentifier(MessageIdentifier source)
    {
        MessageIdentifier copy = new MessageIdentifier();
        copy.BusinessArea = source.BusinessArea;
        copy.MessageFunctionality = source.MessageFunctionality;
        copy.Variant = source.Variant;
        copy.Version = source.Version;
        copy.Value = source.Value;
        return copy;
    }

    internal static IsoAmountValue CopyAmount(IsoAmountValue source)
    {
        IsoAmountValue copy = new IsoAmountValue();
        copy.Amount = CopyDecimal(source.Amount);
        copy.Currency = source.Currency;
        return copy;
    }

    internal static IsoDecimalValue CopyDecimal(IsoDecimalValue source)
    {
        IsoDecimalValue copy = new IsoDecimalValue();
        copy.Value = source.Value;
        copy.Scale = source.Scale;
        copy.IsNegativeZero = source.IsNegativeZero;
        copy.LexicalValue = source.LexicalValue;
        return copy;
    }

    internal static IsoDateTimeValue CopyDateTime(IsoDateTimeValue source)
    {
        IsoDateTimeValue copy = new IsoDateTimeValue();
        copy.Date = CopyDate(source.Date);
        copy.Time = CopyTime(source.Time);
        copy.LexicalMode = source.LexicalMode;
        copy.LexicalValue = source.LexicalValue;
        return copy;
    }

    internal static IsoDateValue CopyDate(IsoDateValue source)
    {
        IsoDateValue copy = new IsoDateValue();
        copy.Year = source.Year;
        copy.Month = source.Month;
        copy.Day = source.Day;
        copy.HasTimezone = source.HasTimezone;
        copy.TimezoneOffsetMinutes = source.TimezoneOffsetMinutes;
        copy.IsUtc = source.IsUtc;
        copy.IsNegativeTimezoneZero = source.IsNegativeTimezoneZero;
        copy.LexicalMode = source.LexicalMode;
        copy.LexicalValue = source.LexicalValue;
        return copy;
    }

    internal static IsoTimeValue CopyTime(IsoTimeValue source)
    {
        IsoTimeValue copy = new IsoTimeValue();
        copy.Hour = source.Hour;
        copy.Minute = source.Minute;
        copy.Second = source.Second;
        copy.FractionalSecondDigits = source.FractionalSecondDigits;
        copy.HasTimezone = source.HasTimezone;
        copy.TimezoneOffsetMinutes = source.TimezoneOffsetMinutes;
        copy.IsUtc = source.IsUtc;
        copy.IsNegativeTimezoneZero = source.IsNegativeTimezoneZero;
        copy.LexicalMode = source.LexicalMode;
        copy.LexicalValue = source.LexicalValue;
        return copy;
    }

    internal static void RedactMetadata(MessageMetadata metadata)
    {
        if (metadata.BusinessMessageIdentifier.Length > 0)
        {
            metadata.BusinessMessageIdentifier = RedactedValue;
        }

        if (metadata.Sender.Length > 0)
        {
            metadata.Sender = RedactedValue;
        }

        if (metadata.Receiver.Length > 0)
        {
            metadata.Receiver = RedactedValue;
        }

        if (metadata.CreationDateTime != null)
        {
            metadata.CreationDateTime = new IsoDateTimeValue();
            metadata.CreationDateTime.LexicalValue = RedactedValue;
        }

        if (metadata.Uetr.Length > 0)
        {
            metadata.Uetr = RedactedValue;
        }

        if (metadata.PrimaryAmount != null)
        {
            metadata.PrimaryAmount = new IsoAmountValue();
            metadata.PrimaryAmount.Amount.LexicalValue = RedactedValue;
            metadata.PrimaryAmount.Currency = RedactedValue;
        }

        if (metadata.ValueDate != null)
        {
            metadata.ValueDate = new IsoDateValue();
            metadata.ValueDate.LexicalValue = RedactedValue;
        }

        int index = 0;
        while (index < metadata.References.Count)
        {
            metadata.References[index] = RedactedValue;
            index++;
        }
    }
}
