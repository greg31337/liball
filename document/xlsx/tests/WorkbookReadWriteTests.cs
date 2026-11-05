using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorkbookReadWriteTests
{
    [Test]
    public void SaveOpen_RoundTripsWorkbookMetadataAndTheme()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.RenameSheet("Sheet1", "Inputs");
        workbook.AddSheet("Data");
        workbook.AddSheet("Archive");
        workbook.HideSheet("Archive", "veryHidden");
        workbook.ReorderSheet("Archive", 0);
        workbook.WorkbookPart.DefinedNames.Add(CreateDefinedName("ReportTitle", "'Inputs'!$A$1"));
        workbook.WorkbookPart.DefinedNames.Add(CreateLocalDefinedName("_xlnm.Print_Area", "'Data'!$A$1:$C$20", 2));
        workbook.CoreProperties.Title = "Workbook read write";
        workbook.CoreProperties.Creator = "Ada";
        workbook.CoreProperties.LastModifiedBy = "Grace";
        workbook.HasCoreProperties = true;
        workbook.ExtendedProperties.Application = "xlsx";
        workbook.ExtendedProperties.Company = "Example Co";
        workbook.ExtendedProperties.Manager = "Ops";
        workbook.HasExtendedProperties = true;
        workbook.CustomProperties.Properties.Add(CreateTextProperty(2, "Department", "Finance"));
        workbook.CustomProperties.Properties.Add(CreateBoolProperty(3, "Approved", true));
        workbook.HasCustomProperties = true;
        workbook.ConfigureWorkbookCalculationProperties("manual", false, true);
        workbook.Theme = XlsxThemePartWriter.CreateDefaultOfficeTheme();
        workbook.HasTheme = true;

        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saveResult.Bytes, new XlsxReaderOptions());

        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(package.GetEntry(XlsxPackagePaths.Theme), Is.Not.Null);
        Assert.That(openResult.Workbook!.WorkbookPart.Sheets.Count, Is.EqualTo(3));
        Assert.That(openResult.Workbook.GetSheetAt(0)!.Name, Is.EqualTo("Archive"));
        Assert.That(openResult.Workbook.GetSheetAt(1)!.Name, Is.EqualTo("Inputs"));
        Assert.That(openResult.Workbook.GetSheetAt(2)!.Name, Is.EqualTo("Data"));
        Assert.That(openResult.Workbook.GetSheet("Archive")!.State, Is.EqualTo("veryHidden"));
        Assert.That(openResult.Workbook.GetSheet("Inputs")!.RelationshipId, Is.EqualTo("rId1"));
        Assert.That(openResult.Workbook.WorkbookPart.GetDefinedName("ReportTitle")!.Text, Is.EqualTo("'Inputs'!$A$1"));
        Assert.That(openResult.Workbook.WorkbookPart.GetDefinedNameForSheet("_xlnm.Print_Area", 2)!.Text, Is.EqualTo("'Data'!$A$1:$C$20"));
        Assert.That(openResult.Workbook.CoreProperties.Title, Is.EqualTo("Workbook read write"));
        Assert.That(openResult.Workbook.CoreProperties.Creator, Is.EqualTo("Ada"));
        Assert.That(openResult.Workbook.CoreProperties.LastModifiedBy, Is.EqualTo("Grace"));
        Assert.That(openResult.Workbook.ExtendedProperties.Company, Is.EqualTo("Example Co"));
        Assert.That(openResult.Workbook.ExtendedProperties.Manager, Is.EqualTo("Ops"));
        Assert.That(openResult.Workbook.CustomProperties.GetProperty("Department")!.TextValue, Is.EqualTo("Finance"));
        Assert.That(openResult.Workbook.CustomProperties.GetProperty("Approved")!.BoolValue, Is.True);
        Assert.That(openResult.Workbook.WorkbookPart.HasCalculationProperties, Is.True);
        Assert.That(openResult.Workbook.WorkbookPart.CalculationProperties.CalcMode, Is.EqualTo("manual"));
        Assert.That(openResult.Workbook.WorkbookPart.CalculationProperties.FullCalcOnLoad, Is.False);
        Assert.That(openResult.Workbook.WorkbookPart.CalculationProperties.HasFullCalcOnLoad, Is.True);
        Assert.That(openResult.Workbook.WorkbookPart.CalculationProperties.ForceFullCalc, Is.True);
        Assert.That(openResult.Workbook.WorkbookPart.CalculationProperties.HasForceFullCalc, Is.True);
        Assert.That(openResult.Workbook.HasTheme, Is.True);
        Assert.That(openResult.Workbook.Theme.Name, Is.EqualTo("Office Theme"));
        Assert.That(openResult.Workbook.Theme.ColorScheme.Colors.Count, Is.EqualTo(12));
        Assert.That(openResult.Workbook.Theme.FontScheme.MinorFont.LatinTypeface, Is.EqualTo("Aptos"));
    }

    [Test]
    public void SaveOpen_RoundTripsExplicitTemplatePackageVariants()
    {
        AssertPackageVariantRoundTrip(XlsxPackageVariant.Xltx, XlsxContentTypes.TemplateWorkbook, false);
        AssertPackageVariantRoundTrip(XlsxPackageVariant.Xltm, XlsxContentTypes.MacroEnabledTemplateWorkbook, true);
    }

    static void AssertPackageVariantRoundTrip(XlsxPackageVariant packageVariant, string contentType, bool allowMacroEnabledPackages)
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWriterOptions writerOptions = new XlsxWriterOptions();
        writerOptions.PackageVariant = packageVariant;
        XlsxReaderOptions readerOptions = new XlsxReaderOptions();
        readerOptions.AllowMacroEnabledPackages = allowMacroEnabledPackages;

        XlsxWorkbookSaveResult saveResult = workbook.SaveBytesWithOptions(writerOptions);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saveResult.Bytes, new XlsxReaderOptions());
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes, readerOptions);

        Assert.That(saveResult.IsValid, Is.True);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, contentType);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.PackageVariant, Is.EqualTo(packageVariant));
        Assert.That(openResult.Workbook.WorkbookPart.Sheets.Count, Is.EqualTo(1));
    }

    static XlsxWorkbookDefinedName CreateDefinedName(string name, string text)
    {
        XlsxWorkbookDefinedName definedName = new XlsxWorkbookDefinedName();
        definedName.Name = name;
        definedName.Text = text;
        return definedName;
    }

    static XlsxWorkbookDefinedName CreateLocalDefinedName(string name, string text, int localSheetId)
    {
        XlsxWorkbookDefinedName definedName = CreateDefinedName(name, text);
        definedName.LocalSheetId = localSheetId;
        definedName.HasLocalSheetId = true;
        return definedName;
    }

    static XlsxCustomProperty CreateTextProperty(int pid, string name, string value)
    {
        XlsxCustomProperty property = new XlsxCustomProperty();
        property.FormatId = XlsxCustomPropertiesWriter.DefaultFormatId;
        property.PropertyId = pid;
        property.Name = name;
        property.ValueType = "lpwstr";
        property.TextValue = value;
        property.HasTextValue = true;
        return property;
    }

    static XlsxCustomProperty CreateBoolProperty(int pid, string name, bool value)
    {
        XlsxCustomProperty property = new XlsxCustomProperty();
        property.FormatId = XlsxCustomPropertiesWriter.DefaultFormatId;
        property.PropertyId = pid;
        property.Name = name;
        property.ValueType = "bool";
        property.BoolValue = value;
        property.HasBoolValue = true;
        return property;
    }

    static void AssertEntryText(XlsxZipPackageReadResult package, string path, string expectedText)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);

        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Contain(expectedText));
    }
}
