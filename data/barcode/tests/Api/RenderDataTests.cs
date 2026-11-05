namespace barcode.Tests;

public class RenderDataTests
{
    [Test]
    public void RenderOptionsAndRequestHoldAllSelections()
    {
        BarcodeSizing sizing = new BarcodeSizing();
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Format = BarcodeRenderFormat.Svg;
        options.Sizing = sizing;
        options.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        options.QuietZone = quietZone;
        options.Rotation = BarcodeRotation.Clockwise270;
        options.ModuleScale = 4;
        options.ForegroundRgba = 0x11223344u;
        options.BackgroundRgba = 0xAABBCCDDu;
        options.TransparentBackground = true;
        options.TextMode = BarcodeTextMode.Custom;
        options.CustomText = "Inventory 42";
        options.UseSvgText = true;
        BarcodeSymbol symbol = new BarcodeSymbol();
        BarcodeRenderRequest request = new BarcodeRenderRequest();
        request.Symbol = symbol;
        request.Options = options;

        Assert.Multiple(() =>
        {
            Assert.That(options.Format, Is.EqualTo(BarcodeRenderFormat.Svg));
            Assert.That(options.Sizing, Is.SameAs(sizing));
            Assert.That(options.QuietZonePolicy, Is.EqualTo(BarcodeQuietZonePolicy.ExactCustom));
            Assert.That(options.QuietZone, Is.SameAs(quietZone));
            Assert.That(options.Rotation, Is.EqualTo(BarcodeRotation.Clockwise270));
            Assert.That(options.ModuleScale, Is.EqualTo(4));
            Assert.That(options.ForegroundRgba, Is.EqualTo(0x11223344u));
            Assert.That(options.BackgroundRgba, Is.EqualTo(0xAABBCCDDu));
            Assert.That(options.TransparentBackground, Is.True);
            Assert.That(options.TextMode, Is.EqualTo(BarcodeTextMode.Custom));
            Assert.That(options.CustomText, Is.EqualTo("Inventory 42"));
            Assert.That(options.UseSvgText, Is.True);
            Assert.That(request.Symbol, Is.SameAs(symbol));
            Assert.That(request.Options, Is.SameAs(options));
        });
    }

    [Test]
    public void RenderedImageHoldsContentDimensionsLayoutAndDiagnostics()
    {
        byte[] bytes = new byte[] { 137, 80, 78, 71 };
        BarcodeSize size = new BarcodeSize();
        BarcodeTextLayout textLayout = new BarcodeTextLayout();
        BarcodeDiagnostic diagnostic = new BarcodeDiagnostic();
        BarcodeDiagnostic[] diagnostics = new BarcodeDiagnostic[] { diagnostic };
        BarcodeRenderedImage image = new BarcodeRenderedImage();
        image.Bytes = bytes;
        image.Text = "<svg></svg>";
        image.MediaType = "image/svg+xml";
        image.Format = BarcodeRenderFormat.Svg;
        image.WidthPixels = 640;
        image.HeightPixels = 240;
        image.Stride = 80;
        image.PixelFormat = BarcodePixelFormat.Gray8;
        image.WidthMillimetres = 54.2;
        image.HeightMillimetres = 20.3;
        image.DpiX = 300.0;
        image.DpiY = 300.0;
        image.Size = size;
        image.TextLayout = textLayout;
        image.Diagnostics = diagnostics;

        bytes[0] = 66;

        Assert.Multiple(() =>
        {
            Assert.That(image.Bytes, Is.SameAs(bytes));
            Assert.That(image.Bytes?[0], Is.EqualTo(66));
            Assert.That(image.Text, Is.EqualTo("<svg></svg>"));
            Assert.That(image.MediaType, Is.EqualTo("image/svg+xml"));
            Assert.That(image.Format, Is.EqualTo(BarcodeRenderFormat.Svg));
            Assert.That(image.WidthPixels, Is.EqualTo(640));
            Assert.That(image.HeightPixels, Is.EqualTo(240));
            Assert.That(image.Stride, Is.EqualTo(80));
            Assert.That(image.PixelFormat, Is.EqualTo(BarcodePixelFormat.Gray8));
            Assert.That(image.WidthMillimetres, Is.EqualTo(54.2));
            Assert.That(image.HeightMillimetres, Is.EqualTo(20.3));
            Assert.That(image.DpiX, Is.EqualTo(300.0));
            Assert.That(image.DpiY, Is.EqualTo(300.0));
            Assert.That(image.Size, Is.SameAs(size));
            Assert.That(image.TextLayout, Is.SameAs(textLayout));
            Assert.That(image.Diagnostics, Is.SameAs(diagnostics));
        });
    }

    [Test]
    public void ConfigurationHoldsRenderRequestAndOutput()
    {
        BarcodeRenderRequest request = new BarcodeRenderRequest();
        BarcodeRenderedImage image = new BarcodeRenderedImage();
        BarcodeConfiguration configuration = new BarcodeConfiguration();
        configuration.RenderRequest = request;
        configuration.RenderedImage = image;

        Assert.Multiple(() =>
        {
            Assert.That(configuration.RenderRequest, Is.SameAs(request));
            Assert.That(configuration.RenderedImage, Is.SameAs(image));
        });
    }
}
