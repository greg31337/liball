namespace fix;

/// <summary>Retains Orchestra lifecycle, deprecation, and replacement pedigree.</summary>
public class OrchestraPedigree
{
    /// <summary>The version in which the item was added.</summary>
    public string AddedVersion = "";
    /// <summary>The extension pack in which the item was added.</summary>
    public string AddedExtensionPack = "";
    /// <summary>The kind of change represented by this revision.</summary>
    public string ChangeType = "";
    /// <summary>The version in which the item was deprecated.</summary>
    public string DeprecatedVersion = "";
    /// <summary>The extension pack in which the item was deprecated.</summary>
    public string DeprecatedExtensionPack = "";
    /// <summary>The issue or work-item reference.</summary>
    public string Issue = "";
    /// <summary>The last-modified version.</summary>
    public string LastModifiedVersion = "";
    /// <summary>The version in which the item was replaced.</summary>
    public string ReplacedVersion = "";
    /// <summary>The extension pack in which the item was replaced.</summary>
    public string ReplacedExtensionPack = "";
    /// <summary>The replacement field identifier in source lexical form.</summary>
    public string ReplacedByFieldId = "";
    /// <summary>The support disposition.</summary>
    public string Supported = "";
    /// <summary>The version in which the item was updated.</summary>
    public string UpdatedVersion = "";
    /// <summary>The extension pack in which the item was updated.</summary>
    public string UpdatedExtensionPack = "";
}
