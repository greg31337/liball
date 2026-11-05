namespace tiff;

/// <summary>Represents photoshop image resource block used by the public TIFF API.</summary>
public class TiffPhotoshopImageResourceBlock
{
    /// <summary>Contains the signature.</summary>
    public string Signature;
    /// <summary>Contains the resource id.</summary>
    public ushort ResourceId;
    /// <summary>Contains the name.</summary>
    public string Name;
    /// <summary>Contains the data.</summary>
    public byte[] Data;

    /// <summary>Creates a new instance of the TIFF Photoshop Image Resource Block class.</summary>
    public TiffPhotoshopImageResourceBlock()
    {
        Signature = "";
        ResourceId = 0;
        Name = "";
        Data = new byte[0];
    }

    /// <summary>Creates a new instance of the TIFF Photoshop Image Resource Block class.</summary>
    /// <param name="signature">The signature.</param>
    /// <param name="resourceId">The resource id.</param>
    /// <param name="name">The name.</param>
    /// <param name="data">The data.</param>
    internal static TiffPhotoshopImageResourceBlock Create(string signature, ushort resourceId, string name, byte[] data)
    {
        TiffPhotoshopImageResourceBlock value = new TiffPhotoshopImageResourceBlock();
        value.Initialize(signature, resourceId, name, data);
        return value;
    }

    internal void Initialize(string signature, ushort resourceId, string name, byte[] data)
    {
        if (signature == null)
        {
            Signature = "";
        }
        else
        {
            Signature = signature;
        }

        ResourceId = resourceId;
        if (name == null)
        {
            Name = "";
        }
        else
        {
            Name = name;
        }

        Data = TiffTagValue.CopyByteArray(data);
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF photoshop image resource block.</returns>
    internal TiffPhotoshopImageResourceBlock Copy()
    {
        return TiffPhotoshopImageResourceBlock.Create(Signature, ResourceId, Name, Data);
    }
}
