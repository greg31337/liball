using xml;

namespace xml.Tests;

[TestFixture]
public class TreeAndOutputDataTests
{
    [Test]
    public void TreeDocumentSelectionAndRangeDataRoundTrips()
    {
        XmlName rootName = new XmlName();
        rootName.LocalName = "root";
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = "p";
        SourceRange sourceRange = new SourceRange();
        XmlNode documentNode = new XmlNode();
        XmlNode attribute = new XmlNode();
        attribute.Kind = XmlNodeKind.Attribute;
        XmlNode child = new XmlNode();
        child.Kind = XmlNodeKind.Text;
        XmlNode root = new XmlNode();
        XmlDocument document = new XmlDocument();
        root.Kind = XmlNodeKind.Element;
        root.Name = rootName;
        root.Value = "value";
        root.Parent = documentNode;
        root.OwnerDocument = document;
        root.Children = [child];
        root.Attributes = [attribute];
        root.NamespaceBindings = [binding];
        root.Range = sourceRange;
        root.Identity = 42;
        root.MutationVersion = 7;
        root.IsReadOnly = true;
        XmlDocumentType documentType = new XmlDocumentType();
        document.DocumentNode = documentNode;
        document.DocumentElement = root;
        document.DocumentType = documentType;
        document.Version = XmlVersion.Xml11;
        document.Standalone = XmlStandalone.Yes;
        document.DocumentUri = "file:///document.xml";
        document.BaseUri = "file:///";
        document.EncodingName = XmlMediaTypes.Utf8;
        document.Diagnostics = [new XmlDiagnostic()];
        document.MutationVersion = 7;
        document.IsReadOnly = true;
        XmlNodeSelection selection = new XmlNodeSelection();
        XmlRange range = new XmlRange();
        selection.Document = document;
        selection.Nodes = [root, child];
        selection.Range = range;
        selection.MutationVersion = 7;
        selection.IsFrozen = true;
        range.Document = document;
        range.StartContainer = root;
        range.StartOffset = 0;
        range.EndContainer = root;
        range.EndOffset = 1;
        range.Snapshot = selection;
        range.MutationVersion = 7;
        range.IsValid = true;

        Assert.That(root.Kind, Is.EqualTo(XmlNodeKind.Element));
        Assert.That(root.Name, Is.SameAs(rootName));
        Assert.That(root.Value, Is.EqualTo("value"));
        Assert.That(root.Parent, Is.SameAs(documentNode));
        Assert.That(root.OwnerDocument, Is.SameAs(document));
        Assert.That(root.Children, Is.EqualTo(new XmlNode[] { child }));
        Assert.That(root.Attributes, Is.EqualTo(new XmlNode[] { attribute }));
        Assert.That(root.NamespaceBindings, Is.EqualTo(new XmlNamespaceBinding[] { binding }));
        Assert.That(root.Range, Is.SameAs(sourceRange));
        Assert.That(root.Identity, Is.EqualTo(42));
        Assert.That(root.MutationVersion, Is.EqualTo(7));
        Assert.That(root.IsReadOnly, Is.True);
        Assert.That(document.DocumentNode, Is.SameAs(documentNode));
        Assert.That(document.DocumentElement, Is.SameAs(root));
        Assert.That(document.DocumentType, Is.SameAs(documentType));
        Assert.That(document.Version, Is.EqualTo(XmlVersion.Xml11));
        Assert.That(document.Standalone, Is.EqualTo(XmlStandalone.Yes));
        Assert.That(document.DocumentUri, Is.EqualTo("file:///document.xml"));
        Assert.That(document.BaseUri, Is.EqualTo("file:///"));
        Assert.That(document.EncodingName, Is.EqualTo("UTF-8"));
        Assert.That(document.Diagnostics, Has.Length.EqualTo(1));
        Assert.That(document.MutationVersion, Is.EqualTo(7));
        Assert.That(document.IsReadOnly, Is.True);
        Assert.That(selection.Document, Is.SameAs(document));
        Assert.That(selection.Nodes, Is.EqualTo(new XmlNode[] { root, child }));
        Assert.That(selection.Range, Is.SameAs(range));
        Assert.That(selection.MutationVersion, Is.EqualTo(7));
        Assert.That(selection.IsFrozen, Is.True);
        Assert.That(range.Document, Is.SameAs(document));
        Assert.That(range.StartContainer, Is.SameAs(root));
        Assert.That(range.StartOffset, Is.Zero);
        Assert.That(range.EndContainer, Is.SameAs(root));
        Assert.That(range.EndOffset, Is.EqualTo(1));
        Assert.That(range.Snapshot, Is.SameAs(selection));
        Assert.That(range.MutationVersion, Is.EqualTo(7));
        Assert.That(range.IsValid, Is.True);
    }

