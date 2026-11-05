using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ContentTypesWriterTests
{
    [Test]
    public void WriteString_WritesParseableContentTypesInStableOrder()
    {
        XlsxContentTypesPart part = CreateBasicPartInReverseOrder();
        XlsxContentTypesWriter writer = new XlsxContentTypesWriter();

        XlsxContentTypesWriteResult writeResult = writer.WriteString(part);
        XlsxContentTypesPart readPart = new XlsxContentTypesReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.StartWith("<?xml"));
        Assert.That(readPart.IsValid, Is.True);
        Assert.That(readPart.Defaults[0].Extension, Is.EqualTo("rels"));
        Assert.That(readPart.Defaults[1].Extension, Is.EqualTo("xml"));
        Assert.That(readPart.Overrides[0].PartName, Is.EqualTo(XlsxPackagePaths.Workbook));
        Assert.That(readPart.Overrides[1].PartName, Is.EqualTo(XlsxPackagePaths.Worksheet1));
        Assert.That(readPart.GetContentType(XlsxPackagePaths.Workbook), Is.EqualTo(XlsxContentTypes.Workbook));
    }

    [Test]
    public void WriteBytes_ProducesIdenticalBytesForEquivalentPartsInDifferentOrders()
    {
        XlsxContentTypesPart first = CreateBasicPartInReverseOrder();
        XlsxContentTypesPart second = CreateBasicPartInForwardOrder();
        XlsxContentTypesWriter writer = new XlsxContentTypesWriter();

        XlsxContentTypesWriteResult firstResult = writer.WriteBytes(first);
        XlsxContentTypesWriteResult secondResult = writer.WriteBytes(second);

        Assert.That(firstResult.IsValid, Is.True);
        Assert.That(secondResult.IsValid, Is.True);
        Assert.That(firstResult.Bytes, Is.EqualTo(secondResult.Bytes));
    }

    [Test]
    public void WriteString_PreservesUnknownAttributesAndRawUnknownXml()
    {
        XlsxContentTypesPart part = new XlsxContentTypesPart();
        part.UnknownRootAttributes.Add(CreateAttribute("urn:x", "x", "root", "r"));
        XlsxContentTypeDefault defaultItem = CreateDefault("xml", XlsxContentTypes.Xml);
        defaultItem.UnknownAttributes.Add(CreateAttribute("urn:x", "x", "default", "d"));
        part.Defaults.Add(defaultItem);
        XlsxContentTypeOverride overrideItem = CreateOverride(XlsxPackagePaths.Workbook, XlsxContentTypes.Workbook);
        overrideItem.UnknownAttributes.Add(CreateAttribute("urn:x", "x", "override", "o"));
        part.Overrides.Add(overrideItem);
        part.UnknownXml.Add("<x:Future xmlns:x=\"urn:x\" x:value=\"1\" />");
        XlsxContentTypesWriter writer = new XlsxContentTypesWriter();

        XlsxContentTypesWriteResult writeResult = writer.WriteString(part);
        XlsxContentTypesPart readPart = new XlsxContentTypesReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(readPart.IsValid, Is.True);
        Assert.That(readPart.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(readPart.UnknownRootAttributes[0].LocalName, Is.EqualTo("root"));
        Assert.That(readPart.Defaults[0].UnknownAttributes[0].LocalName, Is.EqualTo("default"));
        Assert.That(readPart.Overrides[0].UnknownAttributes[0].LocalName, Is.EqualTo("override"));
        Assert.That(readPart.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(readPart.UnknownXml[0], Does.Contain("Future"));
    }

    [Test]
    public void WriteStream_WritesToProvidedStreamWithoutClosingIt()
    {
        XlsxContentTypesPart part = CreateBasicPartInForwardOrder();
        MemoryStream stream = new MemoryStream();
        XlsxContentTypesWriter writer = new XlsxContentTypesWriter();

        XlsxContentTypesWriteResult result = writer.WriteStream(stream, part);
        long lengthAfterWrite = stream.Length;
        stream.WriteByte(32);

        Assert.That(result.IsValid, Is.True);
        Assert.That(lengthAfterWrite, Is.GreaterThan(0));
        Assert.That(stream.Length, Is.EqualTo(lengthAfterWrite + 1));
        stream.Dispose();
    }

    [Test]
    public void WriteBytes_RejectsInvalidContentTypePartsBeforeWriting()
    {
        XlsxContentTypesPart part = new XlsxContentTypesPart();
        part.Defaults.Add(CreateDefault("xml", XlsxContentTypes.Xml));
        part.Defaults.Add(CreateDefault("XML", "text/xml"));
        part.Defaults.Add(CreateDefault("", XlsxContentTypes.Xml));
        part.Overrides.Add(CreateOverride("/xl/workbook.xml", XlsxContentTypes.Workbook));
        part.Overrides.Add(CreateOverride("xl/./workbook.xml", "application/duplicate"));
        part.Overrides.Add(CreateOverride("/xl/../styles.xml", XlsxContentTypes.Styles));
        part.Overrides.Add(CreateOverride(XlsxPackagePaths.Styles, ""));
        XlsxContentTypesWriter writer = new XlsxContentTypesWriter();

        XlsxContentTypesWriteResult result = writer.WriteBytes(part);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
        Assert.That(HasDiagnostic(result, "contentTypes.default.duplicate"), Is.True);
        Assert.That(HasDiagnostic(result, "contentTypes.default.extensionMissing"), Is.True);
        Assert.That(HasDiagnostic(result, "contentTypes.override.duplicate"), Is.True);
        Assert.That(HasDiagnostic(result, "path.parentSegment"), Is.True);
        Assert.That(HasDiagnostic(result, "contentTypes.override.contentTypeMissing"), Is.True);
    }

    [Test]
    public void WriteBytes_RejectsNullPart()
    {
        XlsxContentTypesWriter writer = new XlsxContentTypesWriter();

        XlsxContentTypesWriteResult? result = writer.WriteBytes(null!);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "contentTypes.partNull"), Is.True);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
    }

    static XlsxContentTypesPart CreateBasicPartInReverseOrder()
    {
        XlsxContentTypesPart part = new XlsxContentTypesPart();
        part.Defaults.Add(CreateDefault("xml", XlsxContentTypes.Xml));
        part.Defaults.Add(CreateDefault("rels", XlsxContentTypes.Relationships));
        part.Overrides.Add(CreateOverride(XlsxPackagePaths.Worksheet1, XlsxContentTypes.Worksheet));
        part.Overrides.Add(CreateOverride(XlsxPackagePaths.Workbook, XlsxContentTypes.Workbook));
        return part;
    }

    static XlsxContentTypesPart CreateBasicPartInForwardOrder()
    {
        XlsxContentTypesPart part = new XlsxContentTypesPart();
        part.Defaults.Add(CreateDefault("rels", XlsxContentTypes.Relationships));
        part.Defaults.Add(CreateDefault("xml", XlsxContentTypes.Xml));
        part.Overrides.Add(CreateOverride(XlsxPackagePaths.Workbook, XlsxContentTypes.Workbook));
        part.Overrides.Add(CreateOverride(XlsxPackagePaths.Worksheet1, XlsxContentTypes.Worksheet));
        return part;
    }

    static XlsxContentTypeDefault CreateDefault(string extension, string contentType)
    {
        XlsxContentTypeDefault item = new XlsxContentTypeDefault();
        item.Extension = extension;
        item.ContentType = contentType;
        return item;
    }

    static XlsxContentTypeOverride CreateOverride(string partName, string contentType)
    {
        XlsxContentTypeOverride item = new XlsxContentTypeOverride();
        item.PartName = partName;
        item.ContentType = contentType;
        return item;
    }

    static XlsxXmlAttribute CreateAttribute(string namespaceUri, string prefix, string localName, string value)
    {
        XlsxXmlAttribute attribute = new XlsxXmlAttribute();
        attribute.NamespaceUri = namespaceUri;
        attribute.Prefix = prefix;
        attribute.LocalName = localName;
        attribute.Value = value;
        return attribute;
    }

    static bool HasDiagnostic(XlsxContentTypesWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
