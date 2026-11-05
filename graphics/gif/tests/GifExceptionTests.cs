using NUnit.Framework;

namespace gif.Tests;

[TestFixture]
public class GifExceptionTests
{
    [Test]
    public void Constructor_UsesExplicitNoErrorAndUnknownPosition()
    {
        GifException exception = new GifException(GifErrorCode.None, null, -1, -1, null);

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.None));
        Assert.That(exception.Message, Is.EqualTo(string.Empty));
        Assert.That(exception.ByteOffset, Is.EqualTo(-1));
        Assert.That(exception.FrameIndex, Is.EqualTo(-1));
        Assert.That(exception.BlockName, Is.EqualTo(string.Empty));
        Assert.That(exception.HasByteOffset(), Is.False);
        Assert.That(exception.HasFrameIndex(), Is.False);
        Assert.That(exception.HasBlockName(), Is.False);
    }

    [Test]
    public void Constructor_PreservesErrorCodeMessageAndPositionFields()
    {
        GifException exception = new GifException(GifErrorCode.InvalidImageRectangle, "outside screen", 128, 4, "ImageDescriptor");

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.InvalidImageRectangle));
        Assert.That(exception.Message, Is.EqualTo("outside screen"));
        Assert.That(exception.ByteOffset, Is.EqualTo(128));
        Assert.That(exception.FrameIndex, Is.EqualTo(4));
        Assert.That(exception.BlockName, Is.EqualTo("ImageDescriptor"));
        Assert.That(exception.HasByteOffset(), Is.True);
        Assert.That(exception.HasFrameIndex(), Is.True);
        Assert.That(exception.HasBlockName(), Is.True);
    }

    [Test]
    public void Constructor_NormalizesNullMessageAndBlockName()
    {
        GifException exception = new GifException(GifErrorCode.InvalidSignature, null, 0, -1, null);

        Assert.That(exception.Message, Is.EqualTo(string.Empty));
        Assert.That(exception.BlockName, Is.EqualTo(string.Empty));
        Assert.That(exception.HasBlockName(), Is.False);
    }

    [Test]
    public void FromDiagnostic_CopiesDiagnosticFields()
    {
        GifDiagnosticPosition position = new GifDiagnosticPosition(64, 2, "ImageData");
        GifErrorDiagnostic diagnostic = new GifErrorDiagnostic(GifErrorCode.InvalidLzwCodeStream, "bad code", position);

        GifException exception = GifException.FromDiagnostic(diagnostic);

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.InvalidLzwCodeStream));
        Assert.That(exception.Message, Is.EqualTo("bad code"));
        Assert.That(exception.ByteOffset, Is.EqualTo(64));
        Assert.That(exception.FrameIndex, Is.EqualTo(2));
        Assert.That(exception.BlockName, Is.EqualTo("ImageData"));
    }

    [Test]
    public void FromDiagnostic_NormalizesMissingDiagnostic()
    {
        GifException exception = GifException.FromDiagnostic(null);

        Assert.That(exception.ErrorCode, Is.EqualTo(GifErrorCode.None));
        Assert.That(exception.Message, Is.EqualTo(string.Empty));
        Assert.That(exception.ByteOffset, Is.EqualTo(-1));
        Assert.That(exception.FrameIndex, Is.EqualTo(-1));
        Assert.That(exception.BlockName, Is.EqualTo(string.Empty));
    }

    [TestCaseSource(nameof(ErrorCodes))]
    public void EveryErrorCode_RoundTripsThroughExceptionAndDiagnostic(GifErrorCode code)
    {
        GifDiagnosticPosition position = new GifDiagnosticPosition(123, 4, "FailureBlock");
        GifErrorDiagnostic source = new GifErrorDiagnostic(code, "failure", position);

        GifException exception = GifException.FromDiagnostic(source);
        GifDiagnosticPosition recreatedPosition = exception.CreatePosition();
        GifErrorDiagnostic recreatedDiagnostic = exception.ToDiagnostic();

        Assert.That(exception.ErrorCode, Is.EqualTo(code));
        Assert.That(exception.Message, Is.EqualTo("failure"));
        Assert.That(recreatedPosition, Is.Not.SameAs(position));
        Assert.That(recreatedPosition.ByteOffset, Is.EqualTo(123));
        Assert.That(recreatedPosition.FrameIndex, Is.EqualTo(4));
        Assert.That(recreatedPosition.BlockName, Is.EqualTo("FailureBlock"));
        Assert.That(recreatedDiagnostic.Code, Is.EqualTo(code));
        Assert.That(recreatedDiagnostic.Message, Is.EqualTo("failure"));
        Assert.That(recreatedDiagnostic.Position.ByteOffset, Is.EqualTo(123));
        Assert.That(recreatedDiagnostic.Position.FrameIndex, Is.EqualTo(4));
        Assert.That(recreatedDiagnostic.Position.BlockName, Is.EqualTo("FailureBlock"));
    }

    [Test]
    public void ToDiagnostic_CreatesEquivalentErrorDiagnostic()
    {
        GifException exception = new GifException(GifErrorCode.MissingColorTable, "missing palette", 32, 1, "ImageDescriptor");

        GifErrorDiagnostic diagnostic = exception.ToDiagnostic();

        Assert.That(diagnostic.Code, Is.EqualTo(GifErrorCode.MissingColorTable));
        Assert.That(diagnostic.Message, Is.EqualTo("missing palette"));
        Assert.That(diagnostic.Position.ByteOffset, Is.EqualTo(32));
        Assert.That(diagnostic.Position.FrameIndex, Is.EqualTo(1));
        Assert.That(diagnostic.Position.BlockName, Is.EqualTo("ImageDescriptor"));
    }

    [Test]
    public void FormatMessage_IncludesAvailablePositionFields()
    {
        GifException exception = new GifException(GifErrorCode.MalformedExtension, "bad block", 12, 3, "GraphicControlExtension");

        Assert.That(exception.FormatMessage(), Is.EqualTo("bad block in GraphicControlExtension at frame 3 at byte 12"));
    }

    [Test]
    public void FormatMessage_UsesErrorCodeWhenMessageIsEmpty()
    {
        GifException exception = new GifException(GifErrorCode.UnsupportedVersion, string.Empty, -1, -1, null);

        Assert.That(exception.FormatMessage(), Is.EqualTo("UnsupportedVersion"));
    }

    [TestCase("failure", -1, -1, "", "failure")]
    [TestCase("failure", -1, -1, "Header", "failure in Header")]
    [TestCase("failure", -1, 2, "", "failure at frame 2")]
    [TestCase("failure", 9, -1, "", "failure at byte 9")]
    [TestCase("failure", 9, 2, "", "failure at frame 2 at byte 9")]
    [TestCase("failure", 9, -1, "Header", "failure in Header at byte 9")]
    [TestCase("failure", -1, 2, "Header", "failure in Header at frame 2")]
    public void FormatMessage_IncludesExactlyTheAvailableContext(string message, long byteOffset, int frameIndex, string blockName, string expected)
    {
        GifException exception = new GifException(GifErrorCode.InvalidSignature, message, byteOffset, frameIndex, blockName);

        Assert.That(exception.FormatMessage(), Is.EqualTo(expected));
    }

    static GifErrorCode[] ErrorCodes()
    {
        return Enum.GetValues<GifErrorCode>();
    }
}
