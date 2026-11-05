namespace avif;

/// <summary>
/// Parses and exposes ISO 21496-1 gain-map metadata carried by a tone-map derived item.
/// </summary>
public class AvifGainMapMetadata : IAvifGainMapMetadata, IAvifDiagnosticSource
{
    ParsedGainMapMetadata parsed;
    byte[] data;

    /// <summary>Parses a complete versioned tone-map metadata payload.</summary>
    /// <param name="payload">The complete <c>tmap</c> item payload.</param>
    public AvifGainMapMetadata(byte[] payload)
    {
        parsed = GainMapMetadataParser.Parse(payload);
        data = payload == null
            ? System.Array.Empty<byte>()
            : (byte[])payload.Clone();
    }

    /// <summary>Returns the metadata parsing and validation result.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return parsed.Code;
    }

    /// <summary>Returns diagnostics for gain-map metadata parsing.</summary>
    /// <returns>The structured metadata diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            parsed.Code,
            AvifDiagnosticCategory.Metadata,
            "The gain-map metadata operation did not complete successfully.");
    }

    /// <summary>Returns a copy of the complete versioned <c>tmap</c> metadata payload.</summary>
    /// <returns>The validated payload, or an empty array when parsing failed.</returns>
    public byte[] GetData()
    {
        return parsed.Code == AvifResultCode.Success
            ? (byte[])data.Clone()
            : System.Array.Empty<byte>();
    }

    /// <summary>Returns the syntax version.</summary>
    /// <returns>The parsed version, currently zero.</returns>
    public int GetVersion()
    {
        return parsed.Version;
    }

    /// <summary>Returns the minimum reader version required by the payload.</summary>
    /// <returns>The minimum version.</returns>
    public int GetMinimumVersion()
    {
        return parsed.MinimumVersion;
    }

    /// <summary>Returns the writer metadata version.</summary>
    /// <returns>The writer version.</returns>
    public int GetWriterVersion()
    {
        return parsed.WriterVersion;
    }

    /// <summary>Reports whether distinct parameters are present for three color channels.</summary>
    /// <returns><c>true</c> for multichannel metadata.</returns>
    public bool IsMultichannel()
    {
        return parsed.Multichannel;
    }

    /// <summary>Reports whether gain-map math uses the base image color space.</summary>
    /// <returns><c>true</c> for the base color space; otherwise the alternate color space.</returns>
    public bool UsesBaseColorSpace()
    {
        return parsed.UseBaseColorSpace;
    }

    /// <summary>Returns the base-image log2 HDR headroom.</summary>
    /// <returns>The log2-encoded headroom.</returns>
    public double GetBaseHdrHeadroom()
    {
        return GetValue(parsed.BaseHeadroom);
    }

    /// <summary>Returns the alternate-image log2 HDR headroom.</summary>
    /// <returns>The log2-encoded headroom.</returns>
    public double GetAlternateHdrHeadroom()
    {
        return GetValue(parsed.AlternateHeadroom);
    }

    /// <summary>Returns the base-image linear HDR headroom ratio.</summary>
    /// <returns>Two raised to the log2 headroom.</returns>
    public double GetBaseHdrHeadroomRatio()
    {
        return System.Math.Pow(2, GetBaseHdrHeadroom());
    }

    /// <summary>Returns the alternate-image linear HDR headroom ratio.</summary>
    /// <returns>Two raised to the log2 headroom.</returns>
    public double GetAlternateHdrHeadroomRatio()
    {
        return System.Math.Pow(2, GetAlternateHdrHeadroom());
    }

    /// <summary>Returns one channel's minimum log2 gain.</summary>
    /// <param name="channel">The zero-based red, green, or blue channel.</param>
    /// <returns>The minimum gain, or zero for an invalid channel.</returns>
    public double GetGainMapMinimum(int channel)
    {
        return GetChannelValue(parsed.Minimum, channel);
    }

    /// <summary>Returns one channel's maximum log2 gain.</summary>
    /// <param name="channel">The zero-based red, green, or blue channel.</param>
    /// <returns>The maximum gain, or zero for an invalid channel.</returns>
    public double GetGainMapMaximum(int channel)
    {
        return GetChannelValue(parsed.Maximum, channel);
    }

    /// <summary>Returns one channel's positive encoding gamma.</summary>
    /// <param name="channel">The zero-based red, green, or blue channel.</param>
    /// <returns>The gamma, or zero for an invalid channel.</returns>
    public double GetGamma(int channel)
    {
        return GetChannelValue(parsed.Gamma, channel);
    }

    /// <summary>Returns one channel's base-image linear offset.</summary>
    /// <param name="channel">The zero-based red, green, or blue channel.</param>
    /// <returns>The base offset, or zero for an invalid channel.</returns>
    public double GetBaseOffset(int channel)
    {
        return GetChannelValue(parsed.BaseOffset, channel);
    }

    /// <summary>Returns one channel's alternate-image linear offset.</summary>
    /// <param name="channel">The zero-based red, green, or blue channel.</param>
    /// <returns>The alternate offset, or zero for an invalid channel.</returns>
    public double GetAlternateOffset(int channel)
    {
        return GetChannelValue(parsed.AlternateOffset, channel);
    }

    /// <summary>Computes the signed gain-map weight for a display headroom ratio.</summary>
    /// <param name="displayHeadroom">The positive ratio relative to reference white.</param>
    /// <returns>A value from minus one through one, or zero for invalid metadata or headroom.</returns>
    public double GetWeight(double displayHeadroom)
    {
        if (parsed.Code != AvifResultCode.Success
            || displayHeadroom < 1
            || double.IsNaN(displayHeadroom)
            || double.IsInfinity(displayHeadroom))
        {
            return 0;
        }
        double baseHeadroom = GetBaseHdrHeadroom();
        double alternateHeadroom = GetAlternateHdrHeadroom();
        if (baseHeadroom == alternateHeadroom)
        {
            return 0;
        }
        double displayLog2 = System.Math.Log(displayHeadroom, 2);
        double fraction = (displayLog2 - baseHeadroom)
            / (alternateHeadroom - baseHeadroom);
        fraction = System.Math.Max(0, System.Math.Min(1, fraction));
        return alternateHeadroom < baseHeadroom ? -fraction : fraction;
    }

    static double GetChannelValue(
        GainMapRational[] values,
        int channel)
    {
        if (channel < 0 || channel >= 3)
        {
            return 0;
        }
        return GetValue(values[channel]);
    }

    static double GetValue(GainMapRational? value)
    {
        return value == null || value.Denominator == 0
            ? 0
            : value.GetValue();
    }
}
