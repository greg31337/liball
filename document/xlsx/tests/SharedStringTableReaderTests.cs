using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class SharedStringTableReaderTests
{
    [Test]
    public void ReadString_ParsesPlainTextRichTextCountsAndUnknowns()
    {
        string xml = "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\" count=\"4\" uniqueCount=\"3\" x:root=\"r\">" +
            "<si><t>Plain</t></si>" +
            "<si><r><rPr><b /></rPr><t>Bold</t></r><r><t> and plain</t></r></si>" +
            "<si><t xml:space=\"preserve\"> spaced </t></si>" +
            "<x:future />" +
            "</sst>";
        XlsxSharedStringTableReader reader = new XlsxSharedStringTableReader();

        XlsxSharedStringTablePart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.HasCount, Is.True);
        Assert.That(part.Count, Is.EqualTo(4));
        Assert.That(part.HasUniqueCount, Is.True);
        Assert.That(part.UniqueCount, Is.EqualTo(3));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.Items.Count, Is.EqualTo(3));
        Assert.That(part.GetText(0), Is.EqualTo("Plain"));
        Assert.That(part.GetText(1), Is.EqualTo("Bold and plain"));
        Assert.That(part.GetText(2), Is.EqualTo(" spaced "));
        Assert.That(part.Items[1].IsRichText, Is.True);
        Assert.That(part.Items[1].RichTextRuns.Count, Is.EqualTo(2));
        Assert.That(part.Items[1].RichTextRuns[0].Text, Is.EqualTo("Bold"));
        Assert.That(part.Items[1].RichTextRuns[0].PropertiesRawXml, Does.Contain("<b"));
        Assert.That(part.Items[1].RichTextRuns[1].Text, Is.EqualTo(" and plain"));
    }

    [Test]
    public void ReadString_PreservesUnknownItemAndRunXml()
    {
        string xml = "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\">" +
            "<si x:item=\"1\"><r x:run=\"r\"><rPr><i /></rPr><t>A</t><x:futureRun /></r><rPh sb=\"0\" eb=\"1\"><t>a</t></rPh><phoneticPr fontId=\"1\" /></si>" +
            "</sst>";

        XlsxSharedStringTablePart part = new XlsxSharedStringTableReader().ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Items.Count, Is.EqualTo(1));
        Assert.That(part.Items[0].Text, Is.EqualTo("A"));
        Assert.That(part.Items[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Items[0].UnknownXml.Count, Is.EqualTo(0));
        Assert.That(part.Items[0].PhoneticTexts.Count, Is.EqualTo(1));
        Assert.That(part.Items[0].PhoneticTexts[0].Text, Is.EqualTo("a"));
        Assert.That(part.Items[0].HasPhoneticProperties, Is.True);
        Assert.That(part.Items[0].PhoneticPropertiesRawXml, Does.Contain("phoneticPr"));
        Assert.That(part.Items[0].RichTextRuns.Count, Is.EqualTo(1));
        Assert.That(part.Items[0].RichTextRuns[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.Items[0].RichTextRuns[0].UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.Items[0].RichTextRuns[0].UnknownXml[0], Does.Contain("futureRun"));
        Assert.That(part.Items[0].RichTextRuns[0].RawXml, Does.Contain("<r"));
    }

    [Test]
    public void ReadString_ReportsMissingRootInvalidRootAndInvalidCounts()
    {
        XlsxSharedStringTableReader reader = new XlsxSharedStringTableReader();

        XlsxSharedStringTablePart missingRoot = reader.ReadString("");
        XlsxSharedStringTablePart invalidRoot = reader.ReadString("<bad />");
        XlsxSharedStringTablePart invalidCount = reader.ReadString("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"bad\" uniqueCount=\"bad\"><si><t>A</t></si></sst>");

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "sharedStringTable.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "sharedStringTable.invalidRoot"), Is.True);
        Assert.That(invalidCount.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidCount, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(invalidCount.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_EnforcesConfiguredMaxSharedStrings()
    {
        string xml = "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<si><t>One</t></si>" +
            "<si><t>Two</t></si>" +
            "</sst>";
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.MaxSharedStrings = 1;

        XlsxSharedStringTablePart part = new XlsxSharedStringTableReader().ReadStringWithOptions(xml, options);

        Assert.That(part.IsValid, Is.False);
        Assert.That(HasDiagnostic(part, "sharedStringTable.maxSharedStrings"), Is.True);
        Assert.That(part.Items.Count, Is.EqualTo(1));
        Assert.That(part.GetText(0), Is.EqualTo("One"));
    }

    static bool HasDiagnostic(XlsxSharedStringTablePart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
