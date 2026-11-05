namespace gltf;

/// <summary>Provides ordered transactional document transformations.</summary>
public class GltfTransformer : IGltfTransformer
{
    internal GltfImplementationShell Shell;
    internal RequiredExtensionCapabilityChecker RequiredExtensions;
    internal TransactionalTransformExecutor Executor;
    internal ExtensionUsageCollector ExtensionUsage;

    /// <summary>Initializes a document transformer.</summary>
    public GltfTransformer()
    {
        Shell = new GltfImplementationShell();
        RequiredExtensions = new RequiredExtensionCapabilityChecker();
        Executor = new TransactionalTransformExecutor();
        ExtensionUsage = new ExtensionUsageCollector();
    }

    /// <summary>Transforms a document according to an ordered plan.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="plan">The transform plan.</param>
    /// <param name="options">The transform options.</param>
    /// <returns>The transform report.</returns>
    public GltfTransformReport TransformDocument(GltfDocument document, GltfTransformPlan plan, GltfTransformOptions options)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        if (plan == null)
        {
            throw new System.ArgumentNullException("plan");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        ValidateTransformerInputs(plan, options);

        if (options.RequireSafeUnknownExtensions)
        {
            System.Collections.Generic.List<GltfValidationIssue> issues =
                RequiredExtensions.CheckTransformCapabilities(document, options);
            RemoveHandledQuantizationIssues(plan, issues);
            if (issues.Count > 0)
            {
                throw new GltfUnsupportedFeatureException(issues[0].Message);
            }

            EnsureOptionalExtensionsAreSafe(document, plan, options);
        }

        return Executor.ExecuteTransform(document, plan, options);
    }

    internal void ValidateTransformerInputs(
        GltfTransformPlan plan,
        GltfTransformOptions options)
    {
        if (plan.Steps == null)
        {
            throw new System.ArgumentException(
                "Transform plan steps must not be null.",
                "plan");
        }

        if (options.ValidationOptions == null
            || options.ValidationOptions.ExtensionHandlers == null
            || options.Hooks == null
            || options.CompressionCodecs == null
            || options.ImageCodecs == null)
        {
            throw new System.ArgumentException(
                "Transform option capability collections must not be null.",
                "options");
        }
    }

    internal void EnsureOptionalExtensionsAreSafe(
        GltfDocument document,
        GltfTransformPlan plan,
        GltfTransformOptions options)
    {
        bool hasHookStep = HasEnabledHookTransformStep(plan);
        bool hasReferenceRepackingStep =
            HasEnabledReferenceRepackingStep(plan);
        if (!hasHookStep && !hasReferenceRepackingStep)
        {
            return;
        }

        System.Collections.Generic.List<string> names =
            ExtensionUsage.CollectEncounteredDocumentExtensions(document);
        int nameIndex = 0;
        while (nameIndex < names.Count)
        {
            string name = names[nameIndex];
            GltfExtensionDescriptor? builtIn =
                RequiredExtensions.Registry.FindDescriptor(name);
            if (builtIn == null)
            {
                if (hasReferenceRepackingStep)
                {
                    throw new GltfUnsupportedFeatureException(
                        $"Extension '{name}' cannot safely remap references during resource or buffer repacking.");
                }

                GltfExtensionDescriptor? registered =
                    RequiredExtensions.FindDescriptor(
                        name,
                        options.ValidationOptions.ExtensionHandlers);
                if (registered == null || !registered.CanRemapReferences)
                {
                    throw new GltfUnsupportedFeatureException(
                        $"Extension '{name}' has no registered mutation-safe handler.");
                }
            }

            nameIndex++;
        }
    }

    internal bool HasEnabledHookTransformStep(GltfTransformPlan plan)
    {
        int stepIndex = 0;
        while (stepIndex < plan.Steps.Count)
        {
            GltfTransformStep step = plan.Steps[stepIndex];
            if (step != null
                && step.Enabled
                && IsHookDrivenTransformKind(step.Kind))
            {
                return true;
            }

            stepIndex++;
        }

        return false;
    }

    internal bool HasEnabledReferenceRepackingStep(GltfTransformPlan plan)
    {
        int stepIndex = 0;
        while (stepIndex < plan.Steps.Count)
        {
            GltfTransformStep step = plan.Steps[stepIndex];
            if (step != null
                && step.Enabled
                && (step.Kind == GltfTransformKind.ConsolidateBuffers
                    || step.Kind == GltfTransformKind.PartitionBuffers
                    || step.Kind == GltfTransformKind.PackBuffers
                    || step.Kind == GltfTransformKind.EmbedResources
                    || step.Kind == GltfTransformKind.ExtractResources))
            {
                return true;
            }

            stepIndex++;
        }

        return false;
    }

    internal bool IsHookDrivenTransformKind(GltfTransformKind kind)
    {
        return kind == GltfTransformKind.GenerateTangents
            || kind == GltfTransformKind.GenerateTextureCoordinates
            || kind == GltfTransformKind.ReorderVertices
            || kind == GltfTransformKind.Simplify
            || kind == GltfTransformKind.GenerateLod
            || kind == GltfTransformKind.EncodeDraco
            || kind == GltfTransformKind.DecodeDraco
            || kind == GltfTransformKind.EncodeMeshopt
            || kind == GltfTransformKind.DecodeMeshopt
            || kind == GltfTransformKind.ConvertImages
            || kind == GltfTransformKind.CreatePalette
            || kind == GltfTransformKind.Custom;
    }

    internal void RemoveHandledQuantizationIssues(
        GltfTransformPlan plan,
        System.Collections.Generic.List<GltfValidationIssue> issues)
    {
        bool handlesQuantization = false;
        int stepIndex = 0;
        while (stepIndex < plan.Steps.Count)
        {
            GltfTransformStep step = plan.Steps[stepIndex];
            if (step != null
                && step.Enabled
                && (step.Kind == GltfTransformKind.Quantize
                    || step.Kind == GltfTransformKind.Dequantize))
            {
                handlesQuantization = true;
            }

            stepIndex++;
        }

        if (!handlesQuantization)
        {
            return;
        }

        int issueIndex = issues.Count - 1;
        while (issueIndex >= 0)
        {
            GltfValidationIssue issue = issues[issueIndex];
            if (issue.Code == GltfIssueCodes.RequiredExtensionRemapUnavailable
                && issue.Message.Contains(GltfExtensionNames.KhrMeshQuantization))
            {
                issues.RemoveAt(issueIndex);
            }

            issueIndex--;
        }
    }
}
