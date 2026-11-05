using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookApiTests
{
    [Test]
    public void CreateAndSaveBytes_ProducesMinimumPackageThatCanBeOpened()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();

        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxValidationResult minimumValidation = new XlsxMinimumPackageValidator().ValidateBytes(saveResult.Bytes, new XlsxReaderOptions());
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saveResult.Bytes, new XlsxReaderOptions());

        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(saveResult.Bytes.Length, Is.GreaterThan(0));
        Assert.That(minimumValidation.IsValid, Is.True);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.WorkbookPart.Sheets.Count, Is.EqualTo(1));
        Assert.That(openResult.Workbook.WorkbookPart.Sheets[0].Name, Is.EqualTo("Sheet1"));
        Assert.That(openResult.Workbook.WorkbookRelationships.Relationships.Count, Is.EqualTo(1));
        Assert.That(package.GetEntry(XlsxPackagePaths.Workbook), Is.Not.Null);
        Assert.That(package.GetEntry(XlsxPackagePaths.WorkbookRelationships), Is.Not.Null);
        Assert.That(package.GetEntry(XlsxPackagePaths.Worksheet1), Is.Not.Null);
    }

    [Test]
    public void SaveBytesAndOpenBytes_RoundTripDocumentPropertiesAndUnknownParts()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.CoreProperties.Title = "Budget";
        workbook.CoreProperties.Creator = "Ada";
        workbook.HasCoreProperties = true;
        workbook.ExtendedProperties.Application = "xlsx";
        workbook.ExtendedProperties.Company = "Example";
        workbook.HasExtendedProperties = true;
        workbook.CustomProperties.Properties.Add(CreateTextProperty(2, "Department", "Finance"));
        workbook.HasCustomProperties = true;
        workbook.UnknownParts.AddPart("/xl/custom/item1.xml", "application/vnd.example.item+xml", System.Text.Encoding.UTF8.GetBytes("<item value=\"1\" />"));

        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes);
        XlsxWorkbookSaveResult secondSave = openResult.Workbook!.SaveBytes();
        XlsxZipPackageReadResult secondPackage = new XlsxZipPackageReader().ReadBytes(secondSave.Bytes, new XlsxReaderOptions());

        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook.HasCoreProperties, Is.True);
        Assert.That(openResult.Workbook.CoreProperties.Title, Is.EqualTo("Budget"));
        Assert.That(openResult.Workbook.HasExtendedProperties, Is.True);
        Assert.That(openResult.Workbook.ExtendedProperties.Company, Is.EqualTo("Example"));
        Assert.That(openResult.Workbook.HasCustomProperties, Is.True);
        Assert.That(openResult.Workbook.CustomProperties.GetProperty("Department")!.TextValue, Is.EqualTo("Finance"));
        Assert.That(openResult.Workbook.UnknownParts.ContainsPart("/xl/custom/item1.xml"), Is.True);
        Assert.That(secondSave.IsValid, Is.True);
        Assert.That(secondPackage.GetEntry("/xl/custom/item1.xml"), Is.Not.Null);
        Assert.That(secondPackage.GetEntry("/xl/custom/item1.xml")!.GetText(), Is.EqualTo("<item value=\"1\" />"));
    }

    [Test]
    public void SaveFileAndOpenFile_RoundTripThroughFilePath()
    {
        string filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "xlsx-workbook-api-test.xlsx");
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.WorkbookPart.Sheets[0].Name = "Saved";

        XlsxWorkbookSaveResult saveResult = workbook.SaveFile(filePath);
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenFile(filePath);

        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.WorkbookPart.Sheets[0].Name, Is.EqualTo("Saved"));
        System.IO.File.Delete(filePath);
    }

    [Test]
    public void OpenAndSave_ReportInvalidInputsWithoutThrowing()
    {
        XlsxWorkbookOpenResult? nullBytes = XlsxWorkbook.OpenBytes(null!);
        XlsxWorkbookOpenResult? nullStream = XlsxWorkbook.OpenStream(null!);
        XlsxWorkbook invalidWorkbook = new XlsxWorkbook();
        XlsxWorkbookSaveResult invalidSave = invalidWorkbook.SaveBytes();
        XlsxWorkbookSaveResult? nullSaveStream = invalidWorkbook.SaveStream(null!);

        Assert.That(nullBytes.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullBytes, "workbook.open.bytesEmpty"), Is.True);
        Assert.That(nullStream.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullStream, "workbook.open.streamNull"), Is.True);
        Assert.That(invalidSave.IsValid, Is.False);
        Assert.That(invalidSave.Bytes.Length, Is.EqualTo(0));
        Assert.That(HasDiagnostic(invalidSave, "workbook.sheetsMissing"), Is.True);
        Assert.That(nullSaveStream.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullSaveStream, "workbook.save.streamNull"), Is.True);
    }

    static XlsxCustomProperty CreateTextProperty(int pid, string name, string value)
    {
        XlsxCustomProperty property = new XlsxCustomProperty();
        property.PropertyId = pid;
        property.Name = name;
        property.ValueType = "lpwstr";
        property.TextValue = value;
        property.HasTextValue = true;
        return property;
    }

    static bool HasDiagnostic(XlsxWorkbookOpenResult result, string code)
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

    static bool HasDiagnostic(XlsxWorkbookSaveResult result, string code)
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
