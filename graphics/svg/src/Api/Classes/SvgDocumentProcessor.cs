namespace svg;

/// <summary>Executes deterministic transaction-backed document-processing jobs.</summary>
public class SvgDocumentProcessor : ISvgDocumentProcessor
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgDocumentProcessor()
    {
    }

    /// <summary>Processes a document with an empty default job list.</summary>
    /// <param name="document">The editable document.</param>
    /// <returns>The completed processing result.</returns>
    public SvgProcessResult ProcessWithDocument(SvgDocument document)
    {
        return new SvgDocumentProcessor().ProcessWithDocumentAndOptions(document, new SvgProcessOptions());
    }

    /// <summary>Processes a document using ordered jobs and bounded convergence.</summary>
    /// <param name="document">The editable document.</param>
    /// <param name="options">The jobs, bounds, callbacks, and dry-run behavior.</param>
    /// <returns>The completed or atomically rolled-back processing result.</returns>
    public SvgProcessResult ProcessWithDocumentAndOptions(SvgDocument document, SvgProcessOptions options)
    {
        SvgProcessResult result = new SvgProcessResult(document, options != null && options.DryRun);
        if (!new SvgDocumentProcessor().Valid(document, options)) return new SvgDocumentProcessor().Fail(result, SvgProcessError.InvalidArgument, "SVG processing arguments are invalid.", false, null);
        SvgProcessOptions execution = options ?? throw new System.InvalidOperationException("A required SVG value is unavailable.");
        int maximumPasses = execution.MaximumPasses;
        int maximumChanges = execution.MaximumChanges;
        bool allowSemanticLoss = execution.AllowSemanticLoss;
        bool dryRun = execution.DryRun;
        bool refreshIndexes = execution.RefreshIndexes;
        System.Action<SvgProcessJobRun>? jobCompletedCallback = execution.JobCompletedCallback;
        System.Action<SvgDocument>? cacheInvalidationCallback = execution.CacheInvalidationCallback;
        System.Threading.CancellationToken cancellationToken = execution.CancellationToken;
        if (cancellationToken.IsCancellationRequested) return new SvgDocumentProcessor().Fail(result, SvgProcessError.Cancelled, "SVG processing was cancelled.", false, document.SourceSpan);
        SvgProcessPlan plan = new SvgDocumentProcessor().BuildPlan(execution.Jobs);
        if (plan.Error != SvgProcessError.None) return new SvgDocumentProcessor().Fail(result, plan.Error, plan.Message, false, null);
        SvgProcessError policyError = new SvgDocumentProcessor().CheckPolicy(plan.Jobs, execution);
        if (policyError != SvgProcessError.None) return new SvgDocumentProcessor().Fail(result, policyError, new SvgDocumentProcessor().PolicyMessage(policyError), false, document.SourceSpan);
        if (plan.Jobs.Count == 0)
        {
            result.Converged = true;
            result.Success = true;
            return result;
        }
        SvgChangeSet transaction = new SvgDocumentApi().BeginChangeSet(document);
        int pass = 1;
        while (pass <= maximumPasses && result.Error == SvgProcessError.None)
        {
            int passStart = transaction.Changes.Count;
            int jobIndex = 0;
            while (jobIndex < plan.Jobs.Count && result.Error == SvgProcessError.None)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    new SvgDocumentProcessor().SetFailure(result, SvgProcessError.Cancelled, "SVG processing was cancelled.", document.SourceSpan);
                    break;
                }
                new SvgDocumentProcessor().ExecuteJob(plan.Jobs[jobIndex], pass, jobIndex, document, maximumChanges, allowSemanticLoss, jobCompletedCallback, cancellationToken, transaction, result);
                jobIndex = jobIndex + 1;
            }
            result.PassCount = pass;
            if (result.Error != SvgProcessError.None) break;
            if (transaction.Changes.Count == passStart)
            {
                result.Converged = true;
                break;
            }
            pass = pass + 1;
        }
        if (result.Error == SvgProcessError.None && !result.Converged) new SvgDocumentProcessor().SetFailure(result, SvgProcessError.ConvergenceLimitExceeded, "The processing pipeline did not converge within the configured pass bound.", document.SourceSpan);
        if (result.Error != SvgProcessError.None)
        {
            transaction.Rollback();
            result.RolledBack = true;
            result.EndVersion = document.Version;
            result.Changed = false;
            return result;
        }
        bool attempted = transaction.Changes.Count != 0;
        if (dryRun)
        {
            transaction.Rollback();
            result.RolledBack = attempted;
            result.Changed = false;
        }
        else
        {
            transaction.Commit();
            result.Changed = attempted;
            if (attempted)
            {
                result.CacheInvalidated = true;
                if (refreshIndexes) document.Index.Refresh();
                if (cacheInvalidationCallback != null) cacheInvalidationCallback(document);
            }
        }
        result.EndVersion = document.Version;
        result.Success = true;
        return result;
    }
