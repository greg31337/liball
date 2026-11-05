using System.Collections.Generic;

namespace image;

/// <summary>Describes one frame, page, or image item within a source.</summary>
public class ImageMetadataFrame
{
    /// <summary>The zero-based frame position in <see cref="ImageMetadataDocument.Frames"/>.</summary>
    public int Index;
    /// <summary>The display or relationship role assigned to the frame.</summary>
    public ImageMetadataRole Role;
    /// <summary>The source container item identifier, or negative one when the format has no item identifier.</summary>
    public long ContainerItemId;
    /// <summary>The zero-based parent frame position, or negative one when the frame has no known parent.</summary>
    public int ParentFrameIndex;
    /// <summary>The frame width in pixels.</summary>
    public int Width;
    /// <summary>The frame height in pixels.</summary>
    public int Height;
    /// <summary>The full canvas width in pixels for formats whose frames occupy only part of a canvas.</summary>
    public int CanvasWidth;
    /// <summary>The full canvas height in pixels for formats whose frames occupy only part of a canvas.</summary>
    public int CanvasHeight;
    /// <summary>The horizontal placement of the frame within its canvas in pixels.</summary>
    public int CanvasX;
    /// <summary>The vertical placement of the frame within its canvas in pixels.</summary>
    public int CanvasY;
    /// <summary>The ratio of one pixel's displayed width to its displayed height, or zero when unavailable.</summary>
    public double PixelAspectRatio;
    /// <summary>The horizontal pixel density in <see cref="ResolutionUnit"/>, or zero when unavailable.</summary>
    public double HorizontalResolution;
    /// <summary>The vertical pixel density in <see cref="ResolutionUnit"/>, or zero when unavailable.</summary>
    public double VerticalResolution;
    /// <summary>The source unit for <see cref="HorizontalResolution"/> and <see cref="VerticalResolution"/>, or empty when unavailable.</summary>
    public string ResolutionUnit;
    /// <summary>The number of stored bits per channel or sample when known.</summary>
    public int BitDepth;
    /// <summary>The number of color and alpha channels when known.</summary>
    public int ChannelCount;
    /// <summary>Indicates whether the frame can contain transparent samples.</summary>
    public bool HasAlpha;
    /// <summary>Indicates whether the frame belongs to an animated image.</summary>
    public bool IsAnimated;
    /// <summary>The frame display duration in milliseconds, or zero when unavailable.</summary>
    public int DurationMilliseconds;
    /// <summary>The compression scheme used for the frame samples.</summary>
    public ImageCompressionKind Compression;
    /// <summary>The frame color model or named color space.</summary>
    public ImageColorSpaceKind ColorSpace;
    /// <summary>The source format's color type, mode, or sample-model description.</summary>
    public string SourceColorType;
    /// <summary>Indicates that the source stores this frame using interlaced passes.</summary>
    public bool IsInterlaced;
    /// <summary>Indicates that the source stores this frame using progressive encoding.</summary>
    public bool IsProgressive;
    /// <summary>The transform required to display the frame in its intended orientation.</summary>
    public ImageOrientation Orientation;
    /// <summary>The metadata directories scoped to this frame.</summary>
    public List<MetadataDirectory> Directories;

    /// <summary>Creates an empty frame with zero dimensions and initialized directories.</summary>
    public ImageMetadataFrame()
    {
        Index = 0;
        Role = ImageMetadataRole.Unknown;
        ContainerItemId = -1;
        ParentFrameIndex = -1;
        Width = 0;
        Height = 0;
        CanvasWidth = 0;
        CanvasHeight = 0;
        CanvasX = 0;
        CanvasY = 0;
        PixelAspectRatio = 0;
        HorizontalResolution = 0;
        VerticalResolution = 0;
        ResolutionUnit = "";
        BitDepth = 0;
        ChannelCount = 0;
        HasAlpha = false;
        IsAnimated = false;
        DurationMilliseconds = 0;
        Compression = ImageCompressionKind.Unknown;
        ColorSpace = ImageColorSpaceKind.Unknown;
        SourceColorType = "";
        IsInterlaced = false;
        IsProgressive = false;
        Orientation = ImageOrientation.Unknown;
        Directories = new List<MetadataDirectory>();
    }

    internal MetadataDirectory AddDirectory(MetadataDirectory? directory)
    {
        MetadataDirectory? actual = directory;
        if (actual == null)
        {
            actual = new MetadataDirectory();
        }

        actual.FrameIndex = Index;
        Directories.Add(actual);
        return actual;
    }

    internal MetadataDirectory AddDirectoryWithName(MetadataFamily family, string group, string name)
    {
        MetadataDirectory directory = MetadataDirectory.Create(family, group, name);
        return AddDirectory(directory);
    }
}
