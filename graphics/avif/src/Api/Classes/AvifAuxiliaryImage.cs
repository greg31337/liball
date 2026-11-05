namespace avif;

/// <summary>
/// Describes a resolved alpha, depth, or caller-defined auxiliary association.
/// </summary>
public class AvifAuxiliaryImage : IAvifAuxiliaryImage
{
    const string AlphaType = "urn:mpeg:mpegB:cicp:systems:auxiliary:alpha";
    const string DepthType = "urn:mpeg:mpegB:cicp:systems:auxiliary:depth";

    AvifResultCode code;
    AvifImage? sourceImage;
    uint itemId;
    uint masterItemId;
    AvifAuxiliaryKind kind;
    string? type;
    uint width;
    uint height;
    int bitDepth;
    uint masterWidth;
    uint masterHeight;
    int masterBitDepth;
    bool premultiplied;
    bool resizeRequired;
    int quality = -1;

    /// <summary>Creates an alpha or depth auxiliary image for authoring.</summary>
    /// <param name="image">The image that will be copied when added to a writer.</param>
    /// <param name="kind">The alpha or depth purpose.</param>
    /// <returns>The initialized auxiliary image.</returns>
    internal static AvifAuxiliaryImage Create(
        AvifImage image,
        AvifAuxiliaryKind kind)
    {
        return new AvifAuxiliaryImage(image, kind, false);
    }

    /// <summary>Creates an alpha or depth auxiliary image for authoring.</summary>
    /// <param name="image">The image that will be copied when added to a writer.</param>
    /// <param name="kind">The alpha or depth purpose.</param>
    /// <param name="premultiplied">Whether the master color samples are premultiplied by this alpha.</param>
    public AvifAuxiliaryImage(
        AvifImage? image,
        AvifAuxiliaryKind kind,
        bool premultiplied)
    {
        InitializeAuthoring(
            image,
            kind,
            GetStandardType(kind),
            premultiplied);
    }

    /// <summary>Creates an alpha or depth auxiliary image for authoring.</summary>
    /// <returns>The initialized auxiliary image.</returns>
    internal static AvifAuxiliaryImage Create(
        AvifImage? image,
        AvifAuxiliaryKind kind,
        bool premultiplied)
    {
        return new AvifAuxiliaryImage(image, kind, premultiplied);
    }

    /// <summary>Creates a caller-defined auxiliary image for authoring.</summary>
    /// <param name="image">The image that will be copied when added to a writer.</param>
    /// <param name="type">The nonempty auxiliary type identifier.</param>
    /// <returns>The initialized auxiliary image.</returns>
    internal static AvifAuxiliaryImage Create(AvifImage image, string type)
    {
        AvifAuxiliaryImage result = new AvifAuxiliaryImage(
            image,
            AvifAuxiliaryKind.Other,
            false);
        result.InitializeAuthoring(
            image,
            AvifAuxiliaryKind.Other,
            type,
            false);
        return result;
    }

    internal static AvifAuxiliaryImage FromItems(
        AvifImageItem auxiliary,
        AvifImageItem master,
        bool premultiplied)
    {
        AvifAuxiliaryImage result = new AvifAuxiliaryImage(
            null,
            AvifAuxiliaryKind.None,
            false);
        result.sourceImage = null;
        if (auxiliary == null || master == null)
        {
            result.code = AvifResultCode.InvalidArgument;
            result.type = string.Empty;
            return result;
        }

        result.itemId = auxiliary.GetItemId();
        result.masterItemId = master.GetItemId();
        result.kind = auxiliary.GetAuxiliaryKind();
        result.type = auxiliary.GetAuxiliaryTypeName();
        result.width = auxiliary.GetPixelWidth();
        result.height = auxiliary.GetPixelHeight();
        result.bitDepth = auxiliary.GetPixelBitDepth();
        result.masterWidth = master.GetPixelWidth();
        result.masterHeight = master.GetPixelHeight();
        result.masterBitDepth = master.GetPixelBitDepth();
        result.premultiplied = premultiplied;
        result.resizeRequired = result.width != result.masterWidth
            || result.height != result.masterHeight;
        if (result.kind == AvifAuxiliaryKind.None
            || result.width == 0
            || result.height == 0
            || result.masterWidth == 0
            || result.masterHeight == 0)
        {
            result.code = AvifResultCode.InvalidItem;
        }
        else if (result.kind == AvifAuxiliaryKind.Alpha
            && result.bitDepth > 0
            && result.masterBitDepth > 0
            && result.bitDepth != result.masterBitDepth)
        {
            result.code = AvifResultCode.NonConformingData;
        }
        else
        {
            result.code = AvifResultCode.Success;
        }
        return result;
    }

    /// <summary>Returns the association validation result.</summary>
    /// <returns>A stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns the auxiliary item identifier.</summary>
    /// <returns>The auxiliary item identifier.</returns>
    public uint GetItemId()
    {
        return itemId;
    }

