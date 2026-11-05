namespace barcode;

internal class RenderPlan
{
    internal BarcodeSymbol Symbol = new BarcodeSymbol();
    internal BarcodeRotation Rotation;
    internal int ModuleScale;
    internal int BarHeightPixels;
    internal int TextHeightModules;
    internal int TextHeightPixels;
    internal int TextTopModules;
    internal int TextBottomModules;
    internal int TextTopPixels;
    internal int TextBottomPixels;
    internal int ContentXModules;
    internal int ContentYModules;
    internal int ContentXPixels;
    internal int ContentYPixels;
    internal int UnrotatedWidthModules;
    internal int UnrotatedHeightModules;
    internal int UnrotatedWidthPixels;
    internal int UnrotatedHeightPixels;
    internal BarcodeQuietZone QuietZone = new BarcodeQuietZone();
    internal BarcodeSize Size = new BarcodeSize();
    internal BarcodeTextLayout? TextLayout;
}

internal static class RenderPlanner
{
    internal const double MillimetresPerInch = 25.4;
    internal const double MeasurementTolerance = 0.000001;

    internal static RenderPlan CreatePlan(BarcodeRenderRequest? request)
    {
        if (request == null || request.Symbol == null)
        {
            throw new BarcodeRenderingException(
                "A barcode symbol is required to create a render plan.");
        }

        BarcodeSymbol symbol = request.Symbol;
        BarcodeRenderOptions options = request.Options == null
            ? new BarcodeRenderOptions()
            : request.Options;
        BarcodeSizing sizing = options.Sizing == null
            ? new BarcodeSizing()
            : options.Sizing;
        ValidateInputs(symbol, options, sizing);

        BarcodeQuietZone quietZone = ResolveQuietZone(
            symbol.RequiredQuietZone,
            options,
            sizing);
        string? text = SelectText(symbol, options);
        bool preserveSourceTextLayout = ShouldPreserveSourceTextLayout(
            symbol,
            options,
            text);
        int textHeightModules = ResolveTextHeightModules(symbol, text);
        int textTopModules = ResolveTextTopModules(
            symbol,
            preserveSourceTextLayout,
            textHeightModules);
        int textBottomModules = textHeightModules - textTopModules;
        int unrotatedWidthModules = AddIntegers(
            AddIntegers(symbol.WidthModules, quietZone.LeftModules),
            quietZone.RightModules);
        int unrotatedHeightModules = AddIntegers(
            AddIntegers(
                AddIntegers(symbol.HeightModules, quietZone.TopModules),
                quietZone.BottomModules),
            textHeightModules);

        int requestedUnrotatedWidthPixels = ResolveUnrotatedWidthInteger(
            sizing.RequestedWidthPixels,
            sizing.RequestedHeightPixels,
            options.Rotation);
        int requestedUnrotatedHeightPixels = ResolveUnrotatedHeightInteger(
            sizing.RequestedWidthPixels,
            sizing.RequestedHeightPixels,
            options.Rotation);
        double requestedUnrotatedWidthMillimetres =
            ResolveUnrotatedWidthMeasurement(
                sizing.RequestedWidthMillimetres,
                sizing.RequestedHeightMillimetres,
                options.Rotation);
        double requestedUnrotatedHeightMillimetres =
            ResolveUnrotatedHeightMeasurement(
                sizing.RequestedWidthMillimetres,
                sizing.RequestedHeightMillimetres,
                options.Rotation);

        double desiredModuleSize = ResolveDesiredModuleSize(
            symbol,
            sizing,
            unrotatedWidthModules,
            unrotatedHeightModules,
            requestedUnrotatedWidthMillimetres,
            requestedUnrotatedHeightMillimetres,
            textHeightModules,
            quietZone);
        int moduleScale = ResolveModuleScale(
            symbol,
            options,
            sizing,
            quietZone,
            textHeightModules,
            requestedUnrotatedWidthPixels,
            requestedUnrotatedHeightPixels,
            desiredModuleSize);
        BarcodeQuietZone sizedQuietZone = ResolvePixelQuietZone(
            quietZone,
            symbol.RequiredQuietZone,
            options.QuietZonePolicy,
            moduleScale);
        int textHeightPixels = MultiplyIntegers(
            textHeightModules,
            moduleScale);
        int textTopPixels = MultiplyIntegers(
            textTopModules,
            moduleScale);
        int textBottomPixels = textHeightPixels - textTopPixels;
        int barHeightPixels = ResolveBarHeightPixels(
            symbol,
            sizing,
            sizedQuietZone,
            textHeightPixels,
            requestedUnrotatedHeightPixels,
            requestedUnrotatedHeightMillimetres,
            desiredModuleSize,
            moduleScale);
        int unrotatedWidthPixels = AddIntegers(
            AddIntegers(
                MultiplyIntegers(symbol.WidthModules, moduleScale),
                sizedQuietZone.LeftPixels),
            sizedQuietZone.RightPixels);
        int unrotatedHeightPixels = AddIntegers(
            AddIntegers(
                AddIntegers(
                    barHeightPixels,
                    sizedQuietZone.TopPixels),
                sizedQuietZone.BottomPixels),
            textHeightPixels);
        ValidateResolvedPixelRequests(
            unrotatedWidthPixels,
            unrotatedHeightPixels,
            requestedUnrotatedWidthPixels,
            requestedUnrotatedHeightPixels);

        double actualModuleSize = sizing.DotsPerInch > 0.0
            ? ((double)moduleScale * MillimetresPerInch)
                / sizing.DotsPerInch
            : desiredModuleSize;
        double unrotatedWidthMillimetres = ResolvePhysicalWidth(
            unrotatedWidthPixels,
            moduleScale,
            actualModuleSize,
            sizing.DotsPerInch);
        double unrotatedHeightMillimetres = ResolvePhysicalHeight(
            symbol,
            sizing,
            unrotatedHeightPixels,
            barHeightPixels,
            moduleScale,
            actualModuleSize,
            sizedQuietZone,
            textHeightPixels,
            requestedUnrotatedHeightMillimetres);

        BarcodeSize size = CreateOutputSize(
            unrotatedWidthModules,
            unrotatedHeightModules,
            unrotatedWidthPixels,
            unrotatedHeightPixels,
            unrotatedWidthMillimetres,
            unrotatedHeightMillimetres,
            sizing.DotsPerInch,
            actualModuleSize,
            sizedQuietZone,
            options.Rotation);
        BarcodeTextLayout? textLayout = CreateTextLayout(
            text,
            preserveSourceTextLayout,
            symbol,
            sizedQuietZone,
            moduleScale,
            barHeightPixels,
            textTopModules,
            textBottomModules,
            textTopPixels,
            textBottomPixels,
            unrotatedWidthModules,
            unrotatedHeightModules,
            unrotatedWidthPixels,
            unrotatedHeightPixels,
            unrotatedWidthMillimetres,
            unrotatedHeightMillimetres,
            sizing.DotsPerInch,
            actualModuleSize);

        RenderPlan plan = new RenderPlan();
        plan.Symbol = symbol;
        plan.Rotation = options.Rotation;
        plan.ModuleScale = moduleScale;
        plan.BarHeightPixels = barHeightPixels;
        plan.TextHeightModules = textHeightModules;
        plan.TextHeightPixels = textHeightPixels;
        plan.TextTopModules = textTopModules;
        plan.TextBottomModules = textBottomModules;
        plan.TextTopPixels = textTopPixels;
        plan.TextBottomPixels = textBottomPixels;
        plan.ContentXModules = quietZone.LeftModules;
        plan.ContentYModules = AddIntegers(
            textTopModules,
            quietZone.TopModules);
        plan.ContentXPixels = sizedQuietZone.LeftPixels;
        plan.ContentYPixels = AddIntegers(
            textTopPixels,
            sizedQuietZone.TopPixels);
        plan.UnrotatedWidthModules = unrotatedWidthModules;
        plan.UnrotatedHeightModules = unrotatedHeightModules;
        plan.UnrotatedWidthPixels = unrotatedWidthPixels;
        plan.UnrotatedHeightPixels = unrotatedHeightPixels;
        plan.QuietZone = sizedQuietZone;
        plan.Size = size;
        plan.TextLayout = textLayout;
        return plan;
    }

