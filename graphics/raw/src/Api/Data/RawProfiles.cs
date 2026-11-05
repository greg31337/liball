namespace raw;

/// <summary>Contains versioned read and write camera profiles serialized by <see cref="RawProfileSchemaCodec"/>.</summary>
public class RawProfileSchemaDocument
{
    /// <summary>The profile schema version.</summary>
    public int SchemaVersion;
    /// <summary>Profiles used only to recognize and decode existing files.</summary>
    public RawReadProfile[] ReadProfiles;
    /// <summary>Profiles independently qualified to encode new files.</summary>
    public RawWriteProfile[] WriteProfiles;

    /// <summary>Creates an empty current-version profile document.</summary>
    public RawProfileSchemaDocument()
    {
        SchemaVersion = RawProfileSchemaCodec.CurrentSchemaVersion;
        ReadProfiles = System.Array.Empty<RawReadProfile>();
        WriteProfiles = System.Array.Empty<RawWriteProfile>();
    }

    /// <summary>Validates schema identity, profile separation, bounds, and all nested values.</summary>
    /// <param name="limits">Resource limits.</param>
    /// <returns>All profile findings.</returns>
    internal RawValidationReport Validate(RawLimits limits)
    {
        return RawProfileSchemaValidation.Validate(this, limits);
    }
}

/// <summary>Defines the exact camera identity and firmware range shared by profile matching and reporting.</summary>
public class RawProfileIdentity
{
    /// <summary>The container family.</summary>
    public RawFormat Format;
    /// <summary>The canonical manufacturer name.</summary>
    public string Make;
    /// <summary>The canonical camera model.</summary>
    public string? Model;
    /// <summary>The stable numeric or textual camera identifier, or an empty string.</summary>
    public string CameraId;
    /// <summary>Alternative make/model spellings accepted by later matching logic.</summary>
    public string[] Aliases;
    /// <summary>The inclusive minimum firmware text, or an empty string.</summary>
    public string FirmwareMinimum;
    /// <summary>The inclusive maximum firmware text, or an empty string.</summary>
    public string FirmwareMaximum;

    /// <summary>Creates an empty profile identity.</summary>
    public RawProfileIdentity()
    {
        Format = RawFormat.Unknown;
        Make = string.Empty;
        Model = string.Empty;
        CameraId = string.Empty;
        Aliases = System.Array.Empty<string>();
        FirmwareMinimum = string.Empty;
        FirmwareMaximum = string.Empty;
    }
}

/// <summary>Defines structural evidence used to distinguish files with otherwise similar identity metadata.</summary>
public class RawProfileFingerprint
{
    /// <summary>The TIFF byte order.</summary>
    public RawByteOrder ByteOrder;
    /// <summary>Whether the container uses BigTIFF offsets.</summary>
    public bool BigTiff;
    /// <summary>The observed stored width, or zero when not part of the fingerprint.</summary>
    public int Width;
    /// <summary>The observed stored height, or zero when not part of the fingerprint.</summary>
    public int Height;
    /// <summary>The observed meaningful bit depth, or zero when not part of the fingerprint.</summary>
    public int BitDepth;
    /// <summary>The observed TIFF compression identifier, or zero when not part of the fingerprint.</summary>
    public int Compression;
    /// <summary>The observed samples per pixel, or zero when not part of the fingerprint.</summary>
    public int SamplesPerPixel;
    /// <summary>Stable required IFD paths.</summary>
    public string[] RequiredIfds;
    /// <summary>Stable required tags expressed as <c>path/tag</c> strings.</summary>
    public string[] RequiredTags;
    /// <summary>The maker-note signature, or an empty string.</summary>
    public string MakerNoteSignature;
    /// <summary>An optional lowercase structural digest, or an empty string.</summary>
    public string StructuralHash;

    /// <summary>Creates an empty structural fingerprint.</summary>
    public RawProfileFingerprint()
    {
        ByteOrder = RawByteOrder.LittleEndian;
        BigTiff = false;
        Width = 0;
        Height = 0;
        BitDepth = 0;
        Compression = 0;
        SamplesPerPixel = 0;
        RequiredIfds = System.Array.Empty<string>();
        RequiredTags = System.Array.Empty<string>();
        MakerNoteSignature = string.Empty;
        StructuralHash = string.Empty;
    }
}

