using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace iso20022;

/// <summary>Pulls bounded messages from business-file envelopes and configured sequence wrappers.</summary>
public class MessageStreamReader : IMessageStreamReader, IDisposable
{
    const string ItemWrapperNamespace = "urn:iso20022:internal:stream-item";
    Stream? input;
    StreamReadOptions? options;
    XmlReaderSettings? settings;
    XmlReader? reader;
    ResourceLimitGuard? inputGuard;
    MessageReadResult? initialFailure;
    StreamItemFragment? queuedFragment;
    StreamItemFragment? pendingHeader;
    MessageDocument? repeatingContext;
    List<QualifiedNameData> activePath = new List<QualifiedNameData>();
    bool initialized;
    bool currentNodeReady;
    bool businessFile;
    bool customWrapper;
    bool repeatingBlocks;
    bool stopped;
    int rootDepth;
    long nextIndex;

    /// <summary>Initializes a pull reader over one caller-owned input stream.</summary>
    /// <param name="input">The XML sequence stream, retained open when this reader is disposed.</param>
    /// <param name="options">The per-message, wrapper, continuation, and item-limit options.</param>
    public MessageStreamReader(Stream? input, StreamReadOptions? options)
    {
        this.input = input;
        this.options = options;
        if (input == null)
        {
            initialFailure = CreateReadFailure(
                "stream.input_null",
                "A message sequence input stream is required.");
            return;
        }

        if (options == null)
        {
            initialFailure = CreateReadFailure(
                "stream.options_null",
                "Message stream read options are required.");
            return;
        }

        if (options.MessageOptions == null || options.MessageOptions.Limits == null)
        {
            initialFailure = CreateReadFailure(
                "stream.message_options_invalid",
                "Message and resource-limit options are required.");
            return;
        }

        settings = SecureXmlReaderFactory.CreateSettings(options.MessageOptions);
        settings.CloseInput = false;
        settings.ConformanceLevel = ConformanceLevel.Document;
        inputGuard = new ResourceLimitGuard(options.MessageOptions.Limits);
        repeatingBlocks = options.MessageOptions.SelectedPaths.Count > 0;
    }

    /// <summary>Reads the next business message or returns <c>null</c> after the sequence ends.</summary>
    /// <returns>The next indexed read result, including any item-specific error.</returns>
    public MessageStreamItem? ReadNextMessage()
    {
        if (stopped)
        {
            return null;
        }

        ObserveCancellation();
        if (initialFailure != null)
        {
            MessageStreamItem failureItem = CreateItem(initialFailure, new SourceLocation());
            initialFailure = null;
            stopped = true;
            return failureItem;
        }

        InitializeReader();
        if (!initialized)
        {
            MessageStreamItem? initializationItem = InitializeSequence();
            if (initializationItem != null)
            {
                return initializationItem;
            }
        }

        StreamItemFragment? fragment = ReadNextFragment();
        if (fragment == null)
        {
            stopped = true;
            DisposeReader();
            return null;
        }

        int maximumItems = ResolveMaximumItems();
        if (nextIndex >= maximumItems)
        {
            MessageReadResult limitFailure = CreateReadFailure(
                "stream.item_limit",
                "The message sequence exceeds the configured item limit.");
            limitFailure.EndOfInput = true;
            MessageStreamItem limitItem;
            if (repeatingBlocks)
            {
                limitItem = CreateRepeatingItem(limitFailure, fragment);
            }
            else
            {
                limitItem = CreateItem(limitFailure, fragment.Location);
            }

            stopped = true;
            DisposeReader();
            return limitItem;
        }

        MessageReadResult result;
        if (repeatingBlocks)
        {
            result = MaterializeRepeatingItem(fragment);
        }
        else
        {
            result = MaterializeItem(fragment);
        }

        result.EndOfInput = false;
        MessageStreamItem item;
        if (repeatingBlocks)
        {
            item = CreateRepeatingItem(result, fragment);
        }
        else
        {
            item = CreateItem(result, fragment.Location);
        }
        if (!result.Success && options != null && !options.ContinueAfterInvalidItem)
        {
            result.EndOfInput = true;
            stopped = true;
            DisposeReader();
        }

        return item;
    }

    /// <summary>Releases XML parser state while retaining the caller-owned input stream.</summary>
    public void Dispose()
    {
        stopped = true;
        DisposeReader();
    }