    static void ValidateInputs(
        BarcodeSymbol symbol,
        BarcodeRenderOptions options,
        BarcodeSizing sizing)
    {
        if (symbol.WidthModules <= 0 || symbol.HeightModules <= 0)
        {
            throw new BarcodeRenderingException(
                "The barcode symbol must have positive module dimensions.");
        }

        if (symbol.Modules == null
            || (long)symbol.WidthModules * (long)symbol.HeightModules
                != symbol.Modules.Length)
        {
            throw new BarcodeRenderingException(
                "The barcode symbol module buffer does not match its dimensions.");
        }

        int rotationValue = (int)options.Rotation;
        if (rotationValue < (int)BarcodeRotation.None
            || rotationValue > (int)BarcodeRotation.Clockwise270)
        {
            throw new BarcodeRenderingException(
                "The requested barcode rotation is invalid.");
        }

        int quietPolicyValue = (int)options.QuietZonePolicy;
        if (quietPolicyValue < (int)BarcodeQuietZonePolicy.Standard
            || quietPolicyValue > (int)BarcodeQuietZonePolicy.ExactCustom)
        {
            throw new BarcodeRenderingException(
                "The requested quiet-zone policy is invalid.");
        }

        int textModeValue = (int)options.TextMode;
        if (textModeValue < (int)BarcodeTextMode.Hidden
            || textModeValue > (int)BarcodeTextMode.Custom)
        {
            throw new BarcodeRenderingException(
                "The requested human-readable text mode is invalid.");
        }

        if (options.ModuleScale < 0
            || sizing.RequestedWidthModules < 0
            || sizing.RequestedHeightModules < 0
            || sizing.RequestedWidthPixels < 0
            || sizing.RequestedHeightPixels < 0)
        {
            throw new BarcodeRenderingException(
                "Render sizing values must not be negative.");
        }

        if (sizing.RequestedWidthModules != 0
            && sizing.RequestedWidthModules != symbol.WidthModules)
        {
            throw new BarcodeRenderingException(
                "The requested module width does not match the encoded symbol.");
        }

        if (sizing.RequestedHeightModules != 0
            && sizing.RequestedHeightModules != symbol.HeightModules)
        {
            throw new BarcodeRenderingException(
                "The requested module height does not match the encoded symbol.");
        }

        ValidateMeasurement(
            sizing.RequestedWidthMillimetres,
            "The requested render width must be finite and non-negative.");
        ValidateMeasurement(
            sizing.RequestedHeightMillimetres,
            "The requested render height must be finite and non-negative.");
        ValidateMeasurement(
            sizing.DotsPerInch,
            "The render resolution must be finite and non-negative.");
        ValidateMeasurement(
            sizing.ModuleSizeMillimetres,
            "The requested module size must be finite and non-negative.");
        ValidateMeasurement(
            sizing.BarHeightMillimetres,
            "The requested bar height must be finite and non-negative.");
        ValidateMeasurement(
            sizing.TargetAspectRatio,
            "The requested render aspect ratio must be finite and non-negative.");
        if (symbol.Dimension != BarcodeDimension.Linear
            && sizing.BarHeightMillimetres > 0.0)
        {
            throw new BarcodeRenderingException(
                "A bar-height measurement applies only to linear symbols.");
        }
    }

