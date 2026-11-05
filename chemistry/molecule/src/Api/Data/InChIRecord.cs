using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Stores the parsed fields for one inchi record.
/// </summary>
public class InChIRecord
{
    /// <summary>
    /// The original text.
    /// </summary>
    public string OriginalText;
    /// <summary>
    /// The prefix.
    /// </summary>
    public string Prefix;
    /// <summary>
    /// The version.
    /// </summary>
    public string Version;
    /// <summary>
    /// Whether the value is standard.
    /// </summary>
    public bool IsStandard;
    /// <summary>
    /// The formula layer.
    /// </summary>
    public string FormulaLayer;
    /// <summary>
    /// The connectivity layer.
    /// </summary>
    public string ConnectivityLayer;
    /// <summary>
    /// The hydrogen layer.
    /// </summary>
    public string HydrogenLayer;
    /// <summary>
    /// The tautomer layer.
    /// </summary>
    public string TautomerLayer;
    /// <summary>
    /// The charge layer.
    /// </summary>
    public string ChargeLayer;
    /// <summary>
    /// The proton layer.
    /// </summary>
    public string ProtonLayer;
    /// <summary>
    /// The stereo layer.
    /// </summary>
    public string StereoLayer;
    /// <summary>
    /// The isotope layer.
    /// </summary>
    public string IsotopeLayer;
    /// <summary>
    /// The fixed hlayer.
    /// </summary>
    public string FixedHLayer;
    /// <summary>
    /// The other layers.
    /// </summary>
    public List<MoleculeMetadataEntry> OtherLayers;
    /// <summary>
    /// Diagnostics produced while processing the value.
    /// </summary>
    public MoleculeReport Report;
    /// <summary>
    /// Whether the operation completed without an error diagnostic.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Initializes a new instance of the <see cref="InChIRecord"/> class.
    /// </summary>
    public InChIRecord()
    {
        OriginalText = "";
        Prefix = "";
        Version = "";
        IsStandard = false;
        FormulaLayer = "";
        ConnectivityLayer = "";
        HydrogenLayer = "";
        TautomerLayer = "";
        ChargeLayer = "";
        ProtonLayer = "";
        StereoLayer = "";
        IsotopeLayer = "";
        FixedHLayer = "";
        OtherLayers = new List<MoleculeMetadataEntry>();
        Report = new MoleculeReport();
        Success = true;
    }

    internal bool HasFormula()
    {
        return FormulaLayer.Length > 0;
    }

    internal bool HasConnectivity()
    {
        return ConnectivityLayer.Length > 0;
    }

    internal bool HasHydrogen()
    {
        return HydrogenLayer.Length > 0;
    }

    internal bool HasTautomer()
    {
        return TautomerLayer.Length > 0;
    }

    internal bool HasCharge()
    {
        return ChargeLayer.Length > 0;
    }

    internal bool HasProton()
    {
        return ProtonLayer.Length > 0;
    }

    internal bool HasStereo()
    {
        return StereoLayer.Length > 0;
    }

    internal bool HasIsotope()
    {
        return IsotopeLayer.Length > 0;
    }

    internal bool HasFixedH()
    {
        return FixedHLayer.Length > 0;
    }

    /// <summary>
    /// Returns the layer.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    internal string GetLayer(string label)
    {
        string normalized = NormalizeString(label);
        if (normalized == "formula")
        {
            return FormulaLayer;
        }

        if (normalized == "c")
        {
            return ConnectivityLayer;
        }

        if (normalized == "h")
        {
            return HydrogenLayer;
        }

        if (normalized == "tautomer")
        {
            return TautomerLayer;
        }

        if (normalized == "q")
        {
            return ChargeLayer;
        }

        if (normalized == "p")
        {
            return ProtonLayer;
        }

        if (normalized == "stereo")
        {
            return StereoLayer;
        }

        if (normalized == "i")
        {
            return IsotopeLayer;
        }

        if (normalized == "f")
        {
            return FixedHLayer;
        }

        int index = 0;
        while (index < OtherLayers.Count)
        {
            if (OtherLayers[index].Key == normalized)
            {
                return OtherLayers[index].Value;
            }

            index = index + 1;
        }

        return "";
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}

