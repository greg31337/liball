namespace barcode.Tests;

public class BarcodeRendererTests
{
    [TestCase(BarcodeRenderFormat.Raw1, BarcodeMediaTypes.Raw1)]
    [TestCase(BarcodeRenderFormat.Gray8, BarcodeMediaTypes.Gray8)]
    [TestCase(BarcodeRenderFormat.Png, BarcodeMediaTypes.Png)]
    [TestCase(BarcodeRenderFormat.Bmp, BarcodeMediaTypes.Bmp)]
    [TestCase(BarcodeRenderFormat.Svg, BarcodeMediaTypes.Svg)]
    public void RendererDispatchesEveryOutputFormat(
        BarcodeRenderFormat format,
        string mediaType)
    {
        IBarcodeRenderer renderer = new BarcodeRenderer();
        BarcodeRenderRequest request = CreateRequest(format, 2);

        BarcodeRenderedImage image = renderer.RenderBarcode(request);

        Assert.Multiple(() =>
        {
            Assert.That(image.Format, Is.EqualTo(format));
            Assert.That(image.MediaType, Is.EqualTo(mediaType));
            Assert.That(image.WidthPixels, Is.EqualTo(10));
            Assert.That(image.HeightPixels, Is.EqualTo(8));
            Assert.That(image.Size, Is.Not.Null);
            Assert.That(image.Diagnostics, Is.Not.Null.And.Not.Empty);
        });

        if (format == BarcodeRenderFormat.Svg)
        {
            Assert.Multiple(() =>
            {
                Assert.That(image.Bytes, Is.Null);
                Assert.That(image.Text, Does.StartWith("<?xml"));
                Assert.That(image.Text, Does.Contain("viewBox=\"0 0 10 8\""));
                Assert.That(image.Stride, Is.Zero);
            });
            return;
        }

        Assert.Multiple(() =>
        {
            Assert.That(image.Text, Is.Null);
            Assert.That(image.Bytes, Is.Not.Null.And.Not.Empty);
        });

        if (format == BarcodeRenderFormat.Raw1)
        {
            Assert.Multiple(() =>
            {
                Assert.That(image.Stride, Is.EqualTo(2));
                Assert.That(image.Bytes, Has.Length.EqualTo(16));
                Assert.That(image.PixelFormat, Is.EqualTo(BarcodePixelFormat.Gray8));
            });
        }
        else if (format == BarcodeRenderFormat.Gray8)
        {
            Assert.Multiple(() =>
            {
                Assert.That(image.Stride, Is.EqualTo(10));
                Assert.That(image.Bytes, Has.Length.EqualTo(80));
                Assert.That(image.PixelFormat, Is.EqualTo(BarcodePixelFormat.Gray8));
            });
        }
        else if (format == BarcodeRenderFormat.Png)
        {
            Assert.Multiple(() =>
            {
                Assert.That(image.Stride, Is.Zero);
                Assert.That(image.Bytes![0], Is.EqualTo(137));
                Assert.That(image.Bytes[1], Is.EqualTo(80));
                Assert.That(image.Bytes[2], Is.EqualTo(78));
                Assert.That(image.Bytes[3], Is.EqualTo(71));
            });
        }
        else
        {
            Assert.Multiple(() =>
            {
                Assert.That(image.Stride, Is.Zero);
                Assert.That(image.Bytes![0], Is.EqualTo((byte)'B'));
                Assert.That(image.Bytes[1], Is.EqualTo((byte)'M'));
            });
        }
    }

