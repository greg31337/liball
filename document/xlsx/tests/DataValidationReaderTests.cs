using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class DataValidationReaderTests
{
    [Test]
    public void ReadString_ParsesMajorValidationTypesMessagesFormulasAndUnknowns()
    {
        string xml = "<dataValidations xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\" count=\"7\" disablePrompts=\"1\" xWindow=\"10\" yWindow=\"20\" x:root=\"r\">" +
            "<dataValidation type=\"list\" allowBlank=\"1\" showDropDown=\"0\" showInputMessage=\"1\" showErrorMessage=\"1\" sqref=\"A1:A5 C1:C5\" promptTitle=\"Choose\" prompt=\"Pick a value\" errorTitle=\"Invalid\" error=\"Try again\" x:item=\"i\"><formula1>\"North,South\"</formula1><x:future /></dataValidation>" +
            "<dataValidation type=\"whole\" operator=\"between\" sqref=\"B1:B5\"><formula1>1</formula1><formula2>10</formula2></dataValidation>" +
            "<dataValidation type=\"decimal\" operator=\"greaterThan\" sqref=\"D1:D5\"><formula1>1.5</formula1></dataValidation>" +
            "<dataValidation type=\"date\" operator=\"lessThanOrEqual\" sqref=\"E1:E5\"><formula1>DATE(2024,12,31)</formula1></dataValidation>" +
            "<dataValidation type=\"time\" operator=\"between\" sqref=\"F1:F5\"><formula1>TIME(8,0,0)</formula1><formula2>TIME(17,0,0)</formula2></dataValidation>" +
            "<dataValidation type=\"textLength\" operator=\"lessThan\" sqref=\"G1:G5\"><formula1>20</formula1></dataValidation>" +
            "<dataValidation type=\"custom\" errorStyle=\"warning\" imeMode=\"disabled\" sqref=\"H1:H5\"><formula1>LEN(H1)&gt;0</formula1></dataValidation>" +
            "<extLst><ext uri=\"x\" /></extLst>" +
            "<x:futureRoot />" +
            "</dataValidations>";

        XlsxDataValidations validations = new XlsxDataValidationReader().ReadString(xml);

        Assert.That(validations.IsValid, Is.True);
        Assert.That(validations.RawXml, Does.Contain("<dataValidations"));
        Assert.That(validations.HasCount, Is.True);
        Assert.That(validations.Count, Is.EqualTo(7));
        Assert.That(validations.HasDisablePrompts, Is.True);
        Assert.That(validations.DisablePrompts, Is.True);
        Assert.That(validations.HasXWindow, Is.True);
        Assert.That(validations.XWindow, Is.EqualTo(10));
        Assert.That(validations.HasYWindow, Is.True);
        Assert.That(validations.YWindow, Is.EqualTo(20));
        Assert.That(validations.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(validations.Items.Count, Is.EqualTo(7));
        Assert.That(validations.ExtensionListRawXml, Does.Contain("extLst"));
        Assert.That(validations.UnknownXml.Count, Is.EqualTo(1));

        XlsxDataValidation list = validations.Items[0];
        Assert.That(list.Type, Is.EqualTo("list"));
        Assert.That(list.HasAllowBlank, Is.True);
        Assert.That(list.AllowBlank, Is.True);
        Assert.That(list.HasShowDropDown, Is.True);
        Assert.That(list.ShowDropDown, Is.False);
        Assert.That(list.HasShowInputMessage, Is.True);
        Assert.That(list.ShowInputMessage, Is.True);
        Assert.That(list.HasShowErrorMessage, Is.True);
        Assert.That(list.ShowErrorMessage, Is.True);
        Assert.That(list.Sqref, Is.EqualTo("A1:A5 C1:C5"));
        Assert.That(list.PromptTitle, Is.EqualTo("Choose"));
        Assert.That(list.Prompt, Is.EqualTo("Pick a value"));
        Assert.That(list.ErrorTitle, Is.EqualTo("Invalid"));
        Assert.That(list.Error, Is.EqualTo("Try again"));
        Assert.That(list.Formula1, Is.EqualTo("\"North,South\""));
        Assert.That(list.Formula1RawXml, Does.Contain("formula1"));
        Assert.That(list.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(list.UnknownXml.Count, Is.EqualTo(1));

        Assert.That(validations.Items[1].Type, Is.EqualTo("whole"));
        Assert.That(validations.Items[1].Operator, Is.EqualTo("between"));
        Assert.That(validations.Items[1].Formula1, Is.EqualTo("1"));
        Assert.That(validations.Items[1].Formula2, Is.EqualTo("10"));
        Assert.That(validations.Items[2].Type, Is.EqualTo("decimal"));
        Assert.That(validations.Items[3].Type, Is.EqualTo("date"));
        Assert.That(validations.Items[4].Type, Is.EqualTo("time"));
        Assert.That(validations.Items[5].Type, Is.EqualTo("textLength"));
        Assert.That(validations.Items[6].Type, Is.EqualTo("custom"));
        Assert.That(validations.Items[6].ErrorStyle, Is.EqualTo("warning"));
        Assert.That(validations.Items[6].ImeMode, Is.EqualTo("disabled"));
        Assert.That(validations.Items[6].Formula1, Is.EqualTo("LEN(H1)>0"));
    }

    [Test]
    public void ReadString_ParsesSingleDataValidationRoot()
    {
        string xml = "<dataValidation xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" type=\"list\" sqref=\"A1\"><formula1>Sheet2!$A$1:$A$3</formula1></dataValidation>";

        XlsxDataValidations validations = new XlsxDataValidationReader().ReadString(xml);

        Assert.That(validations.IsValid, Is.True);
        Assert.That(validations.HasCount, Is.True);
        Assert.That(validations.Count, Is.EqualTo(1));
        Assert.That(validations.Items.Count, Is.EqualTo(1));
        Assert.That(validations.Items[0].Type, Is.EqualTo("list"));
        Assert.That(validations.Items[0].Sqref, Is.EqualTo("A1"));
        Assert.That(validations.Items[0].Formula1, Is.EqualTo("Sheet2!$A$1:$A$3"));
    }

    [Test]
    public void ReadString_ReportsMissingInvalidRootInvalidValuesAndInvalidSqref()
    {
        XlsxDataValidations missingRoot = new XlsxDataValidationReader().ReadString("");
        XlsxDataValidations invalidRoot = new XlsxDataValidationReader().ReadString("<bad />");
        XlsxDataValidations invalidValues = new XlsxDataValidationReader().ReadString("<dataValidations xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"bad\" disablePrompts=\"maybe\" xWindow=\"bad\"><dataValidation allowBlank=\"maybe\" showDropDown=\"maybe\" sqref=\"A1 XFE1:XFE2\"><formula1>1</formula1></dataValidation></dataValidations>");

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(invalidValues.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "dataValidations.missingRoot"), Is.True);
        Assert.That(HasDiagnostic(invalidRoot, "dataValidations.invalidRoot"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.boolean.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "a1.cellReference.columnRange"), Is.True);
        Assert.That(invalidValues.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public void WorksheetReader_ParsesAndPreservesDataValidations()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<sheetData />" +
            "<dataValidations count=\"1\"><dataValidation type=\"whole\" operator=\"between\" sqref=\"A1:A5\"><formula1>1</formula1><formula2>10</formula2></dataValidation></dataValidations>" +
            "</worksheet>";

        XlsxWorksheetPart worksheet = new XlsxWorksheetPartReader().ReadString(xml);
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(worksheet);

        Assert.That(worksheet.IsValid, Is.True);
        Assert.That(worksheet.HasDataValidations, Is.True);
        Assert.That(worksheet.DataValidations.Items.Count, Is.EqualTo(1));
        Assert.That(worksheet.DataValidations.Items[0].Type, Is.EqualTo("whole"));
        Assert.That(worksheet.DataValidations.Items[0].Formula2, Is.EqualTo("10"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<dataValidations"));
        Assert.That(write.Xml, Does.Contain("<formula2>10</formula2>"));
    }

    static bool HasDiagnostic(XlsxDataValidations validations, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validations.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
