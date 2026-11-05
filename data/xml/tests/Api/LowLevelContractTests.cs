using xml;

namespace xml.Tests;

[TestFixture]
public class LowLevelContractTests
{
    [Test]
    public void ParserValidationEscapingAndCallbackContractsCarryValues()
    {
        CoreContractStub stub = new CoreContractStub();
        XmlInput input = new XmlInput();
        XmlParserOptions options = new XmlParserOptions();
        XmlDocument document = stub.ParseXmlDocument(input, options, null, stub, stub);
        XmlDocument fragment = stub.ParseXmlFragment(input, options, null, stub, stub);
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        stub.ReportXmlDiagnostic(diagnostic);
        XmlDiagnostic[] documentDiagnostics = stub.ValidateXmlDocumentDtd(document, options);
        XmlDiagnostic[] eventDiagnostics = stub.ValidateXmlEventDtd(new XmlEventBatch(), new XmlDocumentType(), options);

        Assert.That(document.DocumentUri, Is.EqualTo("document"));
        Assert.That(fragment.DocumentUri, Is.EqualTo("fragment"));
        Assert.That(stub.Input, Is.SameAs(input));
        Assert.That(stub.Options, Is.SameAs(options));
        Assert.That(stub.Diagnostic, Is.SameAs(diagnostic));
        Assert.That(stub.IsXmlCancellationRequested(), Is.False);
        Assert.That(documentDiagnostics, Is.Empty);
        Assert.That(eventDiagnostics, Is.Empty);
        Assert.That(stub.EscapeXmlValue("value", XmlVersion.Xml11, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, '\0'), Is.EqualTo("escaped:value"));
        Assert.That(stub.UnescapeXmlValue("escaped:value", XmlVersion.Xml11, XmlCharacterEscapeContext.Text), Is.EqualTo("value"));
    }