    [Test]
    public void RetainedDtdDataRoundTrips()
    {
        XmlName rootName = new XmlName();
        rootName.LocalName = "root";
        SourceRange range = new SourceRange();
        XmlElementDeclaration element = new XmlElementDeclaration();
        element.Name = rootName;
        element.ContentModel = "(#PCDATA|child)*";
        element.ReferencedNames = ["child"];
        element.IsEmpty = false;
        element.IsAny = false;
        element.IsMixed = true;
        element.Range = range;
        XmlAttributeDeclaration attribute = new XmlAttributeDeclaration();
        attribute.ElementName = rootName;
        attribute.Name = new XmlName();
        attribute.TypeName = "ID";
        attribute.AllowedValues = ["one", "two"];
        attribute.DefaultKind = "#FIXED";
        attribute.DefaultValue = "one";
        attribute.IsId = true;
        attribute.IsNamespaceDeclaration = false;
        attribute.Range = range;
        XmlEntityDeclaration entity = new XmlEntityDeclaration();
        entity.Name = "logo";
        entity.ReplacementText = "replacement";
        entity.PublicIdentifier = "public";
        entity.SystemIdentifier = "logo.svg";
        entity.NotationName = "svg";
        entity.BaseUri = "file:///";
        entity.IsParameter = false;
        entity.IsExternal = true;
        entity.IsParsed = false;
        entity.Range = range;
        XmlNotationDeclaration notation = new XmlNotationDeclaration();
        notation.Name = "svg";
        notation.PublicIdentifier = "image/svg+xml";
        notation.SystemIdentifier = "viewer";
        notation.BaseUri = "file:///";
        notation.Range = range;
        XmlDocumentType documentType = new XmlDocumentType();
        documentType.Name = rootName;
        documentType.PublicIdentifier = "public";
        documentType.SystemIdentifier = "document.dtd";
        documentType.InternalSubset = "<!ELEMENT root ANY>";
        documentType.BaseUri = "file:///";
        documentType.ElementDeclarations = [element];
        documentType.AttributeDeclarations = [attribute];
        documentType.EntityDeclarations = [entity];
        documentType.NotationDeclarations = [notation];
        documentType.Range = range;

        Assert.That(element.Name, Is.SameAs(rootName));
        Assert.That(element.ContentModel, Is.EqualTo("(#PCDATA|child)*"));
        Assert.That(element.ReferencedNames, Is.EqualTo(new string[] { "child" }));
        Assert.That(element.IsEmpty, Is.False);
        Assert.That(element.IsAny, Is.False);
        Assert.That(element.IsMixed, Is.True);
        Assert.That(element.Range, Is.SameAs(range));
        Assert.That(attribute.ElementName, Is.SameAs(rootName));
        Assert.That(attribute.Name, Is.Not.Null);
        Assert.That(attribute.TypeName, Is.EqualTo("ID"));
        Assert.That(attribute.AllowedValues, Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(attribute.DefaultKind, Is.EqualTo("#FIXED"));
        Assert.That(attribute.DefaultValue, Is.EqualTo("one"));
        Assert.That(attribute.IsId, Is.True);
        Assert.That(attribute.IsNamespaceDeclaration, Is.False);
        Assert.That(attribute.Range, Is.SameAs(range));
        Assert.That(entity.Name, Is.EqualTo("logo"));
        Assert.That(entity.ReplacementText, Is.EqualTo("replacement"));
        Assert.That(entity.PublicIdentifier, Is.EqualTo("public"));
        Assert.That(entity.SystemIdentifier, Is.EqualTo("logo.svg"));
        Assert.That(entity.NotationName, Is.EqualTo("svg"));
        Assert.That(entity.BaseUri, Is.EqualTo("file:///"));
        Assert.That(entity.IsParameter, Is.False);
        Assert.That(entity.IsExternal, Is.True);
        Assert.That(entity.IsParsed, Is.False);
        Assert.That(entity.Range, Is.SameAs(range));
        Assert.That(notation.Name, Is.EqualTo("svg"));
        Assert.That(notation.PublicIdentifier, Is.EqualTo("image/svg+xml"));
        Assert.That(notation.SystemIdentifier, Is.EqualTo("viewer"));
        Assert.That(notation.BaseUri, Is.EqualTo("file:///"));
        Assert.That(notation.Range, Is.SameAs(range));
        Assert.That(documentType.Name, Is.SameAs(rootName));
        Assert.That(documentType.PublicIdentifier, Is.EqualTo("public"));
        Assert.That(documentType.SystemIdentifier, Is.EqualTo("document.dtd"));
        Assert.That(documentType.InternalSubset, Is.EqualTo("<!ELEMENT root ANY>"));
        Assert.That(documentType.BaseUri, Is.EqualTo("file:///"));
        Assert.That(documentType.ElementDeclarations, Is.EqualTo(new XmlElementDeclaration[] { element }));
        Assert.That(documentType.AttributeDeclarations, Is.EqualTo(new XmlAttributeDeclaration[] { attribute }));
        Assert.That(documentType.EntityDeclarations, Is.EqualTo(new XmlEntityDeclaration[] { entity }));
        Assert.That(documentType.NotationDeclarations, Is.EqualTo(new XmlNotationDeclaration[] { notation }));
        Assert.That(documentType.Range, Is.SameAs(range));
    }

    [Test]
    public void SerializationAndCanonicalizationDataRoundTrips()
    {
        XmlName methodName = new XmlName();
        methodName.LocalName = "custom";
        XmlName elementName = new XmlName();
        elementName.LocalName = "script";
        XmlCharacterMapEntry map = new XmlCharacterMapEntry();
        map.CodePoint = 0x3C;
        map.Replacement = "&lt;";
        map.MapName = methodName;
        XmlSerializationOptions serialization = new XmlSerializationOptions();
        serialization.Method = XmlSerializationMethod.Extension;
        serialization.ExtensionMethodName = methodName;
        serialization.AllowDuplicateNames = true;
        serialization.ByteOrderMark = true;
        serialization.CDataSectionElements = [elementName];
        serialization.DoctypePublic = "public";
        serialization.DoctypeSystem = "system";
        serialization.Encoding = XmlMediaTypes.Utf8;
        serialization.EscapeUriAttributes = true;
        serialization.HtmlVersion = "5";
        serialization.IncludeContentType = true;
        serialization.Indent = true;
        serialization.ItemSeparator = "|";
        serialization.JsonNodeOutputMethod = XmlSerializationMethod.Xml;
        serialization.MediaType = "application/custom";
        serialization.NormalizationForm = "NFC";
        serialization.OmitXmlDeclaration = true;
        serialization.Standalone = XmlStandalone.No;
        serialization.SuppressIndentation = [elementName];
        serialization.UndeclarePrefixes = true;
        serialization.UseCharacterMaps = [map];
        serialization.Version = "1.1";
        serialization.InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Replace;
        serialization.InvalidCharacterReplacement = "?";
        serialization.NewLine = "\n";
        XmlNodeSelection selection = new XmlNodeSelection();
        XmlCanonicalizationOptions canonicalization = new XmlCanonicalizationOptions();
        canonicalization.Method = XmlCanonicalizationMethod.ExclusiveXml10;
        canonicalization.IncludeComments = true;
        canonicalization.Selection = selection;
        canonicalization.InclusivePrefixes = ["p", "#default"];
        canonicalization.PropagateDefaultNamespace = true;
        canonicalization.RewriteUris = true;

        Assert.That(map.CodePoint, Is.EqualTo(0x3C));
        Assert.That(map.Replacement, Is.EqualTo("&lt;"));
        Assert.That(map.MapName, Is.SameAs(methodName));
        Assert.That(serialization.Method, Is.EqualTo(XmlSerializationMethod.Extension));
        Assert.That(serialization.ExtensionMethodName, Is.SameAs(methodName));
        Assert.That(serialization.AllowDuplicateNames, Is.True);
        Assert.That(serialization.ByteOrderMark, Is.True);
        Assert.That(serialization.CDataSectionElements, Is.EqualTo(new XmlName[] { elementName }));
        Assert.That(serialization.DoctypePublic, Is.EqualTo("public"));
        Assert.That(serialization.DoctypeSystem, Is.EqualTo("system"));
        Assert.That(serialization.Encoding, Is.EqualTo("UTF-8"));
        Assert.That(serialization.EscapeUriAttributes, Is.True);
        Assert.That(serialization.HtmlVersion, Is.EqualTo("5"));
        Assert.That(serialization.IncludeContentType, Is.True);
        Assert.That(serialization.Indent, Is.True);
        Assert.That(serialization.ItemSeparator, Is.EqualTo("|"));
        Assert.That(serialization.JsonNodeOutputMethod, Is.EqualTo(XmlSerializationMethod.Xml));
        Assert.That(serialization.MediaType, Is.EqualTo("application/custom"));
        Assert.That(serialization.NormalizationForm, Is.EqualTo("NFC"));
        Assert.That(serialization.OmitXmlDeclaration, Is.True);
        Assert.That(serialization.Standalone, Is.EqualTo(XmlStandalone.No));
        Assert.That(serialization.SuppressIndentation, Is.EqualTo(new XmlName[] { elementName }));
        Assert.That(serialization.UndeclarePrefixes, Is.True);
        Assert.That(serialization.UseCharacterMaps, Is.EqualTo(new XmlCharacterMapEntry[] { map }));
        Assert.That(serialization.Version, Is.EqualTo("1.1"));
        Assert.That(serialization.InvalidCharacterPolicy, Is.EqualTo(XmlInvalidCharacterPolicy.Replace));
        Assert.That(serialization.InvalidCharacterReplacement, Is.EqualTo("?"));
        Assert.That(serialization.NewLine, Is.EqualTo("\n"));
        Assert.That(canonicalization.Method, Is.EqualTo(XmlCanonicalizationMethod.ExclusiveXml10));
        Assert.That(canonicalization.IncludeComments, Is.True);
        Assert.That(canonicalization.Selection, Is.SameAs(selection));
        Assert.That(canonicalization.InclusivePrefixes, Is.EqualTo(new string[] { "p", "#default" }));
        Assert.That(canonicalization.PropagateDefaultNamespace, Is.True);
        Assert.That(canonicalization.RewriteUris, Is.True);
    }

    [Test]
    public void CatalogAndIncludeDataRoundTrips()
    {
        SourceRange range = new SourceRange();
        XmlCatalogEntry child = new XmlCatalogEntry();
        child.Kind = XmlCatalogEntryKind.Uri;
        XmlCatalogEntry catalog = new XmlCatalogEntry();
        catalog.Kind = XmlCatalogEntryKind.Group;
        catalog.PublicIdentifier = "public";
        catalog.SystemIdentifier = "system";
        catalog.Uri = "urn:source";
        catalog.TargetUri = "file:///target.xml";
        catalog.RewritePrefix = "file:///safe/";
        catalog.Suffix = ".dtd";
        catalog.CatalogUri = "next.xml";
        catalog.BaseUri = "file:///catalog.xml";
        catalog.PreferPublic = true;
        catalog.Entries = [child];
        catalog.Range = range;
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.CatalogEntries = [catalog];
        XmlIncludeOptions include = new XmlIncludeOptions();
        include.ProcessNestedIncludes = true;
        include.FixupBaseUri = true;
        include.FixupLanguage = true;
        include.PreserveFallbackNodes = true;
        include.MaximumDepth = 8;
        include.MaximumResourceBytes = 1024;
        include.Accept = "application/xml";
        include.AcceptLanguage = "en";
        include.TextEncoding = XmlMediaTypes.Utf8;
        include.ResolverPolicy = policy;
        include.CatalogEntries = [catalog];

        Assert.That(catalog.Kind, Is.EqualTo(XmlCatalogEntryKind.Group));
        Assert.That(catalog.PublicIdentifier, Is.EqualTo("public"));
        Assert.That(catalog.SystemIdentifier, Is.EqualTo("system"));
        Assert.That(catalog.Uri, Is.EqualTo("urn:source"));
        Assert.That(catalog.TargetUri, Is.EqualTo("file:///target.xml"));
        Assert.That(catalog.RewritePrefix, Is.EqualTo("file:///safe/"));
        Assert.That(catalog.Suffix, Is.EqualTo(".dtd"));
        Assert.That(catalog.CatalogUri, Is.EqualTo("next.xml"));
        Assert.That(catalog.BaseUri, Is.EqualTo("file:///catalog.xml"));
        Assert.That(catalog.PreferPublic, Is.True);
        Assert.That(catalog.Entries, Is.EqualTo(new XmlCatalogEntry[] { child }));
        Assert.That(catalog.Range, Is.SameAs(range));
        Assert.That(policy.CatalogEntries, Is.EqualTo(new XmlCatalogEntry[] { catalog }));
        Assert.That(include.ProcessNestedIncludes, Is.True);
        Assert.That(include.FixupBaseUri, Is.True);
        Assert.That(include.FixupLanguage, Is.True);
        Assert.That(include.PreserveFallbackNodes, Is.True);
        Assert.That(include.MaximumDepth, Is.EqualTo(8));
        Assert.That(include.MaximumResourceBytes, Is.EqualTo(1024));
        Assert.That(include.Accept, Is.EqualTo("application/xml"));
        Assert.That(include.AcceptLanguage, Is.EqualTo("en"));
        Assert.That(include.TextEncoding, Is.EqualTo("UTF-8"));
        Assert.That(include.ResolverPolicy, Is.SameAs(policy));
        Assert.That(include.CatalogEntries, Is.EqualTo(new XmlCatalogEntry[] { catalog }));
    }

    [Test]
    public void PlannedContractsCarryNewDataObjects()
    {
        XmlDocument document = new XmlDocument();
        XmlOutput output = new XmlOutput();
        XmlEventBatch events = new XmlEventBatch();
        XmlNode node = new XmlNode();
        XdmSequence sequence = new XdmSequence();
        XmlSerializationOptions serialization = new XmlSerializationOptions();
        RecordingSerializer serializer = new RecordingSerializer();
        serializer.SerializeXmlDocument(document, output, serialization);
        Assert.That(serializer.Document, Is.SameAs(document));
        serializer.SerializeXmlNode(node, output, serialization);
        Assert.That(serializer.Node, Is.SameAs(node));
        serializer.SerializeXdmSequence(sequence, output, serialization);
        Assert.That(serializer.Sequence, Is.SameAs(sequence));
        serializer.SerializeXmlEvents(events, output, serialization);
        Assert.That(serializer.Events, Is.SameAs(events));
        Assert.That(serializer.Output, Is.SameAs(output));
        Assert.That(serializer.Options, Is.SameAs(serialization));
        XmlCanonicalizationOptions canonicalization = new XmlCanonicalizationOptions();
        RecordingCanonicalizer canonicalizer = new RecordingCanonicalizer();
        canonicalizer.CanonicalizeXml(document, output, canonicalization);
        Assert.That(canonicalizer.Document, Is.SameAs(document));
        canonicalizer.CanonicalizeXmlEvents(events, output, canonicalization);
        Assert.That(canonicalizer.Events, Is.SameAs(events));
        Assert.That(canonicalizer.Output, Is.SameAs(output));
        Assert.That(canonicalizer.Options, Is.SameAs(canonicalization));
        XmlIncludeOptions includeOptions = new XmlIncludeOptions();
        RecordingResolver resolver = new RecordingResolver();
        RecordingIncludeProcessor include = new RecordingIncludeProcessor();
        Assert.That(include.ProcessXmlIncludes(document, includeOptions, resolver), Is.SameAs(document));
        Assert.That(include.Options, Is.SameAs(includeOptions));
        Assert.That(include.Resolver, Is.SameAs(resolver));
    }

    private sealed class RecordingSerializer : IXmlSerializer
    {
        internal XmlDocument? Document;
        internal XmlNode? Node;
        internal XdmSequence? Sequence;
        internal XmlEventBatch? Events;
        internal XmlOutput? Output;
        internal XmlSerializationOptions? Options;

        public void SerializeXmlDocument(XmlDocument document, XmlOutput output, XmlSerializationOptions options)
        {
            Document = document;
            Output = output;
            Options = options;
        }

        public void SerializeXmlNode(XmlNode node, XmlOutput output, XmlSerializationOptions options)
        {
            Node = node;
            Output = output;
            Options = options;
        }

        public void SerializeXdmSequence(XdmSequence sequence, XmlOutput output, XmlSerializationOptions options)
        {
            Sequence = sequence;
            Output = output;
            Options = options;
        }

        public void SerializeXmlEvents(XmlEventBatch events, XmlOutput output, XmlSerializationOptions options)
        {
            Events = events;
            Output = output;
            Options = options;
        }
    }

    private sealed class RecordingCanonicalizer : IXmlCanonicalizer
    {
        internal XmlDocument? Document;
        internal XmlEventBatch? Events;
        internal XmlOutput? Output;
        internal XmlCanonicalizationOptions? Options;

        public void CanonicalizeXml(XmlDocument document, XmlOutput output, XmlCanonicalizationOptions options)
        {
            Document = document;
            Output = output;
            Options = options;
        }

        public void CanonicalizeXmlEvents(XmlEventBatch events, XmlOutput output, XmlCanonicalizationOptions options)
        {
            Events = events;
            Output = output;
            Options = options;
        }
    }

    private sealed class RecordingIncludeProcessor : IXmlIncludeProcessor
    {
        internal XmlIncludeOptions? Options;
        internal IXmlResourceResolver? Resolver;

        public XmlDocument ProcessXmlIncludes(XmlDocument document, XmlIncludeOptions options, IXmlResourceResolver resolver)
        {
            Options = options;
            Resolver = resolver;
            return document;
        }
    }

    private sealed class RecordingResolver : IXmlResourceResolver
    {
        public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
        {
            XmlResolvedResource resource = new XmlResolvedResource();
            resource.Kind = kind;
            resource.PublicIdentifier = publicIdentifier;
            resource.SystemIdentifier = systemIdentifier;
            resource.AbsoluteUri = baseUri;
            return resource;
        }
    }
}
