using System.Collections.Generic;

namespace jpeg;

internal class JpegLosslessTransformer
{
    internal JpegCoefficientReader CoefficientReader = new JpegCoefficientReader();
    internal JpegCoefficientWriter CoefficientWriter = new JpegCoefficientWriter();
    internal JpegCoefficientOrderConverter OrderConverter = new JpegCoefficientOrderConverter();
    internal JpegMcuLayoutCalculator LayoutCalculator = new JpegMcuLayoutCalculator();
    internal JpegTransformOptions Options = new JpegTransformOptions();
    internal System.Threading.CancellationToken CancellationTokenValue;

    internal byte[] Rotate90(byte[] data)
    {
        return Rotate90WithOptionsAndCancellation(data, new JpegTransformOptions(), default(System.Threading.CancellationToken));
    }

    internal byte[] Rotate180(byte[] data)
    {
        return Rotate180WithOptionsAndCancellation(data, new JpegTransformOptions(), default(System.Threading.CancellationToken));
    }

    internal byte[] Rotate270(byte[] data)
    {
        return Rotate270WithOptionsAndCancellation(data, new JpegTransformOptions(), default(System.Threading.CancellationToken));
    }

    internal byte[] FlipHorizontal(byte[] data)
    {
        return FlipHorizontalWithOptionsAndCancellation(data, new JpegTransformOptions(), default(System.Threading.CancellationToken));
    }

    internal byte[] FlipVertical(byte[] data)
    {
        return FlipVerticalWithOptionsAndCancellation(data, new JpegTransformOptions(), default(System.Threading.CancellationToken));
    }

    internal byte[] Transpose(byte[] data)
    {
        return TransposeWithOptionsAndCancellation(data, new JpegTransformOptions(), default(System.Threading.CancellationToken));
    }

    internal byte[] Transverse(byte[] data)
    {
        return TransverseWithOptionsAndCancellation(data, new JpegTransformOptions(), default(System.Threading.CancellationToken));
    }

    internal byte[] Crop(byte[] data, int x, int y, int width, int height)
    {
        return CropWithOptionsAndCancellation(
            data,
            x,
            y,
            width,
            height,
            new JpegTransformOptions(),
            default(System.Threading.CancellationToken));
    }

    internal byte[] ConvertToGrayscale(byte[] data)
    {
        return ConvertToGrayscaleWithOptionsAndCancellation(data, new JpegTransformOptions(), default(System.Threading.CancellationToken));
    }