void ExecuteJob(SvgProcessJob job, int pass, int order, SvgDocument document, int maximumChanges, bool allowSemanticLoss, System.Action<SvgProcessJobRun>? jobCompletedCallback, System.Threading.CancellationToken cancellationToken, SvgChangeSet transaction, SvgProcessResult result)
    {
        int changeStart = transaction.Changes.Count;
        long versionStart = document.Version;
        SvgProcessContext context = new SvgProcessContext(document, job, pass, cancellationToken);
        job.Callback(context);
        int changeCount = transaction.Changes.Count - changeStart;
        long versionEnd = document.Version;
        SvgProcessJobRun run = new SvgProcessJobRun(job, pass, order, versionStart, versionEnd, changeCount, context.Failed);
        result.Runs.Add(run);
        if (versionEnd != versionStart && changeCount == 0)
        {
            new SvgDocumentProcessor().SetFailure(result, SvgProcessError.UntrackedMutation, "The processing job changed the document without using a recorded mutation API.", document.SourceSpan);
            return;
        }
        int completedChangeCount = transaction.Changes.Count;
        long completedVersion = document.Version;
        if (jobCompletedCallback != null) jobCompletedCallback(run);
        if (transaction.Changes.Count != completedChangeCount || document.Version != completedVersion)
        {
            new SvgDocumentProcessor().SetFailure(result, SvgProcessError.UntrackedMutation, "The processing completion callback changed the active document.", document.SourceSpan);
            return;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            new SvgDocumentProcessor().SetFailure(result, SvgProcessError.Cancelled, "SVG processing was cancelled.", document.SourceSpan);
            return;
        }
        int changeIndex = changeStart;
        while (changeIndex < transaction.Changes.Count)
        {
            SvgChange change = transaction.Changes[changeIndex];
            if (change.Reason.Length == 0) change.Reason = context.Reason;
            if (context.SemanticLoss) change.SemanticLoss = true;
            if (change.Severity == SvgProcessChangeSeverity.None) change.Severity = context.Severity;
            if (change.Path.Length == 0) change.Path = new SvgDocumentProcessor().ChangePath(change);
            result.Changes.Add(new SvgDocumentProcessor().CreateProcessChange(result.Changes.Count, pass, job.Id, change));
            changeIndex = changeIndex + 1;
        }
        if (context.SemanticLoss && !allowSemanticLoss)
        {
            new SvgDocumentProcessor().SetFailure(result, SvgProcessError.SemanticLossDenied, new SvgDocumentProcessor().PolicyMessage(SvgProcessError.SemanticLossDenied), document.SourceSpan);
            return;
        }
        if (context.Failed)
        {
            new SvgDocumentProcessor().SetFailure(result, SvgProcessError.JobFailed, context.FailureMessage, document.SourceSpan);
            return;
        }
        if (transaction.Changes.Count > maximumChanges) new SvgDocumentProcessor().SetFailure(result, SvgProcessError.ChangeLimitExceeded, "The processing mutation limit was exceeded.", document.SourceSpan);
    }
SvgProcessChange CreateProcessChange(int sequence, int pass, string jobId, SvgChange change)
    {
        SvgAttribute? attribute = change.Attribute == null ? change.RemovedAttribute : change.Attribute;
        string attributeName = attribute == null ? "" : attribute.QualifiedName;
        string oldValue = change.OldValue;
        string newValue = change.NewValue;
        if (change.Kind == SvgChangeKind.AttributeInsert && change.Attribute != null) newValue = change.Attribute.Value;
        else if (change.Kind == SvgChangeKind.AttributeRemove && change.Attribute != null) oldValue = change.Attribute.Value;
        else if (change.Kind == SvgChangeKind.AttributeReplace)
        {
            if (change.RemovedAttribute != null) oldValue = change.RemovedAttribute.Value;
            if (change.Attribute != null) newValue = change.Attribute.Value;
        }
        return new SvgProcessChange(sequence, pass, jobId, change, change.Path, attributeName, oldValue, newValue);
    }
