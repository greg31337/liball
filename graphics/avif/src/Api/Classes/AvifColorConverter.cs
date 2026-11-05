namespace avif;

/// <summary>
/// Converts between planar AVIF image samples and common packed RGB pixel layouts.
/// </summary>
public class AvifColorConverter : IAvifColorConverter, IAvifDiagnosticSource
{
    AvifColorConversionOptions? options;
    AvifResultCode code;

    /// <summary>Creates a converter with deterministic default options.</summary>
    /// <returns>The initialized color converter.</returns>
    internal static AvifColorConverter Create()
    {
        return new AvifColorConverter(new AvifColorConversionOptions());
    }

    /// <summary>Creates a converter using a copy of the supplied options.</summary>
    /// <param name="options">The conversion settings to copy.</param>
    public AvifColorConverter(AvifColorConversionOptions? options)
    {
        this.options = CopyOptions(options);
        code = IsValidOptions(this.options)
            ? AvifResultCode.Success : AvifResultCode.InvalidArgument;
    }

    /// <summary>Creates a converter using a copy of supplied options.</summary>
    /// <returns>The initialized color converter.</returns>
    internal static AvifColorConverter Create(
        AvifColorConversionOptions? options)
    {
        return new AvifColorConverter(options);
    }

    /// <summary>Returns an independent copy of the active options.</summary>
    /// <returns>The current conversion settings.</returns>
    public AvifColorConversionOptions? GetOptions()
    {
        return CopyOptions(options);
    }

