namespace svg;

/// <summary>Defines one ordered document-processing job.</summary>
public class SvgProcessJob
{
    /// <summary>The stable nonempty job identifier.</summary>
    public string Id;
    /// <summary>The job implementation kind.</summary>
    public SvgProcessJobKind Kind;
    /// <summary>Whether the job participates in execution.</summary>
    public bool Enabled;
    /// <summary>Identifiers of jobs that must execute before this job.</summary>
    public System.Collections.Generic.List<string> Prerequisites;
    /// <summary>The callback invoked once per processing pass.</summary>
    public System.Action<SvgProcessContext> Callback;
    /// <summary>The default stable reason assigned to recorded mutations.</summary>
    public string Reason;
    /// <summary>Whether this job intentionally permits semantic loss.</summary>
    public bool SemanticLoss;
    /// <summary>The impact classification assigned to mutations from this job.</summary>
    public SvgProcessChangeSeverity Severity;
    /// <summary>Whether the job retains accessibility content and relationships.</summary>
    public bool PreservesAccessibility;
    /// <summary>Whether the job retains authored viewBox semantics.</summary>
    public bool PreservesViewBox;

    /// <summary>Creates an enabled caller-supplied processing job.</summary>
    /// <param name="id">The stable nonempty job identifier.</param>
    /// <param name="callback">The callback invoked for each processing pass.</param>
    public SvgProcessJob(string id, System.Action<SvgProcessContext> callback)
    {
        if (id == null || id.Length == 0) throw new System.ArgumentOutOfRangeException("id");
        if (callback == null) throw new System.ArgumentNullException("callback");
        Id = id;
        Kind = SvgProcessJobKind.Custom;
        Enabled = true;
        Prerequisites = new System.Collections.Generic.List<string>();
        Callback = callback;
        Reason = "";
        SemanticLoss = false;
        Severity = SvgProcessChangeSeverity.None;
        PreservesAccessibility = false;
        PreservesViewBox = false;
    }
}

/// <summary>Configures ordered bounded document processing.</summary>
public class SvgProcessOptions
{
    /// <summary>The named preset that supplied this configuration.</summary>
    public SvgProcessPresetKind Preset;
    /// <summary>The ordered declarative operations requested by the preset.</summary>
    public System.Collections.Generic.List<SvgProcessOperation> Operations;
    /// <summary>Whether every enabled job must retain accessibility semantics.</summary>
    public bool PreserveAccessibility;
    /// <summary>Whether every enabled job must retain authored viewBox semantics.</summary>
    public bool PreserveViewBox;
    /// <summary>Whether jobs marked as semantically lossy may execute.</summary>
    public bool AllowSemanticLoss;
    /// <summary>The caller jobs in deterministic tie-break order.</summary>
    public System.Collections.Generic.List<SvgProcessJob> Jobs;
    /// <summary>Whether all successful mutations are rolled back after reporting.</summary>
    public bool DryRun;
    /// <summary>The positive maximum complete processing passes.</summary>
    public int MaximumPasses;
    /// <summary>The positive maximum recorded mutations across all passes.</summary>
    public int MaximumChanges;
    /// <summary>Whether the document index is eagerly refreshed after committed changes.</summary>
    public bool RefreshIndexes;
    /// <summary>An optional callback invoked after each completed job run.</summary>
    public System.Action<SvgProcessJobRun>? JobCompletedCallback;
    /// <summary>An optional callback invoked once after committed changes invalidate caches.</summary>
    public System.Action<SvgDocument>? CacheInvalidationCallback;
    /// <summary>The cancellation token observed between passes and job callbacks.</summary>
    public System.Threading.CancellationToken CancellationToken;

