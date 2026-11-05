using System.Collections.Generic;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class DiagnosticHelperTests
{
    [Test]
    public void AddWarningAndRecoverableError_AppendsDiagnosticsInOrder()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        MetadataDiagnostic warning = MetadataDiagnostic.AddWarning(diagnostics, "jpeg.segment.length", "segment length is suspicious");
        MetadataDiagnostic error = MetadataDiagnostic.AddRecoverableError(diagnostics, "png.chunk.truncated", "chunk was truncated");

        Assert.That(diagnostics, Has.Count.EqualTo(2));
        Assert.That(diagnostics[0], Is.SameAs(warning));
        Assert.That(diagnostics[1], Is.SameAs(error));
        Assert.That(warning.Severity, Is.EqualTo(MetadataSeverity.Warning));
        Assert.That(warning.Code, Is.EqualTo("jpeg.segment.length"));
        Assert.That(error.Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
        Assert.That(error.Message, Is.EqualTo("chunk was truncated"));
    }

    [Test]
    public void AddAtHelpers_PreserveBlockLocationAndLength()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        MetadataDiagnostic warning = MetadataDiagnostic.AddWarningAt(diagnostics, "app1.short", "APP1 payload is short", "APP1", 24, 12);
        MetadataDiagnostic truncated = MetadataDiagnostic.AddTruncated(diagnostics, "png.idat.short", "IDAT data ended early", "IDAT", 128, 64);
        MetadataDiagnostic limited = MetadataDiagnostic.AddLimitExceeded(diagnostics, "xml.limit", "XMP limit reached", "XMP", 256, 4096);

        Assert.That(warning.BlockType, Is.EqualTo("APP1"));
        Assert.That(warning.Offset, Is.EqualTo(24));
        Assert.That(warning.Length, Is.EqualTo(12));
        Assert.That(truncated.Severity, Is.EqualTo(MetadataSeverity.Truncated));
        Assert.That(truncated.BlockType, Is.EqualTo("IDAT"));
        Assert.That(limited.Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
        Assert.That(limited.Length, Is.EqualTo(4096));
        Assert.That(diagnostics, Has.Count.EqualTo(3));
    }

    [Test]
    public void UnsupportedAndCreate_HaveExpectedSeverityAndDefaults()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        MetadataDiagnostic unsupported = MetadataDiagnostic.AddUnsupported(diagnostics, "chunk.unknown", "unknown ancillary chunk", "vpAg");
        MetadataDiagnostic created = MetadataDiagnostic.Create(MetadataSeverity.Warning, "manual", "manual diagnostic", "SOF0", 8, 17);

        Assert.That(unsupported.Severity, Is.EqualTo(MetadataSeverity.Unsupported));
        Assert.That(unsupported.BlockType, Is.EqualTo("vpAg"));
        Assert.That(unsupported.Offset, Is.EqualTo(-1));
        Assert.That(unsupported.Length, Is.EqualTo(0));
        Assert.That(created.Severity, Is.EqualTo(MetadataSeverity.Warning));
        Assert.That(created.BlockType, Is.EqualTo("SOF0"));
        Assert.That(created.Offset, Is.EqualTo(8));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
    }

    [Test]
    public void SeverityQueries_IgnoreNullCollectionsAndNullEntries()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        diagnostics.Add(null!);
        MetadataDiagnostic.AddWarning(diagnostics, "warning", "warning");
        MetadataDiagnostic.AddRecoverableError(diagnostics, "error", "error");
        MetadataDiagnostic.AddTruncated(diagnostics, "truncated", "truncated", "APP1", 4, 2);

        Assert.That(MetadataDiagnostic.HasSeverity(null, MetadataSeverity.Warning), Is.False);
        Assert.That(MetadataDiagnostic.HasSeverity(diagnostics, MetadataSeverity.Warning), Is.True);
        Assert.That(MetadataDiagnostic.HasSeverity(diagnostics, MetadataSeverity.Unsupported), Is.False);
        Assert.That(MetadataDiagnostic.HasWarningsOrErrors(diagnostics), Is.True);
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.Warning), Is.EqualTo(1));
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.RecoverableError), Is.EqualTo(1));
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.Truncated), Is.EqualTo(1));
    }

    [Test]
    public void AddHelpers_ReturnDiagnosticWhenCollectionIsNull()
    {
        MetadataDiagnostic warning = MetadataDiagnostic.AddWarning(null, "warning", "warning");
        MetadataDiagnostic error = MetadataDiagnostic.AddRecoverableErrorAt(null, "error", "error", "TIFF", 12, 6);

        Assert.That(warning.Severity, Is.EqualTo(MetadataSeverity.Warning));
        Assert.That(error.Severity, Is.EqualTo(MetadataSeverity.RecoverableError));
        Assert.That(error.BlockType, Is.EqualTo("TIFF"));
        Assert.That(error.Offset, Is.EqualTo(12));
        Assert.That(error.Length, Is.EqualTo(6));
    }
}