string ChangePath(SvgChange change)
    {
        SvgAttribute? attribute = change.Attribute == null ? change.RemovedAttribute : change.Attribute;
        if (attribute != null)
        {
            SvgNode? owner = attribute.OwnerElement;
            if (owner == null) owner = change.NewParent == null ? change.OldParent : change.NewParent;
            return new SvgDocumentProcessor().NodePath(owner) + new SvgDocumentProcessor().AttributeSuffix(change);
        }
        SvgNode? node = change.Node;
        if (node != null && node.Parent != null) return new SvgDocumentProcessor().NodePath(node) + new SvgDocumentProcessor().AttributeSuffix(change);
        SvgNode? parent = change.NewParent == null ? change.OldParent : change.NewParent;
        int index = change.NewParent == null ? change.OldIndex : change.NewIndex;
        string parentPath = new SvgDocumentProcessor().NodePath(parent);
        string childPath = parentPath + "/node()[" + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
        return childPath + new SvgDocumentProcessor().AttributeSuffix(change);
    }
string AttributeSuffix(SvgChange change)
    {
        SvgAttribute? attribute = change.Attribute == null ? change.RemovedAttribute : change.Attribute;
        return attribute == null ? "" : "/@" + attribute.QualifiedName;
    }
string NodePath(SvgNode? node)
    {
        if (node == null) return "";
        System.Collections.Generic.List<SvgNode> ancestors = new System.Collections.Generic.List<SvgNode>();
        SvgNode? current = node;
        while (current != null && current.Kind != SvgNodeKind.Document)
        {
            ancestors.Add(current);
            current = current.Parent;
        }
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = ancestors.Count - 1;
        while (index >= 0)
        {
            SvgNode item = ancestors[index];
            result.Append('/');
            result.Append(item.Kind == SvgNodeKind.Element ? item.QualifiedName : new SvgDocumentProcessor().NodeKindName(item.Kind));
            result.Append('[');
            result.Append(new SvgDocumentProcessor().SiblingIndex(item).ToString(System.Globalization.CultureInfo.InvariantCulture));
            result.Append(']');
            index = index - 1;
        }
        return result.ToString();
    }
int SiblingIndex(SvgNode node)
    {
        if (node.Parent == null) return 1;
        int position = 0;
        int index = 0;
        while (index < node.Parent.Children.Count)
        {
            SvgNode sibling = node.Parent.Children[index];
            bool same = sibling.Kind == node.Kind;
            if (same && node.Kind == SvgNodeKind.Element) same = sibling.NamespaceUri == node.NamespaceUri && sibling.LocalName == node.LocalName;
            if (same) position = position + 1;
            if (object.ReferenceEquals(sibling, node)) return position;
            index = index + 1;
        }
        return 1;
    }
string NodeKindName(SvgNodeKind kind)
    {
        if (kind == SvgNodeKind.Text) return "text()";
        if (kind == SvgNodeKind.CData) return "cdata()";
        if (kind == SvgNodeKind.Comment) return "comment()";
        if (kind == SvgNodeKind.ProcessingInstruction) return "processing-instruction()";
        if (kind == SvgNodeKind.Doctype) return "doctype()";
        return "node()";
    }
SvgProcessPlan BuildPlan(System.Collections.Generic.List<SvgProcessJob> jobs)
    {
        SvgProcessPlan plan = new SvgProcessPlan();
        int index = 0;
        while (index < jobs.Count)
        {
            SvgProcessJob job = jobs[index];
            if (job == null || job.Id == null || job.Id.Length == 0 || job.Prerequisites == null || job.Callback == null)
            {
                return new SvgDocumentProcessor().PlanFailure(SvgProcessError.InvalidArgument, "Every processing job requires an identifier, prerequisite list, and callback.");
            }
            if (job.Kind < SvgProcessJobKind.Custom || job.Kind > SvgProcessJobKind.IsolateNamespace) return new SvgDocumentProcessor().PlanFailure(SvgProcessError.InvalidArgument, "A processing job kind is invalid.");
            if (job.Severity < SvgProcessChangeSeverity.None || job.Severity > SvgProcessChangeSeverity.Destructive) return new SvgDocumentProcessor().PlanFailure(SvgProcessError.InvalidArgument, "A processing job severity is invalid.");
            int otherIndex = 0;
            while (otherIndex < index)
            {
                if (jobs[otherIndex] != null && jobs[otherIndex].Id == job.Id) return new SvgDocumentProcessor().PlanFailure(SvgProcessError.InvalidJobGraph, "Processing job identifiers must be unique.");
                otherIndex = otherIndex + 1;
            }
            index = index + 1;
        }
        System.Collections.Generic.List<SvgProcessJob> remaining = new System.Collections.Generic.List<SvgProcessJob>();
        index = 0;
        while (index < jobs.Count)
        {
            if (jobs[index].Enabled) remaining.Add(jobs[index]);
            index = index + 1;
        }
        while (remaining.Count != 0)
        {
            bool progressed = false;
            index = 0;
            while (index < remaining.Count && !progressed)
            {
                SvgProcessJob job = remaining[index];
                if (new SvgDocumentProcessor().PrerequisitesReady(job, plan.Jobs))
                {
                    plan.Jobs.Add(job);
                    remaining.RemoveAt(index);
                    progressed = true;
                }
                else index = index + 1;
            }
            if (!progressed) return new SvgDocumentProcessor().PlanFailure(SvgProcessError.InvalidJobGraph, "Processing job prerequisites are missing, disabled, or cyclic.");
        }
        return plan;
    }
