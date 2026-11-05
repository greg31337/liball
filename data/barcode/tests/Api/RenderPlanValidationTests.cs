namespace barcode.Tests;

public class RenderPlanValidationTests
{
    [Test]
    public void ConformantRenderPlanReportsResolvedSizingAndContrastMetrics()
    {
        BarcodeRenderRequest render = CreateRenderRequest();
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.ModuleSizeMillimetres = 0.254;
        sizing.BarHeightMillimetres = 5.0;
        sizing.DotsPerInch = 300.0;
        render.Options!.Sizing = sizing;

        BarcodeValidationReport report = Validate(render);
        string[] names = report.Metrics!
            .Select(metric => metric.Name!).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsConformant, Is.True, JoinCodes(report));
            Assert.That(report.Issues, Is.Empty);
            Assert.That(report.Standard, Is.EqualTo(BarcodeStandards.Code128));
            Assert.That(names, Is.EquivalentTo(new string[]
            {
                "foreground_background_contrast",
                "module_scale_pixels",
                "output_pixel_count",
                "module_size_millimetres",
                "output_aspect_ratio"
            }));
            Assert.That(report.Metrics, Has.All.Matches<BarcodeQualityMetric>(
                metric => !metric.IsHeuristic &&
                    metric.Interpretation != null &&
                    metric.Interpretation.Length > 0));
            Assert.That(report.Metrics,
                Has.One.Matches<BarcodeQualityMetric>(
                    metric => metric.Name == "module_scale_pixels" &&
                        metric.Value == 3.0 && metric.MeetsThreshold));
            Assert.That(report.Metrics,
                Has.One.Matches<BarcodeQualityMetric>(
                    metric => metric.Name == "foreground_background_contrast" &&
                        metric.Value == 1.0 && metric.MeetsThreshold));
        });
    }

    [Test]
    public void FractionalAndConflictingScalesProduceIssuesWithoutThrowing()
    {
        BarcodeRenderRequest fractional = CreateRenderRequest();
        BarcodeSizing fractionalSizing = new BarcodeSizing();
        fractionalSizing.RequestedWidthPixels = 100;
        fractional.Options!.Sizing = fractionalSizing;

        BarcodeRenderRequest conflicting = CreateRenderRequest();
        conflicting.Options!.ModuleScale = 2;
        BarcodeSizing conflictingSizing = new BarcodeSizing();
        conflictingSizing.ModuleSizeMillimetres = 0.254;
        conflictingSizing.DotsPerInch = 300.0;
        conflicting.Options.Sizing = conflictingSizing;

        BarcodeValidationReport fractionalReport = Validate(fractional);
        BarcodeValidationReport conflictingReport = Validate(conflicting);

        Assert.Multiple(() =>
        {
            Assert.That(fractionalReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_MODULE_ROUNDING_INVALID"));
            Assert.That(conflictingReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_SCALE_CONFLICT"));
        });
    }

    [Test]
    public void PhysicalRatioAndBarHeightConstraintsAreValidated()
    {
        BarcodeRenderRequest physical = CreateRenderRequest();
        BarcodeSizing physicalSizing = new BarcodeSizing();
        physicalSizing.ModuleSizeMillimetres = 0.5;
        physicalSizing.RequestedWidthMillimetres = 10.0;
        physical.Options!.Sizing = physicalSizing;

        BarcodeRenderRequest ratio = CreateRenderRequest();
        BarcodeSizing ratioSizing = new BarcodeSizing();
        ratioSizing.TargetAspectRatio = 1.0;
        ratio.Options!.Sizing = ratioSizing;

        BarcodeRenderRequest height = CreateRenderRequest();
        height.Options!.TextMode = BarcodeTextMode.Custom;
        height.Options.CustomText = "ABC123";
        BarcodeSizing heightSizing = new BarcodeSizing();
        heightSizing.RequestedHeightPixels = 1;
        height.Options.Sizing = heightSizing;

        BarcodeValidationReport physicalReport = Validate(physical);
        BarcodeValidationReport ratioReport = Validate(ratio);
        BarcodeValidationReport heightReport = Validate(height);

        Assert.Multiple(() =>
        {
            Assert.That(physicalReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_PHYSICAL_SIZE_CONFLICT"));
            Assert.That(ratioReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_ASPECT_RATIO_MISMATCH"));
            Assert.That(heightReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_BAR_HEIGHT_INSUFFICIENT"));
        });
    }

    [Test]
    public void HumanReadableTextGeometryIsCheckedBeforePlanning()
    {
        BarcodeRenderRequest render = CreateRenderRequest();
        BarcodeTextRun run = new BarcodeTextRun();
        run.Text = "ABC123";
        run.X = double.NaN;
        run.Width = 10.0;
        run.Height = 1.0;
        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.Runs = new BarcodeTextRun[] { run };
        layout.ReservedHeight = double.PositiveInfinity;
        render.Symbol!.TextLayout = layout;
        render.Options!.TextMode = BarcodeTextMode.Standard;

        BarcodeValidationReport report = Validate(render);
        string[] codes = report.Issues!.Select(issue => issue.Code!).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsConformant, Is.False);
            Assert.That(codes, Does.Contain("RENDER_HRT_HEIGHT_INVALID"));
            Assert.That(codes, Does.Contain("RENDER_HRT_RUN_INVALID"));
        });
    }

    [Test]
    public void ColorContrastPolarityAndVisibilityAreValidated()
    {
        BarcodeRenderRequest lowContrast = CreateRenderRequest();
        lowContrast.Options!.ForegroundRgba = 0x777777FFu;
        lowContrast.Options.BackgroundRgba = 0x888888FFu;
        BarcodeRenderRequest reversed = CreateRenderRequest();
        reversed.Options!.ForegroundRgba = 0xFFFFFFFFu;
        reversed.Options.BackgroundRgba = 0x000000FFu;
        BarcodeRenderRequest invisible = CreateRenderRequest();
        invisible.Options!.ForegroundRgba = 0x11223300u;

        BarcodeValidationReport lowContrastReport = Validate(lowContrast);
        BarcodeValidationReport reversedReport = Validate(reversed);
        BarcodeValidationReport invisibleReport = Validate(invisible);

        Assert.Multiple(() =>
        {
            Assert.That(lowContrastReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_CONTRAST_INSUFFICIENT"));
            Assert.That(reversedReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_COLOR_POLARITY_INVALID"));
            Assert.That(invisibleReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_FOREGROUND_TRANSPARENT"));
        });
    }

    [Test]
    public void OutputFeatureAndAllocationConstraintsAreValidated()
    {
        BarcodeRenderRequest svgText = CreateRenderRequest();
        svgText.Options!.UseSvgText = true;
        BarcodeRenderRequest transparency = CreateRenderRequest();
        transparency.Options!.TransparentBackground = true;
        BarcodeRenderRequest excessive = CreateRenderRequest();
        excessive.Options!.ModuleScale = 1000000;

        BarcodeValidationReport svgTextReport = Validate(svgText);
        BarcodeValidationReport transparencyReport = Validate(transparency);
        BarcodeValidationReport excessiveReport = Validate(excessive);

        Assert.Multiple(() =>
        {
            Assert.That(svgTextReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_SVG_TEXT_NOT_APPLICABLE"));
            Assert.That(transparencyReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_TRANSPARENCY_UNSUPPORTED"));
            Assert.That(excessiveReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_OUTPUT_CAPACITY_EXCEEDED"));
        });
    }

    [Test]
    public void MissingRequestAndReturnedCollectionsAreDefensive()
    {
        IBarcodeValidator validator = new BarcodeValidator();
        BarcodeValidationRequest missing = new BarcodeValidationRequest();
        missing.Mode = BarcodeValidationMode.Strict;
        BarcodeValidationReport missingReport =
            validator.ValidateBarcodeRender(missing);
        BarcodeRenderRequest render = CreateRenderRequest();
        BarcodeValidationRequest request = CreateValidationRequest(render);
        BarcodeValidationReport first = validator.ValidateBarcodeRender(request);
        first.Metrics![0].Name = "changed";
        BarcodeValidationReport second = validator.ValidateBarcodeRender(request);

        Assert.Multiple(() =>
        {
            Assert.That(missingReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "RENDER_REQUEST_MISSING"));
            Assert.That(second.Metrics![0].Name, Is.Not.EqualTo("changed"));
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    static BarcodeValidationReport Validate(BarcodeRenderRequest render)
    {
        return new BarcodeValidator().ValidateBarcodeRender(
            CreateValidationRequest(render));
    }

    static BarcodeValidationRequest CreateValidationRequest(
        BarcodeRenderRequest render)
    {
        BarcodeValidationRequest request = new BarcodeValidationRequest();
        request.Format = BarcodeFormat.Code128;
        request.Render = render;
        request.Mode = BarcodeValidationMode.Strict;
        return request;
    }

    static BarcodeRenderRequest CreateRenderRequest()
    {
        byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes("ABC123");
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "ABC123";
        payload.RawBytes = bytes;
        BarcodeSymbol symbol = Code128Layout.CreateSymbol(
            Code128Encoder.EncodeBytes(bytes, null),
            payload,
            null);
        BarcodeRenderOptions options = new BarcodeRenderOptions();
        options.Format = BarcodeRenderFormat.Gray8;
        BarcodeRenderRequest render = new BarcodeRenderRequest();
        render.Symbol = symbol;
        render.Options = options;
        return render;
    }

    static string JoinCodes(BarcodeValidationReport report)
    {
        if (report.Issues == null)
        {
            return "null";
        }

        return string.Join(",", report.Issues.Select(issue => issue.Code));
    }
}
