using System;

namespace pdf;

/// <summary>
/// Stores the archival profile and output-intent information used to prepare PDF/A output.
/// </summary>
public class PdfAPreparationOptions
{
    /// <summary>Selects the level-B archival conformance profile.</summary>
    public PdfAConformance Conformance;
    /// <summary>Stores the complete ICC profile used as the document output intent.</summary>
    public byte[] IccProfileData;
    /// <summary>Stores the standardized or custom output-condition identifier.</summary>
    public string OutputConditionIdentifier;
    /// <summary>Stores a human-readable output condition.</summary>
    public string OutputCondition;
    /// <summary>Stores the registry that defines the output-condition identifier.</summary>
    public string RegistryName;
    /// <summary>Stores additional output-intent information.</summary>
    public string Info;
    /// <summary>Stores the relationship assigned to ordinary attachments when preparing PDF/A-3b.</summary>
    public PdfAssociatedFileRelationship DefaultAssociatedFileRelationship;

    /// <summary>
    /// Creates PDF/A-3b options with standard sRGB output-condition labels and no ICC bytes.
    /// </summary>
    public PdfAPreparationOptions()
    {
        Conformance = PdfAConformance.PdfA3B;
        IccProfileData = new byte[0];
        OutputConditionIdentifier = "sRGB IEC61966-2.1";
        OutputCondition = "sRGB IEC61966-2.1";
        RegistryName = "http://www.color.org";
        Info = "sRGB IEC61966-2.1";
        DefaultAssociatedFileRelationship = PdfAssociatedFileRelationship.Data;
    }

    internal PdfAPreparationOptions Copy()
    {
        PdfAPreparationOptions copied = new PdfAPreparationOptions();
        copied.Conformance = Conformance;
        copied.IccProfileData = new byte[IccProfileData.Length];
        Array.Copy(IccProfileData, copied.IccProfileData, IccProfileData.Length);
        copied.OutputConditionIdentifier = OutputConditionIdentifier;
        copied.OutputCondition = OutputCondition;
        copied.RegistryName = RegistryName;
        copied.Info = Info;
        copied.DefaultAssociatedFileRelationship = DefaultAssociatedFileRelationship;
        return copied;
    }
}
