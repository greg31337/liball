using System.Collections.Generic;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class MetadataByteRangeTests
{
    [Test]
    public void Create_WithValidOffsetAndLength_ComputesExclusiveEndOffset()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        MetadataByteRange range = MetadataByteRange.Create(12, 8, diagnostics, "APP1");

        Assert.That(range.IsValid, Is.True);
        Assert.That(range.Offset, Is.EqualTo(12));
        Assert.That(range.Length, Is.EqualTo(8));
        Assert.That(range.EndOffset, Is.EqualTo(20));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void Create_WithNegativeOffsetOrLength_AddsRecoverableDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        MetadataByteRange negativeOffset = MetadataByteRange.Create(-1, 8, diagnostics, "TIFF");
        MetadataByteRange negativeLength = MetadataByteRange.Create(1, -8, diagnostics, "TIFF");

        Assert.That(negativeOffset.IsValid, Is.False);
        Assert.That(negativeLength.IsValid, Is.False);
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.RecoverableError), Is.EqualTo(2));
        Assert.That(diagnostics[0].Code, Is.EqualTo("range.offset.negative"));
        Assert.That(diagnostics[1].Code, Is.EqualTo("range.length.negative"));
    }

    [Test]
    public void AddNonNegativeOffset_DetectsOverflowWithoutThrowing()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        long value = MetadataByteRange.AddNonNegativeOffset(long.MaxValue - 2, 3, diagnostics, "ifd.value", "TIFF");

        Assert.That(value, Is.EqualTo(-1));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
        Assert.That(diagnostics[0].Code, Is.EqualTo("ifd.value.overflow"));
    }

    [Test]
    public void AddOffset_DetectsSignedUnderflowWithoutThrowing()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        long value = MetadataByteRange.AddOffset(long.MinValue + 2, -3, diagnostics, "relative.offset", "CIFF");

        Assert.That(value, Is.EqualTo(-1));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.LimitExceeded));
        Assert.That(diagnostics[0].Code, Is.EqualTo("relative.offset.underflow"));
    }

    [Test]
    public void IsRangeWithin_ValidatesChildRangeAgainstContainer()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        bool inside = MetadataByteRange.IsRangeWithin(10, 20, 12, 8, diagnostics, "meta");
        bool before = MetadataByteRange.IsRangeWithin(10, 20, 9, 2, diagnostics, "meta");
        bool after = MetadataByteRange.IsRangeWithin(10, 20, 25, 8, diagnostics, "meta");

        Assert.That(inside, Is.True);
        Assert.That(before, Is.False);
        Assert.That(after, Is.False);
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.Truncated), Is.EqualTo(2));
        Assert.That(diagnostics[0].Code, Is.EqualTo("range.child.before.container"));
        Assert.That(diagnostics[1].Code, Is.EqualTo("range.child.after.container"));
    }

    [Test]
    public void IsOffsetWithin_TreatsEndOffsetAsExclusive()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        bool start = MetadataByteRange.IsOffsetWithin(4, 6, 4, diagnostics, "box");
        bool last = MetadataByteRange.IsOffsetWithin(4, 6, 9, diagnostics, "box");
        bool end = MetadataByteRange.IsOffsetWithin(4, 6, 10, diagnostics, "box");

        Assert.That(start, Is.True);
        Assert.That(last, Is.True);
        Assert.That(end, Is.False);
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("range.offset.outside"));
    }

    [Test]
    public void ClampLengthToRange_ReturnsOriginalOrClampedLength()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        long unchanged = MetadataByteRange.ClampLengthToRange(100, 50, 110, 10, diagnostics, "box");
        long clamped = MetadataByteRange.ClampLengthToRange(100, 50, 140, 20, diagnostics, "box");
        long before = MetadataByteRange.ClampLengthToRange(100, 50, 90, 20, diagnostics, "box");

        Assert.That(unchanged, Is.EqualTo(10));
        Assert.That(clamped, Is.EqualTo(10));
        Assert.That(before, Is.EqualTo(0));
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.Truncated), Is.EqualTo(2));
        Assert.That(diagnostics[0].Code, Is.EqualTo("range.clamp.after.container"));
        Assert.That(diagnostics[1].Code, Is.EqualTo("range.clamp.before.container"));
    }
}