    static BarcodeQuietZone ResolveQuietZone(
        BarcodeQuietZone? requiredSource,
        BarcodeRenderOptions options,
        BarcodeSizing sizing)
    {
        BarcodeQuietZone required = requiredSource == null
            ? new BarcodeQuietZone()
            : CopyQuietZone(requiredSource);
        ValidateQuietZone(required);
        if (options.QuietZonePolicy == BarcodeQuietZonePolicy.Standard)
        {
            return required;
        }

        BarcodeQuietZone? customSource = options.QuietZone;
        if (customSource == null)
        {
            customSource = sizing.QuietZone;
        }

        if (customSource == null)
        {
            throw new BarcodeRenderingException(
                "The selected quiet-zone policy requires custom dimensions.");
        }

        BarcodeQuietZone custom = CopyQuietZone(customSource);
        ValidateQuietZone(custom);
        if (options.QuietZonePolicy
                == BarcodeQuietZonePolicy.CustomAtLeastStandard
            && (custom.LeftModules < required.LeftModules
                || custom.TopModules < required.TopModules
                || custom.RightModules < required.RightModules
                || custom.BottomModules < required.BottomModules))
        {
            throw new BarcodeRenderingException(
                "Custom quiet zones must meet the symbol's standard minimums.");
        }

        return custom;
    }

    static double ResolveDesiredModuleSize(
        BarcodeSymbol symbol,
        BarcodeSizing sizing,
        int totalWidthModules,
        int totalHeightModules,
        double requestedWidthMillimetres,
        double requestedHeightMillimetres,
        int textHeightModules,
        BarcodeQuietZone quietZone)
    {
        double moduleSize = sizing.ModuleSizeMillimetres;
        if (requestedWidthMillimetres > 0.0)
        {
            moduleSize = MergeMeasurement(
                moduleSize,
                requestedWidthMillimetres / (double)totalWidthModules,
                "The requested physical width conflicts with the module size.");
        }

        if (symbol.Dimension != BarcodeDimension.Linear
            && requestedHeightMillimetres > 0.0)
        {
            moduleSize = MergeMeasurement(
                moduleSize,
                requestedHeightMillimetres / (double)totalHeightModules,
                "The requested physical height would distort the modules.");
        }

        if (symbol.Dimension == BarcodeDimension.Linear
            && requestedHeightMillimetres > 0.0
            && sizing.BarHeightMillimetres > 0.0)
        {
            double nonBarHeight = (double)(quietZone.TopModules
                + quietZone.BottomModules
                + textHeightModules) * moduleSize;
            if (moduleSize > 0.0
                && global::System.Math.Abs(
                    nonBarHeight
                        + sizing.BarHeightMillimetres
                        - requestedHeightMillimetres)
                    > MeasurementTolerance)
            {
                throw new BarcodeRenderingException(
                    "The requested physical height conflicts with the bar height and module size.");
            }
        }

        return moduleSize;
    }

    static int ResolveModuleScale(
        BarcodeSymbol symbol,
        BarcodeRenderOptions options,
        BarcodeSizing sizing,
        BarcodeQuietZone quietZone,
        int textHeightModules,
        int requestedWidthPixels,
        int requestedHeightPixels,
        double desiredModuleSize)
    {
        int scale = options.ModuleScale;
        int widthFixedPixels = AddIntegers(
            quietZone.LeftPixels,
            quietZone.RightPixels);
        int widthUnits = symbol.WidthModules;
        if (quietZone.LeftPixels == 0)
        {
            widthUnits = AddIntegers(widthUnits, quietZone.LeftModules);
        }

        if (quietZone.RightPixels == 0)
        {
            widthUnits = AddIntegers(widthUnits, quietZone.RightModules);
        }

        if (requestedWidthPixels > 0)
        {
            int widthScale = ResolveScaleFromPixels(
                requestedWidthPixels,
                widthFixedPixels,
                widthUnits,
                "The requested pixel width is not an integral module scale.");
            scale = MergeScale(scale, widthScale);
        }

        if (symbol.Dimension != BarcodeDimension.Linear
            && requestedHeightPixels > 0)
        {
            int heightFixedPixels = AddIntegers(
                quietZone.TopPixels,
                quietZone.BottomPixels);
            int heightUnits = AddIntegers(
                symbol.HeightModules,
                textHeightModules);
            if (quietZone.TopPixels == 0)
            {
                heightUnits = AddIntegers(
                    heightUnits,
                    quietZone.TopModules);
            }

            if (quietZone.BottomPixels == 0)
            {
                heightUnits = AddIntegers(
                    heightUnits,
                    quietZone.BottomModules);
            }

            int heightScale = ResolveScaleFromPixels(
                requestedHeightPixels,
                heightFixedPixels,
                heightUnits,
                "The requested pixel height is not an integral module scale.");
            scale = MergeScale(scale, heightScale);
        }

        if (desiredModuleSize > 0.0
            && sizing.DotsPerInch > 0.0)
        {
            double requestedScale = desiredModuleSize
                * sizing.DotsPerInch
                / MillimetresPerInch;
            int physicalScale = RoundPositiveScale(requestedScale);
            if (scale == 0)
            {
                scale = physicalScale;
            }
            else if (scale != physicalScale)
            {
                throw new BarcodeRenderingException(
                    "The module scale conflicts with the requested physical size and resolution.");
            }
        }

        if (scale == 0)
        {
            scale = 1;
        }

        return scale;
    }

