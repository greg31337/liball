namespace tiff;

/// <summary>Defines access to caller-resolved external overview sources and their base-image relationships.</summary>
public interface ITiffExternalOverviewSet
{
    /// <summary>Indicates whether all supplied sources were inspected without errors.</summary>
    /// <returns><c>true</c> when no error diagnostic is present.</returns>
    bool GetSuccess();
    /// <summary>Indicates whether the external source handles remain open.</summary>
    /// <returns><c>true</c> until the set or one of its usable sources is closed.</returns>
    bool GetIsOpen();
    /// <summary>Closes all external TIFF handles owned by this set.</summary>
    void Close();
    /// <summary>Returns the caller-owned base image used during resolution.</summary>
    /// <returns>The base image.</returns>
    TiffImage GetBaseImage();
    /// <summary>Returns the number of caller-supplied external sources.</summary>
    /// <returns>The external source count.</returns>
    int GetSourceCount();
    /// <summary>Returns one opened external TIFF source.</summary>
    /// <param name="sourceIndex">The zero-based index in the caller-supplied path or stream array.</param>
    /// <returns>The selected TIFF file handle.</returns>
    TiffFile GetSourceFile(int sourceIndex);
    /// <summary>Returns the number of compatible external overview relationships.</summary>
    /// <returns>The relationship count.</returns>
    int GetRelationshipCount();
    /// <summary>Returns one detached external overview relationship.</summary>
    /// <param name="relationshipIndex">The zero-based relationship index.</param>
    /// <returns>The selected relationship.</returns>
    TiffExternalOverviewRelationship GetRelationship(int relationshipIndex);
    /// <summary>Returns the external image identified by one relationship.</summary>
    /// <param name="relationshipIndex">The zero-based relationship index.</param>
    /// <returns>The selected external overview image.</returns>
    TiffImage GetOverviewImage(int relationshipIndex);
    /// <summary>Returns collected resolution and source-open diagnostics.</summary>
    /// <returns>The diagnostic collection.</returns>
    TiffDiagnostics GetDiagnostics();
}
