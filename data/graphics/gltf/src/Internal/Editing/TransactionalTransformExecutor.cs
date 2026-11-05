using System;
using System.Collections.Generic;

namespace gltf;

internal class TransformStepExecution
{
    internal GltfDocument Document = new GltfDocument();
    internal GltfTransformStepResult Result = new GltfTransformStepResult();
}

internal class TransactionalTransformExecutor
{
    internal DocumentTransaction Transaction = new DocumentTransaction();
    internal IGltfValidator Validator = new GltfValidator();
    internal DocumentPruner Pruner = new DocumentPruner();
    internal DocumentDeduplicator Deduplicator = new DocumentDeduplicator();
    internal DocumentCanonicalizer Canonicalizer = new DocumentCanonicalizer();
    internal DocumentVertexTransformer VertexTransformer =
        new DocumentVertexTransformer();
    internal DocumentPrimitiveTransformer PrimitiveTransformer =
        new DocumentPrimitiveTransformer();
    internal DocumentSceneTransformer SceneTransformer =
        new DocumentSceneTransformer();
    internal DocumentGeometryGenerator GeometryGenerator =
        new DocumentGeometryGenerator();
    internal DocumentAnimationCleaner AnimationCleaner =
        new DocumentAnimationCleaner();
    internal DocumentQuantizationTransformer QuantizationTransformer =
        new DocumentQuantizationTransformer();
    internal DocumentStorageTransformer StorageTransformer =
        new DocumentStorageTransformer();
    internal TransformHookDispatcher HookDispatcher =
        new TransformHookDispatcher();
    internal TransformReportMetrics Metrics = new TransformReportMetrics();
    internal TransformReportAggregator Aggregator =
        new TransformReportAggregator();

    internal GltfTransformReport ExecuteTransform(
        GltfDocument document,
        GltfTransformPlan plan,
        GltfTransformOptions options)
    {
        ValidateInputs(document, plan, options);
        if (options.ValidateBefore)
        {
            ValidateTransformDocument(
                document,
                options.ValidationOptions,
                "The source document is invalid.");
        }

        GltfTransformReport report = new GltfTransformReport();
        TransformMetricSnapshot originalMetrics = Metrics.CreateMetricSnapshot(
            document,
            options);
        Metrics.SetOriginalMetrics(report, originalMetrics);
        GltfDocument working = Transaction.CreateWorkingDocument(document);
        int stepIndex = 0;
        while (stepIndex < plan.Steps.Count)
        {
            GltfTransformStep step = plan.Steps[stepIndex];
            if (step == null)
            {
                throw new ArgumentException(
                    $"Transform step {stepIndex} must not be null.",
                    "plan");
            }

            if (!step.Enabled)
            {
                GltfTransformStepResult skipped = new GltfTransformStepResult();
                skipped.Step = step;
                skipped.Status = GltfOperationStatus.Skipped;
                skipped.Reason = "The transform step is disabled.";
                Aggregator.AddStepResult(report, skipped, false);
                stepIndex++;
                continue;
            }

            TransformExtensionSnapshot extensionSnapshot =
                Aggregator.CaptureExtensionSnapshot(working);
            TransformStepExecution execution = ExecuteStepTransaction(
                working,
                step,
                options,
                report.Diagnostics);
            bool retainExecution =
                execution.Result.Status == GltfOperationStatus.Success
                || (execution.Result.Status != GltfOperationStatus.Skipped
                    && execution.Result.Status != GltfOperationStatus.Unavailable
                    && !options.RollbackOnFailure);
            if (retainExecution)
            {
                Aggregator.RecordExtensionChanges(
                    extensionSnapshot,
                    execution.Document,
                    execution.Result);
            }

            Aggregator.AddStepResult(report, execution.Result, retainExecution);
            if (execution.Result.Status == GltfOperationStatus.Success)
            {
                working = execution.Document;
            }
            else if (execution.Result.Status != GltfOperationStatus.Skipped)
            {
                report.Status = execution.Result.Status;
                if (!options.RollbackOnFailure)
                {
                    report.Document = execution.Document;
                    report.Resources = execution.Document.Resources;
                    TransformMetricSnapshot partialMetrics =
                        Metrics.CreateMetricSnapshot(execution.Document, options);
                    Metrics.SetResultMetrics(report, partialMetrics);
                }
                else
                {
                    Metrics.SetRolledBackMetrics(report);
                }

                return report;
            }

            stepIndex++;
        }

        if (options.ValidateAfter)
        {
            ValidateTransformDocument(
                working,
                options.ValidationOptions,
                "The transformed document is invalid.");
        }

        if (options.CloneDocument)
        {
            report.Document = working;
        }
        else
        {
            Transaction.CommitWorkingDocument(document, working);
            report.Document = document;
        }

        report.Resources = report.Document.Resources;
        TransformMetricSnapshot resultMetrics = Metrics.CreateMetricSnapshot(
            report.Document,
            options);
        Metrics.SetResultMetrics(report, resultMetrics);
        return report;
    }

