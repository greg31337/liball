namespace png;

/// <summary>Default service for immutable-style PNG metadata edits.</summary>
public class PngMetadataService : IPngMetadataEditor
{
    /// <summary>Creates an independent PNG metadata editor.</summary>
    public PngMetadataService()
    {
    }

    /// <summary>Sets indexed transparency.</summary><param name="metadata">Source.</param><param name="transparency">Transparency.</param><returns>Edited metadata.</returns>
    public PngMetadata SetIndexedTransparency(PngMetadata metadata, PngIndexedTransparency transparency) { return PngTypedMetadataEditor.SetIndexedTransparency(metadata, transparency); }
    /// <summary>Sets grayscale transparency.</summary><param name="metadata">Source.</param><param name="transparency">Transparency.</param><returns>Edited metadata.</returns>
    public PngMetadata SetGrayscaleTransparency(PngMetadata metadata, PngGrayscaleTransparency transparency) { return PngTypedMetadataEditor.SetGrayscaleTransparency(metadata, transparency); }
    /// <summary>Sets truecolor transparency.</summary><param name="metadata">Source.</param><param name="transparency">Transparency.</param><returns>Edited metadata.</returns>
    public PngMetadata SetTruecolorTransparency(PngMetadata metadata, PngTruecolorTransparency transparency) { return PngTypedMetadataEditor.SetTruecolorTransparency(metadata, transparency); }
    /// <summary>Sets chromaticities.</summary><param name="metadata">Source.</param><param name="chromaticities">Chromaticities.</param><returns>Edited metadata.</returns>
    public PngMetadata SetChromaticities(PngMetadata metadata, PngChromaticities chromaticities) { return PngTypedMetadataEditor.SetChromaticities(metadata, chromaticities); }
    /// <summary>Sets the ICC profile.</summary><param name="metadata">Source.</param><param name="profile">Profile.</param><returns>Edited metadata.</returns>
    public PngMetadata SetIccProfile(PngMetadata metadata, PngIccProfile profile) { return PngMetadataEditor.SetIccProfile(metadata, profile); }
    /// <summary>Sets grayscale significant bits.</summary><param name="metadata">Source.</param><param name="bits">Bits.</param><returns>Edited metadata.</returns>
    public PngMetadata SetGrayscaleSignificantBits(PngMetadata metadata, PngSignificantBits bits) { return PngTypedMetadataEditor.SetGrayscaleSignificantBits(metadata, bits); }
    /// <summary>Sets grayscale-alpha significant bits.</summary><param name="metadata">Source.</param><param name="bits">Bits.</param><returns>Edited metadata.</returns>
    public PngMetadata SetGrayscaleAlphaSignificantBits(PngMetadata metadata, PngSignificantBits bits) { return PngTypedMetadataEditor.SetGrayscaleAlphaSignificantBits(metadata, bits); }
    /// <summary>Sets indexed significant bits.</summary><param name="metadata">Source.</param><param name="bits">Bits.</param><returns>Edited metadata.</returns>
    public PngMetadata SetIndexedSignificantBits(PngMetadata metadata, PngSignificantBits bits) { return PngTypedMetadataEditor.SetIndexedSignificantBits(metadata, bits); }
    /// <summary>Sets truecolor significant bits.</summary><param name="metadata">Source.</param><param name="bits">Bits.</param><returns>Edited metadata.</returns>
    public PngMetadata SetTruecolorSignificantBits(PngMetadata metadata, PngSignificantBits bits) { return PngTypedMetadataEditor.SetTruecolorSignificantBits(metadata, bits); }
    /// <summary>Sets truecolor-alpha significant bits.</summary><param name="metadata">Source.</param><param name="bits">Bits.</param><returns>Edited metadata.</returns>
    public PngMetadata SetTruecolorAlphaSignificantBits(PngMetadata metadata, PngSignificantBits bits) { return PngTypedMetadataEditor.SetTruecolorAlphaSignificantBits(metadata, bits); }
    /// <summary>Sets code point values.</summary><param name="metadata">Source.</param><param name="colorPrimaries">Primaries.</param><param name="transferCharacteristics">Transfer.</param><param name="matrixCoefficients">Matrix.</param><param name="videoFullRangeFlag">Range.</param><returns>Edited metadata.</returns>
    public PngMetadata SetCodingIndependentCodePointValues(PngMetadata metadata, byte colorPrimaries, byte transferCharacteristics, byte matrixCoefficients, byte videoFullRangeFlag) { return PngMetadataEditor.SetCodingIndependentCodePoints(metadata, colorPrimaries, transferCharacteristics, matrixCoefficients, videoFullRangeFlag); }
    /// <summary>Sets code point metadata.</summary><param name="metadata">Source.</param><param name="codePoints">Code points.</param><returns>Edited metadata.</returns>
    public PngMetadata SetCodingIndependentCodePoints(PngMetadata metadata, PngCodingIndependentCodePoints codePoints) { return PngMetadataEditor.SetCodingIndependentCodePoints(metadata, codePoints); }
    /// <summary>Sets gamma by value.</summary><param name="metadata">Source.</param><param name="gammaValue">Gamma value.</param><returns>Edited metadata.</returns>
    public PngMetadata SetGammaValue(PngMetadata metadata, uint gammaValue) { return PngMetadataEditor.SetGamma(metadata, gammaValue); }
    /// <summary>Sets gamma metadata.</summary><param name="metadata">Source.</param><param name="gamma">Gamma.</param><returns>Edited metadata.</returns>
    public PngMetadata SetGamma(PngMetadata metadata, PngGamma gamma) { return PngMetadataEditor.SetGamma(metadata, gamma); }
    /// <summary>Sets sRGB intent.</summary><param name="metadata">Source.</param><param name="renderingIntent">Intent.</param><returns>Edited metadata.</returns>
    public PngMetadata SetStandardRgbIntent(PngMetadata metadata, byte renderingIntent) { return PngMetadataEditor.SetStandardRgb(metadata, renderingIntent); }
    /// <summary>Sets sRGB metadata.</summary><param name="metadata">Source.</param><param name="standardRgb">sRGB.</param><returns>Edited metadata.</returns>
    public PngMetadata SetStandardRgb(PngMetadata metadata, PngStandardRgb standardRgb) { return PngMetadataEditor.SetStandardRgb(metadata, standardRgb); }
    /// <summary>Sets mastering-display metadata.</summary><param name="metadata">Source.</param><param name="volume">Volume.</param><returns>Edited metadata.</returns>
    public PngMetadata SetMasteringDisplayColorVolume(PngMetadata metadata, PngMasteringDisplayColorVolume volume) { return PngTypedMetadataEditor.SetMasteringDisplayColorVolume(metadata, volume); }
    /// <summary>Sets content light levels.</summary><param name="metadata">Source.</param><param name="lightLevels">Light levels.</param><returns>Edited metadata.</returns>
    public PngMetadata SetContentLightLevelInformation(PngMetadata metadata, PngContentLightLevelInformation lightLevels) { return PngTypedMetadataEditor.SetContentLightLevelInformation(metadata, lightLevels); }
    /// <summary>Adds Latin-1 text.</summary><param name="metadata">Source.</param><param name="text">Text.</param><returns>Edited metadata.</returns>
    public PngMetadata AddLatin1Text(PngMetadata metadata, PngText text) { return PngTypedMetadataEditor.AddLatin1Text(metadata, text); }
    /// <summary>Adds compressed text.</summary><param name="metadata">Source.</param><param name="text">Text.</param><returns>Edited metadata.</returns>
    public PngMetadata AddCompressedText(PngMetadata metadata, PngCompressedText text) { return PngTypedMetadataEditor.AddCompressedText(metadata, text); }
    /// <summary>Adds international text.</summary><param name="metadata">Source.</param><param name="text">Text.</param><returns>Edited metadata.</returns>
    public PngMetadata AddInternationalText(PngMetadata metadata, PngInternationalText text) { return PngTypedMetadataEditor.AddInternationalText(metadata, text); }
    /// <summary>Replaces Latin-1 text.</summary><param name="metadata">Source.</param><param name="entries">Entries.</param><returns>Edited metadata.</returns>
    public PngMetadata ReplaceLatin1Text(PngMetadata metadata, PngText[] entries) { return PngTypedMetadataEditor.ReplaceLatin1Text(metadata, entries); }
    /// <summary>Replaces compressed text.</summary><param name="metadata">Source.</param><param name="entries">Entries.</param><returns>Edited metadata.</returns>
    public PngMetadata ReplaceCompressedText(PngMetadata metadata, PngCompressedText[] entries) { return PngTypedMetadataEditor.ReplaceCompressedText(metadata, entries); }
    /// <summary>Replaces international text.</summary><param name="metadata">Source.</param><param name="entries">Entries.</param><returns>Edited metadata.</returns>
    public PngMetadata ReplaceInternationalText(PngMetadata metadata, PngInternationalText[] entries) { return PngTypedMetadataEditor.ReplaceInternationalText(metadata, entries); }
    /// <summary>Sets an indexed background.</summary><param name="metadata">Source.</param><param name="background">Background.</param><returns>Edited metadata.</returns>
    public PngMetadata SetIndexedBackgroundColor(PngMetadata metadata, PngBackgroundColor background) { return PngTypedMetadataEditor.SetIndexedBackgroundColor(metadata, background); }
    /// <summary>Sets a grayscale background.</summary><param name="metadata">Source.</param><param name="background">Background.</param><returns>Edited metadata.</returns>
    public PngMetadata SetGrayscaleBackgroundColor(PngMetadata metadata, PngBackgroundColor background) { return PngTypedMetadataEditor.SetGrayscaleBackgroundColor(metadata, background); }
    /// <summary>Sets a truecolor background.</summary><param name="metadata">Source.</param><param name="background">Background.</param><returns>Edited metadata.</returns>
    public PngMetadata SetTruecolorBackgroundColor(PngMetadata metadata, PngBackgroundColor background) { return PngTypedMetadataEditor.SetTruecolorBackgroundColor(metadata, background); }
    /// <summary>Sets a palette histogram.</summary><param name="metadata">Source.</param><param name="histogram">Histogram.</param><returns>Edited metadata.</returns>
    public PngMetadata SetPaletteHistogram(PngMetadata metadata, PngPaletteHistogram histogram) { return PngTypedMetadataEditor.SetPaletteHistogram(metadata, histogram); }
    /// <summary>Sets physical dimensions by value.</summary><param name="metadata">Source.</param><param name="pixelsPerUnitX">X density.</param><param name="pixelsPerUnitY">Y density.</param><param name="unitSpecifier">Unit.</param><returns>Edited metadata.</returns>
    public PngMetadata SetPhysicalDimensionValues(PngMetadata metadata, uint pixelsPerUnitX, uint pixelsPerUnitY, byte unitSpecifier) { return PngMetadataEditor.SetPhysicalDimensions(metadata, pixelsPerUnitX, pixelsPerUnitY, unitSpecifier); }
    /// <summary>Sets physical dimension metadata.</summary><param name="metadata">Source.</param><param name="physicalPixels">Dimensions.</param><returns>Edited metadata.</returns>
    public PngMetadata SetPhysicalDimensions(PngMetadata metadata, PngPhysicalPixels physicalPixels) { return PngMetadataEditor.SetPhysicalDimensions(metadata, physicalPixels); }
    /// <summary>Adds a suggested palette.</summary><param name="metadata">Source.</param><param name="palette">Palette.</param><returns>Edited metadata.</returns>
    public PngMetadata AddSuggestedPalette(PngMetadata metadata, PngSuggestedPalette palette) { return PngTypedMetadataEditor.AddSuggestedPalette(metadata, palette); }
    /// <summary>Replaces suggested palettes.</summary><param name="metadata">Source.</param><param name="palettes">Palettes.</param><returns>Edited metadata.</returns>
    public PngMetadata ReplaceSuggestedPalettes(PngMetadata metadata, PngSuggestedPalette[] palettes) { return PngTypedMetadataEditor.ReplaceSuggestedPalettes(metadata, palettes); }
    /// <summary>Sets an Exif profile.</summary><param name="metadata">Source.</param><param name="profile">Profile.</param><returns>Edited metadata.</returns>
    public PngMetadata SetExifProfile(PngMetadata metadata, PngExifProfile profile) { return PngTypedMetadataEditor.SetExifProfile(metadata, profile); }
    /// <summary>Sets modification time.</summary><param name="metadata">Source.</param><param name="time">Time.</param><returns>Edited metadata.</returns>
    public PngMetadata SetTime(PngMetadata metadata, PngTime time) { return PngTypedMetadataEditor.SetTime(metadata, time); }
    /// <summary>Sets animation control.</summary><param name="metadata">Source.</param><param name="control">Control.</param><returns>Edited metadata.</returns>
    public PngMetadata SetAnimationControl(PngMetadata metadata, PngAnimationControl control) { return PngTypedMetadataEditor.SetAnimationControl(metadata, control); }
    /// <summary>Adds frame control.</summary><param name="metadata">Source.</param><param name="control">Control.</param><returns>Edited metadata.</returns>
    public PngMetadata AddFrameControl(PngMetadata metadata, PngFrameControl control) { return PngTypedMetadataEditor.AddFrameControl(metadata, control); }
    /// <summary>Replaces frame controls.</summary><param name="metadata">Source.</param><param name="controls">Controls.</param><returns>Edited metadata.</returns>
    public PngMetadata ReplaceFrameControls(PngMetadata metadata, PngFrameControl[] controls) { return PngTypedMetadataEditor.ReplaceFrameControls(metadata, controls); }
    /// <summary>Adds frame data.</summary><param name="metadata">Source.</param><param name="frameData">Frame data.</param><returns>Edited metadata.</returns>
    public PngMetadata AddFrameData(PngMetadata metadata, PngFrameData frameData) { return PngTypedMetadataEditor.AddFrameData(metadata, frameData); }
    /// <summary>Replaces frame data.</summary><param name="metadata">Source.</param><param name="frameData">Frame data.</param><returns>Edited metadata.</returns>
    public PngMetadata ReplaceFrameData(PngMetadata metadata, PngFrameData[] frameData) { return PngTypedMetadataEditor.ReplaceFrameData(metadata, frameData); }
    /// <summary>Replaces text from models.</summary><param name="metadata">Source.</param><param name="textChunks">Text.</param><returns>Edited metadata.</returns>
    public PngMetadata ReplaceTextModels(PngMetadata metadata, PngText[] textChunks) { return PngMetadataEditor.ReplaceTextChunks(metadata, textChunks); }
    /// <summary>Replaces raw text chunks.</summary><param name="metadata">Source.</param><param name="textChunks">Chunks.</param><returns>Edited metadata.</returns>
    public PngMetadata ReplaceTextChunks(PngMetadata metadata, PngChunk[] textChunks) { return PngMetadataEditor.ReplaceTextChunks(metadata, textChunks); }
    /// <summary>Removes transparency.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveTransparency(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveTransparency(metadata); }
    /// <summary>Removes chromaticities.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveChromaticities(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveChromaticities(metadata); }
    /// <summary>Removes gamma.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveGamma(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveGamma(metadata); }
    /// <summary>Removes ICC profile.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveIccProfile(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveIccProfile(metadata); }
    /// <summary>Removes significant bits.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveSignificantBits(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveSignificantBits(metadata); }
    /// <summary>Removes standard RGB.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveStandardRgb(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveStandardRgb(metadata); }
    /// <summary>Removes coding-independent code points.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveCodingIndependentCodePoints(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveCodingIndependentCodePoints(metadata); }
    /// <summary>Removes mastering-display metadata.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveMasteringDisplayColorVolume(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveMasteringDisplayColorVolume(metadata); }
    /// <summary>Removes content light levels.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveContentLightLevelInformation(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveContentLightLevelInformation(metadata); }
    /// <summary>Removes Latin-1 text.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveLatin1Text(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveLatin1Text(metadata); }
    /// <summary>Removes compressed text.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveCompressedText(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveCompressedText(metadata); }
    /// <summary>Removes international text.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveInternationalText(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveInternationalText(metadata); }
    /// <summary>Removes all text.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveText(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveText(metadata); }
    /// <summary>Removes background color.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveBackgroundColor(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveBackgroundColor(metadata); }
    /// <summary>Removes palette histogram.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemovePaletteHistogram(PngMetadata metadata) { return PngTypedMetadataEditor.RemovePaletteHistogram(metadata); }
    /// <summary>Removes physical dimensions.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemovePhysicalDimensions(PngMetadata metadata) { return PngTypedMetadataEditor.RemovePhysicalDimensions(metadata); }
    /// <summary>Removes suggested palettes.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveSuggestedPalettes(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveSuggestedPalettes(metadata); }
    /// <summary>Removes Exif profile.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveExifProfile(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveExifProfile(metadata); }
    /// <summary>Removes time metadata.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveTime(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveTime(metadata); }
    /// <summary>Removes animation control.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveAnimationControl(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveAnimationControl(metadata); }
    /// <summary>Removes frame controls.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveFrameControls(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveFrameControls(metadata); }
    /// <summary>Removes frame data.</summary><param name="metadata">Source.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveFrameData(PngMetadata metadata) { return PngTypedMetadataEditor.RemoveFrameData(metadata); }
    /// <summary>Removes one chunk type.</summary><param name="metadata">Source.</param><param name="chunkType">Type.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveChunk(PngMetadata metadata, string chunkType) { return PngMetadataEditor.RemoveChunks(metadata, chunkType); }
    /// <summary>Removes chunk types.</summary><param name="metadata">Source.</param><param name="chunkTypes">Types.</param><returns>Edited metadata.</returns>
    public PngMetadata RemoveChunks(PngMetadata metadata, string[] chunkTypes) { return PngMetadataEditor.RemoveChunks(metadata, chunkTypes); }
}