/// <summary>Defines stored, active, masked, and color-filter geometry for a profile.</summary>
public class RawProfileGeometry
{
    /// <summary>The complete stored sample width.</summary>
    public int StoredWidth;
    /// <summary>The complete stored sample height.</summary>
    public int StoredHeight;
    /// <summary>The active image area.</summary>
    public RawArea? ActiveArea;
    /// <summary>Optical-black or otherwise masked sample areas.</summary>
    public RawArea[] MaskedAreas;
    /// <summary>The color-filter array, or <c>null</c> for non-CFA data.</summary>
    public RawCfaPattern? Cfa;

    /// <summary>Creates empty profile geometry.</summary>
    public RawProfileGeometry()
    {
        StoredWidth = 0;
        StoredHeight = 0;
        ActiveArea = null;
        MaskedAreas = System.Array.Empty<RawArea>();
        Cfa = null;
    }
}

/// <summary>Defines the container codec and named layout quirks required by a profile.</summary>
public class RawProfileCodec
{
    /// <summary>The stable codec name.</summary>
    public string Name;
    /// <summary>The TIFF compression identifier, or zero for a non-TIFF codec.</summary>
    public int Compression;
    /// <summary>The packed-sample convention.</summary>
    public RawPacking Packing;
    /// <summary>The stored word width in bits.</summary>
    public int StoredWordBits;
    /// <summary>The predictor identifier, or zero when absent.</summary>
    public int Predictor;
    /// <summary>The TIFF planar configuration, normally one or two.</summary>
    public int PlanarConfiguration;
    /// <summary>The required row-byte alignment.</summary>
    public int RowAlignment;
    /// <summary>The first row using an alternate table or predictor, or <c>-1</c>.</summary>
    public int SplitRow;
    /// <summary>Profile-specific slice widths in stored samples.</summary>
    public int[] SliceWidths;
    /// <summary>Stable named codec or layout quirks.</summary>
    public string[] Quirks;

    /// <summary>Creates empty codec rules.</summary>
    public RawProfileCodec()
    {
        Name = string.Empty;
        Compression = 0;
        Packing = RawPacking.ByteAligned;
        StoredWordBits = 0;
        Predictor = 0;
        PlanarConfiguration = 1;
        RowAlignment = 1;
        SplitRow = -1;
        SliceWidths = System.Array.Empty<int>();
        Quirks = System.Array.Empty<string>();
    }
}

/// <summary>Defines sensor black/white levels, white balance, matrices, illuminant, and baseline exposure.</summary>
public class RawProfileCalibration
{
    /// <summary>Black levels in profile channel order.</summary>
    public double[] BlackLevels;
    /// <summary>White levels in profile channel order.</summary>
    public double[] WhiteLevels;
    /// <summary>As-shot white-balance multipliers in profile channel order.</summary>
    public double[] WhiteBalance;
    /// <summary>The row-major camera-to-XYZ color matrix, or an empty array.</summary>
    public double[] ColorMatrix;
    /// <summary>The row-major forward matrix, or an empty array.</summary>
    public double[] ForwardMatrix;
    /// <summary>The calibration illuminant name, or an empty string.</summary>
    public string Illuminant;
    /// <summary>The default scene-linear exposure adjustment in stops.</summary>
    public double BaselineExposure;

    /// <summary>Creates empty calibration data.</summary>
    public RawProfileCalibration()
    {
        BlackLevels = System.Array.Empty<double>();
        WhiteLevels = System.Array.Empty<double>();
        WhiteBalance = System.Array.Empty<double>();
        ColorMatrix = System.Array.Empty<double>();
        ForwardMatrix = System.Array.Empty<double>();
        Illuminant = string.Empty;
        BaselineExposure = 0;
    }
}

/// <summary>Records where one profile value or group of values came from.</summary>
public class RawProfileProvenance
{
    /// <summary>The stable dotted path of the supported field.</summary>
    public string Field;
    /// <summary>The source citation, sample identifier, or stable URL.</summary>
    public string Source;
    /// <summary>The independent source category.</summary>
    public RawProfileProvenanceKind Kind;
    /// <summary>The source license or redistribution statement, or an empty string.</summary>
    public string? License;
    /// <summary>The retrieval or observation date in ISO form, or an empty string.</summary>
    public string Retrieved;
    /// <summary>Additional review notes, or an empty string.</summary>
    public string Notes;