    /// <summary>Creates an empty bounded processing pipeline.</summary>
    public SvgProcessOptions()
    {
        Preset = SvgProcessPresetKind.Custom;
        Operations = new System.Collections.Generic.List<SvgProcessOperation>();
        PreserveAccessibility = false;
        PreserveViewBox = false;
        AllowSemanticLoss = true;
        Jobs = new System.Collections.Generic.List<SvgProcessJob>();
        DryRun = false;
        MaximumPasses = 16;
        MaximumChanges = 100000;
        RefreshIndexes = true;
        JobCompletedCallback = null;
        CacheInvalidationCallback = null;
        CancellationToken = System.Threading.CancellationToken.None;
    }
}

/// <summary>Provides one job callback with the active document and mutation APIs.</summary>
public class SvgProcessContext
{
    /// <summary>The document being processed.</summary>
    public SvgDocument Document;
    /// <summary>The active job.</summary>
    public SvgProcessJob Job;
    /// <summary>The one-based processing pass.</summary>
    public int Pass;
    /// <summary>The editable-node mutation API whose changes are recorded automatically.</summary>
    public SvgNodeApi Mutations;
    /// <summary>The document construction API for detached replacement content.</summary>
    public SvgDocumentApi Documents;
    /// <summary>The reason assigned to mutations made by this callback invocation.</summary>
    public string Reason;
    /// <summary>Whether mutations made by this callback invocation intentionally lose semantics.</summary>
    public bool SemanticLoss;
    /// <summary>The impact classification assigned to callback mutations.</summary>
    public SvgProcessChangeSeverity Severity;
    /// <summary>The snapshotted cancellation token for cooperative callback work.</summary>
    public System.Threading.CancellationToken CancellationToken;
    internal bool Failed;
    internal string FailureMessage;

    internal SvgProcessContext(SvgDocument document, SvgProcessJob job, int pass, System.Threading.CancellationToken cancellationToken)
    {
        Document = document;
        Job = job;
        Pass = pass;
        Mutations = new SvgNodeApi();
        Documents = new SvgDocumentApi();
        Reason = job.Reason;
        SemanticLoss = job.SemanticLoss;
        Severity = job.Severity;
        CancellationToken = cancellationToken;
        Failed = false;
        FailureMessage = "";
    }

    /// <summary>Reports a recoverable job failure and requests atomic rollback.</summary>
    /// <param name="message">The nonempty stable failure explanation.</param>
    internal void Fail(string message)
    {
        Failed = true;
        FailureMessage = message == null || message.Length == 0 ? "The processing job failed." : message;
    }

    /// <summary>Tests whether this callback has requested failure.</summary>
    /// <returns><c>true</c> after <see cref="Fail"/> is called.</returns>
    internal bool IsFailed()
    {
        return Failed;
    }
}

/// <summary>Describes one completed job callback invocation.</summary>
public class SvgProcessJobRun
{
    /// <summary>The executed job.</summary>
    public SvgProcessJob Job;
    /// <summary>The one-based convergence pass.</summary>
    public int Pass;
    /// <summary>The zero-based execution position within the pass.</summary>
    public int Order;
    /// <summary>The document version before the callback.</summary>
    public long StartVersion;
    /// <summary>The document version after the callback.</summary>
    public long EndVersion;
    /// <summary>The number of mutations recorded by the callback.</summary>
    public int ChangeCount;
    /// <summary>Whether the callback recorded at least one mutation.</summary>
    public bool Changed;
    /// <summary>Whether the callback explicitly reported failure.</summary>
    public bool Failed;

    internal SvgProcessJobRun(SvgProcessJob job, int pass, int order, long startVersion, long endVersion, int changeCount, bool failed)
    {
        Job = job;
        Pass = pass;
        Order = order;
        StartVersion = startVersion;
        EndVersion = endVersion;
        ChangeCount = changeCount;
        Changed = changeCount != 0;
        Failed = failed;
    }
}

