using System.IO.Compression;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class CoreApiTests
{
    [Test]
    public void Diagnostics_InitializeAndTrackLocation()
    {
        XlsxDiagnostic diagnostic = new XlsxDiagnostic(XlsxDiagnosticSeverity.Info, "", "");

        diagnostic.SetLocation("/xl/workbook.xml", "workbook", "date1904");
        diagnostic.SetCellLocation(12, 4);
        diagnostic.SetRelationshipId("rId7");

        Assert.That(diagnostic.Severity, Is.EqualTo(XlsxDiagnosticSeverity.Info));
        Assert.That(diagnostic.Code, Is.EqualTo(""));
        Assert.That(diagnostic.Message, Is.EqualTo(""));
        Assert.That(diagnostic.PartPath, Is.EqualTo("/xl/workbook.xml"));
        Assert.That(diagnostic.ElementName, Is.EqualTo("workbook"));
        Assert.That(diagnostic.AttributeName, Is.EqualTo("date1904"));
        Assert.That(diagnostic.Row, Is.EqualTo(12));
        Assert.That(diagnostic.Column, Is.EqualTo(4));
        Assert.That(diagnostic.RelationshipId, Is.EqualTo("rId7"));
    }

    [Test]
    public void ValidationResult_CountsSeveritiesAndInvalidatesOnError()
    {
        XlsxValidationResult result = new XlsxValidationResult();

        result.AddInfo("info", "informational");
        result.AddWarning("warning", "warning");
        XlsxDiagnostic error = result.AddError("error", "error");
        error.SetLocation("/xl/worksheets/sheet1.xml", "sheetData", "");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.CountSeverity(XlsxDiagnosticSeverity.Info), Is.EqualTo(1));
        Assert.That(result.CountSeverity(XlsxDiagnosticSeverity.Warning), Is.EqualTo(1));
        Assert.That(result.CountSeverity(XlsxDiagnosticSeverity.Error), Is.EqualTo(1));
        Assert.That(result.Diagnostics[2].PartPath, Is.EqualTo("/xl/worksheets/sheet1.xml"));
    }

    [Test]
    public void ReaderOptions_ExposeConservativeDefaults()
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        XlsxValidationResult result = options.Validate();

        Assert.That(result.IsValid, Is.True);
        Assert.That(options.PreserveUnknownParts, Is.True);
        Assert.That(options.PreserveUnknownXml, Is.True);
        Assert.That(options.AllowMacroEnabledPackages, Is.False);
        Assert.That(options.MetadataOnly, Is.False);
        Assert.That(options.ValuesOnly, Is.False);
        Assert.That(options.MaxRows, Is.EqualTo(1048576));
        Assert.That(options.MaxColumns, Is.EqualTo(16384));
        Assert.That(options.MaxXmlDepth, Is.GreaterThan(0));
        Assert.That(options.MaxUncompressedBytes, Is.GreaterThanOrEqualTo(options.MaxCompressedBytes));
    }

    [Test]
    public void ReaderOptions_ReportInvalidLimitsAndConflictingModes()
    {
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.MaxZipEntries = 0;
        options.MaxCompressedBytes = 10;
        options.MaxUncompressedBytes = 9;
        options.MaxXmlDepth = -1;
        options.MetadataOnly = true;
        options.ValuesOnly = true;

        XlsxValidationResult result = options.Validate();

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.CountSeverity(XlsxDiagnosticSeverity.Error), Is.EqualTo(4));
    }

    [Test]
    public void WriterOptions_ValidatePackageVariantAndCompressionLevel()
    {
        XlsxWriterOptions options = new XlsxWriterOptions();
        XlsxValidationResult defaultResult = options.Validate();

        Assert.That(defaultResult.IsValid, Is.True);
        Assert.That(options.PreserveUnknownParts, Is.True);
        Assert.That(options.DeterministicOutput, Is.True);
        Assert.That(options.UseSharedStrings, Is.True);
        Assert.That(options.PackageVariant, Is.EqualTo(XlsxPackageVariant.Xlsx));
        Assert.That(options.CompressionLevel, Is.EqualTo(CompressionLevel.Optimal));

        options.MaxZipEntries = -1;
        options.MaxUncompressedBytes = 0;
        options.PackageVariant = (XlsxPackageVariant)100;
        options.CompressionLevel = (CompressionLevel)100;

        XlsxValidationResult invalidResult = options.Validate();

        Assert.That(invalidResult.IsValid, Is.False);
        Assert.That(invalidResult.CountSeverity(XlsxDiagnosticSeverity.Error), Is.EqualTo(4));
    }

    [Test]
    public void XlsxException_SupportsStableBoundaryMessages()
    {
        System.Exception inner = new System.Exception("inner");
        XlsxException exception = new XlsxException("outer", inner);

        Assert.That(exception.Message, Is.EqualTo("outer"));
        Assert.That(exception.InnerException, Is.SameAs(inner));
    }
}
