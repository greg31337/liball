namespace png;

/// <summary>Defines immutable-style edits to PNG metadata models.</summary>
public interface IPngMetadataEditor
{
    /// <summary>Sets indexed transparency.</summary><param name="metadata">Source metadata.</param><param name="transparency">Transparency values.</param><returns>Edited metadata.</returns>
    PngMetadata SetIndexedTransparency(PngMetadata metadata, PngIndexedTransparency transparency);
    /// <summary>Sets grayscale transparency.</summary><param name="metadata">Source metadata.</param><param name="transparency">Transparency value.</param><returns>Edited metadata.</returns>
    PngMetadata SetGrayscaleTransparency(PngMetadata metadata, PngGrayscaleTransparency transparency);
    /// <summary>Sets truecolor transparency.</summary><param name="metadata">Source metadata.</param><param name="transparency">Transparency values.</param><returns>Edited metadata.</returns>
    PngMetadata SetTruecolorTransparency(PngMetadata metadata, PngTruecolorTransparency transparency);
    /// <summary>Sets chromaticities.</summary><param name="metadata">Source metadata.</param><param name="chromaticities">Chromaticity values.</param><returns>Edited metadata.</returns>
    PngMetadata SetChromaticities(PngMetadata metadata, PngChromaticities chromaticities);
    /// <summary>Sets the ICC profile.</summary><param name="metadata">Source metadata.</param><param name="profile">ICC profile.</param><returns>Edited metadata.</returns>
    PngMetadata SetIccProfile(PngMetadata metadata, PngIccProfile profile);
    /// <summary>Sets grayscale significant bits.</summary><param name="metadata">Source metadata.</param><param name="bits">Significant-bit values.</param><returns>Edited metadata.</returns>
    PngMetadata SetGrayscaleSignificantBits(PngMetadata metadata, PngSignificantBits bits);
    /// <summary>Sets grayscale-alpha significant bits.</summary><param name="metadata">Source metadata.</param><param name="bits">Significant-bit values.</param><returns>Edited metadata.</returns>
    PngMetadata SetGrayscaleAlphaSignificantBits(PngMetadata metadata, PngSignificantBits bits);
    /// <summary>Sets indexed significant bits.</summary><param name="metadata">Source metadata.</param><param name="bits">Significant-bit values.</param><returns>Edited metadata.</returns>
    PngMetadata SetIndexedSignificantBits(PngMetadata metadata, PngSignificantBits bits);
    /// <summary>Sets truecolor significant bits.</summary><param name="metadata">Source metadata.</param><param name="bits">Significant-bit values.</param><returns>Edited metadata.</returns>
    PngMetadata SetTruecolorSignificantBits(PngMetadata metadata, PngSignificantBits bits);
    /// <summary>Sets truecolor-alpha significant bits.</summary><param name="metadata">Source metadata.</param><param name="bits">Significant-bit values.</param><returns>Edited metadata.</returns>
    PngMetadata SetTruecolorAlphaSignificantBits(PngMetadata metadata, PngSignificantBits bits);
    /// <summary>Sets coding-independent code point values.</summary><param name="metadata">Source metadata.</param><param name="colorPrimaries">Color primaries.</param><param name="transferCharacteristics">Transfer characteristics.</param><param name="matrixCoefficients">Matrix coefficients.</param><param name="videoFullRangeFlag">Range flag.</param><returns>Edited metadata.</returns>
    PngMetadata SetCodingIndependentCodePointValues(PngMetadata metadata, byte colorPrimaries, byte transferCharacteristics, byte matrixCoefficients, byte videoFullRangeFlag);
    /// <summary>Sets coding-independent code point metadata.</summary><param name="metadata">Source metadata.</param><param name="codePoints">Code point metadata.</param><returns>Edited metadata.</returns>
    PngMetadata SetCodingIndependentCodePoints(PngMetadata metadata, PngCodingIndependentCodePoints codePoints);
    /// <summary>Sets an encoded gamma value.</summary><param name="metadata">Source metadata.</param><param name="gammaValue">Encoded gamma value.</param><returns>Edited metadata.</returns>
    PngMetadata SetGammaValue(PngMetadata metadata, uint gammaValue);
    /// <summary>Sets gamma metadata.</summary><param name="metadata">Source metadata.</param><param name="gamma">Gamma metadata.</param><returns>Edited metadata.</returns>
    PngMetadata SetGamma(PngMetadata metadata, PngGamma gamma);
    /// <summary>Sets an sRGB rendering intent.</summary><param name="metadata">Source metadata.</param><param name="renderingIntent">Rendering intent.</param><returns>Edited metadata.</returns>
    PngMetadata SetStandardRgbIntent(PngMetadata metadata, byte renderingIntent);
    /// <summary>Sets sRGB metadata.</summary><param name="metadata">Source metadata.</param><param name="standardRgb">sRGB metadata.</param><returns>Edited metadata.</returns>
    PngMetadata SetStandardRgb(PngMetadata metadata, PngStandardRgb standardRgb);
    /// <summary>Sets mastering-display color volume metadata.</summary><param name="metadata">Source metadata.</param><param name="volume">Mastering-display values.</param><returns>Edited metadata.</returns>
    PngMetadata SetMasteringDisplayColorVolume(PngMetadata metadata, PngMasteringDisplayColorVolume volume);
    /// <summary>Sets content light-level metadata.</summary><param name="metadata">Source metadata.</param><param name="lightLevels">Content light-level values.</param><returns>Edited metadata.</returns>
    PngMetadata SetContentLightLevelInformation(PngMetadata metadata, PngContentLightLevelInformation lightLevels);
    /// <summary>Adds uncompressed Latin-1 text.</summary><param name="metadata">Source metadata.</param><param name="text">Text entry.</param><returns>Edited metadata.</returns>
    PngMetadata AddLatin1Text(PngMetadata metadata, PngText text);
    /// <summary>Adds compressed Latin-1 text.</summary><param name="metadata">Source metadata.</param><param name="text">Text entry.</param><returns>Edited metadata.</returns>
    PngMetadata AddCompressedText(PngMetadata metadata, PngCompressedText text);
    /// <summary>Adds international text.</summary><param name="metadata">Source metadata.</param><param name="text">Text entry.</param><returns>Edited metadata.</returns>
    PngMetadata AddInternationalText(PngMetadata metadata, PngInternationalText text);
    /// <summary>Replaces uncompressed Latin-1 text entries.</summary><param name="metadata">Source metadata.</param><param name="entries">Replacement entries.</param><returns>Edited metadata.</returns>
    PngMetadata ReplaceLatin1Text(PngMetadata metadata, PngText[] entries);
    /// <summary>Replaces compressed Latin-1 text entries.</summary><param name="metadata">Source metadata.</param><param name="entries">Replacement entries.</param><returns>Edited metadata.</returns>
    PngMetadata ReplaceCompressedText(PngMetadata metadata, PngCompressedText[] entries);
    /// <summary>Replaces international text entries.</summary><param name="metadata">Source metadata.</param><param name="entries">Replacement entries.</param><returns>Edited metadata.</returns>
    PngMetadata ReplaceInternationalText(PngMetadata metadata, PngInternationalText[] entries);
    /// <summary>Sets an indexed background color.</summary><param name="metadata">Source metadata.</param><param name="background">Background value.</param><returns>Edited metadata.</returns>
    PngMetadata SetIndexedBackgroundColor(PngMetadata metadata, PngBackgroundColor background);
    /// <summary>Sets a grayscale background color.</summary><param name="metadata">Source metadata.</param><param name="background">Background value.</param><returns>Edited metadata.</returns>
    PngMetadata SetGrayscaleBackgroundColor(PngMetadata metadata, PngBackgroundColor background);
    /// <summary>Sets a truecolor background color.</summary><param name="metadata">Source metadata.</param><param name="background">Background value.</param><returns>Edited metadata.</returns>
    PngMetadata SetTruecolorBackgroundColor(PngMetadata metadata, PngBackgroundColor background);
    /// <summary>Sets the palette histogram.</summary><param name="metadata">Source metadata.</param><param name="histogram">Palette histogram.</param><returns>Edited metadata.</returns>
    PngMetadata SetPaletteHistogram(PngMetadata metadata, PngPaletteHistogram histogram);
    /// <summary>Sets physical pixel values.</summary><param name="metadata">Source metadata.</param><param name="pixelsPerUnitX">Horizontal density.</param><param name="pixelsPerUnitY">Vertical density.</param><param name="unitSpecifier">Unit specifier.</param><returns>Edited metadata.</returns>
    PngMetadata SetPhysicalDimensionValues(PngMetadata metadata, uint pixelsPerUnitX, uint pixelsPerUnitY, byte unitSpecifier);
    /// <summary>Sets physical pixel metadata.</summary><param name="metadata">Source metadata.</param><param name="physicalPixels">Physical pixel metadata.</param><returns>Edited metadata.</returns>
    PngMetadata SetPhysicalDimensions(PngMetadata metadata, PngPhysicalPixels physicalPixels);
    /// <summary>Adds a suggested palette.</summary><param name="metadata">Source metadata.</param><param name="palette">Suggested palette.</param><returns>Edited metadata.</returns>
    PngMetadata AddSuggestedPalette(PngMetadata metadata, PngSuggestedPalette palette);
    /// <summary>Replaces suggested palettes.</summary><param name="metadata">Source metadata.</param><param name="palettes">Replacement palettes.</param><returns>Edited metadata.</returns>
    PngMetadata ReplaceSuggestedPalettes(PngMetadata metadata, PngSuggestedPalette[] palettes);
    /// <summary>Sets an Exif profile.</summary><param name="metadata">Source metadata.</param><param name="profile">Exif profile.</param><returns>Edited metadata.</returns>
    PngMetadata SetExifProfile(PngMetadata metadata, PngExifProfile profile);
    /// <summary>Sets the last-modification time.</summary><param name="metadata">Source metadata.</param><param name="time">Modification time.</param><returns>Edited metadata.</returns>
    PngMetadata SetTime(PngMetadata metadata, PngTime time);
    /// <summary>Sets animation control metadata.</summary><param name="metadata">Source metadata.</param><param name="control">Animation control.</param><returns>Edited metadata.</returns>
    PngMetadata SetAnimationControl(PngMetadata metadata, PngAnimationControl control);
    /// <summary>Adds a frame control.</summary><param name="metadata">Source metadata.</param><param name="control">Frame control.</param><returns>Edited metadata.</returns>
    PngMetadata AddFrameControl(PngMetadata metadata, PngFrameControl control);
    /// <summary>Replaces frame controls.</summary><param name="metadata">Source metadata.</param><param name="controls">Replacement frame controls.</param><returns>Edited metadata.</returns>
    PngMetadata ReplaceFrameControls(PngMetadata metadata, PngFrameControl[] controls);
    /// <summary>Adds a frame-data fragment.</summary><param name="metadata">Source metadata.</param><param name="frameData">Frame data.</param><returns>Edited metadata.</returns>
    PngMetadata AddFrameData(PngMetadata metadata, PngFrameData frameData);
    /// <summary>Replaces frame-data fragments.</summary><param name="metadata">Source metadata.</param><param name="frameData">Replacement frame data.</param><returns>Edited metadata.</returns>
    PngMetadata ReplaceFrameData(PngMetadata metadata, PngFrameData[] frameData);
    /// <summary>Replaces text chunks from text models.</summary><param name="metadata">Source metadata.</param><param name="textChunks">Text models.</param><returns>Edited metadata.</returns>
    PngMetadata ReplaceTextModels(PngMetadata metadata, PngText[] textChunks);
    /// <summary>Replaces text chunks from raw chunks.</summary><param name="metadata">Source metadata.</param><param name="textChunks">Raw text chunks.</param><returns>Edited metadata.</returns>
    PngMetadata ReplaceTextChunks(PngMetadata metadata, PngChunk[] textChunks);
    /// <summary>Removes transparency metadata.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveTransparency(PngMetadata metadata);
    /// <summary>Removes chromaticities.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveChromaticities(PngMetadata metadata);
    /// <summary>Removes gamma metadata.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveGamma(PngMetadata metadata);
    /// <summary>Removes the ICC profile.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveIccProfile(PngMetadata metadata);
    /// <summary>Removes significant-bit metadata.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveSignificantBits(PngMetadata metadata);
    /// <summary>Removes standard-RGB metadata.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveStandardRgb(PngMetadata metadata);
    /// <summary>Removes coding-independent code points.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveCodingIndependentCodePoints(PngMetadata metadata);
    /// <summary>Removes mastering-display metadata.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveMasteringDisplayColorVolume(PngMetadata metadata);
    /// <summary>Removes content light-level metadata.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveContentLightLevelInformation(PngMetadata metadata);
    /// <summary>Removes uncompressed Latin-1 text.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveLatin1Text(PngMetadata metadata);
    /// <summary>Removes compressed Latin-1 text.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveCompressedText(PngMetadata metadata);
    /// <summary>Removes international text.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveInternationalText(PngMetadata metadata);
    /// <summary>Removes all standard text.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveText(PngMetadata metadata);
    /// <summary>Removes the background color.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveBackgroundColor(PngMetadata metadata);
    /// <summary>Removes the palette histogram.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemovePaletteHistogram(PngMetadata metadata);
    /// <summary>Removes physical dimensions.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemovePhysicalDimensions(PngMetadata metadata);
    /// <summary>Removes suggested palettes.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveSuggestedPalettes(PngMetadata metadata);
    /// <summary>Removes the Exif profile.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveExifProfile(PngMetadata metadata);
    /// <summary>Removes last-modification time metadata.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveTime(PngMetadata metadata);
    /// <summary>Removes animation control metadata.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveAnimationControl(PngMetadata metadata);
    /// <summary>Removes frame controls.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveFrameControls(PngMetadata metadata);
    /// <summary>Removes frame-data fragments.</summary><param name="metadata">Source metadata.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveFrameData(PngMetadata metadata);
    /// <summary>Removes one chunk type.</summary><param name="metadata">Source metadata.</param><param name="chunkType">Chunk type.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveChunk(PngMetadata metadata, string chunkType);
    /// <summary>Removes several chunk types.</summary><param name="metadata">Source metadata.</param><param name="chunkTypes">Chunk types.</param><returns>Edited metadata.</returns>
    PngMetadata RemoveChunks(PngMetadata metadata, string[] chunkTypes);
}

