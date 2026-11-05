using System;

namespace gltf;

internal class SceneNodeCameraValidator
{
    internal TransformMath TransformMath = new TransformMath();

    internal GltfValidationReport ValidateDocumentSceneNodeCameraRules(
        GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        GltfValidationReport report = new GltfValidationReport();
        SceneTransformAnalysis transforms = TransformMath.EvaluateDocumentTransforms(document);
        CopyTransformIssues(transforms, report);
        ValidateNodes(document, report);
        ValidateCameras(document, report);
        return report;
    }

    internal void CopyTransformIssues(
        SceneTransformAnalysis transforms,
        GltfValidationReport report)
    {
        int index = 0;
        while (index < transforms.Diagnostics.Count)
        {
            GltfValidationIssue issue = transforms.Diagnostics[index];
            report.Issues.Add(issue);
            if (issue.Severity == GltfValidationSeverity.Error)
            {
                report.IsValid = false;
            }

            index++;
        }
    }

    internal void ValidateNodes(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Nodes == null)
        {
            return;
        }

        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[nodeIndex];
            if (node != null)
            {
                string pointer = $"/nodes/{nodeIndex}";
                if (!HasNodeContent(node))
                {
                    AddSceneNodeCameraIssue(
                        report,
                        GltfIssueCodes.NodeEmpty,
                        GltfValidationSeverity.Information,
                        "The node does not contain scene content or a transform.",
                        pointer);
                }

                if (node.HasMatrix && IsIdentityMatrix(node.Matrix))
                {
                    AddSceneNodeCameraIssue(
                        report,
                        GltfIssueCodes.NodeMatrixDefault,
                        GltfValidationSeverity.Information,
                        "The explicitly defined node matrix is the identity default.",
                        $"{pointer}/matrix");
                }

                ValidateNodeWeights(document, node, nodeIndex, report);
            }

