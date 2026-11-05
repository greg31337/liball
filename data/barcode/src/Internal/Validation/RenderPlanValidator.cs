namespace barcode;

internal class RenderPlanValidationState
{
    internal BarcodeValidationMode Mode;
    internal string Standard = "Barcode rendering";
    internal string? Profile;
    internal bool CanCreatePlan = true;
    internal global::System.Collections.Generic.List<BarcodeValidationIssue> Issues;
    internal global::System.Collections.Generic.List<BarcodeQualityMetric> Metrics;

    internal RenderPlanValidationState(BarcodeValidationMode mode)
    {
        Mode = mode;
        Issues = new global::System.Collections.Generic.List<BarcodeValidationIssue>();
        Metrics = new global::System.Collections.Generic.List<BarcodeQualityMetric>();
    }
}

internal static class RenderPlanValidator
{
    internal const double MinimumContrast = 0.5;
    internal const double RatioTolerance = 0.01;
    internal const int MaximumOutputBytes = 67108864;

    internal static BarcodeValidationReport ValidateRenderRequest(
        BarcodeValidationRequest request)
    {
        if (request == null)
        {
            throw new BarcodeValidationException(
                "A barcode render validation request is required.");
        }

        if (request.Mode != BarcodeValidationMode.Strict &&
            request.Mode != BarcodeValidationMode.Tolerant)
        {
            throw new BarcodeValidationException(
                "Barcode validation mode is invalid.");
        }

        RenderPlanValidationState state = new RenderPlanValidationState(request.Mode);
        BarcodeRenderRequest? render = request.Render;
        if (render == null)
        {
            AddRenderIssue(
                state,
                "RENDER_REQUEST_MISSING",
                "A render request is required for render-plan validation.",
                "Render",
                "a barcode render request",
                "null");
            return CreateRenderReport(state, request.Mode);
        }

        BarcodeSymbol? symbol = render.Symbol;
        if (symbol == null)
        {
            AddRenderIssue(
                state,
                "RENDER_SYMBOL_MISSING",
                "A logical barcode symbol is required for render-plan validation.",
                "Render.Symbol",
                "an encoded logical symbol",
                "null");
            return CreateRenderReport(state, request.Mode);
        }

        MergeLogicalValidation(state, request, symbol);
        BarcodeRenderOptions options = render.Options == null
            ? new BarcodeRenderOptions()
            : render.Options;
        BarcodeSizing sizing = options.Sizing == null
            ? new BarcodeSizing()
            : options.Sizing;
        ValidateRenderEnums(state, options);
        ValidateRenderSizing(state, symbol, options, sizing);
        ValidateRenderQuietZones(state, symbol, options, sizing);
        ValidateSourceTextLayout(state, symbol, options);
        ValidateRenderColors(state, options);
        ValidateOutputSettings(state, options);
        if (state.CanCreatePlan)
        {
            ValidatePlanCompatibility(state, symbol, options, sizing);
        }

        if (state.CanCreatePlan)
        {
            RenderPlan plan = RenderPlanner.CreatePlan(render);
            ValidateResolvedPlan(state, symbol, options, sizing, plan);
        }

        return CreateRenderReport(state, request.Mode);
    }

    static void MergeLogicalValidation(
        RenderPlanValidationState state,
        BarcodeValidationRequest request,
        BarcodeSymbol symbol)
    {
        BarcodeValidationRequest logicalRequest = new BarcodeValidationRequest();
        logicalRequest.Format = request.Format;
        logicalRequest.Symbol = symbol;
        logicalRequest.Mode = request.Mode;
        logicalRequest.ValidateGs1Profile = request.ValidateGs1Profile;
        BarcodeValidationReport logical =
            LogicalSymbolValidator.ValidateSymbolRequest(logicalRequest);
        if (logical.Standard != null)
        {
            state.Standard = logical.Standard;
        }

        state.Profile = logical.Profile;
        BarcodeValidationIssue[] issues = logical.Issues == null
            ? new BarcodeValidationIssue[0]
            : logical.Issues;
        int index = 0;
        while (index < issues.Length)
        {
            state.Issues.Add(ValidationIssueFactory.CopyWithOffset(
                issues[index],
                issues[index].Offset));
            state.CanCreatePlan = false;
            index = index + 1;
        }
    }