    static int ResolveBarHeightPixels(
        BarcodeSymbol symbol,
        BarcodeSizing sizing,
        BarcodeQuietZone quietZone,
        int textHeightPixels,
        int requestedHeightPixels,
        double requestedHeightMillimetres,
        double desiredModuleSize,
        int moduleScale)
    {
        if (symbol.Dimension != BarcodeDimension.Linear)
        {
            return MultiplyIntegers(symbol.HeightModules, moduleScale);
        }

        int nonBarHeight = AddIntegers(
            AddIntegers(
                quietZone.TopPixels,
                quietZone.BottomPixels),
            textHeightPixels);
        if (requestedHeightPixels > 0)
        {
            int result = requestedHeightPixels - nonBarHeight;
            if (result <= 0)
            {
                throw new BarcodeRenderingException(
                    "The requested pixel height does not leave a positive bar height.");
            }

            if (sizing.BarHeightMillimetres > 0.0
                && sizing.DotsPerInch > 0.0
                && result != RoundPositivePixels(
                    sizing.BarHeightMillimetres
                        * sizing.DotsPerInch
                        / MillimetresPerInch))
            {
                throw new BarcodeRenderingException(
                    "The requested pixel height conflicts with the physical bar height.");
            }

            if (requestedHeightMillimetres > 0.0
                && sizing.DotsPerInch > 0.0
                && requestedHeightPixels != RoundPositivePixels(
                    requestedHeightMillimetres
                        * sizing.DotsPerInch
                        / MillimetresPerInch))
            {
                throw new BarcodeRenderingException(
                    "The requested pixel and physical heights conflict at the selected resolution.");
            }

            return result;
        }

        if (sizing.BarHeightMillimetres > 0.0
            && sizing.DotsPerInch > 0.0)
        {
            return RoundPositivePixels(
                sizing.BarHeightMillimetres
                    * sizing.DotsPerInch
                    / MillimetresPerInch);
        }

        if (requestedHeightMillimetres > 0.0
            && sizing.DotsPerInch > 0.0)
        {
            int requestedTotal = RoundPositivePixels(
                requestedHeightMillimetres
                    * sizing.DotsPerInch
                    / MillimetresPerInch);
            int result = requestedTotal - nonBarHeight;
            if (result <= 0)
            {
                throw new BarcodeRenderingException(
                    "The requested physical height does not leave a positive bar height.");
            }

            return result;
        }

        if (sizing.BarHeightMillimetres > 0.0
            && desiredModuleSize > 0.0)
        {
            return RoundPositivePixels(
                sizing.BarHeightMillimetres
                    * (double)moduleScale
                    / desiredModuleSize);
        }

        return MultiplyIntegers(symbol.HeightModules, moduleScale);
    }

    static BarcodeQuietZone ResolvePixelQuietZone(
        BarcodeQuietZone selected,
        BarcodeQuietZone? required,
        BarcodeQuietZonePolicy policy,
        int moduleScale)
    {
        BarcodeQuietZone result = CopyQuietZone(selected);
        result.LeftPixels = ResolveQuietPixels(
            selected.LeftPixels,
            selected.LeftModules,
            moduleScale);
        result.TopPixels = ResolveQuietPixels(
            selected.TopPixels,
            selected.TopModules,
            moduleScale);
        result.RightPixels = ResolveQuietPixels(
            selected.RightPixels,
            selected.RightModules,
            moduleScale);
        result.BottomPixels = ResolveQuietPixels(
            selected.BottomPixels,
            selected.BottomModules,
            moduleScale);
        if (policy == BarcodeQuietZonePolicy.CustomAtLeastStandard
            && required != null)
        {
            if (result.LeftPixels
                    < ResolveMinimumQuietPixels(
                        required.LeftPixels,
                        required.LeftModules,
                        moduleScale)
                || result.TopPixels
                    < ResolveMinimumQuietPixels(
                        required.TopPixels,
                        required.TopModules,
                        moduleScale)
                || result.RightPixels
                    < ResolveMinimumQuietPixels(
                        required.RightPixels,
                        required.RightModules,
                        moduleScale)
                || result.BottomPixels
                    < ResolveMinimumQuietPixels(
                        required.BottomPixels,
                        required.BottomModules,
                        moduleScale))
            {
                throw new BarcodeRenderingException(
                    "Custom pixel quiet zones must meet the symbol's standard minimums.");
            }
        }

        return result;
    }

    static BarcodeSize CreateOutputSize(
        int unrotatedWidthModules,
        int unrotatedHeightModules,
        int unrotatedWidthPixels,
        int unrotatedHeightPixels,
        double unrotatedWidthMillimetres,
        double unrotatedHeightMillimetres,
        double dotsPerInch,
        double moduleSizeMillimetres,
        BarcodeQuietZone quietZone,
        BarcodeRotation rotation)
    {
        bool swap = RotationSwapsAxes(rotation);
        BarcodeSize size = new BarcodeSize();
        size.WidthModules = swap
            ? unrotatedHeightModules
            : unrotatedWidthModules;
        size.HeightModules = swap
            ? unrotatedWidthModules
            : unrotatedHeightModules;
        size.WidthPixels = swap
            ? unrotatedHeightPixels
            : unrotatedWidthPixels;
        size.HeightPixels = swap
            ? unrotatedWidthPixels
            : unrotatedHeightPixels;
        size.WidthMillimetres = swap
            ? unrotatedHeightMillimetres
            : unrotatedWidthMillimetres;
        size.HeightMillimetres = swap
            ? unrotatedWidthMillimetres
            : unrotatedHeightMillimetres;
        size.DotsPerInch = dotsPerInch;
        size.ModuleSizeMillimetres = moduleSizeMillimetres;
        size.QuietZone = RotateQuietZone(quietZone, rotation);
        return size;
    }

