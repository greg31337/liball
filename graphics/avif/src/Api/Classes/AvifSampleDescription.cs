using System;

namespace avif;

/// <summary>Describes one validated AV1 visual sample entry.</summary>
public class AvifSampleDescription : IAvifSampleDescription
{
    int index;
    string format;
    int dataReferenceIndex;
    int width;
    int height;
    uint horizontalResolutionFixed;
    uint verticalResolutionFixed;
    string compressorName;
    int depth;
    ParsedAv1Configuration? configuration;
    string auxiliaryType;

    internal AvifSampleDescription(ParsedAv1SampleDescription parsed)
    {
        index = parsed.Index;
        format = parsed.Format;
        dataReferenceIndex = parsed.DataReferenceIndex;
        width = parsed.Width;
        height = parsed.Height;
        horizontalResolutionFixed = parsed.HorizontalResolutionFixed;
        verticalResolutionFixed = parsed.VerticalResolutionFixed;
        compressorName = parsed.CompressorName;
        depth = parsed.Depth;
        configuration = parsed.Configuration;
        auxiliaryType = parsed.AuxiliaryType == null
            ? string.Empty : parsed.AuxiliaryType.GetTypeValue();
    }

    /// <summary>Returns the one-based sample-description index.</summary>
    /// <returns>The description index.</returns>
    public int GetIndex()
    {
        return index;
    }

    /// <summary>Returns the coded sample-entry format.</summary>
    /// <returns>The four-character format.</returns>
    public string GetFormat()
    {
        return format;
    }

    /// <summary>Returns the one-based data-reference index.</summary>
    /// <returns>The data-reference index.</returns>
    public int GetDataReferenceIndex()
    {
        return dataReferenceIndex;
    }

    /// <summary>Returns the coded width declared by the visual sample entry.</summary>
    /// <returns>The positive width.</returns>
    public int GetWidth()
    {
        return width;
    }

    /// <summary>Returns the coded height declared by the visual sample entry.</summary>
    /// <returns>The positive height.</returns>
    public int GetHeight()
    {
        return height;
    }

    /// <summary>Returns the horizontal resolution as a 16.16 fixed-point value.</summary>
    /// <returns>The decoded resolution.</returns>
    public double GetHorizontalResolution()
    {
        return horizontalResolutionFixed / 65536.0;
    }

    /// <summary>Returns the vertical resolution as a 16.16 fixed-point value.</summary>
    /// <returns>The decoded resolution.</returns>
    public double GetVerticalResolution()
    {
        return verticalResolutionFixed / 65536.0;
    }

    /// <summary>Returns the visual sample-entry compressor name.</summary>
    /// <returns>The decoded compressor name.</returns>
    public string GetCompressorName()
    {
        return compressorName;
    }

    /// <summary>Returns the visual sample-entry pixel depth.</summary>
    /// <returns>The depth field.</returns>
    public int GetDepth()
    {
        return depth;
    }

    /// <summary>Returns the auxiliary-track type declared by the sample entry.</summary>
    /// <returns>The exact auxiliary type URI, or an empty string when absent.</returns>
    public string GetAuxiliaryType()
    {
        return auxiliaryType;
    }

    /// <summary>Returns the AV1 sequence profile.</summary>
    /// <returns>The profile.</returns>
    public int GetAv1Profile()
    {
        return (configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetSequenceProfile();
    }

    /// <summary>Returns the AV1 sequence level index.</summary>
    /// <returns>The level index.</returns>
    public int GetAv1LevelIndex()
    {
        return (configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetSequenceLevelIndex();
    }

    /// <summary>Returns the AV1 sequence tier.</summary>
    /// <returns>The tier.</returns>
    public int GetAv1Tier()
    {
        return (configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetSequenceTier();
    }

    /// <summary>Returns the configured AV1 bit depth.</summary>
    /// <returns>The bit depth.</returns>
    public int GetBitDepth()
    {
        return (configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetBitDepth();
    }

    /// <summary>Returns the configured AV1 channel count.</summary>
    /// <returns>One for monochrome or three for color.</returns>
    public int GetChannelCount()
    {
        return (configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetChannelCount();
    }

    /// <summary>Reports whether horizontal chroma subsampling is enabled.</summary>
    /// <returns><c>true</c> when chroma is horizontally subsampled.</returns>
    public bool HasChromaSubsamplingX()
    {
        return (configuration ?? throw new InvalidOperationException("Invalid internal state.")).HasChromaSubsamplingX();
    }

    /// <summary>Reports whether vertical chroma subsampling is enabled.</summary>
    /// <returns><c>true</c> when chroma is vertically subsampled.</returns>
    public bool HasChromaSubsamplingY()
    {
        return (configuration ?? throw new InvalidOperationException("Invalid internal state.")).HasChromaSubsamplingY();
    }

    /// <summary>Returns a copy of the AV1 configuration OBUs.</summary>
    /// <returns>The configuration OBU bytes.</returns>
    public byte[] GetConfigObus()
    {
        return (configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetConfigObus();
    }
}
