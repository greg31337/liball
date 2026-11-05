using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class GenericXmlApiRoundTripTests
{
    [Test]
    public void PreserveModeRoundTripsNamespaceAttributeAndMixedContentOrderNilAndComments()
    {
        string xml = "<r:Root xmlns:r='urn:root' xmlns:a='urn:attributes' xmlns:xsi='" + Iso20022Namespaces.XmlSchemaInstance + "' plain='one' a:z='two' a:a='three'>first<![CDATA[raw<value]]><!--inside--><r:Nil xsi:nil='1'/><r:Full></r:Full>last</r:Root>";
        MessageReadOptions readOptions = PreserveReadOptions();
        IXmlMessageReader reader = new XmlMessageReader();
        IXmlMessageWriter writer = new XmlMessageWriter();

        MessageReadResult firstRead = reader.ReadGenericText(xml, readOptions);
        DynamicXmlNode firstRoot = RequiredRoot(firstRead);
        MessageWriteResult write = writer.WriteGenericString(firstRead.Document, PreserveWriteOptions());
        MessageReadResult secondRead = reader.ReadGenericText(write.Text, PreserveReadOptions());
        DynamicXmlNode secondRoot = RequiredRoot(secondRead);

        Assert.Multiple(() =>
        {
            Assert.That(firstRoot.Name.Prefix, Is.EqualTo("r"));
            Assert.That(firstRoot.NamespaceDeclarations.Select(value => value.Prefix), Is.EqualTo(new[] { "r", "a", "xsi" }));
            Assert.That(firstRoot.Attributes.Select(value => value.Name.LocalName), Is.EqualTo(new[] { "plain", "z", "a" }));
            Assert.That(firstRoot.Children.Select(value => value.Kind), Is.EqualTo(new[]
            {
                XmlNodeKind.Text,
                XmlNodeKind.CData,
                XmlNodeKind.Comment,
                XmlNodeKind.Element,
                XmlNodeKind.Element,
                XmlNodeKind.Text
            }));
            Assert.That(firstRoot.Children[3].IsNil, Is.True);
            Assert.That(firstRoot.Children[3].Attributes[0].Value, Is.EqualTo("1"));
            Assert.That(firstRoot.Children[3].IsEmptyElement, Is.True);
            Assert.That(firstRoot.Children[4].IsEmptyElement, Is.False);
            Assert.That(write.Success, Is.True);
            Assert.That(write.Text, Does.Contain("a:z=\"two\" a:a=\"three\""));
            Assert.That(write.Text, Does.Contain("<![CDATA[raw<value]]>"));
            Assert.That(write.Text, Does.Contain("<!--inside-->"));
            Assert.That(write.Text, Does.Contain("xsi:nil=\"1\""));
            Assert.That(write.Text, Does.Contain("<r:Full></r:Full>"));
        });
        AssertEquivalentNode(firstRoot, secondRoot);
    }

    [Test]
    public void PrefixShadowingDefaultNamespacesAndQualifiedAttributesRoundTripSemantically()
    {
        string xml = "<p:Root xmlns:p='urn:outer' xmlns:q='urn:attributes' q:b='2'><p:Outer/><p:Child xmlns:p='urn:inner' q:a='1'><Item xmlns='urn:default'/><Item xmlns=''/></p:Child></p:Root>";
        IXmlMessageReader reader = new XmlMessageReader();
        IXmlMessageWriter writer = new XmlMessageWriter();

        MessageReadResult first = reader.ReadGenericText(xml, PreserveReadOptions());
        MessageWriteResult write = writer.WriteGenericString(first.Document, PreserveWriteOptions());
        MessageReadResult second = reader.ReadGenericText(write.Text, PreserveReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(write.Success, Is.True);
            Assert.That(write.Text, Does.Contain("xmlns:p=\"urn:outer\""));
            Assert.That(write.Text, Does.Contain("<p:Child xmlns:p=\"urn:inner\""));
            Assert.That(write.Text, Does.Contain("q:a=\"1\""));
            Assert.That(write.Text, Does.Contain("xmlns=\"urn:default\""));
            Assert.That(write.Text, Does.Contain("xmlns=\"\""));
        });
        AssertEquivalentNode(RequiredRoot(first), RequiredRoot(second));
    }

    [Test]
    public void CommentProcessingInstructionAndWhitespacePoliciesAreObservableThroughPublicApi()
    {
        string xml = "<Root> \n<!--comment--><?target value?><Child/> \n</Root>";
        MessageReadOptions preserve = PreserveReadOptions();
        MessageReadOptions filter = new MessageReadOptions
        {
            PreserveComments = false,
            PreserveProcessingInstructions = false,
            PreserveWhitespace = false
        };
        IXmlMessageReader reader = new XmlMessageReader();

        DynamicXmlNode preserved = RequiredRoot(reader.ReadGenericText(xml, preserve));
        DynamicXmlNode filtered = RequiredRoot(reader.ReadGenericText(xml, filter));

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Children.Select(value => value.Kind), Is.EqualTo(new[]
            {
                XmlNodeKind.Text,
                XmlNodeKind.Comment,
                XmlNodeKind.ProcessingInstruction,
                XmlNodeKind.Element,
                XmlNodeKind.Text
            }));
            Assert.That(filtered.Children, Has.Count.EqualTo(1));
            Assert.That(filtered.Children[0].Kind, Is.EqualTo(XmlNodeKind.Element));
        });
    }

    [Test]
    public void RawFragmentTextAndUtf8BytesUseNamespaceClosureAndRespectUnknownContentPolicy()
    {
        RawXmlFragment textFragment = CreateRawFragment("<ext:Extension ext:id='17'>before<!--inside--><ext:Child/></ext:Extension>");
        RawXmlFragment byteFragment = CreateRawFragment(string.Empty);
        byteFragment.Utf8Bytes = Encoding.UTF8.GetBytes("<ext:Extension ext:id='18'><ext:Child/></ext:Extension>");
        MessageDocument textDocument = RawDocument(textFragment);
        MessageDocument byteDocument = RawDocument(byteFragment);
        IXmlMessageWriter writer = new XmlMessageWriter();
        IXmlMessageReader reader = new XmlMessageReader();

        MessageWriteResult textWrite = writer.WriteGenericString(textDocument, PreserveWriteOptions());
        MessageWriteResult byteWrite = writer.WriteGenericString(byteDocument, PreserveWriteOptions());
        MessageReadResult read = reader.ReadGenericText(textWrite.Text, PreserveReadOptions());
        DynamicXmlNode root = RequiredRoot(read);
        MessageWriteOptions rejectingOptions = PreserveWriteOptions();
        rejectingOptions.UnknownContentMode = UnknownContentMode.Reject;
        MessageWriteOptions ignoringOptions = PreserveWriteOptions();
        ignoringOptions.UnknownContentMode = UnknownContentMode.Ignore;
        MessageWriteOptions boundaryOptions = PreserveWriteOptions();
        boundaryOptions.Limits.MaxDepth = 2;
        boundaryOptions.Limits.MaxElementCount = 2;
        boundaryOptions.Limits.MaxAttributesPerElement = 2;

        XmlException? rejectException = Assert.Throws<XmlException>(() => writer.WriteGenericString(textDocument, rejectingOptions));
        MessageWriteResult ignored = writer.WriteGenericString(textDocument, ignoringOptions);
        MessageWriteResult boundary = writer.WriteGenericString(textDocument, boundaryOptions);

        Assert.Multiple(() =>
        {
            Assert.That(textWrite.Success, Is.True);
            Assert.That(textWrite.Text, Does.Contain("xmlns:ext=\"urn:extension\""));
            Assert.That(textWrite.Text, Does.Contain("ext:id=\"17\""));
            Assert.That(textWrite.Text, Does.Contain("<!--inside-->"));
            Assert.That(byteWrite.Success, Is.True);
            Assert.That(byteWrite.Text, Does.Contain("ext:id=\"18\""));
            Assert.That(root.Name.NamespaceUri, Is.EqualTo("urn:extension"));
            Assert.That(root.Name.Prefix, Is.EqualTo("ext"));
            Assert.That(root.Attributes[0].Name.NamespaceUri, Is.EqualTo("urn:extension"));
            Assert.That(rejectException?.Message, Does.Contain("reject"));
            Assert.That(ignored.Success, Is.False);
            Assert.That(ignored.ErrorCode, Is.EqualTo("writer.content_missing"));
            Assert.That(boundary.Success, Is.True);
            Assert.That(textFragment.NamespaceDeclarations[0].NamespaceUri, Is.EqualTo("urn:extension"));
        });
    }

    [Test]
    public void RawFragmentRootIdentityMismatchIsRejectedBeforeOutput()
    {
        RawXmlFragment fragment = CreateRawFragment("<ext:Different/>");
        IXmlMessageWriter writer = new XmlMessageWriter();

        XmlException? exception = Assert.Throws<XmlException>(() => writer.WriteGenericString(RawDocument(fragment), PreserveWriteOptions()));

        Assert.That(exception?.Message, Does.Contain("root name"));
    }

    [Test]
    public void CanonicalOutputIsPrefixOrderCdataAndEmptyStyleIndependentAndIdempotent()
    {
        string firstXml = "<z:Root xmlns:z='urn:z' xmlns:a='urn:a' a:b='2' plain='1'><z:Empty/><![CDATA[value<]]><!--same--></z:Root>";
        string secondXml = "<other:Root xmlns:other='urn:z' xmlns:q='urn:a' plain='1' q:b='2'><other:Empty></other:Empty>value&lt;<!--same--></other:Root>";
        MessageReadOptions readOptions = PreserveReadOptions();
        MessageWriteOptions canonicalOptions = new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Canonical,
            WriteXmlDeclaration = true,
            WriteByteOrderMark = true
        };
        IXmlMessageReader reader = new XmlMessageReader();
        IXmlMessageWriter writer = new XmlMessageWriter();

        MessageWriteResult first = writer.WriteGenericString(reader.ReadGenericText(firstXml, readOptions).Document, canonicalOptions);
        MessageWriteResult second = writer.WriteGenericString(reader.ReadGenericText(secondXml, readOptions).Document, canonicalOptions);
        MessageReadResult canonicalRead = reader.ReadGenericText(first.Text, PreserveReadOptions());
        MessageWriteResult repeated = writer.WriteGenericString(canonicalRead.Document, canonicalOptions);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(first.Text, Is.EqualTo(second.Text));
            Assert.That(repeated.Text, Is.EqualTo(first.Text));
            Assert.That(first.Text, Does.Not.StartWith("<?xml"));
            Assert.That(first.Text, Does.Not.Contain("<![CDATA["));
            Assert.That(first.Text, Does.Contain("<n1:Empty></n1:Empty>"));
            Assert.That(first.Text, Does.Contain("plain=\"1\" n0:b=\"2\""));
            Assert.That(first.Text, Does.Contain("<!--same-->"));
        });
    }

    [Test]
    public void AllWriterTargetsProduceEquivalentGenericRoundTrips()
    {
        string xml = "<r:Root xmlns:r='urn:root'><r:Child id='17'>value</r:Child></r:Root>";
        MessageDocument? document = new XmlMessageReader().ReadGenericText(xml, PreserveReadOptions()).Document;
        MessageWriteOptions options = PreserveWriteOptions();
        IXmlMessageWriter writer = new XmlMessageWriter();
        MemoryStream stream = new MemoryStream();
        StringWriter textWriter = new StringWriter();
        ArrayBufferWriter<byte> buffer = new ArrayBufferWriter<byte>();

        MessageWriteResult stringResult = writer.WriteGenericString(document, options);
        MessageWriteResult streamResult = writer.WriteGenericStream(document, stream, options);
        MessageWriteResult textResult = writer.WriteGenericTextWriter(document, textWriter, options);
        MessageWriteResult bufferResult = writer.WriteGenericBuffer(document, buffer, options);
        string streamText = Encoding.UTF8.GetString(stream.ToArray());
        string bufferText = Encoding.UTF8.GetString(buffer.WrittenSpan);

        Assert.Multiple(() =>
        {
            Assert.That(stringResult.Success, Is.True);
            Assert.That(streamResult.Success, Is.True);
            Assert.That(textResult.Success, Is.True);
            Assert.That(bufferResult.Success, Is.True);
            Assert.That(streamText, Is.EqualTo(stringResult.Text));
            Assert.That(textWriter.ToString(), Is.EqualTo(stringResult.Text));
            Assert.That(bufferText, Is.EqualTo(stringResult.Text));
            Assert.That(bufferResult.Bytes, Is.EqualTo(buffer.WrittenMemory.ToArray()));
        });
    }

    [Test]
    public void GenericXmlJsonXmlRoundTripRetainsIdentityPrecisionAndUnknownValues()
    {
        const string namespaceUri =
            "urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10";
        const string schemaIdentifier =
            "urn:iso:std:iso:20022:tech:json:pacs.008.001.10";
        const string preciseAmount = "123456789012345678.1200";
        const string preciseRate = "-0.0000000000000000012300";
        string xml = "<Document xmlns=\"" + namespaceUri +
            "\" xmlns:ext=\"urn:extension\" xmlns:meta=\"urn:metadata\">" +
            "<Amt Ccy=\"USD\">" + preciseAmount + "</Amt><Rate>" + preciseRate +
            "</Rate><ext:Unknown meta:id=\"007\"><ext:Value>opaque-value" +
            "</ext:Value></ext:Unknown></Document>";
        IXmlMessageReader reader = new XmlMessageReader();
        IMessageJsonConverter converter = new MessageJsonConverter();

        MessageReadResult original = reader.ReadGenericText(xml, PreserveReadOptions());
        DynamicXmlNode originalRoot = RequiredRoot(original);
        MessageJsonResult json = converter.WriteGenericDocument(original.Document);
        using JsonDocument parsed = JsonDocument.Parse(json.Json);
        MessageJsonResult restored = converter.ReadGenericText(json.Json);
        MessageReadResult reread = reader.ReadGenericText(
            restored.Xml,
            PreserveReadOptions());
        DynamicXmlNode restoredRoot = RequiredRoot(reread);
        DynamicXmlNode originalUnknown = originalRoot.Children.Single(value =>
            value.Kind == XmlNodeKind.Element && value.Name.LocalName == "Unknown");
        DynamicXmlNode restoredUnknown = restoredRoot.Children.Single(value =>
            value.Kind == XmlNodeKind.Element && value.Name.LocalName == "Unknown");
        DynamicXmlNode restoredAmount = restoredRoot.Children.Single(value =>
            value.Kind == XmlNodeKind.Element && value.Name.LocalName == "Amt");
        DynamicXmlNode restoredRate = restoredRoot.Children.Single(value =>
            value.Kind == XmlNodeKind.Element && value.Name.LocalName == "Rate");
        JsonElement jsonRoot = parsed.RootElement.GetProperty("Document");

        Assert.Multiple(() =>
        {
            Assert.That(json.Success, Is.True, json.ErrorMessage);
            Assert.That(json.SchemaIdentifier, Is.EqualTo(schemaIdentifier));
            Assert.That(json.NamespaceUri, Is.EqualTo(namespaceUri));
            Assert.That(jsonRoot.GetProperty("Amt").GetProperty("$value").GetString(),
                Is.EqualTo(preciseAmount));
            Assert.That(jsonRoot.GetProperty("Rate").GetString(),
                Is.EqualTo(preciseRate));
            Assert.That(restored.Success, Is.True, restored.ErrorMessage);
            Assert.That(restored.SchemaIdentifier, Is.EqualTo(schemaIdentifier));
            Assert.That(restored.NamespaceUri, Is.EqualTo(namespaceUri));
            Assert.That(reread.Document?.Metadata.NamespaceUri,
                Is.EqualTo(namespaceUri));
            Assert.That(restoredRoot.Name.NamespaceUri, Is.EqualTo(namespaceUri));
            Assert.That(ElementText(restoredAmount), Is.EqualTo(preciseAmount));
            Assert.That(restoredAmount.Attributes.Single(value =>
                value.Name.LocalName == "Ccy").Value, Is.EqualTo("USD"));
            Assert.That(ElementText(restoredRate), Is.EqualTo(preciseRate));
            Assert.That(originalUnknown.Name.NamespaceUri,
                Is.EqualTo("urn:extension"));
            Assert.That(restoredUnknown.Name.NamespaceUri, Is.EqualTo(namespaceUri));
            Assert.That(ElementText(restoredUnknown.Children.Single(value =>
                value.Kind == XmlNodeKind.Element)), Is.EqualTo("opaque-value"));
            Assert.That(restoredUnknown.Attributes.Single(value =>
                value.Name.LocalName == "id").Value, Is.EqualTo("007"));
            Assert.That(json.IsLossless, Is.False);
            Assert.That(json.Diagnostics.Select(value => value.Code), Does.Contain(
                "json.wildcard_namespace_loss"));
            Assert.That(json.Diagnostics.Select(value => value.Code), Does.Contain(
                "json.wildcard_attribute_namespace_loss"));
        });
    }

    internal static string ElementText(DynamicXmlNode element)
    {
        return string.Concat(element.Children.Where(value =>
            value.Kind == XmlNodeKind.Text || value.Kind == XmlNodeKind.CData)
            .Select(value => value.Value));
    }

    internal static MessageReadOptions PreserveReadOptions()
    {
        return new MessageReadOptions
        {
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreserveWhitespace = true,
            PreserveLexicalValues = true
        };
    }

    internal static MessageWriteOptions PreserveWriteOptions()
    {
        return new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Preserve,
            WriteXmlDeclaration = false,
            PreservePrefixes = true,
            PreserveLexicalValues = true,
            UnknownContentMode = UnknownContentMode.Preserve
        };
    }

    internal static RawXmlFragment CreateRawFragment(string xml)
    {
        RawXmlFragment fragment = new RawXmlFragment();
        fragment.Xml = xml;
        fragment.RootName = new QualifiedNameData { Prefix = "ext", LocalName = "Extension", NamespaceUri = "urn:extension" };
        fragment.NamespaceDeclarations.Add(new NamespaceDeclarationData { Prefix = "ext", NamespaceUri = "urn:extension" });
        return fragment;
    }

    internal static MessageDocument RawDocument(RawXmlFragment fragment)
    {
        MessageDocument document = new MessageDocument();
        document.Payloads.Add(new MessagePayload { RawContent = fragment });
        return document;
    }

    internal static DynamicXmlNode RequiredRoot(MessageReadResult result)
    {
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(result.Document, Is.Not.Null);
        Assert.That(result.Document?.Payloads, Has.Count.EqualTo(1));
        Assert.That(result.Document?.Payloads[0].GenericContent, Is.Not.Null);
        return result.Document!.Payloads[0].GenericContent!;
    }

    internal static void AssertEquivalentNode(DynamicXmlNode expected, DynamicXmlNode actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind));
            Assert.That(actual.Name.NamespaceUri, Is.EqualTo(expected.Name.NamespaceUri));
            Assert.That(actual.Name.LocalName, Is.EqualTo(expected.Name.LocalName));
            Assert.That(actual.Name.Prefix, Is.EqualTo(expected.Name.Prefix));
            Assert.That(actual.Value, Is.EqualTo(expected.Value));
            Assert.That(actual.IsNil, Is.EqualTo(expected.IsNil));
            Assert.That(actual.IsEmptyElement, Is.EqualTo(expected.IsEmptyElement));
            Assert.That(actual.NamespaceDeclarations.Select(value => (value.Prefix, value.NamespaceUri)), Is.EqualTo(expected.NamespaceDeclarations.Select(value => (value.Prefix, value.NamespaceUri))));
            Assert.That(actual.Attributes.Select(value => (value.Name.NamespaceUri, value.Name.LocalName, value.Name.Prefix, value.Value)), Is.EqualTo(expected.Attributes.Select(value => (value.Name.NamespaceUri, value.Name.LocalName, value.Name.Prefix, value.Value))));
            Assert.That(actual.Children, Has.Count.EqualTo(expected.Children.Count));
        });

        int index = 0;
        while (index < expected.Children.Count)
        {
            AssertEquivalentNode(expected.Children[index], actual.Children[index]);
            index++;
        }
    }
}
