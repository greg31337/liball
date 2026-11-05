namespace avif;

/// <summary>
/// Owns and validates an ICC v2 or v4 profile and exposes bounded color transforms.
/// </summary>
public class AvifIccProfile : IAvifIccProfile, IAvifDiagnosticSource
{
    IccParsedProfile? profile;
    AvifResultCode code;

    /// <summary>Parses an ICC profile using the default metadata limits.</summary>
    /// <param name="data">The complete ICC profile bytes.</param>
    /// <returns>The parsed ICC profile.</returns>
    internal static AvifIccProfile Create(byte[] data)
    {
        return new AvifIccProfile(data, new AvifSecurityLimits());
    }

    /// <summary>Parses an ICC profile using explicit resource limits.</summary>
    /// <param name="data">The complete ICC profile bytes.</param>
    /// <param name="limits">The metadata and ICC byte limits.</param>
    public AvifIccProfile(byte[] data, AvifSecurityLimits? limits)
    {
        Initialize(data, limits);
    }

    /// <summary>Parses an ICC profile using explicit resource limits.</summary>
    /// <returns>The parsed ICC profile.</returns>
    internal static AvifIccProfile Create(
        byte[] data,
        AvifSecurityLimits? limits)
    {
        return new AvifIccProfile(data, limits);
    }

    /// <summary>Returns the profile parsing result.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns diagnostics for the ICC parse or transform operation.</summary>
    /// <returns>The structured metadata diagnostics.</returns>
    public AvifDiagnosticReport GetDiagnostics()
    {
        return AvifDiagnosticReport.FromCode(
            code,
            AvifDiagnosticCategory.Metadata,
            "The ICC profile operation did not complete successfully.");
    }

    /// <summary>Returns a copy of the complete profile bytes.</summary>
    /// <returns>The retained profile, or an empty array after a parsing failure.</returns>
    public byte[] GetData()
    {
        return profile == null
            ? System.Array.Empty<byte>() : (byte[])profile.Data.Clone();
    }

    /// <summary>Returns the ICC major version.</summary>
    /// <returns>Two or four for a valid supported profile.</returns>
    public int GetVersionMajor()
    {
        return profile == null ? 0 : profile.VersionMajor;
    }

    /// <summary>Returns the ICC minor version nibble.</summary>
    /// <returns>The minor version, or zero when unavailable.</returns>
    public int GetVersionMinor()
    {
        return profile == null ? 0 : profile.VersionMinor;
    }

    /// <summary>Returns the four-character profile/device class signature.</summary>
    /// <returns>The class signature, or an empty string.</returns>
    public string GetDeviceClass()
    {
        return profile == null ? string.Empty : profile.DeviceClass;
    }

    /// <summary>Returns the four-character data color-space signature.</summary>
    /// <returns>The color-space signature, or an empty string.</returns>
    public string GetColorSpace()
    {
        return profile == null ? string.Empty : profile.ColorSpace;
    }

    /// <summary>Returns the four-character profile connection-space signature.</summary>
    /// <returns><c>XYZ </c>, <c>Lab </c>, or an empty string.</returns>
    public string GetProfileConnectionSpace()
    {
        return profile == null ? string.Empty : profile.Pcs;
    }

    /// <summary>Returns the header's default rendering intent.</summary>
    /// <returns>The profile rendering intent.</returns>
    public AvifRenderingIntent GetRenderingIntent()
    {
        return profile == null
            ? AvifRenderingIntent.Perceptual : profile.Intent;
    }

    /// <summary>Returns the device color-space channel count.</summary>
    /// <returns>One through fifteen, or zero when unavailable.</returns>
    public int GetChannelCount()
    {
        return profile == null
            ? 0 : IccParser.GetChannelCount(profile.ColorSpace);
    }

    /// <summary>Returns the number of validated tag table entries.</summary>
    /// <returns>The tag count.</returns>
    public int GetTagCount()
    {
        return profile == null ? 0 : profile.Tags.Count;
    }

    /// <summary>Returns one tag signature.</summary>
    /// <param name="index">The zero-based tag index.</param>
    /// <returns>The four-character signature, or an empty string.</returns>
    public string GetTagSignature(int index)
    {
        if (profile == null || index < 0 || index >= profile.Tags.Count)
        {
            return string.Empty;
        }
        return profile.Tags[index].Signature;
    }

    /// <summary>Returns one tag's type signature.</summary>
    /// <param name="index">The zero-based tag index.</param>
    /// <returns>The four-character type signature, or an empty string.</returns>
    public string GetTagType(int index)
    {
        if (profile == null || index < 0 || index >= profile.Tags.Count)
        {
            return string.Empty;
        }
        return profile.Tags[index].Type;
    }

    /// <summary>Reports whether a device-to-PCS transform is available.</summary>
    /// <returns><c>true</c> for a matrix/TRC or A-to-B LUT transform.</returns>
    public bool CanTransformToPcs()
    {
        return IccTransform.CanTransformToPcs(profile);
    }

    /// <summary>Reports whether a PCS-to-device transform is available.</summary>
    /// <returns><c>true</c> for an invertible matrix/TRC or B-to-A LUT transform.</returns>
    public bool CanTransformFromPcs()
    {
        return IccTransform.CanTransformFromPcs(profile);
    }

    /// <summary>Transforms device values to D50 XYZ profile-connection values.</summary>
    /// <param name="source">The normalized device components.</param>
    /// <param name="destination">A caller array with space for three XYZ values.</param>
    /// <param name="intent">The requested rendering intent.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode TransformToPcs(
        double[] source,
        double[] destination,
        AvifRenderingIntent intent)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        return IccTransform.ToPcs(profile, source, destination, intent);
    }

    /// <summary>Transforms D50 XYZ profile-connection values to device values.</summary>
    /// <param name="source">Three D50 XYZ values.</param>
    /// <param name="destination">A caller array sized for the device components.</param>
    /// <param name="intent">The requested rendering intent.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode TransformFromPcs(
        double[] source,
        double[] destination,
        AvifRenderingIntent intent)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        return IccTransform.FromPcs(profile, source, destination, intent);
    }

    /// <summary>Converts device values directly into another ICC profile.</summary>
    /// <param name="source">The normalized components in this profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="destination">A caller array sized for destination components.</param>
    /// <param name="intent">The rendering intent used by both transforms.</param>
    /// <returns>A stable result code.</returns>
    public AvifResultCode Convert(
        double[] source,
        AvifIccProfile destinationProfile,
        double[] destination,
        AvifRenderingIntent intent)
    {
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        if (destinationProfile == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (destinationProfile.code != AvifResultCode.Success)
        {
            return destinationProfile.code;
        }
        double[] pcs = new double[3];
        AvifResultCode result = IccTransform.ToPcs(
            profile, source, pcs, intent);
        if (result != AvifResultCode.Success)
        {
            return result;
        }
        return IccTransform.FromPcs(
            destinationProfile.profile, pcs, destination, intent);
    }

    void Initialize(byte[] data, AvifSecurityLimits? limits)
    {
        IccParseResult result = IccParser.Parse(data, limits);
        code = result.Code;
        profile = result.Profile;
    }
}
