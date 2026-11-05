using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class SceneExtensionValidator
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal SceneExtensionJsonReader Reader = new SceneExtensionJsonReader();

    internal GltfExtensionResult ValidateSceneExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result;
        GltfTypedExtensionData? typed;
        if (context.TypedData == null)
        {
            result = Reader.ReadSceneExtension(context, registry);
            typed = result.TypedData;
        }
        else
        {
            result = Fallback.CreatePreservedResult(context, registry);
            typed = context.TypedData;
            result.TypedData = typed;
        }

        if (typed == null)
        {
            return result;
        }

        ValidateExtensionLocation(context, result);
        if (context.Name == GltfExtensionNames.KhrAnimationPointer
            && typed.AnimationPointer != null)
        {
            ValidateAnimationPointer(context, result, typed.AnimationPointer);
        }
        else if (context.Name == GltfExtensionNames.KhrTextureTransform
            && typed.TextureTransform != null)
        {
            ValidateTextureTransform(context, result, typed.TextureTransform);
        }
        else if (context.Name == GltfExtensionNames.KhrLightsPunctual)
        {
            if (typed.PunctualLightNode != null)
            {
                ValidatePunctualLightNode(context, result, typed.PunctualLightNode);
            }
            else if (typed.LightsPunctual != null)
            {
                ValidatePunctualLights(context, result, typed.LightsPunctual);
            }
        }
        else if (context.Name == GltfExtensionNames.ExtMeshGpuInstancing
            && typed.MeshGpuInstancing != null)
        {
            ValidateMeshGpuInstancing(context, result, typed.MeshGpuInstancing);
        }

        return result;
    }

    internal void ValidateExtensionLocation(
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        bool valid = false;
        if (context.Name == GltfExtensionNames.KhrAnimationPointer)
        {
            valid = context.ObjectLocation == "animation.channel.target";
        }
        else if (context.Name == GltfExtensionNames.KhrTextureTransform)
        {
            valid = context.ObjectLocation == "textureInfo";
        }
        else if (context.Name == GltfExtensionNames.KhrLightsPunctual)
        {
            valid = context.ObjectLocation == "glTF" || context.ObjectLocation == "node";
        }
        else if (context.Name == GltfExtensionNames.KhrNodeVisibility
            || context.Name == GltfExtensionNames.KhrNodeHoverability
            || context.Name == GltfExtensionNames.KhrNodeSelectability
            || context.Name == GltfExtensionNames.ExtMeshGpuInstancing)
        {
            valid = context.ObjectLocation == "node";
        }

        if (!valid)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.UnexpectedExtensionObject,
                $"Extension '{context.Name}' is not valid on {context.ObjectLocation}.",
                Fallback.CreateExtensionPointer(context));
        }
    }

    internal void ValidateAnimationPointer(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfAnimationPointerExtension value)
    {
        AnimationPointerTarget target = new AnimationPointerResolver().ResolvePointer(
            context.Document,
            value.Pointer);
        if (!target.IsValid)
        {
            AddValidationIssue(
                result,
                target.IssueCode,
                target.Message,
                Fallback.CreateExtensionPointer(context) + "/pointer");
        }

        if (context.TypedData != null)
        {
            new SceneExtensionJsonReader().AddAnimationPointerReference(
                context,
                result,
                value.Pointer);
        }
    }

    internal void ValidateTextureTransform(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfTextureTransformExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasOffset
            && (value.Offset == null || value.Offset.Length != 2))
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                "Texture-transform offset must contain two values.",
                pointer + "/offset");
        }
        else if (value.HasOffset)
        {
            ValidateTextureTransformValues(
                result,
                value.Offset,
                pointer + "/offset");
        }

        if (value.HasRotation && !FiniteNumber.IsFiniteDouble(value.Rotation))
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                "Texture-transform rotation must be finite.",
                pointer + "/rotation");
        }

        if (value.HasScale
            && (value.Scale == null || value.Scale.Length != 2))
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                "Texture-transform scale must contain two values.",
                pointer + "/scale");
        }
        else if (value.HasScale)
        {
            ValidateTextureTransformValues(
                result,
                value.Scale,
                pointer + "/scale");
        }

        if (value.HasTextureCoordinate && value.TextureCoordinate < 0)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                "Texture-transform texCoord must be non-negative.",
                pointer + "/texCoord");
        }
    }

    internal void ValidateTextureTransformValues(
        GltfExtensionResult result,
        double[] values,
        string pointer)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!FiniteNumber.IsFiniteDouble(values[index]))
            {
                AddValidationIssue(
                    result,
                    GltfIssueCodes.ValueNotInRange,
                    "Texture-transform values must be finite.",
                    pointer + "/" + index);
            }

            index++;
        }
    }

    internal void ValidatePunctualLights(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfLightsPunctualExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context) + "/lights";
        if (value.Lights == null || value.Lights.Count == 0)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                "The punctual-lights array must not be empty.",
                pointer);
            return;
        }

        int index = 0;
        while (index < value.Lights.Count)
        {
            ValidatePunctualLight(result, value.Lights[index], pointer + "/" + index);
            index++;
        }
    }

    internal void ValidatePunctualLight(
        GltfExtensionResult result,
        GltfPunctualLight light,
        string pointer)
    {
        if (light.Type != "directional" && light.Type != "point" && light.Type != "spot")
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.ValueNotInList,
                "Punctual-light type must be directional, point, or spot.",
                pointer + "/type");
        }

        if (light.Color == null || light.Color.Length != 3)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                "Punctual-light color must contain three values.",
                pointer + "/color");
        }
        else
        {
            int colorIndex = 0;
            while (colorIndex < light.Color.Length)
            {
                if (!FiniteNumber.IsFiniteDouble(light.Color[colorIndex])
                    || light.Color[colorIndex] < 0.0
                    || light.Color[colorIndex] > 1.0)
                {
                    AddValidationIssue(
                        result,
                        GltfIssueCodes.ValueNotInRange,
                        "Punctual-light color values must be finite and within [0, 1].",
                        pointer + "/color/" + colorIndex);
                }

                colorIndex++;
            }
        }

        if (!FiniteNumber.IsFiniteDouble(light.Intensity) || light.Intensity < 0.0)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                "Punctual-light intensity must be finite and non-negative.",
                pointer + "/intensity");
        }

        if (light.HasRange
            && (!FiniteNumber.IsFiniteDouble(light.Range) || light.Range <= 0.0))
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                "Punctual-light range must be finite and greater than zero.",
                pointer + "/range");
        }

        if (light.Type == "directional" && light.HasRange)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.ValueNotInList,
                "Directional lights must not define range.",
                pointer + "/range");
        }

        if (light.Type == "spot" && light.Spot == null)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "Spot lights must define spot cone data.",
                pointer + "/spot");
        }
        else if (light.Spot != null)
        {
            if (light.Type != "spot")
            {
                AddValidationIssue(
                    result,
                    GltfIssueCodes.ValueNotInList,
                    "Only spot lights may define spot cone data.",
                    pointer + "/spot");
            }

            ValidatePunctualLightSpot(result, light.Spot, pointer + "/spot");
        }
    }

    internal void ValidatePunctualLightSpot(
        GltfExtensionResult result,
        GltfPunctualLightSpot spot,
        string pointer)
    {
        bool valid = FiniteNumber.IsFiniteDouble(spot.InnerConeAngle)
            && FiniteNumber.IsFiniteDouble(spot.OuterConeAngle)
            && spot.InnerConeAngle >= 0.0
            && spot.InnerConeAngle < spot.OuterConeAngle
            && spot.OuterConeAngle <= Math.PI / 2.0;
        if (!valid)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.KhrLightsPunctualLightSpotAngles,
                "Spot-light cone angles must satisfy 0 <= inner < outer <= PI/2.",
                pointer);
        }
    }

    internal void ValidatePunctualLightNode(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfPunctualLightNodeExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context) + "/light";
        int lightCount = GetPunctualLightCount(context.Document);
        if (value.Light < 0 || value.Light >= lightCount)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                $"Punctual-light index {value.Light} does not resolve to a root light.",
                pointer);
        }
    }

    internal int GetPunctualLightCount(GltfDocument? document)
    {
        if (document == null || document.PropertyJson == null)
        {
            return 0;
        }

        if (document.PropertyJson.TypedExtensions != null
            && document.PropertyJson.TypedExtensions.ContainsKey(
                GltfExtensionNames.KhrLightsPunctual))
        {
            GltfTypedExtensionData typed = document.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrLightsPunctual];
            if (typed.LightsPunctual != null && typed.LightsPunctual.Lights != null)
            {
                return typed.LightsPunctual.Lights.Count;
            }
        }

        if (document.PropertyJson.Extensions != null
            && document.PropertyJson.Extensions.ContainsKey(
                GltfExtensionNames.KhrLightsPunctual))
        {
            JsonNode? raw = document.PropertyJson.Extensions[
                GltfExtensionNames.KhrLightsPunctual];
            if (raw != null && raw.GetValueKind() == JsonValueKind.Object)
            {
                JsonNode? lights = raw["lights"];
                if (lights != null && lights.GetValueKind() == JsonValueKind.Array)
                {
                    return lights.AsArray().Count;
                }
            }
        }

        return 0;
    }

    internal void ValidateMeshGpuInstancing(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMeshGpuInstancingExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.Attributes == null || value.Attributes.Count == 0)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.EmptyEntity,
                "GPU instancing must define at least one instance attribute.",
                pointer + "/attributes");
            return;
        }

        GltfDocument? document = context.Document;
        if (document == null
            || !context.HasObjectIndex
            || context.ObjectIndex < 0
            || context.ObjectIndex >= document.Nodes.Count)
        {
            return;
        }

        GltfNode node = document.Nodes[context.ObjectIndex];
        if (!node.HasMesh)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                "GPU instancing is only valid on a node that references a mesh.",
                pointer);
        }

        int expectedCount = GltfConstants.AbsentIndex;
        List<string> names = new SceneExtensionPropertyJson().GetSortedIndexKeys(value.Attributes);
        int attributeIndex = 0;
        while (attributeIndex < names.Count)
        {
            string name = names[attributeIndex];
            int accessorIndex = value.Attributes[name];
            string attributePointer = pointer + "/attributes/" + name;
            if (!IsInstanceSemanticValid(name))
            {
                AddValidationIssue(
                    result,
                    GltfIssueCodes.MeshPrimitiveInvalidAttribute,
                    $"Instance attribute semantic '{name}' is invalid.",
                    attributePointer);
            }

            if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count)
            {
                AddValidationIssue(
                    result,
                    GltfIssueCodes.UnresolvedReference,
                    $"Instance attribute accessor {accessorIndex} does not resolve.",
                    attributePointer);
            }
            else
            {
                GltfAccessor accessor = document.Accessors[accessorIndex];
                if (expectedCount == GltfConstants.AbsentIndex)
                {
                    expectedCount = accessor.Count;
                }
                else if (accessor.Count != expectedCount)
                {
                    AddValidationIssue(
                        result,
                        GltfIssueCodes.MeshPrimitiveUnequalAccessorCount,
                        "All GPU instance attribute accessors must have equal counts.",
                        attributePointer);
                }

                ValidateInstanceAccessor(result, name, accessor, attributePointer);
            }

            attributeIndex++;
        }
    }

    internal bool IsInstanceSemanticValid(string name)
    {
        if (name == "TRANSLATION" || name == "ROTATION" || name == "SCALE")
        {
            return true;
        }

        return name.Length > 1 && name[0] == '_';
    }

    internal void ValidateInstanceAccessor(
        GltfExtensionResult result,
        string name,
        GltfAccessor accessor,
        string pointer)
    {
        bool valid = true;
        if (name == "TRANSLATION" || name == "SCALE")
        {
            valid = accessor.Type == GltfAccessorType.Vector3
                && accessor.ComponentType == GltfComponentType.Float
                && !accessor.Normalized;
        }
        else if (name == "ROTATION")
        {
            valid = accessor.Type == GltfAccessorType.Vector4
                && (accessor.ComponentType == GltfComponentType.Float
                    || ((accessor.ComponentType == GltfComponentType.SignedByte
                            || accessor.ComponentType == GltfComponentType.SignedShort)
                        && accessor.Normalized));
        }

        if (!valid)
        {
            AddValidationIssue(
                result,
                GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat,
                $"GPU instance attribute '{name}' uses an invalid accessor format.",
                pointer);
        }
    }

    internal void AddValidationIssue(
        GltfExtensionResult result,
        string code,
        string message,
        string pointer)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = message;
        issue.JsonPointer = pointer;
        result.Diagnostics.Add(issue);
        result.Status = GltfOperationStatus.Invalid;
    }
}
