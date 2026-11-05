using System;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifSampleDescription"/>.</summary>
public interface IAvifSampleDescription
{
    /// <summary>Returns the one-based sample-description index.</summary>
    /// <returns>The description index.</returns>
    int GetIndex();

    /// <summary>Returns the coded sample-entry format.</summary>
    /// <returns>The four-character format.</returns>
    string GetFormat();

    /// <summary>Returns the one-based data-reference index.</summary>
    /// <returns>The data-reference index.</returns>
    int GetDataReferenceIndex();

    /// <summary>Returns the coded width declared by the visual sample entry.</summary>
    /// <returns>The positive width.</returns>
    int GetWidth();

    /// <summary>Returns the coded height declared by the visual sample entry.</summary>
    /// <returns>The positive height.</returns>
    int GetHeight();

    /// <summary>Returns the horizontal resolution as a 16.16 fixed-point value.</summary>
    /// <returns>The decoded resolution.</returns>
    double GetHorizontalResolution();

    /// <summary>Returns the vertical resolution as a 16.16 fixed-point value.</summary>
    /// <returns>The decoded resolution.</returns>
    double GetVerticalResolution();

    /// <summary>Returns the visual sample-entry compressor name.</summary>
    /// <returns>The decoded compressor name.</returns>
    string GetCompressorName();

    /// <summary>Returns the visual sample-entry pixel depth.</summary>
    /// <returns>The depth field.</returns>
    int GetDepth();

    /// <summary>Returns the auxiliary-track type declared by the sample entry.</summary>
    /// <returns>The exact auxiliary type URI, or an empty string when absent.</returns>
    string GetAuxiliaryType();

    /// <summary>Returns the AV1 sequence profile.</summary>
    /// <returns>The profile.</returns>
    int GetAv1Profile();

    /// <summary>Returns the AV1 sequence level index.</summary>
    /// <returns>The level index.</returns>
    int GetAv1LevelIndex();

    /// <summary>Returns the AV1 sequence tier.</summary>
    /// <returns>The tier.</returns>
    int GetAv1Tier();

    /// <summary>Returns the configured AV1 bit depth.</summary>
    /// <returns>The bit depth.</returns>
    int GetBitDepth();

    /// <summary>Returns the configured AV1 channel count.</summary>
    /// <returns>One for monochrome or three for color.</returns>
    int GetChannelCount();

    /// <summary>Reports whether horizontal chroma subsampling is enabled.</summary>
    /// <returns><c>true</c> when chroma is horizontally subsampled.</returns>
    bool HasChromaSubsamplingX();

    /// <summary>Reports whether vertical chroma subsampling is enabled.</summary>
    /// <returns><c>true</c> when chroma is vertically subsampled.</returns>
    bool HasChromaSubsamplingY();

    /// <summary>Returns a copy of the AV1 configuration OBUs.</summary>
    /// <returns>The configuration OBU bytes.</returns>
    byte[] GetConfigObus();
}
