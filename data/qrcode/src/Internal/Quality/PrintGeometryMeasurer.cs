namespace qrcode;

internal class PrintGeometryMeasurer
{
    // Raster polarity is normalized (dark = 0). Reference is a known nominal
    // encoding-region matrix aligned with detection.Corners, without quiet-zone
    // modules. Growth is observed dark-run width minus nominal run width, in
    // modules, NOT divided by run length: merged dark modules share two edges.
    // Geometry must be independently estimated; fitting it to ink edges can hide
    // growth. These directional averages are uncalibrated and remain ungraded.
    internal static PrintGeometryMeasurement Measure(QrRaster raster, QrDetection detection, QrMatrix reference,
        QrDetectionOptions sampling, QrQualityOptions quality)
    {
        DetectorPipeline.ValidateOptions(sampling);
        if (quality == null || reference == null || detection == null || !detection.HasSymbolKindEstimate)
        {
            throw new QrInputException("Quality options, nominal modules, and a profile detection are required.", "reference");
        }
        ValidateProfile(detection, quality);
        PerspectiveTransform transform = PerspectiveSamplingPipeline.CreateInitialTransform(detection, reference, sampling);
        int area = CheckedDimensions.CalculateArea(reference.Width, reference.Height, quality.MaximumModuleSamples);
        if (reference.Values == null || reference.Values.Length != area)
        {
            throw new QrInputException("Nominal module values must match the encoding region.", "Values");
        }
        foreach (QrModuleValue value in reference.Values)
        {
            if (value != QrModuleValue.Light && value != QrModuleValue.Dark)
            {
                throw new QrInputException("Nominal modules must be known light or dark values.", "Values");
            }
        }
        PrintGeometryMeasurement result = new PrintGeometryMeasurement
        {
            QuietZone = QuietZoneMeasurer.Measure(raster, detection, sampling)
        };
        result.HorizontalPrintGrowth.Limitation = "Print-growth measurement is disabled.";
        result.VerticalPrintGrowth.Limitation = "Print-growth measurement is disabled.";
        if (!quality.AnalyzePrintGrowth) { return result; }
        PrintGrowthSampler sampler = new PrintGrowthSampler
        {
            Raster = raster, Transform = transform, MaximumSamples = quality.MaximumModuleSamples
        };
        MeasureAxis(reference, sampler, true, result);
        MeasureAxis(reference, sampler, false, result);
        result.SampleCount = sampler.SampleCount;
        return result;
    }

    static void MeasureAxis(QrMatrix reference, PrintGrowthSampler sampler, bool horizontal, PrintGeometryMeasurement result)
    {
        int lines = horizontal ? reference.Height : reference.Width;
        int length = horizontal ? reference.Width : reference.Height;
        int count = 0;
        int rejected = 0;
        double mean = 0;
        for (int line = 0; line < lines; line++)
        {
            int position = 0;
            while (position < length)
            {
                if (NominalValue(reference, position, line, horizontal) != QrModuleValue.Dark) { position++; continue; }
                int start = position;
                bool observedDark = true;
                while (position < length && NominalValue(reference, position, line, horizontal) == QrModuleValue.Dark)
                {
                    if (sampler.Read(position + 0.5, line + 0.5, horizontal) != QrModuleValue.Dark) { observedDark = false; }
                    position++;
                }
                int end = position;
                if (!observedDark || sampler.Read(start - 0.5, line + 0.5, horizontal) != QrModuleValue.Light
                    || sampler.Read(end + 0.5, line + 0.5, horizontal) != QrModuleValue.Light)
                {
                    rejected++;
                    continue;
                }
                double first = sampler.FindBoundary(start - 0.5, start + 0.5, line + 0.5, horizontal, false);
                double last = sampler.FindBoundary(end - 0.5, end + 0.5, line + 0.5, horizontal, true);
                if (!double.IsFinite(first) || !double.IsFinite(last)) { rejected++; continue; }
                double growth = last - first - (end - start);
                count++;
                mean += (growth - mean) / count;
            }
        }
        QrQualityMetric metric = horizontal ? result.HorizontalPrintGrowth : result.VerticalPrintGrowth;
        metric.IsCalculated = count > 0;
        metric.Value = mean;
        metric.Limitation = count == 0 ? "No fully observed dark runs with readable bounding transitions."
            : "Mean dark-run width change in modules from a binary raster; pixel resolution, nominal geometry, and unreadable runs limit the measurement. No calibrated grade is assigned.";
        if (horizontal) { result.HorizontalRunCount = count; result.RejectedHorizontalRunCount = rejected; }
        else { result.VerticalRunCount = count; result.RejectedVerticalRunCount = rejected; }
    }

    static QrModuleValue NominalValue(QrMatrix reference, int position, int line, bool horizontal)
    {
        return reference.Values[horizontal ? line * reference.Width + position : position * reference.Width + line];
    }

    static void ValidateProfile(QrDetection detection, QrQualityOptions quality)
    {
        QrSymbolKind kind = detection.EstimatedSymbolKind;
        int width = detection.EstimatedWidth;
        int height = detection.EstimatedHeight;
        bool valid = false;
        bool allowed = false;
        if (kind == QrSymbolKind.Model2)
        {
            allowed = quality.AllowModel2;
            valid = width == height && width >= Model2VersionTable.GetDimension(QrLimits.Model2MinimumVersion)
                && width <= Model2VersionTable.GetDimension(QrLimits.Model2MaximumVersion) && (width - 17) % 4 == 0;
        }
        else if (kind == QrSymbolKind.Micro)
        {
            allowed = quality.AllowMicro;
            valid = width == height && width >= MicroVersionTable.GetDimension(QrLimits.MicroMinimumVersion)
                && width <= MicroVersionTable.GetDimension(QrLimits.MicroMaximumVersion) && (width - 9) % 2 == 0;
        }
        else if (kind == QrSymbolKind.LegacyModel1)
        {
            allowed = quality.AllowLegacyModel1;
            valid = width == height && width >= LegacyModel1Table.GetDimension(LegacyModel1Table.MinimumVersion)
                && width <= LegacyModel1Table.GetDimension(LegacyModel1Table.MaximumVersion) && (width - 17) % 4 == 0;
        }
        else if (kind == QrSymbolKind.rMQR)
        {
            allowed = quality.AllowRectangularMicro;
            for (int version = RectangularMicroVersionTable.MinimumVersionIndex; version <= RectangularMicroVersionTable.MaximumVersionIndex; version++)
            {
                if (width == RectangularMicroVersionTable.GetWidth(version) && height == RectangularMicroVersionTable.GetHeight(version)) { valid = true; }
            }
        }
        if (!valid || !allowed || !quality.AutomaticSymbolKind && quality.SymbolKind != kind)
        {
            throw new QrInputException("The nominal dimensions or selected quality profile are invalid or disabled.", "SymbolKind");
        }
    }
}