    static BarcodeTextLayout? CreateTextLayout(
        string? text,
        bool preserveSourceLayout,
        BarcodeSymbol symbol,
        BarcodeQuietZone pixelQuietZone,
        int moduleScale,
        int barHeightPixels,
        int textTopModules,
        int textBottomModules,
        int textTopPixels,
        int textBottomPixels,
        int totalWidthModules,
        int totalHeightModules,
        int totalWidthPixels,
        int totalHeightPixels,
        double widthMillimetres,
        double heightMillimetres,
        double dotsPerInch,
        double moduleSizeMillimetres)
    {
        if (text == null)
        {
            return null;
        }

        BarcodeSize size = CreateTextOutputSize(
            totalWidthModules,
            totalHeightModules,
            totalWidthPixels,
            totalHeightPixels,
            widthMillimetres,
            heightMillimetres,
            dotsPerInch,
            moduleSizeMillimetres,
            pixelQuietZone);
        if (preserveSourceLayout)
        {
            return CreatePreservedTextLayout(
                symbol,
                pixelQuietZone,
                moduleScale,
                barHeightPixels,
                textTopModules,
                textTopPixels,
                size);
        }

        int baseBottomPixels = AddIntegers(
            AddIntegers(
                AddIntegers(textTopPixels, pixelQuietZone.TopPixels),
                barHeightPixels),
            pixelQuietZone.BottomPixels);
        if (textBottomPixels <= 0)
        {
            throw new BarcodeRenderingException(
                "Human-readable text requires positive space below the symbol.");
        }

        BarcodeTextRun run = new BarcodeTextRun();
        run.Text = text;
        run.X = (double)pixelQuietZone.LeftPixels / moduleScale;
        run.BaselineY = (double)AddIntegers(
            baseBottomPixels,
            textBottomPixels) / moduleScale;
        run.Width = symbol.WidthModules;
        run.Height = (double)textBottomPixels / moduleScale;

        BarcodeRectangle bounds = new BarcodeRectangle();
        bounds.X = run.X;
        bounds.Y = (double)baseBottomPixels / moduleScale;
        bounds.Width = symbol.WidthModules;
        bounds.Height = run.Height;

        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.Runs = new BarcodeTextRun[] { run };
        layout.ReservedHeight = textTopModules + textBottomModules;
        layout.Bounds = bounds;
        layout.Size = size;
        return layout;
    }

    static BarcodeSize CreateTextOutputSize(
        int totalWidthModules,
        int totalHeightModules,
        int totalWidthPixels,
        int totalHeightPixels,
        double widthMillimetres,
        double heightMillimetres,
        double dotsPerInch,
        double moduleSizeMillimetres,
        BarcodeQuietZone pixelQuietZone)
    {
        BarcodeSize size = new BarcodeSize();
        size.WidthModules = totalWidthModules;
        size.HeightModules = totalHeightModules;
        size.WidthPixels = totalWidthPixels;
        size.HeightPixels = totalHeightPixels;
        size.WidthMillimetres = widthMillimetres;
        size.HeightMillimetres = heightMillimetres;
        size.DotsPerInch = dotsPerInch;
        size.ModuleSizeMillimetres = moduleSizeMillimetres;
        size.QuietZone = CopyQuietZone(pixelQuietZone);
        return size;
    }

    static BarcodeTextLayout CreatePreservedTextLayout(
        BarcodeSymbol symbol,
        BarcodeQuietZone pixelQuietZone,
        int moduleScale,
        int barHeightPixels,
        int textTopModules,
        int textTopPixels,
        BarcodeSize size)
    {
        BarcodeTextLayout? sourceValue = symbol.TextLayout;
        if (sourceValue == null || sourceValue.Runs == null)
        {
            throw new BarcodeRenderingException(
                "The source human-readable text layout is missing.");
        }

        BarcodeTextLayout source = sourceValue;
        BarcodeQuietZone sourceQuietZone = ResolveSourceTextQuietZone(
            symbol,
            source);
        int sourceBaseHeight = ResolveSourceBaseHeight(
            symbol,
            sourceQuietZone);
        int renderBaseHeightPixels = AddIntegers(
            AddIntegers(pixelQuietZone.TopPixels, barHeightPixels),
            pixelQuietZone.BottomPixels);
        BarcodeTextRun[] runs = new BarcodeTextRun[source.Runs.Length];
        int index = 0;
        while (index < source.Runs.Length)
        {
            BarcodeTextRun sourceRun = source.Runs[index];
            ValidateSourceTextRun(sourceRun);
            double sourceTop = sourceRun.BaselineY - sourceRun.Height;
            double topPixels = MapSourceTextYToPixels(
                sourceTop,
                sourceBaseHeight,
                textTopModules,
                textTopPixels,
                renderBaseHeightPixels,
                moduleScale);
            double baselinePixels = MapSourceTextYToPixels(
                sourceRun.BaselineY,
                sourceBaseHeight,
                textTopModules,
                textTopPixels,
                renderBaseHeightPixels,
                moduleScale);
            BarcodeTextRun run = new BarcodeTextRun();
            run.Text = sourceRun.Text;
            run.X = ((double)pixelQuietZone.LeftPixels
                + ((sourceRun.X - sourceQuietZone.LeftModules)
                    * moduleScale)) / moduleScale;
            run.BaselineY = baselinePixels / moduleScale;
            run.Width = sourceRun.Width;
            run.Height = (baselinePixels - topPixels) / moduleScale;
            runs[index] = run;
            index = index + 1;
        }

        BarcodeRectangle[] extensions = CreatePlannedBarExtensions(
            source.BarExtensions,
            sourceQuietZone,
            pixelQuietZone,
            moduleScale,
            barHeightPixels,
            textTopPixels);
        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.Runs = runs;
        layout.BarExtensions = extensions;
        layout.ReservedHeight = source.ReservedHeight;
        layout.Bounds = FindTextBounds(runs);
        layout.Size = size;
        return layout;
    }

    static BarcodeQuietZone ResolveSourceTextQuietZone(
        BarcodeSymbol symbol,
        BarcodeTextLayout layout)
    {
        if (layout.Size != null && layout.Size.QuietZone != null)
        {
            return CopyQuietZone(layout.Size.QuietZone);
        }

        if (symbol.Size != null && symbol.Size.QuietZone != null)
        {
            return CopyQuietZone(symbol.Size.QuietZone);
        }

        return symbol.RequiredQuietZone == null
            ? new BarcodeQuietZone()
            : CopyQuietZone(symbol.RequiredQuietZone);
    }

    static int ResolveSourceBaseHeight(
        BarcodeSymbol symbol,
        BarcodeQuietZone quietZone)
    {
        if (symbol.Size != null && symbol.Size.HeightModules > 0)
        {
            return symbol.Size.HeightModules;
        }

        return AddIntegers(
            AddIntegers(symbol.HeightModules, quietZone.TopModules),
            quietZone.BottomModules);
    }

