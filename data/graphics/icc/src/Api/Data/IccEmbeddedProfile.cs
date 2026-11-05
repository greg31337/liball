namespace icc;

/// <summary>Represents one profile located in an external carrier.</summary>
public class IccEmbeddedProfile
{
    /// <summary>Gets or sets the carrier format.</summary>
    public IccEmbeddedFormat Format;
    /// <summary>Gets or sets the zero-based profile index.</summary>
    public int Index;
    /// <summary>Gets or sets the carrier byte location.</summary>
    public IccByteLocation Location;
    /// <summary>Gets or sets the extracted profile.</summary>
    public IccProfile Profile;
    /// <summary>Gets or sets carrier segment or chunk locations.</summary>
    public IccByteLocation[] Parts;
    /// <summary>Gets or sets an optional inspection summary of the extracted profile.</summary>
    public IccProfileSummary? Summary;

    /// <summary>Initializes an unlocated raw profile.</summary>
    public IccEmbeddedProfile()
    {
        Format = IccEmbeddedFormat.Unknown;
        Index = -1;
        Location = new IccByteLocation();
        Profile = new IccProfile();
        Parts = new IccByteLocation[0];
        Summary = null;
    }
}
