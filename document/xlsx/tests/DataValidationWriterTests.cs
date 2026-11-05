using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class DataValidationWriterTests
{
    [Test]
    public void WriteString_WritesStructuredValidationsMessagesFormulasAndUnknowns()
    {
        XlsxDataValidations validations = new XlsxDataValidations();
        validations.HasDisablePrompts = true;
        validations.DisablePrompts = true;
        validations.HasXWindow = true;
        validations.XWindow = 10;
        validations.HasYWindow = true;
        validations.YWindow = 20;
        validations.UnknownAttributes.Add(CreateAttribute("x", "root", "urn:x", "r"));
        validations.ExtensionListRawXml = "<extLst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><ext uri=\"x\" /></extLst>";
        validations.UnknownXml.Add("<x:futureRoot xmlns:x=\"urn:x\" />");

        XlsxDataValidation list = CreateValidation("list", "A1:A5");
        list.AllowBlank = true;
        list.HasAllowBlank = true;
        list.ShowDropDown = false;
        list.HasShowDropDown = true;
        list.ShowInputMessage = true;
        list.HasShowInputMessage = true;
        list.ShowErrorMessage = true;
        list.HasShowErrorMessage = true;
        list.PromptTitle = "Choose";
        list.Prompt = "Pick a value";
        list.ErrorTitle = "Invalid";
        list.Error = "Try again";
        list.Formula1 = "\"North,South\"";
        list.UnknownAttributes.Add(CreateAttribute("x", "item", "urn:x", "i"));
        list.UnknownXml.Add("<x:future xmlns:x=\"urn:x\" />");
        validations.Items.Add(list);

        XlsxDataValidation whole = CreateValidation("whole", "B1:B5");
        whole.Operator = "between";
        whole.Formula1 = "1";
        whole.Formula2 = "10";
        validations.Items.Add(whole);

        XlsxDataValidationWriteResult result = new XlsxDataValidationWriter().WriteString(validations);
        XlsxDataValidations readBack = new XlsxDataValidationReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<dataValidations"));
        Assert.That(result.Xml, Does.Contain("count=\"2\""));
        Assert.That(result.Xml, Does.Contain("disablePrompts=\"1\""));
        Assert.That(result.Xml, Does.Contain("xWindow=\"10\""));
        Assert.That(result.Xml, Does.Contain("yWindow=\"20\""));
        Assert.That(result.Xml, Does.Contain("x:root=\"r\""));
        Assert.That(result.Xml, Does.Contain("type=\"list\""));
        Assert.That(result.Xml, Does.Contain("allowBlank=\"1\""));
        Assert.That(result.Xml, Does.Contain("showDropDown=\"0\""));
        Assert.That(result.Xml, Does.Contain("promptTitle=\"Choose\""));
        Assert.That(result.Xml, Does.Contain("<formula1>\"North,South\"</formula1>"));
        Assert.That(result.Xml, Does.Contain("<formula2>10</formula2>"));
        Assert.That(result.Xml, Does.Contain("<extLst"));
        Assert.That(result.Xml, Does.Contain("<x:futureRoot"));

        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.Count, Is.EqualTo(2));
        Assert.That(readBack.Items.Count, Is.EqualTo(2));
        Assert.That(readBack.Items[0].Formula1, Is.EqualTo("\"North,South\""));
        Assert.That(readBack.Items[0].Prompt, Is.EqualTo("Pick a value"));
        Assert.That(readBack.Items[1].Operator, Is.EqualTo("between"));
        Assert.That(readBack.Items[1].Formula2, Is.EqualTo("10"));
        Assert.That(readBack.UnknownAttributes.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteDataValidationString_WritesSingleItemRoot()
    {
        XlsxDataValidation validation = CreateValidation("custom", "C1:C5");
        validation.ErrorStyle = "warning";
        validation.ImeMode = "disabled";
        validation.Formula1 = "LEN(C1)>0";

        XlsxDataValidationWriteResult result = new XlsxDataValidationWriter().WriteDataValidationString(validation);
        XlsxDataValidations readBack = new XlsxDataValidationReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<dataValidation"));
        Assert.That(result.Xml, Does.Contain("type=\"custom\""));
        Assert.That(result.Xml, Does.Contain("errorStyle=\"warning\""));
        Assert.That(result.Xml, Does.Contain("imeMode=\"disabled\""));
        Assert.That(result.Xml, Does.Contain("<formula1>LEN(C1)&gt;0</formula1>"));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.Items.Count, Is.EqualTo(1));
        Assert.That(readBack.Items[0].Formula1, Is.EqualTo("LEN(C1)>0"));
    }

    [Test]
    public void WriteString_PreservesRawXmlAndReportsInvalidInputs()
    {
        XlsxDataValidations raw = new XlsxDataValidations();
        raw.RawXml = "<dataValidations xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"0\" />";
        XlsxDataValidationWriteResult rawResult = new XlsxDataValidationWriter().WriteString(raw);

        XlsxDataValidations invalid = new XlsxDataValidations();
        invalid.NamespaceUri = "";
        invalid.HasCount = true;
        invalid.Count = 2;
        invalid.UnknownAttributes = null!;
        invalid.UnknownXml = null!;
        XlsxDataValidation item = new XlsxDataValidation();
        item.Sqref = "XFE1:XFE2";
        item.UnknownAttributes = null!;
        item.UnknownXml = null!;
        invalid.Items.Add(item);

        XlsxDataValidationWriteResult? nullResult = new XlsxDataValidationWriter().WriteString(null!);
        XlsxDataValidationWriteResult invalidResult = new XlsxDataValidationWriter().WriteString(invalid);
        XlsxDataValidationWriteResult missingSqref = new XlsxDataValidationWriter().WriteDataValidationString(new XlsxDataValidation());

        Assert.That(rawResult.IsValid, Is.True);
        Assert.That(rawResult.Xml, Is.EqualTo(raw.RawXml));
        Assert.That(nullResult.IsValid, Is.False);
        Assert.That(invalidResult.IsValid, Is.False);
        Assert.That(missingSqref.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullResult, "dataValidationWriter.validationsNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "dataValidationWriter.namespaceMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "dataValidationWriter.countMismatch"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "dataValidationWriter.unknownAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "dataValidationWriter.unknownXmlNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "dataValidationWriter.itemUnknownAttributesNull"), Is.True);
        Assert.That(HasDiagnostic(invalidResult, "dataValidationWriter.itemUnknownXmlNull"), Is.True);
        Assert.That(HasDiagnostic(missingSqref, "dataValidationWriter.sqrefMissing"), Is.True);
    }

    [Test]
    public void WorksheetWriter_WritesStructuredDataValidations()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.HasDataValidations = true;
        worksheet.DataValidations = new XlsxDataValidations();
        XlsxDataValidation validation = CreateValidation("whole", "A1:A5");
        validation.Operator = "between";
        validation.Formula1 = "1";
        validation.Formula2 = "10";
        worksheet.DataValidations.Items.Add(validation);

        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadString(write.Xml);

        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<dataValidations count=\"1\">"));
        Assert.That(write.Xml, Does.Contain("<dataValidation type=\"whole\" operator=\"between\" sqref=\"A1:A5\">"));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.HasDataValidations, Is.True);
        Assert.That(readBack.DataValidations.Items.Count, Is.EqualTo(1));
        Assert.That(readBack.DataValidations.Items[0].Formula2, Is.EqualTo("10"));
    }

    static XlsxDataValidation CreateValidation(string type, string sqref)
    {
        XlsxDataValidation validation = new XlsxDataValidation();
        validation.Type = type;
        validation.Sqref = sqref;
        return validation;
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

    static bool HasDiagnostic(XlsxDataValidationWriteResult result, string code)
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
