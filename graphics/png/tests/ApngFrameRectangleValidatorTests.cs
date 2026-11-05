using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ApngFrameRectangleValidatorTests
{
    [Test]
    public void Validate_AcceptsFrameInsideCanvas()
    {
        PngValidationResult result = PngApngFrameRectangleValidator.Validate(CreateFrame(100, 120, 10, 20), CreateIhdr(640, 480));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(PngApngFrameRectangleValidator.IsValid(CreateFrame(100, 120, 10, 20), CreateIhdr(640, 480)), Is.True);
    }

    [Test]
    public void Validate_ReportsZeroFrameDimensions()
    {
        PngValidationResult result = PngApngFrameRectangleValidator.Validate(CreateFrame(0, 0, 0, 0), CreateIhdr(640, 480));

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasError(result, "PNG_APNG_FRAME_WIDTH_INVALID"), Is.True);
        Assert.That(HasError(result, "PNG_APNG_FRAME_HEIGHT_INVALID"), Is.True);
    }

    [Test]
    public void Validate_ReportsFrameRectanglesOutsideCanvas()
    {
        PngValidationResult tooWide = PngApngFrameRectangleValidator.Validate(CreateFrame(100, 10, 600, 0), CreateIhdr(640, 480));
        PngValidationResult tooHigh = PngApngFrameRectangleValidator.Validate(CreateFrame(10, 100, 0, 440), CreateIhdr(640, 480));
        PngValidationResult xPastCanvas = PngApngFrameRectangleValidator.Validate(CreateFrame(1, 1, 641, 0), CreateIhdr(640, 480));
        PngValidationResult yPastCanvas = PngApngFrameRectangleValidator.Validate(CreateFrame(1, 1, 0, 481), CreateIhdr(640, 480));

        Assert.That(tooWide.IsValid, Is.False);
        Assert.That(HasError(tooWide, "PNG_APNG_FRAME_RECTANGLE_OUT_OF_BOUNDS"), Is.True);
        Assert.That(tooHigh.IsValid, Is.False);
        Assert.That(HasError(tooHigh, "PNG_APNG_FRAME_RECTANGLE_OUT_OF_BOUNDS"), Is.True);
        Assert.That(xPastCanvas.IsValid, Is.False);
        Assert.That(HasError(xPastCanvas, "PNG_APNG_FRAME_RECTANGLE_OUT_OF_BOUNDS"), Is.True);
        Assert.That(yPastCanvas.IsValid, Is.False);
        Assert.That(HasError(yPastCanvas, "PNG_APNG_FRAME_RECTANGLE_OUT_OF_BOUNDS"), Is.True);
    }

    [Test]
    public void ValidateDefaultImageFrame_AcceptsFullCanvasAtOrigin()
    {
        PngValidationResult result = PngApngFrameRectangleValidator.ValidateDefaultImageFrame(CreateFrame(640, 480, 0, 0), CreateIhdr(640, 480));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(PngApngFrameRectangleValidator.IsValidDefaultImageFrame(CreateFrame(640, 480, 0, 0), CreateIhdr(640, 480)), Is.True);
    }

    [Test]
    public void ValidateDefaultImageFrame_ReportsNonFullCanvasOrOffsetFrame()
    {
        PngValidationResult smaller = PngApngFrameRectangleValidator.ValidateDefaultImageFrame(CreateFrame(320, 480, 0, 0), CreateIhdr(640, 480));
        PngValidationResult offset = PngApngFrameRectangleValidator.ValidateDefaultImageFrame(CreateFrame(640, 480, 1, 0), CreateIhdr(640, 480));

        Assert.That(smaller.IsValid, Is.False);
        Assert.That(HasError(smaller, "PNG_APNG_DEFAULT_IMAGE_RECTANGLE_INVALID"), Is.True);
        Assert.That(offset.IsValid, Is.False);
        Assert.That(HasError(offset, "PNG_APNG_DEFAULT_IMAGE_RECTANGLE_INVALID"), Is.True);
    }

    [Test]
    public void Validate_ReportsInvalidCanvasDimensions()
    {
        PngValidationResult zeroWidth = PngApngFrameRectangleValidator.Validate(CreateFrame(1, 1, 0, 0), CreateIhdr(0, 1));
        PngValidationResult zeroHeight = PngApngFrameRectangleValidator.ValidateDefaultImageFrame(CreateFrame(1, 1, 0, 0), CreateIhdr(1, 0));

        Assert.That(zeroWidth.IsValid, Is.False);
        Assert.That(HasError(zeroWidth, "PNG_APNG_CANVAS_DIMENSION_INVALID"), Is.True);
        Assert.That(zeroHeight.IsValid, Is.False);
        Assert.That(HasError(zeroHeight, "PNG_APNG_CANVAS_DIMENSION_INVALID"), Is.True);
    }

    [Test]
    public void Validate_RejectsInvalidInputs()
    {
        PngFrameControl frame = CreateFrame(1, 1, 0, 0);
        PngIhdr ihdr = CreateIhdr(1, 1);

        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleValidator.Validate(null!, ihdr));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleValidator.Validate(frame, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleValidator.ValidateDefaultImageFrame(null!, ihdr));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleValidator.ValidateDefaultImageFrame(frame, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleValidator.IsValid(null!, ihdr));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleValidator.IsValid(frame, null!));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleValidator.IsValidDefaultImageFrame(null!, ihdr));
        Assert.Throws<ArgumentNullException>(() => PngApngFrameRectangleValidator.IsValidDefaultImageFrame(frame, null!));
    }

    static PngFrameControl CreateFrame(uint width, uint height, uint xOffset, uint yOffset)
    {
        PngFrameControl frame = new PngFrameControl();
        frame.SequenceNumber = 0;
        frame.Width = width;
        frame.Height = height;
        frame.XOffset = xOffset;
        frame.YOffset = yOffset;
        frame.DelayNumerator = 1;
        frame.DelayDenominator = 24;
        frame.DisposeOperation = PngApngDisposeOperation.None;
        frame.BlendOperation = PngApngBlendOperation.Source;
        return frame;
    }

    static PngIhdr CreateIhdr(int width, int height)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = width;
        ihdr.Height = height;
        ihdr.BitDepth = PngBitDepth.Eight;
        ihdr.ColorType = PngColorType.TruecolorAlpha;
        ihdr.CompressionMethod = PngCompressionMethod.Deflate;
        ihdr.FilterMethod = PngFilterMethod.Adaptive;
        ihdr.InterlaceMethod = PngInterlaceMethod.None;
        return ihdr;
    }

    static bool HasError(PngValidationResult result, string code)
    {
        int index = 0;
        while (index < result.Errors.Length)
        {
            if (result.Errors[index].Code == code)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
