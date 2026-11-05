namespace barcode.Tests;

public class RectangleCoalescerTests
{
    [Test]
    public void LinearRunsBecomeFullHeightScaledBarRectangles()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Linear,
            8,
            1,
            new byte[] { 1, 1, 0, 1, 0, 1, 1, 1 },
            CreateQuietZone(2, 1, 1, 2));
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedHeightPixels = 20;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 3;
        options.Sizing = sizing;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        RenderRectangleSet result = RectangleCoalescer.Coalesce(plan);

        Assert.Multiple(() =>
        {
            Assert.That(plan.BarHeightPixels, Is.EqualTo(11));
            Assert.That(result.ModuleRectangles, Has.Length.EqualTo(3));
            AssertRectangle(result.ModuleRectangles[0], 2, 1, 2, 1);
            AssertRectangle(result.ModuleRectangles[1], 5, 1, 1, 1);
            AssertRectangle(result.ModuleRectangles[2], 7, 1, 3, 1);
            AssertRectangle(result.PixelRectangles[0], 6, 3, 6, 11);
            AssertRectangle(result.PixelRectangles[1], 15, 3, 3, 11);
            AssertRectangle(result.PixelRectangles[2], 21, 3, 9, 11);
        });
    }

    [Test]
    public void MatrixRunsMergeVerticallyOnlyWhenBothEdgesMatch()
    {
        byte[] modules = new byte[]
        {
            1, 1, 0, 1, 1,
            1, 1, 0, 0, 1,
            0, 0, 1, 1, 1,
            0, 0, 1, 1, 1
        };
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            5,
            4,
            modules,
            CreateQuietZone(1, 1, 1, 1));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 2;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        RenderRectangleSet result = RectangleCoalescer.Coalesce(plan);

        Assert.Multiple(() =>
        {
            Assert.That(result.ModuleRectangles, Has.Length.EqualTo(4));
            AssertRectangle(result.ModuleRectangles[0], 1, 1, 2, 2);
            AssertRectangle(result.ModuleRectangles[1], 4, 1, 2, 1);
            AssertRectangle(result.ModuleRectangles[2], 5, 2, 1, 1);
            AssertRectangle(result.ModuleRectangles[3], 3, 3, 3, 2);
            AssertRectangle(result.PixelRectangles[0], 2, 2, 4, 4);
            AssertRectangle(result.PixelRectangles[1], 8, 2, 4, 2);
            AssertRectangle(result.PixelRectangles[2], 10, 4, 2, 2);
            AssertRectangle(result.PixelRectangles[3], 6, 6, 6, 4);
            Assert.That(SumArea(result.ModuleRectangles), Is.EqualTo(13));
        });
    }

    [TestCase(BarcodeRotation.None)]
    [TestCase(BarcodeRotation.Clockwise90)]
    [TestCase(BarcodeRotation.Clockwise180)]
    [TestCase(BarcodeRotation.Clockwise270)]
    public void RotatedRectanglesReconstructEveryDarkModuleExactly(
        BarcodeRotation rotation)
    {
        byte[] modules = new byte[]
        {
            1, 1, 0,
            0, 1, 0,
            0, 1, 1
        };
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            3,
            3,
            modules,
            CreateQuietZone(1, 2, 3, 4));
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 3;
        options.Rotation = rotation;
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        RenderRectangleSet result = RectangleCoalescer.Coalesce(plan);
        bool[] expected = CreateExpectedRotatedModules(plan);
        bool[] observed = FillRectangles(
            result.ModuleRectangles,
            plan.Size.WidthModules,
            plan.Size.HeightModules);

        Assert.Multiple(() =>
        {
            Assert.That(observed, Is.EqualTo(expected));
            Assert.That(SumArea(result.ModuleRectangles), Is.EqualTo(5));
            Assert.That(SumArea(result.PixelRectangles),
                Is.EqualTo(5 * 3 * 3));
            Assert.That(AllRectangleEdgesAreIntegers(
                result.ModuleRectangles), Is.True);
            Assert.That(AllRectangleEdgesAreIntegers(
                result.PixelRectangles), Is.True);
        });
    }

    [Test]
    public void EmptyAndNonBinaryModuleValuesAreHandledDeterministically()
    {
        BarcodeSymbol empty = CreateSymbol(
            BarcodeDimension.Matrix,
            3,
            2,
            new byte[6],
            CreateQuietZone(0, 0, 0, 0));
        BarcodeSymbol nonBinary = CreateSymbol(
            BarcodeDimension.Matrix,
            3,
            2,
            new byte[] { 0, 2, 255, 0, 2, 255 },
            CreateQuietZone(0, 0, 0, 0));
        BarcodeRenderOptions options = new BarcodeRenderOptions();

        RenderRectangleSet emptyResult = RectangleCoalescer.Coalesce(
            RenderPlanner.CreatePlan(CreateRequest(empty, options)));
        RenderRectangleSet darkResult = RectangleCoalescer.Coalesce(
            RenderPlanner.CreatePlan(CreateRequest(nonBinary, options)));

        Assert.Multiple(() =>
        {
            Assert.That(emptyResult.ModuleRectangles, Is.Empty);
            Assert.That(emptyResult.PixelRectangles, Is.Empty);
            Assert.That(darkResult.ModuleRectangles, Has.Length.EqualTo(1));
            AssertRectangle(darkResult.ModuleRectangles[0], 1, 0, 2, 2);
        });
    }

    [Test]
    public void InvalidPlansAreRejectedBeforeScanningModules()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Linear,
            3,
            1,
            new byte[] { 1, 0, 1 },
            CreateQuietZone(0, 0, 0, 0));
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, new BarcodeRenderOptions()));
        symbol.HeightModules = 2;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => RectangleCoalescer.Coalesce(null),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => RectangleCoalescer.Coalesce(plan),
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

    static void AssertRectangle(
        BarcodeRectangle rectangle,
        int x,
        int y,
        int width,
        int height)
    {
        Assert.Multiple(() =>
        {
            Assert.That(rectangle.X, Is.EqualTo((double)x));
            Assert.That(rectangle.Y, Is.EqualTo((double)y));
            Assert.That(rectangle.Width, Is.EqualTo((double)width));
            Assert.That(rectangle.Height, Is.EqualTo((double)height));
        });
    }

    static int SumArea(BarcodeRectangle[] rectangles)
    {
        int area = 0;
        int index = 0;
        while (index < rectangles.Length)
        {
            area = area + (int)(rectangles[index].Width
                * rectangles[index].Height);
            index = index + 1;
        }

        return area;
    }

    static bool[] CreateExpectedRotatedModules(RenderPlan plan)
    {
        int outputWidth = plan.Size.WidthModules;
        int outputHeight = plan.Size.HeightModules;
        bool[] result = new bool[outputWidth * outputHeight];
        BarcodeSymbol symbol = plan.Symbol;
        byte[] source = symbol.Modules!;
        int row = 0;
        while (row < symbol.HeightModules)
        {
            int column = 0;
            while (column < symbol.WidthModules)
            {
                if (source[(row * symbol.WidthModules) + column] != 0)
                {
                    int sourceX = plan.ContentXModules + column;
                    int sourceY = plan.ContentYModules + row;
                    int outputX = RotateX(plan, sourceX, sourceY);
                    int outputY = RotateY(plan, sourceX, sourceY);
                    result[(outputY * outputWidth) + outputX] = true;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return result;
    }

    static int RotateX(RenderPlan plan, int x, int y)
    {
        switch (plan.Rotation)
        {
            case BarcodeRotation.None:
                return x;
            case BarcodeRotation.Clockwise90:
                return plan.UnrotatedHeightModules - 1 - y;
            case BarcodeRotation.Clockwise180:
                return plan.UnrotatedWidthModules - 1 - x;
            case BarcodeRotation.Clockwise270:
                return y;
            default:
                return 0;
        }
    }

    static int RotateY(RenderPlan plan, int x, int y)
    {
        switch (plan.Rotation)
        {
            case BarcodeRotation.None:
                return y;
            case BarcodeRotation.Clockwise90:
                return x;
            case BarcodeRotation.Clockwise180:
                return plan.UnrotatedHeightModules - 1 - y;
            case BarcodeRotation.Clockwise270:
                return plan.UnrotatedWidthModules - 1 - x;
            default:
                return 0;
        }
    }

    static bool[] FillRectangles(
        BarcodeRectangle[] rectangles,
        int width,
        int height)
    {
        bool[] result = new bool[width * height];
        int rectangleIndex = 0;
        while (rectangleIndex < rectangles.Length)
        {
            BarcodeRectangle rectangle = rectangles[rectangleIndex];
            int top = (int)rectangle.Y;
            int bottom = top + (int)rectangle.Height;
            int row = top;
            while (row < bottom)
            {
                int left = (int)rectangle.X;
                int right = left + (int)rectangle.Width;
                int column = left;
                while (column < right)
                {
                    int offset = (row * width) + column;
                    Assert.That(result[offset], Is.False,
                        "Coalesced rectangles must not overlap.");
                    result[offset] = true;
                    column = column + 1;
                }

                row = row + 1;
            }

            rectangleIndex = rectangleIndex + 1;
        }

        return result;
    }

    static bool AllRectangleEdgesAreIntegers(
        BarcodeRectangle[] rectangles)
    {
        int index = 0;
        while (index < rectangles.Length)
        {
            BarcodeRectangle rectangle = rectangles[index];
            if (rectangle.X != global::System.Math.Floor(rectangle.X)
                || rectangle.Y != global::System.Math.Floor(rectangle.Y)
                || rectangle.Width
                    != global::System.Math.Floor(rectangle.Width)
                || rectangle.Height
                    != global::System.Math.Floor(rectangle.Height))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
