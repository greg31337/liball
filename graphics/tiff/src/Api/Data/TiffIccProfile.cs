namespace tiff;

/// <summary>Represents ICC profile used by the public TIFF API.</summary>
public class TiffIccProfile
{
    /// <summary>Indicates whether the profile is present.</summary>
    public bool HasProfile;
    /// <summary>Contains the resulting bytes in their documented storage or pixel order.</summary>
    public byte[] Bytes;

    /// <summary>Creates a new instance of the TIFF ICC Profile class.</summary>
    public TiffIccProfile()
    {
        HasProfile = false;
        Bytes = new byte[0];
    }

    /// <summary>Creates a new instance of the TIFF ICC Profile class.</summary>
    /// <param name="hasProfile">The has profile.</param>
    /// <param name="bytes">The bytes.</param>
    internal static TiffIccProfile Create(bool hasProfile, byte[] bytes)
    {
        TiffIccProfile value = new TiffIccProfile();
        value.Initialize(hasProfile, bytes);
        return value;
    }

    internal void Initialize(bool hasProfile, byte[] bytes)
    {
        HasProfile = hasProfile;
        Bytes = TiffTagValue.CopyByteArray(bytes);
    }

    /// <summary>Returns the number of available byte entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int ByteCount()
    {
        return Bytes.Length;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF ICC profile.</returns>
    internal TiffIccProfile Copy()
    {
        return TiffIccProfile.Create(HasProfile, Bytes);
    }
}
