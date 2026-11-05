using System.Collections.Generic;

namespace image;

/// <summary>Provides commonly needed metadata in one format-independent object.</summary>
/// <remarks>Fields retain default values when the source does not contain a usable corresponding tag.</remarks>
public class ImageMetadataSummary
{
    /// <summary>The primary image width in pixels.</summary>
    public int Width;
    /// <summary>The primary image height in pixels.</summary>
    public int Height;
    /// <summary>The number of frames, pages, or image items discovered.</summary>
    public int FrameCount;
    /// <summary>The ratio of one pixel's displayed width to its displayed height, or zero when unavailable.</summary>
    public double PixelAspectRatio;
    /// <summary>The normalized horizontal pixel density in <see cref="ResolutionUnit"/>, or zero when unavailable.</summary>
    public double HorizontalResolution;
    /// <summary>The normalized vertical pixel density in <see cref="ResolutionUnit"/>, or zero when unavailable.</summary>
    public double VerticalResolution;
    /// <summary>The source unit for <see cref="HorizontalResolution"/> and <see cref="VerticalResolution"/>, or empty when unavailable.</summary>
    public string ResolutionUnit;
    /// <summary>The normalized display orientation.</summary>
    public ImageOrientation Orientation;
    /// <summary>The camera or device manufacturer text.</summary>
    public string CameraMake;
    /// <summary>The camera or device model text.</summary>
    public string CameraModel;
    /// <summary>The lens model text.</summary>
    public string LensModel;
    /// <summary>The original capture date and time text.</summary>
    /// <remarks>The text remains unchanged even when <see cref="CaptureTime"/> contains a parsed representation.</remarks>
    public string CaptureTimeText;
    /// <summary>The parsed capture date and time, or an invalid result when the source is absent or unsupported.</summary>
    public MetadataDate CaptureTime;
    /// <summary>The normalized geographic position, or an invalid coordinate when unavailable.</summary>
    public GpsCoordinate Gps;
    /// <summary>The creator, artist, or author text.</summary>
    public string Author;
    /// <summary>The copyright notice.</summary>
    public string Copyright;
    /// <summary>The image title.</summary>
    public string Title;
    /// <summary>The image description or caption.</summary>
    public string Description;
    /// <summary>The deduplicated keywords found in supported metadata families.</summary>
    public List<string> Keywords;
    /// <summary>The normalized rating value, or zero when unavailable.</summary>
    public int Rating;
    /// <summary>Indicates whether an embedded color profile was found.</summary>
    public bool HasColorProfile;
    /// <summary>The embedded color profile name when one could be decoded.</summary>
    public string ColorProfileName;
    /// <summary>The normalized image color model or named RGB space.</summary>
    public ImageColorSpaceKind ColorSpace;
    /// <summary>The source format's color type, mode, or sample-model description.</summary>
    public string SourceColorType;
    /// <summary>Indicates that the primary image is stored using interlaced passes.</summary>
    public bool IsInterlaced;
    /// <summary>Indicates that the primary image is stored using progressive encoding.</summary>
    public bool IsProgressive;
    /// <summary>Indicates that the source contains an animation.</summary>
    public bool IsAnimated;
    /// <summary>The declared animation repeat count, where zero can mean infinite for formats that use that convention.</summary>
    public int AnimationLoopCount;
    /// <summary>The sum of known frame display durations in milliseconds.</summary>
    public long TotalDurationMilliseconds;
    /// <summary>The source resolution rendered as human-readable text.</summary>
    public string ResolutionText;
    /// <summary>The software or firmware reported as having created or modified the image.</summary>
    public string Software;

    /// <summary>Creates an empty summary with initialized GPS and keyword values.</summary>
    public ImageMetadataSummary()
    {
        Width = 0;
        Height = 0;
        FrameCount = 0;
        PixelAspectRatio = 0;
        HorizontalResolution = 0;
        VerticalResolution = 0;
        ResolutionUnit = "";
        Orientation = ImageOrientation.Unknown;
        CameraMake = "";
        CameraModel = "";
        LensModel = "";
        CaptureTimeText = "";
        CaptureTime = new MetadataDate();
        Gps = new GpsCoordinate();
        Author = "";
        Copyright = "";
        Title = "";
        Description = "";
        Keywords = new List<string>();
        Rating = 0;
        HasColorProfile = false;
        ColorProfileName = "";
        ColorSpace = ImageColorSpaceKind.Unknown;
        SourceColorType = "";
        IsInterlaced = false;
        IsProgressive = false;
        IsAnimated = false;
        AnimationLoopCount = 0;
        TotalDurationMilliseconds = 0;
        ResolutionText = "";
        Software = "";
    }
}
