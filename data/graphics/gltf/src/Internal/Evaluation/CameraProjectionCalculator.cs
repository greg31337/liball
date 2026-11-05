using System;
using System.Numerics;

namespace gltf;

internal class CameraProjectionCalculator
{
    internal GltfProjectionResult CalculateDocumentProjection(
        GltfDocument document,
        int cameraIndex)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        GltfProjectionResult result = new GltfProjectionResult();
        if (document.Cameras == null
            || cameraIndex < 0
            || cameraIndex >= document.Cameras.Count)
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.UnresolvedReference,
                $"Camera index {cameraIndex} does not resolve to a camera.",
                $"/cameras/{cameraIndex}");
        }

        GltfCamera camera = document.Cameras[cameraIndex];
        string pointer = $"/cameras/{cameraIndex}";
        if (camera == null)
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ArrayTypeMismatch,
                "The selected camera is null.",
                pointer);
        }

        switch (camera.Type)
        {
            case GltfCameraType.Perspective:
                if (camera.Perspective == null)
                {
                    return FailProjection(
                        result,
                        GltfOperationStatus.Invalid,
                        GltfIssueCodes.UnresolvedReference,
                        "The perspective camera parameters are missing.",
                        $"{pointer}/perspective");
                }

                return CalculatePerspectiveProjection(
                    camera.Perspective,
                    $"{pointer}/perspective",
                    result);
            case GltfCameraType.Orthographic:
                if (camera.Orthographic == null)
                {
                    return FailProjection(
                        result,
                        GltfOperationStatus.Invalid,
                        GltfIssueCodes.UnresolvedReference,
                        "The orthographic camera parameters are missing.",
                        $"{pointer}/orthographic");
                }

                return CalculateOrthographicProjection(
                    camera.Orthographic,
                    $"{pointer}/orthographic",
                    result);
            default:
                return FailProjection(
                    result,
                    GltfOperationStatus.Invalid,
                    GltfIssueCodes.ValueNotInList,
                    $"Camera type value {(int)camera.Type} is not supported.",
                    $"{pointer}/type");
        }
    }

    internal GltfProjectionResult CalculatePerspectiveProjection(
        GltfPerspectiveCamera perspective,
        string pointer,
        GltfProjectionResult result)
    {
        if (!perspective.HasAspectRatio)
        {
            return FailProjection(
                result,
                GltfOperationStatus.Unavailable,
                GltfIssueCodes.OperationUnavailable,
                "A viewport aspect ratio is required when camera.perspective.aspectRatio is absent.",
                $"{pointer}/aspectRatio");
        }

        if (!double.IsFinite(perspective.AspectRatio)
            || perspective.AspectRatio <= 0.0)
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "The perspective aspect ratio must be finite and greater than zero.",
                $"{pointer}/aspectRatio");
        }

        if (!double.IsFinite(perspective.VerticalFieldOfView)
            || perspective.VerticalFieldOfView <= 0.0)
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "The perspective vertical field of view must be finite and greater than zero.",
                $"{pointer}/yfov");
        }

        if (perspective.VerticalFieldOfView >= Math.PI)
        {
            AddProjectionIssue(
                result,
                GltfIssueCodes.CameraYfovGequalPi,
                GltfValidationSeverity.Warning,
                "The perspective vertical field of view should be less than Pi.",
                $"{pointer}/yfov");
        }

        if (!double.IsFinite(perspective.NearPlane)
            || perspective.NearPlane <= 0.0)
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "The perspective near plane must be finite and greater than zero.",
                $"{pointer}/znear");
        }

        if (perspective.HasFarPlane)
        {
            if (!double.IsFinite(perspective.FarPlane))
            {
                return FailProjection(
                    result,
                    GltfOperationStatus.Invalid,
                    GltfIssueCodes.ValueNotInRange,
                    "The perspective far plane must be finite when present.",
                    $"{pointer}/zfar");
            }

            if (perspective.FarPlane <= perspective.NearPlane)
            {
                return FailProjection(
                    result,
                    GltfOperationStatus.Invalid,
                    GltfIssueCodes.CameraZfarLequalZnear,
                    "The perspective far plane must be greater than the near plane.",
                    $"{pointer}/zfar");
            }
        }

        double tangent = Math.Tan(0.5 * perspective.VerticalFieldOfView);
        double horizontalScale = 1.0 / (perspective.AspectRatio * tangent);
        double verticalScale = 1.0 / tangent;
        if (!double.IsFinite(horizontalScale)
            || !double.IsFinite(verticalScale)
            || horizontalScale == 0.0
            || verticalScale == 0.0)
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "The perspective parameters do not produce a finite projection scale.",
                pointer);
        }

        if (perspective.HasFarPlane)
        {
            double depthScale = (perspective.FarPlane + perspective.NearPlane)
                / (perspective.NearPlane - perspective.FarPlane);
            double depthTranslation = (2.0 * perspective.FarPlane * perspective.NearPlane)
                / (perspective.NearPlane - perspective.FarPlane);
            if (!AreProjectionFactorsRepresentable(
                horizontalScale,
                verticalScale,
                depthScale,
                depthTranslation))
            {
                return FailProjection(
                    result,
                    GltfOperationStatus.Invalid,
                    GltfIssueCodes.ValueNotInRange,
                    "The perspective parameters exceed the projection matrix range.",
                    pointer);
            }

            result.Projection = new Matrix4x4(
                (float)horizontalScale, 0.0f, 0.0f, 0.0f,
                0.0f, (float)verticalScale, 0.0f, 0.0f,
                0.0f, 0.0f, (float)depthScale, (float)depthTranslation,
                0.0f, 0.0f, -1.0f, 0.0f);
        }
        else
        {
            double depthTranslation = -2.0 * perspective.NearPlane;
            if (!AreProjectionFactorsRepresentable(
                horizontalScale,
                verticalScale,
                -1.0,
                depthTranslation))
            {
                return FailProjection(
                    result,
                    GltfOperationStatus.Invalid,
                    GltfIssueCodes.ValueNotInRange,
                    "The perspective parameters exceed the projection matrix range.",
                    pointer);
            }

            result.Projection = new Matrix4x4(
                (float)horizontalScale, 0.0f, 0.0f, 0.0f,
                0.0f, (float)verticalScale, 0.0f, 0.0f,
                0.0f, 0.0f, -1.0f, (float)depthTranslation,
                0.0f, 0.0f, -1.0f, 0.0f);
        }

        return result;
    }

    internal GltfProjectionResult CalculateOrthographicProjection(
        GltfOrthographicCamera orthographic,
        string pointer,
        GltfProjectionResult result)
    {
        if (!double.IsFinite(orthographic.HorizontalMagnification))
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "The orthographic horizontal magnification must be finite.",
                $"{pointer}/xmag");
        }

        if (!double.IsFinite(orthographic.VerticalMagnification))
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "The orthographic vertical magnification must be finite.",
                $"{pointer}/ymag");
        }

        if (orthographic.HorizontalMagnification == 0.0)
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.CameraXmagYmagZero,
                "The orthographic horizontal magnification must not be zero.",
                $"{pointer}/xmag");
        }

        if (orthographic.VerticalMagnification == 0.0)
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.CameraXmagYmagZero,
                "The orthographic vertical magnification must not be zero.",
                $"{pointer}/ymag");
        }

        if (orthographic.HorizontalMagnification < 0.0)
        {
            AddProjectionIssue(
                result,
                GltfIssueCodes.CameraXmagYmagNegative,
                GltfValidationSeverity.Warning,
                "The orthographic horizontal magnification should not be negative.",
                $"{pointer}/xmag");
        }

        if (orthographic.VerticalMagnification < 0.0)
        {
            AddProjectionIssue(
                result,
                GltfIssueCodes.CameraXmagYmagNegative,
                GltfValidationSeverity.Warning,
                "The orthographic vertical magnification should not be negative.",
                $"{pointer}/ymag");
        }

        if (!double.IsFinite(orthographic.NearPlane)
            || orthographic.NearPlane < 0.0)
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "The orthographic near plane must be finite and non-negative.",
                $"{pointer}/znear");
        }

        if (!double.IsFinite(orthographic.FarPlane))
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "The orthographic far plane must be finite.",
                $"{pointer}/zfar");
        }

        if (orthographic.FarPlane <= orthographic.NearPlane)
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.CameraZfarLequalZnear,
                "The orthographic far plane must be greater than the near plane.",
                $"{pointer}/zfar");
        }

        double horizontalScale = 1.0 / orthographic.HorizontalMagnification;
        double verticalScale = 1.0 / orthographic.VerticalMagnification;
        double depthScale = 2.0 / (orthographic.NearPlane - orthographic.FarPlane);
        double depthTranslation = (orthographic.FarPlane + orthographic.NearPlane)
            / (orthographic.NearPlane - orthographic.FarPlane);
        if (!AreProjectionFactorsRepresentable(
            horizontalScale,
            verticalScale,
            depthScale,
            depthTranslation))
        {
            return FailProjection(
                result,
                GltfOperationStatus.Invalid,
                GltfIssueCodes.ValueNotInRange,
                "The orthographic parameters exceed the projection matrix range.",
                pointer);
        }

        result.Projection = new Matrix4x4(
            (float)horizontalScale, 0.0f, 0.0f, 0.0f,
            0.0f, (float)verticalScale, 0.0f, 0.0f,
            0.0f, 0.0f, (float)depthScale, (float)depthTranslation,
            0.0f, 0.0f, 0.0f, 1.0f);
        return result;
    }

    internal bool AreProjectionFactorsRepresentable(
        double horizontalScale,
        double verticalScale,
        double depthScale,
        double depthTranslation)
    {
        return double.IsFinite(horizontalScale)
            && double.IsFinite(verticalScale)
            && double.IsFinite(depthScale)
            && double.IsFinite(depthTranslation)
            && Math.Abs(horizontalScale) <= float.MaxValue
            && Math.Abs(verticalScale) <= float.MaxValue
            && Math.Abs(depthScale) <= float.MaxValue
            && Math.Abs(depthTranslation) <= float.MaxValue
            && (float)horizontalScale != 0.0f
            && (float)verticalScale != 0.0f
            && (float)depthScale != 0.0f
            && (float)depthTranslation != 0.0f;
    }

    internal GltfProjectionResult FailProjection(
        GltfProjectionResult result,
        GltfOperationStatus status,
        string code,
        string message,
        string pointer)
    {
        result.Status = status;
        AddProjectionIssue(
            result,
            code,
            GltfValidationSeverity.Error,
            message,
            pointer);
        return result;
    }

    internal void AddProjectionIssue(
        GltfProjectionResult result,
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
        result.Diagnostics.Add(issue);
    }
}
