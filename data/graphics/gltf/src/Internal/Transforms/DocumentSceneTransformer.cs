using System;
using System.Numerics;

namespace gltf;

internal class DocumentSceneTransformer
{
    internal SceneTransformSettingsReader Settings = new SceneTransformSettingsReader();
    internal DocumentHierarchyTransformer Hierarchy = new DocumentHierarchyTransformer();
    internal DocumentCoordinateTransformer Coordinates =
        new DocumentCoordinateTransformer();
    internal CoordinateConverter Converter = new CoordinateConverter();
    internal DocumentCompactor Mapping = new DocumentCompactor();

    internal TransformStepExecution TransformSceneDocument(
        GltfDocument document,
        GltfTransformStep step)
    {
        TransformStepExecution execution = new TransformStepExecution();
        execution.Document = document;
        execution.Result.Step = step;
        execution.Result.Remap = Mapping.CreateIdentityRemap(document);
        SceneTransformSettings settings = Settings.ReadSceneTransformSettings(
            step,
            document.Nodes.Count,
            document.Scenes.Count);
        bool changed = false;
        if (step.Kind == GltfTransformKind.ApplyNodeTransform)
        {
            changed = Hierarchy.ApplySelectedNodeMatrix(
                document,
                settings,
                execution.Result);
        }
        else if (step.Kind == GltfTransformKind.BakeNodeTransforms)
        {
            changed = Hierarchy.BakeSelectedNodeTransforms(
                document,
                settings,
                execution.Result);
        }
        else if (step.Kind == GltfTransformKind.FlattenHierarchy)
        {
            changed = Hierarchy.FlattenDocumentHierarchy(
                document,
                settings,
                execution.Result);
        }
        else if (step.Kind == GltfTransformKind.CenterScene
            || step.Kind == GltfTransformKind.GroundScene)
        {
            changed = Hierarchy.AlignSceneBounds(
                document,
                settings,
                step.Kind == GltfTransformKind.GroundScene,
                execution.Result);
        }
        else if (step.Kind == GltfTransformKind.FlipWinding)
        {
            changed = Coordinates.FlipDocumentWinding(
                document,
                execution.Result,
                false);
        }
        else if (step.Kind == GltfTransformKind.ConvertUnits)
        {
            if (settings.UnitScale != 1.0)
            {
                CoordinateConversion conversion = Converter.CreateAxisUnitConversion(
                    Matrix4x4.Identity,
                    settings.UnitScale);
                changed = Coordinates.ConvertDocumentCoordinates(
                    document,
                    conversion,
                    execution.Result);
            }
        }
        else if (step.Kind == GltfTransformKind.ConvertAxes)
        {
            if (!settings.HasAxisBasis)
            {
                throw new ArgumentException(
                    "ConvertAxes requires an 'axisBasis' setting.",
                    "step");
            }

            CoordinateConversion conversion = Converter.CreateAxisUnitConversion(
                settings.AxisBasis,
                1.0);
            changed = Coordinates.ConvertDocumentCoordinates(
                document,
                conversion,
                execution.Result);
        }

        if (changed && document.Container != null)
        {
            document.Container = null;
            execution.Result.ChangedObjects.Add("container");
        }

        return execution;
    }
}
