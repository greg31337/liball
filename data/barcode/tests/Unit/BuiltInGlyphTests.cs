namespace barcode.Tests;

public class BuiltInGlyphTests
{
    [Test]
    public void PrintableAsciiTableHasStableMetricsAndFallbackGlyphs()
    {
        int character = BuiltInGlyphs.FirstCharacter;
        while (character <= BuiltInGlyphs.LastCharacter)
        {
            int column = 0;
            while (column < BuiltInGlyphs.Width)
            {
                Assert.That(
                    BuiltInGlyphs.ReadColumn((char)character, column),
                    Is.InRange(0, 127));
                column = column + 1;
            }

            character = character + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(BuiltInGlyphs.Width, Is.EqualTo(5));
            Assert.That(BuiltInGlyphs.Height, Is.EqualTo(7));
            Assert.That(BuiltInGlyphs.Advance, Is.EqualTo(6));
            Assert.That(ReadColumns(' '),
                Is.EqualTo(new int[] { 0, 0, 0, 0, 0 }));
            Assert.That(ReadColumns('A'),
                Is.EqualTo(new int[] { 0x7E, 0x11, 0x11, 0x11, 0x7E }));
            Assert.That(ReadColumns('\u2603'), Is.EqualTo(ReadColumns('?')));
            Assert.That(
                () => BuiltInGlyphs.ReadColumn('A', 5),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => BuiltInGlyphs.IsDark('A', 0, 7),
                Throws.TypeOf<BarcodeRenderingException>());
        });
    }

    [TestCase(BarcodeRotation.None, 22, 16)]
    [TestCase(BarcodeRotation.Clockwise90, 16, 22)]
    [TestCase(BarcodeRotation.Clockwise180, 22, 16)]
    [TestCase(BarcodeRotation.Clockwise270, 16, 22)]
    public void GlyphGeometryDrawsDeterministicPixelAlignedRotations(
        BarcodeRotation rotation,
        int expectedWidth,
        int expectedHeight)
    {
        BarcodeRenderOptions options = CreateTextOptions(rotation);
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(CreateTextSymbol("A"), options));

        HumanReadableTextRenderResult first =
            HumanReadableTextRenderer.Render(plan);
        HumanReadableTextRenderResult second =
            HumanReadableTextRenderer.Render(plan);
        RasterCanvas gray = RasterCanvas.CreateGray8(plan, options);
        RasterCanvas raw = RasterCanvas.CreateRaw1(plan, options);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Size.WidthPixels, Is.EqualTo(expectedWidth));
            Assert.That(plan.Size.HeightPixels, Is.EqualTo(expectedHeight));
            Assert.That(first.GlyphCount, Is.EqualTo(1));
            Assert.That(first.TextRunCount, Is.EqualTo(1));
            Assert.That(first.PixelRectangles, Has.Length.EqualTo(12));
            Assert.That(RectangleValues(first.PixelRectangles),
                Is.EqualTo(RectangleValues(second.PixelRectangles)));
            Assert.That(AllRectanglesAreInside(first.PixelRectangles, plan),
                Is.True);
            Assert.That(SumArea(first.PixelRectangles), Is.EqualTo(160));
            Assert.That(CountGrayPixels(gray.Pixels, 0), Is.EqualTo(160));
            Assert.That(CountRawBits(raw, 0), Is.EqualTo(160));
        });
    }

    [Test]
    public void SpaceIsBlankAndUnicodeScalarUsesOneFallbackGlyph()
    {
        BarcodeRenderOptions options = CreateTextOptions(
            BarcodeRotation.None);
        RenderPlan spacePlan = RenderPlanner.CreatePlan(
            CreateRequest(CreateTextSymbol(" "), options));
        RenderPlan unicodePlan = RenderPlanner.CreatePlan(
            CreateRequest(CreateTextSymbol("\U0001F642"), options));
        RenderPlan fallbackPlan = RenderPlanner.CreatePlan(
            CreateRequest(CreateTextSymbol("?"), options));

        HumanReadableTextRenderResult space =
            HumanReadableTextRenderer.Render(spacePlan);
        HumanReadableTextRenderResult unicode =
            HumanReadableTextRenderer.Render(unicodePlan);
        HumanReadableTextRenderResult fallback =
            HumanReadableTextRenderer.Render(fallbackPlan);

        Assert.Multiple(() =>
        {
            Assert.That(space.GlyphCount, Is.EqualTo(1));
            Assert.That(space.PixelRectangles, Is.Empty);
            Assert.That(unicode.GlyphCount, Is.EqualTo(1));
            Assert.That(RectangleValues(unicode.PixelRectangles),
                Is.EqualTo(RectangleValues(fallback.PixelRectangles)));
        });
    }

    [Test]
    public void MissingPlansAndInvalidTextGeometryAreRejected()
    {
        BarcodeRenderOptions options = CreateTextOptions(
            BarcodeRotation.None);
        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(CreateTextSymbol("A"), options));
        BarcodeTextRun run = plan.TextLayout!.Runs![0];
        run.BaselineY = double.NaN;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => HumanReadableTextRenderer.Render(null),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => HumanReadableTextRenderer.Render(plan),
                Throws.TypeOf<BarcodeRenderingException>());
        });
    }

    static int[] ReadColumns(char character)
    {
        int[] result = new int[BuiltInGlyphs.Width];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = BuiltInGlyphs.ReadColumn(character, index);
            index = index + 1;
        }

        return result;
    }

    static BarcodeRenderOptions CreateTextOptions(
        BarcodeRotation rotation)
    {
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 2;
        options.Rotation = rotation;
        options.TextMode = BarcodeTextMode.Custom;
        options.CustomText = "A";
        return options;
    }

    static BarcodeSymbol CreateTextSymbol(string text)
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.ReservedHeight = 7.0;
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Dimension = BarcodeDimension.Matrix;
        symbol.WidthModules = 11;
        symbol.HeightModules = 1;
        symbol.Modules = new byte[11];
        symbol.RequiredQuietZone = quietZone;
        symbol.TextLayout = layout;
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        symbol.Payload = payload;
        return symbol;
    }

    static BarcodeRenderRequest CreateRequest(
        BarcodeSymbol symbol,
        BarcodeRenderOptions options)
    {
        options.CustomText = symbol.Payload!.Text;
        BarcodeRenderRequest request = new BarcodeRenderRequest();
        request.Symbol = symbol;
        request.Options = options;
        return request;
    }

    static double[] RectangleValues(BarcodeRectangle[] rectangles)
    {
        double[] values = new double[rectangles.Length * 4];
        int index = 0;
        while (index < rectangles.Length)
        {
            values[index * 4] = rectangles[index].X;
            values[(index * 4) + 1] = rectangles[index].Y;
            values[(index * 4) + 2] = rectangles[index].Width;
            values[(index * 4) + 3] = rectangles[index].Height;
            index = index + 1;
        }

        return values;
    }

    static bool AllRectanglesAreInside(
        BarcodeRectangle[] rectangles,
        RenderPlan plan)
    {
        int index = 0;
        while (index < rectangles.Length)
        {
            BarcodeRectangle rectangle = rectangles[index];
            if (rectangle.X < 0.0
                || rectangle.Y < 0.0
                || rectangle.Width <= 0.0
                || rectangle.Height <= 0.0
                || rectangle.X + rectangle.Width > plan.Size.WidthPixels
                || rectangle.Y + rectangle.Height > plan.Size.HeightPixels)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
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

    static int CountGrayPixels(byte[] pixels, byte value)
    {
        int count = 0;
        int index = 0;
        while (index < pixels.Length)
        {
            if (pixels[index] == value)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    static int CountRawBits(RasterCanvas canvas, int value)
    {
        int count = 0;
        int row = 0;
        while (row < canvas.Height)
        {
            int column = 0;
            while (column < canvas.Width)
            {
                int offset = (row * canvas.Stride) + (column / 8);
                int bit = (canvas.Pixels[offset]
                    >> (7 - (column % 8))) & 1;
                if (bit == value)
                {
                    count = count + 1;
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return count;
    }
}
