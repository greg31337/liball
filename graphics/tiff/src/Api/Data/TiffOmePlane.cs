namespace tiff;

/// <summary>Maps one OME C/Z/T plane to a TIFF IFD in the current or a referenced file.</summary>
public class TiffOmePlane
{
    /// <summary>Contains the zero-based OME image index.</summary>
    public int OmeImageIndex;
    /// <summary>Contains the zero-based plane index within the OME image.</summary>
    public int PlaneIndex;
    /// <summary>Contains the zero-based channel coordinate.</summary>
    public int ChannelIndex;
    /// <summary>Contains the zero-based depth coordinate.</summary>
    public int ZIndex;
    /// <summary>Contains the zero-based time coordinate.</summary>
    public int TimeIndex;
    /// <summary>Indicates whether an IFD mapping is present.</summary>
    public bool HasIfd;
    /// <summary>Contains the zero-based TIFF IFD index.</summary>
    public int IfdIndex;
    /// <summary>Contains the referenced file UUID.</summary>
    public string Uuid;
    /// <summary>Contains the referenced file name.</summary>
    public string FileName;
    /// <summary>Indicates whether the mapped plane belongs to another file.</summary>
    public bool IsExternal;

    /// <summary>Creates an empty OME plane mapping.</summary>
    public TiffOmePlane()
    {
        OmeImageIndex = 0;
        PlaneIndex = 0;
        ChannelIndex = 0;
        ZIndex = 0;
        TimeIndex = 0;
        HasIfd = false;
        IfdIndex = 0;
        Uuid = "";
        FileName = "";
        IsExternal = false;
    }

    internal static TiffOmePlane Create(int omeImageIndex, int planeIndex, int channelIndex, int zIndex, int timeIndex, bool hasIfd, int ifdIndex, string uuid, string fileName, bool isExternal)
    {
        TiffOmePlane value = new TiffOmePlane();
        value.OmeImageIndex = omeImageIndex;
        value.PlaneIndex = planeIndex;
        value.ChannelIndex = channelIndex;
        value.ZIndex = zIndex;
        value.TimeIndex = timeIndex;
        value.HasIfd = hasIfd;
        value.IfdIndex = ifdIndex;
        value.Uuid = TiffOmeMetadata.NormalizeString(uuid);
        value.FileName = TiffOmeMetadata.NormalizeString(fileName);
        value.IsExternal = isExternal;
        return value;
    }

    internal TiffOmePlane Copy()
    {
        return TiffOmePlane.Create(OmeImageIndex, PlaneIndex, ChannelIndex, ZIndex, TimeIndex, HasIfd, IfdIndex, Uuid, FileName, IsExternal);
    }
}
