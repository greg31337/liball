namespace odf;

/// <summary>
/// Identifies a category of explicit document repair.
/// </summary>
public enum OdfRepairKind
{
    /// <summary>
    /// No repair category is selected.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// Restores or normalizes the package mimetype entry.
    /// </summary>
    PackageMimetype = 1,
    /// <summary>
    /// Restores the package manifest entry.
    /// </summary>
    PackageManifest = 2,
    /// <summary>
    /// Synchronizes manifest state with package entries.
    /// </summary>
    ManifestSynchronization = 3,
    /// <summary>
    /// Adds or corrects an XML namespace binding.
    /// </summary>
    NamespaceBinding = 4,
    /// <summary>
    /// Synchronizes materialized XML ID state.
    /// </summary>
    XmlIdCache = 5,
    /// <summary>
    /// Replaces an invalid XML ID value.
    /// </summary>
    XmlIdValue = 6,
    /// <summary>
    /// Corrects an unambiguous local reference.
    /// </summary>
    BrokenReference = 7
}
