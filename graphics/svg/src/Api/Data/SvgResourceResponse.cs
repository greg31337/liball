namespace svg;

/// <summary>Contains caller-provided resource data and metadata.</summary>
public class SvgResourceResponse
{
    /// <summary>The caller-owned byte source, or <c>null</c>.</summary>
    public byte[]? Bytes;
    /// <summary>The caller-owned stream source, or <c>null</c>.</summary>
    public System.IO.Stream? Stream;
    /// <summary>The declared media type, or an empty string.</summary>
    public string DeclaredMediaType;
    /// <summary>The actual media type, or an empty string when not known.</summary>
    public string ActualMediaType;
    /// <summary>The final URI after caller-managed redirects.</summary>
    public string FinalUri;
    /// <summary>The caller-managed redirect count represented by the final URI.</summary>
    public int RedirectCount;
    /// <summary>Whether later resource stages may cache the response.</summary>
    public bool Cacheable;
    /// <summary>Optional image dimensions and decoded-content availability.</summary>
    public SvgImageMetadata? ImageMetadata;
    /// <summary>Caller-provided diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    /// <summary>Creates an empty response to be populated by a callback.</summary>
    public SvgResourceResponse()
    {
        Bytes = null;
        Stream = null;
        DeclaredMediaType = "";
        ActualMediaType = "";
        FinalUri = "";
        RedirectCount = 0;
        Cacheable = false;
        ImageMetadata = null;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }

    /// <summary>Tests whether exactly one usable data source and valid metadata are present.</summary>
    /// <returns><c>true</c> when this response can be consumed.</returns>
    internal bool IsValid()
    {
        if ((Bytes == null) == (Stream == null)) return false;
        if (DeclaredMediaType == null || ActualMediaType == null || FinalUri == null || RedirectCount < 0 || Diagnostics == null) return false;
        if (!ValidImageMetadata(ImageMetadata)) return false;
        int index = 0;
        while (index < Diagnostics.Count)
        {
            if (Diagnostics[index] == null) return false;
            index = index + 1;
        }
        return Stream == null || Stream.CanRead;
    }

    internal static bool ValidImageMetadata(SvgImageMetadata? metadata)
    {
        if (metadata == null || metadata.IntrinsicSize == null) return true;
        SvgIntrinsicSize intrinsic = metadata.IntrinsicSize;
        if (intrinsic.HasWidth && (!SvgFiniteMath.IsFinite(intrinsic.Width) || intrinsic.Width <= 0)) return false;
        if (intrinsic.HasHeight && (!SvgFiniteMath.IsFinite(intrinsic.Height) || intrinsic.Height <= 0)) return false;
        if (intrinsic.HasAspectRatio && (!SvgFiniteMath.IsFinite(intrinsic.AspectRatio) || intrinsic.AspectRatio <= 0)) return false;
        return intrinsic.HasWidth || intrinsic.HasHeight || intrinsic.HasAspectRatio;
    }
}
