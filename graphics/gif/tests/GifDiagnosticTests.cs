using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifDiagnosticTests
{
    [Test]
    public void DiagnosticPosition_ExplicitUnknownLocationIsStable()
    {
        GifDiagnosticPosition position = new GifDiagnosticPosition(-1, -1, null);

        Assert.That(position.ByteOffset, Is.EqualTo(-1));
        Assert.That(position.FrameIndex, Is.EqualTo(-1));
        Assert.That(position.BlockName, Is.EqualTo(string.Empty));
        Assert.That(position.HasByteOffset(), Is.False);
        Assert.That(position.HasFrameIndex(), Is.False);
        Assert.That(position.HasBlockName(), Is.False);
    }

    [Test]
    public void DiagnosticPosition_PreservesKnownLocation()
    {
        GifDiagnosticPosition position = new GifDiagnosticPosition(42, 3, "ImageDescriptor");

        Assert.That(position.ByteOffset, Is.EqualTo(42));
        Assert.That(position.FrameIndex, Is.EqualTo(3));
        Assert.That(position.BlockName, Is.EqualTo("ImageDescriptor"));
        Assert.That(position.HasByteOffset(), Is.True);
        Assert.That(position.HasFrameIndex(), Is.True);
        Assert.That(position.HasBlockName(), Is.True);
    }

    [Test]
    public void DiagnosticPosition_NormalizesNullBlockName()
    {
        GifDiagnosticPosition position = new GifDiagnosticPosition(7, 0, null);

        Assert.That(position.BlockName, Is.EqualTo(string.Empty));
        Assert.That(position.HasBlockName(), Is.False);
    }

    [Test]
    public void ErrorDiagnostic_ExplicitNoErrorAtUnknownLocationIsStable()
    {
        GifErrorDiagnostic diagnostic = new GifErrorDiagnostic(GifErrorCode.None, string.Empty, null);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.None));
        Assert.That(diagnostic.Message, Is.EqualTo(string.Empty));
        Assert.That(diagnostic.Position, Is.Not.Null);
        Assert.That(diagnostic.Position.HasByteOffset(), Is.False);
    }

    [Test]
    public void ErrorDiagnostic_PreservesCodeMessageAndPosition()
    {
        GifDiagnosticPosition position = new GifDiagnosticPosition(12, 1, "GraphicControlExtension");
        GifErrorDiagnostic diagnostic = new GifErrorDiagnostic(GifErrorCode.MalformedExtension, "bad extension", position);

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.MalformedExtension));
        Assert.That(diagnostic.Message, Is.EqualTo("bad extension"));
        Assert.That(diagnostic.Position, Is.SameAs(position));
    }

    [Test]
    public void WarningDiagnostic_PreservesCodeMessageAndPosition()
    {
        GifDiagnosticPosition position = new GifDiagnosticPosition(20, 2, "ImageData");
        GifWarningDiagnostic diagnostic = new GifWarningDiagnostic(GifWarningCode.ToleratedMalformedBlock, "continued", position);

        Assert.That(diagnostic.Code, Is.EqualTo(GifWarningCode.ToleratedMalformedBlock));
        Assert.That(diagnostic.Message, Is.EqualTo("continued"));
        Assert.That(diagnostic.Position, Is.SameAs(position));
    }

    [Test]
    public void Diagnostics_NormalizeNullMessageAndPosition()
    {
        GifErrorDiagnostic error = new GifErrorDiagnostic(GifErrorCode.InvalidSignature, null, null);
        GifWarningDiagnostic warning = new GifWarningDiagnostic(GifWarningCode.MissingTrailer, null, null);

        Assert.That(error.Message, Is.EqualTo(string.Empty));
        Assert.That(error.Position, Is.Not.Null);
        Assert.That(error.Position.HasByteOffset(), Is.False);
        Assert.That(warning.Message, Is.EqualTo(string.Empty));
        Assert.That(warning.Position, Is.Not.Null);
        Assert.That(warning.Position.HasByteOffset(), Is.False);
    }

    [TestCaseSource(nameof(WarningCodes))]
    public void EveryWarningCode_PreservesMessageAndCompletePosition(GifWarningCode code)
    {
        GifDiagnosticPosition position = new GifDiagnosticPosition(45, 6, "WarningBlock");

        GifWarningDiagnostic warning = new GifWarningDiagnostic(code, "warning", position);

        Assert.That(warning.Code, Is.EqualTo(code));
        Assert.That(warning.Message, Is.EqualTo("warning"));
        Assert.That(warning.Position, Is.SameAs(position));
        Assert.That(warning.Position.ByteOffset, Is.EqualTo(45));
        Assert.That(warning.Position.FrameIndex, Is.EqualTo(6));
        Assert.That(warning.Position.BlockName, Is.EqualTo("WarningBlock"));
    }

    static GifWarningCode[] WarningCodes()
    {
        return Enum.GetValues<GifWarningCode>();
    }
}
