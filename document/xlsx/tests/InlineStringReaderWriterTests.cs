using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class InlineStringReaderWriterTests
{
    [Test]
    public void ReadString_ParsesPlainRichTextAndUnknowns()
    {
        string xml = "<is xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<r><rPr><b /></rPr><t>Bold</t></r>" +
            "<r><t>Tail</t><x:futureRun /></r>" +
            "<rPh sb=\"0\" eb=\"2\"><t>bt</t></rPh>" +
            "<phoneticPr fontId=\"1\" />" +
            "</is>";

        XlsxInlineStringResult result = new XlsxInlineStringReader().ReadString(xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.Text, Is.EqualTo("BoldTail"));
        Assert.That(result.Value.IsRichText, Is.True);
        Assert.That(result.Value.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(result.Value.UnknownXml.Count, Is.EqualTo(0));
        Assert.That(result.Value.PhoneticTexts.Count, Is.EqualTo(1));
        Assert.That(result.Value.PhoneticTexts[0].Text, Is.EqualTo("bt"));
        Assert.That(result.Value.HasPhoneticProperties, Is.True);
        Assert.That(result.Value.PhoneticPropertiesRawXml, Does.Contain("phoneticPr"));
        Assert.That(result.Value.RichTextRuns.Count, Is.EqualTo(2));
        Assert.That(result.Value.RichTextRuns[0].Text, Is.EqualTo("Bold"));
        Assert.That(result.Value.RichTextRuns[0].PropertiesRawXml, Does.Contain("<b"));
        Assert.That(result.Value.RichTextRuns[1].UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_WritesPlainInlineStringAndRoundTripsWhitespace()
    {
        XlsxInlineString value = XlsxInlineString.CreateText(" spaced ");
        value.UnknownAttributes.Add(CreateAttribute("urn:x", "x", "root", "r"));
        value.UnknownXml.Add("<x:future xmlns:x=\"urn:x\" />");

        XlsxInlineStringResult result = new XlsxInlineStringWriter().WriteString(value);
        XlsxInlineStringResult roundTrip = new XlsxInlineStringReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<is"));
        Assert.That(result.Xml, Does.Contain("xml:space=\"preserve\""));
        Assert.That(result.Xml, Does.Contain("x:future"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Value.Text, Is.EqualTo(" spaced "));
        Assert.That(roundTrip.Value.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Value.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_WritesRichInlineStringAndRoundTripsRuns()
    {
        XlsxInlineString value = new XlsxInlineString();
        value.IsRichText = true;
        value.Text = "BoldTail";
        value.RichTextRuns.Add(CreateRun("Bold", "<rPr><b /></rPr>"));
        value.RichTextRuns.Add(CreateRun("Tail", ""));

        XlsxInlineStringResult result = new XlsxInlineStringWriter().WriteString(value);
        XlsxInlineStringResult roundTrip = new XlsxInlineStringReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<rPr><b"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Value.IsRichText, Is.True);
        Assert.That(roundTrip.Value.Text, Is.EqualTo("BoldTail"));
        Assert.That(roundTrip.Value.RichTextRuns.Count, Is.EqualTo(2));
    }

    [Test]
    public void WorksheetReaderWriter_RoundTripsRichInlineStringModel()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\">" +
            "<c r=\"A1\" t=\"inlineStr\"><is><r><rPr><b /></rPr><t>Bold</t></r><r><t>Tail</t></r></is></c>" +
            "</row></sheetData></worksheet>";

        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadString(xml);
        XlsxCellValueResult value = XlsxCellValueConverter.FromWorksheetCell(part.GetCell("A1"));
        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);
        XlsxWorksheetCell? roundTripCell = roundTrip.GetCell("A1");

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.GetCell("A1")!.HasInlineString, Is.True);
        Assert.That(part.GetCell("A1")!.InlineString.IsRichText, Is.True);
        Assert.That(part.GetCell("A1")!.InlineString.Text, Is.EqualTo("BoldTail"));
        Assert.That(value.IsValid, Is.True);
        Assert.That(value.Value.Kind, Is.EqualTo(XlsxCellValueKind.RichText));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTripCell!.HasInlineString, Is.True);
        Assert.That(roundTripCell.InlineString.IsRichText, Is.True);
        Assert.That(roundTripCell.InlineString.RichTextRuns.Count, Is.EqualTo(2));
    }

    [Test]
    public void InlineStringReaderWriter_ReportInvalidInputs()
    {
        XlsxInlineStringResult missingRoot = new XlsxInlineStringReader().ReadString("");
        XlsxInlineStringResult invalidRoot = new XlsxInlineStringReader().ReadString("<bad />");
        XlsxInlineStringResult? nullValue = new XlsxInlineStringWriter().WriteBytes(null!);
        XlsxInlineString rich = new XlsxInlineString();
        rich.IsRichText = true;
        rich.RichTextRuns = null!;
        XlsxInlineStringResult nullRuns = new XlsxInlineStringWriter().WriteBytes(rich);

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "inlineString.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "inlineString.invalidRoot"), Is.True);
        Assert.That(nullValue.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullValue, "inlineStringWriter.valueNull"), Is.True);
        Assert.That(nullRuns.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullRuns, "inlineStringWriter.richTextRunsNull"), Is.True);
    }

    static XlsxSharedStringRichTextRun CreateRun(string text, string propertiesRawXml)
    {
        XlsxSharedStringRichTextRun run = new XlsxSharedStringRichTextRun();
        run.Text = text;
        run.PropertiesRawXml = propertiesRawXml;
        return run;
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

    static bool HasDiagnostic(XlsxInlineStringResult result, string code)
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
