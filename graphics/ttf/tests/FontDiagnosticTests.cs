using ttf;

namespace ttf.Tests;

public class FontDiagnosticTests
{
    [Test]
    public void ConstructorPreservesStructuredFinding()
    {
        FontDiagnostic diagnostic = new FontDiagnostic(
            FontDiagnosticSeverity.Error,
            FontDiagnosticCode.InvalidCharacterMapping,
            "The mapping group overlaps its predecessor.",
            3,
            FontTags.Cmap,
            4294967300L,
            "cmap.encodingRecords[2].groups[7]");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.GetSeverity(), Is.EqualTo(FontDiagnosticSeverity.Error));
            Assert.That(diagnostic.GetCode(), Is.EqualTo(FontDiagnosticCode.InvalidCharacterMapping));
            Assert.That(diagnostic.GetMessage(), Is.EqualTo("The mapping group overlaps its predecessor."));
            Assert.That(diagnostic.GetFaceIndex(), Is.EqualTo(3));
            Assert.That(diagnostic.GetTableTag(), Is.EqualTo(FontTags.Cmap));
            Assert.That(diagnostic.GetAbsoluteOffset(), Is.EqualTo(4294967300L));
            Assert.That(diagnostic.GetLogicalPath(), Is.EqualTo("cmap.encodingRecords[2].groups[7]"));
        });
    }

    [Test]
    public void ConstructorAcceptsPackageLevelLocationSentinels()
    {
        FontDiagnostic diagnostic = new FontDiagnostic(
            FontDiagnosticSeverity.Fatal,
            FontDiagnosticCode.UnknownContainer,
            "Unknown container.",
            -1,
            0,
            -1,
            "");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.GetFaceIndex(), Is.EqualTo(-1));
            Assert.That(diagnostic.GetTableTag(), Is.EqualTo(0));
            Assert.That(diagnostic.GetAbsoluteOffset(), Is.EqualTo(-1));
            Assert.That(diagnostic.GetLogicalPath(), Is.Empty);
        });
    }

    [TestCase(-1)]
    [TestCase(4)]
    public void ConstructorRejectsUndefinedSeverity(int severity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(
            (FontDiagnosticSeverity)severity,
            FontDiagnosticCode.MalformedTable,
            "message",
            0,
            "path"));
    }

    [TestCase(0)]
    [TestCase(999)]
    [TestCase(6000)]
    public void ConstructorRejectsMissingOrUndefinedCode(int code)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(
            FontDiagnosticSeverity.Warning,
            (FontDiagnosticCode)code,
            "message",
            0,
            "path"));
    }

    [Test]
    public void ConstructorRejectsNullMessage()
    {
        Assert.Throws<ArgumentNullException>(() => Create(
            FontDiagnosticSeverity.Warning,
            FontDiagnosticCode.MalformedTable,
            null,
            0,
            "path"));
    }

    [Test]
    public void ConstructorRejectsFaceIndexBelowSentinel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(
            FontDiagnosticSeverity.Warning,
            FontDiagnosticCode.MalformedTable,
            "message",
            -2,
            "path"));
    }

    [Test]
    public void ConstructorRejectsAbsoluteOffsetBelowSentinel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FontDiagnostic(
            FontDiagnosticSeverity.Warning,
            FontDiagnosticCode.MalformedTable,
            "message",
            0,
            FontTags.Head,
            -2,
            "path"));
    }

    [Test]
    public void ConstructorRejectsNullLogicalPath()
    {
        Assert.Throws<ArgumentNullException>(() => Create(
            FontDiagnosticSeverity.Warning,
            FontDiagnosticCode.MalformedTable,
            "message",
            0,
            null));
    }

    private static FontDiagnostic Create(
        FontDiagnosticSeverity severity,
        FontDiagnosticCode code,
        string? message,
        int faceIndex,
        string? logicalPath)
    {
        return new FontDiagnostic(
            severity,
            code,
            message!,
            faceIndex,
            FontTags.Head,
            0,
            logicalPath!);
    }
}
