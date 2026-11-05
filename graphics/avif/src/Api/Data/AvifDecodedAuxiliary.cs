namespace avif;

/// <summary>Describes one decoded item- or track-associated auxiliary image.</summary>
public class AvifDecodedAuxiliary
{
    /// <summary>The interpreted alpha, depth, or caller-defined purpose.</summary>
    public AvifAuxiliaryKind Kind;

    /// <summary>The exact auxiliary type identifier, or an empty string.</summary>
    public string TypeName;

    /// <summary>The auxiliary item identifier, or zero for a timed-track auxiliary.</summary>
    public uint ItemId;

    /// <summary>The master item identifier, or zero for a timed-track auxiliary.</summary>
    public uint MasterItemId;

    /// <summary>The auxiliary track identifier, or zero for an item auxiliary.</summary>
    public uint TrackId;

    /// <summary>The master track identifier, or zero for an item auxiliary.</summary>
    public uint MasterTrackId;

    /// <summary>Whether the associated color samples are premultiplied by this alpha image.</summary>
    public bool Premultiplied;

    /// <summary>The caller-owned decoded auxiliary image.</summary>
    public AvifImage Image;

    /// <summary>Creates one decoded auxiliary output description.</summary>
    /// <param name="kind">The interpreted auxiliary purpose.</param>
    /// <param name="typeName">The exact auxiliary type identifier.</param>
    /// <param name="itemId">The auxiliary item identifier.</param>
    /// <param name="masterItemId">The master item identifier.</param>
    /// <param name="trackId">The auxiliary track identifier.</param>
    /// <param name="masterTrackId">The master track identifier.</param>
    /// <param name="premultiplied">Whether color is premultiplied by this alpha.</param>
    /// <param name="image">The caller-owned decoded auxiliary image.</param>
    public AvifDecodedAuxiliary(
        AvifAuxiliaryKind kind,
        string? typeName,
        uint itemId,
        uint masterItemId,
        uint trackId,
        uint masterTrackId,
        bool premultiplied,
        AvifImage image)
    {
        Kind = kind;
        TypeName = typeName == null ? string.Empty : typeName;
        ItemId = itemId;
        MasterItemId = masterItemId;
        TrackId = trackId;
        MasterTrackId = masterTrackId;
        Premultiplied = premultiplied;
        Image = image;
    }
}
