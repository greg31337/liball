using xml;

namespace xml.Tests;

[TestFixture]
public class DtdValidatorTests
{
    [Test]
    public void ReportsDuplicateRetainedElementAndNotationDeclarations()
    {
        string subset = "<!ELEMENT root EMPTY><!NOTATION image SYSTEM 'image/png'>";
        XmlDocument document = ParseRetainedDocument("<!DOCTYPE root [" + subset + "]><root/>");
        XmlElementDeclaration element = document.DocumentType!.ElementDeclarations![0];
        XmlNotationDeclaration notation = document.DocumentType.NotationDeclarations![0];
        document.DocumentType.ElementDeclarations = [element, element];
        document.DocumentType.NotationDeclarations = [notation, notation];

        XmlDiagnostic[] diagnostics = new DtdValidator().ValidateXmlDocumentDtd(document, ValidationOptions(true));

        Assert.That(diagnostics, Has.Length.EqualTo(2));
        Assert.That(diagnostics[0].Message, Does.Contain("Element type 'root'"));
        Assert.That(diagnostics[1].Message, Does.Contain("Notation 'image'"));
    }

    [Test]
    public void ValidatesACompatibleRetainedTree()
    {
        string subset = "<!ELEMENT root (item,item)><!ELEMENT item EMPTY><!ATTLIST item id ID #REQUIRED link IDREF #IMPLIED>";
        XmlDocument document = ParseRetainedDocument("<!DOCTYPE root [" + subset + "]><root><item id='first'/><item id='second' link='first'/></root>");

        XmlDiagnostic[] diagnostics = new DtdValidator().ValidateXmlDocumentDtd(document, ValidationOptions(true));

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void AccumulatesTreeContentDeclarationAndAttributeErrors()
    {
        string subset = "<!ELEMENT root (item,item)><!ELEMENT item EMPTY><!ATTLIST item code (a|b) #REQUIRED>";
        XmlDocument document = ParseRetainedDocument("<!DOCTYPE root [" + subset + "]><root><item code='bad'/><wrong/></root>");

        XmlDiagnostic[] diagnostics = new DtdValidator().ValidateXmlDocumentDtd(document, ValidationOptions(true));

        Assert.That(diagnostics, Has.Length.EqualTo(4));
        Assert.That(diagnostics.All(item => item.Code == XmlErrorCodes.DtdValidity), Is.True);
        Assert.That(diagnostics.All(item => item.Category == XmlErrorCategory.Validation), Is.True);
    }

    [Test]
    public void ValidatesAlreadyNormalizedRetainedAttributeValues()
    {
        string subset = "<!ELEMENT root EMPTY><!ATTLIST root value CDATA #REQUIRED>";
        XmlDocument document = ParseRetainedDocument("<!DOCTYPE root [" + subset + "]><root value='a&amp;b'/>");

        XmlDiagnostic[] diagnostics = new DtdValidator().ValidateXmlDocumentDtd(document, ValidationOptions(true));

        Assert.That(document.DocumentElement!.Attributes![0].Value, Is.EqualTo("a&b"));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void RetainedTreeValidationUsesFailFastPolicy()
    {
        string subset = "<!ELEMENT root EMPTY><!ATTLIST root value (a|b) #REQUIRED>";
        XmlDocument document = ParseRetainedDocument("<!DOCTYPE root [" + subset + "]><root value='wrong'/>");

        XmlValidationException failure = Assert.Throws<XmlValidationException>(() => new DtdValidator().ValidateXmlDocumentDtd(document, ValidationOptions(false)))!;

        Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.DtdValidity));
    }

