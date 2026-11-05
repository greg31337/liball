using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetConditionalFormattingApiTests
{
    [Test]
    public void ConditionalFormattingApis_AddCommonRulesAndWriteWorksheet()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetConditionalFormattingResult expression = worksheet.AddExpressionConditionalFormatting("B2:A1 C3", "A1>0");
        XlsxWorksheetConditionalFormattingResult styled = worksheet.SetConditionalFormattingDxfId(expression.Rule, 2);
        XlsxWorksheetConditionalFormattingResult stop = worksheet.SetConditionalFormattingStopIfTrue(expression.Rule!, true);
        XlsxWorksheetConditionalFormattingResult cellIs = worksheet.AddCellIsConditionalFormatting("D1:D5", "between", "1", "10");
        XlsxWorksheetConditionalFormattingResult text = worksheet.AddContainsTextConditionalFormatting("E1:E5", "Overdue");
        XlsxWorksheetConditionalFormattingResult top10 = worksheet.AddTop10ConditionalFormatting("F1:F5", 5, true, false);
        XlsxWorksheetConditionalFormattingResult belowAverage = worksheet.AddAboveAverageConditionalFormatting("G1:G5", false, true, 2);
        XlsxWorksheetConditionalFormattingResult duplicate = worksheet.AddDuplicateValuesConditionalFormatting("H1:H5");
        XlsxWorksheetConditionalFormattingResult unique = worksheet.AddUniqueValuesConditionalFormatting("I1:I5");
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadString(write.Xml);

        Assert.That(expression.IsValid, Is.True);
        Assert.That(styled.IsValid, Is.True);
        Assert.That(stop.IsValid, Is.True);
        Assert.That(cellIs.IsValid, Is.True);
        Assert.That(text.IsValid, Is.True);
        Assert.That(top10.IsValid, Is.True);
        Assert.That(belowAverage.IsValid, Is.True);
        Assert.That(duplicate.IsValid, Is.True);
        Assert.That(unique.IsValid, Is.True);
        Assert.That(worksheet.ConditionalFormattings.Count, Is.EqualTo(7));
        Assert.That(expression.ConditionalFormatting!.Sqref, Is.EqualTo("A1:B2 C3"));
        Assert.That(expression.Rule!.Priority, Is.EqualTo(1));
        Assert.That(cellIs.Rule!.Priority, Is.EqualTo(2));
        Assert.That(unique.Rule!.Priority, Is.EqualTo(7));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<conditionalFormatting sqref=\"A1:B2 C3\">"));
        Assert.That(write.Xml, Does.Contain("<cfRule type=\"expression\" priority=\"1\" dxfId=\"2\" stopIfTrue=\"1\">"));
        Assert.That(write.Xml, Does.Contain("<formula>A1&gt;0</formula>"));
        Assert.That(write.Xml, Does.Contain("<cfRule type=\"cellIs\" priority=\"2\" operator=\"between\">"));
        Assert.That(write.Xml, Does.Contain("<formula>10</formula>"));
        Assert.That(write.Xml, Does.Contain("type=\"containsText\""));
        Assert.That(write.Xml, Does.Contain("text=\"Overdue\""));
        Assert.That(write.Xml, Does.Contain("SEARCH(\"Overdue\",E1)"));
        Assert.That(write.Xml, Does.Contain("type=\"top10\""));
        Assert.That(write.Xml, Does.Contain("rank=\"5\""));
        Assert.That(write.Xml, Does.Contain("bottom=\"0\""));
        Assert.That(write.Xml, Does.Contain("type=\"aboveAverage\""));
        Assert.That(write.Xml, Does.Contain("aboveAverage=\"0\""));
        Assert.That(write.Xml, Does.Contain("equalAverage=\"1\""));
        Assert.That(write.Xml, Does.Contain("stdDev=\"2\""));
        Assert.That(write.Xml, Does.Contain("type=\"duplicateValues\""));
        Assert.That(write.Xml, Does.Contain("type=\"uniqueValues\""));

        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.ConditionalFormattings.Count, Is.EqualTo(7));
        Assert.That(readBack.ConditionalFormattings[0].Rules[0].DxfId, Is.EqualTo(2));
        Assert.That(readBack.ConditionalFormattings[0].Rules[0].StopIfTrue, Is.True);
        Assert.That(readBack.ConditionalFormattings[1].Rules[0].Formulas[1], Is.EqualTo("10"));
        Assert.That(readBack.ConditionalFormattings[2].Rules[0].Text, Is.EqualTo("Overdue"));
        Assert.That(readBack.ConditionalFormattings[3].Rules[0].Rank, Is.EqualTo(5));
        Assert.That(readBack.ConditionalFormattings[4].Rules[0].AboveAverage, Is.False);
        Assert.That(readBack.ConditionalFormattings[5].Rules[0].Type, Is.EqualTo("duplicateValues"));
        Assert.That(readBack.ConditionalFormattings[6].Rules[0].Type, Is.EqualTo("uniqueValues"));
    }

    [Test]
    public void ConditionalFormattingApis_AddVisualRulesAndClear()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetConditionalFormattingResult colorScale = worksheet.AddColorScaleConditionalFormatting("A1:A10", "FFFF0000", "percentile", "50", "FFFFFF00", "FF00B050");
        XlsxWorksheetConditionalFormattingResult dataBar = worksheet.AddDataBarConditionalFormatting("B1:B10", "FF638EC6");
        XlsxWorksheetConditionalFormattingResult iconSet = worksheet.AddIconSetConditionalFormatting("C1:C10", "3TrafficLights1");
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadString(write.Xml);
        XlsxWorksheetConditionalFormattingResult clear = worksheet.ClearConditionalFormattings();

        Assert.That(colorScale.IsValid, Is.True);
        Assert.That(dataBar.IsValid, Is.True);
        Assert.That(iconSet.IsValid, Is.True);
        Assert.That(colorScale.Rule!.HasColorScale, Is.True);
        Assert.That(colorScale.Rule.ColorScale.Values.Count, Is.EqualTo(3));
        Assert.That(colorScale.Rule.ColorScale.Colors.Count, Is.EqualTo(3));
        Assert.That(dataBar.Rule!.HasDataBar, Is.True);
        Assert.That(dataBar.Rule.DataBar.ShowValue, Is.True);
        Assert.That(iconSet.Rule!.HasIconSet, Is.True);
        Assert.That(iconSet.Rule.IconSet.Values.Count, Is.EqualTo(3));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<colorScale>"));
        Assert.That(write.Xml, Does.Contain("<cfvo type=\"percentile\" val=\"50\" />"));
        Assert.That(write.Xml, Does.Contain("<dataBar showValue=\"1\">"));
        Assert.That(write.Xml, Does.Contain("<iconSet iconSet=\"3TrafficLights1\" percent=\"1\">"));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.ConditionalFormattings[0].Rules[0].ColorScale.Colors[2].Rgb, Is.EqualTo("FF00B050"));
        Assert.That(readBack.ConditionalFormattings[1].Rules[0].DataBar.Colors[0].Rgb, Is.EqualTo("FF638EC6"));
        Assert.That(readBack.ConditionalFormattings[2].Rules[0].IconSet.IconSet, Is.EqualTo("3TrafficLights1"));
        Assert.That(clear.IsValid, Is.True);
        Assert.That(clear.RemovedCount, Is.EqualTo(3));
        Assert.That(worksheet.ConditionalFormattings.Count, Is.EqualTo(0));
    }

    [Test]
    public void ConditionalFormattingApis_ReportInvalidStates()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetConditionalFormattingResult missingRange = worksheet.AddExpressionConditionalFormatting("", "A1>0");
        XlsxWorksheetConditionalFormattingResult invalidRange = worksheet.AddCellIsConditionalFormatting("XFE1:XFE2", "greaterThan", "1", "");
        XlsxWorksheetConditionalFormattingResult? nullRuleDxf = worksheet.SetConditionalFormattingDxfId(null, 1);
        XlsxWorksheetConditionalFormattingResult? nullRuleStop = worksheet.SetConditionalFormattingStopIfTrue(null!, true);
        worksheet.ConditionalFormattings = null!;
        XlsxWorksheetConditionalFormattingResult nullItems = worksheet.AddDuplicateValuesConditionalFormatting("A1:A2");

        Assert.That(missingRange.IsValid, Is.False);
        Assert.That(invalidRange.IsValid, Is.False);
        Assert.That(nullRuleDxf.IsValid, Is.False);
        Assert.That(nullRuleStop.IsValid, Is.False);
        Assert.That(nullItems.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRange, "worksheetConditionalFormatting.sqrefMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidRange, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(nullRuleDxf, "worksheetConditionalFormatting.ruleNull"), Is.True);
        Assert.That(HasDiagnostic(nullRuleStop, "worksheetConditionalFormatting.ruleNull"), Is.True);
        Assert.That(HasDiagnostic(nullItems, "worksheetConditionalFormatting.itemsNull"), Is.True);
    }

    static bool HasDiagnostic(XlsxWorksheetConditionalFormattingResult result, string code)
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
