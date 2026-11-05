namespace tiff;

/// <summary>Keeps caller-resolved external overview sources open and exposes their base-image relationships.</summary>
/// <remarks>The base image remains caller-owned. Closing this set closes only the external TIFF handles, which in turn honor the supplied read options for stream ownership.</remarks>
public class TiffExternalOverviewSet : ITiffExternalOverviewSet
{
    internal TiffImage BaseImage;
    internal TiffFile[] SourceFiles;
    internal TiffExternalOverviewRelationship[] Relationships;
    internal TiffDiagnostics Diagnostics;
    internal bool IsOpen;

    internal TiffExternalOverviewSet()
    {
        BaseImage = new TiffImage();
        SourceFiles = new TiffFile[0];
        Relationships = new TiffExternalOverviewRelationship[0];
        Diagnostics = new TiffDiagnostics();
        IsOpen = false;
    }

    internal void Initialize(TiffImage baseImage, TiffFile[] sourceFiles, TiffExternalOverviewRelationship[] relationships, TiffDiagnostics diagnostics)
    {
        if (baseImage != null)
        {
            BaseImage = baseImage;
        }

        if (sourceFiles != null)
        {
            SourceFiles = sourceFiles;
        }

        if (relationships != null)
        {
            Relationships = relationships;
        }

        if (diagnostics != null)
        {
            Diagnostics = diagnostics;
        }

        IsOpen = true;
    }

    /// <summary>Indicates whether all supplied sources were inspected without errors.</summary>
    /// <returns><c>true</c> when no error diagnostic is present.</returns>
    public bool GetSuccess()
    {
        return !Diagnostics.HasErrors();
    }

    /// <summary>Indicates whether the external source handles remain open.</summary>
    /// <returns><c>true</c> until the set or one of its usable sources is closed.</returns>
    public bool GetIsOpen()
    {
        if (!IsOpen)
        {
            return false;
        }

        int index = 0;
        while (index < SourceFiles.Length)
        {
            if (SourceFiles[index].GetSuccess() && !SourceFiles[index].GetIsOpen())
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    /// <summary>Closes all external TIFF handles owned by this set.</summary>
    public void Close()
    {
        int index = 0;
        while (index < SourceFiles.Length)
        {
            SourceFiles[index].Close();
            index = index + 1;
        }

        IsOpen = false;
    }

    /// <summary>Returns the caller-owned base image used during resolution.</summary>
    /// <returns>The base image.</returns>
    public TiffImage GetBaseImage()
    {
        return BaseImage;
    }

    /// <summary>Returns the number of caller-supplied external sources.</summary>
    /// <returns>The external source count.</returns>
    public int GetSourceCount()
    {
        return SourceFiles.Length;
    }

    /// <summary>Returns one opened external TIFF source.</summary>
    /// <param name="sourceIndex">The zero-based index in the caller-supplied path or stream array.</param>
    /// <returns>The selected TIFF file handle.</returns>
    public TiffFile GetSourceFile(int sourceIndex)
    {
        if (sourceIndex < 0 || sourceIndex >= SourceFiles.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "External overview source index is outside the available range.");
            return new TiffFile();
        }

        return SourceFiles[sourceIndex];
    }

    /// <summary>Returns the number of compatible external overview relationships.</summary>
    /// <returns>The relationship count.</returns>
    public int GetRelationshipCount()
    {
        return Relationships.Length;
    }

    /// <summary>Returns one detached external overview relationship.</summary>
    /// <param name="relationshipIndex">The zero-based relationship index.</param>
    /// <returns>The selected relationship.</returns>
    public TiffExternalOverviewRelationship GetRelationship(int relationshipIndex)
    {
        if (relationshipIndex < 0 || relationshipIndex >= Relationships.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "External overview relationship index is outside the available range.");
            return new TiffExternalOverviewRelationship();
        }

        return Relationships[relationshipIndex].Copy();
    }

    /// <summary>Returns the external image identified by one relationship.</summary>
    /// <param name="relationshipIndex">The zero-based relationship index.</param>
    /// <returns>The selected external overview image.</returns>
    public TiffImage GetOverviewImage(int relationshipIndex)
    {
        if (relationshipIndex < 0 || relationshipIndex >= Relationships.Length)
        {
            Diagnostics.AddError(TiffErrorCode.InvalidOffset, "External overview relationship index is outside the available range.");
            return new TiffImage();
        }

        TiffExternalOverviewRelationship relationship = Relationships[relationshipIndex];
        return SourceFiles[relationship.SourceIndex].GetImage(relationship.SourceImageIndex);
    }

    /// <summary>Returns collected resolution and source-open diagnostics.</summary>
    /// <returns>The diagnostic collection.</returns>
    public TiffDiagnostics GetDiagnostics()
    {
        return Diagnostics;
    }
}
