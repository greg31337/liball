namespace barcode.Tests;

public class DomainExceptionTests
{
    [Test]
    public void EncodingExceptionPreservesMessageAndCanBeThrown()
    {
        BarcodeEncodingException exception = new BarcodeEncodingException("Encoding failed.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Encoding failed."));
            Assert.That(() => throw exception, Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void DecodingExceptionPreservesMessageAndCanBeThrown()
    {
        BarcodeDecodingException exception = new BarcodeDecodingException("Decoding failed.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Decoding failed."));
            Assert.That(() => throw exception, Throws.TypeOf<BarcodeDecodingException>());
        });
    }

    [Test]
    public void DetectionExceptionPreservesMessageAndCanBeThrown()
    {
        BarcodeDetectionException exception = new BarcodeDetectionException("Detection failed.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Detection failed."));
            Assert.That(() => throw exception, Throws.TypeOf<BarcodeDetectionException>());
        });
    }

    [Test]
    public void RenderingExceptionPreservesMessageAndCanBeThrown()
    {
        BarcodeRenderingException exception = new BarcodeRenderingException("Rendering failed.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Rendering failed."));
            Assert.That(() => throw exception, Throws.TypeOf<BarcodeRenderingException>());
        });
    }

    [Test]
    public void ValidationExceptionPreservesMessageAndCanBeThrown()
    {
        BarcodeValidationException exception = new BarcodeValidationException("Validation failed.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("Validation failed."));
            Assert.That(() => throw exception, Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void Gs1SyntaxExceptionPreservesMessageAndCanBeThrown()
    {
        Gs1SyntaxException exception = new Gs1SyntaxException("GS1 syntax failed.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("GS1 syntax failed."));
            Assert.That(() => throw exception, Throws.TypeOf<Gs1SyntaxException>());
        });
    }

    [Test]
    public void UnsupportedFormatExceptionPreservesMessageAndCanBeThrown()
    {
        UnsupportedBarcodeFormatException exception = new UnsupportedBarcodeFormatException("The format is unsupported.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("The format is unsupported."));
            Assert.That(() => throw exception, Throws.TypeOf<UnsupportedBarcodeFormatException>());
        });
    }
}
