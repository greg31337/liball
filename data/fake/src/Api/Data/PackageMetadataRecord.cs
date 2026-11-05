namespace fake;

/// <summary>Represents correlated metadata for a synthetic software package fixture.</summary>
/// <remarks>The record describes generated test data and makes no claim about a published or installed package.</remarks>
public class PackageMetadataRecord
{
    /// <summary>The project-authored package name.</summary>
    public string Name = string.Empty;

    /// <summary>The generated three-component application version.</summary>
    public string Version = string.Empty;

    /// <summary>The selected framework family.</summary>
    public string FrameworkName = string.Empty;

    /// <summary>The reviewed framework version correlated with <c>FrameworkName</c>.</summary>
    public string FrameworkVersion = string.Empty;

    /// <summary>The target-framework identifier correlated with the framework name and version.</summary>
    public string TargetFramework = string.Empty;

    /// <summary>The package-manager label correlated with the framework family.</summary>
    public string PackageManager = string.Empty;

    /// <summary>The syntax used by <c>InstallPath</c>.</summary>
    public FilePathStyle PathStyle;

    /// <summary>A relative synthetic package path using <c>PathStyle</c>.</summary>
    public string InstallPath = string.Empty;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>A stable marker identifying synthetic package metadata.</summary>
    public string TestMarker = string.Empty;

    /// <summary>Whether the record claims to represent a published package, always <c>false</c>.</summary>
    public bool RepresentsPublishedPackage;

    /// <summary>Whether the record claims to represent an installed package, always <c>false</c>.</summary>
    public bool RepresentsInstalledPackage;

    /// <summary>Whether reviewed framework versions are claimed to be current, always <c>false</c>.</summary>
    public bool VersionsAreCurrent;

    internal PackageMetadataRecord()
    {
    }
}
