using System.Collections.Generic;

namespace gltf;

/// <summary>Provides scene transform, bounds, skin, and projection evaluation.</summary>
public class GltfSceneEvaluator : IGltfSceneEvaluator
{
    internal SceneEvaluator Evaluator;
    internal CameraProjectionCalculator ProjectionCalculator;
    internal RequiredExtensionCapabilityChecker RequiredExtensions;

    /// <summary>Initializes a scene evaluator.</summary>
    public GltfSceneEvaluator()
    {
        Evaluator = new SceneEvaluator();
        ProjectionCalculator = new CameraProjectionCalculator();
        RequiredExtensions = new RequiredExtensionCapabilityChecker();
    }

    /// <summary>Evaluates one scene.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The evaluation options.</param>
    /// <returns>The evaluated scene pose.</returns>
    public GltfScenePose EvaluateScene(GltfDocument document, GltfSceneEvaluationOptions options)
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
            GltfScenePose result = new GltfScenePose();
            result.Status = GltfOperationStatus.Unavailable;
            CopyCapabilityIssues(issues, result.Diagnostics);
            return result;
        }

        return Evaluator.EvaluateDocumentScene(document, options);
    }

    /// <summary>Calculates the bounds of one scene.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The evaluation options.</param>
    /// <returns>The calculated bounds.</returns>
    public GltfBounds CalculateBounds(GltfDocument document, GltfSceneEvaluationOptions options)
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
            throw new GltfUnsupportedFeatureException(issues[0].Message);
        }

        SceneBoundsEvaluation result = Evaluator.CalculateDocumentSceneBounds(document, options);
        if (result.Status != GltfOperationStatus.Success)
        {
            string message = "Scene bounds could not be calculated.";
            if (result.Diagnostics.Count > 0)
            {
                message = result.Diagnostics[0].Message;
            }

            throw new GltfValidationException(message);
        }

        return result.Bounds;
    }

    /// <summary>Calculates a camera projection.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="camera">The camera index.</param>
    /// <returns>The projection result.</returns>
    public GltfProjectionResult CalculateProjection(GltfDocument document, int camera)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        List<GltfValidationIssue> issues =
            RequiredExtensions.CheckEvaluationCapabilities(document);
        if (issues.Count > 0)
        {
            GltfProjectionResult result = new GltfProjectionResult();
            result.Status = GltfOperationStatus.Unavailable;
            CopyCapabilityIssues(issues, result.Diagnostics);
            return result;
        }

        return ProjectionCalculator.CalculateDocumentProjection(document, camera);
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
