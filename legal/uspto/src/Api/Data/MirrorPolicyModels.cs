namespace uspto;

/// <summary>
/// Configures non-destructive local dataset mirror operations.
/// </summary>
public class MirrorPolicy
{
    /// <summary>The mirror root directory, or <c>null</c> when mirroring is not configured.</summary>
    public string? Directory = null;

    /// <summary>The product families selected for mirror planning.</summary>
    public DatasetFamily[] DatasetFamilies = new DatasetFamily[0];

    /// <summary>Whether planning avoids all transfer and filesystem mutations.</summary>
    public bool DryRun = true;

    /// <summary>The maximum number of concurrent transfers.</summary>
    public int MaximumParallelTransfers = 1;

    /// <summary>Whether downloaded files are checked when a catalog digest is available.</summary>
    public bool VerifyChecksums = true;

    /// <summary>Whether verified archives are extracted after download.</summary>
    public bool ExtractArchives = false;

    /// <summary>An optional release time below which catalog files are not selected.</summary>
    public global::System.DateTimeOffset HighWaterMark = default(global::System.DateTimeOffset);

    /// <summary>Whether <see cref="HighWaterMark"/> contains a value.</summary>
    public bool HasHighWaterMark = false;
}
