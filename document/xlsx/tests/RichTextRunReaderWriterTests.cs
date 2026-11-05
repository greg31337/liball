using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class RichTextRunReaderWriterTests
{
    [Test]
    public void ReadString_ParsesTextPropertiesUnknownAttributesAndUnknownXml()
    {
        string xml = "<r xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\" x:run=\"r\">" +
            "<rPr><b /><color rgb=\"FFFF0000\" /></rPr>" +
            "<t xml:space=\"preserve\"> Bold </t>" +
            "<x:future />" +
            "</r>";

        XlsxRichTextRunResult result = new XlsxRichTextRunReader().ReadString(xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Run.Text, Is.EqualTo(" Bold "));
        Assert.That(result.Run.PropertiesRawXml, Does.Contain("<b"));
        Assert.That(result.Run.PropertiesRawXml, Does.Contain("FFFF0000"));
        Assert.That(result.Run.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(result.Run.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(result.Run.UnknownXml[0], Does.Contain("future"));
    }

    [Test]
    public void WriteString_WritesRunPropertiesWhitespaceAndUnknownsThenRoundTrips()
    {
        XlsxRichTextRun run = new XlsxRichTextRun();
        run.Text = " Tail ";
        run.PropertiesRawXml = "<rPr><i /></rPr>";
        run.UnknownAttributes.Add(CreateAttribute("urn:x", "x", "run", "r"));
        run.UnknownXml.Add("<x:future xmlns:x=\"urn:x\" />");

        XlsxRichTextRunResult result = new XlsxRichTextRunWriter().WriteString(run);
        XlsxRichTextRunResult roundTrip = new XlsxRichTextRunReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<r"));
        Assert.That(result.Xml, Does.Contain("<i"));
        Assert.That(result.Xml, Does.Contain("xml:space=\"preserve\""));
        Assert.That(result.Xml, Does.Contain("x:future"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Run.Text, Is.EqualTo(" Tail "));
        Assert.That(roundTrip.Run.PropertiesRawXml, Does.Contain("<i"));
        Assert.That(roundTrip.Run.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Run.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_PreservesRawRunXmlWhenProvided()
    {
        XlsxRichTextRun run = new XlsxRichTextRun();
        run.RawXml = "<r xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><rPr><u /></rPr><t>Raw</t></r>";

        XlsxRichTextRunResult result = new XlsxRichTextRunWriter().WriteString(run);
        XlsxRichTextRunResult roundTrip = new XlsxRichTextRunReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<u"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Run.Text, Is.EqualTo("Raw"));
        Assert.That(roundTrip.Run.PropertiesRawXml, Does.Contain("<u"));
    }

    [Test]
    public void SharedStringRunConversion_PreservesFields()
    {
        XlsxSharedStringRichTextRun shared = new XlsxSharedStringRichTextRun();
        shared.Text = "Shared";
        shared.PropertiesRawXml = "<rPr><b /></rPr>";
        shared.UnknownXml.Add("<x:future xmlns:x=\"urn:x\" />");

        XlsxRichTextRun generic = XlsxRichTextRunWriter.FromSharedStringRun(shared);
        XlsxSharedStringRichTextRun converted = XlsxRichTextRunReader.ToSharedStringRun(generic);

        Assert.That(generic.Text, Is.EqualTo("Shared"));
        Assert.That(generic.PropertiesRawXml, Does.Contain("<b"));
        Assert.That(generic.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(converted.Text, Is.EqualTo("Shared"));
        Assert.That(converted.PropertiesRawXml, Does.Contain("<b"));
        Assert.That(converted.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void RichTextRunReaderWriter_ReportInvalidInputs()
    {
        XlsxRichTextRunResult missingRoot = new XlsxRichTextRunReader().ReadString("");
        XlsxRichTextRunResult invalidRoot = new XlsxRichTextRunReader().ReadString("<bad />");
        XlsxRichTextRunResult? nullRun = new XlsxRichTextRunWriter().WriteBytes(null!);

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "richTextRun.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "richTextRun.invalidRoot"), Is.True);
        Assert.That(nullRun.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullRun, "richTextRunWriter.runNull"), Is.True);
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

    static bool HasDiagnostic(XlsxRichTextRunResult result, string code)
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
