namespace png;

/// <summary>Default service for PNG pixel, palette, sample, and color transformations.</summary>
public class PngTransformer : IPngTransformer
{
    /// <summary>Creates an independent PNG transformer.</summary>
    public PngTransformer()
    {
    }

    /// <summary>Converts pixels to any supported native PNG pixel format.</summary><param name="buffer">Source.</param><param name="options">Conversion options.</param><returns>The converted pixels.</returns>
    public PngPixelBuffer ConvertPixelFormat(PngPixelBuffer buffer, PngPixelConversionOptions options) { return PngPixelFormatConverter.Convert(buffer, options); }
    /// <summary>Adds opaque alpha samples.</summary><param name="buffer">The source pixels.</param><returns>The transformed pixels.</returns>
    public PngPixelBuffer AddOpaqueAlpha(PngPixelBuffer buffer) { return PngAlphaTransformer.AddOpaqueAlpha(buffer); }
    /// <summary>Removes alpha samples.</summary><param name="buffer">The source pixels.</param><returns>The transformed pixels.</returns>
    public PngPixelBuffer StripAlpha(PngPixelBuffer buffer) { return PngAlphaTransformer.StripAlpha(buffer); }
    /// <summary>Removes alpha samples when all are opaque.</summary><param name="buffer">The source pixels.</param><returns>The original or transformed pixels.</returns>
    public PngPixelBuffer StripAlphaIfOpaque(PngPixelBuffer buffer) { return PngAlphaTransformer.StripAlphaIfOpaque(buffer); }
    /// <summary>Determines whether every alpha sample is opaque.</summary><param name="buffer">The source pixels.</param><returns><c>true</c> when opaque.</returns>
    public bool IsOpaque(PngPixelBuffer buffer) { return PngAlphaTransformer.IsOpaque(buffer); }
    /// <summary>Inverts alpha samples.</summary><param name="buffer">The source pixels.</param><returns>The transformed pixels.</returns>
    public PngPixelBuffer InvertAlpha(PngPixelBuffer buffer) { return PngAlphaTransformer.InvertAlpha(buffer); }
    /// <summary>Composites pixels over a background.</summary><param name="buffer">The source pixels.</param><param name="background">The background.</param><returns>The composited pixels.</returns>
    public PngPixelBuffer CompositeOverBackground(PngPixelBuffer buffer, PngBackgroundColor background) { return PngAlphaTransformer.CompositeOverBackground(buffer, background); }
    /// <summary>Premultiplies color samples by alpha.</summary><param name="buffer">The source pixels.</param><returns>The transformed pixels.</returns>
    public PngPixelBuffer PremultiplyAlpha(PngPixelBuffer buffer) { return PngAlphaTransformer.PremultiplyAlpha(buffer); }
    /// <summary>Removes alpha premultiplication.</summary><param name="buffer">The source pixels.</param><returns>The transformed pixels.</returns>
    public PngPixelBuffer UnpremultiplyAlpha(PngPixelBuffer buffer) { return PngAlphaTransformer.UnpremultiplyAlpha(buffer); }
    /// <summary>Clears colors under transparent pixels.</summary><param name="buffer">The source pixels.</param><returns>The transformed pixels.</returns>
    public PngPixelBuffer ClearTransparentPixelColors(PngPixelBuffer buffer) { return PngAlphaTransformer.ClearTransparentPixelColors(buffer); }
    /// <summary>Converts between RGB/BGR or RGBA/BGRA order.</summary><param name="buffer">The source pixels.</param><returns>The transformed pixels with an accurate reordered format.</returns>
    public PngPixelBuffer SwapRgbBgr(PngPixelBuffer buffer) { return PngChannelTransformer.SwapRgbBgr(buffer); }
    /// <summary>Converts between RGBA and ARGB order.</summary><param name="buffer">The source pixels.</param><returns>The transformed pixels with an accurate reordered format.</returns>
    public PngPixelBuffer SwapRgbaArgb(PngPixelBuffer buffer) { return PngChannelTransformer.SwapRgbaArgb(buffer); }
    /// <summary>Flips pixels vertically.</summary><param name="buffer">The source pixels.</param><returns>The transformed pixels.</returns>
    public PngPixelBuffer FlipVertical(PngPixelBuffer buffer) { return PngGeometryTransformer.FlipVertical(buffer); }
    /// <summary>Crops pixels.</summary><param name="buffer">The source pixels.</param><param name="x">Left.</param><param name="y">Top.</param><param name="width">Width.</param><param name="height">Height.</param><returns>The cropped pixels.</returns>
    public PngPixelBuffer Crop(PngPixelBuffer buffer, int x, int y, int width, int height) { return PngGeometryTransformer.Crop(buffer, x, y, width, height); }
    /// <summary>Pads a canvas.</summary><param name="buffer">The source pixels.</param><param name="left">Left padding.</param><param name="top">Top padding.</param><param name="right">Right padding.</param><param name="bottom">Bottom padding.</param><returns>The padded pixels.</returns>
    public PngPixelBuffer PadCanvas(PngPixelBuffer buffer, int left, int top, int right, int bottom) { return PngGeometryTransformer.PadCanvas(buffer, left, top, right, bottom); }
    /// <summary>Copies a rectangle.</summary><param name="source">Source.</param><param name="sourceX">Source X.</param><param name="sourceY">Source Y.</param><param name="destination">Destination.</param><param name="destinationX">Destination X.</param><param name="destinationY">Destination Y.</param><param name="width">Width.</param><param name="height">Height.</param>
    public void CopyRectangle(PngPixelBuffer source, int sourceX, int sourceY, PngPixelBuffer destination, int destinationX, int destinationY, int width, int height) { PngGeometryTransformer.CopyRectangle(source, sourceX, sourceY, destination, destinationX, destinationY, width, height); }
    /// <summary>Resizes with nearest-neighbor sampling.</summary><param name="buffer">Source.</param><param name="width">Width.</param><param name="height">Height.</param><returns>The resized pixels.</returns>
    public PngPixelBuffer ResizeNearestNeighbor(PngPixelBuffer buffer, int width, int height) { return PngGeometryTransformer.ResizeNearestNeighbor(buffer, width, height); }
    /// <summary>Resizes with bilinear sampling.</summary><param name="buffer">Source.</param><param name="width">Width.</param><param name="height">Height.</param><returns>The resized pixels.</returns>
    public PngPixelBuffer ResizeBilinear(PngPixelBuffer buffer, int width, int height) { return PngGeometryTransformer.ResizeBilinear(buffer, width, height); }
    /// <summary>Expands grayscale or grayscale-alpha to RGB or RGBA, preserving alpha.</summary><param name="buffer">Source.</param><returns>The expanded pixels.</returns>
    public PngPixelBuffer ExpandGrayscaleToRgb(PngPixelBuffer buffer) { return PngGrayscaleExpander.ExpandToRgb(buffer); }
    /// <summary>Expands grayscale or grayscale-alpha to RGB8 or RGBA8, preserving alpha.</summary><param name="buffer">Source.</param><returns>The expanded pixels.</returns>
    public PngPixelBuffer ExpandGrayscaleToRgb8(PngPixelBuffer buffer) { return PngGrayscaleExpander.ExpandToRgb8(buffer); }
    /// <summary>Expands packed grayscale to grayscale8.</summary><param name="buffer">Source.</param><returns>The expanded pixels.</returns>
    public PngPixelBuffer ExpandLowBitToGrayscale8(PngPixelBuffer buffer) { return PngGrayscaleExpander.ExpandLowBitToGrayscale8(buffer); }
    /// <summary>Tests for packed grayscale.</summary><param name="pixelFormat">Format.</param><returns><c>true</c> when packed grayscale.</returns>
    public bool IsLowBitGrayscaleFormat(PngPixelFormat pixelFormat) { return PngGrayscaleExpander.IsLowBitGrayscaleFormat(pixelFormat); }
    /// <summary>Inverts monochrome samples.</summary><param name="buffer">Source.</param><returns>The transformed pixels.</returns>
    public PngPixelBuffer InvertMonochrome(PngPixelBuffer buffer) { return PngMonochromeTransformer.Invert(buffer); }
    /// <summary>Expands indexed pixels with a palette.</summary><param name="buffer">Source.</param><param name="palette">Palette.</param><returns>The RGB8 pixels.</returns>
    public PngPixelBuffer ExpandPaletteToRgb8(PngPixelBuffer buffer, PngPalette palette) { return PngPaletteExpander.ExpandToRgb8(buffer, palette); }
    /// <summary>Expands indexed pixels with a PLTE chunk.</summary><param name="buffer">Source.</param><param name="paletteChunk">PLTE chunk.</param><returns>The RGB8 pixels.</returns>
    public PngPixelBuffer ExpandPaletteChunkToRgb8(PngPixelBuffer buffer, PngChunk paletteChunk) { return PngPaletteExpander.ExpandToRgb8(buffer, paletteChunk); }
    /// <summary>Generates a palette.</summary><param name="buffer">Source.</param><returns>The palette.</returns>
    public PngPalette GeneratePalette(PngPixelBuffer buffer) { return PngPaletteGenerator.Generate(buffer); }
    /// <summary>Unpacks indexed pixels to indexed8.</summary><param name="buffer">Source.</param><returns>The indexed8 pixels.</returns>
    public PngPixelBuffer ToIndexed8(PngPixelBuffer buffer) { return PngPaletteIndexPixelConverter.ToIndexed8(buffer); }
    /// <summary>Tests for indexed pixels.</summary><param name="pixelFormat">Format.</param><returns><c>true</c> when indexed.</returns>
    public bool IsIndexedFormat(PngPixelFormat pixelFormat) { return PngPaletteIndexPixelConverter.IsIndexedFormat(pixelFormat); }
    /// <summary>Trims a palette.</summary><param name="buffer">Source.</param><param name="palette">Palette.</param><returns>The trimmed palette.</returns>
    public PngPalette TrimPalette(PngPixelBuffer buffer, PngPalette palette) { return PngPaletteTrimmer.Trim(buffer, palette); }
    /// <summary>Converts RGB to grayscale.</summary><param name="buffer">Source.</param><returns>The grayscale pixels.</returns>
    public PngPixelBuffer ConvertRgbToGrayscale(PngPixelBuffer buffer) { return PngRgbToGrayscaleConverter.Convert(buffer); }
    /// <summary>Converts RGB to grayscale when possible.</summary><param name="buffer">Source.</param><returns>The original or converted pixels.</returns>
    public PngPixelBuffer ConvertRgbToGrayscaleIfPossible(PngPixelBuffer buffer) { return PngRgbToGrayscaleConverter.ConvertIfGrayscale(buffer); }
    /// <summary>Tests whether all colors are gray.</summary><param name="buffer">Source.</param><returns><c>true</c> when gray.</returns>
    public bool IsGrayscale(PngPixelBuffer buffer) { return PngRgbToGrayscaleConverter.IsGrayscale(buffer); }
    /// <summary>Calculates 8-bit luminance.</summary><param name="red">Red.</param><param name="green">Green.</param><param name="blue">Blue.</param><returns>Luminance.</returns>
    public byte CalculateLuminance8(byte red, byte green, byte blue) { return PngRgbToGrayscaleConverter.CalculateLuminance8(red, green, blue); }
    /// <summary>Calculates 16-bit luminance.</summary><param name="red">Red.</param><param name="green">Green.</param><param name="blue">Blue.</param><returns>Luminance.</returns>
    public ushort CalculateLuminance16(ushort red, ushort green, ushort blue) { return PngRgbToGrayscaleConverter.CalculateLuminance16(red, green, blue); }
    /// <summary>Swaps 16-bit sample bytes.</summary><param name="buffer">Source.</param><returns>The transformed pixels.</returns>
    public PngPixelBuffer ByteSwap16BitSamples(PngPixelBuffer buffer) { return PngSampleTransformer.ByteSwap16BitSamples(buffer); }
    /// <summary>Reduces 16-bit samples when lossless.</summary><param name="buffer">Source.</param><returns>The original or reduced pixels.</returns>
    public PngPixelBuffer Reduce16BitTo8BitIfLossless(PngPixelBuffer buffer) { return PngSampleTransformer.Reduce16BitTo8BitIfLossless(buffer); }
    /// <summary>Tests lossless 16-bit reduction.</summary><param name="buffer">Source.</param><returns><c>true</c> when lossless.</returns>
    public bool CanReduce16BitTo8Bit(PngPixelBuffer buffer) { return PngSampleTransformer.CanReduce16BitTo8Bit(buffer); }
    /// <summary>Downshifts significant bits.</summary><param name="buffer">Source.</param><param name="bits">Metadata.</param><returns>The transformed pixels.</returns>
    public PngPixelBuffer DownshiftToSignificantBits(PngPixelBuffer buffer, PngSignificantBits bits) { return PngSampleTransformer.DownshiftToSignificantBits(buffer, bits); }
    /// <summary>Upscales significant bits.</summary><param name="buffer">Source.</param><param name="bits">Metadata.</param><returns>The transformed pixels.</returns>
    public PngPixelBuffer UpscaleFromSignificantBits(PngPixelBuffer buffer, PngSignificantBits bits) { return PngSampleTransformer.UpscaleFromSignificantBits(buffer, bits); }
    /// <summary>Expands indexed transparency.</summary><param name="buffer">Source.</param><param name="palette">Palette.</param><param name="transparency">Transparency.</param><returns>The RGBA8 pixels.</returns>
    public PngPixelBuffer ExpandIndexedTransparency(PngPixelBuffer buffer, PngPalette palette, PngIndexedTransparency transparency) { return PngTransparencyExpander.ExpandIndexedToRgba8(buffer, palette, transparency); }
    /// <summary>Expands indexed transparency chunks.</summary><param name="buffer">Source.</param><param name="paletteChunk">PLTE.</param><param name="transparencyChunk">tRNS.</param><returns>The RGBA8 pixels.</returns>
    public PngPixelBuffer ExpandIndexedTransparencyChunks(PngPixelBuffer buffer, PngChunk paletteChunk, PngChunk transparencyChunk) { return PngTransparencyExpander.ExpandIndexedToRgba8(buffer, paletteChunk, transparencyChunk); }
    /// <summary>Expands grayscale transparency.</summary><param name="buffer">Source.</param><param name="transparency">Transparency.</param><returns>The pixels with alpha.</returns>
    public PngPixelBuffer ExpandGrayscaleTransparency(PngPixelBuffer buffer, PngGrayscaleTransparency transparency) { return PngTransparencyExpander.ExpandGrayscaleToAlpha(buffer, transparency); }
    /// <summary>Expands a grayscale transparency chunk.</summary><param name="buffer">Source.</param><param name="transparencyChunk">tRNS.</param><returns>The pixels with alpha.</returns>
    public PngPixelBuffer ExpandGrayscaleTransparencyChunk(PngPixelBuffer buffer, PngChunk transparencyChunk) { return PngTransparencyExpander.ExpandGrayscaleToAlpha(buffer, transparencyChunk); }
    /// <summary>Expands truecolor transparency.</summary><param name="buffer">Source.</param><param name="transparency">Transparency.</param><returns>The pixels with alpha.</returns>
    public PngPixelBuffer ExpandTruecolorTransparency(PngPixelBuffer buffer, PngTruecolorTransparency transparency) { return PngTransparencyExpander.ExpandTruecolorToAlpha(buffer, transparency); }
    /// <summary>Expands a truecolor transparency chunk.</summary><param name="buffer">Source.</param><param name="transparencyChunk">tRNS.</param><returns>The pixels with alpha.</returns>
    public PngPixelBuffer ExpandTruecolorTransparencyChunk(PngPixelBuffer buffer, PngChunk transparencyChunk) { return PngTransparencyExpander.ExpandTruecolorToAlpha(buffer, transparencyChunk); }
    /// <summary>Builds an 8-bit gamma table.</summary><param name="gamma">Gamma.</param><param name="displayExponent">Display exponent.</param><returns>The table.</returns>
    public byte[] Build8BitGammaTable(PngGamma gamma, double displayExponent) { return PngGammaCorrection.Build8BitTable(gamma, displayExponent); }
    /// <summary>Builds an 8-bit gamma table from a value.</summary><param name="pngGammaValue">Gamma value.</param><param name="displayExponent">Display exponent.</param><returns>The table.</returns>
    public byte[] Build8BitGammaTableFromValue(uint pngGammaValue, double displayExponent) { return PngGammaCorrection.Build8BitTable(pngGammaValue, displayExponent); }
    /// <summary>Builds an 8-bit gamma table from an exponent.</summary><param name="imageGamma">Image exponent.</param><param name="displayExponent">Display exponent.</param><returns>The table.</returns>
    public byte[] Build8BitGammaTableFromExponent(double imageGamma, double displayExponent) { return PngGammaCorrection.Build8BitTable(imageGamma, displayExponent); }
    /// <summary>Builds an 8-bit gamma table with a user exponent.</summary><param name="imageGamma">Image exponent.</param><param name="displayExponent">Display exponent.</param><param name="userExponent">User exponent.</param><returns>The table.</returns>
    public byte[] Build8BitGammaTableWithUserExponent(double imageGamma, double displayExponent, double userExponent) { return PngGammaCorrection.Build8BitTable(imageGamma, displayExponent, userExponent); }
    /// <summary>Builds an identity 8-bit gamma table.</summary><returns>The table.</returns>
    public byte[] BuildIdentity8BitGammaTable() { return PngGammaCorrection.BuildIdentity8BitTable(); }
    /// <summary>Builds a 16-bit gamma table.</summary><param name="gamma">Gamma.</param><param name="displayExponent">Display exponent.</param><returns>The table.</returns>
    public ushort[] Build16BitGammaTable(PngGamma gamma, double displayExponent) { return PngGammaCorrection.Build16BitTable(gamma, displayExponent); }
    /// <summary>Builds a 16-bit gamma table from a value.</summary><param name="pngGammaValue">Gamma value.</param><param name="displayExponent">Display exponent.</param><returns>The table.</returns>
    public ushort[] Build16BitGammaTableFromValue(uint pngGammaValue, double displayExponent) { return PngGammaCorrection.Build16BitTable(pngGammaValue, displayExponent); }
    /// <summary>Builds a 16-bit gamma table from an exponent.</summary><param name="imageGamma">Image exponent.</param><param name="displayExponent">Display exponent.</param><returns>The table.</returns>
    public ushort[] Build16BitGammaTableFromExponent(double imageGamma, double displayExponent) { return PngGammaCorrection.Build16BitTable(imageGamma, displayExponent); }
    /// <summary>Builds a 16-bit gamma table with a user exponent.</summary><param name="imageGamma">Image exponent.</param><param name="displayExponent">Display exponent.</param><param name="userExponent">User exponent.</param><returns>The table.</returns>
    public ushort[] Build16BitGammaTableWithUserExponent(double imageGamma, double displayExponent, double userExponent) { return PngGammaCorrection.Build16BitTable(imageGamma, displayExponent, userExponent); }
    /// <summary>Builds an identity 16-bit gamma table.</summary><returns>The table.</returns>
    public ushort[] BuildIdentity16BitGammaTable() { return PngGammaCorrection.BuildIdentity16BitTable(); }
    /// <summary>Applies an 8-bit gamma table.</summary><param name="buffer">Source.</param><param name="table">Table.</param><returns>The corrected pixels.</returns>
    public PngPixelBuffer Apply8BitGammaTable(PngPixelBuffer buffer, byte[] table) { return PngGammaCorrection.Apply8BitTable(buffer, table); }
    /// <summary>Applies a 16-bit gamma table.</summary><param name="buffer">Source.</param><param name="table">Table.</param><returns>The corrected pixels.</returns>
    public PngPixelBuffer Apply16BitGammaTable(PngPixelBuffer buffer, ushort[] table) { return PngGammaCorrection.Apply16BitTable(buffer, table); }
}
