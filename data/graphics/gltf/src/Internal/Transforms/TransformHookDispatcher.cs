using System;
using System.Collections.Generic;

namespace gltf;

internal class TransformHookDispatcher
{
    internal DocumentTransaction Transaction = new DocumentTransaction();
    internal DocumentCompactor Mapping = new DocumentCompactor();

    internal TransformStepExecution DispatchTransformHook(
        GltfDocument working,
        GltfTransformStep step,
        GltfTransformOptions options,
        List<GltfValidationIssue> diagnostics)
    {
        ValidateDispatchInputs(working, step, options, diagnostics);
        List<GltfValidationIssue> unavailableDiagnostics =
            new List<GltfValidationIssue>();
        int hookIndex = 0;
        while (hookIndex < options.Hooks.Count)
        {
            IGltfTransformHook hook = GetTransformHook(options.Hooks, hookIndex);
            GltfDocument candidate = Transaction.CreateWorkingDocument(working);
            GltfTransformContext context = CreateHookContext(
                candidate,
                step,
                options,
                diagnostics);
            GltfTransformStepResult? hookResult = hook.ApplyTransformHook(context);
            ValidateHookResult(hookResult, hookIndex);
            if (hookResult == null)
            {
                throw new GltfUnsupportedFeatureException(
                    $"Transform hook {hookIndex} returned a null result.");
            }

            if (hookResult.Step == null)
            {
                hookResult.Step = step;
            }

            if (hookResult.Status != GltfOperationStatus.Unavailable)
            {
                if (IsEmptyRemap(hookResult.Remap))
                {
                    hookResult.Remap = context.Remap;
                }

                TransformStepExecution handled = new TransformStepExecution();
                handled.Document = RequireContextDocument(context, hookIndex);
                handled.Document.Resources = context.Resources;
                handled.Result = hookResult;
                return handled;
            }

            AppendDiagnostics(
                hookResult.Diagnostics,
                unavailableDiagnostics);
            hookIndex++;
        }

        return CreateUnavailableExecution(
            working,
            step,
            unavailableDiagnostics);
    }

    internal void ValidateDispatchInputs(
        GltfDocument working,
        GltfTransformStep step,
        GltfTransformOptions options,
        List<GltfValidationIssue> diagnostics)
    {
        if (working == null)
        {
            throw new ArgumentNullException("working");
        }

        if (step == null)
        {
            throw new ArgumentNullException("step");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        if (options.Hooks == null
            || options.CompressionCodecs == null
            || options.ImageCodecs == null)
        {
            throw new ArgumentException(
                "Transform capability collections must not be null.",
                "options");
        }

        ValidateCompressionCodecs(options.CompressionCodecs);
        ValidateImageCodecs(options.ImageCodecs);
    }

    internal void ValidateCompressionCodecs(
        List<IGltfCompressionCodec> codecs)
    {
        int codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            if (codecs[codecIndex] == null)
            {
                throw new ArgumentException(
                    $"Compression codec {codecIndex} must not be null.",
                    "options");
            }

            codecIndex++;
        }
    }

    internal void ValidateImageCodecs(List<IGltfImageCodec> codecs)
    {
        int codecIndex = 0;
        while (codecIndex < codecs.Count)
        {
            if (codecs[codecIndex] == null)
            {
                throw new ArgumentException(
                    $"Image codec {codecIndex} must not be null.",
                    "options");
            }

            codecIndex++;
        }
    }

    internal IGltfTransformHook GetTransformHook(
        List<IGltfTransformHook> hooks,
        int hookIndex)
    {
        IGltfTransformHook? hook = hooks[hookIndex];
        if (hook == null)
        {
            throw new ArgumentException(
                $"Transform hook {hookIndex} must not be null.",
                "options");
        }

        return hook;
    }

    internal GltfTransformContext CreateHookContext(
        GltfDocument document,
        GltfTransformStep step,
        GltfTransformOptions options,
        List<GltfValidationIssue> diagnostics)
    {
        GltfTransformContext result = new GltfTransformContext();
        result.Document = document;
        result.Resources = document.Resources;
        result.Step = step;
        result.Remap = Mapping.CreateIdentityRemap(document);
        CopyCompressionCodecs(options.CompressionCodecs, result.CompressionCodecs);
        CopyImageCodecs(options.ImageCodecs, result.ImageCodecs);
        CopyTransformHooks(options.Hooks, result.Hooks);
        AppendDiagnostics(diagnostics, result.Diagnostics);
        return result;
    }

