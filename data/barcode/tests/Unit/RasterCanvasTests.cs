namespace barcode.Tests;

public class RasterCanvasTests
{
    [Test]
    public void Gray8UsesDefaultBlackAndWhiteWithExactIntegralScaling()
    {
        byte[] modules = new byte[]
        {
            1, 0, 1,
            0, 1, 0
        };
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            3,
            2,
            modules,
            CreateQuietZone(1, 1, 1, 1));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 2;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        RasterCanvas canvas = RasterCanvas.CreateGray8(plan, options);

        Assert.Multiple(() =>
        {
            Assert.That(canvas.Format, Is.EqualTo(BarcodeRenderFormat.Gray8));
            Assert.That(canvas.PixelFormat, Is.EqualTo(BarcodePixelFormat.Gray8));
            Assert.That(canvas.Width, Is.EqualTo(10));
            Assert.That(canvas.Height, Is.EqualTo(8));
            Assert.That(canvas.Stride, Is.EqualTo(10));
            Assert.That(canvas.Pixels, Has.Length.EqualTo(80));
            Assert.That(canvas.ForegroundSample, Is.EqualTo(0));
            Assert.That(canvas.BackgroundSample, Is.EqualTo(255));
            Assert.That(canvas.Pixels,
                Is.EqualTo(CreateExpectedGrayPixels(plan, 0, 255)));
        });
    }

    [TestCase(BarcodeRotation.None)]
    [TestCase(BarcodeRotation.Clockwise90)]
    [TestCase(BarcodeRotation.Clockwise180)]
    [TestCase(BarcodeRotation.Clockwise270)]
    public void Gray8RotationPreservesNearestNeighbourModuleBlocks(
        BarcodeRotation rotation)
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            3,
            2,
            new byte[] { 1, 0, 0, 0, 1, 1 },
            CreateQuietZone(1, 2, 3, 1));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 3;
        options.Rotation = rotation;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        RasterCanvas canvas = RasterCanvas.CreateGray8(plan, options);

        Assert.Multiple(() =>
        {
            Assert.That(canvas.Width, Is.EqualTo(plan.Size.WidthPixels));
            Assert.That(canvas.Height, Is.EqualTo(plan.Size.HeightPixels));
            Assert.That(canvas.Pixels,
                Is.EqualTo(CreateExpectedGrayPixels(plan, 0, 255)));
            Assert.That(CountSample(canvas.Pixels, 0),
                Is.EqualTo(3 * 3 * 3));
        });
    }

    [Test]
    public void Gray8CompositesRgbaColorsBeforeLuminanceConversion()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            1,
            1,
            new byte[] { 1 },
            CreateQuietZone(0, 0, 1, 0));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ForegroundRgba = 0xFF000080u;
        options.BackgroundRgba = 0x0000FF80u;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        RasterCanvas canvas = RasterCanvas.CreateGray8(plan, options);

        Assert.Multiple(() =>
        {
            Assert.That(canvas.ForegroundSample, Is.EqualTo(109));
            Assert.That(canvas.BackgroundSample, Is.EqualTo(142));
            Assert.That(canvas.Pixels, Is.EqualTo(new byte[] { 109, 142 }));
        });
    }

    [Test]
    public void TransparentBackgroundFlattensToWhiteForAlphaLessOutput()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            1,
            1,
            new byte[] { 1 },
            CreateQuietZone(0, 0, 1, 0));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ForegroundRgba = 0xFF000080u;
        options.BackgroundRgba = 0x000000FFu;
        options.TransparentBackground = true;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        RasterCanvas canvas = RasterCanvas.CreateGray8(plan, options);

        Assert.Multiple(() =>
        {
            Assert.That(canvas.ForegroundSample, Is.EqualTo(166));
            Assert.That(canvas.BackgroundSample, Is.EqualTo(255));
            Assert.That(canvas.Pixels, Is.EqualTo(new byte[] { 166, 255 }));
        });
    }

    [Test]
    public void Raw1PacksMostSignificantBitFirstAndClearsTailBits()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Linear,
            10,
            1,
            new byte[] { 1, 0, 1, 0, 0, 0, 0, 0, 0, 1 },
            CreateQuietZone(0, 0, 0, 0));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        RasterCanvas canvas = RasterCanvas.CreateRaw1(plan, options);

        Assert.Multiple(() =>
        {
            Assert.That(canvas.Format, Is.EqualTo(BarcodeRenderFormat.Raw1));
            Assert.That(canvas.Width, Is.EqualTo(10));
            Assert.That(canvas.Height, Is.EqualTo(1));
            Assert.That(canvas.Stride, Is.EqualTo(2));
            Assert.That(canvas.Pixels,
                Is.EqualTo(new byte[] { 0x5F, 0x80 }));
        });
    }

    [Test]
    public void Raw1HonorsInvertedForegroundAndBackgroundLuminance()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Linear,
            3,
            1,
            new byte[] { 1, 0, 0 },
            CreateQuietZone(0, 0, 0, 0));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ForegroundRgba = 0xFFFFFFFFu;
        options.BackgroundRgba = 0x000000FFu;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        RasterCanvas canvas = RasterCanvas.CreateRaw1(plan, options);

        Assert.Multiple(() =>
        {
            Assert.That(canvas.ForegroundSample, Is.EqualTo(255));
            Assert.That(canvas.BackgroundSample, Is.EqualTo(0));
            Assert.That(canvas.Stride, Is.EqualTo(1));
            Assert.That(canvas.Pixels, Is.EqualTo(new byte[] { 0x80 }));
        });
    }

    [Test]
    public void LinearBarsUseResolvedHeightWithoutInterpolatingHorizontally()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Linear,
            4,
            1,
            new byte[] { 1, 0, 1, 0 },
            CreateQuietZone(1, 1, 1, 1));
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedHeightPixels = 17;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 3;
        options.Sizing = sizing;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        RasterCanvas canvas = RasterCanvas.CreateGray8(plan, options);

        Assert.Multiple(() =>
        {
            Assert.That(plan.BarHeightPixels, Is.EqualTo(11));
            Assert.That(canvas.Width, Is.EqualTo(18));
            Assert.That(canvas.Height, Is.EqualTo(17));
            Assert.That(CountSample(canvas.Pixels, 0),
                Is.EqualTo(2 * 3 * 11));
            Assert.That(canvas.Pixels,
                Is.EqualTo(CreateExpectedGrayPixels(plan, 0, 255)));
        });
    }

    [Test]
    public void MissingOrInconsistentPlansAreRejected()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            1,
            1,
            new byte[] { 1 },
            CreateQuietZone(0, 0, 0, 0));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));
        plan.Size.WidthPixels = 2;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => RasterCanvas.CreateGray8(null, options),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => RasterCanvas.CreateRaw1(plan, options),
                Throws.TypeOf<BarcodeRenderingException>());
        });
    }

    static BarcodeRenderRequest CreateRequest(
        BarcodeSymbol symbol,
        BarcodeRenderOptions options)
    {
        BarcodeRenderRequest request = new BarcodeRenderRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
    }

    static BarcodeSymbol CreateSymbol(
        BarcodeDimension dimension,
        int width,
        int height,
        byte[] modules,
        BarcodeQuietZone quietZone)
    {
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Dimension = dimension;
        symbol.WidthModules = width;
        symbol.HeightModules = height;
        symbol.Modules = modules;
        symbol.RequiredQuietZone = quietZone;
        return symbol;
    }

    static BarcodeQuietZone CreateQuietZone(
        int left,
        int top,
        int right,
        int bottom)
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = left;
        quietZone.TopModules = top;
        quietZone.RightModules = right;
        quietZone.BottomModules = bottom;
        return quietZone;
    }

    static byte[] CreateExpectedGrayPixels(
        RenderPlan plan,
        byte foreground,
        byte background)
    {
        int width = plan.Size.WidthPixels;
        int height = plan.Size.HeightPixels;
        byte[] result = new byte[width * height];
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int sourceX = InverseRotateX(plan, x, y);
                int sourceY = InverseRotateY(plan, x, y);
                result[(y * width) + x] = IsDarkSourcePixel(
                    plan,
                    sourceX,
                    sourceY)
                    ? foreground
                    : background;
                x = x + 1;
            }

            y = y + 1;
        }

        return result;
    }

    static bool IsDarkSourcePixel(RenderPlan plan, int x, int y)
    {
        BarcodeSymbol symbol = plan.Symbol;
        int localX = x - plan.ContentXPixels;
        int localY = y - plan.ContentYPixels;
        if (localX < 0
            || localX >= symbol.WidthModules * plan.ModuleScale
            || localY < 0
            || localY >= plan.BarHeightPixels)
        {
            return false;
        }

        int moduleX = localX / plan.ModuleScale;
        int moduleY = symbol.Dimension == BarcodeDimension.Linear
            ? 0
            : localY / plan.ModuleScale;
        if (moduleY >= symbol.HeightModules)
        {
            return false;
        }

        return symbol.Modules![
            (moduleY * symbol.WidthModules) + moduleX] != 0;
    }

    static int InverseRotateX(RenderPlan plan, int x, int y)
    {
        switch (plan.Rotation)
        {
            case BarcodeRotation.None:
                return x;
            case BarcodeRotation.Clockwise90:
                return y;
            case BarcodeRotation.Clockwise180:
                return plan.UnrotatedWidthPixels - 1 - x;
            case BarcodeRotation.Clockwise270:
                return plan.UnrotatedWidthPixels - 1 - y;
            default:
                return 0;
        }
    }

    static int InverseRotateY(RenderPlan plan, int x, int y)
    {
        switch (plan.Rotation)
        {
            case BarcodeRotation.None:
                return y;
            case BarcodeRotation.Clockwise90:
                return plan.UnrotatedHeightPixels - 1 - x;
            case BarcodeRotation.Clockwise180:
                return plan.UnrotatedHeightPixels - 1 - y;
            case BarcodeRotation.Clockwise270:
                return x;
            default:
                return 0;
        }
    }

    static int CountSample(byte[] pixels, byte sample)
    {
        int count = 0;
        int index = 0;
        while (index < pixels.Length)
        {
            if (pixels[index] == sample)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
