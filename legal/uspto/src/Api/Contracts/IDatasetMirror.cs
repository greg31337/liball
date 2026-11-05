namespace uspto;

/// <summary>
/// Provides non-destructive planning, synchronization, verification, and repair of local dataset mirrors.
/// </summary>
public interface IDatasetMirror
{
    /// <summary>Plans mirror actions without transferring or deleting data.</summary>
    /// <param name="policy">The mirror selection and safety policy.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The planned file action classifications.</returns>
    MirrorAction[] PlanDatasetMirror(MirrorPolicy policy, RequestContext context);

    /// <summary>Synchronizes files selected by a mirror policy.</summary>
    /// <param name="policy">The mirror selection and safety policy.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The transfer results.</returns>
    DownloadResult[] SynchronizeDatasetMirror(MirrorPolicy policy, RequestContext context);

    /// <summary>Refreshes selected mirror files through parsing and retains every per-file outcome.</summary>
    /// <param name="policy">The mirror selection and safety policy.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The complete per-family and per-file refresh aggregation.</returns>
    MirrorWorkflowResult RefreshDatasetMirror(MirrorPolicy policy, RequestContext context);

    /// <summary>Gets the persisted credential-free dataset manifest.</summary>
    /// <param name="mirrorDirectory">The local mirror directory.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The typed, credential-free manifest.</returns>
    DatasetManifest GetDatasetManifest(string mirrorDirectory, RequestContext context);

    /// <summary>Verifies local files and persisted mirror state.</summary>
    /// <param name="policy">The mirror selection and safety policy.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The verification results.</returns>
    DownloadResult[] VerifyDatasetMirror(MirrorPolicy policy, RequestContext context);

    /// <summary>Repairs missing or corrupt artifacts without deleting superseded data.</summary>
    /// <param name="policy">The mirror selection and safety policy.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The repair transfer results.</returns>
    DownloadResult[] RepairDatasetMirror(MirrorPolicy policy, RequestContext context);
}
