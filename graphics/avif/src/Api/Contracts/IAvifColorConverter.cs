namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifColorConverter"/>.</summary>
public interface IAvifColorConverter
{
    /// <summary>Returns an independent copy of the active options.</summary>
    /// <returns>The current conversion settings.</returns>
    AvifColorConversionOptions? GetOptions();

    /// <summary>Replaces the active conversion settings.</summary>
    /// <param name="options">The settings to validate and copy.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetOptions(AvifColorConversionOptions? options);

    /// <summary>Converts a planar image to a newly allocated eight-bit packed buffer.</summary>
    /// <param name="source">The planar source image.</param>
    /// <param name="format">The requested packed component order.</param>
    /// <returns>The converted buffer or a buffer carrying an error code.</returns>
    AvifPixelBuffer Convert(
        AvifImage source,
        AvifPixelFormat format);

    /// <summary>Converts a planar image to a newly allocated packed buffer.</summary>
    /// <param name="source">The planar source image.</param>
    /// <param name="format">The requested packed component order.</param>
    /// <param name="bitDepth">The integer depth, or 16 for half float.</param>
    /// <param name="floatingPoint">Whether RGB components are linear half floats.</param>
    /// <returns>The converted buffer or a buffer carrying an error code.</returns>
    AvifPixelBuffer ConvertWithDepth(
        AvifImage source,
        AvifPixelFormat format,
        int bitDepth,
        bool floatingPoint);

    /// <summary>Converts planar image samples into a caller-selected packed destination.</summary>
    /// <param name="source">The planar source image.</param>
    /// <param name="destination">The compatible packed destination.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode ConvertToPixelBuffer(
        AvifImage source,
        AvifPixelBuffer destination);

    /// <summary>Converts packed RGB samples into a caller-selected planar destination.</summary>
    /// <param name="source">The packed RGB source.</param>
    /// <param name="destination">The planar destination and its color signaling.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode ConvertToImage(
        AvifPixelBuffer source,
        AvifImage destination);

    /// <summary>Applies an ISO 21496-1 gain map and returns linear BT.709 half-float RGBA.</summary>
    /// <param name="baseImage">The decoded base image.</param>
    /// <param name="gainMap">The decoded gain map and metadata.</param>
    /// <param name="displayHeadroom">The display HDR-to-reference-white ratio, at least one.</param>
    /// <returns>An owning linear half-float buffer, or a buffer carrying a stable error code.</returns>
    AvifPixelBuffer ApplyGainMap(
        AvifImage baseImage,
        AvifGainMap gainMap,
        float displayHeadroom);

    /// <summary>Applies a gain map with explicit output allocation limits.</summary>
    /// <param name="baseImage">The decoded base image.</param>
    /// <param name="gainMap">The decoded gain map and metadata.</param>
    /// <param name="displayHeadroom">The display HDR-to-reference-white ratio, at least one.</param>
    /// <param name="limits">The output allocation and dimension limits.</param>
    /// <returns>An owning linear half-float buffer, or a buffer carrying a stable error code.</returns>
    AvifPixelBuffer ApplyGainMapWithLimits(
        AvifImage baseImage,
        AvifGainMap gainMap,
        float displayHeadroom,
        AvifSecurityLimits limits);

    /// <summary>Computes an eight-bit full-resolution color gain map.</summary>
    /// <param name="baseImage">The decoded base representation.</param>
    /// <param name="alternateImage">The decoded alternate representation.</param>
    /// <returns>An owning gain map with inverse metadata, or an object carrying an error code.</returns>
    AvifGainMap ComputeGainMap(
        AvifImage baseImage,
        AvifImage alternateImage);

    /// <summary>Computes a gain map using caller-selected encoded image characteristics.</summary>
    /// <param name="baseImage">The decoded base representation.</param>
    /// <param name="alternateImage">The decoded alternate representation.</param>
    /// <param name="gainMapWidth">The positive encoded gain-map width.</param>
    /// <param name="gainMapHeight">The positive encoded gain-map height.</param>
    /// <param name="gainMapBitDepth">The encoded depth: 8, 10, 12, or 16.</param>
    /// <param name="gainMapChromaFormat">The monochrome or YUV encoded layout.</param>
    /// <param name="limits">The source, output, and temporary allocation limits.</param>
    /// <returns>An owning gain map with inverse metadata, or an object carrying an error code.</returns>
    AvifGainMap ComputeGainMapWithEncoding(
        AvifImage baseImage,
        AvifImage alternateImage,
        int gainMapWidth,
        int gainMapHeight,
        int gainMapBitDepth,
        AvifChromaFormat gainMapChromaFormat,
        AvifSecurityLimits limits);
}