    internal void ValidateHookResult(
        GltfTransformStepResult? result,
        int hookIndex)
    {
        if (result == null)
        {
            return;
        }

        if (result.Remap == null
            || result.ChangedObjects == null
            || result.ChangedResources == null
            || result.ExtensionChanges == null
            || result.Codecs == null
            || result.Diagnostics == null)
        {
            throw new GltfUnsupportedFeatureException(
                $"Transform hook {hookIndex} returned incomplete result collections.");
        }

        switch (result.Status)
        {
            case GltfOperationStatus.Success:
            case GltfOperationStatus.Skipped:
            case GltfOperationStatus.Unavailable:
            case GltfOperationStatus.Invalid:
            case GltfOperationStatus.Failed:
                return;
            default:
                throw new GltfUnsupportedFeatureException(
                    $"Transform hook {hookIndex} returned an unknown operation status.");
        }
    }

    internal GltfDocument RequireContextDocument(
        GltfTransformContext context,
        int hookIndex)
    {
        if (context.Document == null)
        {
            throw new GltfUnsupportedFeatureException(
                $"Transform hook {hookIndex} cleared the working document.");
        }

        if (context.Resources == null)
        {
            throw new GltfUnsupportedFeatureException(
                $"Transform hook {hookIndex} cleared the working resources.");
        }

        return context.Document;
    }

    internal bool IsEmptyRemap(GltfIndexRemap remap)
    {
        return remap.Accessors.Count == 0
            && remap.Animations.Count == 0
            && remap.Buffers.Count == 0
            && remap.BufferViews.Count == 0
            && remap.Cameras.Count == 0
            && remap.Images.Count == 0
            && remap.Materials.Count == 0
            && remap.Meshes.Count == 0
            && remap.Nodes.Count == 0
            && remap.Resources.Count == 0
            && remap.Samplers.Count == 0
            && remap.Scenes.Count == 0
            && remap.Skins.Count == 0
            && remap.Textures.Count == 0
            && remap.PunctualLights.Count == 0
            && remap.MaterialVariants.Count == 0
            && remap.XmpJsonLdPackets.Count == 0;
    }

    internal TransformStepExecution CreateUnavailableExecution(
        GltfDocument working,
        GltfTransformStep step,
        List<GltfValidationIssue> diagnostics)
    {
        TransformStepExecution unavailable = new TransformStepExecution();
        unavailable.Document = working;
        unavailable.Result.Step = step;
        unavailable.Result.Status = GltfOperationStatus.Unavailable;
        unavailable.Result.Remap = Mapping.CreateIdentityRemap(working);
        unavailable.Result.Reason = GetUnavailableReason(step.Kind);
        AppendDiagnostics(diagnostics, unavailable.Result.Diagnostics);
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = GltfIssueCodes.TransformHookUnavailable;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = unavailable.Result.Reason;
        issue.JsonPointer = "";
        unavailable.Result.Diagnostics.Add(issue);
        return unavailable;
    }

    internal string GetUnavailableReason(GltfTransformKind kind)
    {
        switch (kind)
        {
            case GltfTransformKind.GenerateTangents:
                return "No registered transform hook can generate tangents.";
            case GltfTransformKind.GenerateTextureCoordinates:
                return "No registered transform hook can generate texture coordinates.";
            case GltfTransformKind.ReorderVertices:
                return "No registered transform hook can reorder vertices.";
            case GltfTransformKind.Simplify:
                return "No registered transform hook can simplify meshes.";
            case GltfTransformKind.GenerateLod:
                return "No registered transform hook can generate levels of detail.";
            case GltfTransformKind.EncodeDraco:
                return "No registered transform hook can encode Draco geometry.";
            case GltfTransformKind.DecodeDraco:
                return "No registered transform hook can decode Draco geometry.";
            case GltfTransformKind.EncodeMeshopt:
                return "No registered transform hook can encode Meshopt data.";
            case GltfTransformKind.DecodeMeshopt:
                return "No registered transform hook can decode Meshopt data.";
            case GltfTransformKind.ConvertImages:
                return "No registered transform hook can convert images.";
            case GltfTransformKind.CreatePalette:
                return "No registered transform hook can create a material palette.";
            case GltfTransformKind.Custom:
                return "No registered transform hook accepted the custom step.";
            default:
                return $"No registered transform hook accepted transform kind {kind}.";
        }
    }

    internal void CopyCompressionCodecs(
        List<IGltfCompressionCodec> source,
        List<IGltfCompressionCodec> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }

    internal void CopyImageCodecs(
        List<IGltfImageCodec> source,
        List<IGltfImageCodec> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }

    internal void CopyTransformHooks(
        List<IGltfTransformHook> source,
        List<IGltfTransformHook> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }

    internal void AppendDiagnostics(
        List<GltfValidationIssue> source,
        List<GltfValidationIssue> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }
}