    [Test]
    public void RendererReportsRotatedActualSizeAndPlanDiagnostics()
    {
        IBarcodeRenderer renderer = new BarcodeRenderer();
        BarcodeRenderRequest request = CreateRequest(
            BarcodeRenderFormat.Gray8,
            4);
        request.Options!.Rotation = BarcodeRotation.Clockwise90;
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.DotsPerInch = 200.0;
        request.Options.Sizing = sizing;

        BarcodeRenderedImage image = renderer.RenderBarcode(request);

        Assert.Multiple(() =>
        {
            Assert.That(image.WidthPixels, Is.EqualTo(16));
            Assert.That(image.HeightPixels, Is.EqualTo(20));
            Assert.That(image.WidthMillimetres,
                Is.EqualTo(16.0 * 25.4 / 200.0).Within(0.0000001));
            Assert.That(image.HeightMillimetres,
                Is.EqualTo(20.0 * 25.4 / 200.0).Within(0.0000001));
            Assert.That(image.DpiX, Is.EqualTo(200.0));
            Assert.That(image.DpiY, Is.EqualTo(200.0));
            Assert.That(image.Size!.WidthPixels, Is.EqualTo(16));
            Assert.That(image.Size.HeightPixels, Is.EqualTo(20));
            Assert.That(image.Size.ModuleSizeMillimetres,
                Is.EqualTo(4.0 * 25.4 / 200.0).Within(0.0000001));
            Assert.That(image.Size.QuietZone!.LeftPixels, Is.EqualTo(4));
            Assert.That(image.Diagnostics, Has.Length.EqualTo(4));
            Assert.That(image.Diagnostics![0].Stage, Is.EqualTo("RenderPlan"));
            Assert.That(image.Diagnostics[0].Code, Is.EqualTo("MODULE_SCALE"));
            Assert.That(image.Diagnostics[0].HasNumericEvidence, Is.True);
            Assert.That(image.Diagnostics[0].Value, Is.EqualTo(4.0));
            Assert.That(image.Diagnostics[1].Code,
                Is.EqualTo("ACTUAL_WIDTH_MILLIMETRES"));
            Assert.That(image.Diagnostics[2].Code,
                Is.EqualTo("ACTUAL_HEIGHT_MILLIMETRES"));
            Assert.That(image.Diagnostics[3].Code, Is.EqualTo("OUTPUT_BOUNDS"));
            Assert.That(image.Diagnostics[3].Region!.Width, Is.EqualTo(16.0));
            Assert.That(image.Diagnostics[3].Region!.Height, Is.EqualTo(20.0));
        });
    }

    [Test]
    public void RendererReturnsIndependentResolvedMetadata()
    {
        IBarcodeRenderer renderer = new BarcodeRenderer();
        BarcodeRenderRequest request = CreateRequest(
            BarcodeRenderFormat.Gray8,
            3);

        BarcodeRenderedImage first = renderer.RenderBarcode(request);
        first.Size!.QuietZone!.LeftPixels = 999;
        first.Diagnostics![3].Region!.Width = 999.0;

        BarcodeRenderedImage second = renderer.RenderBarcode(request);

        Assert.Multiple(() =>
        {
            Assert.That(second.Size, Is.Not.SameAs(first.Size));
            Assert.That(second.Size!.QuietZone,
                Is.Not.SameAs(first.Size.QuietZone));
            Assert.That(second.Size.QuietZone!.LeftPixels, Is.EqualTo(3));
            Assert.That(second.Diagnostics, Is.Not.SameAs(first.Diagnostics));
            Assert.That(second.Diagnostics![3].Region,
                Is.Not.SameAs(first.Diagnostics[3].Region));
            Assert.That(second.Diagnostics[3].Region!.Width, Is.EqualTo(15.0));
        });
    }

    [Test]
    public void RendererRefusesFractionalConflictingAndUnknownScales()
    {
        IBarcodeRenderer renderer = new BarcodeRenderer();
        BarcodeRenderRequest fractional = CreateRequest(
            BarcodeRenderFormat.Png,
            0);
        BarcodeSizing fractionalSizing = new BarcodeSizing();
        fractionalSizing.RequestedWidthPixels = 26;
        fractional.Options!.Sizing = fractionalSizing;

        BarcodeRenderRequest conflicting = CreateRequest(
            BarcodeRenderFormat.Bmp,
            2);
        BarcodeSizing conflictingSizing = new BarcodeSizing();
        conflictingSizing.RequestedWidthPixels = 15;
        conflicting.Options!.Sizing = conflictingSizing;

        BarcodeRenderRequest unknown = CreateRequest(
            (BarcodeRenderFormat)99,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => renderer.RenderBarcode(fractional),
                Throws.TypeOf<BarcodeRenderingException>().With.Message.EqualTo(
                    "The requested pixel width is not an integral module scale."));
            Assert.That(
                () => renderer.RenderBarcode(conflicting),
                Throws.TypeOf<BarcodeRenderingException>().With.Message.EqualTo(
                    "The requested pixel dimensions would distort the modules."));
            Assert.That(
                () => renderer.RenderBarcode(unknown),
                Throws.TypeOf<BarcodeRenderingException>().With.Message.EqualTo(
                    "The requested barcode render format is invalid."));
        });
    }

    static BarcodeRenderRequest CreateRequest(
        BarcodeRenderFormat format,
        int moduleScale)
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = 1;
        quietZone.TopModules = 1;
        quietZone.RightModules = 1;
        quietZone.BottomModules = 1;

        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Dimension = BarcodeDimension.Matrix;
        symbol.WidthModules = 3;
        symbol.HeightModules = 2;
        symbol.Modules = new byte[] { 1, 0, 1, 0, 1, 0 };
        symbol.RequiredQuietZone = quietZone;

        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Format = format;
        options.ModuleScale = moduleScale;

        BarcodeRenderRequest request = new BarcodeRenderRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
    }
}
