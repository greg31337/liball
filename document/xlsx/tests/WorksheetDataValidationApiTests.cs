using System.Collections.Generic;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetDataValidationApiTests
{
    [Test]
    public void DataValidationApis_AddAllValidationTypesAndWriteWorksheet()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetDataValidationResult list = worksheet.AddListDataValidation("A1:A5", CreateValues("North", "South"));
        XlsxWorksheetDataValidationResult allowBlank = worksheet.SetDataValidationAllowBlank(list.DataValidation, true);
        XlsxWorksheetDataValidationResult dropDown = worksheet.SetDataValidationShowDropDown(list.DataValidation!, false);
        XlsxWorksheetDataValidationResult messages = worksheet.SetDataValidationMessages(list.DataValidation!, true, "Choose", "Pick a value", true, "Invalid", "Try again", "warning");
        XlsxWorksheetDataValidationResult whole = worksheet.AddWholeNumberDataValidation("B1:B5", "between", "1", "10");
        XlsxWorksheetDataValidationResult decimalValidation = worksheet.AddDecimalDataValidation("C1:C5", "greaterThan", "1.5", "");
        XlsxWorksheetDataValidationResult date = worksheet.AddDateDataValidation("D1:D5", "lessThanOrEqual", "DATE(2024,12,31)", "");
        XlsxWorksheetDataValidationResult time = worksheet.AddTimeDataValidation("E1:E5", "between", "TIME(8,0,0)", "TIME(17,0,0)");
        XlsxWorksheetDataValidationResult textLength = worksheet.AddTextLengthDataValidation("F1:F5", "lessThan", "20", "");
        XlsxWorksheetDataValidationResult custom = worksheet.AddCustomDataValidation("G1:G5", "LEN(G1)>0");
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadString(write.Xml);

        Assert.That(list.IsValid, Is.True);
        Assert.That(allowBlank.IsValid, Is.True);
        Assert.That(dropDown.IsValid, Is.True);
        Assert.That(messages.IsValid, Is.True);
        Assert.That(whole.IsValid, Is.True);
        Assert.That(decimalValidation.IsValid, Is.True);
        Assert.That(date.IsValid, Is.True);
        Assert.That(time.IsValid, Is.True);
        Assert.That(textLength.IsValid, Is.True);
        Assert.That(custom.IsValid, Is.True);
        Assert.That(worksheet.HasDataValidations, Is.True);
        Assert.That(worksheet.DataValidations.Items.Count, Is.EqualTo(7));
        Assert.That(worksheet.DataValidations.Count, Is.EqualTo(7));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<dataValidations count=\"7\">"));
        Assert.That(write.Xml, Does.Contain("type=\"list\""));
        Assert.That(write.Xml, Does.Contain("allowBlank=\"1\""));
        Assert.That(write.Xml, Does.Contain("showDropDown=\"0\""));
        Assert.That(write.Xml, Does.Contain("promptTitle=\"Choose\""));
        Assert.That(write.Xml, Does.Contain("errorStyle=\"warning\""));
        Assert.That(write.Xml, Does.Contain("<formula1>\"North,South\"</formula1>"));
        Assert.That(write.Xml, Does.Contain("type=\"whole\""));
        Assert.That(write.Xml, Does.Contain("type=\"decimal\""));
        Assert.That(write.Xml, Does.Contain("type=\"date\""));
        Assert.That(write.Xml, Does.Contain("type=\"time\""));
        Assert.That(write.Xml, Does.Contain("type=\"textLength\""));
        Assert.That(write.Xml, Does.Contain("type=\"custom\""));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.HasDataValidations, Is.True);
        Assert.That(readBack.DataValidations.Items.Count, Is.EqualTo(7));
        Assert.That(readBack.DataValidations.Items[0].Formula1, Is.EqualTo("\"North,South\""));
        Assert.That(readBack.DataValidations.Items[1].Formula2, Is.EqualTo("10"));
        Assert.That(readBack.DataValidations.Items[6].Formula1, Is.EqualTo("LEN(G1)>0"));
    }

    [Test]
    public void DataValidationApis_NormalizeSqrefUseFormulaListsAndClear()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetDataValidationResult any = worksheet.AddDataValidation("B2:A1 C3");
        XlsxWorksheetDataValidationResult listFormula = worksheet.AddListDataValidationFormula("D1:D5", "Sheet2!$A$1:$A$3");
        XlsxWorksheetDataValidationResult clear = worksheet.ClearDataValidations();

        Assert.That(any.IsValid, Is.True);
        Assert.That(any.DataValidation!.Type, Is.EqualTo(""));
        Assert.That(any.DataValidation.Sqref, Is.EqualTo("A1:B2 C3"));
        Assert.That(listFormula.IsValid, Is.True);
        Assert.That(listFormula.DataValidation!.Type, Is.EqualTo("list"));
        Assert.That(listFormula.DataValidation.Formula1, Is.EqualTo("Sheet2!$A$1:$A$3"));
        Assert.That(clear.IsValid, Is.True);
        Assert.That(clear.RemovedCount, Is.EqualTo(2));
        Assert.That(worksheet.HasDataValidations, Is.False);
    }

    [Test]
    public void DataValidationApis_ReportInvalidStates()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetDataValidationResult missingRange = worksheet.AddWholeNumberDataValidation("", "between", "1", "10");
        XlsxWorksheetDataValidationResult invalidRange = worksheet.AddDecimalDataValidation("XFE1:XFE2", "greaterThan", "1", "");
        XlsxWorksheetDataValidationResult? nullValues = worksheet.AddListDataValidation("A1:A2", null!);
        XlsxWorksheetDataValidationResult emptyValues = worksheet.AddListDataValidation("A1:A2", new List<string>());
        XlsxWorksheetDataValidationResult? nullMessage = worksheet.SetDataValidationMessages(null!, true, "Prompt", "Text", true, "Error", "Text", "stop");
        worksheet.DataValidations = new XlsxDataValidations();
        worksheet.DataValidations.Items = null!;
        worksheet.HasDataValidations = true;
        XlsxWorksheetDataValidationResult nullItems = worksheet.AddCustomDataValidation("A1", "A1>0");

        Assert.That(missingRange.IsValid, Is.False);
        Assert.That(invalidRange.IsValid, Is.False);
        Assert.That(nullValues.IsValid, Is.False);
        Assert.That(emptyValues.IsValid, Is.False);
        Assert.That(nullMessage.IsValid, Is.False);
        Assert.That(nullItems.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRange, "worksheetDataValidation.sqrefMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidRange, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(nullValues, "worksheetDataValidation.listValuesNull"), Is.True);
        Assert.That(HasDiagnostic(emptyValues, "worksheetDataValidation.listValuesEmpty"), Is.True);
        Assert.That(HasDiagnostic(nullMessage, "worksheetDataValidation.validationNull"), Is.True);
        Assert.That(HasDiagnostic(nullItems, "worksheetDataValidation.itemsNull"), Is.True);
    }

    static List<string> CreateValues(string first, string second)
    {
        List<string> values = new List<string>();
        values.Add(first);
        values.Add(second);
        return values;
    }

    static bool HasDiagnostic(XlsxWorksheetDataValidationResult result, string code)
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