bool PrerequisitesReady(SvgProcessJob job, System.Collections.Generic.List<SvgProcessJob> completed)
    {
        int index = 0;
        while (index < job.Prerequisites.Count)
        {
            string prerequisite = job.Prerequisites[index];
            if (prerequisite == null || prerequisite.Length == 0 || prerequisite == job.Id) return false;
            bool found = false;
            int completedIndex = 0;
            while (completedIndex < completed.Count)
            {
                if (completed[completedIndex].Id == prerequisite) found = true;
                completedIndex = completedIndex + 1;
            }
            if (!found) return false;
            index = index + 1;
        }
        return true;
    }
SvgProcessPlan PlanFailure(SvgProcessError error, string message)
    {
        SvgProcessPlan result = new SvgProcessPlan();
        result.Error = error;
        result.Message = message;
        return result;
    }
SvgProcessError CheckPolicy(System.Collections.Generic.List<SvgProcessJob> jobs, SvgProcessOptions options)
    {
        int index = 0;
        while (index < jobs.Count)
        {
            SvgProcessJob job = jobs[index];
            if (job.SemanticLoss && !options.AllowSemanticLoss) return SvgProcessError.SemanticLossDenied;
            if (options.PreserveAccessibility && !job.PreservesAccessibility) return SvgProcessError.PreservationRequired;
            if (options.PreserveViewBox && !job.PreservesViewBox) return SvgProcessError.PreservationRequired;
            index = index + 1;
        }
        return SvgProcessError.None;
    }
string PolicyMessage(SvgProcessError error)
    {
        if (error == SvgProcessError.SemanticLossDenied) return "The selected process policy does not permit semantically lossy jobs.";
        return "The selected process policy requires every job to preserve accessibility and viewBox semantics.";
    }
bool Valid(SvgDocument? document, SvgProcessOptions? options)
    {
        if (document == null || document.DocumentNode == null || options == null || options.Jobs == null || options.Operations == null) return false;
        if (options.MaximumPasses < 1 || options.MaximumChanges < 1) return false;
        if (options.Preset < SvgProcessPresetKind.Custom || options.Preset > SvgProcessPresetKind.Sanitize) return false;
        int operationIndex = 0;
        while (operationIndex < options.Operations.Count)
        {
            SvgProcessOperation operation = options.Operations[operationIndex];
            if (operation < SvgProcessOperation.Sanitize || operation > SvgProcessOperation.BuildSprite) return false;
            int previousIndex = 0;
            while (previousIndex < operationIndex)
            {
                if (options.Operations[previousIndex] == operation) return false;
                previousIndex = previousIndex + 1;
            }
            operationIndex = operationIndex + 1;
        }
        if (document.ActiveChangeSet != null && !document.ActiveChangeSet.Completed) return false;
        return true;
    }
    SvgProcessResult Fail(SvgProcessResult result, SvgProcessError error, string message, bool rolledBack, SvgSourceSpan? source)
    {
        result.Error = error;
        result.RolledBack = rolledBack;
        SvgDiagnosticCode code = error == SvgProcessError.Cancelled ? SvgDiagnosticCode.Cancelled : SvgDiagnosticCode.InvalidArgument;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
        return result;
    }
void SetFailure(SvgProcessResult result, SvgProcessError error, string message, SvgSourceSpan? source)
    {
        result.Error = error;
        SvgDiagnosticCode code = error == SvgProcessError.ChangeLimitExceeded || error == SvgProcessError.ConvergenceLimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : error == SvgProcessError.Cancelled ? SvgDiagnosticCode.Cancelled : SvgDiagnosticCode.InvalidArgument;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
}
