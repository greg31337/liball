using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ConditionalFormattingWriterTests
{
    [Test]
    public void WriteString_WritesStandardRulesFormulasAttributesAndUnknowns()
    {
        XlsxConditionalFormatting formatting = new XlsxConditionalFormatting();
        formatting.Sqref = "A1:A10 C1:C10";
        formatting.Pivot = true;
        formatting.HasPivot = true;
        formatting.UnknownAttributes.Add(CreateAttribute("x", "root", "urn:x", "r"));
        formatting.ExtensionListRawXml = "<extLst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><ext uri=\"x\" /></extLst>";
        formatting.UnknownXml.Add("<x:futureRoot xmlns:x=\"urn:x\" />");

        XlsxConditionalFormatRule cellIs = CreateRule("cellIs", 1);
        cellIs.DxfId = 2;
        cellIs.HasDxfId = true;
        cellIs.StopIfTrue = true;
        cellIs.HasStopIfTrue = true;
        cellIs.Operator = "between";
        cellIs.Formulas.Add("1");
        cellIs.Formulas.Add("10");
        cellIs.UnknownAttributes.Add(CreateAttribute("x", "rule", "urn:x", "rr"));
        cellIs.UnknownXml.Add("<x:future xmlns:x=\"urn:x\" />");
        formatting.Rules.Add(cellIs);

        XlsxConditionalFormatRule top10 = CreateRule("top10", 2);
        top10.Rank = 5;
        top10.HasRank = true;
        top10.Percent = true;
        top10.HasPercent = true;
        top10.Bottom = false;
        top10.HasBottom = true;
        formatting.Rules.Add(top10);

        XlsxConditionalFormatRule above = CreateRule("aboveAverage", 3);
        above.AboveAverage = false;
        above.HasAboveAverage = true;
        above.EqualAverage = true;
        above.HasEqualAverage = true;
        above.StdDev = 2;
        above.HasStdDev = true;
        formatting.Rules.Add(above);

        XlsxConditionalFormattingWriteResult result = new XlsxConditionalFormattingWriter().WriteString(formatting);
        XlsxConditionalFormatting readBack = new XlsxConditionalFormattingReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<conditionalFormatting"));
        Assert.That(result.Xml, Does.Contain("sqref=\"A1:A10 C1:C10\""));
        Assert.That(result.Xml, Does.Contain("pivot=\"1\""));
        Assert.That(result.Xml, Does.Contain("x:root=\"r\""));
        Assert.That(result.Xml, Does.Contain("type=\"cellIs\""));
        Assert.That(result.Xml, Does.Contain("priority=\"1\""));
        Assert.That(result.Xml, Does.Contain("operator=\"between\""));
        Assert.That(result.Xml, Does.Contain("<formula>1</formula>"));
        Assert.That(result.Xml, Does.Contain("<formula>10</formula>"));
        Assert.That(result.Xml, Does.Contain("type=\"top10\""));
        Assert.That(result.Xml, Does.Contain("rank=\"5\""));
        Assert.That(result.Xml, Does.Contain("<extLst"));
        Assert.That(result.Xml, Does.Contain("<x:futureRoot"));

        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.Sqref, Is.EqualTo("A1:A10 C1:C10"));
        Assert.That(readBack.HasPivot, Is.True);
        Assert.That(readBack.Pivot, Is.True);
        Assert.That(readBack.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(readBack.Rules.Count, Is.EqualTo(3));
        Assert.That(readBack.Rules[0].Formulas[0], Is.EqualTo("1"));
        Assert.That(readBack.Rules[0].Formulas[1], Is.EqualTo("10"));
        Assert.That(readBack.Rules[1].Percent, Is.True);
        Assert.That(readBack.Rules[1].Bottom, Is.False);
        Assert.That(readBack.Rules[2].EqualAverage, Is.True);
    }

    [Test]
    public void WriteString_WritesColorScaleDataBarIconSetAndDifferentialStyle()
    {
        XlsxConditionalFormatting formatting = new XlsxConditionalFormatting();
        formatting.Sqref = "B1:B10";

        XlsxConditionalFormatRule colorScaleRule = CreateRule("colorScale", 1);
        colorScaleRule.HasColorScale = true;
        colorScaleRule.ColorScale.Values.Add(CreateValue("min", ""));
        XlsxConditionalFormatValueObject midpoint = CreateValue("percentile", "50");
        midpoint.GreaterThanOrEqual = false;
        midpoint.HasGreaterThanOrEqual = true;
        colorScaleRule.ColorScale.Values.Add(midpoint);
        colorScaleRule.ColorScale.Values.Add(CreateValue("max", ""));
        colorScaleRule.ColorScale.Colors.Add(CreateRgbColor("FFFF0000"));
        XlsxConditionalFormatColor themeColor = new XlsxConditionalFormatColor();
        themeColor.Theme = 5;
        themeColor.HasTheme = true;
        themeColor.Tint = 0.25d;
        themeColor.HasTint = true;
        colorScaleRule.ColorScale.Colors.Add(themeColor);
        XlsxConditionalFormatColor indexedColor = new XlsxConditionalFormatColor();
        indexedColor.Indexed = 64;
        indexedColor.HasIndexed = true;
        colorScaleRule.ColorScale.Colors.Add(indexedColor);
        formatting.Rules.Add(colorScaleRule);

        XlsxConditionalFormatRule dataBarRule = CreateRule("dataBar", 2);
        dataBarRule.HasDataBar = true;
        dataBarRule.DataBar.MinLength = 10;
        dataBarRule.DataBar.HasMinLength = true;
        dataBarRule.DataBar.MaxLength = 90;
        dataBarRule.DataBar.HasMaxLength = true;
        dataBarRule.DataBar.ShowValue = false;
        dataBarRule.DataBar.HasShowValue = true;
        dataBarRule.DataBar.Values.Add(CreateValue("min", ""));
        dataBarRule.DataBar.Values.Add(CreateValue("max", ""));
        dataBarRule.DataBar.Colors.Add(CreateRgbColor("FF638EC6"));
        formatting.Rules.Add(dataBarRule);

        XlsxConditionalFormatRule iconRule = CreateRule("iconSet", 3);
        iconRule.HasIconSet = true;
        iconRule.IconSet.IconSet = "3TrafficLights1";
        iconRule.IconSet.ShowValue = true;
        iconRule.IconSet.HasShowValue = true;
        iconRule.IconSet.Percent = true;
        iconRule.IconSet.HasPercent = true;
        iconRule.IconSet.Reverse = false;
        iconRule.IconSet.HasReverse = true;
        iconRule.IconSet.Values.Add(CreateValue("percent", "0"));
        iconRule.IconSet.Values.Add(CreateValue("percent", "33"));
        iconRule.IconSet.Values.Add(CreateValue("percent", "67"));
        formatting.Rules.Add(iconRule);

        XlsxConditionalFormatRule expression = CreateRule("expression", 4);
        expression.Formulas.Add("A1>0");
        expression.DifferentialStyleRawXml = "<dxf xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><font><b /></font></dxf>";
        formatting.Rules.Add(expression);

        XlsxConditionalFormattingWriteResult result = new XlsxConditionalFormattingWriter().WriteString(formatting);
        XlsxConditionalFormatting readBack = new XlsxConditionalFormattingReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<colorScale>"));
        Assert.That(result.Xml, Does.Contain("<dataBar minLength=\"10\" maxLength=\"90\" showValue=\"0\">"));
        Assert.That(result.Xml, Does.Contain("<iconSet iconSet=\"3TrafficLights1\" showValue=\"1\" percent=\"1\" reverse=\"0\">"));
        Assert.That(result.Xml, Does.Contain("<formula>A1&gt;0</formula>"));
        Assert.That(result.Xml, Does.Contain("<dxf"));

        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.Rules.Count, Is.EqualTo(4));
        Assert.That(readBack.Rules[0].ColorScale.Values.Count, Is.EqualTo(3));
        Assert.That(readBack.Rules[0].ColorScale.Colors[1].Theme, Is.EqualTo(5));
        Assert.That(readBack.Rules[1].DataBar.ShowValue, Is.False);
        Assert.That(readBack.Rules[1].DataBar.Colors[0].Rgb, Is.EqualTo("FF638EC6"));
        Assert.That(readBack.Rules[2].IconSet.Values.Count, Is.EqualTo(3));
        Assert.That(readBack.Rules[3].Formulas[0], Is.EqualTo("A1>0"));
        Assert.That(readBack.Rules[3].DifferentialStyleRawXml, Does.Contain("<dxf"));
    }

    [Test]
    public void WriteString_PreservesRawXmlAndReportsInvalidInputs()
    {
        XlsxConditionalFormatting raw = new XlsxConditionalFormatting();
        raw.RawXml = "<conditionalFormatting xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" sqref=\"A1\"><cfRule type=\"expression\" priority=\"1\"><formula>TRUE</formula></cfRule></conditionalFormatting>";
        XlsxConditionalFormattingWriteResult rawResult = new XlsxConditionalFormattingWriter().WriteString(raw);

        XlsxConditionalFormatting invalid = new XlsxConditionalFormatting();
        invalid.NamespaceUri = "";
        invalid.Sqref = "XFE1:XFE2";
        invalid.UnknownAttributes = null!;
        invalid.UnknownXml = null!;
        XlsxConditionalFormatRule rule = new XlsxConditionalFormatRule();
        rule.Formulas = null!;
        rule.FormulaRawXml = null!;
        rule.UnknownAttributes = null!;
        rule.UnknownXml = null!;
        rule.HasColorScale = true;
        rule.ColorScale = null!;
        invalid.Rules.Add(rule);

        XlsxConditionalFormattingWriteResult? nullResult = new XlsxConditionalFormattingWriter().WriteString(null!);
        XlsxConditionalFormattingWriteResult invalidResult = new XlsxConditionalFormattingWriter().WriteString(invalid);

        Assert.That(rawResult.IsValid, Is.True);
        Assert.That(rawResult.Xml, Is.EqualTo(raw.RawXml));
        Assert.That(nullResult.IsValid, Is.False);
        Assert.That(invalidResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullResult, "conditionalFormattingWriter.formattingNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "conditionalFormattingWriter.namespaceMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "conditionalFormattingWriter.unknownAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "conditionalFormattingWriter.unknownXmlNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "conditionalFormattingWriter.ruleTypeMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "conditionalFormattingWriter.priorityMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "conditionalFormattingWriter.formulasNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "conditionalFormattingWriter.formulaRawXmlNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "conditionalFormattingWriter.ruleUnknownAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "conditionalFormattingWriter.ruleUnknownXmlNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "conditionalFormattingWriter.colorScaleNull"), Is.True);
    }

    [Test]
    public void WorksheetWriter_WritesStructuredConditionalFormatting()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxConditionalFormatting formatting = new XlsxConditionalFormatting();
        formatting.Sqref = "A1:A5";
        XlsxConditionalFormatRule rule = CreateRule("expression", 1);
        rule.Formulas.Add("A1>0");
        formatting.Rules.Add(rule);
        worksheet.ConditionalFormattings.Add(formatting);

        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadString(write.Xml);

        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<conditionalFormatting sqref=\"A1:A5\">"));
        Assert.That(write.Xml, Does.Contain("<cfRule type=\"expression\" priority=\"1\">"));
        Assert.That(write.Xml, Does.Contain("<formula>A1&gt;0</formula>"));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.ConditionalFormattings.Count, Is.EqualTo(1));
        Assert.That(readBack.ConditionalFormattings[0].Rules.Count, Is.EqualTo(1));
        Assert.That(readBack.ConditionalFormattings[0].Rules[0].Formulas[0], Is.EqualTo("A1>0"));
    }

    static XlsxConditionalFormatRule CreateRule(string type, uint priority)
    {
        XlsxConditionalFormatRule rule = new XlsxConditionalFormatRule();
        rule.Type = type;
        rule.Priority = priority;
        rule.HasPriority = true;
        return rule;
    }

    static XlsxConditionalFormatValueObject CreateValue(string type, string value)
    {
        XlsxConditionalFormatValueObject valueObject = new XlsxConditionalFormatValueObject();
        valueObject.Type = type;
        valueObject.Value = value;
        return valueObject;
    }

    static XlsxConditionalFormatColor CreateRgbColor(string rgb)
    {
        XlsxConditionalFormatColor color = new XlsxConditionalFormatColor();
        color.Rgb = rgb;
        return color;
    }

    static XlsxXmlAttribute CreateAttribute(string prefix, string localName, string namespaceUri, string value)
    {
        XlsxXmlAttribute attribute = new XlsxXmlAttribute();
        attribute.Prefix = prefix;
        attribute.LocalName = localName;
        attribute.NamespaceUri = namespaceUri;
        attribute.Value = value;
        return attribute;
    }

    static bool HasDiagnostic(XlsxConditionalFormattingWriteResult result, string code)
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
