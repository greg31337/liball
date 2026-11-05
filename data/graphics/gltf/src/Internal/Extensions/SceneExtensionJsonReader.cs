using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class SceneExtensionJsonReader
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();

    internal GltfExtensionResult ReadSceneExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        JsonObject? source = ReadExtensionObject(context, result);
        if (source == null)
        {
            return result;
        }

        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        if (context.Name == GltfExtensionNames.KhrAnimationPointer)
        {
            typed.AnimationPointer = ReadAnimationPointer(source, context, result);
            AddAnimationPointerReference(context, result, typed.AnimationPointer.Pointer);
        }
        else if (context.Name == GltfExtensionNames.KhrTextureTransform)
        {
            typed.TextureTransform = ReadTextureTransform(source, context, result);
        }
        else if (context.Name == GltfExtensionNames.KhrLightsPunctual)
        {
            if (context.ObjectLocation == "node")
            {
                typed.PunctualLightNode = ReadPunctualLightNode(source, context, result);
                AddPunctualLightReference(context, result, typed.PunctualLightNode.Light);
            }
            else
            {
                typed.LightsPunctual = ReadPunctualLights(source, context, result);
            }
        }
        else if (context.Name == GltfExtensionNames.KhrNodeVisibility)
        {
            typed.NodeVisibility = ReadNodeVisibility(source, context, result);
        }
        else if (context.Name == GltfExtensionNames.KhrNodeHoverability)
        {
            typed.NodeHoverability = ReadNodeHoverability(source, context, result);
        }
        else if (context.Name == GltfExtensionNames.KhrNodeSelectability)
        {
            typed.NodeSelectability = ReadNodeSelectability(source, context, result);
        }
        else if (context.Name == GltfExtensionNames.ExtMeshGpuInstancing)
        {
            typed.MeshGpuInstancing = ReadMeshGpuInstancing(source, context, result);
            AddGpuInstancingReferences(context, result, typed.MeshGpuInstancing);
        }

        result.TypedData = typed;
        return result;
    }

    internal GltfAnimationPointerExtension ReadAnimationPointer(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfAnimationPointerExtension value = new GltfAnimationPointerExtension();
        string pointer = Fallback.CreateExtensionPointer(context) + "/pointer";
        JsonNode? pointerNode = source["pointer"];
        if (!source.ContainsKey("pointer"))
        {
            AddReadIssue(
                result,
                GltfIssueCodes.UndefinedProperty,
                "The animation-pointer payload must define pointer.",
                pointer);
        }
        else if (pointerNode == null || pointerNode.GetValueKind() != JsonValueKind.String)
        {
            AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The animation pointer must be a string.",
                pointer);
        }
        else
        {
            value.Pointer = pointerNode.GetValue<string>();
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["pointer"]);
        return value;
    }

    internal JsonObject? ReadExtensionObject(
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        if (context.Json == null || context.Json.GetValueKind() != JsonValueKind.Object)
        {
            AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The extension payload must be a JSON object.",
                Fallback.CreateExtensionPointer(context));
            return null;
        }

        return context.Json.AsObject();
    }

    internal GltfTextureTransformExtension ReadTextureTransform(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfTextureTransformExtension value = new GltfTextureTransformExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (source.ContainsKey("offset"))
        {
            value.HasOffset = true;
            value.Offset = ReadNumberArray(source["offset"], 2, result, pointer + "/offset");
        }

        if (source.ContainsKey("rotation"))
        {
            value.HasRotation = true;
            value.Rotation = ReadNumberValue(source["rotation"], result, pointer + "/rotation");
        }

        if (source.ContainsKey("scale"))
        {
            value.HasScale = true;
            value.Scale = ReadNumberArray(source["scale"], 2, result, pointer + "/scale");
        }

        if (source.ContainsKey("texCoord"))
        {
            value.HasTextureCoordinate = true;
            value.TextureCoordinate = ReadNonNegativeInteger(
                source["texCoord"],
                result,
                pointer + "/texCoord");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["offset", "rotation", "scale", "texCoord"]);
        return value;
    }

    internal GltfLightsPunctualExtension ReadPunctualLights(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfLightsPunctualExtension value = new GltfLightsPunctualExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        JsonNode? lightsNode = source["lights"];
        if (!source.ContainsKey("lights")
            || lightsNode == null
            || lightsNode.GetValueKind() != JsonValueKind.Array)
        {
            AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The punctual-lights payload must contain a lights array.",
                pointer + "/lights");
        }
        else
        {
            JsonArray lights = lightsNode.AsArray();
            if (lights.Count == 0)
            {
                AddReadIssue(
                    result,
                    GltfIssueCodes.ArrayLengthNotInList,
                    "The punctual-lights array must not be empty.",
                    pointer + "/lights");
            }

            int lightIndex = 0;
            while (lightIndex < lights.Count)
            {
                JsonNode? lightNode = lights[lightIndex];
                string lightPointer = pointer + "/lights/" + lightIndex;
                if (lightNode == null || lightNode.GetValueKind() != JsonValueKind.Object)
                {
                    AddReadIssue(
                        result,
                        GltfIssueCodes.TypeMismatch,
                        "A punctual light must be a JSON object.",
                        lightPointer);
                }
                else
                {
                    value.Lights.Add(ReadPunctualLight(
                        lightNode.AsObject(),
                        result,
                        lightPointer));
                }

                lightIndex++;
            }
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["lights"]);
        return value;
    }

    internal GltfPunctualLight ReadPunctualLight(
        JsonObject source,
        GltfExtensionResult result,
        string pointer)
    {
        GltfPunctualLight value = new GltfPunctualLight();
        JsonNode? typeNode = source["type"];
        if (!source.ContainsKey("type")
            || typeNode == null
            || typeNode.GetValueKind() != JsonValueKind.String)
        {
            AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "A punctual light must contain a string type.",
                pointer + "/type");
        }
        else
        {
            value.Type = typeNode.GetValue<string>();
        }

        if (source.ContainsKey("color"))
        {
            value.HasColor = true;
            value.Color = ReadNumberArray(source["color"], 3, result, pointer + "/color");
        }

        if (source.ContainsKey("intensity"))
        {
            value.HasIntensity = true;
            value.Intensity = ReadNumberValue(source["intensity"], result, pointer + "/intensity");
        }

        if (source.ContainsKey("range"))
        {
            value.HasRange = true;
            value.Range = ReadNumberValue(source["range"], result, pointer + "/range");
        }

        if (source.ContainsKey("spot"))
        {
            JsonNode? spotNode = source["spot"];
            if (spotNode == null || spotNode.GetValueKind() != JsonValueKind.Object)
            {
                AddReadIssue(
                    result,
                    GltfIssueCodes.TypeMismatch,
                    "The punctual-light spot value must be an object.",
                    pointer + "/spot");
            }
            else
            {
                value.Spot = ReadPunctualLightSpot(
                    spotNode.AsObject(),
                    result,
                    pointer + "/spot");
            }
        }

        if (source.ContainsKey("name"))
        {
            JsonNode? nameNode = source["name"];
            if (nameNode == null || nameNode.GetValueKind() != JsonValueKind.String)
            {
                AddReadIssue(
                    result,
                    GltfIssueCodes.TypeMismatch,
                    "The punctual-light name must be a string.",
                    pointer + "/name");
            }
            else
            {
                value.Name = nameNode.GetValue<string>();
            }
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["color", "intensity", "spot", "type", "range", "name"]);
        return value;
    }

    internal GltfPunctualLightSpot ReadPunctualLightSpot(
        JsonObject source,
        GltfExtensionResult result,
        string pointer)
    {
        GltfPunctualLightSpot value = new GltfPunctualLightSpot();
        if (source.ContainsKey("innerConeAngle"))
        {
            value.HasInnerConeAngle = true;
            value.InnerConeAngle = ReadNumberValue(
                source["innerConeAngle"],
                result,
                pointer + "/innerConeAngle");
        }

        if (source.ContainsKey("outerConeAngle"))
        {
            value.HasOuterConeAngle = true;
            value.OuterConeAngle = ReadNumberValue(
                source["outerConeAngle"],
                result,
                pointer + "/outerConeAngle");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["innerConeAngle", "outerConeAngle"]);
        return value;
    }

    internal GltfPunctualLightNodeExtension ReadPunctualLightNode(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfPunctualLightNodeExtension value = new GltfPunctualLightNodeExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (!source.ContainsKey("light"))
        {
            AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "The node punctual-light payload must contain a light index.",
                pointer + "/light");
        }
        else
        {
            value.Light = ReadNonNegativeInteger(source["light"], result, pointer + "/light");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["light"]);
        return value;
    }

    internal GltfNodeVisibilityExtension ReadNodeVisibility(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfNodeVisibilityExtension value = new GltfNodeVisibilityExtension();
        if (source.ContainsKey("visible"))
        {
            value.HasVisible = true;
            value.Visible = ReadBooleanValue(
                source["visible"],
                result,
                Fallback.CreateExtensionPointer(context) + "/visible");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["visible"]);
        return value;
    }

    internal GltfNodeHoverabilityExtension ReadNodeHoverability(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfNodeHoverabilityExtension value = new GltfNodeHoverabilityExtension();
        if (source.ContainsKey("hoverable"))
        {
            value.HasHoverable = true;
            value.Hoverable = ReadBooleanValue(
                source["hoverable"],
                result,
                Fallback.CreateExtensionPointer(context) + "/hoverable");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["hoverable"]);
        return value;
    }

    internal GltfNodeSelectabilityExtension ReadNodeSelectability(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfNodeSelectabilityExtension value = new GltfNodeSelectabilityExtension();
        if (source.ContainsKey("selectable"))
        {
            value.HasSelectable = true;
            value.Selectable = ReadBooleanValue(
                source["selectable"],
                result,
                Fallback.CreateExtensionPointer(context) + "/selectable");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["selectable"]);
        return value;
    }

    internal GltfMeshGpuInstancingExtension ReadMeshGpuInstancing(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfMeshGpuInstancingExtension value = new GltfMeshGpuInstancingExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        JsonNode? attributesNode = source["attributes"];
        if (!source.ContainsKey("attributes")
            || attributesNode == null
            || attributesNode.GetValueKind() != JsonValueKind.Object)
        {
            AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "GPU instancing must contain an attributes object.",
                pointer + "/attributes");
        }
        else
        {
            JsonObject attributes = attributesNode.AsObject();
            if (attributes.Count == 0)
            {
                AddReadIssue(
                    result,
                    GltfIssueCodes.EmptyEntity,
                    "GPU instancing must contain at least one instance attribute.",
                    pointer + "/attributes");
            }

            foreach (KeyValuePair<string, JsonNode?> attribute in attributes)
            {
                value.Attributes[attribute.Key] = ReadNonNegativeInteger(
                    attribute.Value,
                    result,
                    pointer + "/attributes/" + EscapePointerSegment(attribute.Key));
            }
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["attributes"]);
        return value;
    }

    internal double[] ReadNumberArray(
        JsonNode? node,
        int requiredLength,
        GltfExtensionResult result,
        string pointer)
    {
        double[] values = new double[requiredLength];
        if (node == null || node.GetValueKind() != JsonValueKind.Array)
        {
            AddReadIssue(result, GltfIssueCodes.ArrayTypeMismatch, "Expected a number array.", pointer);
            return values;
        }

        JsonArray array = node.AsArray();
        if (array.Count != requiredLength)
        {
            AddReadIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                $"Expected exactly {requiredLength} array values.",
                pointer);
        }

        int count = array.Count;
        if (count > requiredLength)
        {
            count = requiredLength;
        }

        int index = 0;
        while (index < count)
        {
            values[index] = ReadNumberValue(array[index], result, pointer + "/" + index);
            index++;
        }

        return values;
    }

    internal double ReadNumberValue(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Number)
        {
            AddReadIssue(result, GltfIssueCodes.TypeMismatch, "Expected a JSON number.", pointer);
            return 0.0;
        }

        double value = double.Parse(
            node.ToJsonString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
        if (!FiniteNumber.IsFiniteDouble(value))
        {
            AddReadIssue(result, GltfIssueCodes.ValueNotInRange, "Expected a finite number.", pointer);
            return 0.0;
        }

        return value;
    }

    internal int ReadNonNegativeInteger(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Number)
        {
            AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "Expected a non-negative 32-bit integer.",
                pointer);
            return GltfConstants.AbsentIndex;
        }

        double value = ReadNumberValue(node, result, pointer);
        if (value < 0.0 || value > int.MaxValue || value != Math.Truncate(value))
        {
            AddReadIssue(
                result,
                GltfIssueCodes.ValueNotInRange,
                "Expected a non-negative 32-bit integer.",
                pointer);
            return GltfConstants.AbsentIndex;
        }

        return (int)value;
    }

    internal bool ReadBooleanValue(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null
            || (node.GetValueKind() != JsonValueKind.True
                && node.GetValueKind() != JsonValueKind.False))
        {
            AddReadIssue(result, GltfIssueCodes.TypeMismatch, "Expected a JSON boolean.", pointer);
            return false;
        }

        return node.GetValue<bool>();
    }

    internal void AddPunctualLightReference(
        GltfExtensionContext context,
        GltfExtensionResult result,
        int light)
    {
        if (light < 0)
        {
            return;
        }

        GltfDependency reference = new GltfDependency();
        reference.SourceCollection = "nodes";
        reference.SourceIndex = context.ObjectIndex;
        reference.SourceMember = "extensions/KHR_lights_punctual/light";
        reference.TargetCollection = "extensions/KHR_lights_punctual/lights";
        reference.TargetIndex = light;
        result.References.Add(reference);
    }

    internal void AddAnimationPointerReference(
        GltfExtensionContext context,
        GltfExtensionResult result,
        string pointer)
    {
        AnimationPointerTarget target = new AnimationPointerResolver().ParsePointer(pointer);
        if (!target.IsValid || target.Segments.Count < 2)
        {
            return;
        }

        string collection = target.Segments[0];
        int targetIndex = new AnimationPointerResolver().ReadCollectionIndex(target, 1);
        if (collection == "extensions"
            && target.Segments.Count > 3
            && target.Segments[1] == GltfExtensionNames.KhrLightsPunctual
            && target.Segments[2] == "lights")
        {
            collection = "extensions/KHR_lights_punctual/lights";
            targetIndex = new AnimationPointerResolver().ReadCollectionIndex(target, 3);
        }

        if (targetIndex < 0)
        {
            return;
        }

        GltfDependency reference = new GltfDependency();
        reference.SourceCollection = "animations";
        reference.SourceIndex = context.ObjectIndex;
        reference.SourceMember = "channels/target/extensions/KHR_animation_pointer/pointer";
        reference.TargetCollection = collection;
        reference.TargetIndex = targetIndex;
        result.References.Add(reference);
    }

    internal string RemapAnimationPointer(string pointer, GltfIndexRemap remap)
    {
        AnimationPointerResolver resolver = new AnimationPointerResolver();
        AnimationPointerTarget target = resolver.ParsePointer(pointer);
        if (!target.IsValid || target.Segments.Count < 2)
        {
            return pointer;
        }

        string collection = target.Segments[0];
        int oldIndex = resolver.ReadCollectionIndex(target, 1);
        int newIndex = oldIndex;
        if (collection == "accessors" && remap.Accessors.ContainsKey(oldIndex))
        {
            newIndex = remap.Accessors[oldIndex];
        }
        else if (collection == "animations" && remap.Animations.ContainsKey(oldIndex))
        {
            newIndex = remap.Animations[oldIndex];
        }
        else if (collection == "buffers" && remap.Buffers.ContainsKey(oldIndex))
        {
            newIndex = remap.Buffers[oldIndex];
        }
        else if (collection == "bufferViews" && remap.BufferViews.ContainsKey(oldIndex))
        {
            newIndex = remap.BufferViews[oldIndex];
        }
        else if (collection == "cameras" && remap.Cameras.ContainsKey(oldIndex))
        {
            newIndex = remap.Cameras[oldIndex];
        }
        else if (collection == "images" && remap.Images.ContainsKey(oldIndex))
        {
            newIndex = remap.Images[oldIndex];
        }
        else if (collection == "materials" && remap.Materials.ContainsKey(oldIndex))
        {
            newIndex = remap.Materials[oldIndex];
        }
        else if (collection == "meshes" && remap.Meshes.ContainsKey(oldIndex))
        {
            newIndex = remap.Meshes[oldIndex];
        }
        else if (collection == "nodes" && remap.Nodes.ContainsKey(oldIndex))
        {
            newIndex = remap.Nodes[oldIndex];
        }
        else if (collection == "samplers" && remap.Samplers.ContainsKey(oldIndex))
        {
            newIndex = remap.Samplers[oldIndex];
        }
        else if (collection == "scenes" && remap.Scenes.ContainsKey(oldIndex))
        {
            newIndex = remap.Scenes[oldIndex];
        }
        else if (collection == "skins" && remap.Skins.ContainsKey(oldIndex))
        {
            newIndex = remap.Skins[oldIndex];
        }
        else if (collection == "textures" && remap.Textures.ContainsKey(oldIndex))
        {
            newIndex = remap.Textures[oldIndex];
        }

        if (newIndex != oldIndex)
        {
            target.Segments[1] = newIndex.ToString(CultureInfo.InvariantCulture);
            return resolver.EncodePointer(target.Segments);
        }

        return pointer;
    }

    internal void AddGpuInstancingReferences(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMeshGpuInstancingExtension extension)
    {
        List<string> names = PropertyJson.GetSortedIndexKeys(extension.Attributes);
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            int accessor = extension.Attributes[name];
            if (accessor >= 0)
            {
                GltfDependency reference = new GltfDependency();
                reference.SourceCollection = "nodes";
                reference.SourceIndex = context.ObjectIndex;
                reference.SourceMember = "extensions/EXT_mesh_gpu_instancing/attributes/" + name;
                reference.TargetCollection = "accessors";
                reference.TargetIndex = accessor;
                result.References.Add(reference);
            }

            index++;
        }
    }

    internal void AddReadIssue(
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

    internal string EscapePointerSegment(string value)
    {
        return value.Replace("~", "~0", StringComparison.Ordinal)
            .Replace("/", "~1", StringComparison.Ordinal);
    }
}
