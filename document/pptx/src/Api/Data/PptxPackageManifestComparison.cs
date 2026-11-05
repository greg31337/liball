using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Describes differences between two effective PowerPoint package manifests.
/// </summary>
public class PptxPackageManifestComparison
{
    /// <summary>
    /// Stores the inspected left package manifest.
    /// </summary>
    public PptxPackageManifest Left;
    /// <summary>
    /// Stores the inspected right package manifest.
    /// </summary>
    public PptxPackageManifest Right;
    /// <summary>
    /// Indicates whether both manifests contain the same entry names, metadata, and bytes.
    /// </summary>
    public bool Equal;
    /// <summary>
    /// Contains entry names present only in the right manifest.
    /// </summary>
    public List<string> AddedEntryNames;
    /// <summary>
    /// Contains entry names present only in the left manifest.
    /// </summary>
    public List<string> RemovedEntryNames;
    /// <summary>
    /// Contains entry names whose metadata or effective bytes differ.
    /// </summary>
    public List<string> ChangedEntryNames;

    /// <summary>
    /// Initializes an empty package manifest comparison.
    /// </summary>
    public PptxPackageManifestComparison()
    {
        Left = new PptxPackageManifest();
        Right = new PptxPackageManifest();
        Equal = true;
        AddedEntryNames = new List<string>();
        RemovedEntryNames = new List<string>();
        ChangedEntryNames = new List<string>();
    }
}
