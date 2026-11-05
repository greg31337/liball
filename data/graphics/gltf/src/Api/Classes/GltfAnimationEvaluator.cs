using System.Collections.Generic;

namespace gltf;

/// <summary>Provides animation sampling and pose application operations.</summary>
public class GltfAnimationEvaluator : IGltfAnimationEvaluator
{
    internal AnimationSampler Sampler;
    internal RequiredExtensionCapabilityChecker RequiredExtensions;

    /// <summary>Initializes an animation evaluator.</summary>
    public GltfAnimationEvaluator()
    {
        Sampler = new AnimationSampler();
        RequiredExtensions = new RequiredExtensionCapabilityChecker();
    }

    /// <summary>Samples one animation clip.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The animation evaluation options.</param>
    /// <returns>The sampled animation pose.</returns>
    public GltfAnimationPose SampleAnimation(GltfDocument document, GltfAnimationEvaluationOptions options)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        List<GltfValidationIssue> issues =
            RequiredExtensions.CheckEvaluationCapabilities(document);
        if (issues.Count > 0)
        {
            GltfAnimationPose result = new GltfAnimationPose();
            result.Status = GltfOperationStatus.Unavailable;
            CopyCapabilityIssues(issues, result.Diagnostics);
            return result;
        }

        return Sampler.SampleDocumentAnimation(document, options);
    }

    /// <summary>Applies sampled animation values while evaluating a scene.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="pose">The sampled animation pose.</param>
    /// <param name="options">The scene evaluation options.</param>
    /// <returns>The evaluated scene pose.</returns>
    public GltfScenePose ApplyAnimationPose(GltfDocument document, GltfAnimationPose pose, GltfSceneEvaluationOptions options)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        if (pose == null)
        {
            throw new System.ArgumentNullException("pose");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        List<GltfValidationIssue> issues =
            RequiredExtensions.CheckEvaluationCapabilities(document);
        if (issues.Count > 0)
        {
            GltfScenePose result = new GltfScenePose();
            result.Status = GltfOperationStatus.Unavailable;
            CopyCapabilityIssues(issues, result.Diagnostics);
            return result;
        }

        return Sampler.ApplyDocumentAnimationPose(document, pose, options);
    }

    /// <summary>Calculates the duration of one animation clip.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="animation">The animation index.</param>
    /// <returns>The duration in seconds.</returns>
    public double CalculateAnimationDuration(GltfDocument document, int animation)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        List<GltfValidationIssue> issues =
            RequiredExtensions.CheckEvaluationCapabilities(document);
        if (issues.Count > 0)
        {
            throw new GltfUnsupportedFeatureException(issues[0].Message);
        }

        return Sampler.CalculateDocumentAnimationDuration(document, animation);
    }

    internal void CopyCapabilityIssues(
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
