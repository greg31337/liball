using System.Collections.Generic;

namespace image;

/// <summary>Describes the file format selected from source bytes and optional filename hints.</summary>
public class ImageFormatInfo
{
    /// <summary>The machine-readable format identifier.</summary>
    public ImageFormatKind Kind;
    /// <summary>The human-readable format name.</summary>
    public string Name;
    /// <summary>The media type normally used for this format.</summary>
    public string MimeType;
    /// <summary>The preferred filename extension without a leading period.</summary>
    public string PreferredExtension;
    /// <summary>The ISO base media brand when the format is identified from a branded container.</summary>
    public string ContainerBrand;
    /// <summary>The detector confidence from 0 to 100.</summary>
    public int Confidence;
    /// <summary>Indicates that file bytes, rather than a filename hint, established the format.</summary>
    public bool DetectedByMagic;
    /// <summary>The non-fatal conditions encountered while detecting the format.</summary>
    public List<MetadataDiagnostic> Diagnostics;

    /// <summary>Creates an unknown format result with the generic binary media type.</summary>
    public ImageFormatInfo()
    {
        Kind = ImageFormatKind.Unknown;
        Name = "";
        MimeType = "application/octet-stream";
        PreferredExtension = "";
        ContainerBrand = "";
        Confidence = 0;
        DetectedByMagic = false;
        Diagnostics = new List<MetadataDiagnostic>();
    }

    /// <summary>Creates a format description with caller-supplied names.</summary>
    /// <param name="kind">The machine-readable format identifier.</param>
    /// <param name="name">The human-readable format name.</param>
    /// <param name="mimeType">The media type for the format.</param>
    /// <param name="preferredExtension">The preferred extension without a leading period.</param>
    /// <returns>A format description initialized with the supplied names.</returns>
    internal static ImageFormatInfo Create(ImageFormatKind kind, string name, string mimeType, string preferredExtension)
    {
        ImageFormatInfo info = new ImageFormatInfo();
        info.Kind = kind;
        info.Name = name;
        info.MimeType = mimeType;
        info.PreferredExtension = preferredExtension;
        return info;
    }
}
