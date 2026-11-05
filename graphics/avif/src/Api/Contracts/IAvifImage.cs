using System;

namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifImage"/>.</summary>
public interface IAvifImage
{
    /// <summary>Returns the current image state.</summary>
    /// <returns>The stable result code, including closure of shared storage.</returns>
    AvifResultCode GetCode();

    /// <summary>Returns the luma width in pixels.</summary>
    /// <returns>The image width, or zero after closing.</returns>
    int GetWidth();

    /// <summary>Returns the luma height in pixels.</summary>
    /// <returns>The image height, or zero after closing.</returns>
    int GetHeight();

    /// <summary>Returns the integer sample bit depth.</summary>
    /// <returns>The image bit depth.</returns>
    int GetBitDepth();

    /// <summary>Returns the chroma layout.</summary>
    /// <returns>The monochrome, 4:2:0, 4:2:2, or 4:4:4 layout.</returns>
    AvifChromaFormat GetChromaFormat();

    /// <summary>Returns the chroma sample position.</summary>
    /// <returns>The stored chroma position.</returns>
    AvifChromaSamplePosition GetChromaSamplePosition();

    /// <summary>Returns the integer sample range.</summary>
    /// <returns>The full or limited range.</returns>
    AvifRange GetRange();

    /// <summary>Returns an independent copy of the image's H.273 color signaling.</summary>
    /// <returns>The color information, or null after the image is closed.</returns>
    AvifColorInfo? GetColorInfo();

    /// <summary>Replaces the image's H.273 color signaling and sample-range interpretation.</summary>
    /// <param name="value">The color information to copy.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetColorInfo(AvifColorInfo? value);

    /// <summary>Returns the validated embedded ICC profile.</summary>
    /// <returns>The immutable profile, or null when no profile is attached.</returns>
    AvifIccProfile? GetIccProfile();

    /// <summary>Attaches or clears a validated embedded ICC profile.</summary>
    /// <param name="value">The profile to attach, or null to clear it.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode SetIccProfile(AvifIccProfile? value);

    /// <summary>Returns the alpha representation.</summary>
    /// <returns>The none, straight, or premultiplied alpha mode.</returns>
    AvifAlphaMode GetAlphaMode();

    /// <summary>Attaches a decoded monochrome auxiliary image as full-resolution alpha.</summary>
    /// <param name="auxiliary">The monochrome alpha image.</param>
    /// <param name="premultiplied">Whether this image's color samples are premultiplied.</param>
    /// <param name="filter">The filter used when auxiliary dimensions differ.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode AttachAlpha(
        AvifImage auxiliary,
        bool premultiplied,
        AvifResamplingFilter filter);

    /// <summary>Returns the number of allocated or viewed planes.</summary>
    /// <returns>One through four planes according to chroma and alpha.</returns>
    int GetPlaneCount();

    /// <summary>Returns one image plane.</summary>
    /// <param name="kind">The requested luma, chroma, or alpha kind.</param>
    /// <returns>The plane, or null when that kind is absent.</returns>
    AvifPlane? GetPlane(AvifPlaneKind kind);

    /// <summary>Returns the combined allocated row storage in bytes.</summary>
    /// <returns>The sum of stride multiplied by height for every plane.</returns>
    long GetStorageByteCount();

    /// <summary>Creates a non-owning image view sharing this image's plane storage.</summary>
    /// <param name="x">The left luma coordinate.</param>
    /// <param name="y">The top luma coordinate.</param>
    /// <param name="width">The view width in luma pixels.</param>
    /// <param name="height">The view height in luma pixels.</param>
    /// <returns>A shared image view, or an image carrying an error code.</returns>
    AvifImage CreateView(int x, int y, int width, int height);

    /// <summary>Creates an independent copy of this image and its visible samples.</summary>
    /// <returns>An owning image copy, or an image carrying an error code.</returns>
    AvifImage CreateCopy();

    /// <summary>Copies all visible planes from a layout-compatible image.</summary>
    /// <param name="source">The source image.</param>
    /// <returns>A stable result code.</returns>
    AvifResultCode CopyFrom(AvifImage source);

    /// <summary>Closes every plane and releases owned allocations.</summary>
    /// <returns><c>Success</c>; repeated calls are harmless.</returns>
    AvifResultCode Close();
}
