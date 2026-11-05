namespace tiff;

/// <summary>Contains the values and diagnostics produced by a ICC profile read operation.</summary>
public class TiffIccProfileReadResult
{
    /// <summary>Indicates whether the operation completed without error diagnostics.</summary>
    public bool Success;
    /// <summary>Contains the profile.</summary>
    public TiffIccProfile Profile;
    /// <summary>Contains the warnings and errors produced by the operation.</summary>
    public TiffDiagnostics Diagnostics;

    /// <summary>Creates a new instance of the TIFF ICC Profile Read Result class.</summary>
    public TiffIccProfileReadResult()
    {
        Success = true;
        Profile = new TiffIccProfile();
        Diagnostics = new TiffDiagnostics();
    }

    /// <summary>Creates a new instance of the TIFF ICC Profile Read Result class.</summary>
    /// <param name="profile">The profile.</param>
    /// <param name="diagnostics">The collection that receives validation warnings and errors.</param>
    internal static TiffIccProfileReadResult Create(TiffIccProfile profile, TiffDiagnostics diagnostics)
    {
        TiffIccProfileReadResult value = new TiffIccProfileReadResult();
        value.Initialize(profile, diagnostics);
        return value;
    }

    internal void Initialize(TiffIccProfile profile, TiffDiagnostics diagnostics)
    {
        if (profile == null)
        {
            Profile = new TiffIccProfile();
        }
        else
        {
            Profile = profile.Copy();
        }

        Diagnostics = diagnostics;
        Success = !diagnostics.HasErrors();
    }
}
