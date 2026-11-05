namespace svg;

/// <summary>Creates independent named document-processing configurations.</summary>
public class SvgProcessPresets : ISvgProcessPresets
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgProcessPresets()
    {
    }

    /// <summary>Creates conservative semantics-preserving cleanup defaults.</summary>
    /// <returns>An independent safe-default configuration.</returns>
    public SvgProcessOptions SafeDefault()
    {
        SvgProcessOptions result = new SvgProcessPresets().Base(SvgProcessPresetKind.SafeDefault, false);
        new SvgProcessPresets().AddSafeOperations(result);
        return result;
    }

    /// <summary>Creates deterministic canonicalization defaults.</summary>
    /// <returns>An independent canonicalization configuration.</returns>
    public SvgProcessOptions Canonicalize()
    {
        SvgProcessOptions result = new SvgProcessPresets().Base(SvgProcessPresetKind.Canonicalize, false);
        new SvgProcessPresets().AddSafeOperations(result);
        new SvgProcessPresets().Add(result, SvgProcessOperation.CanonicalizeValues);
        return result;
    }

    /// <summary>Creates aggressive size-oriented defaults with explicit semantic loss enabled.</summary>
    /// <returns>An independent aggressive configuration.</returns>
    public SvgProcessOptions Aggressive()
    {
        SvgProcessOptions result = new SvgProcessPresets().Base(SvgProcessPresetKind.Aggressive, true);
        new SvgProcessPresets().AddSafeOperations(result);
        new SvgProcessPresets().Add(result, SvgProcessOperation.InlineStyles);
        new SvgProcessPresets().Add(result, SvgProcessOperation.ResolveCustomProperties);
        new SvgProcessPresets().Add(result, SvgProcessOperation.CanonicalizeValues);
        new SvgProcessPresets().Add(result, SvgProcessOperation.MinifyIds);
        new SvgProcessPresets().Add(result, SvgProcessOperation.ConvertShapesToPaths);
        new SvgProcessPresets().Add(result, SvgProcessOperation.BakeTransforms);
        new SvgProcessPresets().Add(result, SvgProcessOperation.SimplifyPaths);
        new SvgProcessPresets().Add(result, SvgProcessOperation.ConvertTextToPaths);
        new SvgProcessPresets().Add(result, SvgProcessOperation.FlattenEffects);
        new SvgProcessPresets().Add(result, SvgProcessOperation.EmbedImages);
        new SvgProcessPresets().Add(result, SvgProcessOperation.FlattenUses);
        result.MaximumPasses = 32;
        return result;
    }

    /// <summary>Creates safe sprite-isolation and construction defaults.</summary>
    /// <returns>An independent sprite configuration.</returns>
    public SvgProcessOptions Sprite()
    {
        SvgProcessOptions result = new SvgProcessPresets().Base(SvgProcessPresetKind.Sprite, false);
        new SvgProcessPresets().AddSafeOperations(result);
        new SvgProcessPresets().Add(result, SvgProcessOperation.CanonicalizeValues);
        new SvgProcessPresets().Add(result, SvgProcessOperation.PrefixIds);
        new SvgProcessPresets().Add(result, SvgProcessOperation.FlattenUses);
        new SvgProcessPresets().Add(result, SvgProcessOperation.BuildSprite);
        result.MaximumPasses = 32;
        return result;
    }

    /// <summary>Creates secure-static sanitization defaults.</summary>
    /// <returns>An independent sanitization configuration.</returns>
    public SvgProcessOptions Sanitize()
    {
        SvgProcessOptions result = new SvgProcessPresets().Base(SvgProcessPresetKind.Sanitize, false);
        new SvgProcessPresets().Add(result, SvgProcessOperation.Sanitize);
        new SvgProcessPresets().Add(result, SvgProcessOperation.RemoveUnusedNamespaces);
        new SvgProcessPresets().Add(result, SvgProcessOperation.RemoveEmptyContainers);
        new SvgProcessPresets().Add(result, SvgProcessOperation.RemoveUnusedDefinitions);
        return result;
    }

    /// <summary>Creates one named preset configuration.</summary>
    /// <param name="kind">The non-custom preset kind.</param>
    /// <returns>An independent named configuration.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for <c>Custom</c> or an unknown kind.</exception>
    public SvgProcessOptions Create(SvgProcessPresetKind kind)
    {
        if (kind == SvgProcessPresetKind.SafeDefault) return new SvgProcessPresets().SafeDefault();
        if (kind == SvgProcessPresetKind.Canonicalize) return new SvgProcessPresets().Canonicalize();
        if (kind == SvgProcessPresetKind.Aggressive) return new SvgProcessPresets().Aggressive();
        if (kind == SvgProcessPresetKind.Sprite) return new SvgProcessPresets().Sprite();
        if (kind == SvgProcessPresetKind.Sanitize) return new SvgProcessPresets().Sanitize();
        throw new System.ArgumentOutOfRangeException("kind");
    }
SvgProcessOptions Base(SvgProcessPresetKind kind, bool allowSemanticLoss)
    {
        SvgProcessOptions result = new SvgProcessOptions();
        result.Preset = kind;
        result.PreserveAccessibility = true;
        result.PreserveViewBox = true;
        result.AllowSemanticLoss = allowSemanticLoss;
        return result;
    }
void AddSafeOperations(SvgProcessOptions options)
    {
        new SvgProcessPresets().Add(options, SvgProcessOperation.RemoveComments);
        new SvgProcessPresets().Add(options, SvgProcessOperation.RemoveEditorMetadata);
        new SvgProcessPresets().Add(options, SvgProcessOperation.RemoveDefaultAttributes);
        new SvgProcessPresets().Add(options, SvgProcessOperation.RemoveUnreferencedInvisibleNodes);
        new SvgProcessPresets().Add(options, SvgProcessOperation.RemoveEmptyContainers);
        new SvgProcessPresets().Add(options, SvgProcessOperation.RemoveUnusedNamespaces);
        new SvgProcessPresets().Add(options, SvgProcessOperation.RemoveUnusedDefinitions);
        new SvgProcessPresets().Add(options, SvgProcessOperation.DeduplicateDefinitions);
        new SvgProcessPresets().Add(options, SvgProcessOperation.CollapseGroups);
        new SvgProcessPresets().Add(options, SvgProcessOperation.HoistCommonAttributes);
        new SvgProcessPresets().Add(options, SvgProcessOperation.MergeAdjacentPaths);
        new SvgProcessPresets().Add(options, SvgProcessOperation.CleanupStyles);
    }
void Add(SvgProcessOptions options, SvgProcessOperation operation)
    {
        options.Operations.Add(operation);
        SvgProcessJob? job = new SvgProcessJobs().Create(operation);
        if (job != null) options.Jobs.Add(job);
    }
}