            nodeIndex++;
        }
    }

    internal bool HasNodeContent(GltfNode node)
    {
        if (node.HasCamera
            || node.HasSkin
            || node.HasMesh
            || node.HasMatrix
            || node.HasRotation
            || node.HasScale
            || node.HasTranslation
            || node.Weights != null)
        {
            return true;
        }

        if (node.Children != null && node.Children.Count > 0)
        {
            return true;
        }

        return node.PropertyJson != null
            && node.PropertyJson.Extensions != null
            && node.PropertyJson.Extensions.Count > 0;
    }

    internal bool IsIdentityMatrix(double[]? matrix)
    {
        if (matrix == null || matrix.Length != 16)
        {
            return false;
        }

        int index = 0;
        while (index < matrix.Length)
        {
            double expected = index == 0
                    || index == 5
                    || index == 10
                    || index == 15
                ? 1.0
                : 0.0;
            if (matrix[index] != expected)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal void ValidateNodeWeights(
        GltfDocument document,
        GltfNode node,
        int nodeIndex,
        GltfValidationReport report)
    {
        if (node.Weights == null)
        {
            return;
        }

        string pointer = $"/nodes/{nodeIndex}/weights";
        int expectedCount = GetNodeMorphTargetCount(document, node);
        if (expectedCount < 0 || node.Weights.Length != expectedCount)
        {
            AddSceneNodeCameraIssue(
                report,
                GltfIssueCodes.NodeWeightsInvalid,
                GltfValidationSeverity.Error,
                expectedCount < 0
                    ? "Node weights require a valid mesh reference."
                    : $"Node weights count {node.Weights.Length} does not match morph target count {expectedCount}.",
                pointer);
        }

        int weightIndex = 0;
        while (weightIndex < node.Weights.Length)
        {
            if (!double.IsFinite(node.Weights[weightIndex]))
            {
                AddSceneNodeCameraIssue(
                    report,
                    GltfIssueCodes.AccessorInvalidFloat,
                    GltfValidationSeverity.Error,
                    "Node morph weights must be finite.",
                    $"{pointer}/{weightIndex}");
            }

            weightIndex++;
        }
    }

    internal int GetNodeMorphTargetCount(GltfDocument document, GltfNode node)
    {
        if (!node.HasMesh
            || document.Meshes == null
            || node.Mesh < 0
            || node.Mesh >= document.Meshes.Count)
        {
            return -1;
        }

        GltfMesh mesh = document.Meshes[node.Mesh];
        if (mesh == null || mesh.Primitives == null || mesh.Primitives.Count == 0)
        {
            return 0;
        }

        int primitiveIndex = 0;
        while (primitiveIndex < mesh.Primitives.Count)
        {
            GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
            if (primitive != null && primitive.Targets != null)
            {
                return primitive.Targets.Count;
            }

            primitiveIndex++;
        }

        return 0;
    }

    internal void ValidateCameras(
        GltfDocument document,
        GltfValidationReport report)
    {
        if (document.Cameras == null)
        {
            return;
        }

        int cameraIndex = 0;
        while (cameraIndex < document.Cameras.Count)
        {
            GltfCamera camera = document.Cameras[cameraIndex];
            if (camera != null)
            {
                ValidateCamera(
                    camera,
                    $"/cameras/{cameraIndex}",
                    report);
            }

            cameraIndex++;
        }
    }

    internal void ValidateCamera(
        GltfCamera camera,
        string pointer,
        GltfValidationReport report)
    {
        switch (camera.Type)
        {
            case GltfCameraType.Perspective:
                if (camera.Perspective != null)
                {
                    ValidatePerspectiveCamera(
                        camera.Perspective,
                        $"{pointer}/perspective",
                        report);
                }

                if (camera.Orthographic != null)
                {
                    AddInactiveCameraIssue(
                        report,
                        "orthographic",
                        $"{pointer}/orthographic");
                }

                break;
            case GltfCameraType.Orthographic:
                if (camera.Orthographic != null)
                {
                    ValidateOrthographicCamera(
                        camera.Orthographic,
                        $"{pointer}/orthographic",
                        report);
                }

                if (camera.Perspective != null)
                {
                    AddInactiveCameraIssue(
                        report,
                        "perspective",
                        $"{pointer}/perspective");
                }

                break;
            default:
                AddSceneNodeCameraIssue(
                    report,
                    GltfIssueCodes.ValueNotInList,
                    GltfValidationSeverity.Warning,
                    $"Camera type value {(int)camera.Type} is not recognized.",
                    $"{pointer}/type");
                break;
        }
    }

    internal void AddInactiveCameraIssue(
        GltfValidationReport report,
        string member,
        string pointer)
    {
        AddSceneNodeCameraIssue(
            report,
            GltfIssueCodes.ExtraProperty,
            GltfValidationSeverity.Information,
            $"The {member} projection is inactive for the selected camera type.",
            pointer);
    }

    internal void ValidatePerspectiveCamera(
        GltfPerspectiveCamera camera,
        string pointer,
        GltfValidationReport report)
    {
        if (camera.HasAspectRatio
            && (!double.IsFinite(camera.AspectRatio) || camera.AspectRatio <= 0.0))
        {
            AddCameraRangeIssue(report, $"{pointer}/aspectRatio");
        }

        if (!double.IsFinite(camera.VerticalFieldOfView)
            || camera.VerticalFieldOfView <= 0.0)
        {
            AddCameraRangeIssue(report, $"{pointer}/yfov");
        }
        else if (camera.VerticalFieldOfView >= Math.PI)
        {
            AddSceneNodeCameraIssue(
                report,
                GltfIssueCodes.CameraYfovGequalPi,
                GltfValidationSeverity.Warning,
                "The perspective vertical field of view should be less than Pi.",
                $"{pointer}/yfov");
        }

        if (!double.IsFinite(camera.NearPlane) || camera.NearPlane <= 0.0)
        {
            AddCameraRangeIssue(report, $"{pointer}/znear");
        }

        if (camera.HasFarPlane)
        {
            if (!double.IsFinite(camera.FarPlane) || camera.FarPlane <= 0.0)
            {
                AddCameraRangeIssue(report, $"{pointer}/zfar");
            }

            if (double.IsFinite(camera.FarPlane)
                && double.IsFinite(camera.NearPlane)
                && camera.FarPlane <= camera.NearPlane)
            {
                AddSceneNodeCameraIssue(
                    report,
                    GltfIssueCodes.CameraZfarLequalZnear,
                    GltfValidationSeverity.Error,
                    "The perspective far plane must be greater than the near plane.",
                    $"{pointer}/zfar");
            }
        }
    }

    internal void ValidateOrthographicCamera(
        GltfOrthographicCamera camera,
        string pointer,
        GltfValidationReport report)
    {
        ValidateOrthographicMagnitude(
            camera.HorizontalMagnification,
            $"{pointer}/xmag",
            report);
        ValidateOrthographicMagnitude(
            camera.VerticalMagnification,
            $"{pointer}/ymag",
            report);
        if (!double.IsFinite(camera.NearPlane) || camera.NearPlane < 0.0)
        {
            AddCameraRangeIssue(report, $"{pointer}/znear");
        }

        if (!double.IsFinite(camera.FarPlane) || camera.FarPlane <= 0.0)
        {
            AddCameraRangeIssue(report, $"{pointer}/zfar");
        }

        if (double.IsFinite(camera.FarPlane)
            && double.IsFinite(camera.NearPlane)
            && camera.FarPlane <= camera.NearPlane)
        {
            AddSceneNodeCameraIssue(
                report,
                GltfIssueCodes.CameraZfarLequalZnear,
                GltfValidationSeverity.Error,
                "The orthographic far plane must be greater than the near plane.",
                $"{pointer}/zfar");
        }
    }

    internal void ValidateOrthographicMagnitude(
        double magnitude,
        string pointer,
        GltfValidationReport report)
    {
        if (!double.IsFinite(magnitude))
        {
            AddCameraRangeIssue(report, pointer);
        }
        else if (magnitude == 0.0)
        {
            AddSceneNodeCameraIssue(
                report,
                GltfIssueCodes.CameraXmagYmagZero,
                GltfValidationSeverity.Error,
                "Orthographic magnification must not be zero.",
                pointer);
        }
        else if (magnitude < 0.0)
        {
            AddSceneNodeCameraIssue(
                report,
                GltfIssueCodes.CameraXmagYmagNegative,
                GltfValidationSeverity.Warning,
                "Orthographic magnification should not be negative.",
                pointer);
        }
    }

    internal void AddCameraRangeIssue(
        GltfValidationReport report,
        string pointer)
    {
        AddSceneNodeCameraIssue(
            report,
            GltfIssueCodes.ValueNotInRange,
            GltfValidationSeverity.Error,
            "The camera value is outside its permitted finite range.",
            pointer);
    }

    internal void AddSceneNodeCameraIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = severity;
        issue.Message = message;
        issue.JsonPointer = pointer;
        report.Issues.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            report.IsValid = false;
        }
    }
}
