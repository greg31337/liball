using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ConditionalFormattingReaderTests
{
    [Test]
    public void ReadString_ParsesStandardAndBuiltInRulesFormulasPrioritiesAndMessages()
    {
        string xml = "<conditionalFormatting xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\" sqref=\"A1:A10 C1:C10\" pivot=\"1\" x:root=\"r\">" +
            "<cfRule type=\"cellIs\" priority=\"1\" dxfId=\"2\" stopIfTrue=\"1\" operator=\"between\" x:rule=\"r\"><formula>1</formula><formula>10</formula><x:future /></cfRule>" +
            "<cfRule type=\"top10\" priority=\"2\" rank=\"5\" percent=\"1\" bottom=\"0\" />" +
            "<cfRule type=\"aboveAverage\" priority=\"3\" aboveAverage=\"0\" equalAverage=\"1\" stdDev=\"2\" />" +
            "<cfRule type=\"containsText\" priority=\"4\" operator=\"containsText\" text=\"Overdue\"><formula>NOT(ISERROR(SEARCH(\"Overdue\",A1)))</formula></cfRule>" +
            "<cfRule type=\"timePeriod\" priority=\"5\" timePeriod=\"last7Days\"><formula>TODAY()-7</formula></cfRule>" +
            "<extLst><ext uri=\"x\" /></extLst>" +
            "<x:futureRoot />" +
            "</conditionalFormatting>";

        XlsxConditionalFormatting formatting = new XlsxConditionalFormattingReader().ReadString(xml);

        Assert.That(formatting.IsValid, Is.True);
        Assert.That(formatting.RawXml, Does.Contain("<conditionalFormatting"));
        Assert.That(formatting.Sqref, Is.EqualTo("A1:A10 C1:C10"));
        Assert.That(formatting.HasPivot, Is.True);
        Assert.That(formatting.Pivot, Is.True);
        Assert.That(formatting.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(formatting.Rules.Count, Is.EqualTo(5));
        Assert.That(formatting.ExtensionListRawXml, Does.Contain("extLst"));
        Assert.That(formatting.UnknownXml.Count, Is.EqualTo(1));

        XlsxConditionalFormatRule cellIs = formatting.Rules[0];
        Assert.That(cellIs.Type, Is.EqualTo("cellIs"));
        Assert.That(cellIs.HasPriority, Is.True);
        Assert.That(cellIs.Priority, Is.EqualTo(1));
        Assert.That(cellIs.HasDxfId, Is.True);
        Assert.That(cellIs.DxfId, Is.EqualTo(2));
        Assert.That(cellIs.HasStopIfTrue, Is.True);
        Assert.That(cellIs.StopIfTrue, Is.True);
        Assert.That(cellIs.Operator, Is.EqualTo("between"));
        Assert.That(cellIs.Formulas.Count, Is.EqualTo(2));
        Assert.That(cellIs.Formulas[0], Is.EqualTo("1"));
        Assert.That(cellIs.Formulas[1], Is.EqualTo("10"));
        Assert.That(cellIs.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(cellIs.UnknownXml.Count, Is.EqualTo(1));

        XlsxConditionalFormatRule top10 = formatting.Rules[1];
        Assert.That(top10.Type, Is.EqualTo("top10"));
        Assert.That(top10.HasRank, Is.True);
        Assert.That(top10.Rank, Is.EqualTo(5));
        Assert.That(top10.HasPercent, Is.True);
        Assert.That(top10.Percent, Is.True);
        Assert.That(top10.HasBottom, Is.True);
        Assert.That(top10.Bottom, Is.False);

        Assert.That(formatting.Rules[2].Type, Is.EqualTo("aboveAverage"));
        Assert.That(formatting.Rules[2].AboveAverage, Is.False);
        Assert.That(formatting.Rules[2].EqualAverage, Is.True);
        Assert.That(formatting.Rules[2].StdDev, Is.EqualTo(2));
        Assert.That(formatting.Rules[3].Text, Is.EqualTo("Overdue"));
        Assert.That(formatting.Rules[4].TimePeriod, Is.EqualTo("last7Days"));
    }

    [Test]
    public void ReadString_ParsesColorScaleDataBarIconSetAndDifferentialStyle()
    {
        string xml = "<conditionalFormatting xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" sqref=\"B1:B10\">" +
            "<cfRule type=\"colorScale\" priority=\"1\"><colorScale><cfvo type=\"min\" /><cfvo type=\"percentile\" val=\"50\" gte=\"0\" /><cfvo type=\"max\" /><color rgb=\"FFFF0000\" /><color theme=\"5\" tint=\"0.25\" /><color indexed=\"64\" /></colorScale></cfRule>" +
            "<cfRule type=\"dataBar\" priority=\"2\"><dataBar minLength=\"10\" maxLength=\"90\" showValue=\"0\"><cfvo type=\"min\" /><cfvo type=\"max\" /><color rgb=\"FF638EC6\" /></dataBar></cfRule>" +
            "<cfRule type=\"iconSet\" priority=\"3\"><iconSet iconSet=\"3TrafficLights1\" showValue=\"1\" percent=\"1\" reverse=\"0\"><cfvo type=\"percent\" val=\"0\" /><cfvo type=\"percent\" val=\"33\" /><cfvo type=\"percent\" val=\"67\" /></iconSet></cfRule>" +
            "<cfRule type=\"expression\" priority=\"4\"><formula>A1&gt;0</formula><dxf><font><b /></font></dxf></cfRule>" +
            "</conditionalFormatting>";

        XlsxConditionalFormatting formatting = new XlsxConditionalFormattingReader().ReadString(xml);

        Assert.That(formatting.IsValid, Is.True);
        Assert.That(formatting.Rules.Count, Is.EqualTo(4));

        XlsxConditionalFormatRule colorScaleRule = formatting.Rules[0];
        Assert.That(colorScaleRule.HasColorScale, Is.True);
        Assert.That(colorScaleRule.ColorScale.Values.Count, Is.EqualTo(3));
        Assert.That(colorScaleRule.ColorScale.Values[1].Type, Is.EqualTo("percentile"));
        Assert.That(colorScaleRule.ColorScale.Values[1].Value, Is.EqualTo("50"));
        Assert.That(colorScaleRule.ColorScale.Values[1].HasGreaterThanOrEqual, Is.True);
        Assert.That(colorScaleRule.ColorScale.Values[1].GreaterThanOrEqual, Is.False);
        Assert.That(colorScaleRule.ColorScale.Colors.Count, Is.EqualTo(3));
        Assert.That(colorScaleRule.ColorScale.Colors[0].Rgb, Is.EqualTo("FFFF0000"));
        Assert.That(colorScaleRule.ColorScale.Colors[1].HasTheme, Is.True);
        Assert.That(colorScaleRule.ColorScale.Colors[1].Theme, Is.EqualTo(5));
        Assert.That(colorScaleRule.ColorScale.Colors[1].HasTint, Is.True);
        Assert.That(colorScaleRule.ColorScale.Colors[1].Tint, Is.EqualTo(0.25d));
        Assert.That(colorScaleRule.ColorScale.Colors[2].HasIndexed, Is.True);
        Assert.That(colorScaleRule.ColorScale.Colors[2].Indexed, Is.EqualTo(64));

        XlsxConditionalFormatRule dataBarRule = formatting.Rules[1];
        Assert.That(dataBarRule.HasDataBar, Is.True);
        Assert.That(dataBarRule.DataBar.HasMinLength, Is.True);
        Assert.That(dataBarRule.DataBar.MinLength, Is.EqualTo(10));
        Assert.That(dataBarRule.DataBar.HasMaxLength, Is.True);
        Assert.That(dataBarRule.DataBar.MaxLength, Is.EqualTo(90));
        Assert.That(dataBarRule.DataBar.HasShowValue, Is.True);
        Assert.That(dataBarRule.DataBar.ShowValue, Is.False);
        Assert.That(dataBarRule.DataBar.Values.Count, Is.EqualTo(2));
        Assert.That(dataBarRule.DataBar.Colors[0].Rgb, Is.EqualTo("FF638EC6"));

        XlsxConditionalFormatRule iconRule = formatting.Rules[2];
        Assert.That(iconRule.HasIconSet, Is.True);
        Assert.That(iconRule.IconSet.IconSet, Is.EqualTo("3TrafficLights1"));
        Assert.That(iconRule.IconSet.HasShowValue, Is.True);
        Assert.That(iconRule.IconSet.ShowValue, Is.True);
        Assert.That(iconRule.IconSet.HasPercent, Is.True);
        Assert.That(iconRule.IconSet.Percent, Is.True);
        Assert.That(iconRule.IconSet.HasReverse, Is.True);
        Assert.That(iconRule.IconSet.Reverse, Is.False);
        Assert.That(iconRule.IconSet.Values.Count, Is.EqualTo(3));

        Assert.That(formatting.Rules[3].Formulas[0], Is.EqualTo("A1>0"));
        Assert.That(formatting.Rules[3].DifferentialStyleRawXml, Does.Contain("<dxf"));
    }

    [Test]
    public void ReadString_ReportsMissingInvalidRootInvalidValuesAndInvalidSqref()
    {
        XlsxConditionalFormatting missingRoot = new XlsxConditionalFormattingReader().ReadString("");
        XlsxConditionalFormatting invalidRoot = new XlsxConditionalFormattingReader().ReadString("<bad />");
        XlsxConditionalFormatting invalidValues = new XlsxConditionalFormattingReader().ReadString("<conditionalFormatting xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" sqref=\"A1 XFE1:XFE2\" pivot=\"maybe\"><cfRule type=\"cellIs\" priority=\"bad\" dxfId=\"bad\" stopIfTrue=\"maybe\"><colorScale><cfvo type=\"min\" gte=\"maybe\" /><color indexed=\"bad\" tint=\"bad\" /></colorScale></cfRule></conditionalFormatting>");

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(invalidValues.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "conditionalFormatting.missingRoot"), Is.True);
        Assert.That(HasDiagnostic(invalidRoot, "conditionalFormatting.invalidRoot"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.boolean.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.double.invalid"), Is.True);
        Assert.That(invalidValues.Rules.Count, Is.EqualTo(1));
    }

    [Test]
    public void WorksheetReader_ParsesAndPreservesConditionalFormatting()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<sheetData />" +
            "<conditionalFormatting sqref=\"A1:A5\"><cfRule type=\"expression\" priority=\"1\"><formula>A1&gt;0</formula></cfRule></conditionalFormatting>" +
            "</worksheet>";

        XlsxWorksheetPart worksheet = new XlsxWorksheetPartReader().ReadString(xml);
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(worksheet);

        Assert.That(worksheet.IsValid, Is.True);
        Assert.That(worksheet.ConditionalFormattings.Count, Is.EqualTo(1));
        Assert.That(worksheet.ConditionalFormattings[0].Rules.Count, Is.EqualTo(1));
        Assert.That(worksheet.ConditionalFormattings[0].Rules[0].Formulas[0], Is.EqualTo("A1>0"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<conditionalFormatting"));
        Assert.That(write.Xml, Does.Contain("<formula>A1&gt;0</formula>"));
    }

    static bool HasDiagnostic(XlsxConditionalFormatting formatting, string code)
    {
        foreach (XlsxDiagnostic diagnostic in formatting.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
