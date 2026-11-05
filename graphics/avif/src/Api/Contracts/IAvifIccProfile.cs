namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifIccProfile"/>.</summary>
public interface IAvifIccProfile
{
    /// <summary>Returns the profile parsing result.</summary>
    /// <returns>A stable result code.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns a copy of the complete profile bytes.</summary>
    /// <returns>The retained profile, or an empty array after a parsing failure.</returns>
    byte[] GetData();

    /// <summary>Returns the ICC major version.</summary>
    /// <returns>Two or four for a valid supported profile.</returns>
    int GetVersionMajor();

    /// <summary>Returns the ICC minor version nibble.</summary>
    /// <returns>The minor version, or zero when unavailable.</returns>
    int GetVersionMinor();

    /// <summary>Returns the four-character profile/device class signature.</summary>
    /// <returns>The class signature, or an empty string.</returns>
    string GetDeviceClass();

    /// <summary>Returns the four-character data color-space signature.</summary>
    /// <returns>The color-space signature, or an empty string.</returns>
    string GetColorSpace();

    /// <summary>Returns the four-character profile connection-space signature.</summary>
    /// <returns><c>XYZ </c>, <c>Lab </c>, or an empty string.</returns>
    string GetProfileConnectionSpace();

    /// <summary>Returns the header's default rendering intent.</summary>
    /// <returns>The profile rendering intent.</returns>
    AvifRenderingIntent GetRenderingIntent();

    /// <summary>Returns the device color-space channel count.</summary>
    /// <returns>One through fifteen, or zero when unavailable.</returns>
    int GetChannelCount();

    /// <summary>Returns the number of validated tag table entries.</summary>
    /// <returns>The tag count.</returns>
    int GetTagCount();

    /// <summary>Returns one tag signature.</summary>
    /// <param name="index">The zero-based tag index.</param>
    /// <returns>The four-character signature, or an empty string.</returns>
    string GetTagSignature(int index);

    /// <summary>Returns one tag's type signature.</summary>
    /// <param name="index">The zero-based tag index.</param>
    /// <returns>The four-character type signature, or an empty string.</returns>
    string GetTagType(int index);

    /// <summary>Reports whether a device-to-PCS transform is available.</summary>
    /// <returns><c>true</c> for a matrix/TRC or A-to-B LUT transform.</returns>
    bool CanTransformToPcs();

    /// <summary>Reports whether a PCS-to-device transform is available.</summary>
    /// <returns><c>true</c> for an invertible matrix/TRC or B-to-A LUT transform.</returns>
    bool CanTransformFromPcs();

    /// <summary>Transforms device values to D50 XYZ profile-connection values.</summary>
    /// <param name="source">The normalized device components.</param>
    /// <param name="destination">A caller array with space for three XYZ values.</param>
    /// <param name="intent">The requested rendering intent.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode TransformToPcs(
        double[] source,
        double[] destination,
        AvifRenderingIntent intent);

    /// <summary>Transforms D50 XYZ profile-connection values to device values.</summary>
    /// <param name="source">Three D50 XYZ values.</param>
    /// <param name="destination">A caller array sized for the device components.</param>
    /// <param name="intent">The requested rendering intent.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode TransformFromPcs(
        double[] source,
        double[] destination,
        AvifRenderingIntent intent);

    /// <summary>Converts device values directly into another ICC profile.</summary>
    /// <param name="source">The normalized components in this profile.</param>
    /// <param name="destinationProfile">The destination profile.</param>
    /// <param name="destination">A caller array sized for destination components.</param>
    /// <param name="intent">The rendering intent used by both transforms.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode Convert(
        double[] source,
        AvifIccProfile destinationProfile,
        double[] destination,
        AvifRenderingIntent intent);
}