    /// <summary>Returns diagnostics for the most recent color conversion operation.</summary>
    /// <returns>The structured color diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            code,
            AvifDiagnosticCategory.Color,
            "The color conversion operation did not complete successfully.");
    }

    /// <summary>Replaces the active conversion settings.</summary>
    /// <param name="options">The settings to validate and copy.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode SetOptions(AvifColorConversionOptions? options)
    {
        if (!IsValidOptions(options))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        this.options = CopyOptions(options);
        code = AvifResultCode.Success;
        return code;
    }

    /// <summary>Converts a planar image to a newly allocated eight-bit packed buffer.</summary>
    /// <param name="source">The planar source image.</param>
    /// <param name="format">The requested packed component order.</param>
    /// <returns>The converted buffer or a buffer carrying an error code.</returns>
    public AvifPixelBuffer Convert(
        AvifImage source,
        AvifPixelFormat format)
    {
        return ConvertWithDepth(source, format, 8, false);
    }

    /// <summary>Converts a planar image to a newly allocated packed buffer.</summary>
    /// <param name="source">The planar source image.</param>
    /// <param name="format">The requested packed component order.</param>
    /// <param name="bitDepth">The integer depth, or 16 for half float.</param>
    /// <param name="floatingPoint">Whether RGB components are linear half floats.</param>
    /// <returns>The converted buffer or a buffer carrying an error code.</returns>
    public AvifPixelBuffer ConvertWithDepth(
        AvifImage source,
        AvifPixelFormat format,
        int bitDepth,
        bool floatingPoint)
    {
        if (source == null)
        {
            code = AvifResultCode.InvalidArgument;
            return AvifPixelBuffer.FromCode(code);
        }
        AvifResultCode sourceState = source.GetCode();
        if (sourceState != AvifResultCode.Success)
        {
            code = sourceState;
            return AvifPixelBuffer.FromCode(sourceState);
        }
        AvifPixelBuffer destination = AvifPixelBuffer.Create(
            source.GetWidth(),
            source.GetHeight(),
            format,
            bitDepth,
            floatingPoint);
        if (destination.GetCode() != AvifResultCode.Success)
        {
            code = destination.GetCode();
            return destination;
        }
        AvifResultCode result = ConvertToPixelBuffer(source, destination);
        if (result != AvifResultCode.Success)
        {
            destination.Close();
            code = result;
            return AvifPixelBuffer.FromCode(result);
        }
        code = AvifResultCode.Success;
        return destination;
    }

    /// <summary>Converts planar image samples into a caller-selected packed destination.</summary>
    /// <param name="source">The planar source image.</param>
    /// <param name="destination">The compatible packed destination.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode ConvertToPixelBuffer(
        AvifImage source,
        AvifPixelBuffer destination)
    {
        if (!IsValidOptions(options))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        code = YuvToRgb.Convert(source, destination, options);
        return code;
    }

    /// <summary>Converts packed RGB samples into a caller-selected planar destination.</summary>
    /// <param name="source">The packed RGB source.</param>
    /// <param name="destination">The planar destination and its color signaling.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode ConvertToImage(
        AvifPixelBuffer source,
        AvifImage destination)
    {
        if (!IsValidOptions(options))
        {
            code = AvifResultCode.InvalidArgument;
            return code;
        }
        code = RgbToYuv.Convert(source, destination, options);
        return code;
    }

    /// <summary>Applies an ISO 21496-1 gain map and returns linear BT.709 half-float RGBA.</summary>
    /// <param name="baseImage">The decoded base image.</param>
    /// <param name="gainMap">The decoded gain map and metadata.</param>
    /// <param name="displayHeadroom">The display HDR-to-reference-white ratio, at least one.</param>
    /// <returns>An owning linear half-float buffer, or a buffer carrying a stable error code.</returns>
    public AvifPixelBuffer ApplyGainMap(
        AvifImage baseImage,
        AvifGainMap gainMap,
        float displayHeadroom)
    {
        AvifPixelBuffer result = GainMapRenderer.Apply(
            baseImage,
            gainMap,
            displayHeadroom,
            options,
            new AvifSecurityLimits());
        code = result.GetCode();
        return result;
    }

    /// <summary>Applies a gain map with explicit output allocation limits.</summary>
    /// <param name="baseImage">The decoded base image.</param>
    /// <param name="gainMap">The decoded gain map and metadata.</param>
    /// <param name="displayHeadroom">The display HDR-to-reference-white ratio, at least one.</param>
    /// <param name="limits">The output allocation and dimension limits.</param>
    /// <returns>An owning linear half-float buffer, or a buffer carrying a stable error code.</returns>
    public AvifPixelBuffer ApplyGainMapWithLimits(
        AvifImage baseImage,
        AvifGainMap gainMap,
        float displayHeadroom,
        AvifSecurityLimits limits)
    {
        AvifPixelBuffer result = GainMapRenderer.Apply(
            baseImage,
            gainMap,
            displayHeadroom,
            options,
            limits);
        code = result.GetCode();
        return result;
    }

    /// <summary>Computes an eight-bit full-resolution color gain map.</summary>
    /// <param name="baseImage">The decoded base representation.</param>
    /// <param name="alternateImage">The decoded alternate representation.</param>
    /// <returns>An owning gain map with inverse metadata, or an object carrying an error code.</returns>
    public AvifGainMap ComputeGainMap(
        AvifImage baseImage,
        AvifImage alternateImage)
    {
        int width = baseImage == null ? 0 : baseImage.GetWidth();
        int height = baseImage == null ? 0 : baseImage.GetHeight();
        AvifGainMap result = GainMapGenerator.Generate(
            baseImage,
            alternateImage,
            width,
            height,
            8,
            AvifChromaFormat.Yuv444,
            options,
            new AvifSecurityLimits());
        code = result.GetCode();
        return result;
    }

    /// <summary>Computes a gain map using caller-selected encoded image characteristics.</summary>
    /// <param name="baseImage">The decoded base representation.</param>
    /// <param name="alternateImage">The decoded alternate representation.</param>
    /// <param name="gainMapWidth">The positive encoded gain-map width.</param>
    /// <param name="gainMapHeight">The positive encoded gain-map height.</param>
    /// <param name="gainMapBitDepth">The encoded depth: 8, 10, 12, or 16.</param>
    /// <param name="gainMapChromaFormat">The monochrome or YUV encoded layout.</param>
    /// <param name="limits">The source, output, and temporary allocation limits.</param>
    /// <returns>An owning gain map with inverse metadata, or an object carrying an error code.</returns>
    public AvifGainMap ComputeGainMapWithEncoding(
        AvifImage baseImage,
        AvifImage alternateImage,
        int gainMapWidth,
        int gainMapHeight,
        int gainMapBitDepth,
        AvifChromaFormat gainMapChromaFormat,
        AvifSecurityLimits limits)
    {
        AvifGainMap result = GainMapGenerator.Generate(
            baseImage,
            alternateImage,
            gainMapWidth,
            gainMapHeight,
            gainMapBitDepth,
            gainMapChromaFormat,
            options,
            limits);
        code = result.GetCode();
        return result;
    }

    static AvifColorConversionOptions? CopyOptions(
        AvifColorConversionOptions? source)
    {
        if (source == null)
        {
            return null;
        }
        AvifColorConversionOptions copy = new AvifColorConversionOptions();
        copy.ResamplingFilter = source.ResamplingFilter;
        copy.DitherMode = source.DitherMode;
        copy.UseEmbeddedIccProfile = source.UseEmbeddedIccProfile;
        copy.ClampOutOfGamut = source.ClampOutOfGamut;
        copy.PreserveAlpha = source.PreserveAlpha;
        copy.OutputPremultipliedAlpha = source.OutputPremultipliedAlpha;
        copy.WorkerCount = source.WorkerCount;
        return copy;
    }

    static bool IsValidOptions(AvifColorConversionOptions? value)
    {
        if (value == null || value.WorkerCount <= 0)
        {
            return false;
        }
        bool validFilter = value.ResamplingFilter
                == AvifResamplingFilter.Nearest
            || value.ResamplingFilter == AvifResamplingFilter.Bilinear
            || value.ResamplingFilter == AvifResamplingFilter.Bicubic
            || value.ResamplingFilter == AvifResamplingFilter.Lanczos3;
        bool validDither = value.DitherMode == AvifDitherMode.None
            || value.DitherMode == AvifDitherMode.Ordered
            || value.DitherMode == AvifDitherMode.ErrorDiffusion;
        return validFilter && validDither;
    }
}