    [Test]
    public void ValidatesCompatibleNormalizedEventsWithinBatchBounds()
    {
        XmlDocumentType documentType = ParseRetainedDocument("<!DOCTYPE root [<!ELEMENT root (child)><!ELEMENT child EMPTY><!ATTLIST child state (ready|done) #REQUIRED>]><root><child state='ready'/></root>").DocumentType!;
        XmlEventBatch batch = CreateEventBatch(
        [
            ValueEvent(XmlEventKind.Comment, "padding"),
            SimpleEvent(XmlEventKind.StartDocument, null),
            ElementEvent(XmlEventKind.StartElement, "root", null),
            ValueEvent(XmlEventKind.IgnorableWhitespace, " "),
            ElementEvent(XmlEventKind.StartElement, "child", [Attribute("state", "ready")]),
            ElementEvent(XmlEventKind.EndElement, "child", null),
            ElementEvent(XmlEventKind.EndElement, "root", null),
            SimpleEvent(XmlEventKind.EndDocument, null),
            ValueEvent(XmlEventKind.Comment, "padding")
        ], 1, 7);

        XmlDiagnostic[] diagnostics = new DtdValidator().ValidateXmlEventDtd(batch, documentType, ValidationOptions(true));

        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void AccumulatesNormalizedEventAttributeErrors()
    {
        XmlDocumentType documentType = ParseRetainedDocument("<!DOCTYPE root [<!ELEMENT root (child)><!ELEMENT child EMPTY><!ATTLIST child state (ready|done) #REQUIRED>]><root><child state='ready'/></root>").DocumentType!;
        XmlEventBatch batch = CreateEventBatch(
        [
            ElementEvent(XmlEventKind.StartElement, "root", null),
            ElementEvent(XmlEventKind.StartElement, "child", [Attribute("state", "bad"), Attribute("extra", "value")]),
            ElementEvent(XmlEventKind.EndElement, "child", null),
            ElementEvent(XmlEventKind.EndElement, "root", null)
        ], 0, 4);

        XmlDiagnostic[] diagnostics = new DtdValidator().ValidateXmlEventDtd(batch, documentType, ValidationOptions(true));

        Assert.That(diagnostics, Has.Length.EqualTo(2));
        Assert.That(diagnostics[0].Code, Is.EqualTo(XmlErrorCodes.DtdValidity));
        Assert.That(diagnostics[1].Code, Is.EqualTo(XmlErrorCodes.DtdValidity));
    }

    [Test]
    public void RejectsIncompatibleRetainedInputs()
    {
        XmlDocument withoutDtd = ParseDocument("<root/>");
        XmlDocumentType documentType = ParseRetainedDocument("<!DOCTYPE root [<!ELEMENT root EMPTY>]><root/>").DocumentType!;
        XmlEventBatch mismatched = CreateEventBatch(
        [
            ElementEvent(XmlEventKind.StartElement, "root", null),
            ElementEvent(XmlEventKind.EndElement, "different", null)
        ], 0, 2);

        Assert.Throws<ArgumentException>(() => new DtdValidator().ValidateXmlDocumentDtd(withoutDtd, ValidationOptions(true)));
        Assert.Throws<ArgumentException>(() => new DtdValidator().ValidateXmlEventDtd(mismatched, documentType, ValidationOptions(true)));
    }

    private static XmlDocument ParseRetainedDocument(string text)
    {
        return ParseDocument(text);
    }

    private static XmlDocument ParseDocument(string text)
    {
        XmlInput input = new XmlInput();
        input.Kind = XmlInputKind.String;
        input.Text = text;
        input.BaseUri = "urn:dtd-validator";
        XmlParserOptions options = ValidationOptions(false);
        options.ValidationMode = XmlValidationMode.None;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        return new XmlParser().ParseXmlDocument(input, options, null, null, null);
    }

    private static XmlParserOptions ValidationOptions(bool accumulate)
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Disabled;
        options.DtdProcessing = XmlDtdProcessing.Parse;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.Dtd;
        options.AccumulateValidationDiagnostics = accumulate;
        return options;
    }

    private static XmlEventBatch CreateEventBatch(XmlEvent[] items, int offset, int count)
    {
        XmlEventBatch batch = new XmlEventBatch();
        batch.Events = items;
        batch.Offset = offset;
        batch.Count = count;
        batch.IsFinal = true;
        batch.Status = XmlParserStatus.Completed;
        return batch;
    }

    private static XmlEvent ElementEvent(XmlEventKind kind, string name, XmlAttributeData[]? attributes)
    {
        XmlEvent item = SimpleEvent(kind, name);
        item.NodeKind = XmlNodeKind.Element;
        item.Attributes = attributes;
        return item;
    }

    private static XmlEvent ValueEvent(XmlEventKind kind, string value)
    {
        XmlEvent item = SimpleEvent(kind, null);
        item.Value = value;
        item.IsIgnorableWhitespace = kind == XmlEventKind.IgnorableWhitespace;
        return item;
    }

    private static XmlEvent SimpleEvent(XmlEventKind kind, string? name)
    {
        XmlEvent item = new XmlEvent();
        item.Kind = kind;
        if (name != null)
        {
            item.Name = Name(name);
        }

        return item;
    }

    private static XmlAttributeData Attribute(string name, string value)
    {
        XmlAttributeData attribute = new XmlAttributeData();
        attribute.Name = Name(name);
        attribute.Value = value;
        attribute.IsSpecified = true;
        return attribute;
    }

    private static XmlName Name(string value)
    {
        XmlName name = new XmlName();
        name.LocalName = value;
        name.QualifiedName = value;
        return name;
    }
}