    internal TransformStepExecution ExecuteStepTransaction(
        GltfDocument working,
        GltfTransformStep step,
        GltfTransformOptions options,
        List<GltfValidationIssue> diagnostics)
    {
        if (step.Kind == GltfTransformKind.Prune)
        {
            GltfDocument candidate = Transaction.CreateWorkingDocument(working);
            return Pruner.PruneDocument(
                candidate,
                step,
                options.RequireSafeUnknownExtensions);
        }

        if (step.Kind == GltfTransformKind.Deduplicate)
        {
            GltfDocument candidate = Transaction.CreateWorkingDocument(working);
            return Deduplicator.DeduplicateDocument(
                candidate,
                step,
                options.RequireSafeUnknownExtensions);
        }

        if (step.Kind == GltfTransformKind.Canonicalize)
        {
            GltfDocument candidate = Transaction.CreateWorkingDocument(working);
            return Canonicalizer.CanonicalizeDocument(
                candidate,
                step,
                options.RequireSafeUnknownExtensions);
        }

        if (step.Kind == GltfTransformKind.Weld
            || step.Kind == GltfTransformKind.Unweld)
        {
            GltfDocument candidate = Transaction.CreateWorkingDocument(working);
            return VertexTransformer.TransformVertices(
                candidate,
                step,
                step.Kind == GltfTransformKind.Weld);
        }

        if (step.Kind == GltfTransformKind.JoinPrimitives
            || step.Kind == GltfTransformKind.SplitPrimitives
            || step.Kind == GltfTransformKind.CreateGpuInstances)
        {
            GltfDocument candidate = Transaction.CreateWorkingDocument(working);
            return PrimitiveTransformer.TransformPrimitiveDocument(candidate, step);
        }

        if (step.Kind == GltfTransformKind.ApplyNodeTransform
            || step.Kind == GltfTransformKind.BakeNodeTransforms
            || step.Kind == GltfTransformKind.FlattenHierarchy
            || step.Kind == GltfTransformKind.CenterScene
            || step.Kind == GltfTransformKind.GroundScene
            || step.Kind == GltfTransformKind.FlipWinding
            || step.Kind == GltfTransformKind.ConvertUnits
            || step.Kind == GltfTransformKind.ConvertAxes)
        {
            GltfDocument candidate = Transaction.CreateWorkingDocument(working);
            return SceneTransformer.TransformSceneDocument(candidate, step);
        }

        if (step.Kind == GltfTransformKind.GenerateBounds
            || step.Kind == GltfTransformKind.GenerateNormals)
        {
            GltfDocument candidate = Transaction.CreateWorkingDocument(working);
            return GeometryGenerator.GenerateGeometry(candidate, step);
        }

        if (step.Kind == GltfTransformKind.RemoveRedundantAnimationKeys)
        {
            GltfDocument candidate = Transaction.CreateWorkingDocument(working);
            return AnimationCleaner.RemoveRedundantKeys(candidate, step);
        }

        if (step.Kind == GltfTransformKind.Quantize
            || step.Kind == GltfTransformKind.Dequantize)
        {
            GltfDocument candidate = Transaction.CreateWorkingDocument(working);
            return QuantizationTransformer.TransformQuantization(
                candidate,
                step,
                step.Kind == GltfTransformKind.Quantize);
        }

        if (step.Kind == GltfTransformKind.DenseToSparse
            || step.Kind == GltfTransformKind.SparseToDense
            || step.Kind == GltfTransformKind.ConsolidateBuffers
            || step.Kind == GltfTransformKind.PartitionBuffers
            || step.Kind == GltfTransformKind.PackBuffers
            || step.Kind == GltfTransformKind.EmbedResources
            || step.Kind == GltfTransformKind.ExtractResources)
        {
            GltfDocument candidate = Transaction.CreateWorkingDocument(working);
            return StorageTransformer.TransformStorage(candidate, step);
        }

        return HookDispatcher.DispatchTransformHook(
            working,
            step,
            options,
            diagnostics);
    }

    internal void ValidateInputs(
        GltfDocument document,
        GltfTransformPlan plan,
        GltfTransformOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (plan == null)
        {
            throw new ArgumentNullException("plan");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (options.ValidationOptions == null)
        {
            throw new ArgumentException(
                "Transform validation options must not be null.",
                "options");
        }

        if (options.Hooks == null)
        {
            throw new ArgumentException(
                "Transform hooks must not be null.",
                "options");
        }
    }

    internal void ValidateTransformDocument(
        GltfDocument document,
        GltfValidationOptions options,
        string fallbackMessage)
    {
        GltfValidationReport validation = Validator.ValidateDocument(document, options);
        if (!validation.IsValid)
        {
            string message = fallbackMessage;
            if (validation.Issues.Count > 0)
            {
                message = validation.Issues[0].Message;
            }

            throw new GltfValidationException(message);
        }
    }

}