    static double MapSourceTextYToPixels(
        double value,
        int sourceBaseHeight,
        int textTopModules,
        int textTopPixels,
        int renderBaseHeightPixels,
        int moduleScale)
    {
        if (value <= 0.0)
        {
            return (value + textTopModules) * moduleScale;
        }

        if (value >= sourceBaseHeight)
        {
            return textTopPixels
                + renderBaseHeightPixels
                + ((value - sourceBaseHeight) * moduleScale);
        }

        return textTopPixels
            + ((value / sourceBaseHeight) * renderBaseHeightPixels);
    }

    static BarcodeRectangle[] CreatePlannedBarExtensions(
        BarcodeRectangle[]? source,
        BarcodeQuietZone sourceQuietZone,
        BarcodeQuietZone pixelQuietZone,
        int moduleScale,
        int barHeightPixels,
        int textTopPixels)
    {
        if (source == null)
        {
            return new BarcodeRectangle[0];
        }

        BarcodeRectangle[] result = new BarcodeRectangle[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            BarcodeRectangle extension = source[index];
            ValidateSourceBarExtension(extension);
            BarcodeRectangle planned = new BarcodeRectangle();
            planned.X = ((double)pixelQuietZone.LeftPixels
                + ((extension.X - sourceQuietZone.LeftModules)
                    * moduleScale)) / moduleScale;
            planned.Y = (double)AddIntegers(
                AddIntegers(textTopPixels, pixelQuietZone.TopPixels),
                barHeightPixels) / moduleScale;
            planned.Width = extension.Width;
            planned.Height = extension.Height;
            result[index] = planned;
            index = index + 1;
        }

        return result;
    }

    static BarcodeRectangle FindTextBounds(BarcodeTextRun[] runs)
    {
        BarcodeRectangle bounds = new BarcodeRectangle();
        if (runs.Length == 0)
        {
            return bounds;
        }

        double left = runs[0].X;
        double top = runs[0].BaselineY - runs[0].Height;
        double right = runs[0].X + runs[0].Width;
        double bottom = runs[0].BaselineY;
        int index = 1;
        while (index < runs.Length)
        {
            BarcodeTextRun run = runs[index];
            double runTop = run.BaselineY - run.Height;
            if (run.X < left)
            {
                left = run.X;
            }

            if (runTop < top)
            {
                top = runTop;
            }

            if (run.X + run.Width > right)
            {
                right = run.X + run.Width;
            }

            if (run.BaselineY > bottom)
            {
                bottom = run.BaselineY;
            }

            index = index + 1;
        }

        bounds.X = left;
        bounds.Y = top;
        bounds.Width = right - left;
        bounds.Height = bottom - top;
        return bounds;
    }

    static void ValidateSourceTextRun(BarcodeTextRun run)
    {
        if (run == null
            || double.IsNaN(run.X)
            || double.IsInfinity(run.X)
            || double.IsNaN(run.BaselineY)
            || double.IsInfinity(run.BaselineY)
            || double.IsNaN(run.Width)
            || double.IsInfinity(run.Width)
            || double.IsNaN(run.Height)
            || double.IsInfinity(run.Height)
            || run.Width < 0.0
            || run.Height <= 0.0)
        {
            throw new BarcodeRenderingException(
                "The source human-readable text layout is invalid.");
        }
    }

    static void ValidateSourceBarExtension(BarcodeRectangle extension)
    {
        if (extension == null
            || double.IsNaN(extension.X)
            || double.IsInfinity(extension.X)
            || double.IsNaN(extension.Width)
            || double.IsInfinity(extension.Width)
            || double.IsNaN(extension.Height)
            || double.IsInfinity(extension.Height)
            || extension.Width <= 0.0
            || extension.Height <= 0.0)
        {
            throw new BarcodeRenderingException(
                "A source bar extension is invalid.");
        }
    }

    static string? SelectText(
        BarcodeSymbol symbol,
        BarcodeRenderOptions options)
    {
        switch (options.TextMode)
        {
            case BarcodeTextMode.Hidden:
                return null;
            case BarcodeTextMode.Standard:
                string? standard = ReadExistingText(symbol.TextLayout);
                if (standard != null)
                {
                    return standard;
                }

                return ReadPayloadText(symbol.Payload, false);
            case BarcodeTextMode.RawPayload:
                return ReadPayloadText(symbol.Payload, true);
            case BarcodeTextMode.Custom:
                if (options.CustomText == null)
                {
                    throw new BarcodeRenderingException(
                        "Custom human-readable text must not be null.");
                }

                return options.CustomText;
            default:
                return null;
        }
    }

    static string? ReadExistingText(BarcodeTextLayout? layout)
    {
        if (layout == null || layout.Runs == null
            || layout.Runs.Length == 0)
        {
            return null;
        }

        global::System.Text.StringBuilder text =
            new global::System.Text.StringBuilder();
        int index = 0;
        while (index < layout.Runs.Length)
        {
            BarcodeTextRun run = layout.Runs[index];
            if (run != null && run.Text != null)
            {
                text.Append(run.Text);
            }

            index = index + 1;
        }

        return text.Length == 0 ? null : text.ToString();
    }

    static string? ReadPayloadText(
        BarcodePayload? payload,
        bool preferRawBytes)
    {
        if (payload == null)
        {
            return null;
        }

        if (preferRawBytes && payload.RawBytes != null)
        {
            return ConvertBytesToText(payload.RawBytes);
        }

        if (payload.Text != null)
        {
            return payload.Text;
        }

        return payload.RawBytes == null
            ? null
            : ConvertBytesToText(payload.RawBytes);
    }

