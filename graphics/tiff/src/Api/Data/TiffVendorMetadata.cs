namespace tiff;

/// <summary>Represents vendor metadata extracted from or prepared for a TIFF file.</summary>
public class TiffVendorMetadata
{
    /// <summary>Indicates whether the vendor metadata is present.</summary>
    public bool HasVendorMetadata;
    /// <summary>Contains the primary vendor.</summary>
    public string PrimaryVendor;
    /// <summary>Contains the primary format.</summary>
    public string PrimaryFormat;
    /// <summary>Contains the matches.</summary>
    public TiffVendorMetadataMatch[] Matches;
    /// <summary>Contains recognized Digital Negative fields.</summary>
    public TiffDngMetadata Dng;
    /// <summary>Contains recognized Aperio SVS metadata.</summary>
    public TiffVendorVariantMetadata Aperio;
    /// <summary>Contains recognized Hamamatsu NDPI metadata.</summary>
    public TiffVendorVariantMetadata Hamamatsu;
    /// <summary>Contains recognized Leica SCN metadata.</summary>
    public TiffVendorVariantMetadata Leica;
    /// <summary>Contains recognized Philips TIFF metadata.</summary>
    public TiffVendorVariantMetadata Philips;
    /// <summary>Contains recognized Roche or Ventana BIF metadata.</summary>
    public TiffVendorVariantMetadata RocheVentana;
    /// <summary>Contains recognized Zeiss LSM metadata.</summary>
    public TiffVendorVariantMetadata Zeiss;
    /// <summary>Contains recognized Micro-Manager metadata.</summary>
    public TiffVendorVariantMetadata MicroManager;
    /// <summary>Contains recognized Astro-TIFF metadata.</summary>
    public TiffVendorVariantMetadata AstroTiff;
    /// <summary>Contains opaque vendor-private payloads preserved from the image directory.</summary>
    public TiffVendorPrivatePayload[] PrivatePayloads;

    /// <summary>Creates a new instance of the TIFF Vendor Metadata class.</summary>
    public TiffVendorMetadata()
    {
        HasVendorMetadata = false;
        PrimaryVendor = "";
        PrimaryFormat = "";
        Matches = new TiffVendorMetadataMatch[0];
        Dng = new TiffDngMetadata();
        Aperio = new TiffVendorVariantMetadata();
        Hamamatsu = new TiffVendorVariantMetadata();
        Leica = new TiffVendorVariantMetadata();
        Philips = new TiffVendorVariantMetadata();
        RocheVentana = new TiffVendorVariantMetadata();
        Zeiss = new TiffVendorVariantMetadata();
        MicroManager = new TiffVendorVariantMetadata();
        AstroTiff = new TiffVendorVariantMetadata();
        PrivatePayloads = new TiffVendorPrivatePayload[0];
    }

    /// <summary>Creates a new instance of the TIFF Vendor Metadata class.</summary>
    /// <param name="matches">The matches.</param>
    /// <param name="dng">The DNG metadata.</param>
    /// <param name="variants">The named vendor variants in documented order.</param>
    /// <param name="privatePayloads">The opaque private payloads.</param>
    internal static TiffVendorMetadata Create(TiffVendorMetadataMatch[] matches, TiffDngMetadata dng, TiffVendorVariantMetadata[] variants, TiffVendorPrivatePayload[] privatePayloads)
    {
        TiffVendorMetadata value = new TiffVendorMetadata();
        value.Initialize(matches, dng, variants, privatePayloads);
        return value;
    }

    internal void Initialize(TiffVendorMetadataMatch[] matches, TiffDngMetadata dng, TiffVendorVariantMetadata[] variants, TiffVendorPrivatePayload[] privatePayloads)
    {
        Matches = CopyMatches(matches);
        HasVendorMetadata = Matches.Length > 0;
        if (HasVendorMetadata)
        {
            PrimaryVendor = Matches[0].Vendor;
            PrimaryFormat = Matches[0].Format;
        }
        else
        {
            PrimaryVendor = "";
            PrimaryFormat = "";
        }

        if (dng != null)
        {
            Dng = dng.Copy();
        }

        TiffVendorVariantMetadata[] actualVariants = CopyVariants(variants);
        if (actualVariants.Length >= 8)
        {
            Aperio = actualVariants[0];
            Hamamatsu = actualVariants[1];
            Leica = actualVariants[2];
            Philips = actualVariants[3];
            RocheVentana = actualVariants[4];
            Zeiss = actualVariants[5];
            MicroManager = actualVariants[6];
            AstroTiff = actualVariants[7];
        }

        PrivatePayloads = TiffVendorVariantMetadata.CopyPayloads(privatePayloads);
    }

    /// <summary>Returns the number of available match entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int MatchCount()
    {
        return Matches.Length;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF vendor metadata.</returns>
    internal TiffVendorMetadata Copy()
    {
        return TiffVendorMetadata.Create(Matches, Dng, GetVariants(), PrivatePayloads);
    }

    internal TiffVendorVariantMetadata[] GetVariants()
    {
        return new TiffVendorVariantMetadata[] { Aperio, Hamamatsu, Leica, Philips, RocheVentana, Zeiss, MicroManager, AstroTiff };
    }

    internal static TiffVendorVariantMetadata[] CopyVariants(TiffVendorVariantMetadata[] variants)
    {
        TiffVendorVariantMetadata[] copy = new TiffVendorVariantMetadata[8];
        int index = 0;
        while (index < copy.Length)
        {
            if (variants != null && index < variants.Length && variants[index] != null)
            {
                copy[index] = variants[index].Copy();
            }
            else
            {
                copy[index] = new TiffVendorVariantMetadata();
            }

            index = index + 1;
        }

        return copy;
    }

    internal static TiffVendorMetadataMatch[] CopyMatches(TiffVendorMetadataMatch[] matches)
    {
        if (matches == null)
        {
            return new TiffVendorMetadataMatch[0];
        }

        TiffVendorMetadataMatch[] copy = new TiffVendorMetadataMatch[matches.Length];
        int index = 0;
        while (index < matches.Length)
        {
            if (matches[index] == null)
            {
                copy[index] = new TiffVendorMetadataMatch();
            }
            else
            {
                copy[index] = matches[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }
}
