namespace jpeg.Tests;

internal class DiagnosticCollectionTests
{
    [Test]
    public void DiagnosticCollection_AddsDiagnosticsAndCountsSeverities()
    {
        global::jpeg.JpegDiagnosticCollection diagnostics = new global::jpeg.JpegDiagnosticCollection();

        global::jpeg.JpegDiagnostic info = diagnostics.AddInfo(
            global::jpeg.JpegDiagnosticCode.None,
            "identified",
            0,
            global::jpeg.JpegMarker.StartOfImage);
        global::jpeg.JpegDiagnostic warning = diagnostics.AddWarning(
            global::jpeg.JpegDiagnosticCode.CorruptMetadata,
            "metadata skipped",
            12,
            global::jpeg.JpegMarker.App2);
        global::jpeg.JpegDiagnostic error = diagnostics.AddError(
            global::jpeg.JpegDiagnosticCode.InvalidMarkerLength,
            "bad length",
            20,
            global::jpeg.JpegMarker.App1);

        Assert.That(diagnostics.Items.Count, Is.EqualTo(3));
        Assert.That(diagnostics.InfoCount, Is.EqualTo(1));
        Assert.That(diagnostics.WarningCount, Is.EqualTo(1));
        Assert.That(diagnostics.ErrorCount, Is.EqualTo(1));
        Assert.That(diagnostics.HasWarnings, Is.True);
        Assert.That(diagnostics.HasErrors, Is.True);
        Assert.That(info.Severity, Is.EqualTo(global::jpeg.JpegDiagnosticSeverity.Info));
        Assert.That(warning.Severity, Is.EqualTo(global::jpeg.JpegDiagnosticSeverity.Warning));
        Assert.That(error.Severity, Is.EqualTo(global::jpeg.JpegDiagnosticSeverity.Error));
        Assert.That(error.Offset, Is.EqualTo(20));
        Assert.That(error.Marker, Is.EqualTo(global::jpeg.JpegMarker.App1));
    }

    [Test]
    public void DiagnosticCollection_TreatsNullMessageAsEmpty()
    {
        global::jpeg.JpegDiagnosticCollection diagnostics = new global::jpeg.JpegDiagnosticCollection();

        global::jpeg.JpegDiagnostic diagnostic = diagnostics.AddWarning(
            global::jpeg.JpegDiagnosticCode.CorruptMetadata,
            null,
            4,
            global::jpeg.JpegMarker.App1);

        Assert.That(diagnostic.Message, Is.EqualTo(""));
    }

    [Test]
    public void DiagnosticCollection_CopiesToFlatDiagnosticList()
    {
        global::jpeg.JpegDiagnosticCollection diagnostics = new global::jpeg.JpegDiagnosticCollection();
        diagnostics.AddWarning(
            global::jpeg.JpegDiagnosticCode.UnsupportedColorSpace,
            "unknown color",
            7,
            global::jpeg.JpegMarker.StartOfFrame0);
        global::jpeg.JpegInfo info = new global::jpeg.JpegInfo();

        diagnostics.CopyTo(info.Diagnostics);

        Assert.That(info.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(info.Diagnostics[0].Code, Is.EqualTo(global::jpeg.JpegDiagnosticCode.UnsupportedColorSpace));
    }

    [Test]
    public void DiagnosticCollection_ClearResetsItemsCountsAndFlags()
    {
        global::jpeg.JpegDiagnosticCollection diagnostics = new global::jpeg.JpegDiagnosticCollection();
        diagnostics.AddWarning(global::jpeg.JpegDiagnosticCode.CorruptMetadata, "warning", 1, global::jpeg.JpegMarker.App1);
        diagnostics.AddError(global::jpeg.JpegDiagnosticCode.InvalidMarker, "error", 2, global::jpeg.JpegMarker.Unknown);

        diagnostics.Clear();

        Assert.That(diagnostics.Items, Is.Empty);
        Assert.That(diagnostics.InfoCount, Is.EqualTo(0));
        Assert.That(diagnostics.WarningCount, Is.EqualTo(0));
        Assert.That(diagnostics.ErrorCount, Is.EqualTo(0));
        Assert.That(diagnostics.HasWarnings, Is.False);
        Assert.That(diagnostics.HasErrors, Is.False);
    }

    [Test]
    public void DiagnosticCollection_CopyToRejectsNullTarget()
    {
        global::jpeg.JpegDiagnosticCollection diagnostics = new global::jpeg.JpegDiagnosticCollection();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                diagnostics.CopyTo(null!);
            });
    }
}
