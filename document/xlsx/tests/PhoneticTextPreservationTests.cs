using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class PhoneticTextPreservationTests
{
    [Test]
    public void SharedStringReaderWriter_PreservesPhoneticTextAndProperties()
    {
        string xml = "<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<si><t>漢字</t><rPh sb=\"0\" eb=\"2\"><t>かんじ</t></rPh><phoneticPr fontId=\"1\" type=\"noConversion\" /></si>" +
            "</sst>";

        XlsxSharedStringTablePart part = new XlsxSharedStringTableReader().ReadString(xml);
        XlsxSharedStringTableWriteResult writeResult = new XlsxSharedStringTableWriter().WriteString(part);
        XlsxSharedStringTablePart roundTrip = new XlsxSharedStringTableReader().ReadString(writeResult.Xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.Items[0].PhoneticTexts.Count, Is.EqualTo(1));
        Assert.That(part.Items[0].PhoneticTexts[0].Text, Is.EqualTo("かんじ"));
        Assert.That(part.Items[0].PhoneticTexts[0].HasStartBaseIndex, Is.True);
        Assert.That(part.Items[0].PhoneticTexts[0].StartBaseIndex, Is.EqualTo(0));
        Assert.That(part.Items[0].PhoneticTexts[0].HasEndBaseIndex, Is.True);
        Assert.That(part.Items[0].PhoneticTexts[0].EndBaseIndex, Is.EqualTo(2));
        Assert.That(part.Items[0].HasPhoneticProperties, Is.True);
        Assert.That(part.Items[0].PhoneticPropertiesRawXml, Does.Contain("noConversion"));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<rPh"));
        Assert.That(writeResult.Xml, Does.Contain("<phoneticPr"));
        Assert.That(roundTrip.Items[0].PhoneticTexts.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Items[0].PhoneticTexts[0].Text, Is.EqualTo("かんじ"));
        Assert.That(roundTrip.Items[0].HasPhoneticProperties, Is.True);
    }

    [Test]
    public void SharedStringWriter_WritesProgrammaticPhoneticText()
    {
        XlsxSharedStringTablePart part = new XlsxSharedStringTablePart();
        XlsxSharedStringItem item = new XlsxSharedStringItem();
        item.Text = "東京";
        item.PhoneticTexts.Add(CreatePhoneticText("とうきょう", 0, 2));
        item.PhoneticPropertiesRawXml = "<phoneticPr fontId=\"2\" />";
        item.HasPhoneticProperties = true;
        part.Items.Add(item);

        XlsxSharedStringTableWriteResult result = new XlsxSharedStringTableWriter().WriteString(part);
        XlsxSharedStringTablePart roundTrip = new XlsxSharedStringTableReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("sb=\"0\""));
        Assert.That(result.Xml, Does.Contain("eb=\"2\""));
        Assert.That(roundTrip.Items[0].PhoneticTexts[0].Text, Is.EqualTo("とうきょう"));
        Assert.That(roundTrip.Items[0].PhoneticPropertiesRawXml, Does.Contain("fontId=\"2\""));
    }

    [Test]
    public void InlineStringReaderWriter_PreservesPhoneticTextAndProperties()
    {
        string xml = "<is xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<t>大阪</t><rPh sb=\"0\" eb=\"2\"><t>おおさか</t></rPh><phoneticPr fontId=\"3\" />" +
            "</is>";

        XlsxInlineStringResult result = new XlsxInlineStringReader().ReadString(xml);
        XlsxInlineStringResult writeResult = new XlsxInlineStringWriter().WriteString(result.Value);
        XlsxInlineStringResult roundTrip = new XlsxInlineStringReader().ReadString(writeResult.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Value.PhoneticTexts.Count, Is.EqualTo(1));
        Assert.That(result.Value.PhoneticTexts[0].Text, Is.EqualTo("おおさか"));
        Assert.That(result.Value.HasPhoneticProperties, Is.True);
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<rPh"));
        Assert.That(writeResult.Xml, Does.Contain("<phoneticPr"));
        Assert.That(roundTrip.Value.PhoneticTexts[0].Text, Is.EqualTo("おおさか"));
        Assert.That(roundTrip.Value.PhoneticPropertiesRawXml, Does.Contain("fontId=\"3\""));
    }

    [Test]
    public void WorksheetReaderWriter_RoundTripsInlinePhoneticText()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\">" +
            "<c r=\"A1\" t=\"inlineStr\"><is><t>京都</t><rPh sb=\"0\" eb=\"2\"><t>きょうと</t></rPh><phoneticPr fontId=\"4\" /></is></c>" +
            "</row></sheetData></worksheet>";

        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadString(xml);
        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);
        XlsxWorksheetCell? cell = roundTrip.GetCell("A1");

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.GetCell("A1")!.InlineString.PhoneticTexts[0].Text, Is.EqualTo("きょうと"));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<rPh"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(cell!.HasInlineString, Is.True);
        Assert.That(cell.InlineString.PhoneticTexts.Count, Is.EqualTo(1));
        Assert.That(cell.InlineString.PhoneticTexts[0].Text, Is.EqualTo("きょうと"));
        Assert.That(cell.InlineString.HasPhoneticProperties, Is.True);
    }

    static XlsxPhoneticText CreatePhoneticText(string text, uint startBaseIndex, uint endBaseIndex)
    {
        XlsxPhoneticText phoneticText = new XlsxPhoneticText();
        phoneticText.Text = text;
        phoneticText.StartBaseIndex = startBaseIndex;
        phoneticText.HasStartBaseIndex = true;
        phoneticText.EndBaseIndex = endBaseIndex;
        phoneticText.HasEndBaseIndex = true;
        return phoneticText;
    }
}