    /// <summary>Creates an empty provenance record.</summary>
    public RawProfileProvenance()
    {
        Field = string.Empty;
        Source = string.Empty;
        Kind = RawProfileProvenanceKind.Unknown;
        License = string.Empty;
        Retrieved = string.Empty;
        Notes = string.Empty;
    }
}

/// <summary>Records one independent compatibility observation for a write profile.</summary>
public class RawProfileEvidence
{
    /// <summary>The compatibility check category.</summary>
    public RawProfileEvidenceKind Kind;
    /// <summary>The validating tool or application.</summary>
    public string Tool;
    /// <summary>The exact tool version, or an empty string.</summary>
    public string Version;
    /// <summary>The sample or generated-file identifier.</summary>
    public string SampleId;
    /// <summary>The compatibility check performed.</summary>
    public string Check;
    /// <summary>The recorded outcome.</summary>
    public RawProfileEvidenceResult Result;
    /// <summary>Additional outcome details, or an empty string.</summary>
    public string Details;

    /// <summary>Creates an empty compatibility-evidence record.</summary>
    public RawProfileEvidence()
    {
        Kind = RawProfileEvidenceKind.Unknown;
        Tool = string.Empty;
        Version = string.Empty;
        SampleId = string.Empty;
        Check = string.Empty;
        Result = RawProfileEvidenceResult.Unknown;
        Details = string.Empty;
    }
}

/// <summary>Defines one independently sourced profile for recognizing and decoding existing files.</summary>
public class RawReadProfile
{
    /// <summary>The globally stable read-profile identifier.</summary>
    public string Id;
    /// <summary>Camera identity, aliases, and firmware bounds.</summary>
    public RawProfileIdentity Identity;
    /// <summary>The structural matching fingerprint.</summary>
    public RawProfileFingerprint Fingerprint;
    /// <summary>Stored and active sensor geometry.</summary>
    public RawProfileGeometry Geometry;
    /// <summary>Codec rules and quirks.</summary>
    public RawProfileCodec Codec;
    /// <summary>Calibration data.</summary>
    public RawProfileCalibration Calibration;
    /// <summary>Per-field provenance records.</summary>
    public RawProfileProvenance[] Provenance;
    /// <summary>Whether callers must explicitly opt in to use the profile.</summary>
    public bool Experimental;

    /// <summary>Creates an empty read profile.</summary>
    public RawReadProfile()
    {
        Id = string.Empty;
        Identity = new RawProfileIdentity();
        Fingerprint = new RawProfileFingerprint();
        Geometry = new RawProfileGeometry();
        Codec = new RawProfileCodec();
        Calibration = new RawProfileCalibration();
        Provenance = System.Array.Empty<RawProfileProvenance>();
        Experimental = false;
    }
}

/// <summary>Defines one separately qualified profile for encoding new files.</summary>
public class RawWriteProfile
{
    /// <summary>The globally stable write-profile identifier.</summary>
    public string Id;
    /// <summary>The required read-profile identifier used for reopen validation.</summary>
    public string ReadProfileId;
    /// <summary>Camera identity, aliases, and firmware bounds.</summary>
    public RawProfileIdentity Identity;
    /// <summary>The structural fingerprint the writer must emit.</summary>
    public RawProfileFingerprint Fingerprint;
    /// <summary>Stored and active sensor geometry.</summary>
    public RawProfileGeometry Geometry;
    /// <summary>Codec rules and quirks.</summary>
    public RawProfileCodec Codec;
    /// <summary>Calibration data.</summary>
    public RawProfileCalibration Calibration;
    /// <summary>Per-field provenance records.</summary>
    public RawProfileProvenance[] Provenance;
    /// <summary>Independent application and tool compatibility evidence.</summary>
    public RawProfileEvidence[] Evidence;
    /// <summary>Whether callers must explicitly opt in to use the writer.</summary>
    public bool Experimental;

    /// <summary>Creates an empty write profile.</summary>
    public RawWriteProfile()
    {
        Id = string.Empty;
        ReadProfileId = string.Empty;
        Identity = new RawProfileIdentity();
        Fingerprint = new RawProfileFingerprint();
        Geometry = new RawProfileGeometry();
        Codec = new RawProfileCodec();
        Calibration = new RawProfileCalibration();
        Provenance = System.Array.Empty<RawProfileProvenance>();
        Evidence = System.Array.Empty<RawProfileEvidence>();
        Experimental = true;
    }
}
