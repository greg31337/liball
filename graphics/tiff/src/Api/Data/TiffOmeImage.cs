namespace tiff;

/// <summary>Describes one image and its pixel planes in an OME dataset.</summary>
public class TiffOmeImage
{
    /// <summary>Contains the zero-based image index in the OME document.</summary>
    public int ImageIndex;
    /// <summary>Contains the OME image identifier.</summary>
    public string Id;
    /// <summary>Contains the optional OME image name.</summary>
    public string Name;
    /// <summary>Contains the OME pixels identifier.</summary>
    public string PixelsId;
    /// <summary>Contains the declared OME dimension order.</summary>
    public TiffOmeDimensionOrder DimensionOrder;
    /// <summary>Contains the original dimension-order text.</summary>
    public string DimensionOrderText;
    /// <summary>Contains the X dimension.</summary>
    public ulong SizeX;
    /// <summary>Contains the Y dimension.</summary>
    public ulong SizeY;
    /// <summary>Contains the channel dimension.</summary>
    public int SizeC;
    /// <summary>Contains the depth dimension.</summary>
    public int SizeZ;
    /// <summary>Contains the time dimension.</summary>
    public int SizeT;
    /// <summary>Contains the OME pixel type text.</summary>
    public string PixelType;
    /// <summary>Contains expanded C/Z/T-to-IFD plane mappings.</summary>
    public TiffOmePlane[] Planes;

    /// <summary>Creates an empty OME image description.</summary>
    public TiffOmeImage()
    {
        ImageIndex = 0;
        Id = "";
        Name = "";
        PixelsId = "";
        DimensionOrder = TiffOmeDimensionOrder.Unknown;
        DimensionOrderText = "";
        SizeX = 0;
        SizeY = 0;
        SizeC = 0;
        SizeZ = 0;
        SizeT = 0;
        PixelType = "";
        Planes = new TiffOmePlane[0];
    }

    internal static TiffOmeImage Create(int imageIndex, string id, string name, string pixelsId, TiffOmeDimensionOrder dimensionOrder, string dimensionOrderText, ulong sizeX, ulong sizeY, int sizeC, int sizeZ, int sizeT, string pixelType, TiffOmePlane[] planes)
    {
        TiffOmeImage value = new TiffOmeImage();
        value.ImageIndex = imageIndex;
        value.Id = TiffOmeMetadata.NormalizeString(id);
        value.Name = TiffOmeMetadata.NormalizeString(name);
        value.PixelsId = TiffOmeMetadata.NormalizeString(pixelsId);
        value.DimensionOrder = dimensionOrder;
        value.DimensionOrderText = TiffOmeMetadata.NormalizeString(dimensionOrderText);
        value.SizeX = sizeX;
        value.SizeY = sizeY;
        value.SizeC = sizeC;
        value.SizeZ = sizeZ;
        value.SizeT = sizeT;
        value.PixelType = TiffOmeMetadata.NormalizeString(pixelType);
        value.Planes = CopyPlanes(planes);
        return value;
    }

    internal TiffOmeImage Copy()
    {
        return TiffOmeImage.Create(ImageIndex, Id, Name, PixelsId, DimensionOrder, DimensionOrderText, SizeX, SizeY, SizeC, SizeZ, SizeT, PixelType, Planes);
    }

    internal static TiffOmePlane[] CopyPlanes(TiffOmePlane[] planes)
    {
        if (planes == null)
        {
            return new TiffOmePlane[0];
        }

        TiffOmePlane[] copy = new TiffOmePlane[planes.Length];
        int index = 0;
        while (index < planes.Length)
        {
            copy[index] = planes[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