    static int ResolveTextHeightModules(
        BarcodeSymbol symbol,
        string? text)
    {
        if (text == null)
        {
            return 0;
        }

        double height = 1.0;
        if (symbol.TextLayout != null
            && symbol.TextLayout.ReservedHeight > 0.0)
        {
            height = symbol.TextLayout.ReservedHeight;
        }

        if (double.IsNaN(height) || double.IsInfinity(height))
        {
            throw new BarcodeRenderingException(
                "The human-readable text height must be finite.");
        }

        double rounded = global::System.Math.Ceiling(height);
        if (rounded > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The human-readable text height exceeds the supported range.");
        }

        return (int)rounded;
    }

    static bool ShouldPreserveSourceTextLayout(
        BarcodeSymbol symbol,
        BarcodeRenderOptions options,
        string? text)
    {
        return text != null
            && options.TextMode == BarcodeTextMode.Standard
            && symbol.TextLayout != null
            && symbol.TextLayout.Runs != null
            && symbol.TextLayout.Runs.Length != 0
            && HasPositionedSourceTextLayout(symbol.TextLayout);
    }

    static bool HasPositionedSourceTextLayout(BarcodeTextLayout layout)
    {
        if (layout.Size == null
            || layout.Bounds == null
            || layout.Runs == null)
        {
            return false;
        }

        int index = 0;
        while (index < layout.Runs.Length)
        {
            BarcodeTextRun run = layout.Runs[index];
            if (run == null
                || double.IsNaN(run.X)
                || double.IsInfinity(run.X)
                || double.IsNaN(run.BaselineY)
                || double.IsInfinity(run.BaselineY)
                || double.IsNaN(run.Width)
                || double.IsInfinity(run.Width)
                || double.IsNaN(run.Height)
                || double.IsInfinity(run.Height)
                || run.Width < 0.0
                || run.Height <= 0.0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int ResolveTextTopModules(
        BarcodeSymbol symbol,
        bool preserveSourceLayout,
        int totalTextHeight)
    {
        if (!preserveSourceLayout
            || symbol.TextLayout == null
            || symbol.TextLayout.Bounds == null
            || symbol.TextLayout.Bounds.Y >= 0.0)
        {
            return 0;
        }

        double value = global::System.Math.Ceiling(
            -symbol.TextLayout.Bounds.Y);
        if (double.IsNaN(value)
            || double.IsInfinity(value)
            || value > totalTextHeight)
        {
            throw new BarcodeRenderingException(
                "The source text area extends beyond its reserved height.");
        }

        return (int)value;
    }

    static double ResolvePhysicalWidth(
        int widthPixels,
        int moduleScale,
        double moduleSize,
        double dotsPerInch)
    {
        if (dotsPerInch > 0.0)
        {
            return (double)widthPixels
                * MillimetresPerInch
                / dotsPerInch;
        }

        if (moduleSize == 0.0)
        {
            return 0.0;
        }

        return ((double)widthPixels / (double)moduleScale)
            * moduleSize;
    }

    static double ResolvePhysicalHeight(
        BarcodeSymbol symbol,
        BarcodeSizing sizing,
        int heightPixels,
        int barHeightPixels,
        int moduleScale,
        double moduleSize,
        BarcodeQuietZone quietZone,
        int textHeightPixels,
        double requestedHeightMillimetres)
    {
        if (sizing.DotsPerInch > 0.0)
        {
            return (double)heightPixels
                * MillimetresPerInch
                / sizing.DotsPerInch;
        }

        if (symbol.Dimension != BarcodeDimension.Linear)
        {
            if (moduleSize == 0.0)
            {
                return 0.0;
            }

            return ((double)heightPixels / (double)moduleScale)
                * moduleSize;
        }

        if (requestedHeightMillimetres > 0.0)
        {
            return requestedHeightMillimetres;
        }

        if (moduleSize == 0.0)
        {
            if (sizing.BarHeightMillimetres > 0.0
                && quietZone.TopPixels == 0
                && quietZone.BottomPixels == 0
                && textHeightPixels == 0)
            {
                return sizing.BarHeightMillimetres;
            }

            return 0.0;
        }

        double barHeight = sizing.BarHeightMillimetres;
        if (barHeight == 0.0)
        {
            barHeight = ((double)barHeightPixels / (double)moduleScale)
                * moduleSize;
        }

        double externalUnits = (double)(quietZone.TopPixels
            + quietZone.BottomPixels
            + textHeightPixels) / (double)moduleScale;
        return barHeight + (externalUnits * moduleSize);
    }

    static void ValidateResolvedPixelRequests(
        int actualWidth,
        int actualHeight,
        int requestedWidth,
        int requestedHeight)
    {
        if ((requestedWidth > 0 && actualWidth != requestedWidth)
            || (requestedHeight > 0 && actualHeight != requestedHeight))
        {
            throw new BarcodeRenderingException(
                "The requested pixel dimensions conflict with the integral render scale.");
        }
    }

    static int ResolveScaleFromPixels(
        int requestedPixels,
        int fixedPixels,
        int scalableUnits,
        string message)
    {
        int scalablePixels = requestedPixels - fixedPixels;
        if (scalablePixels <= 0
            || scalableUnits <= 0
            || scalablePixels % scalableUnits != 0)
        {
            throw new BarcodeRenderingException(message);
        }

        int scale = scalablePixels / scalableUnits;
        if (scale <= 0)
        {
            throw new BarcodeRenderingException(message);
        }

        return scale;
    }

    static int MergeScale(int current, int candidate)
    {
        if (current == 0)
        {
            return candidate;
        }

        if (current != candidate)
        {
            throw new BarcodeRenderingException(
                "The requested pixel dimensions would distort the modules.");
        }

        return current;
    }

    static double MergeMeasurement(
        double current,
        double candidate,
        string message)
    {
        if (current == 0.0)
        {
            return candidate;
        }

        if (global::System.Math.Abs(current - candidate)
            > MeasurementTolerance)
        {
            throw new BarcodeRenderingException(message);
        }

        return current;
    }

    static int RoundPositiveScale(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)
            || value > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The resolved module scale exceeds the supported range.");
        }

        int rounded = (int)global::System.Math.Floor(value + 0.5);
        return rounded < 1 ? 1 : rounded;
    }

    static int RoundPositivePixels(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)
            || value > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The resolved pixel dimension exceeds the supported range.");
        }