    internal byte[] Rotate90WithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        Configure(options, cancellationToken);
        return RotateBytes(data, 90);
    }

    internal byte[] Rotate180WithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        Configure(options, cancellationToken);
        return RotateBytes(data, 180);
    }

    internal byte[] Rotate270WithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        Configure(options, cancellationToken);
        return RotateBytes(data, 270);
    }

    internal byte[] FlipHorizontalWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        Configure(options, cancellationToken);
        return FlipBytes(data, true);
    }

    internal byte[] FlipVerticalWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        Configure(options, cancellationToken);
        return FlipBytes(data, false);
    }

    internal byte[] TransposeWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        Configure(options, cancellationToken);
        return TransposeBytes(data, false);
    }

    internal byte[] TransverseWithOptionsAndCancellation(byte[] data, JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        Configure(options, cancellationToken);
        return TransposeBytes(data, true);
    }

    internal byte[] CropWithOptionsAndCancellation(
        byte[] data,
        int x,
        int y,
        int width,
        int height,
        JpegTransformOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Configure(options, cancellationToken);
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        CheckCancellation();
        JpegCoefficientImage image = CoefficientReader.Read(data);
        CheckCancellation();
        JpegCoefficientImage cropped = CropCoefficientModel(image, x, y, width, height);
        ApplyMetadataPolicy(cropped);
        CheckCancellation();
        return CoefficientWriter.Write(cropped);
    }

    internal byte[] ConvertToGrayscaleWithOptionsAndCancellation(
        byte[] data,
        JpegTransformOptions options,
        System.Threading.CancellationToken cancellationToken)
    {
        Configure(options, cancellationToken);
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        CheckCancellation();
        JpegCoefficientImage image = CoefficientReader.Read(data);
        CheckCancellation();
        JpegCoefficientImage grayscale = ConvertCoefficientImageToGrayscale(image);
        ApplyMetadataPolicy(grayscale);
        CheckCancellation();
        return CoefficientWriter.Write(grayscale);
    }

    internal JpegCoefficientImage Rotate90Coefficients(JpegCoefficientImage image)
    {
        Configure(new JpegTransformOptions(), default(System.Threading.CancellationToken));
        return Rotate(image, 90);
    }

    internal JpegCoefficientImage Rotate180Coefficients(JpegCoefficientImage image)
    {
        Configure(new JpegTransformOptions(), default(System.Threading.CancellationToken));
        return Rotate(image, 180);
    }

    internal JpegCoefficientImage Rotate270Coefficients(JpegCoefficientImage image)
    {
        Configure(new JpegTransformOptions(), default(System.Threading.CancellationToken));
        return Rotate(image, 270);
    }

    internal JpegCoefficientImage FlipHorizontalCoefficients(JpegCoefficientImage image)
    {
        Configure(new JpegTransformOptions(), default(System.Threading.CancellationToken));
        return Flip(image, true);
    }

    internal JpegCoefficientImage FlipVerticalCoefficients(JpegCoefficientImage image)
    {
        Configure(new JpegTransformOptions(), default(System.Threading.CancellationToken));
        return Flip(image, false);
    }

    internal JpegCoefficientImage TransposeCoefficients(JpegCoefficientImage image)
    {
        Configure(new JpegTransformOptions(), default(System.Threading.CancellationToken));
        return TransposeCoefficientsWithDirection(image, false);
    }

    internal JpegCoefficientImage TransverseCoefficients(JpegCoefficientImage image)
    {
        Configure(new JpegTransformOptions(), default(System.Threading.CancellationToken));
        return TransposeCoefficientsWithDirection(image, true);
    }

    internal JpegCoefficientImage CropCoefficients(JpegCoefficientImage image, int x, int y, int width, int height)
    {
        Configure(new JpegTransformOptions(), default(System.Threading.CancellationToken));
        return CropCoefficientModel(image, x, y, width, height);
    }

    internal JpegCoefficientImage ConvertToGrayscaleCoefficients(JpegCoefficientImage image)
    {
        Configure(new JpegTransformOptions(), default(System.Threading.CancellationToken));
        return ConvertCoefficientImageToGrayscale(image);
    }

    internal JpegCoefficientImage CropCoefficientModel(JpegCoefficientImage image, int x, int y, int width, int height)
    {
        ValidateImage(image);
        JpegFrameHeader sourceFrame = CoefficientWriter.ResolveFrame(image);
        JpegMcuLayout sourceLayout = LayoutCalculator.Calculate(sourceFrame);
        JpegRectangle rectangle = ResolveCropRectangle(sourceLayout, x, y, width, height);
        JpegFrameHeader croppedFrame = CropFrame(sourceFrame, rectangle.Width, rectangle.Height);
        JpegMcuLayout croppedLayout = LayoutCalculator.Calculate(croppedFrame);

        JpegCoefficientImage cropped = new JpegCoefficientImage();
        cropped.Width = rectangle.Width;
        cropped.Height = rectangle.Height;
        cropped.Precision = sourceFrame.Precision;
        cropped.ComponentCount = sourceFrame.Components.Count;
        cropped.Process = GetCoefficientProcess(image, sourceFrame);
        cropped.EntropyCoding = image.EntropyCoding;
        cropped.Progressive = image.Progressive;
        cropped.ArithmeticCoding = image.ArithmeticCoding;
        cropped.ColorSpace = image.ColorSpace;
        cropped.Subsampling = image.Subsampling;
        cropped.Frame = croppedFrame;
        cropped.ScanHeaders = CloneScanHeaders(image.ScanHeaders);
        cropped.QuantizationTables = RotateQuantizationTables(image.QuantizationTables, 180);
        cropped.HuffmanTables = CreateTransformHuffmanTables(image.HuffmanTables);
        cropped.ArithmeticConditioningTables = image.ArithmeticConditioningTables;
        cropped.HasRestartInterval = image.HasRestartInterval;
        cropped.RestartInterval = image.RestartInterval;
        cropped.RestartIntervalUnit = image.RestartIntervalUnit;
        cropped.RestartMarkerCount = image.RestartMarkerCount;
        cropped.Markers = image.Markers;
        cropped.Metadata = image.Metadata;
        cropped.Diagnostics = CloneDiagnostics(image.Diagnostics);
        cropped.DiagnosticCollection = image.DiagnosticCollection;
        cropped.Components = CropComponents(image.Components, sourceLayout, croppedLayout, rectangle.X, rectangle.Y);
        return cropped;
    }

    internal byte[] RotateBytes(byte[] data, int degrees)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        CheckCancellation();
        JpegCoefficientImage image = CoefficientReader.Read(data);
        CheckCancellation();
        JpegCoefficientImage rotated = Rotate(image, degrees);
        ApplyMetadataPolicy(rotated);
        CheckCancellation();
        return CoefficientWriter.Write(rotated);
    }

    internal byte[] FlipBytes(byte[] data, bool horizontal)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        CheckCancellation();
        JpegCoefficientImage image = CoefficientReader.Read(data);
        CheckCancellation();
        JpegCoefficientImage flipped = Flip(image, horizontal);
        ApplyMetadataPolicy(flipped);
        CheckCancellation();
        return CoefficientWriter.Write(flipped);
    }

    internal byte[] TransposeBytes(byte[] data, bool transverse)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        CheckCancellation();
        JpegCoefficientImage image = CoefficientReader.Read(data);
        CheckCancellation();
        JpegCoefficientImage transposed = TransposeCoefficientsWithDirection(image, transverse);
        ApplyMetadataPolicy(transposed);
        CheckCancellation();
        return CoefficientWriter.Write(transposed);
    }

    internal JpegCoefficientImage Rotate(JpegCoefficientImage image, int degrees)
    {
        ValidateRotation(degrees);
        ValidateImage(image);
        JpegFrameHeader sourceFrame = CoefficientWriter.ResolveFrame(image);
        JpegMcuLayout sourceLayout = LayoutCalculator.Calculate(sourceFrame);
        if (Options.RequirePerfectTransform)
        {
            ValidateMcuAligned(sourceLayout);
        }

        bool swapAxes = ShouldSwapAxes(degrees);
        JpegCoefficientImage rotated = new JpegCoefficientImage();
        rotated.Width = GetRotatedWidth(sourceFrame, swapAxes);
        rotated.Height = GetRotatedHeight(sourceFrame, swapAxes);
        rotated.Precision = sourceFrame.Precision;
        rotated.ComponentCount = sourceFrame.Components.Count;
        rotated.Process = GetCoefficientProcess(image, sourceFrame);
        rotated.EntropyCoding = image.EntropyCoding;
        rotated.Progressive = image.Progressive;
        rotated.ArithmeticCoding = image.ArithmeticCoding;
        rotated.ColorSpace = image.ColorSpace;
        rotated.Subsampling = RotateSubsampling(image.Subsampling, degrees);
        rotated.Frame = RotateFrame(sourceFrame, degrees);
        rotated.ScanHeaders = CloneScanHeaders(image.ScanHeaders);
        rotated.QuantizationTables = RotateQuantizationTables(image.QuantizationTables, degrees);
        rotated.HuffmanTables = CreateTransformHuffmanTables(image.HuffmanTables);
        rotated.ArithmeticConditioningTables = image.ArithmeticConditioningTables;
        rotated.HasRestartInterval = image.HasRestartInterval;
        rotated.RestartInterval = image.RestartInterval;
        rotated.RestartIntervalUnit = image.RestartIntervalUnit;
        rotated.RestartMarkerCount = image.RestartMarkerCount;
        rotated.Markers = image.Markers;
        rotated.Metadata = image.Metadata;
        rotated.Diagnostics = CloneDiagnostics(image.Diagnostics);
        rotated.DiagnosticCollection = image.DiagnosticCollection;
        rotated.Components = RotateComponents(image.Components, degrees);
        return rotated;
    }

    internal JpegCoefficientImage Flip(JpegCoefficientImage image, bool horizontal)
    {
        ValidateImage(image);
        JpegFrameHeader sourceFrame = CoefficientWriter.ResolveFrame(image);
        JpegMcuLayout sourceLayout = LayoutCalculator.Calculate(sourceFrame);
        if (Options.RequirePerfectTransform)
        {
            ValidateMcuAligned(sourceLayout);
        }

        JpegCoefficientImage flipped = new JpegCoefficientImage();
        flipped.Width = sourceFrame.Width;
        flipped.Height = sourceFrame.Height;
        flipped.Precision = sourceFrame.Precision;
        flipped.ComponentCount = sourceFrame.Components.Count;
        flipped.Process = GetCoefficientProcess(image, sourceFrame);
        flipped.EntropyCoding = image.EntropyCoding;
        flipped.Progressive = image.Progressive;
        flipped.ArithmeticCoding = image.ArithmeticCoding;
        flipped.ColorSpace = image.ColorSpace;
        flipped.Subsampling = image.Subsampling;
        flipped.Frame = RotateFrame(sourceFrame, 180);
        flipped.ScanHeaders = CloneScanHeaders(image.ScanHeaders);
        flipped.QuantizationTables = RotateQuantizationTables(image.QuantizationTables, 180);
        flipped.HuffmanTables = CreateTransformHuffmanTables(image.HuffmanTables);
        flipped.ArithmeticConditioningTables = image.ArithmeticConditioningTables;
        flipped.HasRestartInterval = image.HasRestartInterval;
        flipped.RestartInterval = image.RestartInterval;
        flipped.RestartIntervalUnit = image.RestartIntervalUnit;
        flipped.RestartMarkerCount = image.RestartMarkerCount;
        flipped.Markers = image.Markers;
        flipped.Metadata = image.Metadata;
        flipped.Diagnostics = CloneDiagnostics(image.Diagnostics);
        flipped.DiagnosticCollection = image.DiagnosticCollection;
        flipped.Components = FlipComponents(image.Components, horizontal);
        return flipped;
    }

    internal JpegCoefficientImage TransposeCoefficientsWithDirection(JpegCoefficientImage image, bool transverse)
    {
        ValidateImage(image);
        JpegFrameHeader sourceFrame = CoefficientWriter.ResolveFrame(image);
        JpegMcuLayout sourceLayout = LayoutCalculator.Calculate(sourceFrame);
        if (Options.RequirePerfectTransform)
        {
            ValidateMcuAligned(sourceLayout);
        }

        JpegCoefficientImage transposed = new JpegCoefficientImage();
        transposed.Width = sourceFrame.Height;
        transposed.Height = sourceFrame.Width;
        transposed.Precision = sourceFrame.Precision;
        transposed.ComponentCount = sourceFrame.Components.Count;
        transposed.Process = GetCoefficientProcess(image, sourceFrame);
        transposed.EntropyCoding = image.EntropyCoding;
        transposed.Progressive = image.Progressive;
        transposed.ArithmeticCoding = image.ArithmeticCoding;
        transposed.ColorSpace = image.ColorSpace;
        transposed.Subsampling = RotateSubsampling(image.Subsampling, 90);
        transposed.Frame = RotateFrame(sourceFrame, 90);
        transposed.ScanHeaders = CloneScanHeaders(image.ScanHeaders);
        transposed.QuantizationTables = RotateQuantizationTables(image.QuantizationTables, 90);
        transposed.HuffmanTables = CreateTransformHuffmanTables(image.HuffmanTables);
        transposed.ArithmeticConditioningTables = image.ArithmeticConditioningTables;
        transposed.HasRestartInterval = image.HasRestartInterval;
        transposed.RestartInterval = image.RestartInterval;
        transposed.RestartIntervalUnit = image.RestartIntervalUnit;
        transposed.RestartMarkerCount = image.RestartMarkerCount;
        transposed.Markers = image.Markers;
        transposed.Metadata = image.Metadata;
        transposed.Diagnostics = CloneDiagnostics(image.Diagnostics);
        transposed.DiagnosticCollection = image.DiagnosticCollection;
        transposed.Components = TransposeComponents(image.Components, transverse);
        return transposed;
    }

    internal void ValidateRotation(int degrees)
    {
        if (degrees != 90 && degrees != 180 && degrees != 270)
        {
            throw new System.ArgumentOutOfRangeException("degrees");
        }
    }

    internal void ValidateImage(JpegCoefficientImage image)
    {
        CoefficientWriter.ValidateImage(image);
    }

    internal void Configure(JpegTransformOptions options, System.Threading.CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.McuCropMode != JpegMcuCropMode.Exact
            && options.McuCropMode != JpegMcuCropMode.Trim
            && options.McuCropMode != JpegMcuCropMode.Expand)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        new JpegMetadataContainerEditor().ValidatePolicy(options.MetadataPolicy);
        Options = options;
        CancellationTokenValue = cancellationToken;
        CheckCancellation();
    }

    internal void CheckCancellation()
    {
        CancellationTokenValue.ThrowIfCancellationRequested();
    }

    internal void ApplyMetadataPolicy(JpegCoefficientImage image)
    {
        image.Metadata = new JpegMetadataPolicyFilter().CreateMetadataForPolicy(image.Metadata, Options.MetadataPolicy);
    }

    internal void ValidateMcuAligned(JpegMcuLayout layout)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        if (layout.Width % layout.McuWidth != 0 || layout.Height % layout.McuHeight != 0)
        {
            throw JpegWriteValidationException.Create("Lossless rotation requires image dimensions aligned to the MCU size.");
        }
    }

    internal void ValidateCropRectangle(JpegMcuLayout layout, int x, int y, int width, int height)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        if (x < 0 || y < 0)
        {
            throw JpegWriteValidationException.Create("Lossless crop origin cannot be negative.");
        }

        if (width <= 0 || height <= 0)
        {
            throw JpegWriteValidationException.Create("Lossless crop dimensions must be greater than zero.");
        }

        if (x > layout.Width - width || y > layout.Height - height)
        {
            throw JpegWriteValidationException.Create("Lossless crop rectangle must fit inside the image.");
        }

        if (x % layout.McuWidth != 0 || y % layout.McuHeight != 0)
        {
            throw JpegWriteValidationException.Create("Lossless crop origin must be aligned to the MCU size.");
        }

        if (width % layout.McuWidth != 0 || height % layout.McuHeight != 0)
        {
            throw JpegWriteValidationException.Create("Lossless crop dimensions must be aligned to the MCU size.");
        }
    }

    internal JpegRectangle ResolveCropRectangle(JpegMcuLayout layout, int x, int y, int width, int height)
    {
        ValidateCropBounds(layout, x, y, width, height);
        if (Options.McuCropMode == JpegMcuCropMode.Exact)
        {
            ValidateCropRectangle(layout, x, y, width, height);
            return CreateRectangle(x, y, width, height);
        }

        int right = x + width;
        int bottom = y + height;
        int resolvedX;
        int resolvedY;
        int resolvedRight;
        int resolvedBottom;
        if (Options.McuCropMode == JpegMcuCropMode.Trim)
        {
            resolvedX = RoundUpToMcu(x, layout.McuWidth);
            resolvedY = RoundUpToMcu(y, layout.McuHeight);
            resolvedRight = RoundDownToMcu(right, layout.McuWidth);
            resolvedBottom = RoundDownToMcu(bottom, layout.McuHeight);
        }
        else
        {
            resolvedX = RoundDownToMcu(x, layout.McuWidth);
            resolvedY = RoundDownToMcu(y, layout.McuHeight);
            resolvedRight = RoundUpToMcu(right, layout.McuWidth);
            resolvedBottom = RoundUpToMcu(bottom, layout.McuHeight);
            if (resolvedRight > layout.Width)
            {
                resolvedRight = layout.Width;
            }
            if (resolvedBottom > layout.Height)
            {
                resolvedBottom = layout.Height;
            }
        }

        if (resolvedRight <= resolvedX || resolvedBottom <= resolvedY)
        {
            throw JpegWriteValidationException.Create("Resolved lossless crop rectangle does not contain a complete MCU region.");
        }

        return CreateRectangle(resolvedX, resolvedY, resolvedRight - resolvedX, resolvedBottom - resolvedY);
    }

    internal void ValidateCropBounds(JpegMcuLayout layout, int x, int y, int width, int height)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }
        if (x < 0 || y < 0)
        {
            throw JpegWriteValidationException.Create("Lossless crop origin cannot be negative.");
        }
        if (width <= 0 || height <= 0)
        {
            throw JpegWriteValidationException.Create("Lossless crop dimensions must be greater than zero.");
        }
        if (x > layout.Width - width || y > layout.Height - height)
        {
            throw JpegWriteValidationException.Create("Lossless crop rectangle must fit inside the image.");
        }
    }

    internal int RoundUpToMcu(int value, int unit)
    {
        return LayoutCalculator.DivideRoundUp(value, unit) * unit;
    }

    internal int RoundDownToMcu(int value, int unit)
    {
        return (value / unit) * unit;
    }

    internal JpegRectangle CreateRectangle(int x, int y, int width, int height)
    {
        JpegRectangle rectangle = new JpegRectangle();
        rectangle.X = x;
        rectangle.Y = y;
        rectangle.Width = width;
        rectangle.Height = height;
        return rectangle;
    }

    internal JpegCoefficientImage ConvertCoefficientImageToGrayscale(JpegCoefficientImage image)
    {
        ValidateImage(image);
        JpegFrameHeader sourceFrame = CoefficientWriter.ResolveFrame(image);
        JpegMcuLayout sourceLayout = LayoutCalculator.Calculate(sourceFrame);
        if (image.ColorSpace != JpegColorSpace.YCbCr || sourceFrame.Components.Count != 3 || image.Components.Count != 3)
        {
            throw JpegUnsupportedException.Create("Lossless grayscale conversion requires a three-component YCbCr coefficient image.");
        }

        JpegFrameComponent luminanceFrame = sourceFrame.Components[0];
        if (luminanceFrame.HorizontalSamplingFactor != sourceLayout.MaxHorizontalSamplingFactor
            || luminanceFrame.VerticalSamplingFactor != sourceLayout.MaxVerticalSamplingFactor)
        {
            throw JpegUnsupportedException.Create("Lossless grayscale conversion requires a full-resolution luminance component.");
        }

        JpegCoefficientImage grayscale = CreateGrayscaleImageSummary(image, sourceFrame);
        grayscale.Frame = CreateGrayscaleFrame(sourceFrame, luminanceFrame);
        grayscale.ScanHeaders = CreateGrayscaleScans(image, luminanceFrame.Identifier);
        grayscale.QuantizationTables = RotateQuantizationTables(image.QuantizationTables, 180);
        grayscale.HuffmanTables = CreateTransformHuffmanTables(image.HuffmanTables);
        grayscale.ArithmeticConditioningTables = image.ArithmeticConditioningTables;
        grayscale.Components.Add(CreateGrayscaleComponent(image.Components[0]));
        return grayscale;
    }

    internal JpegCoefficientImage CreateGrayscaleImageSummary(JpegCoefficientImage image, JpegFrameHeader sourceFrame)
    {
        JpegCoefficientImage grayscale = new JpegCoefficientImage();
        grayscale.Width = sourceFrame.Width;
        grayscale.Height = sourceFrame.Height;
        grayscale.Precision = sourceFrame.Precision;
        grayscale.ComponentCount = 1;
        grayscale.Process = GetCoefficientProcess(image, sourceFrame);
        grayscale.EntropyCoding = image.EntropyCoding;
        grayscale.Progressive = image.Progressive;
        grayscale.ArithmeticCoding = image.ArithmeticCoding;
        grayscale.ColorSpace = JpegColorSpace.Grayscale;
        grayscale.Subsampling = JpegSubsampling.Grayscale;
        grayscale.HasRestartInterval = image.HasRestartInterval;
        grayscale.RestartInterval = image.RestartInterval;
        grayscale.RestartIntervalUnit = image.RestartIntervalUnit;
        grayscale.RestartMarkerCount = image.RestartMarkerCount;
        grayscale.Markers = image.Markers;
        grayscale.Metadata = image.Metadata;
        grayscale.Diagnostics = CloneDiagnostics(image.Diagnostics);
        grayscale.DiagnosticCollection = image.DiagnosticCollection;
        return grayscale;
    }

    internal JpegFrameHeader CreateGrayscaleFrame(JpegFrameHeader source, JpegFrameComponent luminance)
    {
        JpegFrameHeader frame = RotateFrame(source, 180);
        frame.ComponentCount = 1;
        frame.Components = new List<JpegFrameComponent>();
        JpegFrameComponent component = RotateFrameComponent(luminance, 180);
        component.HorizontalSamplingFactor = 1;
        component.VerticalSamplingFactor = 1;
        frame.Components.Add(component);
        return frame;
    }

    internal JpegScanHeader CreateGrayscaleScan(JpegScanHeader source, int identifier)
    {
        JpegScanHeader scan = new JpegScanHeader();
        scan.ComponentCount = 1;
        scan.SpectralStart = source.SpectralStart;
        scan.SpectralEnd = source.SpectralEnd;
        scan.SuccessiveHigh = source.SuccessiveHigh;
        scan.SuccessiveLow = source.SuccessiveLow;
        foreach (JpegScanComponent sourceComponent in source.Components)
        {
            if (sourceComponent.Selector == identifier)
            {
                scan.Components.Add(CloneScanComponent(sourceComponent));
                return scan;
            }
        }

        throw JpegWriteValidationException.Create("Luminance component is missing from the coefficient scan.");
    }

    internal List<JpegScanHeader> CreateGrayscaleScans(JpegCoefficientImage image, int identifier)
    {
        List<JpegScanHeader> scans = new List<JpegScanHeader>();
        if (image.ScanHeaders == null || image.ScanHeaders.Count == 0)
        {
            scans.Add(CreateGrayscaleScan(CoefficientWriter.ResolveScan(image), identifier));
            return scans;
        }

        foreach (JpegScanHeader source in image.ScanHeaders)
        {
            bool containsLuminance = false;
            foreach (JpegScanComponent component in source.Components)
            {
                if (component.Selector == identifier)
                {
                    containsLuminance = true;
                }
            }

            if (containsLuminance)
            {
                scans.Add(CreateGrayscaleScan(source, identifier));
            }
        }

        if (scans.Count == 0)
        {
            throw JpegWriteValidationException.Create("Luminance component is missing from the coefficient scans.");
        }

        return scans;
    }

    internal JpegCoefficientComponent CreateGrayscaleComponent(JpegCoefficientComponent source)
    {
        JpegCoefficientComponent component = new JpegCoefficientComponent();
        component.ComponentIndex = 0;
        component.Identifier = source.Identifier;
        component.HorizontalSamplingFactor = 1;
        component.VerticalSamplingFactor = 1;
        component.QuantizationTableIdentifier = source.QuantizationTableIdentifier;
        component.BlocksPerRow = source.BlocksPerRow;
        component.BlockRows = source.BlockRows;
        component.Coefficients = CloneIntArray(source.Coefficients);
        return component;
    }

    internal bool ShouldSwapAxes(int degrees)
    {
        if (degrees == 90 || degrees == 270)
        {
            return true;
        }

        return false;
    }

    internal int GetRotatedWidth(JpegFrameHeader frame, bool swapAxes)
    {
        if (swapAxes)
        {
            return frame.Height;
        }

        return frame.Width;
    }

    internal int GetRotatedHeight(JpegFrameHeader frame, bool swapAxes)
    {
        if (swapAxes)
        {
            return frame.Width;
        }

        return frame.Height;
    }

    internal JpegProcess GetCoefficientProcess(JpegCoefficientImage image, JpegFrameHeader frame)
    {
        if (image.Process != JpegProcess.Unknown)
        {
            return image.Process;
        }

        return frame.Process;
    }

    internal JpegFrameHeader RotateFrame(JpegFrameHeader source, int degrees)
    {
        bool swapAxes = ShouldSwapAxes(degrees);
        JpegFrameHeader frame = new JpegFrameHeader();
        frame.Marker = source.Marker;
        frame.Value = source.Value;
        frame.Process = source.Process;
        frame.Precision = source.Precision;
        frame.Width = source.Width;
        frame.Height = source.Height;
        if (swapAxes)
        {
            frame.Width = source.Height;
            frame.Height = source.Width;
        }

        frame.ComponentCount = source.Components.Count;
        frame.Progressive = source.Progressive;
        frame.ArithmeticCoding = source.ArithmeticCoding;

        int index = 0;
        while (index < source.Components.Count)
        {
            frame.Components.Add(RotateFrameComponent(source.Components[index], degrees));
            index = index + 1;
        }

        return frame;
    }

    internal JpegFrameHeader CropFrame(JpegFrameHeader source, int width, int height)
    {
        JpegFrameHeader frame = RotateFrame(source, 180);
        frame.Width = width;
        frame.Height = height;
        return frame;
    }

    internal JpegFrameComponent RotateFrameComponent(JpegFrameComponent source, int degrees)
    {
        JpegFrameComponent component = new JpegFrameComponent();
        component.Identifier = source.Identifier;
        component.HorizontalSamplingFactor = source.HorizontalSamplingFactor;
        component.VerticalSamplingFactor = source.VerticalSamplingFactor;
        if (ShouldSwapAxes(degrees))
        {
            component.HorizontalSamplingFactor = source.VerticalSamplingFactor;
            component.VerticalSamplingFactor = source.HorizontalSamplingFactor;
        }

        component.QuantizationTableIdentifier = source.QuantizationTableIdentifier;
        return component;
    }

    internal List<JpegCoefficientComponent> RotateComponents(List<JpegCoefficientComponent> source, int degrees)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        List<JpegCoefficientComponent> components = new List<JpegCoefficientComponent>();
        int index = 0;
        while (index < source.Count)
        {
            CheckCancellation();
            components.Add(RotateComponent(source[index], degrees));
            index = index + 1;
        }

        return components;
    }

    internal List<JpegCoefficientComponent> FlipComponents(List<JpegCoefficientComponent> source, bool horizontal)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        List<JpegCoefficientComponent> components = new List<JpegCoefficientComponent>();
        int index = 0;
        while (index < source.Count)
        {
            CheckCancellation();
            components.Add(FlipComponent(source[index], horizontal));
            index = index + 1;
        }

        return components;
    }

    internal List<JpegCoefficientComponent> TransposeComponents(List<JpegCoefficientComponent> source, bool transverse)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        List<JpegCoefficientComponent> components = new List<JpegCoefficientComponent>();
        int index = 0;
        while (index < source.Count)
        {
            CheckCancellation();
            components.Add(TransposeComponent(source[index], transverse));
            index = index + 1;
        }

        return components;
    }

    internal List<JpegCoefficientComponent> CropComponents(
        List<JpegCoefficientComponent> source,
        JpegMcuLayout sourceLayout,
        JpegMcuLayout destinationLayout,
        int x,
        int y)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        List<JpegCoefficientComponent> components = new List<JpegCoefficientComponent>();
        int index = 0;
        while (index < source.Count)
        {
            CheckCancellation();
            components.Add(CropComponent(source[index], sourceLayout, destinationLayout, x, y));
            index = index + 1;
        }

        return components;
    }

    internal JpegCoefficientComponent RotateComponent(JpegCoefficientComponent source, int degrees)
    {
        CoefficientWriter.ValidateComponent(source);
        JpegCoefficientComponent component = new JpegCoefficientComponent();
        component.ComponentIndex = source.ComponentIndex;
        component.Identifier = source.Identifier;
        component.HorizontalSamplingFactor = source.HorizontalSamplingFactor;
        component.VerticalSamplingFactor = source.VerticalSamplingFactor;
        if (ShouldSwapAxes(degrees))
        {
            component.HorizontalSamplingFactor = source.VerticalSamplingFactor;
            component.VerticalSamplingFactor = source.HorizontalSamplingFactor;
            component.BlocksPerRow = source.BlockRows;
            component.BlockRows = source.BlocksPerRow;
        }
        else
        {
            component.BlocksPerRow = source.BlocksPerRow;
            component.BlockRows = source.BlockRows;
        }

        component.QuantizationTableIdentifier = source.QuantizationTableIdentifier;
        component.Coefficients = new int[component.BlocksPerRow * component.BlockRows * JpegConstants.BlockSampleCount];

        int blockY = 0;
        while (blockY < source.BlockRows)
        {
            CheckCancellation();
            int blockX = 0;
            while (blockX < source.BlocksPerRow)
            {
                CopyRotatedBlock(source, component, blockX, blockY, degrees);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }

        return component;
    }

    internal JpegCoefficientComponent FlipComponent(JpegCoefficientComponent source, bool horizontal)
    {
        CoefficientWriter.ValidateComponent(source);
        JpegCoefficientComponent component = new JpegCoefficientComponent();
        component.ComponentIndex = source.ComponentIndex;
        component.Identifier = source.Identifier;
        component.HorizontalSamplingFactor = source.HorizontalSamplingFactor;
        component.VerticalSamplingFactor = source.VerticalSamplingFactor;
        component.QuantizationTableIdentifier = source.QuantizationTableIdentifier;
        component.BlocksPerRow = source.BlocksPerRow;
        component.BlockRows = source.BlockRows;
        component.Coefficients = new int[component.BlocksPerRow * component.BlockRows * JpegConstants.BlockSampleCount];

        int blockY = 0;
        while (blockY < source.BlockRows)
        {
            CheckCancellation();
            int blockX = 0;
            while (blockX < source.BlocksPerRow)
            {
                CopyFlippedBlock(source, component, blockX, blockY, horizontal);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }

        return component;
    }

    internal JpegCoefficientComponent TransposeComponent(JpegCoefficientComponent source, bool transverse)
    {
        CoefficientWriter.ValidateComponent(source);
        JpegCoefficientComponent component = new JpegCoefficientComponent();
        component.ComponentIndex = source.ComponentIndex;
        component.Identifier = source.Identifier;
        component.HorizontalSamplingFactor = source.VerticalSamplingFactor;
        component.VerticalSamplingFactor = source.HorizontalSamplingFactor;
        component.QuantizationTableIdentifier = source.QuantizationTableIdentifier;
        component.BlocksPerRow = source.BlockRows;
        component.BlockRows = source.BlocksPerRow;
        component.Coefficients = new int[component.BlocksPerRow * component.BlockRows * JpegConstants.BlockSampleCount];

        int blockY = 0;
        while (blockY < source.BlockRows)
        {
            CheckCancellation();
            int blockX = 0;
            while (blockX < source.BlocksPerRow)
            {
                CopyTransposedBlock(source, component, blockX, blockY, transverse);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }

        return component;
    }

    internal JpegCoefficientComponent CropComponent(
        JpegCoefficientComponent source,
        JpegMcuLayout sourceLayout,
        JpegMcuLayout destinationLayout,
        int x,
        int y)
    {
        CoefficientWriter.ValidateComponent(source);
        JpegMcuComponentLayout componentLayout = LayoutCalculator.FindComponentLayout(sourceLayout, source.Identifier);
        JpegMcuComponentLayout destinationComponentLayout = LayoutCalculator.FindComponentLayout(destinationLayout, source.Identifier);
        int sourceBlockX = GetCropBlockX(componentLayout, sourceLayout, x);
        int sourceBlockY = GetCropBlockY(componentLayout, sourceLayout, y);
        int destinationBlocksPerRow = destinationComponentLayout.BlocksPerRow;
        int destinationBlockRows = destinationComponentLayout.BlockRows;

        JpegCoefficientComponent component = new JpegCoefficientComponent();
        component.ComponentIndex = source.ComponentIndex;
        component.Identifier = source.Identifier;
        component.HorizontalSamplingFactor = source.HorizontalSamplingFactor;
        component.VerticalSamplingFactor = source.VerticalSamplingFactor;
        component.QuantizationTableIdentifier = source.QuantizationTableIdentifier;
        component.BlocksPerRow = destinationBlocksPerRow;
        component.BlockRows = destinationBlockRows;
        component.Coefficients = new int[component.BlocksPerRow * component.BlockRows * JpegConstants.BlockSampleCount];

        int blockY = 0;
        while (blockY < destinationBlockRows)
        {
            CheckCancellation();
            int blockX = 0;
            while (blockX < destinationBlocksPerRow)
            {
                CopyBlock(source, component, sourceBlockX + blockX, sourceBlockY + blockY, blockX, blockY);
                blockX = blockX + 1;
            }

            blockY = blockY + 1;
        }

        return component;
    }

    internal void CopyRotatedBlock(JpegCoefficientComponent source, JpegCoefficientComponent destination, int blockX, int blockY, int degrees)
    {
        int destinationX = GetDestinationBlockX(source, blockX, blockY, degrees);
        int destinationY = GetDestinationBlockY(source, blockX, blockY, degrees);
        int sourceOffset = CoefficientReader.GetCoefficientOffset(source, blockX, blockY);
        int destinationOffset = CoefficientReader.GetCoefficientOffset(destination, destinationX, destinationY);
        int[] sourceBlock = new int[JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            sourceBlock[index] = source.Coefficients[sourceOffset + index];
            index = index + 1;
        }

        int[] rotatedBlock = RotateBlock(sourceBlock, degrees);
        index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            destination.Coefficients[destinationOffset + index] = rotatedBlock[index];
            index = index + 1;
        }
    }

    internal void CopyFlippedBlock(JpegCoefficientComponent source, JpegCoefficientComponent destination, int blockX, int blockY, bool horizontal)
    {
        int destinationX = GetFlippedDestinationBlockX(source, blockX, horizontal);
        int destinationY = GetFlippedDestinationBlockY(source, blockY, horizontal);
        int sourceOffset = CoefficientReader.GetCoefficientOffset(source, blockX, blockY);
        int destinationOffset = CoefficientReader.GetCoefficientOffset(destination, destinationX, destinationY);
        int[] sourceBlock = new int[JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            sourceBlock[index] = source.Coefficients[sourceOffset + index];
            index = index + 1;
        }

        int[] flippedBlock = FlipBlock(sourceBlock, horizontal);
        index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            destination.Coefficients[destinationOffset + index] = flippedBlock[index];
            index = index + 1;
        }
    }

    internal void CopyTransposedBlock(JpegCoefficientComponent source, JpegCoefficientComponent destination, int blockX, int blockY, bool transverse)
    {
        int destinationX = GetTransposedDestinationBlockX(source, blockX, blockY, transverse);
        int destinationY = GetTransposedDestinationBlockY(source, blockX, blockY, transverse);
        int sourceOffset = CoefficientReader.GetCoefficientOffset(source, blockX, blockY);
        int destinationOffset = CoefficientReader.GetCoefficientOffset(destination, destinationX, destinationY);
        int[] sourceBlock = new int[JpegConstants.BlockSampleCount];
        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            sourceBlock[index] = source.Coefficients[sourceOffset + index];
            index = index + 1;
        }

        int[] transposedBlock = TransposeBlock(sourceBlock, transverse);
        index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            destination.Coefficients[destinationOffset + index] = transposedBlock[index];
            index = index + 1;
        }
    }

    internal void CopyBlock(
        JpegCoefficientComponent source,
        JpegCoefficientComponent destination,
        int sourceBlockX,
        int sourceBlockY,
        int destinationBlockX,
        int destinationBlockY)
    {
        int sourceOffset = CoefficientReader.GetCoefficientOffset(source, sourceBlockX, sourceBlockY);
        int destinationOffset = CoefficientReader.GetCoefficientOffset(destination, destinationBlockX, destinationBlockY);
        int index = 0;
        while (index < JpegConstants.BlockSampleCount)
        {
            destination.Coefficients[destinationOffset + index] = source.Coefficients[sourceOffset + index];
            index = index + 1;
        }
    }

    internal int GetCropBlockX(JpegMcuComponentLayout componentLayout, JpegMcuLayout sourceLayout, int x)
    {
        int mcuX = x / sourceLayout.McuWidth;
        return mcuX * componentLayout.HorizontalSamplingFactor;
    }

    internal int GetCropBlockY(JpegMcuComponentLayout componentLayout, JpegMcuLayout sourceLayout, int y)
    {
        int mcuY = y / sourceLayout.McuHeight;
        return mcuY * componentLayout.VerticalSamplingFactor;
    }

    internal int GetCropBlockWidth(JpegMcuComponentLayout componentLayout, JpegMcuLayout sourceLayout, int width)
    {
        int mcuWidth = width / sourceLayout.McuWidth;
        return mcuWidth * componentLayout.HorizontalSamplingFactor;
    }

    internal int GetCropBlockHeight(JpegMcuComponentLayout componentLayout, JpegMcuLayout sourceLayout, int height)
    {
        int mcuHeight = height / sourceLayout.McuHeight;
        return mcuHeight * componentLayout.VerticalSamplingFactor;
    }

    internal int GetDestinationBlockX(JpegCoefficientComponent component, int blockX, int blockY, int degrees)
    {
        if (degrees == 90)
        {
            return component.BlockRows - 1 - blockY;
        }

        if (degrees == 180)
        {
            return component.BlocksPerRow - 1 - blockX;
        }

        return blockY;
    }

    internal int GetDestinationBlockY(JpegCoefficientComponent component, int blockX, int blockY, int degrees)
    {
        if (degrees == 90)
        {
            return blockX;
        }

        if (degrees == 180)
        {
            return component.BlockRows - 1 - blockY;
        }

        return component.BlocksPerRow - 1 - blockX;
    }

    internal int GetFlippedDestinationBlockX(JpegCoefficientComponent component, int blockX, bool horizontal)
    {
        if (horizontal)
        {
            return component.BlocksPerRow - 1 - blockX;
        }

        return blockX;
    }

    internal int GetFlippedDestinationBlockY(JpegCoefficientComponent component, int blockY, bool horizontal)
    {
        if (horizontal)
        {
            return blockY;
        }

        return component.BlockRows - 1 - blockY;
    }

    internal int GetTransposedDestinationBlockX(JpegCoefficientComponent component, int blockX, int blockY, bool transverse)
    {
        if (transverse)
        {
            return component.BlockRows - 1 - blockY;
        }

        return blockY;
    }

    internal int GetTransposedDestinationBlockY(JpegCoefficientComponent component, int blockX, int blockY, bool transverse)
    {
        if (transverse)
        {
            return component.BlocksPerRow - 1 - blockX;
        }

        return blockX;
    }

    internal int[] RotateBlock(int[] zigZagBlock, int degrees)
    {
        OrderConverter.ValidateBlock(zigZagBlock, "zigZagBlock");
        int[] sourceNatural = OrderConverter.ZigZagToNatural(zigZagBlock);
        int[] destinationNatural = new int[JpegConstants.BlockSampleCount];

        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                int value = sourceNatural[GetSourceNaturalIndex(x, y, degrees)];
                if (ShouldNegateCoefficient(x, y, degrees))
                {
                    value = NegateCoefficient(value);
                }

                destinationNatural[y * JpegConstants.BlockWidth + x] = value;
                x = x + 1;
            }

            y = y + 1;
        }

        return OrderConverter.NaturalToZigZag(destinationNatural);
    }

    internal int[] FlipBlock(int[] zigZagBlock, bool horizontal)
    {
        OrderConverter.ValidateBlock(zigZagBlock, "zigZagBlock");
        int[] sourceNatural = OrderConverter.ZigZagToNatural(zigZagBlock);
        int[] destinationNatural = new int[JpegConstants.BlockSampleCount];

        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                int value = sourceNatural[y * JpegConstants.BlockWidth + x];
                if (ShouldNegateFlippedCoefficient(x, y, horizontal))
                {
                    value = NegateCoefficient(value);
                }

                destinationNatural[y * JpegConstants.BlockWidth + x] = value;
                x = x + 1;
            }

            y = y + 1;
        }

        return OrderConverter.NaturalToZigZag(destinationNatural);
    }

    internal int[] TransposeBlock(int[] zigZagBlock, bool transverse)
    {
        OrderConverter.ValidateBlock(zigZagBlock, "zigZagBlock");
        int[] sourceNatural = OrderConverter.ZigZagToNatural(zigZagBlock);
        int[] destinationNatural = new int[JpegConstants.BlockSampleCount];

        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                int value = sourceNatural[x * JpegConstants.BlockWidth + y];
                if (transverse && IsOdd(x + y))
                {
                    value = NegateCoefficient(value);
                }

                destinationNatural[y * JpegConstants.BlockWidth + x] = value;
                x = x + 1;
            }

            y = y + 1;
        }

        return OrderConverter.NaturalToZigZag(destinationNatural);
    }

    internal int NegateCoefficient(int value)
    {
        if (value == int.MinValue)
        {
            throw JpegWriteValidationException.Create("Coefficient cannot be negated within the supported integer range.");
        }

        return 0 - value;
    }

    internal int GetSourceNaturalIndex(int x, int y, int degrees)
    {
        if (degrees == 180)
        {
            return y * JpegConstants.BlockWidth + x;
        }

        return x * JpegConstants.BlockWidth + y;
    }

    internal bool ShouldNegateCoefficient(int x, int y, int degrees)
    {
        if (degrees == 90)
        {
            return IsOdd(x);
        }

        if (degrees == 180)
        {
            return IsOdd(x + y);
        }

        return IsOdd(y);
    }

    internal bool ShouldNegateFlippedCoefficient(int x, int y, bool horizontal)
    {
        if (horizontal)
        {
            return IsOdd(x);
        }

        return IsOdd(y);
    }

    internal bool IsOdd(int value)
    {
        return value % 2 != 0;
    }

    internal List<JpegQuantizationTable> RotateQuantizationTables(List<JpegQuantizationTable> source, int degrees)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        List<JpegQuantizationTable> tables = new List<JpegQuantizationTable>();
        int index = 0;
        while (index < source.Count)
        {
            tables.Add(RotateQuantizationTable(source[index], degrees));
            index = index + 1;
        }

        return tables;
    }

    internal JpegQuantizationTable RotateQuantizationTable(JpegQuantizationTable source, int degrees)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        JpegQuantizationTable table = new JpegQuantizationTable();
        table.Identifier = source.Identifier;
        table.Precision = source.Precision;
        table.Is16Bit = source.Is16Bit;
        if (ShouldSwapAxes(degrees))
        {
            TransposeQuantizationValues(source, table);
        }
        else
        {
            CopyIntArray(source.Values, table.Values);
            CopyIntArray(source.ZigZagValues, table.ZigZagValues);
        }

        return table;
    }

    internal void TransposeQuantizationValues(JpegQuantizationTable source, JpegQuantizationTable destination)
    {
        int y = 0;
        while (y < JpegConstants.BlockHeight)
        {
            int x = 0;
            while (x < JpegConstants.BlockWidth)
            {
                int destinationIndex = y * JpegConstants.BlockWidth + x;
                int sourceIndex = x * JpegConstants.BlockWidth + y;
                destination.Values[destinationIndex] = source.Values[sourceIndex];
                x = x + 1;
            }

            y = y + 1;
        }

        int zigZagIndex = 0;
        while (zigZagIndex < JpegConstants.BlockSampleCount)
        {
            int naturalIndex = JpegConstants.ZigZagOrder[zigZagIndex];
            destination.ZigZagValues[zigZagIndex] = destination.Values[naturalIndex];
            zigZagIndex = zigZagIndex + 1;
        }
    }

    internal List<JpegScanHeader> CloneScanHeaders(List<JpegScanHeader>? source)
    {
        List<JpegScanHeader> scans = new List<JpegScanHeader>();
        if (source == null)
        {
            return scans;
        }

        int index = 0;
        while (index < source.Count)
        {
            scans.Add(CloneScanHeader(source[index]));
            index = index + 1;
        }

        return scans;
    }

    internal JpegScanHeader CloneScanHeader(JpegScanHeader source)
    {
        JpegScanHeader scan = new JpegScanHeader();
        scan.ComponentCount = source.ComponentCount;
        scan.SpectralStart = source.SpectralStart;
        scan.SpectralEnd = source.SpectralEnd;
        scan.SuccessiveHigh = source.SuccessiveHigh;
        scan.SuccessiveLow = source.SuccessiveLow;
        int index = 0;
        while (index < source.Components.Count)
        {
            scan.Components.Add(CloneScanComponent(source.Components[index]));
            index = index + 1;
        }

        return scan;
    }

    internal JpegScanComponent CloneScanComponent(JpegScanComponent source)
    {
        JpegScanComponent component = new JpegScanComponent();
        component.Selector = source.Selector;
        component.DcEntropyTableIdentifier = source.DcEntropyTableIdentifier;
        component.AcEntropyTableIdentifier = source.AcEntropyTableIdentifier;
        return component;
    }

    internal List<JpegHuffmanTable> CloneHuffmanTables(List<JpegHuffmanTable>? source)
    {
        List<JpegHuffmanTable> tables = new List<JpegHuffmanTable>();
        if (source == null)
        {
            return tables;
        }

        int index = 0;
        while (index < source.Count)
        {
            tables.Add(CloneHuffmanTable(source[index]));
            index = index + 1;
        }

        return tables;
    }

    internal List<JpegHuffmanTable> CreateTransformHuffmanTables(List<JpegHuffmanTable>? source)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        JpegImageWriter writer = new JpegImageWriter();
        List<JpegHuffmanTable> tables = new List<JpegHuffmanTable>();
        foreach (JpegHuffmanTable table in source)
        {
            tables.Add(writer.CreateComprehensiveHuffmanTable(table.TableClass, table.Identifier, false));
        }

        return tables;
    }

    internal JpegHuffmanTable CloneHuffmanTable(JpegHuffmanTable source)
    {
        JpegHuffmanTable table = new JpegHuffmanTable();
        table.TableClass = source.TableClass;
        table.Identifier = source.Identifier;
        table.SymbolCount = source.SymbolCount;
        table.CodeCounts = CloneIntArray(source.CodeCounts);
        table.Symbols = CloneIntArray(source.Symbols);
        table.Codes = CloneIntArray(source.Codes);
        table.CodeLengths = CloneIntArray(source.CodeLengths);
        table.MinCode = CloneIntArray(source.MinCode);
        table.MaxCode = CloneIntArray(source.MaxCode);
        table.ValueOffset = CloneIntArray(source.ValueOffset);
        return table;
    }

    internal List<JpegDiagnostic> CloneDiagnostics(List<JpegDiagnostic>? source)
    {
        List<JpegDiagnostic> diagnostics = new List<JpegDiagnostic>();
        if (source == null)
        {
            return diagnostics;
        }

        int index = 0;
        while (index < source.Count)
        {
            diagnostics.Add(source[index]);
            index = index + 1;
        }

        return diagnostics;
    }

    internal int[] CloneIntArray(int[]? source)
    {
        if (source == null)
        {
            return new int[0];
        }

        int[] destination = new int[source.Length];
        CopyIntArray(source, destination);
        return destination;
    }

    internal void CopyIntArray(int[] source, int[] destination)
    {
        int length = source.Length;
        if (destination.Length < length)
        {
            length = destination.Length;
        }

        int index = 0;
        while (index < length)
        {
            destination[index] = source[index];
            index = index + 1;
        }
    }

    internal JpegSubsampling RotateSubsampling(JpegSubsampling subsampling, int degrees)
    {
        if (!ShouldSwapAxes(degrees))
        {
            return subsampling;
        }

        if (subsampling == JpegSubsampling.Ratio422)
        {
            return JpegSubsampling.Ratio440;
        }

        if (subsampling == JpegSubsampling.Ratio440)
        {
            return JpegSubsampling.Ratio422;
        }

        if (subsampling == JpegSubsampling.Ratio411)
        {
            return JpegSubsampling.Custom;
        }

        return subsampling;
    }
}