    [Test]
    public void PushPullAndHandlerContractsExposeStateTransitions()
    {
        StreamingContractStub stub = new StreamingContractStub();
        XmlParserStatus started = stub.BeginXmlPush(new XmlInput(), new XmlParserOptions(), stub, null, null, null);
        XmlEventBatch bytes = stub.FeedXmlPushBytes(new byte[] { 1 }, false);
        XmlEventBatch characters = stub.FeedXmlPushCharacters("x".AsMemory(), false);
        XmlHandlerAction action = stub.HandleXmlEvent(new XmlEvent());
        XmlReaderState advanced = stub.AdvanceXmlPull();
        XmlReaderState inspected = stub.InspectXmlPull();

        Assert.That(started, Is.EqualTo(XmlParserStatus.Running));
        Assert.That(bytes.Count, Is.EqualTo(1));
        Assert.That(characters.Count, Is.EqualTo(1));
        Assert.That(action, Is.EqualTo(XmlHandlerAction.Continue));
        Assert.That(advanced.Status, Is.EqualTo(XmlParserStatus.Running));
        Assert.That(inspected, Is.SameAs(advanced));
        XmlReaderState skipped = stub.SkipXmlPullSubtree();
        Assert.That(skipped.Status, Is.EqualTo(XmlParserStatus.Completed));
        Assert.That(stub.PauseXmlPush(), Is.EqualTo(XmlParserStatus.Paused));
        Assert.That(stub.ResumeXmlPush(), Is.EqualTo(XmlParserStatus.Running));
        Assert.That(stub.AbortXmlPush(), Is.EqualTo(XmlParserStatus.Aborted));
        Assert.That(stub.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Aborted));
        Assert.That(stub.FinishXmlPush().IsFinal, Is.True);
        stub.ResetXmlPush();
        Assert.That(stub.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Ready));
        stub.CloseXmlPush();
        stub.CloseXmlPull();
        Assert.That(stub.InspectXmlPushStatus(), Is.EqualTo(XmlParserStatus.Closed));
    }

    [Test]
    public void TreeEmissionAndCatalogContractsComposeWithoutMemberCollisions()
    {
        TreeContractStub stub = new TreeContractStub();
        XmlDocument document = new XmlDocument();
        XmlNode parent = stub.CreateXmlTreeNode(document, XmlNodeKind.Element, new XmlName(), null);
        XmlNode child = stub.CreateXmlTreeNode(document, XmlNodeKind.Text, null, "before");
        XmlNode appended = stub.AppendXmlTreeChild(parent, child);
        stub.SetXmlTreeText(child, "after");
        stub.FreezeXmlTree(parent, true);
        XmlRange range = new XmlRange();
        range.StartContainer = parent;
        XmlNodeSelection selection = stub.SelectXmlRange(range);
        XmlEventBatch events = stub.EmitXmlSelectionEvents(selection, new XmlParserOptions());
        XmlCatalogEntry entry = new XmlCatalogEntry();
        RecordingResolver resolver = new RecordingResolver();
        stub.LoadXmlCatalog([entry], resolver, new XmlResolverPolicy());
        XmlResolvedResource resolved = stub.ResolveXmlCatalog(XmlResourceKind.Document, null, null, "urn:test");

        Assert.That(appended, Is.SameAs(child));
        Assert.That(parent.Children, Is.EqualTo(new XmlNode[] { child }));
        Assert.That(child.Value, Is.EqualTo("after"));
        Assert.That(parent.IsReadOnly, Is.True);
        Assert.That(selection.Range, Is.SameAs(range));
        Assert.That(events.Count, Is.EqualTo(1));
        Assert.That(stub.CatalogEntries, Is.EqualTo(new XmlCatalogEntry[] { entry }));
        Assert.That(resolved.AbsoluteUri, Is.EqualTo("urn:test"));
        Assert.That(stub.TryResolveXmlCatalog(XmlResourceKind.Document, null, null, "urn:test"), Is.SameAs(resolved));
        stub.ClearXmlCatalogCache();
        Assert.That(stub.CatalogEntries, Is.Empty);
    }

    private sealed class TreeContractStub : IXmlTreeNavigator, IXmlTreeEditor, IXmlEventEmitter, IXmlCatalogResolver
    {
        internal XmlCatalogEntry[] CatalogEntries = [];
        internal XmlResolvedResource Resource = new XmlResolvedResource();

        public XmlNode? FindXmlParent(XmlNode node)
        {
            return node.Parent;
        }

        public XmlNodeSelection SelectXmlChildren(XmlNode node)
        {
            return CreateSelection(node.Children);
        }

        public XmlNodeSelection SelectXmlAncestors(XmlNode node)
        {
            return CreateSelection([node]);
        }

        public XmlNodeSelection SelectXmlDescendants(XmlNode node)
        {
            return CreateSelection(node.Children);
        }

        public XmlNodeSelection SelectXmlFollowingSiblings(XmlNode node)
        {
            return CreateSelection([]);
        }

        public XmlNodeSelection SelectXmlPrecedingSiblings(XmlNode node)
        {
            return CreateSelection([]);
        }

        public XmlNodeSelection SelectXmlFollowing(XmlNode node)
        {
            return CreateSelection([]);
        }

        public XmlNodeSelection SelectXmlPreceding(XmlNode node)
        {
            return CreateSelection([]);
        }

        public XmlNodeSelection SelectXmlAttributes(XmlNode node)
        {
            return CreateSelection(node.Attributes);
        }

        public XmlNamespaceBinding[] SelectXmlNamespaces(XmlNode node)
        {
            return node.NamespaceBindings ?? [];
        }

        public XmlNodeSelection SelectXmlRange(XmlRange range)
        {
            XmlNodeSelection selection = CreateSelection(range.StartContainer == null ? [] : [range.StartContainer]);
            selection.Range = range;
            return selection;
        }

        public XmlNode? FindXmlNodeById(XmlDocument document, string identifier)
        {
            return document.DocumentElement;
        }

        public string GetXmlNodeStringValue(XmlNode node)
        {
            return node.Value ?? "";
        }

        public string? GetXmlNodeBaseUri(XmlNode node)
        {
            return node.OwnerDocument?.BaseUri;
        }

        public string? GetXmlNodeLanguage(XmlNode node)
        {
            return null;
        }

        public XmlSpaceMode GetXmlNodeSpaceMode(XmlNode node)
        {
            return XmlSpaceMode.Default;
        }

        public int CompareXmlNodeOrder(XmlNode left, XmlNode right)
        {
            return left.Identity.CompareTo(right.Identity);
        }

        public bool ContainsXmlNodeIdentity(XmlNode left, XmlNode right)
        {
            return ReferenceEquals(left, right);
        }

        public XmlNode CreateXmlTreeNode(XmlDocument document, XmlNodeKind kind, XmlName? name, string? value)
        {
            XmlNode node = new XmlNode();
            node.OwnerDocument = document;
            node.Kind = kind;
            node.Name = name;
            node.Value = value;
            return node;
        }

        public XmlNode CloneXmlTreeNode(XmlNode node, bool deep)
        {
            XmlNode clone = new XmlNode();
            clone.Kind = node.Kind;
            clone.Name = node.Name;
            clone.Value = node.Value;
            clone.Children = deep ? node.Children : [];
            return clone;
        }

        public XmlNode ImportXmlTreeNode(XmlDocument document, XmlNode node, bool deep)
        {
            XmlNode imported = CloneXmlTreeNode(node, deep);
            imported.OwnerDocument = document;
            return imported;
        }

        public XmlNode AdoptXmlTreeNode(XmlDocument document, XmlNode node)
        {
            node.OwnerDocument = document;
            return node;
        }

        public XmlNode AppendXmlTreeChild(XmlNode parent, XmlNode child)
        {
            parent.Children = [child];
            child.Parent = parent;
            return child;
        }

        public XmlNode InsertXmlTreeBefore(XmlNode parent, XmlNode node, XmlNode reference)
        {
            parent.Children = [node, reference];
            node.Parent = parent;
            return node;
        }

        public XmlNode ReplaceXmlTreeNode(XmlNode parent, XmlNode replacement, XmlNode existing)
        {
            parent.Children = [replacement];
            replacement.Parent = parent;
            return existing;
        }

        public XmlNode RemoveXmlTreeNode(XmlNode parent, XmlNode child)
        {
            parent.Children = [];
            child.Parent = null;
            return child;
        }

        public void NormalizeXmlTreeNode(XmlNode node)
        {
        }

        public void SetXmlTreeText(XmlNode node, string value)
        {
            node.Value = value;
        }

        public void SetXmlTreeAttribute(XmlNode element, XmlNode attribute)
        {
            element.Attributes = [attribute];
        }

        public XmlNode? RemoveXmlTreeAttribute(XmlNode element, XmlName name)
        {
            XmlNode? removed = element.Attributes == null || element.Attributes.Length == 0 ? null : element.Attributes[0];
            element.Attributes = [];
            return removed;
        }

        public void FreezeXmlTree(XmlNode node, bool deep)
        {
            node.IsReadOnly = true;
        }

        public XmlEventBatch EmitXmlNodeEvents(XmlNode node, XmlParserOptions options)
        {
            XmlEventBatch events = new XmlEventBatch();
            events.Count = 1;
            return events;
        }

        public XmlEventBatch EmitXmlSelectionEvents(XmlNodeSelection selection, XmlParserOptions options)
        {
            XmlEventBatch events = new XmlEventBatch();
            events.Count = selection.Nodes == null ? 0 : selection.Nodes.Length;
            return events;
        }

        public void LoadXmlCatalog(XmlCatalogEntry[] entries, IXmlResourceResolver resolver, XmlResolverPolicy policy)
        {
            CatalogEntries = entries;
        }

        public XmlResolvedResource ResolveXmlCatalog(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? uri)
        {
            Resource.Kind = kind;
            Resource.AbsoluteUri = uri;
            return Resource;
        }

        public XmlResolvedResource? TryResolveXmlCatalog(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? uri)
        {
            return Resource;
        }

        public void ClearXmlCatalogCache()
        {
            CatalogEntries = [];
        }

        private static XmlNodeSelection CreateSelection(XmlNode[]? nodes)
        {
            XmlNodeSelection selection = new XmlNodeSelection();
            selection.Nodes = nodes ?? [];
            return selection;
        }
    }

    private sealed class CoreContractStub : IXmlParser, IXmlErrorReporter, IXmlCancellation, IDtdValidator, IXmlEscaper
    {
        internal XmlInput? Input;
        internal XmlParserOptions? Options;
        internal XmlDiagnostic? Diagnostic;

        public XmlDocument ParseXmlDocument(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation)
        {
            Input = input;
            Options = options;
            XmlDocument document = new XmlDocument();
            document.DocumentUri = "document";
            return document;
        }

        public XmlDocument ParseXmlFragment(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation)
        {
            Input = input;
            Options = options;
            XmlDocument document = new XmlDocument();
            document.DocumentUri = "fragment";
            return document;
        }

        public void ReportXmlDiagnostic(XmlDiagnostic diagnostic)
        {
            Diagnostic = diagnostic;
        }

        public bool IsXmlCancellationRequested()
        {
            return false;
        }

        public XmlDiagnostic[] ValidateXmlDocumentDtd(XmlDocument document, XmlParserOptions options)
        {
            return [];
        }

        public XmlDiagnostic[] ValidateXmlEventDtd(XmlEventBatch events, XmlDocumentType documentType, XmlParserOptions options)
        {
            return [];
        }

        public string EscapeXmlValue(string value, XmlVersion version, XmlCharacterEscapeContext context, XmlInvalidCharacterPolicy policy, char quote)
        {
            return "escaped:" + value;
        }

        public string UnescapeXmlValue(string value, XmlVersion version, XmlCharacterEscapeContext context)
        {
            return value.Substring(8);
        }
    }

    private sealed class StreamingContractStub : IXmlPushParser, IXmlEventHandler, IXmlPullReader
    {
        internal XmlParserStatus Status;
        internal XmlReaderState State = new XmlReaderState();

        public XmlParserStatus BeginXmlPush(XmlInput input, XmlParserOptions options, IXmlEventHandler? handler, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation)
        {
            Status = XmlParserStatus.Running;
            return Status;
        }

        public XmlEventBatch FeedXmlPushBytes(ReadOnlyMemory<byte> bytes, bool isFinal)
        {
            XmlEventBatch result = new XmlEventBatch();
            result.Count = bytes.Length;
            result.IsFinal = isFinal;
            return result;
        }

        public XmlEventBatch FeedXmlPushCharacters(ReadOnlyMemory<char> characters, bool isFinal)
        {
            XmlEventBatch result = new XmlEventBatch();
            result.Count = characters.Length;
            result.IsFinal = isFinal;
            return result;
        }

        public XmlEventBatch FinishXmlPush()
        {
            Status = XmlParserStatus.Completed;
            XmlEventBatch result = new XmlEventBatch();
            result.IsFinal = true;
            result.Status = Status;
            return result;
        }

        public XmlParserStatus PauseXmlPush()
        {
            Status = XmlParserStatus.Paused;
            return Status;
        }

        public XmlParserStatus ResumeXmlPush()
        {
            Status = XmlParserStatus.Running;
            return Status;
        }

        public XmlParserStatus AbortXmlPush()
        {
            Status = XmlParserStatus.Aborted;
            return Status;
        }

        public void ResetXmlPush()
        {
            Status = XmlParserStatus.Ready;
        }

        public void CloseXmlPush()
        {
            Status = XmlParserStatus.Closed;
        }

        public XmlParserStatus InspectXmlPushStatus()
        {
            return Status;
        }

        public XmlHandlerAction HandleXmlEvent(XmlEvent value)
        {
            return XmlHandlerAction.Continue;
        }

        public XmlReaderState BeginXmlPull(XmlInput input, XmlParserOptions options, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation)
        {
            State.Status = XmlParserStatus.Running;
            State.Options = options;
            return State;
        }

        public XmlReaderState AdvanceXmlPull()
        {
            State.Status = XmlParserStatus.Running;
            return State;
        }

        public XmlReaderState InspectXmlPull()
        {
            return State;
        }

        public XmlReaderState SkipXmlPullSubtree()
        {
            State.Status = XmlParserStatus.Completed;
            return State;
        }

        public void CloseXmlPull()
        {
            State.Status = XmlParserStatus.Closed;
        }
    }

    private sealed class RecordingResolver : IXmlResourceResolver
    {
        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            XmlResolvedResource result = new XmlResolvedResource();
            result.Kind = kind;
            result.AbsoluteUri = baseUri;
            return result;
        }
    }
}