/// <summary>Contains one deterministic processing change-log entry.</summary>
public class SvgProcessChange
{
    /// <summary>The zero-based global change sequence.</summary>
    public int Sequence;
    /// <summary>The one-based convergence pass.</summary>
    public int Pass;
    /// <summary>The stable job identifier.</summary>
    public string JobId;
    /// <summary>The underlying editable-document mutation kind.</summary>
    public SvgChangeKind Kind;
    /// <summary>The deterministic affected-node path.</summary>
    public string Path;
    /// <summary>The affected attribute qualified name, or an empty string.</summary>
    public string AttributeName;
    /// <summary>The value before the mutation, or an empty string.</summary>
    public string OldValue;
    /// <summary>The value after the mutation, or an empty string.</summary>
    public string NewValue;
    /// <summary>Whether the mutation intentionally loses semantics.</summary>
    public bool SemanticLoss;
    /// <summary>The explicit impact classification.</summary>
    public SvgProcessChangeSeverity Severity;
    /// <summary>The stable transformation reason, or an empty string.</summary>
    public string Reason;
    /// <summary>The affected authored source span, or <c>null</c>.</summary>
    public SvgSourceSpan? Source;
    /// <summary>The document version before the mutation.</summary>
    public long BeforeVersion;
    /// <summary>The document version after the mutation.</summary>
    public long AfterVersion;
    /// <summary>The complete recorded editable-document mutation.</summary>
    public SvgChange Change;

    internal SvgProcessChange(int sequence, int pass, string jobId, SvgChange change, string path, string attributeName, string oldValue, string newValue)
    {
        Sequence = sequence;
        Pass = pass;
        JobId = jobId;
        Kind = change.Kind;
        Path = path;
        AttributeName = attributeName;
        OldValue = oldValue;
        NewValue = newValue;
        SemanticLoss = change.SemanticLoss;
        Severity = change.Severity;
        Reason = change.Reason;
        Source = change.Source;
        BeforeVersion = change.BeforeVersion;
        AfterVersion = change.AfterVersion;
        Change = change;
    }
}

/// <summary>Contains bounded ordered document-processing output.</summary>
public class SvgProcessResult
{
    /// <summary>The processed source document.</summary>
    public SvgDocument? Document;
    /// <summary>The document version before processing.</summary>
    public long StartVersion;
    /// <summary>The document version after completion or rollback.</summary>
    public long EndVersion;
    /// <summary>The deterministic prospective or committed changes.</summary>
    public System.Collections.Generic.List<SvgProcessChange> Changes;
    /// <summary>The completed job invocations in pass and execution order.</summary>
    public System.Collections.Generic.List<SvgProcessJobRun> Runs;
    /// <summary>Operational diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The number of complete or partially completed passes.</summary>
    public int PassCount;
    /// <summary>Whether execution reached a pass with no mutations.</summary>
    public bool Converged;
    /// <summary>Whether successful output differs from the input document.</summary>
    public bool Changed;
    /// <summary>Whether processing was configured as a dry run.</summary>
    public bool DryRun;
    /// <summary>Whether attempted mutations were rolled back.</summary>
    public bool RolledBack;
    /// <summary>Whether versioned document caches were invalidated by committed changes.</summary>
    public bool CacheInvalidated;
    /// <summary>The stable operational failure.</summary>
    public SvgProcessError Error;
    /// <summary>Whether processing completed successfully.</summary>
    public bool Success;

    internal SvgProcessResult(SvgDocument? document, bool dryRun)
    {
        Document = document;
        StartVersion = document == null ? 0 : document.Version;
        EndVersion = StartVersion;
        Changes = new System.Collections.Generic.List<SvgProcessChange>();
        Runs = new System.Collections.Generic.List<SvgProcessJobRun>();
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        PassCount = 0;
        Converged = false;
        Changed = false;
        DryRun = dryRun;
        RolledBack = false;
        CacheInvalidated = false;
        Error = SvgProcessError.None;
        Success = false;
    }

    /// <summary>Checks whether the result still represents the document's current version.</summary>
    /// <returns><c>true</c> when the captured end version is current.</returns>
    internal bool IsCurrent()
    {
        return Document != null && Document.Version == EndVersion;
    }
}
