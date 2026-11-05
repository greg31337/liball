using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class CoreDataTests
{
    [Test]
    public void SourceAndDiagnosticDataRoundTrips()
    {
        SourceLocation start = new SourceLocation();
        start.SourceUri = "file:///source.xml";
        start.CharacterOffset = 12;
        start.ByteOffset = 15;
        start.Line = 2;
        start.Column = 4;
        SourceLocation end = new SourceLocation();
        end.SourceUri = start.SourceUri;
        end.CharacterOffset = 18;
        end.ByteOffset = 21;
        end.Line = 2;
        end.Column = 10;
        SourceRange range = new SourceRange();
        range.Start = start;
        range.End = end;
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.InvalidCharacter;
        diagnostic.Category = XmlErrorCategory.Xml;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = "Invalid character.";
        diagnostic.Range = range;
        diagnostic.RelatedLocation = start;
        diagnostic.EntityStack = ["document", "entity"];
        diagnostic.IncludeStack = ["include.xml"];
        diagnostic.TemplateStack = ["main"];

        Assert.That(start.SourceUri, Is.EqualTo("file:///source.xml"));
        Assert.That(start.CharacterOffset, Is.EqualTo(12));
        Assert.That(start.ByteOffset, Is.EqualTo(15));
        Assert.That(start.Line, Is.EqualTo(2));
        Assert.That(start.Column, Is.EqualTo(4));
        Assert.That(range.Start, Is.SameAs(start));
        Assert.That(range.End, Is.SameAs(end));
        Assert.That(diagnostic.Code, Is.EqualTo("XML0001"));
        Assert.That(diagnostic.Category, Is.EqualTo(XmlErrorCategory.Xml));
        Assert.That(diagnostic.Severity, Is.EqualTo(XmlDiagnosticSeverity.Fatal));
        Assert.That(diagnostic.Message, Is.EqualTo("Invalid character."));
        Assert.That(diagnostic.Range, Is.SameAs(range));
        Assert.That(diagnostic.RelatedLocation, Is.SameAs(start));
        Assert.That(diagnostic.EntityStack, Is.EqualTo(new string[] { "document", "entity" }));
        Assert.That(diagnostic.IncludeStack, Is.EqualTo(new string[] { "include.xml" }));
        Assert.That(diagnostic.TemplateStack, Is.EqualTo(new string[] { "main" }));
    }

    [Test]
    public void NameNamespaceAndAttributeDataRoundTrips()
    {
        SourceRange range = new SourceRange();
        XmlName name = new XmlName();
        name.Prefix = "p";
        name.LocalName = "id";
        name.NamespaceUri = "urn:test";
        name.QualifiedName = "p:id";
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = "p";
        binding.NamespaceUri = "urn:test";
        binding.IsUndeclaration = false;
        binding.Range = range;
        XmlAttributeData attribute = new XmlAttributeData();
        attribute.Name = name;
        attribute.Value = "normalized";
        attribute.RawValue = "raw";
        attribute.Quote = '\'';
        attribute.IsSpecified = true;
        attribute.IsDefaulted = false;
        attribute.IsFixed = true;
        attribute.IsNamespaceDeclaration = false;
        attribute.Range = range;

        Assert.That(name.Prefix, Is.EqualTo("p"));
        Assert.That(name.LocalName, Is.EqualTo("id"));
        Assert.That(name.NamespaceUri, Is.EqualTo("urn:test"));
        Assert.That(name.QualifiedName, Is.EqualTo("p:id"));
        Assert.That(binding.Prefix, Is.EqualTo("p"));
        Assert.That(binding.NamespaceUri, Is.EqualTo("urn:test"));
        Assert.That(binding.IsUndeclaration, Is.False);
        Assert.That(binding.Range, Is.SameAs(range));
        Assert.That(attribute.Name, Is.SameAs(name));
        Assert.That(attribute.Value, Is.EqualTo("normalized"));
        Assert.That(attribute.RawValue, Is.EqualTo("raw"));
        Assert.That(attribute.Quote, Is.EqualTo('\''));
        Assert.That(attribute.IsSpecified, Is.True);
        Assert.That(attribute.IsDefaulted, Is.False);
        Assert.That(attribute.IsFixed, Is.True);
        Assert.That(attribute.IsNamespaceDeclaration, Is.False);
        Assert.That(attribute.Range, Is.SameAs(range));
    }

    [Test]
    public void InputOutputAndResolvedResourceDataRoundTrips()
    {
        MemoryStream inputStream = new MemoryStream(new byte[] { 1, 2, 3 });
        StringReader reader = new StringReader("<root/>");
        Encoding encoding = new UTF8Encoding(false, true);
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.ByteMemory;
        input.Text = "<root/>";
        input.Characters = "chars".AsMemory();
        input.Bytes = new byte[] { 1, 2, 3 };
        input.ByteStream = inputStream;
        input.TextReader = reader;
        input.BaseUri = "file:///input.xml";
        input.MediaType = XmlMediaTypes.ApplicationXml;
        input.EncodingName = XmlMediaTypes.Utf8;
        input.Encoding = encoding;
        input.OwnsResource = true;
        MemoryStream outputStream = new MemoryStream();
        StringWriter writer = new StringWriter();
        RecordingByteSink sink = new RecordingByteSink();
        XmlOutput output = new XmlOutput();
        output.Kind = XmlOutputKind.ByteSink;
        output.ByteStream = outputStream;
        output.TextWriter = writer;
        output.ByteSink = sink;
        output.BaseUri = "file:///output.xml";
        output.MediaType = XmlMediaTypes.ApplicationXml;
        output.EncodingName = XmlMediaTypes.Utf8;
        output.Encoding = encoding;
        output.OwnsResource = false;
        XmlResolvedResource resource = new XmlResolvedResource();
        resource.Kind = XmlResourceKind.Document;
        resource.Access = XmlResourceAccess.Allowed;
        resource.Input = input;
        resource.RequestedUri = "document.xml";
        resource.AbsoluteUri = "file:///document.xml";
        resource.PublicIdentifier = "public";
        resource.SystemIdentifier = "system";
        resource.MediaType = XmlMediaTypes.ApplicationXml;
        resource.EncodingName = XmlMediaTypes.Utf8;
        resource.OwnsResource = true;

        Assert.That(input.Kind, Is.EqualTo(XmlInputKind.ByteMemory));
        Assert.That(input.Text, Is.EqualTo("<root/>"));
        Assert.That(input.Characters.ToString(), Is.EqualTo("chars"));
        Assert.That(input.Bytes.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(input.ByteStream, Is.SameAs(inputStream));
        Assert.That(input.TextReader, Is.SameAs(reader));
        Assert.That(input.BaseUri, Is.EqualTo("file:///input.xml"));
        Assert.That(input.MediaType, Is.EqualTo("application/xml"));
        Assert.That(input.EncodingName, Is.EqualTo("UTF-8"));
        Assert.That(input.Encoding, Is.SameAs(encoding));
        Assert.That(input.OwnsResource, Is.True);
        Assert.That(output.Kind, Is.EqualTo(XmlOutputKind.ByteSink));
        Assert.That(output.ByteStream, Is.SameAs(outputStream));
        Assert.That(output.TextWriter, Is.SameAs(writer));
        Assert.That(output.ByteSink, Is.SameAs(sink));
        Assert.That(output.BaseUri, Is.EqualTo("file:///output.xml"));
        Assert.That(output.MediaType, Is.EqualTo("application/xml"));
        Assert.That(output.EncodingName, Is.EqualTo("UTF-8"));
        Assert.That(output.Encoding, Is.SameAs(encoding));
        Assert.That(output.OwnsResource, Is.False);
        Assert.That(resource.Kind, Is.EqualTo(XmlResourceKind.Document));
        Assert.That(resource.Access, Is.EqualTo(XmlResourceAccess.Allowed));
        Assert.That(resource.Input, Is.SameAs(input));
        Assert.That(resource.RequestedUri, Is.EqualTo("document.xml"));
        Assert.That(resource.AbsoluteUri, Is.EqualTo("file:///document.xml"));
        Assert.That(resource.PublicIdentifier, Is.EqualTo("public"));
        Assert.That(resource.SystemIdentifier, Is.EqualTo("system"));
        Assert.That(resource.MediaType, Is.EqualTo("application/xml"));
        Assert.That(resource.EncodingName, Is.EqualTo("UTF-8"));
        Assert.That(resource.OwnsResource, Is.True);
    }

    [Test]
    public void SecurityResolverAndParserOptionDataRoundTrips()
    {
        XmlSecurityLimits limits = new XmlSecurityLimits();
        limits.MaximumDepth = 10;
        limits.MaximumNodeCount = 20;
        limits.MaximumAttributesPerElement = 30;
        limits.MaximumNameLength = 40;
        limits.MaximumTextLength = 50;
        limits.MaximumTokenLength = 60;
        limits.MaximumInputCharacters = 70;
        limits.MaximumInputBytes = 80;
        limits.MaximumDtdCharacters = 90;
        limits.MaximumEntityDepth = 100;
        limits.MaximumEntityCount = 110;
        limits.MaximumEntityCharacters = 120;
        limits.MaximumEntityAmplification = 130;
        limits.MaximumExternalResources = 140;
        limits.MaximumResourceDepth = 150;
        limits.MaximumResourceBytes = 160;
        limits.CancellationPollingInterval = 170;
        XmlResolverPolicy policy = new XmlResolverPolicy();
        policy.Access = XmlResourceAccess.Allowed;
        policy.AllowedSchemes = ["file", "https"];
        policy.AllowedRoots = ["/safe"];
        policy.AllowRedirects = true;
        policy.MaximumRedirects = 3;
        policy.MaximumDepth = 4;
        policy.MaximumResourceBytes = 500;
        policy.MaximumWorkUnits = 600;
        policy.CacheResources = true;
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml11;
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces11;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.EntityHandling = XmlEntityHandling.PreserveReferences;
        options.ValidationMode = XmlValidationMode.Dtd;
        options.ParseFragment = true;
        options.PreserveCData = true;
        options.PreserveEntityReferences = true;
        options.PreserveComments = true;
        options.PreserveProcessingInstructions = true;
        options.PreservePrefixes = true;
        options.PreserveIgnorableWhitespace = true;
        options.PreserveSourceLocations = true;
        options.RetainNamespaceDeclarations = true;
        options.AccumulateValidationDiagnostics = true;
        options.SecurityLimits = limits;
        options.ResolverPolicy = policy;

        Assert.That(limits.MaximumDepth, Is.EqualTo(10));
        Assert.That(limits.MaximumNodeCount, Is.EqualTo(20));
        Assert.That(limits.MaximumAttributesPerElement, Is.EqualTo(30));
        Assert.That(limits.MaximumNameLength, Is.EqualTo(40));
        Assert.That(limits.MaximumTextLength, Is.EqualTo(50));
        Assert.That(limits.MaximumTokenLength, Is.EqualTo(60));
        Assert.That(limits.MaximumInputCharacters, Is.EqualTo(70));
        Assert.That(limits.MaximumInputBytes, Is.EqualTo(80));
        Assert.That(limits.MaximumDtdCharacters, Is.EqualTo(90));
        Assert.That(limits.MaximumEntityDepth, Is.EqualTo(100));
        Assert.That(limits.MaximumEntityCount, Is.EqualTo(110));
        Assert.That(limits.MaximumEntityCharacters, Is.EqualTo(120));
        Assert.That(limits.MaximumEntityAmplification, Is.EqualTo(130));
        Assert.That(limits.MaximumExternalResources, Is.EqualTo(140));
        Assert.That(limits.MaximumResourceDepth, Is.EqualTo(150));
        Assert.That(limits.MaximumResourceBytes, Is.EqualTo(160));
        Assert.That(limits.CancellationPollingInterval, Is.EqualTo(170));
        Assert.That(policy.Access, Is.EqualTo(XmlResourceAccess.Allowed));
        Assert.That(policy.AllowedSchemes, Is.EqualTo(new string[] { "file", "https" }));
        Assert.That(policy.AllowedRoots, Is.EqualTo(new string[] { "/safe" }));
        Assert.That(policy.AllowRedirects, Is.True);
        Assert.That(policy.MaximumRedirects, Is.EqualTo(3));
        Assert.That(policy.MaximumDepth, Is.EqualTo(4));
        Assert.That(policy.MaximumResourceBytes, Is.EqualTo(500));
        Assert.That(policy.MaximumWorkUnits, Is.EqualTo(600));
        Assert.That(policy.CacheResources, Is.True);
        Assert.That(options.Version, Is.EqualTo(XmlVersion.Xml11));
        Assert.That(options.NamespaceHandling, Is.EqualTo(XmlNamespaceHandling.Namespaces11));
        Assert.That(options.DtdProcessing, Is.EqualTo(XmlDtdProcessing.Parse));
        Assert.That(options.EntityHandling, Is.EqualTo(XmlEntityHandling.PreserveReferences));
        Assert.That(options.ValidationMode, Is.EqualTo(XmlValidationMode.Dtd));
        Assert.That(options.ParseFragment, Is.True);
        Assert.That(options.PreserveCData, Is.True);
        Assert.That(options.PreserveEntityReferences, Is.True);
        Assert.That(options.PreserveComments, Is.True);
        Assert.That(options.PreserveProcessingInstructions, Is.True);
        Assert.That(options.PreservePrefixes, Is.True);
        Assert.That(options.PreserveIgnorableWhitespace, Is.True);
        Assert.That(options.PreserveSourceLocations, Is.True);
        Assert.That(options.RetainNamespaceDeclarations, Is.True);
        Assert.That(options.AccumulateValidationDiagnostics, Is.True);
        Assert.That(options.SecurityLimits, Is.SameAs(limits));
        Assert.That(options.ResolverPolicy, Is.SameAs(policy));
    }

    [Test]
    public void EventBatchAndReaderStateDataRoundTrips()
    {
        XmlName name = new XmlName();
        name.LocalName = "root";
        XmlAttributeData attribute = new XmlAttributeData();
        attribute.Value = "value";
        XmlNamespaceBinding binding = new XmlNamespaceBinding();
        binding.Prefix = "p";
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = "TEST";
        SourceRange range = new SourceRange();
        XmlParserOptions options = new XmlParserOptions();
        XmlEvent item = new XmlEvent();
        item.Kind = XmlEventKind.StartElement;
        item.NodeKind = XmlNodeKind.Element;
        item.Name = name;
        item.Value = "text";
        item.Attributes = [attribute];
        item.NamespaceBindings = [binding];
        item.Depth = 2;
        item.IsEmptyElement = true;
        item.IsIgnorableWhitespace = false;
        item.Range = range;
        item.Diagnostic = diagnostic;
        item.PublicIdentifier = "public";
        item.SystemIdentifier = "system";
        XmlReaderState state = new XmlReaderState();
        state.Status = XmlParserStatus.Paused;
        state.Kind = XmlEventKind.StartElement;
        state.NodeKind = XmlNodeKind.Element;
        state.Name = name;
        state.Value = "text";
        state.Attributes = [attribute];
        state.NamespaceBindings = [binding];
        state.Depth = 2;
        state.BaseUri = "file:///source.xml";
        state.Language = "en";
        state.SpaceMode = XmlSpaceMode.Preserve;
        state.IsEmptyElement = true;
        state.Range = range;
        state.Diagnostic = diagnostic;
        state.Options = options;
        XmlEventBatch batch = new XmlEventBatch();
        batch.Events = [item];
        batch.Offset = 0;
        batch.Count = 1;
        batch.Status = XmlParserStatus.Paused;
        batch.IsFinal = false;
        batch.ReaderState = state;

        Assert.That(item.Kind, Is.EqualTo(XmlEventKind.StartElement));
        Assert.That(item.NodeKind, Is.EqualTo(XmlNodeKind.Element));
        Assert.That(item.Name, Is.SameAs(name));
        Assert.That(item.Value, Is.EqualTo("text"));
        Assert.That(item.Attributes, Is.EqualTo(new XmlAttributeData[] { attribute }));
        Assert.That(item.NamespaceBindings, Is.EqualTo(new XmlNamespaceBinding[] { binding }));
        Assert.That(item.Depth, Is.EqualTo(2));
        Assert.That(item.IsEmptyElement, Is.True);
        Assert.That(item.IsIgnorableWhitespace, Is.False);
        Assert.That(item.Range, Is.SameAs(range));
        Assert.That(item.Diagnostic, Is.SameAs(diagnostic));
        Assert.That(item.PublicIdentifier, Is.EqualTo("public"));
        Assert.That(item.SystemIdentifier, Is.EqualTo("system"));
        Assert.That(state.Status, Is.EqualTo(XmlParserStatus.Paused));
        Assert.That(state.Kind, Is.EqualTo(XmlEventKind.StartElement));
        Assert.That(state.NodeKind, Is.EqualTo(XmlNodeKind.Element));
        Assert.That(state.Name, Is.SameAs(name));
        Assert.That(state.Value, Is.EqualTo("text"));
        Assert.That(state.Attributes, Is.EqualTo(new XmlAttributeData[] { attribute }));
        Assert.That(state.NamespaceBindings, Is.EqualTo(new XmlNamespaceBinding[] { binding }));
        Assert.That(state.Depth, Is.EqualTo(2));
        Assert.That(state.BaseUri, Is.EqualTo("file:///source.xml"));
        Assert.That(state.Language, Is.EqualTo("en"));
        Assert.That(state.SpaceMode, Is.EqualTo(XmlSpaceMode.Preserve));
        Assert.That(state.IsEmptyElement, Is.True);
        Assert.That(state.Range, Is.SameAs(range));
        Assert.That(state.Diagnostic, Is.SameAs(diagnostic));
        Assert.That(state.Options, Is.SameAs(options));
        Assert.That(batch.Events, Is.EqualTo(new XmlEvent[] { item }));
        Assert.That(batch.Offset, Is.Zero);
        Assert.That(batch.Count, Is.EqualTo(1));
        Assert.That(batch.Status, Is.EqualTo(XmlParserStatus.Paused));
        Assert.That(batch.IsFinal, Is.False);
        Assert.That(batch.ReaderState, Is.SameAs(state));
    }

    [Test]
    public void ByteSinkContractReceivesMemoryWithoutChangingIt()
    {
        RecordingByteSink sink = new RecordingByteSink();

        sink.WriteXmlBytes(new byte[] { 1, 2, 3 });

        Assert.That(sink.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    private sealed class RecordingByteSink : IXmlByteSink
    {
        internal byte[] Bytes = [];

        public void WriteXmlBytes(ReadOnlyMemory<byte> bytes)
        {
            Bytes = bytes.ToArray();
        }
    }
}
