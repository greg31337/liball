namespace barcode.Tests;

public class ImageQualityValidationTests
{
    [Test]
    public void CleanImageReportsEveryHeuristicQualityCategory()
    {
        BarcodeImage image = DrawCode128Image("QUALITY", 3, 40, 12, 40);

        BarcodeValidationReport report = Validate(image);
        string[] names = report.Metrics!.Select(metric => metric.Name!).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(report.IsConformant, Is.True, JoinCodes(report));
            Assert.That(report.Standard, Is.EqualTo(BarcodeStandards.Code128));
            Assert.That(names, Is.EquivalentTo(new string[]
            {
                "image_contrast",
                "detected_bounds_coverage",
                "module_size_consistency",
                "quiet_zone_clearance_modules",
                "truncation_margin_modules",
                "skew_degrees",
                "perspective_distortion",
                "axial_grid_nonuniformity_proxy",
                "symbol_contrast",
                "modulation_proxy",
                "decode_confidence",
                "fixed_pattern_integrity",
                "unused_ecc_margin"
            }));
            Assert.That(report.Metrics, Has.All.Matches<BarcodeQualityMetric>(
                metric => metric.IsHeuristic &&
                    metric.Interpretation != null &&
                    metric.Interpretation.Length > 0));
            Assert.That(names, Has.All.Matches<string>(name =>
                report.Diagnostics!.Any(diagnostic =>
                    diagnostic.Code == name.ToUpperInvariant())));
            Assert.That(report.Diagnostics, Is.Not.Empty);
        });
    }

    [Test]
    public void LowContrastAndUndecodableImagesReportQualityFailures()
    {
        BarcodeImage lowContrast = DrawCode128Image(
            "FAINT",
            3,
            40,
            12,
            40);
        RemapImageSamples(lowContrast, 120, 150);
        BarcodeImage blank = BarcodeTestBuilder.CreateGrayImage(80, 40, 240);

        BarcodeValidationReport lowContrastReport = Validate(lowContrast);
        BarcodeValidationReport blankReport = Validate(blank);

        Assert.Multiple(() =>
        {
            Assert.That(lowContrastReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "IMAGE_CONTRAST_INSUFFICIENT"));
            Assert.That(blankReport.IsConformant, Is.False);
            Assert.That(blankReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "IMAGE_NOT_DECODED"));
            Assert.That(blankReport.Metrics,
                Has.One.Matches<BarcodeQualityMetric>(
                    metric => metric.Name == "image_contrast" &&
                        !metric.MeetsThreshold));
        });
    }

    [Test]
    public void DarkBoundaryEvidenceReportsImageTruncation()
    {
        BarcodeImage image = BarcodeTestBuilder.CreateGrayImage(40, 30, 240);
        int y = 4;
        while (y < 26)
        {
            image.Pixels![(y * image.Stride)] = 20;
            y = y + 1;
        }

        BarcodeValidationReport report = Validate(image);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsConformant, Is.False);
            Assert.That(report.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "IMAGE_TRUNCATED"));
            Assert.That(report.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "IMAGE_NOT_DECODED"));
        });
    }

    [Test]
    public void MalformedImageEnvelopeProducesIssuesWithoutThrowing()
    {
        BarcodeImage missing = new BarcodeImage();
        BarcodeImage invalid = new BarcodeImage();
        invalid.Pixels = new byte[4];
        invalid.Width = 3;
        invalid.Height = 2;
        invalid.Stride = 2;
        invalid.Offset = -1;
        invalid.PixelFormat = (BarcodePixelFormat)99;
        invalid.DpiX = double.NaN;

        BarcodeValidationReport missingReport = Validate(missing);
        BarcodeValidationReport invalidReport = Validate(invalid);
        string[] invalidCodes = invalidReport.Issues!
            .Select(issue => issue.Code!).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(missingReport.Issues,
                Has.One.Matches<BarcodeValidationIssue>(
                    issue => issue.Code == "IMAGE_PIXELS_MISSING"));
            Assert.That(invalidCodes, Does.Contain("IMAGE_DPI_INVALID"));
            Assert.That(invalidCodes, Does.Contain("IMAGE_PIXEL_FORMAT_INVALID"));
        });
    }

    [Test]
    public void ImageValidationReturnsDefensiveMetricsAndDiagnostics()
    {
        BarcodeImage image = DrawCode128Image("COPY", 3, 40, 12, 40);
        BarcodeValidationRequest request = CreateRequest(image);
        IBarcodeValidator validator = new BarcodeValidator();

        BarcodeValidationReport first = validator.ValidateBarcodeImage(request);
        first.Metrics![0].Name = "changed";
        first.Diagnostics![0].Code = "CHANGED";
        BarcodeValidationReport second = validator.ValidateBarcodeImage(request);

        Assert.Multiple(() =>
        {
            Assert.That(second.Metrics![0].Name, Is.Not.EqualTo("changed"));
            Assert.That(second.Diagnostics![0].Code, Is.Not.EqualTo("CHANGED"));
        });
    }

    static BarcodeValidationReport Validate(BarcodeImage image)
    {
        return new BarcodeValidator().ValidateBarcodeImage(
            CreateRequest(image));
    }

    static BarcodeValidationRequest CreateRequest(BarcodeImage image)
    {
        BarcodeValidationRequest request = new BarcodeValidationRequest();
        request.Format = BarcodeFormat.Code128;
        request.Image = image;
        request.Mode = BarcodeValidationMode.Strict;
        return request;
    }

    static BarcodeImage DrawCode128Image(
        string text,
        int scale,
        int left,
        int top,
        int barHeight)
    {
        byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes(text);
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = (byte[])bytes.Clone();
        BarcodeSymbol symbol = Code128Layout.CreateSymbol(
            Code128Encoder.EncodeBytes(bytes, null),
            payload,
            null);
        int width = left + (symbol.WidthModules * scale) + 40;
        int height = top + barHeight + 20;
        BarcodeImage image = BarcodeTestBuilder.CreateGrayImage(
            width,
            height,
            240);
        int y = top;
        while (y < top + barHeight)
        {
            int module = 0;
            while (module < symbol.WidthModules)
            {
                if (symbol.Modules![module] != 0)
                {
                    int repeat = 0;
                    while (repeat < scale)
                    {
                        int x = left + (module * scale) + repeat;
                        image.Pixels![(y * image.Stride) + x] = 20;
                        repeat = repeat + 1;
                    }
                }

                module = module + 1;
            }

            y = y + 1;
        }

        return image;
    }

    static void RemapImageSamples(
        BarcodeImage image,
        byte dark,
        byte light)
    {
        int index = 0;
        while (index < image.Pixels!.Length)
        {
            image.Pixels[index] = image.Pixels[index] < 128
                ? dark
                : light;
            index = index + 1;
        }
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
