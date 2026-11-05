namespace qrcode;

/// <summary>Provides stateless luminance conversion and binary image preprocessing.</summary>
public class QrBinarizer : IQrBinarizer
{
    /// <summary>Initializes a reusable stateless QR image binarizer.</summary>
    public QrBinarizer()
    {
    }

    /// <summary>Converts a supported raw image to a tightly packed Gray8 luminance image.</summary>
    /// <param name="image">The caller-owned raw source image.</param>
    /// <param name="options">The preprocessing and resource-limit options.</param>
    /// <returns>A newly allocated Gray8 image that does not share caller memory.</returns>
    public QrImage CreateLuminanceImage(QrImage image, QrDetectionOptions options)
    {
        ValidateOptions(options);
        QrImage luminance = LuminanceConverter.ConvertToGray8(image, options);
        if (options.NormalizeContrast)
        {
            return NormalizeLuminance(luminance);
        }

        return luminance;
    }

    /// <summary>Creates normalized binary matrices for the requested binarization mode and polarity.</summary>
    /// <param name="image">The caller-owned raw or supplied-binary source image.</param>
    /// <param name="options">The binarization, polarity, cleanup, and resource-limit options.</param>
    /// <returns>One matrix for an explicit polarity, or dark-on-light then light-on-dark matrices for automatic polarity.</returns>
    public QrMatrix[] BinarizeImage(QrImage image, QrDetectionOptions options)
    {
        ValidateOptions(options);
        if (image == null)
        {
            throw new QrInputException("The source image is required.", "image");
        }

        if (options.BinarizationMode == QrBinarizationMode.SuppliedBinary
            && image.PixelFormat != QrPixelFormat.Gray8)
        {
            throw new QrInputException(
                "Supplied binary input must use the Gray8 pixel format.",
                "PixelFormat");
        }

        QrImage luminance;
        if (options.BinarizationMode == QrBinarizationMode.SuppliedBinary)
        {
            luminance = LuminanceConverter.ConvertToGray8(image, options);
        }
        else
        {
            luminance = CreateLuminanceImage(image, options);
        }
        QrRaster darkOnLight;
        QrRaster lightOnDark;
        if (options.BinarizationMode == QrBinarizationMode.GlobalHistogram)
        {
            GlobalHistogramResult result = GlobalHistogramBinarizer.Binarize(
                luminance,
                options);
            darkOnLight = result.DarkOnLight;
            lightOnDark = result.LightOnDark;
        }
        else if (options.BinarizationMode == QrBinarizationMode.Automatic
            || options.BinarizationMode == QrBinarizationMode.HybridLocal)
        {
            HybridBinarizerResult result = HybridBinarizer.Binarize(
                luminance,
                options);
            darkOnLight = result.DarkOnLight;
            lightOnDark = result.LightOnDark;
        }
        else
        {
            darkOnLight = CreateSuppliedRaster(luminance);
            lightOnDark = InvertRaster(darkOnLight);
        }

        if (options.Polarity == QrPolarity.DarkOnLight)
        {
            QrRaster cleaned = BinaryMorphology.Clean(darkOnLight, options);
            return new QrMatrix[] { ConvertToMatrix(cleaned) };
        }

        if (options.Polarity == QrPolarity.LightOnDark)
        {
            QrRaster cleaned = BinaryMorphology.Clean(lightOnDark, options);
            return new QrMatrix[] { ConvertToMatrix(cleaned) };
        }

        QrRaster cleanedDarkOnLight = BinaryMorphology.Clean(darkOnLight, options);
        QrRaster cleanedLightOnDark = BinaryMorphology.Clean(lightOnDark, options);
        return new QrMatrix[]
        {
            ConvertToMatrix(cleanedDarkOnLight),
            ConvertToMatrix(cleanedLightOnDark)
        };
    }

    static void ValidateOptions(QrDetectionOptions options)
    {
        if (options == null)
        {
            throw new QrInputException("The detection options are required.", "options");
        }

        int binarizationMode = (int)options.BinarizationMode;
        if (binarizationMode < (int)QrBinarizationMode.Automatic
            || binarizationMode > (int)QrBinarizationMode.SuppliedBinary)
        {
            throw new QrInputException(
                "The binarization mode is not defined.",
                "BinarizationMode");
        }

        int polarity = (int)options.Polarity;
        if (polarity < (int)QrPolarity.Automatic
            || polarity > (int)QrPolarity.LightOnDark)
        {
            throw new QrInputException("The polarity is not defined.", "Polarity");
        }
    }

    static QrImage NormalizeLuminance(QrImage luminance)
    {
        int minimum = 255;
        int maximum = 0;
        for (int index = 0; index < luminance.Pixels.Length; index++)
        {
            int value = luminance.Pixels[index];
            if (value < minimum)
            {
                minimum = value;
            }

            if (value > maximum)
            {
                maximum = value;
            }
        }

        QrImage result = new QrImage
        {
            Pixels = new byte[luminance.Pixels.Length],
            Width = luminance.Width,
            Height = luminance.Height,
            Stride = luminance.Width,
            PixelFormat = QrPixelFormat.Gray8
        };
        if (minimum == maximum)
        {
            for (int index = 0; index < luminance.Pixels.Length; index++)
            {
                result.Pixels[index] = luminance.Pixels[index];
            }

            return result;
        }

        int range = maximum - minimum;
        for (int index = 0; index < luminance.Pixels.Length; index++)
        {
            int adjusted = luminance.Pixels[index] - minimum;
            result.Pixels[index] = (byte)((adjusted * 255 + range / 2) / range);
        }

        return result;
    }

    static QrRaster CreateSuppliedRaster(QrImage luminance)
    {
        QrRaster raster = new QrRaster
        {
            Pixels = new byte[luminance.Pixels.Length],
            Width = luminance.Width,
            Height = luminance.Height,
            Stride = luminance.Width
        };
        for (int index = 0; index < luminance.Pixels.Length; index++)
        {
            byte value = luminance.Pixels[index];
            if (value != 0 && value != 255)
            {
                throw new QrInputException(
                    "Supplied binary pixels must be either 0 or 255.",
                    "Pixels");
            }

            raster.Pixels[index] = value;
        }

        return raster;
    }

    static QrRaster InvertRaster(QrRaster source)
    {
        QrRaster result = new QrRaster
        {
            Pixels = new byte[source.Pixels.Length],
            Width = source.Width,
            Height = source.Height,
            Stride = source.Width
        };
        for (int index = 0; index < source.Pixels.Length; index++)
        {
            result.Pixels[index] = source.Pixels[index] == 0 ? (byte)255 : (byte)0;
        }

        return result;
    }

    static QrMatrix ConvertToMatrix(QrRaster raster)
    {
        int pixelCount = CheckedDimensions.CalculateArea(
            raster.Width,
            raster.Height,
            int.MaxValue);
        CheckedDimensions.CalculateAllocationBytes(
            pixelCount,
            4,
            QrLimits.DefaultMaximumAllocationBytes);
        QrMatrix matrix = new QrMatrix
        {
            Width = raster.Width,
            Height = raster.Height,
            Values = new QrModuleValue[pixelCount]
        };
        for (int row = 0; row < raster.Height; row++)
        {
            int rowOffset = row * raster.Stride;
            for (int column = 0; column < raster.Width; column++)
            {
                int index = row * raster.Width + column;
                matrix.Values[index] = raster.Pixels[rowOffset + column] == 0
                    ? QrModuleValue.Dark
                    : QrModuleValue.Light;
            }
        }

        return matrix;
    }
}