/// <summary>Defines PNG encoding trials and optimization report construction.</summary>
public interface IPngOptimizer
{
    /// <summary>Runs the complete lossless optimization workflow for PNG bytes.</summary><param name="data">Source PNG datastream.</param><param name="options">Optimization options.</param><returns>The complete optimization report and best encoded result.</returns>
    PngOptimizerReport OptimizeBytes(byte[] data, PngOptimizerOptions options);
    /// <summary>Runs compression trials with defaults.</summary><param name="image">Source image.</param><returns>Trial results.</returns>
    PngCompressionTrialResult RunCompressionTrials(PngImage image);
    /// <summary>Runs compression trials with options.</summary><param name="image">Source image.</param><param name="options">Optimization options.</param><returns>Trial results.</returns>
    PngCompressionTrialResult RunCompressionTrialsWithOptions(PngImage image, PngOptimizerOptions options);
    /// <summary>Runs compression trials with an injected encoder.</summary><param name="image">Source image.</param><param name="options">Optimization options.</param><param name="encoder">Encoder implementation.</param><returns>Trial results.</returns>
    PngCompressionTrialResult RunCompressionTrialsWithEncoder(PngImage image, PngOptimizerOptions options, IPngEncoder encoder);
    /// <summary>Runs filter trials with defaults.</summary><param name="image">Source image.</param><returns>Trial results.</returns>
    PngFilterStrategyTrialResult RunFilterTrials(PngImage image);
    /// <summary>Runs filter trials with options.</summary><param name="image">Source image.</param><param name="options">Optimization options.</param><returns>Trial results.</returns>
    PngFilterStrategyTrialResult RunFilterTrialsWithOptions(PngImage image, PngOptimizerOptions options);
    /// <summary>Runs filter trials with an injected encoder.</summary><param name="image">Source image.</param><param name="options">Optimization options.</param><param name="encoder">Encoder implementation.</param><returns>Trial results.</returns>
    PngFilterStrategyTrialResult RunFilterTrialsWithEncoder(PngImage image, PngOptimizerOptions options, IPngEncoder encoder);
    /// <summary>Creates an optimization report from a byte count.</summary><param name="originalByteCount">Original size.</param><param name="compressionResult">Compression trials.</param><param name="filterStrategyResult">Filter trials.</param><returns>The report.</returns>
    PngOptimizerReport CreateReportFromByteCount(long originalByteCount, PngCompressionTrialResult compressionResult, PngFilterStrategyTrialResult filterStrategyResult);
    /// <summary>Creates an optimization report from original bytes.</summary><param name="originalBytes">Original PNG bytes.</param><param name="compressionResult">Compression trials.</param><param name="filterStrategyResult">Filter trials.</param><returns>The report.</returns>
    PngOptimizerReport CreateReportFromBytes(byte[] originalBytes, PngCompressionTrialResult compressionResult, PngFilterStrategyTrialResult filterStrategyResult);
}

