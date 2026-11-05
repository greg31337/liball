using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class RangeCopyCompletionTests
{
    [Test]
    public void SameSheetCopy_TranslatesRelativeFormulaReferencesAndClonesFormulaMetadata()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetFormulaCachedValue("B2", "A1+$A1+A$1+$A$1", "4", "n");
        XlsxXmlAttribute formulaAttribute = CreateAttribute("formula", "source");
        worksheet.GetCell("B2")!.FormulaInfo.UnknownAttributes.Add(formulaAttribute);
        IXlsxWorksheetRangeOperations operations = new XlsxWorksheetRangeOperations();

        XlsxWorksheetRangeResult result = operations.CopyTo(worksheet.GetRange("B2"), "D4");
        XlsxWorksheetCell copied = worksheet.GetCell("D4")!;
        formulaAttribute.Value = "changed";

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CopiedCellCount, Is.EqualTo(1));
        Assert.That(result.FormulaCellCount, Is.EqualTo(1));
        Assert.That(copied.Formula, Is.EqualTo("C3+$A3+C$1+$A$1"));
        Assert.That(copied.FormulaInfo.Text, Is.EqualTo("C3+$A3+C$1+$A$1"));
        Assert.That(copied.FormulaInfo.UnknownAttributes[0], Is.Not.SameAs(formulaAttribute));
        Assert.That(copied.FormulaInfo.UnknownAttributes[0].Value, Is.EqualTo("source"));
    }

    [Test]
    public void CrossSheetCopy_TranslatesRelativeFormulaReferences()
    {
        XlsxWorksheetPart source = new XlsxWorksheetPart();
        XlsxWorksheetPart target = new XlsxWorksheetPart();
        source.SetFormulaCachedValue("B2", "A1+$A1+A$1+$A$1", "4", "n");
        IXlsxRangeService service = new XlsxRangeService();

        XlsxWorksheetRangeResult result = service.CopyAcrossSheets(source.GetRange("B2"), target, "E5");

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CopiedCellCount, Is.EqualTo(1));
        Assert.That(result.FormulaCellCount, Is.EqualTo(1));
        Assert.That(target.GetCell("E5")!.Formula, Is.EqualTo("D4+$A4+D$1+$A$1"));
        Assert.That(source.GetCell("B2")!.Formula, Is.EqualTo("A1+$A1+A$1+$A$1"));
    }

    [Test]
    public void SameSheetCopy_DeepClonesRichTextAndUnknownMetadata()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetInlineStringValue("A1", "Tokyo");
        XlsxWorksheetCell source = worksheet.GetCell("A1")!;
        source.InlineString = CreateRichInlineString();
        source.HasInlineString = true;
        source.UnknownAttributes.Add(CreateAttribute("cell", "source-cell"));
        IXlsxWorksheetRangeOperations operations = new XlsxWorksheetRangeOperations();

        XlsxWorksheetRangeResult result = operations.CopyTo(worksheet.GetRange("A1"), "C3");
        XlsxWorksheetCell copied = worksheet.GetCell("C3")!;
        source.UnknownAttributes[0].Value = "changed-cell";
        source.InlineString.UnknownAttributes[0].Value = "changed-inline";
        source.InlineString.RichTextRuns[0].Text = "Changed";
        source.InlineString.RichTextRuns[0].UnknownAttributes[0].Value = "changed-run";
        source.InlineString.PhoneticTexts[0].Text = "changed-phonetic";
        source.InlineString.PhoneticTexts[0].UnknownAttributes[0].Value = "changed-phonetic-attribute";
        source.InlineString.PhoneticProperties.Type = "changed-type";
        source.InlineString.PhoneticProperties.UnknownAttributes[0].Value = "changed-properties";

        Assert.That(result.IsValid, Is.True);
        Assert.That(copied.UnknownAttributes[0].Value, Is.EqualTo("source-cell"));
        Assert.That(copied.InlineString, Is.Not.SameAs(source.InlineString));
        Assert.That(copied.InlineString.UnknownAttributes[0].Value, Is.EqualTo("source-inline"));
        Assert.That(copied.InlineString.RichTextRuns[0], Is.Not.SameAs(source.InlineString.RichTextRuns[0]));
        Assert.That(copied.InlineString.RichTextRuns[0].Text, Is.EqualTo("To"));
        Assert.That(copied.InlineString.RichTextRuns[0].UnknownAttributes[0].Value, Is.EqualTo("source-run"));
        Assert.That(copied.InlineString.PhoneticTexts[0], Is.Not.SameAs(source.InlineString.PhoneticTexts[0]));
        Assert.That(copied.InlineString.PhoneticTexts[0].Text, Is.EqualTo("とうきょう"));
        Assert.That(copied.InlineString.PhoneticTexts[0].UnknownAttributes[0].Value, Is.EqualTo("source-phonetic"));
        Assert.That(copied.InlineString.PhoneticProperties, Is.Not.SameAs(source.InlineString.PhoneticProperties));
        Assert.That(copied.InlineString.PhoneticProperties.Type, Is.EqualTo("Hiragana"));
        Assert.That(copied.InlineString.PhoneticProperties.UnknownAttributes[0].Value, Is.EqualTo("source-properties"));
    }

    static XlsxInlineString CreateRichInlineString()
    {
        XlsxInlineString inline = new XlsxInlineString();
        inline.Text = "Tokyo";
        inline.IsRichText = true;
        inline.UnknownAttributes.Add(CreateAttribute("inline", "source-inline"));
        XlsxSharedStringRichTextRun run = new XlsxSharedStringRichTextRun();
        run.Text = "To";
        run.PropertiesRawXml = "<rPr><b /></rPr>";
        run.UnknownAttributes.Add(CreateAttribute("run", "source-run"));
        run.UnknownXml.Add("<futureRun />");
        inline.RichTextRuns.Add(run);
        XlsxPhoneticText phonetic = new XlsxPhoneticText();
        phonetic.Text = "とうきょう";
        phonetic.StartBaseIndex = 0;
        phonetic.HasStartBaseIndex = true;
        phonetic.EndBaseIndex = 5;
        phonetic.HasEndBaseIndex = true;
        phonetic.UnknownAttributes.Add(CreateAttribute("phonetic", "source-phonetic"));
        phonetic.UnknownXml.Add("<futurePhonetic />");
        inline.PhoneticTexts.Add(phonetic);
        inline.PhoneticPropertiesRawXml = "<phoneticPr fontId=\"3\" />";
        inline.HasPhoneticProperties = true;
        inline.PhoneticProperties.FontId = 3;
        inline.PhoneticProperties.HasFontId = true;
        inline.PhoneticProperties.Type = "Hiragana";
        inline.PhoneticProperties.Alignment = "center";
        inline.PhoneticProperties.UnknownAttributes.Add(CreateAttribute("properties", "source-properties"));
        inline.UnknownXml.Add("<futureInline />");
        return inline;
    }

    static XlsxXmlAttribute CreateAttribute(string localName, string value)
    {
        XlsxXmlAttribute attribute = new XlsxXmlAttribute();
        attribute.NamespaceUri = "urn:test";
        attribute.LocalName = localName;
        attribute.Prefix = "t";
        attribute.Value = value;
        return attribute;
    }
}
