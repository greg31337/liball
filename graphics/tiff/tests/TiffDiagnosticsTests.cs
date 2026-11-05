using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffDiagnosticsTests
{
    [Test]
    public void Diagnostic_ConstructorsPopulateSeverityCodeMessageAndDefaults()
    {
        TiffDiagnostic diagnostic = new TiffDiagnostic(TiffDiagnosticSeverity.Error, TiffErrorCode.InvalidOffset, "bad offset");

        Assert.That(diagnostic.Severity, Is.EqualTo(TiffDiagnosticSeverity.Error));
        Assert.That(diagnostic.Code, Is.EqualTo(TiffErrorCode.InvalidOffset));
        Assert.That(diagnostic.Message, Is.EqualTo("bad offset"));
        Assert.That(diagnostic.Offset, Is.EqualTo(TiffDiagnostic.NoOffset));
        Assert.That(diagnostic.ImageIndex, Is.EqualTo(TiffDiagnostic.NoImageIndex));
        Assert.That(diagnostic.TagId, Is.EqualTo(TiffDiagnostic.NoTagId));
        Assert.That(diagnostic.IsError(), Is.True);
        Assert.That(diagnostic.IsWarning(), Is.False);
        Assert.That(diagnostic.HasOffset(), Is.False);
        Assert.That(diagnostic.HasImageIndex(), Is.False);
        Assert.That(diagnostic.HasTagId(), Is.False);
    }

    [Test]
    public void Diagnostic_ContextConstructorPopulatesLocationFields()
    {
        TiffDiagnostic diagnostic = TiffDiagnostic.Create(TiffDiagnosticSeverity.Warning, TiffErrorCode.DuplicateTag, "duplicate", 128, 2, 256);

        Assert.That(diagnostic.IsWarning(), Is.True);
        Assert.That(diagnostic.Offset, Is.EqualTo(128));
        Assert.That(diagnostic.ImageIndex, Is.EqualTo(2));
        Assert.That(diagnostic.TagId, Is.EqualTo(256));
        Assert.That(diagnostic.HasOffset(), Is.True);
        Assert.That(diagnostic.HasImageIndex(), Is.True);
        Assert.That(diagnostic.HasTagId(), Is.True);
    }

    [Test]
    public void Diagnostic_NullMessageBecomesEmptyString()
    {
        TiffDiagnostic diagnostic = new TiffDiagnostic(TiffDiagnosticSeverity.Error, TiffErrorCode.UnknownError, null!);

        Assert.That(diagnostic.Message, Is.EqualTo(""));
        Assert.That(diagnostic.Format(), Is.EqualTo("Error UnknownError"));
    }

    [Test]
    public void Diagnostic_FormatIncludesAvailableContext()
    {
        TiffDiagnostic diagnostic = TiffDiagnostic.Create(TiffDiagnosticSeverity.Warning, TiffErrorCode.InvalidTagCount, "count mismatch", 64, 1, 258);

        Assert.That(diagnostic.Format(), Is.EqualTo("Warning InvalidTagCount at offset 64 image 1 tag 258: count mismatch"));
    }

    [Test]
    public void Diagnostics_AddErrorAndWarningTrackCountsAndCodes()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        TiffDiagnostic error = diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "bad byte order");
        TiffDiagnostic warning = diagnostics.AddWarning(TiffErrorCode.UnsupportedCompression, "raw only");

        Assert.That(diagnostics.Count(), Is.EqualTo(2));
        Assert.That(diagnostics.ErrorCount(), Is.EqualTo(1));
        Assert.That(diagnostics.WarningCount(), Is.EqualTo(1));
        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.HasWarnings(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.InvalidByteOrder), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.TruncatedStream), Is.False);
        Assert.That(diagnostics.Get(0), Is.SameAs(error));
        Assert.That(diagnostics.Get(1), Is.SameAs(warning));
    }

    [Test]
    public void Diagnostics_AddContextHelpersPopulateLocationFields()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        TiffDiagnostic offsetError = diagnostics.AddErrorAt(TiffErrorCode.TruncatedStream, "short read", 99);
        TiffDiagnostic tagWarning = diagnostics.AddWarningForTag(TiffErrorCode.DuplicateTag, "duplicate", 3, 270);

        Assert.That(offsetError.Offset, Is.EqualTo(99));
        Assert.That(offsetError.ImageIndex, Is.EqualTo(TiffDiagnostic.NoImageIndex));
        Assert.That(offsetError.TagId, Is.EqualTo(TiffDiagnostic.NoTagId));
        Assert.That(tagWarning.Offset, Is.EqualTo(TiffDiagnostic.NoOffset));
        Assert.That(tagWarning.ImageIndex, Is.EqualTo(3));
        Assert.That(tagWarning.TagId, Is.EqualTo(270));
    }

    [Test]
    public void Diagnostics_AddIgnoresNullDiagnostics()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();

        diagnostics.Add(null!);

        Assert.That(diagnostics.Count(), Is.EqualTo(0));
    }

    [Test]
    public void Diagnostics_MergeAppendsExistingItemsAndIgnoresNullCollection()
    {
        TiffDiagnostics first = new TiffDiagnostics();
        TiffDiagnostics second = new TiffDiagnostics();
        first.AddError(TiffErrorCode.InvalidOffset, "bad offset");
        second.AddWarning(TiffErrorCode.UnsupportedPhotometric, "unsupported");

        first.Merge(null!);
        first.Merge(second);

        Assert.That(first.Count(), Is.EqualTo(2));
        Assert.That(first.ErrorCount(), Is.EqualTo(1));
        Assert.That(first.WarningCount(), Is.EqualTo(1));
        Assert.That(first.Get(1).Code, Is.EqualTo(TiffErrorCode.UnsupportedPhotometric));
    }

    [Test]
    public void Diagnostics_ToArrayReturnsSnapshot()
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        diagnostics.AddError(TiffErrorCode.InvalidTiffVersion, "bad version");

        TiffDiagnostic[] snapshot = diagnostics.ToArray();
        diagnostics.Clear();

        Assert.That(snapshot.Length, Is.EqualTo(1));
        Assert.That(snapshot[0].Code, Is.EqualTo(TiffErrorCode.InvalidTiffVersion));
        Assert.That(diagnostics.Count(), Is.EqualTo(0));
    }

    [Test]
    public void DiagnosticSeverityValues_AreStable()
    {
        Assert.That((int)TiffDiagnosticSeverity.Warning, Is.EqualTo(1));
        Assert.That((int)TiffDiagnosticSeverity.Error, Is.EqualTo(2));
    }
}
