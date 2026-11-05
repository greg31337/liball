using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class RangeEditStructuralDependencyTests
{
    [Test]
    public void InsertRows_UpdatesWorksheetFilterValidationAndConditionalFormattingReferences()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.AddAutoFilter("A1:C5");
        worksheet.AutoFilter.HasSortState = true;
        worksheet.AutoFilter.SortState.Reference = "A2:C5";
        worksheet.AutoFilter.SortState.Conditions.Add(CreateSortCondition("B2:B5"));
        XlsxWorksheetDataValidationResult validation = worksheet.AddCustomDataValidation("B2:B5 D2", "B2>0");
        XlsxWorksheetConditionalFormattingResult formatting = worksheet.AddExpressionConditionalFormatting("C2:C5 E2", "C2>=$B$2");

        XlsxWorksheetRowEditResult insert = worksheet.InsertRows(2, 1);
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadString(write.Xml);

        Assert.That(validation.IsValid, Is.True);
        Assert.That(formatting.IsValid, Is.True);
        Assert.That(insert.IsValid, Is.True);
        Assert.That(insert.StructuralReferenceUpdateCount, Is.GreaterThanOrEqualTo(6));
        Assert.That(insert.FormulaReferenceUpdateCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(worksheet.AutoFilter.Reference, Is.EqualTo("A1:C6"));
        Assert.That(worksheet.AutoFilter.SortState.Reference, Is.EqualTo("A3:C6"));
        Assert.That(worksheet.AutoFilter.SortState.Conditions[0].Reference, Is.EqualTo("B3:B6"));
        Assert.That(worksheet.DataValidations.Items[0].Sqref, Is.EqualTo("B3:B6 D3"));
        Assert.That(worksheet.DataValidations.Items[0].Formula1, Is.EqualTo("B3>0"));
        Assert.That(worksheet.ConditionalFormattings[0].Sqref, Is.EqualTo("C3:C6 E3"));
        Assert.That(worksheet.ConditionalFormattings[0].Rules[0].Formulas[0], Is.EqualTo("C3>=$B$3"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.AutoFilter.Reference, Is.EqualTo("A1:C6"));
        Assert.That(readBack.AutoFilter.SortState.Reference, Is.EqualTo("A3:C6"));
        Assert.That(readBack.DataValidations.Items[0].Sqref, Is.EqualTo("B3:B6 D3"));
        Assert.That(readBack.ConditionalFormattings[0].Sqref, Is.EqualTo("C3:C6 E3"));
    }

    [Test]
    public void DeleteColumns_ShrinksAndRemovesDependentFeatureReferences()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.AddAutoFilter("B1:D5");
        worksheet.AutoFilter.HasSortState = true;
        worksheet.AutoFilter.SortState.Reference = "B2:D5";
        worksheet.AutoFilter.SortState.Conditions.Add(CreateSortCondition("D2:D5"));
        worksheet.AddCustomDataValidation("B2:B5 F2", "F2>0");
        worksheet.AddExpressionConditionalFormatting("B2:B5 F2", "F2>0");

        XlsxWorksheetColumnEditResult delete = worksheet.DeleteColumns(2, 1);
        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadString(write.Xml);

        Assert.That(delete.IsValid, Is.True);
        Assert.That(delete.StructuralReferenceUpdateCount, Is.GreaterThanOrEqualTo(4));
        Assert.That(delete.RemovedStructuralReferenceCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(worksheet.AutoFilter.Reference, Is.EqualTo("B1:C5"));
        Assert.That(worksheet.AutoFilter.SortState.Reference, Is.EqualTo("B2:C5"));
        Assert.That(worksheet.AutoFilter.SortState.Conditions[0].Reference, Is.EqualTo("C2:C5"));
        Assert.That(worksheet.DataValidations.Items[0].Sqref, Is.EqualTo("E2"));
        Assert.That(worksheet.DataValidations.Items[0].Formula1, Is.EqualTo("E2>0"));
        Assert.That(worksheet.ConditionalFormattings[0].Sqref, Is.EqualTo("E2"));
        Assert.That(worksheet.ConditionalFormattings[0].Rules[0].Formulas[0], Is.EqualTo("E2>0"));
        Assert.That(write.IsValid, Is.True);
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.AutoFilter.Reference, Is.EqualTo("B1:C5"));
        Assert.That(readBack.DataValidations.Items[0].Sqref, Is.EqualTo("E2"));
        Assert.That(readBack.ConditionalFormattings[0].Sqref, Is.EqualTo("E2"));
    }

    static XlsxSortCondition CreateSortCondition(string reference)
    {
        XlsxSortCondition condition = new XlsxSortCondition();
        condition.Reference = reference;
        return condition;
    }
}
