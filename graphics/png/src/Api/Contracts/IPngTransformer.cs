namespace png;

/// <summary>Defines stateless PNG pixel, palette, sample, and color transformations.</summary>
public interface IPngTransformer
{
    /// <summary>Converts pixels to any supported native PNG pixel format.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <param name="options">The target format and optional palette or transparency metadata.</param>
    /// <returns>The converted pixels in an independent buffer.</returns>
    PngPixelBuffer ConvertPixelFormat(PngPixelBuffer buffer, PngPixelConversionOptions options);
    /// <summary>Adds opaque alpha samples.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <returns>The transformed pixels.</returns>
    PngPixelBuffer AddOpaqueAlpha(PngPixelBuffer buffer);
    /// <summary>Removes alpha samples.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <returns>The transformed pixels.</returns>
    PngPixelBuffer StripAlpha(PngPixelBuffer buffer);
    /// <summary>Removes alpha samples when all are opaque.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <returns>The original or transformed pixels.</returns>
    PngPixelBuffer StripAlphaIfOpaque(PngPixelBuffer buffer);
    /// <summary>Determines whether every alpha sample is opaque.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <returns><c>true</c> when all pixels are opaque.</returns>
    bool IsOpaque(PngPixelBuffer buffer);
    /// <summary>Inverts alpha samples.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <returns>The transformed pixels.</returns>
    PngPixelBuffer InvertAlpha(PngPixelBuffer buffer);
    /// <summary>Composites pixels over a background.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <param name="background">The background color.</param>
    /// <returns>The composited pixels.</returns>
    PngPixelBuffer CompositeOverBackground(PngPixelBuffer buffer, PngBackgroundColor background);
    /// <summary>Premultiplies color samples by alpha.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <returns>The transformed pixels.</returns>
    PngPixelBuffer PremultiplyAlpha(PngPixelBuffer buffer);
    /// <summary>Removes alpha premultiplication.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <returns>The transformed pixels.</returns>
    PngPixelBuffer UnpremultiplyAlpha(PngPixelBuffer buffer);
    /// <summary>Clears color samples under transparent pixels.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <returns>The transformed pixels.</returns>
    PngPixelBuffer ClearTransparentPixelColors(PngPixelBuffer buffer);
    /// <summary>Converts RGB to BGR, RGBA to BGRA, or either reordered layout back to native channel order.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <returns>The transformed pixels.</returns>
    PngPixelBuffer SwapRgbBgr(PngPixelBuffer buffer);
    /// <summary>Converts RGBA to ARGB or ARGB back to RGBA.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <returns>The transformed pixels.</returns>
    PngPixelBuffer SwapRgbaArgb(PngPixelBuffer buffer);
    /// <summary>Flips pixels vertically.</summary>
    /// <param name="buffer">The source pixels.</param>
    /// <returns>The transformed pixels.</returns>
    PngPixelBuffer FlipVertical(PngPixelBuffer buffer);
    /// <summary>Crops a pixel rectangle.</summary>
    /// <param name="buffer">The source pixels.</param><param name="x">The left coordinate.</param><param name="y">The top coordinate.</param><param name="width">The crop width.</param><param name="height">The crop height.</param>
    /// <returns>The cropped pixels.</returns>
    PngPixelBuffer Crop(PngPixelBuffer buffer, int x, int y, int width, int height);
    /// <summary>Pads a pixel canvas.</summary>
    /// <param name="buffer">The source pixels.</param><param name="left">The left padding.</param><param name="top">The top padding.</param><param name="right">The right padding.</param><param name="bottom">The bottom padding.</param>
    /// <returns>The padded pixels.</returns>
    PngPixelBuffer PadCanvas(PngPixelBuffer buffer, int left, int top, int right, int bottom);
    /// <summary>Copies a pixel rectangle between buffers.</summary>
    /// <param name="source">The source pixels.</param><param name="sourceX">The source left coordinate.</param><param name="sourceY">The source top coordinate.</param><param name="destination">The destination pixels.</param><param name="destinationX">The destination left coordinate.</param><param name="destinationY">The destination top coordinate.</param><param name="width">The copy width.</param><param name="height">The copy height.</param>
    void CopyRectangle(PngPixelBuffer source, int sourceX, int sourceY, PngPixelBuffer destination, int destinationX, int destinationY, int width, int height);
    /// <summary>Resizes pixels with nearest-neighbor sampling.</summary>
    /// <param name="buffer">The source pixels.</param><param name="width">The destination width.</param><param name="height">The destination height.</param><returns>The resized pixels.</returns>
    PngPixelBuffer ResizeNearestNeighbor(PngPixelBuffer buffer, int width, int height);
    /// <summary>Resizes pixels with bilinear sampling.</summary>
    /// <param name="buffer">The source pixels.</param><param name="width">The destination width.</param><param name="height">The destination height.</param><returns>The resized pixels.</returns>
    PngPixelBuffer ResizeBilinear(PngPixelBuffer buffer, int width, int height);
    /// <summary>Expands grayscale or grayscale-alpha pixels to RGB or RGBA at their current sample depth, preserving alpha.</summary>
    /// <param name="buffer">The grayscale pixels.</param><returns>The expanded pixels.</returns>
    PngPixelBuffer ExpandGrayscaleToRgb(PngPixelBuffer buffer);
    /// <summary>Expands grayscale or grayscale-alpha pixels to RGB8 or RGBA8, preserving alpha.</summary>
    /// <param name="buffer">The grayscale pixels.</param><returns>The expanded pixels.</returns>
    PngPixelBuffer ExpandGrayscaleToRgb8(PngPixelBuffer buffer);
    /// <summary>Expands packed grayscale pixels to grayscale8.</summary>
    /// <param name="buffer">The grayscale pixels.</param><returns>The expanded pixels.</returns>
    PngPixelBuffer ExpandLowBitToGrayscale8(PngPixelBuffer buffer);
    /// <summary>Tests for a packed grayscale format.</summary>
    /// <param name="pixelFormat">The pixel format.</param><returns><c>true</c> for a packed grayscale format.</returns>
    bool IsLowBitGrayscaleFormat(PngPixelFormat pixelFormat);
    /// <summary>Inverts monochrome samples.</summary>
    /// <param name="buffer">The source pixels.</param><returns>The transformed pixels.</returns>
    PngPixelBuffer InvertMonochrome(PngPixelBuffer buffer);
    /// <summary>Expands indexed pixels using a parsed palette.</summary>
    /// <param name="buffer">The indexed pixels.</param><param name="palette">The palette.</param><returns>The RGB8 pixels.</returns>
    PngPixelBuffer ExpandPaletteToRgb8(PngPixelBuffer buffer, PngPalette palette);
    /// <summary>Expands indexed pixels using a PLTE chunk.</summary>
    /// <param name="buffer">The indexed pixels.</param><param name="paletteChunk">The PLTE chunk.</param><returns>The RGB8 pixels.</returns>
    PngPixelBuffer ExpandPaletteChunkToRgb8(PngPixelBuffer buffer, PngChunk paletteChunk);
    /// <summary>Generates a palette from pixels.</summary>
    /// <param name="buffer">The source pixels.</param><returns>The generated palette.</returns>
    PngPalette GeneratePalette(PngPixelBuffer buffer);
    /// <summary>Unpacks indexed pixels to indexed8.</summary>
    /// <param name="buffer">The indexed pixels.</param><returns>The indexed8 pixels.</returns>
    PngPixelBuffer ToIndexed8(PngPixelBuffer buffer);
    /// <summary>Tests for an indexed pixel format.</summary>
    /// <param name="pixelFormat">The pixel format.</param><returns><c>true</c> for an indexed format.</returns>
    bool IsIndexedFormat(PngPixelFormat pixelFormat);
    /// <summary>Removes unused trailing palette entries.</summary>
    /// <param name="buffer">The indexed pixels.</param><param name="palette">The palette.</param><returns>The trimmed palette.</returns>
    PngPalette TrimPalette(PngPixelBuffer buffer, PngPalette palette);
    /// <summary>Converts RGB pixels to grayscale.</summary>
    /// <param name="buffer">The RGB pixels.</param><returns>The grayscale pixels.</returns>
    PngPixelBuffer ConvertRgbToGrayscale(PngPixelBuffer buffer);
    /// <summary>Converts RGB pixels only when every color is gray.</summary>
    /// <param name="buffer">The RGB pixels.</param><returns>The original or converted pixels.</returns>
    PngPixelBuffer ConvertRgbToGrayscaleIfPossible(PngPixelBuffer buffer);
    /// <summary>Tests whether all RGB colors are gray.</summary>
    /// <param name="buffer">The source pixels.</param><returns><c>true</c> when every color is gray.</returns>
    bool IsGrayscale(PngPixelBuffer buffer);
    /// <summary>Calculates an 8-bit luminance sample.</summary>
    /// <param name="red">Red.</param><param name="green">Green.</param><param name="blue">Blue.</param><returns>The luminance.</returns>
    byte CalculateLuminance8(byte red, byte green, byte blue);
    /// <summary>Calculates a 16-bit luminance sample.</summary>
    /// <param name="red">Red.</param><param name="green">Green.</param><param name="blue">Blue.</param><returns>The luminance.</returns>
    ushort CalculateLuminance16(ushort red, ushort green, ushort blue);
    /// <summary>Swaps bytes within every 16-bit sample.</summary>
    /// <param name="buffer">The source pixels.</param><returns>The transformed pixels.</returns>
    PngPixelBuffer ByteSwap16BitSamples(PngPixelBuffer buffer);
    /// <summary>Reduces 16-bit samples when lossless.</summary>
    /// <param name="buffer">The source pixels.</param><returns>The original or reduced pixels.</returns>
    PngPixelBuffer Reduce16BitTo8BitIfLossless(PngPixelBuffer buffer);
    /// <summary>Tests whether 16-bit samples can be reduced losslessly.</summary>
    /// <param name="buffer">The source pixels.</param><returns><c>true</c> when reduction is lossless.</returns>
    bool CanReduce16BitTo8Bit(PngPixelBuffer buffer);
    /// <summary>Downshifts samples to their significant bits.</summary>
    /// <param name="buffer">The source pixels.</param><param name="bits">The significant-bit metadata.</param><returns>The transformed pixels.</returns>
    PngPixelBuffer DownshiftToSignificantBits(PngPixelBuffer buffer, PngSignificantBits bits);
    /// <summary>Upscales samples from their significant bits.</summary>
    /// <param name="buffer">The source pixels.</param><param name="bits">The significant-bit metadata.</param><returns>The transformed pixels.</returns>
    PngPixelBuffer UpscaleFromSignificantBits(PngPixelBuffer buffer, PngSignificantBits bits);
    /// <summary>Expands indexed transparency using parsed metadata.</summary>
    /// <param name="buffer">The indexed pixels.</param><param name="palette">The palette.</param><param name="transparency">The transparency values.</param><returns>The RGBA8 pixels.</returns>
    PngPixelBuffer ExpandIndexedTransparency(PngPixelBuffer buffer, PngPalette palette, PngIndexedTransparency transparency);
    /// <summary>Expands indexed transparency using raw chunks.</summary>
    /// <param name="buffer">The indexed pixels.</param><param name="paletteChunk">The PLTE chunk.</param><param name="transparencyChunk">The tRNS chunk.</param><returns>The RGBA8 pixels.</returns>
    PngPixelBuffer ExpandIndexedTransparencyChunks(PngPixelBuffer buffer, PngChunk paletteChunk, PngChunk transparencyChunk);
    /// <summary>Expands grayscale transparency using parsed metadata.</summary>
    /// <param name="buffer">The grayscale pixels.</param><param name="transparency">The transparency value.</param><returns>The pixels with alpha.</returns>
    PngPixelBuffer ExpandGrayscaleTransparency(PngPixelBuffer buffer, PngGrayscaleTransparency transparency);
    /// <summary>Expands grayscale transparency using a raw chunk.</summary>
    /// <param name="buffer">The grayscale pixels.</param><param name="transparencyChunk">The tRNS chunk.</param><returns>The pixels with alpha.</returns>
    PngPixelBuffer ExpandGrayscaleTransparencyChunk(PngPixelBuffer buffer, PngChunk transparencyChunk);
    /// <summary>Expands truecolor transparency using parsed metadata.</summary>
    /// <param name="buffer">The RGB pixels.</param><param name="transparency">The transparency values.</param><returns>The pixels with alpha.</returns>
    PngPixelBuffer ExpandTruecolorTransparency(PngPixelBuffer buffer, PngTruecolorTransparency transparency);
    /// <summary>Expands truecolor transparency using a raw chunk.</summary>
    /// <param name="buffer">The RGB pixels.</param><param name="transparencyChunk">The tRNS chunk.</param><returns>The pixels with alpha.</returns>
    PngPixelBuffer ExpandTruecolorTransparencyChunk(PngPixelBuffer buffer, PngChunk transparencyChunk);
    /// <summary>Builds an 8-bit gamma table from parsed metadata.</summary>
    /// <param name="gamma">The PNG gamma metadata.</param><param name="displayExponent">The display exponent.</param><returns>The lookup table.</returns>
    byte[] Build8BitGammaTable(PngGamma gamma, double displayExponent);
    /// <summary>Builds an 8-bit gamma table from an encoded PNG gamma value.</summary>
    /// <param name="pngGammaValue">The encoded PNG gamma value.</param><param name="displayExponent">The display exponent.</param><returns>The lookup table.</returns>
    byte[] Build8BitGammaTableFromValue(uint pngGammaValue, double displayExponent);
    /// <summary>Builds an 8-bit gamma table from an image exponent.</summary>
    /// <param name="imageGamma">The image exponent.</param><param name="displayExponent">The display exponent.</param><returns>The lookup table.</returns>
    byte[] Build8BitGammaTableFromExponent(double imageGamma, double displayExponent);
    /// <summary>Builds an 8-bit gamma table with a user exponent.</summary>
    /// <param name="imageGamma">The image exponent.</param><param name="displayExponent">The display exponent.</param><param name="userExponent">The user exponent.</param><returns>The lookup table.</returns>
    byte[] Build8BitGammaTableWithUserExponent(double imageGamma, double displayExponent, double userExponent);
    /// <summary>Builds an identity 8-bit lookup table.</summary><returns>The lookup table.</returns>
    byte[] BuildIdentity8BitGammaTable();
    /// <summary>Builds a 16-bit gamma table from parsed metadata.</summary>
    /// <param name="gamma">The PNG gamma metadata.</param><param name="displayExponent">The display exponent.</param><returns>The lookup table.</returns>
    ushort[] Build16BitGammaTable(PngGamma gamma, double displayExponent);
    /// <summary>Builds a 16-bit gamma table from an encoded PNG gamma value.</summary>
    /// <param name="pngGammaValue">The encoded PNG gamma value.</param><param name="displayExponent">The display exponent.</param><returns>The lookup table.</returns>
    ushort[] Build16BitGammaTableFromValue(uint pngGammaValue, double displayExponent);
    /// <summary>Builds a 16-bit gamma table from an image exponent.</summary>
    /// <param name="imageGamma">The image exponent.</param><param name="displayExponent">The display exponent.</param><returns>The lookup table.</returns>
    ushort[] Build16BitGammaTableFromExponent(double imageGamma, double displayExponent);
    /// <summary>Builds a 16-bit gamma table with a user exponent.</summary>
    /// <param name="imageGamma">The image exponent.</param><param name="displayExponent">The display exponent.</param><param name="userExponent">The user exponent.</param><returns>The lookup table.</returns>
    ushort[] Build16BitGammaTableWithUserExponent(double imageGamma, double displayExponent, double userExponent);
    /// <summary>Builds an identity 16-bit lookup table.</summary><returns>The lookup table.</returns>
    ushort[] BuildIdentity16BitGammaTable();
    /// <summary>Applies an 8-bit gamma lookup table.</summary>
    /// <param name="buffer">The source pixels.</param><param name="table">The lookup table.</param><returns>The corrected pixels.</returns>
    PngPixelBuffer Apply8BitGammaTable(PngPixelBuffer buffer, byte[] table);
    /// <summary>Applies a 16-bit gamma lookup table.</summary>
    /// <param name="buffer">The source pixels.</param><param name="table">The lookup table.</param><returns>The corrected pixels.</returns>
    PngPixelBuffer Apply16BitGammaTable(PngPixelBuffer buffer, ushort[] table);
}