    internal MessageStreamItem? InitializeSequence()
    {
        initialized = true;
        if (!AdvanceReader())
        {
            stopped = true;
            return CreateItem(
                CreateReadFailure("stream.empty", "The message sequence is empty."),
                new SourceLocation());
        }

        while (reader != null && reader.NodeType != XmlNodeType.Element)
        {
            if (!AdvanceReader())
            {
                stopped = true;
                return CreateItem(
                    CreateReadFailure("stream.empty", "The message sequence is empty."),
                    new SourceLocation());
            }
        }

        if (reader == null)
        {
            stopped = true;
            return CreateItem(
                CreateReadFailure("stream.empty", "The message sequence is empty."),
                new SourceLocation());
        }

        rootDepth = reader.Depth;
        QualifiedNameData rootName = CaptureName();
        if (repeatingBlocks)
        {
            return InitializeRepeatingRoot(rootName);
        }

        businessFile = IsBusinessFileRoot(rootName);
        customWrapper = IsConfiguredWrapper(rootName);
        if (businessFile)
        {
            return null;
        }

        if (IsBusinessMessageRoot(rootName))
        {
            queuedFragment = CaptureFragment();
            return null;
        }

        if (customWrapper)
        {
            return null;
        }

        SourceLocation location = CaptureLocation();
        stopped = true;
        return CreateItem(
            CreateReadFailure(
                "stream.wrapper_unsupported",
                "The XML root is not a business-file, business-message, or configured sequence wrapper."),
            location);
    }

    internal StreamItemFragment? ReadNextFragment()
    {
        if (repeatingBlocks)
        {
            return ReadNextRepeatingFragment();
        }

        if (queuedFragment != null)
        {
            StreamItemFragment queued = queuedFragment;
            queuedFragment = null;
            return queued;
        }

        while (AdvanceReader())
        {
            if (reader == null)
            {
                return null;
            }

            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == rootDepth)
            {
                if (pendingHeader != null)
                {
                    StreamItemFragment header = pendingHeader;
                    pendingHeader = null;
                    return header;
                }

                return null;
            }

            if (reader.NodeType == XmlNodeType.Element && IsItemElement())
            {
                StreamItemFragment current = CaptureFragment();
                if (customWrapper && current.Name.LocalName == "AppHdr")
                {
                    if (pendingHeader == null)
                    {
                        pendingHeader = current;
                        continue;
                    }

                    StreamItemFragment previousHeader = pendingHeader;
                    pendingHeader = current;
                    return previousHeader;
                }

                if (customWrapper && current.Name.LocalName == "Document" && pendingHeader != null)
                {
                    StreamItemFragment header = pendingHeader;
                    pendingHeader = null;
                    return CombineFragments(header, current);
                }

                if (pendingHeader != null)
                {
                    StreamItemFragment header = pendingHeader;
                    pendingHeader = null;
                    queuedFragment = current;
                    return header;
                }

                return current;
            }
        }

        if (pendingHeader != null)
        {
            StreamItemFragment header = pendingHeader;
            pendingHeader = null;
            return header;
        }

