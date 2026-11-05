namespace barcode.Tests;

public class RenderPlannerTests
{
    [Test]
    public void StandardPlanAddsRequiredQuietZoneExactlyOnce()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            4,
            2,
            CreateQuietZone(1, 1, 1, 1));
        BarcodeSize encodedSize = new BarcodeSize();
        encodedSize.WidthModules = 99;
        encodedSize.HeightModules = 88;
        encodedSize.QuietZone = CreateQuietZone(20, 20, 20, 20);
        symbol.Size = encodedSize;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 3;

        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        Assert.Multiple(() =>
        {
            Assert.That(plan.UnrotatedWidthModules, Is.EqualTo(6));
            Assert.That(plan.UnrotatedHeightModules, Is.EqualTo(4));
            Assert.That(plan.UnrotatedWidthPixels, Is.EqualTo(18));
            Assert.That(plan.UnrotatedHeightPixels, Is.EqualTo(12));
            Assert.That(plan.ContentXModules, Is.EqualTo(1));
            Assert.That(plan.ContentYModules, Is.EqualTo(1));
            Assert.That(plan.ContentXPixels, Is.EqualTo(3));
            Assert.That(plan.ContentYPixels, Is.EqualTo(3));
            Assert.That(plan.Size.WidthModules, Is.EqualTo(6));
            Assert.That(plan.Size.HeightModules, Is.EqualTo(4));
            Assert.That(plan.Size.WidthPixels, Is.EqualTo(18));
            Assert.That(plan.Size.HeightPixels, Is.EqualTo(12));
            Assert.That(plan.QuietZone.LeftPixels, Is.EqualTo(3));
            Assert.That(plan.QuietZone.RightPixels, Is.EqualTo(3));
        });
    }

    [Test]
    public void ExactPixelQuietZonesAndRotationResolveOutputAxes()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            4,
            2,
            CreateQuietZone(1, 1, 1, 1));
        BarcodeQuietZone custom = CreateQuietZone(2, 3, 4, 5);
        custom.LeftPixels = 7;
        custom.RightPixels = 13;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 2;
        options.QuietZonePolicy = BarcodeQuietZonePolicy.ExactCustom;
        options.QuietZone = custom;
        options.Rotation = BarcodeRotation.Clockwise90;

        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        Assert.Multiple(() =>
        {
            Assert.That(plan.UnrotatedWidthModules, Is.EqualTo(10));
            Assert.That(plan.UnrotatedHeightModules, Is.EqualTo(10));
            Assert.That(plan.UnrotatedWidthPixels, Is.EqualTo(28));
            Assert.That(plan.UnrotatedHeightPixels, Is.EqualTo(20));
            Assert.That(plan.Size.WidthPixels, Is.EqualTo(20));
            Assert.That(plan.Size.HeightPixels, Is.EqualTo(28));
            Assert.That(plan.Size.QuietZone!.LeftModules, Is.EqualTo(5));
            Assert.That(plan.Size.QuietZone.TopModules, Is.EqualTo(2));
            Assert.That(plan.Size.QuietZone.RightModules, Is.EqualTo(3));
            Assert.That(plan.Size.QuietZone.BottomModules, Is.EqualTo(4));
            Assert.That(plan.Size.QuietZone.LeftPixels, Is.EqualTo(10));
            Assert.That(plan.Size.QuietZone.TopPixels, Is.EqualTo(7));
            Assert.That(plan.Size.QuietZone.RightPixels, Is.EqualTo(6));
            Assert.That(plan.Size.QuietZone.BottomPixels, Is.EqualTo(13));
        });
    }

    [Test]
    public void MatrixPixelDimensionsSelectOneIntegralUndistortedScale()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            4,
            2,
            CreateQuietZone(1, 1, 1, 1));
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthPixels = 24;
        sizing.RequestedHeightPixels = 16;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Sizing = sizing;

        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        BarcodeSizing fractionalSizing = new BarcodeSizing();
        fractionalSizing.RequestedWidthPixels = 25;
        BarcodeRenderOptions fractional = new BarcodeRenderOptions();
        fractional.Sizing = fractionalSizing;

        BarcodeSizing distortedSizing = new BarcodeSizing();
        distortedSizing.RequestedWidthPixels = 24;
        distortedSizing.RequestedHeightPixels = 12;
        BarcodeRenderOptions distorted = new BarcodeRenderOptions();
        distorted.Sizing = distortedSizing;

        Assert.Multiple(() =>
        {
            Assert.That(plan.ModuleScale, Is.EqualTo(4));
            Assert.That(plan.Size.WidthPixels, Is.EqualTo(24));
            Assert.That(plan.Size.HeightPixels, Is.EqualTo(16));
            Assert.That(
                () => RenderPlanner.CreatePlan(
                    CreateRequest(symbol, fractional)),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => RenderPlanner.CreatePlan(
                    CreateRequest(symbol, distorted)),
                Throws.TypeOf<BarcodeRenderingException>());
        });
    }

    [Test]
    public void LinearPlanSeparatesModuleScaleFromRequestedBarHeight()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Linear,
            10,
            1,
            CreateQuietZone(1, 1, 1, 1));
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "012345";
        symbol.Payload = payload;
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthPixels = 36;
        sizing.RequestedHeightPixels = 60;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Sizing = sizing;
        options.TextMode = BarcodeTextMode.Standard;

        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        Assert.Multiple(() =>
        {
            Assert.That(plan.ModuleScale, Is.EqualTo(3));
            Assert.That(plan.BarHeightPixels, Is.EqualTo(51));
            Assert.That(plan.TextHeightModules, Is.EqualTo(1));
            Assert.That(plan.TextHeightPixels, Is.EqualTo(3));
            Assert.That(plan.Size.WidthPixels, Is.EqualTo(36));
            Assert.That(plan.Size.HeightPixels, Is.EqualTo(60));
            Assert.That(plan.TextLayout, Is.Not.Null);
            Assert.That(plan.TextLayout!.Runs![0].Text,
                Is.EqualTo("012345"));
            Assert.That(plan.TextLayout.Bounds!.X, Is.EqualTo(1.0));
            Assert.That(plan.TextLayout.Bounds.Y, Is.EqualTo(19.0));
            Assert.That(plan.TextLayout.Bounds.Height, Is.EqualTo(1.0));
        });
    }

    [Test]
    public void DpiRoundsToIntegralModulesAndReportsActualPhysicalSize()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Linear,
            10,
            1,
            CreateQuietZone(1, 1, 1, 1));
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.ModuleSizeMillimetres = 0.25;
        sizing.BarHeightMillimetres = 5.0;
        sizing.DotsPerInch = 300.0;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Sizing = sizing;

        BarcodeRenderOptions conflictingScale = new BarcodeRenderOptions();
        conflictingScale.Sizing = sizing;
        conflictingScale.ModuleScale = 2;

        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        Assert.Multiple(() =>
        {
            Assert.That(plan.ModuleScale, Is.EqualTo(3));
            Assert.That(plan.BarHeightPixels, Is.EqualTo(59));
            Assert.That(plan.Size.WidthPixels, Is.EqualTo(36));
            Assert.That(plan.Size.HeightPixels, Is.EqualTo(65));
            Assert.That(plan.Size.ModuleSizeMillimetres,
                Is.EqualTo(0.254).Within(0.0000001));
            Assert.That(plan.Size.WidthMillimetres,
                Is.EqualTo(3.048).Within(0.0000001));
            Assert.That(plan.Size.HeightMillimetres,
                Is.EqualTo(65.0 * 25.4 / 300.0).Within(0.0000001));
            Assert.That(plan.Size.DotsPerInch, Is.EqualTo(300.0));
            Assert.That(
                () => RenderPlanner.CreatePlan(
                    CreateRequest(symbol, conflictingScale)),
                Throws.TypeOf<BarcodeRenderingException>());
        });
    }

    [Test]
    public void RotatedPhysicalConstraintsResolveInOutputOrientation()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            4,
            2,
            CreateQuietZone(1, 1, 1, 1));
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthMillimetres = 8.0;
        sizing.RequestedHeightMillimetres = 12.0;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Sizing = sizing;
        options.Rotation = BarcodeRotation.Clockwise270;

        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        BarcodeSizing conflictingSizing = new BarcodeSizing();
        conflictingSizing.RequestedWidthMillimetres = 8.0;
        conflictingSizing.RequestedHeightMillimetres = 18.0;
        BarcodeRenderOptions conflicting = new BarcodeRenderOptions();
        conflicting.Sizing = conflictingSizing;
        conflicting.Rotation = BarcodeRotation.Clockwise270;

        Assert.Multiple(() =>
        {
            Assert.That(plan.Size.WidthModules, Is.EqualTo(4));
            Assert.That(plan.Size.HeightModules, Is.EqualTo(6));
            Assert.That(plan.Size.WidthMillimetres, Is.EqualTo(8.0));
            Assert.That(plan.Size.HeightMillimetres, Is.EqualTo(12.0));
            Assert.That(plan.Size.ModuleSizeMillimetres, Is.EqualTo(2.0));
            Assert.That(
                () => RenderPlanner.CreatePlan(
                    CreateRequest(symbol, conflicting)),
                Throws.TypeOf<BarcodeRenderingException>());
        });
    }

    [Test]
    public void ExistingTextHeightIsReservedAtTheResolvedScale()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Linear,
            8,
            1,
            CreateQuietZone(1, 2, 1, 3));
        BarcodeTextRun encodedRun = new BarcodeTextRun();
        encodedRun.Text = "EAN";
        BarcodeTextLayout encodedLayout = new BarcodeTextLayout();
        encodedLayout.Runs = new BarcodeTextRun[] { encodedRun };
        encodedLayout.ReservedHeight = 7.0;
        symbol.TextLayout = encodedLayout;
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.ModuleScale = 2;
        options.TextMode = BarcodeTextMode.Standard;

        RenderPlan plan = RenderPlanner.CreatePlan(
            CreateRequest(symbol, options));

        Assert.Multiple(() =>
        {
            Assert.That(plan.TextHeightModules, Is.EqualTo(7));
            Assert.That(plan.TextHeightPixels, Is.EqualTo(14));
            Assert.That(plan.UnrotatedHeightModules, Is.EqualTo(13));
            Assert.That(plan.UnrotatedHeightPixels, Is.EqualTo(26));
            Assert.That(plan.TextLayout!.ReservedHeight, Is.EqualTo(7.0));
            Assert.That(plan.TextLayout.Bounds!.Y, Is.EqualTo(6.0));
            Assert.That(plan.TextLayout.Bounds.Height, Is.EqualTo(7.0));
        });
    }

    [Test]
    public void CustomMinimumAndInvalidInputsAreRejected()
    {
        BarcodeSymbol symbol = CreateSymbol(
            BarcodeDimension.Matrix,
            4,
            2,
            CreateQuietZone(2, 2, 2, 2));
        BarcodeRenderOptions tooSmall = new BarcodeRenderOptions();
        tooSmall.QuietZonePolicy =
            BarcodeQuietZonePolicy.CustomAtLeastStandard;
        tooSmall.QuietZone = CreateQuietZone(1, 2, 2, 2);

        BarcodeRenderOptions missingCustom = new BarcodeRenderOptions();
        missingCustom.QuietZonePolicy =
            BarcodeQuietZonePolicy.ExactCustom;

        BarcodeRenderOptions invalidScale = new BarcodeRenderOptions();
        invalidScale.ModuleScale = -1;

        BarcodeSymbol badBuffer = CreateSymbol(
            BarcodeDimension.Matrix,
            4,
            2,
            CreateQuietZone(1, 1, 1, 1));
        badBuffer.Modules = new byte[7];

        Assert.Multiple(() =>
        {
            Assert.That(
                () => RenderPlanner.CreatePlan(
                    CreateRequest(symbol, tooSmall)),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => RenderPlanner.CreatePlan(
                    CreateRequest(symbol, missingCustom)),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => RenderPlanner.CreatePlan(
                    CreateRequest(symbol, invalidScale)),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => RenderPlanner.CreatePlan(
                    CreateRequest(badBuffer, new BarcodeRenderOptions())),
                Throws.TypeOf<BarcodeRenderingException>());
            Assert.That(
                () => RenderPlanner.CreatePlan(null),
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
        BarcodeQuietZone quietZone)
    {
        BarcodeSymbol symbol = new BarcodeSymbol();
        symbol.Dimension = dimension;
        symbol.WidthModules = width;
        symbol.HeightModules = height;
        symbol.Modules = new byte[width * height];
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
}
