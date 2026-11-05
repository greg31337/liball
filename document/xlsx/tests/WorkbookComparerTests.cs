using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookComparerTests
{
    [Test]
    public void Compare_EqualWorkbooksReportsNoDifferences()
    {
        XlsxWorkbook left = CreateComparableWorkbook();
        XlsxWorkbook right = CreateComparableWorkbook();
        IXlsxWorkbookComparerOperations comparer = new XlsxWorkbookComparerOperations();

        XlsxWorkbookCompareResult result = comparer.Compare(left, right);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.AreEqual, Is.True);
        Assert.That(result.DifferenceCount, Is.EqualTo(0));
        Assert.That(result.Differences.Count, Is.EqualTo(0));
        Assert.That(result.Validation.Diagnostics.Count, Is.EqualTo(0));
    }

    [Test]
    public void Compare_DetectsWorkbookStructurePropertiesRelationshipsCellsAndUnknownParts()
    {
        XlsxWorkbook left = CreateComparableWorkbook();
        XlsxWorkbook right = CreateComparableWorkbook();
        right.PackageVariant = XlsxPackageVariant.Xltx;
        right.WorkbookPart.Sheets[0].Name = "Changed";
        right.WorkbookPart.Sheets[0].State = "hidden";
        right.WorkbookPart.DefinedNames[0].Text = "Sheet1!$B$2";
        right.WorkbookRelationships.GetRelationship("rId1")!.Target = "worksheets/other.xml";
        right.CoreProperties.Creator = "Grace";
        right.CustomProperties.GetProperty("Department")!.TextValue = "Operations";
        right.Worksheets[0].SetNumberValue("A1", 42);
        right.UnknownParts.GetPart("/xl/custom/item.xml")!.Data = Encoding.UTF8.GetBytes("<item value=\"2\" />");

        XlsxWorkbookCompareResult result = left.CompareTo(right);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.AreEqual, Is.False);
        Assert.That(result.DifferenceCount, Is.GreaterThanOrEqualTo(9));
        Assert.That(HasDifference(result, "workbookCompare.packageVariant"), Is.True);
        Assert.That(HasDifference(result, "workbookCompare.sheetName"), Is.True);
        Assert.That(HasDifference(result, "workbookCompare.sheetState"), Is.True);
        Assert.That(HasDifference(result, "workbookCompare.definedNameValue"), Is.True);
        Assert.That(HasDifference(result, "workbookCompare.relationshipTarget"), Is.True);
        Assert.That(HasDifference(result, "workbookCompare.coreCreator"), Is.True);
        Assert.That(HasDifference(result, "workbookCompare.customPropertyValue"), Is.True);
        Assert.That(HasDifference(result, "workbookCompare.cellValue"), Is.True);
        Assert.That(HasDifference(result, "workbookCompare.unknownPartBytes"), Is.True);
        Assert.That(HasDiagnostic(result, "workbookCompare.cellValue"), Is.True);
    }

    [Test]
    public void Compare_OptionsCanDisableExpensiveOrPackageScopes()
    {
        XlsxWorkbook left = CreateComparableWorkbook();
        XlsxWorkbook right = CreateComparableWorkbook();
        right.WorkbookRelationships.GetRelationship("rId1")!.Target = "worksheets/other.xml";
        right.Worksheets[0].SetNumberValue("A1", 42);
        right.UnknownParts.GetPart("/xl/custom/item.xml")!.Data = Encoding.UTF8.GetBytes("<item value=\"2\" />");
        XlsxWorkbookCompareOptions options = new XlsxWorkbookCompareOptions();
        options.CompareRelationships = false;
        options.CompareWorksheetCells = false;
        options.CompareUnknownParts = false;

        XlsxWorkbookCompareResult result = new XlsxWorkbookComparerOperations().CompareWithOptions(left, right, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.AreEqual, Is.True);
        Assert.That(result.DifferenceCount, Is.EqualTo(0));
    }

    [Test]
    public void Compare_ReportsNullInputsAsValidationErrors()
    {
        XlsxWorkbookCompareResult leftNull = XlsxWorkbookComparer.Compare(null, new XlsxWorkbook());
        XlsxWorkbookCompareResult rightNull = XlsxWorkbookComparer.Compare(new XlsxWorkbook(), null);

        Assert.That(leftNull.IsValid, Is.False);
        Assert.That(rightNull.IsValid, Is.False);
        Assert.That(HasDiagnostic(leftNull, "workbookCompare.leftNull"), Is.True);
        Assert.That(HasDiagnostic(rightNull, "workbookCompare.rightNull"), Is.True);
    }

    static XlsxWorkbook CreateComparableWorkbook()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.CoreProperties.Title = "Budget";
        workbook.CoreProperties.Creator = "Ada";
        workbook.CoreProperties.LastModifiedBy = "Ada";
        workbook.HasCoreProperties = true;
        workbook.ExtendedProperties.Manager = "Grace";
        workbook.ExtendedProperties.Company = "Example";
        workbook.HasExtendedProperties = true;
        workbook.CustomProperties.Properties.Add(CreateTextProperty(2, "Department", "Finance"));
        workbook.HasCustomProperties = true;

        XlsxWorkbookDefinedName name = new XlsxWorkbookDefinedName();
        name.Name = "Input";
        name.Text = "Sheet1!$A$1";
        workbook.WorkbookPart.DefinedNames.Add(name);

        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.PartPath = XlsxPackagePaths.Worksheet1;
        worksheet.SetTextValue("A1", "Hello");
        worksheet.SetNumberValue("B2", 3);
        workbook.Worksheets.Add(worksheet);

        workbook.UnknownParts.AddPart("/xl/custom/item.xml", "application/vnd.example.item+xml", Encoding.UTF8.GetBytes("<item value=\"1\" />"));
        return workbook;
    }

    static XlsxCustomProperty CreateTextProperty(int propertyId, string name, string value)
    {
        XlsxCustomProperty property = new XlsxCustomProperty();
        property.FormatId = XlsxCustomPropertiesWriter.DefaultFormatId;
        property.PropertyId = propertyId;
        property.Name = name;
        property.ValueType = "lpwstr";
        property.TextValue = value;
        property.HasTextValue = true;
        return property;
    }

    static bool HasDifference(XlsxWorkbookCompareResult result, string code)
    {
        foreach (XlsxWorkbookDifference difference in result.Differences)
        {
            if (difference.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxWorkbookCompareResult result, string code)
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
