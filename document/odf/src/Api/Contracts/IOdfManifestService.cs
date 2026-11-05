namespace odf;

/// <summary>
/// Reads, writes, queries, and edits OpenDocument package manifests.
/// </summary>
public interface IOdfManifestService
{
    /// <summary>
    /// Parses manifest XML bytes.
    /// </summary>
    /// <param name="data">The manifest XML bytes.</param>
    /// <returns>The parsed manifest.</returns>
    public OdfManifest ReadManifestBytes(byte[] data);
    /// <summary>
    /// Serializes a manifest to XML bytes.
    /// </summary>
    /// <param name="manifest">The manifest to serialize.</param>
    /// <param name="options">The serialization options.</param>
    /// <returns>The serialized manifest XML.</returns>
    public byte[] WriteManifestBytes(
        OdfManifest manifest,
        OdfSaveOptions options);
    /// <summary>
    /// Finds a manifest entry by exact full path.
    /// </summary>
    /// <param name="manifest">The manifest to search.</param>
    /// <param name="path">The exact manifest full path.</param>
    /// <returns>The matching entry, or <c>null</c>.</returns>
    public OdfManifestEntry? FindManifestEntry(
        OdfManifest manifest,
        string path);
    /// <summary>
    /// Adds a manifest entry when its full path is not present.
    /// </summary>
    /// <param name="manifest">The manifest to edit.</param>
    /// <param name="entry">The entry to append.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult AddManifestEntry(
        OdfManifest manifest,
        OdfManifestEntry entry);
    /// <summary>
    /// Replaces a manifest entry identified by its current full path.
    /// </summary>
    /// <param name="manifest">The manifest to edit.</param>
    /// <param name="path">The current full path.</param>
    /// <param name="entry">The replacement entry.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult UpdateManifestEntry(
        OdfManifest manifest,
        string path,
        OdfManifestEntry entry);
    /// <summary>
    /// Removes a manifest entry by exact full path.
    /// </summary>
    /// <param name="manifest">The manifest to edit.</param>
    /// <param name="path">The full path to remove.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult RemoveManifestEntry(
        OdfManifest manifest,
        string path);
    /// <summary>
    /// Reconciles a loaded package manifest with its current ZIP entries.
    /// </summary>
    /// <param name="package">The package whose manifest is synchronized.</param>
    /// <returns>The synchronization result.</returns>
    public OdfEditResult SynchronizePackageManifest(OdfPackage package);
}