    static void ValidateRenderEnums(
        RenderPlanValidationState state,
        BarcodeRenderOptions options)
    {
        int format = (int)options.Format;
        if (format < (int)BarcodeRenderFormat.Raw1 ||
            format > (int)BarcodeRenderFormat.Svg)
        {
            AddRenderIssue(
                state,
                "RENDER_FORMAT_INVALID",
                "The requested output format is invalid.",
                "Render.Options.Format",
                "Raw1 through Svg",
                format.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        int rotation = (int)options.Rotation;
        if (rotation < (int)BarcodeRotation.None ||
            rotation > (int)BarcodeRotation.Clockwise270)
        {
            AddRenderIssue(
                state,
                "RENDER_ROTATION_INVALID",
                "The requested output rotation is invalid.",
                "Render.Options.Rotation",
                "None through Clockwise270",
                rotation.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        int policy = (int)options.QuietZonePolicy;
        if (policy < (int)BarcodeQuietZonePolicy.Standard ||
            policy > (int)BarcodeQuietZonePolicy.ExactCustom)
        {
            AddRenderIssue(
                state,
                "RENDER_QUIET_ZONE_POLICY_INVALID",
                "The requested quiet-zone policy is invalid.",
                "Render.Options.QuietZonePolicy",
                "Standard through ExactCustom",
                policy.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        int textMode = (int)options.TextMode;
        if (textMode < (int)BarcodeTextMode.Hidden ||
            textMode > (int)BarcodeTextMode.Custom)
        {
            AddRenderIssue(
                state,
                "RENDER_TEXT_MODE_INVALID",
                "The requested human-readable text mode is invalid.",
                "Render.Options.TextMode",
                "Hidden through Custom",
                textMode.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (options.TextMode == BarcodeTextMode.Custom &&
            options.CustomText == null)
        {
            AddRenderIssue(
                state,
                "RENDER_CUSTOM_TEXT_MISSING",
                "Custom human-readable text must be supplied when custom text is selected.",
                "Render.Options.CustomText",
                "non-null custom text",
                "null");
        }
    }

    static void ValidateRenderSizing(
        RenderPlanValidationState state,
        BarcodeSymbol symbol,
        BarcodeRenderOptions options,
        BarcodeSizing sizing)
    {
        if (options.ModuleScale < 0 ||
            sizing.RequestedWidthModules < 0 ||
            sizing.RequestedHeightModules < 0 ||
            sizing.RequestedWidthPixels < 0 ||
            sizing.RequestedHeightPixels < 0)
        {
            AddRenderIssue(
                state,
                "RENDER_SIZE_NEGATIVE",
                "Render module and pixel sizing values must not be negative.",
                "Render.Options.Sizing",
                "non-negative sizes",
                "a negative size");
        }

        if (sizing.RequestedWidthModules != 0 &&
            sizing.RequestedWidthModules != symbol.WidthModules)
        {
            AddRenderIssue(
                state,
                "RENDER_MODULE_WIDTH_CONFLICT",
                "The requested module width does not match the encoded symbol.",
                "Render.Options.Sizing.RequestedWidthModules",
                symbol.WidthModules.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                sizing.RequestedWidthModules.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (sizing.RequestedHeightModules != 0 &&
            sizing.RequestedHeightModules != symbol.HeightModules)
        {
            AddRenderIssue(
                state,
                "RENDER_MODULE_HEIGHT_CONFLICT",
                "The requested module height does not match the encoded symbol.",
                "Render.Options.Sizing.RequestedHeightModules",
                symbol.HeightModules.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                sizing.RequestedHeightModules.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        ValidateRenderMeasurement(
            state,
            sizing.RequestedWidthMillimetres,
            "RENDER_WIDTH_MEASUREMENT_INVALID",
            "Render.Options.Sizing.RequestedWidthMillimetres");
        ValidateRenderMeasurement(
            state,
            sizing.RequestedHeightMillimetres,
            "RENDER_HEIGHT_MEASUREMENT_INVALID",
            "Render.Options.Sizing.RequestedHeightMillimetres");
        ValidateRenderMeasurement(
            state,
            sizing.DotsPerInch,
            "RENDER_DPI_INVALID",
            "Render.Options.Sizing.DotsPerInch");
        ValidateRenderMeasurement(
            state,
            sizing.ModuleSizeMillimetres,
            "RENDER_MODULE_SIZE_INVALID",
            "Render.Options.Sizing.ModuleSizeMillimetres");
        ValidateRenderMeasurement(
            state,
            sizing.BarHeightMillimetres,
            "RENDER_BAR_HEIGHT_INVALID",
            "Render.Options.Sizing.BarHeightMillimetres");
        ValidateRenderMeasurement(
            state,
            sizing.TargetAspectRatio,
            "RENDER_ASPECT_RATIO_INVALID",
            "Render.Options.Sizing.TargetAspectRatio");

        if (symbol.Dimension != BarcodeDimension.Linear &&
            sizing.BarHeightMillimetres > 0.0)
        {
            AddRenderIssue(
                state,
                "RENDER_BAR_HEIGHT_NOT_APPLICABLE",
                "A bar-height measurement applies only to linear symbols.",
                "Render.Options.Sizing.BarHeightMillimetres",
                "zero for non-linear symbols",
                sizing.BarHeightMillimetres.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void ValidateRenderMeasurement(
        RenderPlanValidationState state,
        double value,
        string code,
        string field)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0.0)
        {
            AddRenderIssue(
                state,
                code,
                "The render measurement must be finite and non-negative.",
                field,
                "a finite non-negative measurement",
                value.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void ValidateRenderQuietZones(
        RenderPlanValidationState state,
        BarcodeSymbol symbol,
        BarcodeRenderOptions options,
        BarcodeSizing sizing)
    {
        BarcodeQuietZone? required = symbol.RequiredQuietZone;
        if (required != null)
        {
            ValidateRenderQuietZoneValues(
                state,
                required,
                "Render.Symbol.RequiredQuietZone");
        }

        BarcodeQuietZone? custom = options.QuietZone;
        if (custom == null)
        {
            custom = sizing.QuietZone;
        }

        if (options.QuietZonePolicy != BarcodeQuietZonePolicy.Standard &&
            custom == null)
        {
            AddRenderIssue(
                state,
                "RENDER_CUSTOM_QUIET_ZONE_MISSING",
                "The selected quiet-zone policy requires custom dimensions.",
                "Render.Options.QuietZone",
                "custom quiet-zone dimensions",
                "null");
            return;
        }

        if (custom == null)
        {
            return;
        }

        ValidateRenderQuietZoneValues(state, custom, "Render.Options.QuietZone");
        if (options.QuietZonePolicy == BarcodeQuietZonePolicy.CustomAtLeastStandard &&
            required != null &&
            (custom.LeftModules < required.LeftModules ||
             custom.TopModules < required.TopModules ||
             custom.RightModules < required.RightModules ||
             custom.BottomModules < required.BottomModules ||
             (custom.LeftPixels != 0 && custom.LeftPixels < required.LeftPixels) ||
             (custom.TopPixels != 0 && custom.TopPixels < required.TopPixels) ||
             (custom.RightPixels != 0 && custom.RightPixels < required.RightPixels) ||
             (custom.BottomPixels != 0 && custom.BottomPixels < required.BottomPixels)))
        {
            AddRenderIssue(
                state,
                "RENDER_QUIET_ZONE_INSUFFICIENT",
                "Custom quiet zones must meet the symbol's standard minimums.",
                "Render.Options.QuietZone",
                "at least the required quiet zone",
                "a smaller custom quiet zone");
        }
    }

    static void ValidateRenderQuietZoneValues(
        RenderPlanValidationState state,
        BarcodeQuietZone quiet,
        string field)
    {
        if (quiet.LeftModules < 0 || quiet.TopModules < 0 ||
            quiet.RightModules < 0 || quiet.BottomModules < 0 ||
            quiet.LeftPixels < 0 || quiet.TopPixels < 0 ||
            quiet.RightPixels < 0 || quiet.BottomPixels < 0)
        {
            AddRenderIssue(
                state,
                "RENDER_QUIET_ZONE_NEGATIVE",
                "Quiet-zone dimensions must not be negative.",
                field,
                "non-negative quiet-zone dimensions",
                "a negative quiet-zone dimension");
        }
    }

    static void ValidateSourceTextLayout(
        RenderPlanValidationState state,
        BarcodeSymbol symbol,
        BarcodeRenderOptions options)
    {
        if (options.TextMode != BarcodeTextMode.Standard ||
            symbol.TextLayout == null)
        {
            return;
        }

        BarcodeTextLayout layout = symbol.TextLayout;
        if (double.IsNaN(layout.ReservedHeight) ||
            double.IsInfinity(layout.ReservedHeight) ||
            layout.ReservedHeight < 0.0 ||
            layout.ReservedHeight > int.MaxValue)
        {
            AddRenderIssue(
                state,
                "RENDER_HRT_HEIGHT_INVALID",
                "The human-readable text reservation must be finite and supported.",
                "Render.Symbol.TextLayout.ReservedHeight",
                "a finite supported height",
                layout.ReservedHeight.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        BarcodeTextRun[] runs = layout.Runs == null
            ? new BarcodeTextRun[0]
            : layout.Runs;
        int runIndex = 0;
        while (runIndex < runs.Length)
        {
            BarcodeTextRun run = runs[runIndex];
            if (run == null || !IsFiniteRenderValue(run.X) ||
                !IsFiniteRenderValue(run.BaselineY) ||
                !IsFiniteRenderValue(run.Width) ||
                !IsFiniteRenderValue(run.Height) ||
                run.Width < 0.0 || run.Height <= 0.0)
            {
                AddRenderIssue(
                    state,
                    "RENDER_HRT_RUN_INVALID",
                    "A human-readable text run has invalid geometry.",
                    "Render.Symbol.TextLayout.Runs",
                    "finite non-negative run geometry",
                    run == null ? "null" : runIndex.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            }

            runIndex = runIndex + 1;
        }

        BarcodeRectangle[] extensions = layout.BarExtensions == null
            ? new BarcodeRectangle[0]
            : layout.BarExtensions;
        int extensionIndex = 0;
        while (extensionIndex < extensions.Length)
        {
            BarcodeRectangle extension = extensions[extensionIndex];
            if (extension == null || !IsFiniteRenderValue(extension.X) ||
                !IsFiniteRenderValue(extension.Y) ||
                !IsFiniteRenderValue(extension.Width) ||
                !IsFiniteRenderValue(extension.Height) ||
                extension.Width <= 0.0 || extension.Height <= 0.0)
            {
                AddRenderIssue(
                    state,
                    "RENDER_HRT_BAR_EXTENSION_INVALID",
                    "A human-readable text bar extension has invalid geometry.",
                    "Render.Symbol.TextLayout.BarExtensions",
                    "finite positive extension geometry",
                    extension == null ? "null" : extensionIndex.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            }

            extensionIndex = extensionIndex + 1;
        }

        if (layout.Bounds != null &&
            (!IsFiniteRenderValue(layout.Bounds.X) ||
             !IsFiniteRenderValue(layout.Bounds.Y) ||
             !IsFiniteRenderValue(layout.Bounds.Width) ||
             !IsFiniteRenderValue(layout.Bounds.Height) ||
             layout.Bounds.Width < 0.0 || layout.Bounds.Height < 0.0 ||
             -layout.Bounds.Y > layout.ReservedHeight))
        {
            AddRenderIssue(
                state,
                "RENDER_HRT_BOUNDS_INVALID",
                "The human-readable text bounds exceed the reserved text area.",
                "Render.Symbol.TextLayout.Bounds",
                "finite bounds within the reserved height",
                "invalid bounds");
        }
    }

    static bool IsFiniteRenderValue(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    static void ValidateRenderColors(
        RenderPlanValidationState state,
        BarcodeRenderOptions options)
    {
        uint foreground = options.ForegroundRgba == 0u
            ? RasterCanvas.DefaultForegroundRgba
            : options.ForegroundRgba;
        uint background = options.BackgroundRgba == 0u
            ? RasterCanvas.DefaultBackgroundRgba
            : options.BackgroundRgba;
        int foregroundAlpha = ReadRenderAlpha(foreground);
        int backgroundAlpha = options.TransparentBackground
            ? 0
            : ReadRenderAlpha(background);
        double foregroundLuminance = ResolveRenderForegroundLuminance(
            foreground,
            foregroundAlpha,
            background,
            backgroundAlpha);
        double backgroundLuminance = ResolveRenderBackgroundLuminance(
            background,
            backgroundAlpha);
        double contrast = global::System.Math.Abs(
            backgroundLuminance - foregroundLuminance) / 255.0;
        AddRenderMetric(
            state,
            "foreground_background_contrast",
            contrast,
            MinimumContrast,
            contrast >= MinimumContrast,
            "Normalized luminance separation after alpha compositing.");

        if (foregroundAlpha == 0)
        {
            AddRenderIssue(
                state,
                "RENDER_FOREGROUND_TRANSPARENT",
                "The barcode foreground must remain visible.",
                "Render.Options.ForegroundRgba",
                "a non-transparent foreground",
                foreground.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (foregroundLuminance >= backgroundLuminance)
        {
            AddRenderIssue(
                state,
                "RENDER_COLOR_POLARITY_INVALID",
                "The barcode foreground must be darker than its background.",
                "Render.Options.ForegroundRgbaBackgroundRgba",
                "dark foreground on a lighter background",
                "foreground luminance is not lower");
        }
        else if (contrast < MinimumContrast)
        {
            AddRenderIssue(
                state,
                "RENDER_CONTRAST_INSUFFICIENT",
                "The foreground and background do not have sufficient luminance contrast.",
                "Render.Options.ForegroundRgbaBackgroundRgba",
                MinimumContrast.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                contrast.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static int ReadRenderAlpha(uint rgba)
    {
        return (int)(rgba & 255u);
    }

    static int ReadRenderRed(uint rgba)
    {
        return (int)((rgba >> 24) & 255u);
    }

    static int ReadRenderGreen(uint rgba)
    {
        return (int)((rgba >> 16) & 255u);
    }

    static int ReadRenderBlue(uint rgba)
    {
        return (int)((rgba >> 8) & 255u);
    }

    static double ResolveRenderBackgroundLuminance(
        uint rgba,
        int alpha)
    {
        double red = CompositeRenderChannel(ReadRenderRed(rgba), 255, alpha);
        double green = CompositeRenderChannel(ReadRenderGreen(rgba), 255, alpha);
        double blue = CompositeRenderChannel(ReadRenderBlue(rgba), 255, alpha);
        return (red * 299.0 + green * 587.0 + blue * 114.0) / 1000.0;
    }

    static double ResolveRenderForegroundLuminance(
        uint foreground,
        int foregroundAlpha,
        uint background,
        int backgroundAlpha)
    {
        double backgroundRed = CompositeRenderChannel(
            ReadRenderRed(background), 255, backgroundAlpha);
        double backgroundGreen = CompositeRenderChannel(
            ReadRenderGreen(background), 255, backgroundAlpha);
        double backgroundBlue = CompositeRenderChannel(
            ReadRenderBlue(background), 255, backgroundAlpha);
        double red = CompositeRenderChannel(
            ReadRenderRed(foreground), backgroundRed, foregroundAlpha);
        double green = CompositeRenderChannel(
            ReadRenderGreen(foreground), backgroundGreen, foregroundAlpha);
        double blue = CompositeRenderChannel(
            ReadRenderBlue(foreground), backgroundBlue, foregroundAlpha);
        return (red * 299.0 + green * 587.0 + blue * 114.0) / 1000.0;
    }

    static double CompositeRenderChannel(
        double foreground,
        double background,
        int alpha)
    {
        return ((foreground * alpha) + (background * (255 - alpha))) / 255.0;
    }

    static void ValidateOutputSettings(
        RenderPlanValidationState state,
        BarcodeRenderOptions options)
    {
        if (options.UseSvgText && options.Format != BarcodeRenderFormat.Svg)
        {
            AddRenderIssue(
                state,
                "RENDER_SVG_TEXT_NOT_APPLICABLE",
                "SVG text can only be selected for SVG output.",
                "Render.Options.UseSvgText",
                "false for non-SVG output",
                "true");
        }

        if (options.TransparentBackground &&
            (options.Format == BarcodeRenderFormat.Raw1 ||
             options.Format == BarcodeRenderFormat.Gray8))
        {
            AddRenderIssue(
                state,
                "RENDER_TRANSPARENCY_UNSUPPORTED",
                "The selected raw raster format cannot preserve transparency.",
                "Render.Options.TransparentBackground",
                "false for Raw1 and Gray8 output",
                "true");
        }
    }

    static void ValidatePlanCompatibility(
        RenderPlanValidationState state,
        BarcodeSymbol symbol,
        BarcodeRenderOptions options,
        BarcodeSizing sizing)
    {
        BarcodeQuietZone quiet = ResolveValidationQuietZone(
            symbol,
            options,
            sizing);
        int textHeight = ResolveValidationTextHeight(symbol, options);
        long totalWidthModules = (long)symbol.WidthModules +
            quiet.LeftModules + quiet.RightModules;
        long totalHeightModules = (long)symbol.HeightModules +
            quiet.TopModules + quiet.BottomModules + textHeight;
        if (totalWidthModules <= 0L || totalHeightModules <= 0L ||
            totalWidthModules > int.MaxValue ||
            totalHeightModules > int.MaxValue)
        {
            AddRenderIssue(
                state,
                "RENDER_DIMENSION_CAPACITY_EXCEEDED",
                "The resolved logical render dimensions exceed the supported range.",
                "Render.Options.Sizing",
                "positive Int32 logical dimensions",
                $"{totalWidthModules}x{totalHeightModules}");
            return;
        }

        bool swap = options.Rotation == BarcodeRotation.Clockwise90 ||
            options.Rotation == BarcodeRotation.Clockwise270;
        int requestedWidthPixels = swap
            ? sizing.RequestedHeightPixels
            : sizing.RequestedWidthPixels;
        int requestedHeightPixels = swap
            ? sizing.RequestedWidthPixels
            : sizing.RequestedHeightPixels;
        double requestedWidthMillimetres = swap
            ? sizing.RequestedHeightMillimetres
            : sizing.RequestedWidthMillimetres;
        double requestedHeightMillimetres = swap
            ? sizing.RequestedWidthMillimetres
            : sizing.RequestedHeightMillimetres;
        double moduleSize = sizing.ModuleSizeMillimetres;
        if (requestedWidthMillimetres > 0.0)
        {
            moduleSize = MergeValidationMeasurement(
                state,
                moduleSize,
                requestedWidthMillimetres / totalWidthModules,
                "The requested physical width conflicts with the module size.");
        }

        if (symbol.Dimension != BarcodeDimension.Linear &&
            requestedHeightMillimetres > 0.0)
        {
            moduleSize = MergeValidationMeasurement(
                state,
                moduleSize,
                requestedHeightMillimetres / totalHeightModules,
                "The requested physical height would distort the modules.");
        }

        if (!state.CanCreatePlan)
        {
            return;
        }

        if (symbol.Dimension == BarcodeDimension.Linear &&
            requestedHeightMillimetres > 0.0 &&
            sizing.BarHeightMillimetres > 0.0 &&
            moduleSize > 0.0)
        {
            double nonBarHeight = (quiet.TopModules +
                quiet.BottomModules + textHeight) * moduleSize;
            if (global::System.Math.Abs(
                nonBarHeight + sizing.BarHeightMillimetres -
                    requestedHeightMillimetres) > RenderPlanner.MeasurementTolerance)
            {
                AddRenderIssue(
                    state,
                    "RENDER_PHYSICAL_HEIGHT_CONFLICT",
                    "The requested physical height conflicts with the bar height and module size.",
                    "Render.Options.Sizing.RequestedHeightMillimetres",
                    "consistent physical height constraints",
                    requestedHeightMillimetres.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
                return;
            }
        }

        int scale = options.ModuleScale;
        long widthFixedPixels = (long)quiet.LeftPixels + quiet.RightPixels;
        long widthUnits = symbol.WidthModules;
        if (quiet.LeftPixels == 0)
        {
            widthUnits = widthUnits + quiet.LeftModules;
        }

        if (quiet.RightPixels == 0)
        {
            widthUnits = widthUnits + quiet.RightModules;
        }

        if (requestedWidthPixels > 0)
        {
            int widthScale = ResolveValidationScale(
                state,
                requestedWidthPixels,
                widthFixedPixels,
                widthUnits,
                "RENDER_MODULE_ROUNDING_INVALID",
                "The requested pixel width is not an integral module scale.",
                "Render.Options.Sizing.RequestedWidthPixels");
            scale = MergeValidationScale(state, scale, widthScale);
        }

        if (symbol.Dimension != BarcodeDimension.Linear &&
            requestedHeightPixels > 0 && state.CanCreatePlan)
        {
            long heightFixedPixels = (long)quiet.TopPixels +
                quiet.BottomPixels;
            long heightUnits = (long)symbol.HeightModules + textHeight;
            if (quiet.TopPixels == 0)
            {
                heightUnits = heightUnits + quiet.TopModules;
            }

            if (quiet.BottomPixels == 0)
            {
                heightUnits = heightUnits + quiet.BottomModules;
            }

            int heightScale = ResolveValidationScale(
                state,
                requestedHeightPixels,
                heightFixedPixels,
                heightUnits,
                "RENDER_MODULE_ROUNDING_INVALID",
                "The requested pixel height is not an integral module scale.",
                "Render.Options.Sizing.RequestedHeightPixels");
            scale = MergeValidationScale(state, scale, heightScale);
        }

        if (moduleSize > 0.0 && sizing.DotsPerInch > 0.0 &&
            state.CanCreatePlan)
        {
            double requestedScale = moduleSize * sizing.DotsPerInch /
                RenderPlanner.MillimetresPerInch;
            if (!IsFiniteRenderValue(requestedScale) ||
                requestedScale > int.MaxValue)
            {
                AddRenderIssue(
                    state,
                    "RENDER_SCALE_CAPACITY_EXCEEDED",
                    "The resolved physical module scale exceeds the supported range.",
                    "Render.Options.Sizing",
                    "an Int32 module scale",
                    requestedScale.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
                return;
            }

            int physicalScale = (int)global::System.Math.Floor(
                requestedScale + 0.5);
            if (physicalScale < 1)
            {
                physicalScale = 1;
            }

            scale = MergeValidationScale(state, scale, physicalScale);
        }

        if (!state.CanCreatePlan)
        {
            return;
        }

        if (scale == 0)
        {
            scale = 1;
        }

        long leftPixels = quiet.LeftPixels > 0
            ? quiet.LeftPixels
            : (long)quiet.LeftModules * scale;
        long topPixels = quiet.TopPixels > 0
            ? quiet.TopPixels
            : (long)quiet.TopModules * scale;
        long rightPixels = quiet.RightPixels > 0
            ? quiet.RightPixels
            : (long)quiet.RightModules * scale;
        long bottomPixels = quiet.BottomPixels > 0
            ? quiet.BottomPixels
            : (long)quiet.BottomModules * scale;
        long textPixels = (long)textHeight * scale;
        long barPixels = (long)symbol.HeightModules * scale;
        long nonBarPixels = topPixels + bottomPixels + textPixels;
        if (symbol.Dimension == BarcodeDimension.Linear)
        {
            barPixels = ResolveValidationBarHeight(
                state,
                sizing,
                requestedHeightPixels,
                requestedHeightMillimetres,
                moduleSize,
                scale,
                nonBarPixels);
        }

        if (!state.CanCreatePlan)
        {
            return;
        }

        long actualWidth = (long)symbol.WidthModules * scale +
            leftPixels + rightPixels;
        long actualHeight = barPixels + nonBarPixels;
        if ((requestedWidthPixels > 0 && actualWidth != requestedWidthPixels) ||
            (requestedHeightPixels > 0 && actualHeight != requestedHeightPixels))
        {
            AddRenderIssue(
                state,
                "RENDER_PIXEL_SIZE_CONFLICT",
                "The requested pixel dimensions conflict with the integral render scale.",
                "Render.Options.Sizing",
                "pixel dimensions produced by one integral module scale",
                $"{requestedWidthPixels}x{requestedHeightPixels}");
            return;
        }

        if (actualWidth <= 0L || actualHeight <= 0L ||
            actualWidth > int.MaxValue || actualHeight > int.MaxValue)
        {
            AddRenderIssue(
                state,
                "RENDER_DIMENSION_CAPACITY_EXCEEDED",
                "The resolved pixel dimensions exceed the supported range.",
                "Render.Options.Sizing",
                "positive Int32 pixel dimensions",
                $"{actualWidth}x{actualHeight}");
            return;
        }

        long outputWidth = swap ? actualHeight : actualWidth;
        long outputHeight = swap ? actualWidth : actualHeight;
        long estimatedBytes = EstimateRenderBytes(
            options.Format,
            (int)outputWidth,
            (int)outputHeight);
        if (estimatedBytes <= 0L || estimatedBytes > MaximumOutputBytes)
        {
            AddRenderIssue(
                state,
                "RENDER_OUTPUT_CAPACITY_EXCEEDED",
                "The resolved output dimensions exceed the supported render allocation.",
                "Render.Options.Sizing",
                $"at most {MaximumOutputBytes} estimated bytes",
                estimatedBytes.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static BarcodeQuietZone ResolveValidationQuietZone(
        BarcodeSymbol symbol,
        BarcodeRenderOptions options,
        BarcodeSizing sizing)
    {
        if (options.QuietZonePolicy == BarcodeQuietZonePolicy.Standard)
        {
            return symbol.RequiredQuietZone == null
                ? new BarcodeQuietZone()
                : symbol.RequiredQuietZone;
        }

        if (options.QuietZone != null)
        {
            return options.QuietZone;
        }

        return sizing.QuietZone == null
            ? new BarcodeQuietZone()
            : sizing.QuietZone;
    }

    static int ResolveValidationTextHeight(
        BarcodeSymbol symbol,
        BarcodeRenderOptions options)
    {
        bool hasText = options.TextMode == BarcodeTextMode.Custom;
        if (options.TextMode == BarcodeTextMode.Standard &&
            symbol.TextLayout != null && symbol.TextLayout.Runs != null)
        {
            int index = 0;
            while (index < symbol.TextLayout.Runs.Length)
            {
                BarcodeTextRun run = symbol.TextLayout.Runs[index];
                if (run != null && run.Text != null)
                {
                    hasText = true;
                }

                index = index + 1;
            }
        }

        if ((options.TextMode == BarcodeTextMode.Standard ||
             options.TextMode == BarcodeTextMode.RawPayload) &&
            symbol.Payload != null &&
            (symbol.Payload.Text != null || symbol.Payload.RawBytes != null))
        {
            hasText = true;
        }

        if (!hasText)
        {
            return 0;
        }

        double height = 1.0;
        if (symbol.TextLayout != null &&
            symbol.TextLayout.ReservedHeight > 0.0)
        {
            height = symbol.TextLayout.ReservedHeight;
        }

        return (int)global::System.Math.Ceiling(height);
    }

    static double MergeValidationMeasurement(
        RenderPlanValidationState state,
        double current,
        double candidate,
        string message)
    {
        if (current == 0.0)
        {
            return candidate;
        }

        if (global::System.Math.Abs(current - candidate) >
            RenderPlanner.MeasurementTolerance)
        {
            AddRenderIssue(
                state,
                "RENDER_PHYSICAL_SIZE_CONFLICT",
                message,
                "Render.Options.Sizing",
                "consistent physical size constraints",
                candidate.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        return current;
    }

    static int ResolveValidationScale(
        RenderPlanValidationState state,
        int requestedPixels,
        long fixedPixels,
        long scalableUnits,
        string code,
        string message,
        string field)
    {
        long scalablePixels = (long)requestedPixels - fixedPixels;
        if (scalablePixels <= 0L || scalableUnits <= 0L ||
            scalablePixels % scalableUnits != 0L ||
            scalablePixels / scalableUnits > int.MaxValue)
        {
            AddRenderIssue(
                state,
                code,
                message,
                field,
                "a positive integral module scale",
                requestedPixels.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            return 0;
        }

        return (int)(scalablePixels / scalableUnits);
    }

    static int MergeValidationScale(
        RenderPlanValidationState state,
        int current,
        int candidate)
    {
        if (!state.CanCreatePlan || candidate == 0)
        {
            return current;
        }

        if (current == 0)
        {
            return candidate;
        }

        if (current != candidate)
        {
            AddRenderIssue(
                state,
                "RENDER_SCALE_CONFLICT",
                "The render constraints resolve to conflicting module scales.",
                "Render.Options.ModuleScale",
                current.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                candidate.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        return current;
    }

    static long ResolveValidationBarHeight(
        RenderPlanValidationState state,
        BarcodeSizing sizing,
        int requestedHeightPixels,
        double requestedHeightMillimetres,
        double moduleSize,
        int scale,
        long nonBarPixels)
    {
        if (requestedHeightPixels > 0)
        {
            long result = (long)requestedHeightPixels - nonBarPixels;
            if (result <= 0L)
            {
                AddRenderIssue(
                    state,
                    "RENDER_BAR_HEIGHT_INSUFFICIENT",
                    "The requested pixel height does not leave a positive bar height.",
                    "Render.Options.Sizing.RequestedHeightPixels",
                    "space for a positive bar height",
                    requestedHeightPixels.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
                return 0L;
            }

            if (sizing.BarHeightMillimetres > 0.0 &&
                sizing.DotsPerInch > 0.0 &&
                result != RoundValidationPixels(
                    sizing.BarHeightMillimetres * sizing.DotsPerInch /
                        RenderPlanner.MillimetresPerInch))
            {
                AddRenderIssue(
                    state,
                    "RENDER_PHYSICAL_HEIGHT_CONFLICT",
                    "The requested pixel height conflicts with the physical bar height.",
                    "Render.Options.Sizing.BarHeightMillimetres",
                    "a bar height consistent with the pixel height",
                    sizing.BarHeightMillimetres.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            }

            if (requestedHeightMillimetres > 0.0 &&
                sizing.DotsPerInch > 0.0 &&
                requestedHeightPixels != RoundValidationPixels(
                    requestedHeightMillimetres * sizing.DotsPerInch /
                        RenderPlanner.MillimetresPerInch))
            {
                AddRenderIssue(
                    state,
                    "RENDER_PHYSICAL_HEIGHT_CONFLICT",
                    "The requested pixel and physical heights conflict at the selected resolution.",
                    "Render.Options.Sizing.RequestedHeightMillimetres",
                    "a height consistent with the pixel height and DPI",
                    requestedHeightMillimetres.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            }

            return result;
        }

        if (sizing.BarHeightMillimetres > 0.0 &&
            sizing.DotsPerInch > 0.0)
        {
            return RoundValidationPixels(
                sizing.BarHeightMillimetres * sizing.DotsPerInch /
                    RenderPlanner.MillimetresPerInch);
        }

        if (requestedHeightMillimetres > 0.0 &&
            sizing.DotsPerInch > 0.0)
        {
            long total = RoundValidationPixels(
                requestedHeightMillimetres * sizing.DotsPerInch /
                    RenderPlanner.MillimetresPerInch);
            long result = total - nonBarPixels;
            if (result <= 0L)
            {
                AddRenderIssue(
                    state,
                    "RENDER_BAR_HEIGHT_INSUFFICIENT",
                    "The requested physical height does not leave a positive bar height.",
                    "Render.Options.Sizing.RequestedHeightMillimetres",
                    "space for a positive bar height",
                    requestedHeightMillimetres.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            }

            return result;
        }

        if (sizing.BarHeightMillimetres > 0.0 && moduleSize > 0.0)
        {
            return RoundValidationPixels(
                sizing.BarHeightMillimetres * scale / moduleSize);
        }

        return scale;
    }

    static long RoundValidationPixels(double value)
    {
        if (!IsFiniteRenderValue(value) || value > int.MaxValue)
        {
            return (long)int.MaxValue + 1L;
        }

        long rounded = (long)global::System.Math.Floor(value + 0.5);
        return rounded < 1L ? 1L : rounded;
    }

    static void ValidateResolvedPlan(
        RenderPlanValidationState state,
        BarcodeSymbol symbol,
        BarcodeRenderOptions options,
        BarcodeSizing sizing,
        RenderPlan plan)
    {
        AddRenderMetric(
            state,
            "module_scale_pixels",
            plan.ModuleScale,
            1.0,
            plan.ModuleScale >= 1,
            "Integral output pixels selected for each logical module.");
        AddRenderMetric(
            state,
            "output_pixel_count",
            (double)plan.Size.WidthPixels * plan.Size.HeightPixels,
            MaximumOutputBytes,
            EstimateRenderBytes(options.Format, plan.Size.WidthPixels, plan.Size.HeightPixels) <= MaximumOutputBytes,
            "Estimated uncompressed output allocation compared with the validation limit.");
        ValidateResolvedOutputSize(state, options, plan);
        ValidateResolvedPhysicalSize(state, symbol, sizing, plan);
        ValidateResolvedAspectRatio(state, sizing, plan);
        ValidateResolvedTextClearance(state, plan);
    }

    static void ValidateResolvedOutputSize(
        RenderPlanValidationState state,
        BarcodeRenderOptions options,
        RenderPlan plan)
    {
        long bytes = EstimateRenderBytes(
            options.Format,
            plan.Size.WidthPixels,
            plan.Size.HeightPixels);
        if (plan.Size.WidthPixels <= 0 || plan.Size.HeightPixels <= 0 ||
            bytes <= 0L || bytes > MaximumOutputBytes)
        {
            AddRenderIssue(
                state,
                "RENDER_OUTPUT_CAPACITY_EXCEEDED",
                "The resolved output dimensions exceed the supported render allocation.",
                "Render.Options.Sizing",
                $"at most {MaximumOutputBytes} estimated bytes",
                bytes.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static long EstimateRenderBytes(
        BarcodeRenderFormat format,
        int width,
        int height)
    {
        long rowBytes = width;
        if (format == BarcodeRenderFormat.Raw1)
        {
            rowBytes = ((long)width + 7L) / 8L;
        }
        else if (format == BarcodeRenderFormat.Png ||
            format == BarcodeRenderFormat.Bmp)
        {
            rowBytes = (long)width * 4L;
        }

        return rowBytes * height;
    }

    static void ValidateResolvedPhysicalSize(
        RenderPlanValidationState state,
        BarcodeSymbol symbol,
        BarcodeSizing sizing,
        RenderPlan plan)
    {
        double moduleSize = plan.Size.ModuleSizeMillimetres;
        if (moduleSize > 0.0)
        {
            AddRenderMetric(
                state,
                "module_size_millimetres",
                moduleSize,
                0.0,
                true,
                "Resolved physical X dimension.");
        }

        if (!IsFiniteRenderValue(plan.Size.WidthMillimetres) ||
            !IsFiniteRenderValue(plan.Size.HeightMillimetres) ||
            !IsFiniteRenderValue(moduleSize))
        {
            AddRenderIssue(
                state,
                "RENDER_PHYSICAL_SIZE_INVALID",
                "The resolved physical render size must be finite.",
                "Render.Options.Sizing",
                "finite physical dimensions",
                $"{plan.Size.WidthMillimetres}x{plan.Size.HeightMillimetres}");
        }

        double minimumModule = 0.0;
        double maximumModule = 0.0;
        if (symbol.Format == BarcodeFormat.Ean8 ||
            symbol.Format == BarcodeFormat.Ean13 ||
            symbol.Format == BarcodeFormat.UpcA ||
            symbol.Format == BarcodeFormat.UpcE ||
            symbol.Format == BarcodeFormat.EanUpcAddon2 ||
            symbol.Format == BarcodeFormat.EanUpcAddon5)
        {
            minimumModule = EanUpcLayout.MinimumModuleSizeMillimetres;
            maximumModule = EanUpcLayout.MaximumModuleSizeMillimetres;
        }
        else if (symbol.Format == BarcodeFormat.Itf14)
        {
            minimumModule = Itf14Profile.MinimumModuleSizeMillimetres;
            maximumModule = Itf14Profile.MaximumModuleSizeMillimetres;
        }
        else if (symbol.Format == BarcodeFormat.Gs1DataMatrix)
        {
            minimumModule = Gs1DataMatrixCodec.MinimumModuleSizeMillimetres;
            maximumModule = Gs1DataMatrixCodec.MaximumModuleSizeMillimetres;
        }

        if (moduleSize > 0.0 && minimumModule > 0.0 &&
            (moduleSize < minimumModule || moduleSize > maximumModule))
        {
            AddRenderIssue(
                state,
                "RENDER_MODULE_SIZE_OUT_OF_RANGE",
                "The resolved physical module size is outside the format range.",
                "Render.Options.Sizing.ModuleSizeMillimetres",
                $"{minimumModule} through {maximumModule} millimetres",
                moduleSize.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }

        if (symbol.Dimension == BarcodeDimension.Linear &&
            plan.BarHeightPixels <= 0)
        {
            AddRenderIssue(
                state,
                "RENDER_BAR_HEIGHT_INSUFFICIENT",
                "The resolved linear bar height must be positive.",
                "Render.Options.Sizing.BarHeightMillimetres",
                "a positive bar height",
                plan.BarHeightPixels.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }


        bool isEanUpc = symbol.Format == BarcodeFormat.Ean8 ||
            symbol.Format == BarcodeFormat.Ean13 ||
            symbol.Format == BarcodeFormat.UpcA ||
            symbol.Format == BarcodeFormat.UpcE;
        if (isEanUpc && moduleSize > 0.0)
        {
            double nominalHeight = symbol.Format == BarcodeFormat.Ean8
                ? EanUpcLayout.NominalEan8BarHeightMillimetres
                : EanUpcLayout.NominalLongBarHeightMillimetres;
            double minimumHeight = nominalHeight * moduleSize /
                EanUpcLayout.NominalModuleSizeMillimetres;
            double barHeight = (double)plan.BarHeightPixels * moduleSize /
                plan.ModuleScale;
            if (barHeight + RenderPlanner.MeasurementTolerance < minimumHeight)
            {
                AddRenderIssue(
                    state,
                    "RENDER_BAR_HEIGHT_INSUFFICIENT",
                    "The EAN/UPC bar height is below the magnification-adjusted minimum.",
                    "Render.Options.Sizing.BarHeightMillimetres",
                    minimumHeight.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                    barHeight.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        if (symbol.Format == BarcodeFormat.Itf14 && moduleSize > 0.0)
        {
            double barHeight = (double)plan.BarHeightPixels * moduleSize / plan.ModuleScale;
            if (barHeight < Itf14Profile.MinimumBarHeightMillimetres)
            {
                AddRenderIssue(
                    state,
                    "RENDER_BAR_HEIGHT_INSUFFICIENT",
                    "The ITF-14 bar height is below the profile minimum.",
                    "Render.Options.Sizing.BarHeightMillimetres",
                    Itf14Profile.MinimumBarHeightMillimetres.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                    barHeight.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        if (symbol.Format == BarcodeFormat.Gs1128 &&
            plan.Size.WidthMillimetres > Gs1128Codec.MaximumPhysicalWidthMillimetres)
        {
            AddRenderIssue(
                state,
                "RENDER_PHYSICAL_WIDTH_EXCEEDED",
                "The GS1-128 physical width exceeds the profile maximum.",
                "Render.Options.Sizing.RequestedWidthMillimetres",
                Gs1128Codec.MaximumPhysicalWidthMillimetres.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                plan.Size.WidthMillimetres.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void ValidateResolvedAspectRatio(
        RenderPlanValidationState state,
        BarcodeSizing sizing,
        RenderPlan plan)
    {
        if (plan.Size.HeightPixels <= 0)
        {
            return;
        }

        double ratio = (double)plan.Size.WidthPixels / plan.Size.HeightPixels;
        double threshold = sizing.TargetAspectRatio > 0.0
            ? sizing.TargetAspectRatio
            : 0.0;
        bool meets = sizing.TargetAspectRatio == 0.0 ||
            global::System.Math.Abs(ratio - sizing.TargetAspectRatio) <=
                sizing.TargetAspectRatio * RatioTolerance;
        AddRenderMetric(
            state,
            "output_aspect_ratio",
            ratio,
            threshold,
            meets,
            "Resolved output width-to-height ratio.");
        if (!meets)
        {
            AddRenderIssue(
                state,
                "RENDER_ASPECT_RATIO_MISMATCH",
                "The resolved output aspect ratio does not match the requested ratio.",
                "Render.Options.Sizing.TargetAspectRatio",
                sizing.TargetAspectRatio.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                ratio.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void ValidateResolvedTextClearance(
        RenderPlanValidationState state,
        RenderPlan plan)
    {
        BarcodeTextLayout? layout = plan.TextLayout;
        if (layout == null)
        {
            return;
        }

        double width = plan.UnrotatedWidthModules;
        double height = plan.UnrotatedHeightModules;
        BarcodeRectangle? bounds = layout.Bounds;
        if (bounds == null || bounds.X < 0.0 || bounds.Y < 0.0 ||
            bounds.Width < 0.0 || bounds.Height <= 0.0 ||
            bounds.X + bounds.Width > width ||
            bounds.Y + bounds.Height > height)
        {
            AddRenderIssue(
                state,
                "RENDER_HRT_CLEARANCE_INSUFFICIENT",
                "The planned human-readable text does not fit within the reserved output area.",
                "Render.Symbol.TextLayout.Bounds",
                "text bounds inside the output",
                "text bounds outside the output");
        }

        BarcodeRectangle[] extensions = layout.BarExtensions == null
            ? new BarcodeRectangle[0]
            : layout.BarExtensions;
        int index = 0;
        while (index < extensions.Length)
        {
            BarcodeRectangle extension = extensions[index];
            if (extension == null || extension.X < 0.0 ||
                extension.Y < 0.0 || extension.Width <= 0.0 ||
                extension.Height <= 0.0 ||
                extension.X + extension.Width > width ||
                extension.Y + extension.Height > height)
            {
                AddRenderIssue(
                    state,
                    "RENDER_HRT_CLEARANCE_INSUFFICIENT",
                    "A planned bar extension does not fit within the human-readable text area.",
                    "Render.Symbol.TextLayout.BarExtensions",
                    "bar extensions inside the output",
                    index.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            }

            index = index + 1;
        }
    }

    static void AddRenderMetric(
        RenderPlanValidationState state,
        string name,
        double value,
        double threshold,
        bool meetsThreshold,
        string interpretation)
    {
        BarcodeQualityMetric metric = new BarcodeQualityMetric();
        metric.Name = name;
        metric.Value = value;
        metric.Threshold = threshold;
        metric.MeetsThreshold = meetsThreshold;
        metric.IsHeuristic = false;
        metric.Interpretation = interpretation;
        state.Metrics.Add(metric);
    }

    static void AddRenderIssue(
        RenderPlanValidationState state,
        string code,
        string message,
        string field,
        string? expected,
        string? observed)
    {
        string standard = state.Profile == null
            ? state.Standard
            : state.Profile;
        state.Issues.Add(ValidationIssueFactory.Create(
            ValidationModePolicy.ResolveSeverity(state.Mode, code),
            code,
            message,
            standard,
            field,
            -1,
            expected,
            observed));
        state.CanCreatePlan = false;
    }

    static BarcodeValidationReport CreateRenderReport(
        RenderPlanValidationState state,
        BarcodeValidationMode mode)
    {
        BarcodeValidationIssue[] issues =
            new BarcodeValidationIssue[state.Issues.Count];
        int issueIndex = 0;
        while (issueIndex < issues.Length)
        {
            issues[issueIndex] = ValidationIssueFactory.CopyWithOffset(
                state.Issues[issueIndex],
                state.Issues[issueIndex].Offset);
            issueIndex = issueIndex + 1;
        }

        BarcodeQualityMetric[] metrics =
            new BarcodeQualityMetric[state.Metrics.Count];
        int metricIndex = 0;
        while (metricIndex < metrics.Length)
        {
            BarcodeQualityMetric source = state.Metrics[metricIndex];
            BarcodeQualityMetric copy = new BarcodeQualityMetric();
            copy.Name = source.Name;
            copy.Value = source.Value;
            copy.Threshold = source.Threshold;
            copy.MeetsThreshold = source.MeetsThreshold;
            copy.IsHeuristic = source.IsHeuristic;
            copy.Interpretation = source.Interpretation;
            metrics[metricIndex] = copy;
            metricIndex = metricIndex + 1;
        }

        BarcodeValidationReport report = new BarcodeValidationReport();
        report.IsConformant = ValidationModePolicy.IsConformant(issues);
        report.Mode = mode;
        report.Standard = state.Standard;
        report.Profile = state.Profile;
        report.Issues = issues;
        report.Metrics = metrics;
        report.Diagnostics = new BarcodeDiagnostic[0];
        return report;
    }
}