    /// <summary>Returns the associated master item identifier.</summary>
    /// <returns>The master item identifier.</returns>
    public uint GetMasterItemId()
    {
        return masterItemId;
    }

    /// <summary>Returns the interpreted auxiliary kind.</summary>
    /// <returns>The alpha, depth, or other kind.</returns>
    public AvifAuxiliaryKind GetKind()
    {
        return kind;
    }

    /// <summary>Returns the exact auxiliary type identifier.</summary>
    /// <returns>The auxiliary type, which may be empty for an untyped association.</returns>
    public string? GetTypeName()
    {
        return type;
    }

    /// <summary>Returns the auxiliary image width.</summary>
    /// <returns>The width from its spatial-extents property.</returns>
    public uint GetWidth()
    {
        return width;
    }

    /// <summary>Returns the auxiliary image height.</summary>
    /// <returns>The height from its spatial-extents property.</returns>
    public uint GetHeight()
    {
        return height;
    }

    /// <summary>Returns the auxiliary image bit depth.</summary>
    /// <returns>The configured depth, or minus one when unavailable.</returns>
    public int GetBitDepth()
    {
        return bitDepth;
    }

    /// <summary>Returns the master image width.</summary>
    /// <returns>The master width.</returns>
    public uint GetMasterWidth()
    {
        return masterWidth;
    }

    /// <summary>Returns the master image height.</summary>
    /// <returns>The master height.</returns>
    public uint GetMasterHeight()
    {
        return masterHeight;
    }

    /// <summary>Returns the master image bit depth.</summary>
    /// <returns>The configured master depth, or minus one when unavailable.</returns>
    public int GetMasterBitDepth()
    {
        return masterBitDepth;
    }

    /// <summary>Reports whether color samples are premultiplied by this alpha item.</summary>
    /// <returns><c>true</c> when a matching <c>prem</c> reference is present.</returns>
    public bool IsPremultiplied()
    {
        return premultiplied;
    }

    /// <summary>Reports whether decoded auxiliary samples require resizing to the master dimensions.</summary>
    /// <returns><c>true</c> when the spatial dimensions differ.</returns>
    public bool RequiresResize()
    {
        return resizeRequired;
    }

    /// <summary>Sets this auxiliary image's independent encoding quality.</summary>
    /// <param name="quality">Zero through one hundred, or negative one to inherit.</param>
    /// <returns>Success or InvalidArgument.</returns>
    public AvifResultCode SetQuality(int quality)
    {
        if (quality < -1 || quality > 100)
        {
            return AvifResultCode.InvalidArgument;
        }
        this.quality = quality;
        return AvifResultCode.Success;
    }

    /// <summary>Returns this auxiliary image's quality override.</summary>
    /// <returns>Zero through one hundred, or negative one to inherit.</returns>
    public int GetQuality()
    {
        return quality;
    }

    internal AvifImage? GetSourceImage()
    {
        return sourceImage;
    }

    static string GetStandardType(AvifAuxiliaryKind value)
    {
        if (value == AvifAuxiliaryKind.Alpha)
        {
            return AlphaType;
        }
        if (value == AvifAuxiliaryKind.Depth)
        {
            return DepthType;
        }
        return string.Empty;
    }

    void InitializeAuthoring(
        AvifImage? image,
        AvifAuxiliaryKind valueKind,
        string valueType,
        bool valuePremultiplied)
    {
        sourceImage = image;
        itemId = 0;
        masterItemId = 0;
        kind = valueKind;
        type = valueType == null ? string.Empty : valueType;
        width = image == null ? 0 : (uint)image.GetWidth();
        height = image == null ? 0 : (uint)image.GetHeight();
        bitDepth = image == null ? -1 : image.GetBitDepth();
        masterWidth = 0;
        masterHeight = 0;
        masterBitDepth = -1;
        premultiplied = valuePremultiplied;
        resizeRequired = false;

        bool standardType = type == AlphaType || type == DepthType;
        if (image == null
            || image.GetCode() != AvifResultCode.Success
            || type.Length == 0
            || type.IndexOf('\0') >= 0
            || kind == AvifAuxiliaryKind.None
            || (kind == AvifAuxiliaryKind.Alpha && type != AlphaType)
            || (kind == AvifAuxiliaryKind.Depth && type != DepthType)
            || (kind == AvifAuxiliaryKind.Other && standardType)
            || (premultiplied && kind != AvifAuxiliaryKind.Alpha)
            || (kind != AvifAuxiliaryKind.Other
                && image.GetChromaFormat() != AvifChromaFormat.Monochrome))
        {
            code = AvifResultCode.InvalidArgument;
        }
        else
        {
            code = AvifResultCode.Success;
        }
    }
}