        return null;
    }

    internal MessageStreamItem? InitializeRepeatingRoot(QualifiedNameData rootName)
    {
        repeatingContext = new MessageDocument();
        repeatingContext.Location = CaptureLocation();
        UpdateActivePath();
        if (rootName.LocalName == "Document" && UpdateMessageContext(rootName))
        {
            repeatingContext.Layout = EnvelopeKind.StandaloneDocument;
            return null;
        }

        if (IsBusinessMessageRoot(rootName))
        {
            repeatingContext.Layout = EnvelopeKind.BusinessMessageEnvelope;
            return null;
        }

        businessFile = IsBusinessFileRoot(rootName);
        if (businessFile)
        {
            repeatingContext.Layout = EnvelopeKind.BusinessFileEnvelope;
            return null;
        }

        customWrapper = IsConfiguredWrapper(rootName);
        if (customWrapper)
        {
            repeatingContext.Layout = EnvelopeKind.CustomWrapper;
            return null;
        }

        stopped = true;
        return CreateItem(
            CreateReadFailure(
                "stream.repeating_root_unsupported",
                "Repeating-block selection requires a recognized message or configured wrapper root."),
            CaptureLocation());
    }

    internal StreamItemFragment? ReadNextRepeatingFragment()
    {
        while (AdvanceReader())
        {
            if (reader == null)
            {
                return null;
            }

            if (reader.NodeType == XmlNodeType.Element)
            {
                UpdateActivePath();
                QualifiedNameData name = CaptureName();
                if (name.LocalName == "Document")
                {
                    UpdateMessageContext(name);
                }
                else if (name.LocalName == "AppHdr")
                {
                    UpdateHeaderContext(name);
                }

                XmlPathData currentPath = CaptureActivePath();
                if (SelectedPathMatches(currentPath))
                {
                    StreamItemFragment fragment = CaptureFragment();
                    fragment.Path = currentPath;
                    return fragment;
                }
            }
        }

        return null;
    }

    internal void UpdateActivePath()
    {
        if (reader == null || reader.NodeType != XmlNodeType.Element)
        {
            return;
        }

        int depth = reader.Depth;
        while (activePath.Count > depth)
        {
            activePath.RemoveAt(activePath.Count - 1);
        }

        QualifiedNameData name = CaptureName();
        if (activePath.Count == depth)
        {
            activePath.Add(name);
        }
        else
        {
            activePath[depth] = name;
        }
    }

    internal XmlPathData CaptureActivePath()
    {
        XmlPathData path = new XmlPathData();
        StringBuilder display = new StringBuilder();
        int index = 0;
        while (index < activePath.Count)
        {
            QualifiedNameData name = XmlPathTracker.CopyName(activePath[index]);
            path.Segments.Add(name);
            display.Append('/');
            if (name.NamespaceUri.Length > 0)
            {
                display.Append("Q{");
                display.Append(name.NamespaceUri);
                display.Append('}');
            }

            display.Append(name.LocalName);
            index++;
        }

        path.Value = display.ToString();
        return path;
    }

    internal bool SelectedPathMatches(XmlPathData current)
    {
        if (options == null)
        {
            return false;
        }

        int index = 0;
        while (index < options.MessageOptions.SelectedPaths.Count)
        {
            if (PathMatches(options.MessageOptions.SelectedPaths[index], current))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static bool PathMatches(XmlPathData requested, XmlPathData current)
    {
        if (requested.Segments.Count > 0)
        {
            if (requested.Segments.Count != current.Segments.Count)
            {
                return false;
            }

            int index = 0;
            while (index < requested.Segments.Count)
            {
                if (requested.Segments[index].LocalName != current.Segments[index].LocalName ||
                    requested.Segments[index].NamespaceUri != current.Segments[index].NamespaceUri)
                {
                    return false;
                }

                index++;
            }

            return true;
        }

        if (requested.Value == current.Value)
        {
            return true;
        }

        StringBuilder localPath = new StringBuilder();
        int localIndex = 0;
        while (localIndex < current.Segments.Count)
        {
            localPath.Append('/');
            localPath.Append(current.Segments[localIndex].LocalName);
            localIndex++;
        }

        return requested.Value == localPath.ToString();
    }

    internal bool UpdateMessageContext(QualifiedNameData name)
    {
        if (repeatingContext == null || options == null)
        {
            return false;
        }

        NamespaceServiceResult result = new NamespaceService().ParseNamespace(
            name.NamespaceUri,
            options.MessageOptions.NamespaceMode);
        if (!result.Success || result.Descriptor == null ||
            result.Descriptor.Kind != NamespaceKind.Message)
        {
            return false;
        }

        repeatingContext.Metadata.MessageIdentifier = result.Descriptor.MessageIdentifier;
        repeatingContext.Metadata.NamespaceUri = result.Descriptor.Uri;
        return true;
    }

    internal void UpdateHeaderContext(QualifiedNameData name)
    {
        if (repeatingContext == null || options == null)
        {
            return;
        }

        NamespaceServiceResult result = new NamespaceService().ParseNamespace(
            name.NamespaceUri,
            options.MessageOptions.NamespaceMode);
        if (result.Success && result.Descriptor != null &&
            result.Descriptor.Kind == NamespaceKind.Header)
        {
            MessageHeader header = new MessageHeader();
            header.Identifier = result.Descriptor.MessageIdentifier;
            header.Namespace = result.Descriptor;
            repeatingContext.Header = header;
            repeatingContext.HeaderPrecedesPayload =
                repeatingContext.Metadata.MessageIdentifier == null;
        }
    }

    internal bool IsItemElement()
    {
        if (reader == null)
        {
            return false;
        }

        if (businessFile)
        {
            if (reader.LocalName == "BizCntt")
            {
                return true;
            }

            return reader.Depth == rootDepth + 1 &&
                (reader.LocalName == "Message" ||
                    reader.LocalName == "BizMsg" ||
                    MessageDetector.IsBusinessMessageEnvelopeName(reader.LocalName));
        }

        return customWrapper && reader.Depth == rootDepth + 1;
    }

    internal MessageReadResult MaterializeItem(StreamItemFragment fragment)
    {
        ObserveCancellation();
        MessageReadOptions itemOptions = CopyMessageOptions(fragment.Name);
        string wrapperXml = CreateNamespaceWrapper(fragment);
        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(itemOptions);
        DynamicXmlNode wrappedDocument = nodeReader.ReadDocumentFromText(wrapperXml, null);
        List<DynamicXmlNode> wrapperRoots = MessageDetector.GetElementChildren(wrappedDocument);
        DynamicXmlNode logicalDocument = new DynamicXmlNode();
        logicalDocument.Kind = XmlNodeKind.Document;
        if (wrapperRoots.Count == 1)
        {
            List<DynamicXmlNode> itemRoots = MessageDetector.GetElementChildren(wrapperRoots[0]);
            int rootIndex = 0;
            while (rootIndex < itemRoots.Count)
            {
                logicalDocument.Children.Add(itemRoots[rootIndex]);
                rootIndex++;
            }
        }

        long byteCount = Encoding.UTF8.GetByteCount(fragment.Xml);
        MessageReadResult result = XmlMessageReader.BuildDetectedResult(
            logicalDocument,
            byteCount,
            itemOptions,
            true,
            true,
            true);
        if (!result.Success)
        {
            result = XmlMessageReader.BuildDetectedResult(
                wrappedDocument,
                byteCount,
                itemOptions,
                true,
                true,
                true);
        }

        XmlMessageReader.PrependInputRepairs(
            result,
            nodeReader.Repairs,
            itemOptions.Limits);
        ObserveCancellation();
        return result;
    }

    internal MessageReadResult MaterializeRepeatingItem(StreamItemFragment fragment)
    {
        ObserveCancellation();
        MessageReadOptions itemOptions = CopyMessageOptions(fragment.Name);
        string wrapperXml = CreateNamespaceWrapper(fragment);
        GenericXmlNodeReader nodeReader = new GenericXmlNodeReader(itemOptions);
        DynamicXmlNode wrappedDocument = nodeReader.ReadDocumentFromText(wrapperXml, null);
        List<DynamicXmlNode> wrapperRoots = MessageDetector.GetElementChildren(wrappedDocument);
        if (wrapperRoots.Count != 1)
        {
            return CreateReadFailure(
                "stream.repeating_item_invalid",
                "A selected repeating item could not be materialized.");
        }

        List<DynamicXmlNode> itemRoots = MessageDetector.GetElementChildren(wrapperRoots[0]);
        if (itemRoots.Count != 1)
        {
            return CreateReadFailure(
                "stream.repeating_item_invalid",
                "A selected repeating item must contain exactly one element subtree.");
        }

        MessageDocument document = new MessageDocument();
        if (repeatingContext != null)
        {
            document.Layout = repeatingContext.Layout;
            document.HeaderPrecedesPayload = repeatingContext.HeaderPrecedesPayload;
            document.Header = repeatingContext.Header;
            document.Metadata = repeatingContext.Metadata;
        }

        MessagePayload payload = new MessagePayload();
        payload.GenericContent = itemRoots[0];
        if (repeatingContext != null)
        {
            payload.Identifier = repeatingContext.Metadata.MessageIdentifier;
            if (repeatingContext.Metadata.NamespaceUri.Length > 0 && options != null)
            {
                NamespaceServiceResult namespaceResult = new NamespaceService().ParseNamespace(
                    repeatingContext.Metadata.NamespaceUri,
                    options.MessageOptions.NamespaceMode);
                payload.Namespace = namespaceResult.Descriptor;
            }
        }

        document.Payloads.Add(payload);
        document.Location = fragment.Location;
        MessageReadResult result = new MessageReadResult();
        result.Success = true;
        result.Document = document;
        result.BytesConsumed = Encoding.UTF8.GetByteCount(fragment.Xml);
        XmlMessageReader.PrependInputRepairs(
            result,
            nodeReader.Repairs,
            itemOptions.Limits);
        ObserveCancellation();
        return result;
    }

    internal MessageReadOptions CopyMessageOptions(QualifiedNameData itemName)
    {
        MessageReadOptions copy = new MessageReadOptions();
        if (options == null || options.MessageOptions == null)
        {
            return copy;
        }

        MessageReadOptions source = options.MessageOptions;
        copy.NamespaceMode = source.NamespaceMode;
        copy.UnknownContentMode = source.UnknownContentMode;
        copy.VersionSelector = source.VersionSelector;
        copy.Limits = source.Limits;
        copy.PreserveComments = source.PreserveComments;
        copy.PreserveProcessingInstructions = source.PreserveProcessingInstructions;
        copy.PreserveWhitespace = source.PreserveWhitespace;
        copy.PreserveLexicalValues = source.PreserveLexicalValues;
        int pathIndex = 0;
        while (pathIndex < source.SelectedPaths.Count)
        {
            copy.SelectedPaths.Add(source.SelectedPaths[pathIndex]);
            pathIndex++;
        }

        int wrapperIndex = 0;
        while (wrapperIndex < source.CustomWrapperNames.Count)
        {
            copy.CustomWrapperNames.Add(source.CustomWrapperNames[wrapperIndex]);
            wrapperIndex++;
        }

        wrapperIndex = 0;
        while (wrapperIndex < options.WrapperNames.Count)
        {
            copy.CustomWrapperNames.Add(options.WrapperNames[wrapperIndex]);
            wrapperIndex++;
        }

        if (!MessageDetector.IsConfiguredWrapper(itemName, copy.CustomWrapperNames))
        {
            copy.CustomWrapperNames.Add(itemName);
        }

        QualifiedNameData streamItemName = new QualifiedNameData();
        streamItemName.NamespaceUri = ItemWrapperNamespace;
        streamItemName.LocalName = "StreamItem";
        streamItemName.Prefix = "stream";
        if (!MessageDetector.IsConfiguredWrapper(streamItemName, copy.CustomWrapperNames))
        {
            copy.CustomWrapperNames.Add(streamItemName);
        }

        return copy;
    }

    internal StreamItemFragment CaptureFragment()
    {
        ObserveCancellation();
        StreamItemFragment fragment = new StreamItemFragment();
        if (reader == null)
        {
            return fragment;
        }

        fragment.Name = CaptureName();
        fragment.Location = CaptureLocation();
        IXmlNamespaceResolver resolver = (IXmlNamespaceResolver)reader;
        IDictionary<string, string> namespaces = resolver.GetNamespacesInScope(
            XmlNamespaceScope.All);
        foreach (KeyValuePair<string, string> item in namespaces)
        {
            if (item.Key != "xml")
            {
                fragment.Namespaces[item.Key] = item.Value;
            }
        }

        fragment.Xml = reader.ReadOuterXml();
        ObserveCancellation();
        currentNodeReady = reader.ReadState == ReadState.Interactive;
        return fragment;
    }

    internal StreamItemFragment CombineFragments(
        StreamItemFragment first,
        StreamItemFragment second)
    {
        StreamItemFragment combined = new StreamItemFragment();
        combined.Xml = first.Xml + second.Xml;
        combined.Name = new QualifiedNameData
        {
            NamespaceUri = ItemWrapperNamespace,
            LocalName = "StreamItem",
            Prefix = "stream"
        };
        combined.Location = first.Location;
        foreach (KeyValuePair<string, string> item in first.Namespaces)
        {
            combined.Namespaces[item.Key] = item.Value;
        }

        foreach (KeyValuePair<string, string> item in second.Namespaces)
        {
            combined.Namespaces[item.Key] = item.Value;
        }

        return combined;
    }

    internal string CreateNamespaceWrapper(StreamItemFragment fragment)
    {
        StringBuilder text = new StringBuilder();
        text.Append("<stream:StreamItem xmlns:stream=\"");
        text.Append(ItemWrapperNamespace);
        text.Append('"');
        List<string> prefixes = new List<string>();
        foreach (KeyValuePair<string, string> item in fragment.Namespaces)
        {
            prefixes.Add(item.Key);
        }

        prefixes.Sort(StringComparer.Ordinal);
        int prefixIndex = 0;
        while (prefixIndex < prefixes.Count)
        {
            string prefix = prefixes[prefixIndex];
            if (prefix.Length == 0)
            {
                text.Append(" xmlns=\"");
            }
            else if (prefix != "stream")
            {
                text.Append(" xmlns:");
                text.Append(prefix);
                text.Append("=\"");
            }

            if (prefix != "stream")
            {
                text.Append(EscapeAttribute(fragment.Namespaces[prefix]));
                text.Append('"');
            }

            prefixIndex++;
        }

        text.Append('>');
        text.Append(fragment.Xml);
        text.Append("</stream:StreamItem>");
        return text.ToString();
    }

    internal bool AdvanceReader()
    {
        ObserveCancellation();
        if (reader == null)
        {
            return false;
        }

        if (currentNodeReady)
        {
            currentNodeReady = false;
            RecordCurrentElement();
            return true;
        }

        bool available = reader.Read();
        ObserveCancellation();
        if (available)
        {
            RecordCurrentElement();
        }

        return available;
    }

    internal void ObserveCancellation()
    {
        if (options != null && options.CancellationToken.IsCancellationRequested)
        {
            stopped = true;
            DisposeReader();
            options.CancellationToken.ThrowIfCancellationRequested();
        }
    }

    internal void InitializeReader()
    {
        if (reader == null && input != null && settings != null)
        {
            reader = XmlReader.Create(input, settings);
        }
    }

    internal void RecordCurrentElement()
    {
        if (reader != null && inputGuard != null && reader.NodeType == XmlNodeType.Element)
        {
            inputGuard.RecordElement(reader.Depth + 1, reader.AttributeCount);
        }
    }

    internal bool IsBusinessFileRoot(QualifiedNameData name)
    {
        if (!MessageDetector.IsBusinessFileEnvelopeName(name.LocalName) || options == null)
        {
            return false;
        }

        NamespaceServiceResult result = new NamespaceService().ParseNamespace(
            name.NamespaceUri,
            options.MessageOptions.NamespaceMode);
        return result.Success && result.Descriptor != null &&
            result.Descriptor.Kind == NamespaceKind.Envelope &&
            result.Descriptor.MessageIdentifier != null &&
            result.Descriptor.MessageIdentifier.MessageFunctionality == "002";
    }

    internal bool IsBusinessMessageRoot(QualifiedNameData name)
    {
        if (!MessageDetector.IsBusinessMessageEnvelopeName(name.LocalName) || options == null)
        {
            return false;
        }

        NamespaceServiceResult result = new NamespaceService().ParseNamespace(
            name.NamespaceUri,
            options.MessageOptions.NamespaceMode);
        return result.Success && result.Descriptor != null &&
            result.Descriptor.Kind == NamespaceKind.Envelope &&
            result.Descriptor.MessageIdentifier != null &&
            result.Descriptor.MessageIdentifier.MessageFunctionality == "001";
    }

    internal bool IsConfiguredWrapper(QualifiedNameData name)
    {
        if (options == null)
        {
            return false;
        }

        return MessageDetector.IsConfiguredWrapper(name, options.WrapperNames) ||
            MessageDetector.IsConfiguredWrapper(
                name,
                options.MessageOptions.CustomWrapperNames);
    }

    internal QualifiedNameData CaptureName()
    {
        QualifiedNameData name = new QualifiedNameData();
        if (reader != null)
        {
            name.NamespaceUri = reader.NamespaceURI;
            name.LocalName = reader.LocalName;
            name.Prefix = reader.Prefix;
        }

        return name;
    }

    internal SourceLocation CaptureLocation()
    {
        SourceLocation location = new SourceLocation();
        if (reader == null)
        {
            return location;
        }

        if (reader.BaseURI.Length > 0)
        {
            location.SourceName = reader.BaseURI;
        }

        IXmlLineInfo lineInformation = (IXmlLineInfo)reader;
        if (lineInformation.HasLineInfo())
        {
            location.HasLineInformation = true;
            location.LineNumber = lineInformation.LineNumber;
            location.ColumnNumber = lineInformation.LinePosition;
        }

        return location;
    }

    internal int ResolveMaximumItems()
    {
        if (options == null)
        {
            return 0;
        }

        if (options.MaxItems > 0)
        {
            return options.MaxItems;
        }

        return options.MessageOptions.Limits.MaxCollectionCount;
    }

    internal MessageStreamItem CreateItem(MessageReadResult result, SourceLocation location)
    {
        MessageStreamItem item = new MessageStreamItem();
        item.Index = nextIndex;
        item.Result = result;
        item.Location = location;
        nextIndex++;
        return item;
    }

    internal MessageStreamItem CreateRepeatingItem(
        MessageReadResult result,
        StreamItemFragment fragment)
    {
        MessageStreamItem item = CreateItem(result, fragment.Location);
        item.Context = repeatingContext;
        item.Path = fragment.Path;
        return item;
    }

    internal static MessageReadResult CreateReadFailure(string code, string message)
    {
        MessageReadResult result = new MessageReadResult();
        result.EndOfInput = true;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

    internal static string EscapeAttribute(string value)
    {
        return value.Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;");
    }

    internal void DisposeReader()
    {
        if (reader != null)
        {
            reader.Dispose();
            reader = null;
        }
    }
}