/// <summary>Calculates PNG CRC-32 values.</summary>
public interface IPngCrcCalculator
{
    /// <summary>Calculates CRC-32 over a bounded span.</summary><param name="data">Input bytes.</param><returns>The CRC.</returns>
    uint CalculateSpan(ReadOnlySpan<byte> data);
    /// <summary>Calculates CRC-32 over an array.</summary><param name="data">Input bytes.</param><returns>The CRC.</returns>
    uint Calculate(byte[] data);
    /// <summary>Calculates CRC-32 over an array range.</summary><param name="data">Input bytes.</param><param name="offset">Range offset.</param><param name="count">Range count.</param><returns>The CRC.</returns>
    uint CalculateRange(byte[] data, int offset, int count);
    /// <summary>Calculates a chunk CRC from a chunk name.</summary><param name="chunkType">Four-character chunk name.</param><param name="data">Chunk payload.</param><returns>The CRC.</returns>
    uint CalculateChunkFromName(string chunkType, byte[] data);
    /// <summary>Calculates a chunk CRC from chunk type bytes.</summary><param name="chunkType">Four chunk type bytes.</param><param name="data">Chunk payload.</param><returns>The CRC.</returns>
    uint CalculateChunkFromBytes(byte[] chunkType, byte[] data);
    /// <summary>Calculates a chunk CRC from a chunk name and bounded payload.</summary><param name="chunkType">Four-character chunk name.</param><param name="data">Chunk payload.</param><returns>The CRC.</returns>
    uint CalculateChunkFromNameSpan(string chunkType, ReadOnlySpan<byte> data);
    /// <summary>Calculates a chunk CRC from bounded type and payload spans.</summary><param name="chunkType">Four chunk type bytes.</param><param name="data">Chunk payload.</param><returns>The CRC.</returns>
    uint CalculateChunkFromSpans(ReadOnlySpan<byte> chunkType, ReadOnlySpan<byte> data);
}