        int rounded = (int)global::System.Math.Floor(value + 0.5);
        return rounded < 1 ? 1 : rounded;
    }

    static int ResolveQuietPixels(
        int requestedPixels,
        int modules,
        int moduleScale)
    {
        return requestedPixels > 0
            ? requestedPixels
            : MultiplyIntegers(modules, moduleScale);
    }

    static int ResolveMinimumQuietPixels(
        int requiredPixels,
        int requiredModules,
        int moduleScale)
    {
        int modulePixels = MultiplyIntegers(
            requiredModules,
            moduleScale);
        return requiredPixels > modulePixels
            ? requiredPixels
            : modulePixels;
    }

    static BarcodeQuietZone RotateQuietZone(
        BarcodeQuietZone source,
        BarcodeRotation rotation)
    {
        BarcodeQuietZone result = new BarcodeQuietZone();
        switch (rotation)
        {
            case BarcodeRotation.None:
                CopyQuietZoneSides(result, source, 0, 1, 2, 3);
                break;
            case BarcodeRotation.Clockwise90:
                CopyQuietZoneSides(result, source, 3, 0, 1, 2);
                break;
            case BarcodeRotation.Clockwise180:
                CopyQuietZoneSides(result, source, 2, 3, 0, 1);
                break;
            case BarcodeRotation.Clockwise270:
                CopyQuietZoneSides(result, source, 1, 2, 3, 0);
                break;
            default:
                break;
        }

        return result;
    }

    static void CopyQuietZoneSides(
        BarcodeQuietZone target,
        BarcodeQuietZone source,
        int leftSource,
        int topSource,
        int rightSource,
        int bottomSource)
    {
        target.LeftModules = ReadModuleSide(source, leftSource);
        target.TopModules = ReadModuleSide(source, topSource);
        target.RightModules = ReadModuleSide(source, rightSource);
        target.BottomModules = ReadModuleSide(source, bottomSource);
        target.LeftPixels = ReadPixelSide(source, leftSource);
        target.TopPixels = ReadPixelSide(source, topSource);
        target.RightPixels = ReadPixelSide(source, rightSource);
        target.BottomPixels = ReadPixelSide(source, bottomSource);
    }

    static int ReadModuleSide(BarcodeQuietZone source, int side)
    {
        switch (side)
        {
            case 0:
                return source.LeftModules;
            case 1:
                return source.TopModules;
            case 2:
                return source.RightModules;
            case 3:
                return source.BottomModules;
            default:
                return 0;
        }
    }

    static int ReadPixelSide(BarcodeQuietZone source, int side)
    {
        switch (side)
        {
            case 0:
                return source.LeftPixels;
            case 1:
                return source.TopPixels;
            case 2:
                return source.RightPixels;
            case 3:
                return source.BottomPixels;
            default:
                return 0;
        }
    }

    static bool RotationSwapsAxes(BarcodeRotation rotation)
    {
        return rotation == BarcodeRotation.Clockwise90
            || rotation == BarcodeRotation.Clockwise270;
    }

    static int ResolveUnrotatedWidthInteger(
        int requestedWidth,
        int requestedHeight,
        BarcodeRotation rotation)
    {
        return RotationSwapsAxes(rotation)
            ? requestedHeight
            : requestedWidth;
    }

    static int ResolveUnrotatedHeightInteger(
        int requestedWidth,
        int requestedHeight,
        BarcodeRotation rotation)
    {
        return RotationSwapsAxes(rotation)
            ? requestedWidth
            : requestedHeight;
    }

    static double ResolveUnrotatedWidthMeasurement(
        double requestedWidth,
        double requestedHeight,
        BarcodeRotation rotation)
    {
        return RotationSwapsAxes(rotation)
            ? requestedHeight
            : requestedWidth;
    }

    static double ResolveUnrotatedHeightMeasurement(
        double requestedWidth,
        double requestedHeight,
        BarcodeRotation rotation)
    {
        return RotationSwapsAxes(rotation)
            ? requestedWidth
            : requestedHeight;
    }

    static BarcodeQuietZone CopyQuietZone(BarcodeQuietZone source)
    {
        BarcodeQuietZone copy = new BarcodeQuietZone();
        copy.LeftModules = source.LeftModules;
        copy.TopModules = source.TopModules;
        copy.RightModules = source.RightModules;
        copy.BottomModules = source.BottomModules;
        copy.LeftPixels = source.LeftPixels;
        copy.TopPixels = source.TopPixels;
        copy.RightPixels = source.RightPixels;
        copy.BottomPixels = source.BottomPixels;
        return copy;
    }

    static void ValidateQuietZone(BarcodeQuietZone quietZone)
    {
        if (quietZone.LeftModules < 0
            || quietZone.TopModules < 0
            || quietZone.RightModules < 0
            || quietZone.BottomModules < 0
            || quietZone.LeftPixels < 0
            || quietZone.TopPixels < 0
            || quietZone.RightPixels < 0
            || quietZone.BottomPixels < 0)
        {
            throw new BarcodeRenderingException(
                "Quiet-zone dimensions must not be negative.");
        }
    }

    static void ValidateMeasurement(double value, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
        {
            throw new BarcodeRenderingException(message);
        }
    }

    static string ConvertBytesToText(byte[] source)
    {
        char[] characters = new char[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            characters[index] = (char)source[index];
            index = index + 1;
        }

        return new string(characters);
    }

    static int AddIntegers(int first, int second)
    {
        long result = (long)first + (long)second;
        if (result > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The resolved render dimensions exceed the supported range.");
        }

        return (int)result;
    }

    static int MultiplyIntegers(int first, int second)
    {
        long result = (long)first * (long)second;
        if (result > int.MaxValue)
        {
            throw new BarcodeRenderingException(
                "The resolved render dimensions exceed the supported range.");
        }

        return (int)result;
    }
}
