using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal enum AnimationPointerValueType
{
    Float = 0,
    FloatArray = 1,
    Float2 = 2,
    Float3 = 3,
    Float4 = 4,
    Integer = 5,
    Boolean = 6
}

internal class AnimationPointerTarget
{
    internal bool IsValid;
    internal string Pointer = "";
    internal List<string> Segments = new List<string>();
    internal AnimationPointerValueType ValueType = AnimationPointerValueType.Float;
    internal int ComponentCount = 1;
    internal bool Quaternion;
    internal string TargetCollection = "";
    internal int TargetIndex = GltfConstants.AbsentIndex;
    internal string IssueCode = GltfIssueCodes.UnresolvedReference;
    internal string Message = "The animation pointer does not resolve to a mutable property.";
}

internal class AnimationPointerResolver
{
    internal AnimationPointerTarget ResolvePointer(GltfDocument? document, string pointer)
    {
        AnimationPointerTarget result = ParsePointer(pointer);
        if (!result.IsValid)
        {
            return result;
        }

        result.IsValid = false;
        if (document == null)
        {
            result.Message = "The animation pointer cannot be resolved without a document.";
            return result;
        }

        if (result.Segments.Count < 1)
        {
            result.Message = "The animation pointer must identify a mutable property.";
            return result;
        }

        string root = result.Segments[0];
        if (root == "nodes")
        {
            ResolveNodePointer(document, result);
        }
        else if (root == "materials")
        {
            ResolveMaterialPointer(document, result);
        }
        else if (root == "cameras")
        {
            ResolveCameraPointer(document, result);
        }
        else if (root == "extensions")
        {
            ResolveRootExtensionPointer(document, result);
        }
        else
        {
            result.Message = $"Animation pointer root '{root}' is not mutable.";
        }

        return result;
    }

    internal AnimationPointerTarget ParsePointer(string pointer)
    {
        AnimationPointerTarget result = new AnimationPointerTarget();
        result.Pointer = pointer;
        if (pointer == null || pointer.Length == 0 || pointer[0] != '/')
        {
            result.IssueCode = GltfIssueCodes.InvalidJson;
            result.Message = "An animation pointer must be a non-empty RFC 6901 JSON Pointer.";
            return result;
        }

        string[] encoded = pointer.Substring(1).Split('/');
        int index = 0;
        while (index < encoded.Length)
        {
            string? segment = DecodeSegment(encoded[index]);
            if (segment == null)
            {
                result.IssueCode = GltfIssueCodes.InvalidJson;
                result.Message = "An animation pointer contains an invalid RFC 6901 escape.";
                return result;
            }

            result.Segments.Add(segment);
            index++;
        }

        result.IsValid = true;
        return result;
    }

    internal string? DecodeSegment(string encoded)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < encoded.Length)
        {
            char value = encoded[index];
            if (value != '~')
            {
                result.Append(value);
            }
            else
            {
                if (index + 1 >= encoded.Length)
                {
                    return null;
                }

                char escaped = encoded[index + 1];
                if (escaped == '0')
                {
                    result.Append('~');
                }
                else if (escaped == '1')
                {
                    result.Append('/');
                }
                else
                {
                    return null;
                }

                index++;
            }

            index++;
        }

        return result.ToString();
    }

    internal void ResolveNodePointer(GltfDocument document, AnimationPointerTarget result)
    {
        int nodeIndex = ReadCollectionIndex(result, 1);
        if (nodeIndex < 0 || nodeIndex >= document.Nodes.Count)
        {
            result.Message = "The animation pointer node index does not resolve.";
            return;
        }

        GltfNode node = document.Nodes[nodeIndex];
        if (node == null)
        {
            result.Message = "The animation pointer resolves to a null node.";
            return;
        }

        result.TargetCollection = "nodes";
        result.TargetIndex = nodeIndex;
        if (result.Segments.Count == 3)
        {
            string member = result.Segments[2];
            if (member == "translation")
            {
                SetTarget(result, AnimationPointerValueType.Float3, 3, false);
            }
            else if (member == "rotation" && !node.HasMatrix)
            {
                SetTarget(result, AnimationPointerValueType.Float4, 4, true);
            }
            else if (member == "scale" && !node.HasMatrix)
            {
                SetTarget(result, AnimationPointerValueType.Float3, 3, false);
            }
            else if (member == "weights")
            {
                int count = FindMorphTargetCount(document, node);
                if (count > 0)
                {
                    SetTarget(result, AnimationPointerValueType.FloatArray, count, false);
                }
            }
        }
        else if (result.Segments.Count == 4 && result.Segments[2] == "weights")
        {
            int count = FindMorphTargetCount(document, node);
            int weightIndex = ReadCollectionIndex(result, 3);
            if (weightIndex >= 0 && weightIndex < count)
            {
                SetTarget(result, AnimationPointerValueType.Float, 1, false);
            }
        }
        else if (result.Segments.Count == 5 && result.Segments[2] == "extensions")
        {
            string extension = result.Segments[3];
            string member = result.Segments[4];
            bool supported = extension == GltfExtensionNames.KhrNodeVisibility
                    && member == "visible"
                || extension == GltfExtensionNames.KhrNodeHoverability
                    && member == "hoverable"
                || extension == GltfExtensionNames.KhrNodeSelectability
                    && member == "selectable";
            if (supported && HasExtension(node.PropertyJson, extension))
            {
                SetTarget(result, AnimationPointerValueType.Boolean, 1, false);
            }
        }

        if (!result.IsValid)
        {
            result.Message = $"Animation pointer '{result.Pointer}' does not resolve to a mutable node property.";
        }
    }

    internal void ResolveMaterialPointer(
        GltfDocument document,
        AnimationPointerTarget result)
    {
        int materialIndex = ReadCollectionIndex(result, 1);
        if (materialIndex < 0 || materialIndex >= document.Materials.Count)
        {
            result.Message = "The animation pointer material index does not resolve.";
            return;
        }

        GltfMaterial material = document.Materials[materialIndex];
        if (material == null)
        {
            result.Message = "The animation pointer resolves to a null material.";
            return;
        }

        result.TargetCollection = "materials";
        result.TargetIndex = materialIndex;
        if (IsTextureTransformPointer(result))
        {
            ResolveTextureTransform(material, result);
        }
        else if (result.Segments.Count == 3)
        {
            string member = result.Segments[2];
            if (member == "alphaCutoff")
            {
                SetTarget(result, AnimationPointerValueType.Float, 1, false);
            }
            else if (member == "emissiveFactor")
            {
                SetTarget(result, AnimationPointerValueType.Float3, 3, false);
            }
        }
        else if (result.Segments.Count == 4)
        {
            ResolveMaterialCoreNested(material, result);
        }
        else if (result.Segments.Count == 5 && result.Segments[2] == "extensions")
        {
            ResolveMaterialExtension(material, result);
        }
        else if (result.Segments.Count == 6
            && result.Segments[2] == "extensions"
            && result.Segments[3] == GltfExtensionNames.KhrMaterialsClearcoat
            && result.Segments[4] == "clearcoatNormalTexture"
            && result.Segments[5] == "scale"
            && HasExtension(
                material.PropertyJson,
                GltfExtensionNames.KhrMaterialsClearcoat))
        {
            SetTarget(result, AnimationPointerValueType.Float, 1, false);
        }

        if (!result.IsValid)
        {
            result.Message = $"Animation pointer '{result.Pointer}' does not resolve to a mutable material property.";
        }
    }

    internal void ResolveMaterialCoreNested(
        GltfMaterial material,
        AnimationPointerTarget result)
    {
        string container = result.Segments[2];
        string member = result.Segments[3];
        if (container == "normalTexture" && member == "scale"
            && material.NormalTexture != null)
        {
            SetTarget(result, AnimationPointerValueType.Float, 1, false);
        }
        else if (container == "occlusionTexture" && member == "strength"
            && material.OcclusionTexture != null)
        {
            SetTarget(result, AnimationPointerValueType.Float, 1, false);
        }
        else if (container == "pbrMetallicRoughness"
            && material.PbrMetallicRoughness != null)
        {
            if (member == "baseColorFactor")
            {
                SetTarget(result, AnimationPointerValueType.Float4, 4, false);
            }
            else if (member == "metallicFactor" || member == "roughnessFactor")
            {
                SetTarget(result, AnimationPointerValueType.Float, 1, false);
            }
        }
    }

    internal void ResolveMaterialExtension(
        GltfMaterial material,
        AnimationPointerTarget result)
    {
        string extension = result.Segments[3];
        string member = result.Segments[4];
        if (!HasExtension(material.PropertyJson, extension))
        {
            return;
        }

        bool scalar = extension == GltfExtensionNames.KhrMaterialsAnisotropy
                && (member == "anisotropyStrength" || member == "anisotropyRotation")
            || extension == GltfExtensionNames.KhrMaterialsClearcoat
                && (member == "clearcoatFactor"
                    || member == "clearcoatRoughnessFactor")
            || extension == GltfExtensionNames.KhrMaterialsDispersion
                && member == "dispersion"
            || extension == GltfExtensionNames.KhrMaterialsDiffuseTransmission
                && member == "diffuseTransmissionFactor"
            || extension == GltfExtensionNames.KhrMaterialsEmissiveStrength
                && member == "emissiveStrength"
            || extension == GltfExtensionNames.KhrMaterialsIor && member == "ior"
            || extension == GltfExtensionNames.KhrMaterialsIridescence
                && (member == "iridescenceFactor"
                    || member == "iridescenceIor"
                    || member == "iridescenceThicknessMinimum"
                    || member == "iridescenceThicknessMaximum")
            || extension == GltfExtensionNames.KhrMaterialsSheen
                && member == "sheenRoughnessFactor"
            || extension == GltfExtensionNames.KhrMaterialsSpecular
                && member == "specularFactor"
            || extension == GltfExtensionNames.KhrMaterialsTransmission
                && member == "transmissionFactor"
            || extension == GltfExtensionNames.KhrMaterialsVolume
                && (member == "thicknessFactor" || member == "attenuationDistance");
        bool color = extension == GltfExtensionNames.KhrMaterialsDiffuseTransmission
                && member == "diffuseTransmissionColorFactor"
            || extension == GltfExtensionNames.KhrMaterialsSheen
                && member == "sheenColorFactor"
            || extension == GltfExtensionNames.KhrMaterialsSpecular
                && member == "specularColorFactor"
            || extension == GltfExtensionNames.KhrMaterialsVolume
                && member == "attenuationColor";
        if (scalar)
        {
            SetTarget(result, AnimationPointerValueType.Float, 1, false);
        }
        else if (color)
        {
            SetTarget(result, AnimationPointerValueType.Float3, 3, false);
        }
    }

    internal bool IsTextureTransformPointer(AnimationPointerTarget result)
    {
        int count = result.Segments.Count;
        return count >= 6
            && result.Segments[count - 3] == "extensions"
            && result.Segments[count - 2] == GltfExtensionNames.KhrTextureTransform
            && (result.Segments[count - 1] == "offset"
                || result.Segments[count - 1] == "rotation"
                || result.Segments[count - 1] == "scale");
    }

    internal void ResolveTextureTransform(
        GltfMaterial material,
        AnimationPointerTarget result)
    {
        int extensionIndex = result.Segments.Count - 3;
        GltfPropertyJson? property = GetTextureInfoProperty(
            material,
            result.Segments,
            extensionIndex);
        if (property == null
            || !HasExtension(property, GltfExtensionNames.KhrTextureTransform))
        {
            return;
        }

        string member = result.Segments[result.Segments.Count - 1];
        if (member == "rotation")
        {
            SetTarget(result, AnimationPointerValueType.Float, 1, false);
        }
        else
        {
            SetTarget(result, AnimationPointerValueType.Float2, 2, false);
        }
    }

    internal GltfPropertyJson? GetTextureInfoProperty(
        GltfMaterial material,
        List<string> segments,
        int extensionIndex)
    {
        if (extensionIndex == 3)
        {
            if (segments[2] == "normalTexture" && material.NormalTexture != null)
            {
                return material.NormalTexture.PropertyJson;
            }

            if (segments[2] == "occlusionTexture" && material.OcclusionTexture != null)
            {
                return material.OcclusionTexture.PropertyJson;
            }

            if (segments[2] == "emissiveTexture" && material.EmissiveTexture != null)
            {
                return material.EmissiveTexture.PropertyJson;
            }
        }
        else if (extensionIndex == 4
            && segments[2] == "pbrMetallicRoughness"
            && material.PbrMetallicRoughness != null)
        {
            if (segments[3] == "baseColorTexture"
                && material.PbrMetallicRoughness.BaseColorTexture != null)
            {
                return material.PbrMetallicRoughness.BaseColorTexture.PropertyJson;
            }

            if (segments[3] == "metallicRoughnessTexture"
                && material.PbrMetallicRoughness.MetallicRoughnessTexture != null)
            {
                return material.PbrMetallicRoughness.MetallicRoughnessTexture.PropertyJson;
            }
        }
        else if (extensionIndex == 5 && segments[2] == "extensions")
        {
            return GetMaterialExtensionTextureProperty(
                material,
                segments[3],
                segments[4]);
        }

        return null;
    }

    internal GltfPropertyJson? GetMaterialExtensionTextureProperty(
        GltfMaterial material,
        string extension,
        string textureMember)
    {
        if (!material.PropertyJson.TypedExtensions.ContainsKey(extension))
        {
            return null;
        }

        GltfTypedExtensionData typed = material.PropertyJson.TypedExtensions[extension];
        if (extension == GltfExtensionNames.KhrMaterialsAnisotropy
            && typed.MaterialsAnisotropy != null
            && textureMember == "anisotropyTexture"
            && typed.MaterialsAnisotropy.AnisotropyTexture != null)
        {
            return typed.MaterialsAnisotropy.AnisotropyTexture.PropertyJson;
        }

        if (extension == GltfExtensionNames.KhrMaterialsClearcoat
            && typed.MaterialsClearcoat != null)
        {
            if (textureMember == "clearcoatTexture"
                && typed.MaterialsClearcoat.ClearcoatTexture != null)
            {
                return typed.MaterialsClearcoat.ClearcoatTexture.PropertyJson;
            }

            if (textureMember == "clearcoatRoughnessTexture"
                && typed.MaterialsClearcoat.ClearcoatRoughnessTexture != null)
            {
                return typed.MaterialsClearcoat.ClearcoatRoughnessTexture.PropertyJson;
            }

            if (textureMember == "clearcoatNormalTexture"
                && typed.MaterialsClearcoat.ClearcoatNormalTexture != null)
            {
                return typed.MaterialsClearcoat.ClearcoatNormalTexture.PropertyJson;
            }
        }

        if (extension == GltfExtensionNames.KhrMaterialsDiffuseTransmission
            && typed.MaterialsDiffuseTransmission != null)
        {
            if (textureMember == "diffuseTransmissionTexture"
                && typed.MaterialsDiffuseTransmission.DiffuseTransmissionTexture != null)
            {
                return typed.MaterialsDiffuseTransmission.DiffuseTransmissionTexture
                    .PropertyJson;
            }

            if (textureMember == "diffuseTransmissionColorTexture"
                && typed.MaterialsDiffuseTransmission.DiffuseTransmissionColorTexture != null)
            {
                return typed.MaterialsDiffuseTransmission.DiffuseTransmissionColorTexture
                    .PropertyJson;
            }
        }

        if (extension == GltfExtensionNames.KhrMaterialsIridescence
            && typed.MaterialsIridescence != null)
        {
            if (textureMember == "iridescenceTexture"
                && typed.MaterialsIridescence.IridescenceTexture != null)
            {
                return typed.MaterialsIridescence.IridescenceTexture.PropertyJson;
            }

            if (textureMember == "iridescenceThicknessTexture"
                && typed.MaterialsIridescence.IridescenceThicknessTexture != null)
            {
                return typed.MaterialsIridescence.IridescenceThicknessTexture.PropertyJson;
            }
        }

        if (extension == GltfExtensionNames.KhrMaterialsSheen
            && typed.MaterialsSheen != null)
        {
            if (textureMember == "sheenColorTexture"
                && typed.MaterialsSheen.SheenColorTexture != null)
            {
                return typed.MaterialsSheen.SheenColorTexture.PropertyJson;
            }

            if (textureMember == "sheenRoughnessTexture"
                && typed.MaterialsSheen.SheenRoughnessTexture != null)
            {
                return typed.MaterialsSheen.SheenRoughnessTexture.PropertyJson;
            }
        }

        if (extension == GltfExtensionNames.KhrMaterialsSpecular
            && typed.MaterialsSpecular != null)
        {
            if (textureMember == "specularTexture"
                && typed.MaterialsSpecular.SpecularTexture != null)
            {
                return typed.MaterialsSpecular.SpecularTexture.PropertyJson;
            }

            if (textureMember == "specularColorTexture"
                && typed.MaterialsSpecular.SpecularColorTexture != null)
            {
                return typed.MaterialsSpecular.SpecularColorTexture.PropertyJson;
            }
        }

        if (extension == GltfExtensionNames.KhrMaterialsTransmission
            && typed.MaterialsTransmission != null
            && textureMember == "transmissionTexture"
            && typed.MaterialsTransmission.TransmissionTexture != null)
        {
            return typed.MaterialsTransmission.TransmissionTexture.PropertyJson;
        }

        if (extension == GltfExtensionNames.KhrMaterialsVolume
            && typed.MaterialsVolume != null
            && textureMember == "thicknessTexture"
            && typed.MaterialsVolume.ThicknessTexture != null)
        {
            return typed.MaterialsVolume.ThicknessTexture.PropertyJson;
        }

        return null;
    }

    internal void ResolveCameraPointer(GltfDocument document, AnimationPointerTarget result)
    {
        int cameraIndex = ReadCollectionIndex(result, 1);
        if (cameraIndex < 0 || cameraIndex >= document.Cameras.Count)
        {
            result.Message = "The animation pointer camera index does not resolve.";
            return;
        }

        GltfCamera camera = document.Cameras[cameraIndex];
        if (camera == null || result.Segments.Count != 4)
        {
            result.Message = "The animation pointer does not resolve to a camera property.";
            return;
        }

        result.TargetCollection = "cameras";
        result.TargetIndex = cameraIndex;
        string projection = result.Segments[2];
        string member = result.Segments[3];
        if (projection == "orthographic" && camera.Orthographic != null
            && (member == "xmag" || member == "ymag"
                || member == "zfar" || member == "znear"))
        {
            SetTarget(result, AnimationPointerValueType.Float, 1, false);
        }
        else if (projection == "perspective" && camera.Perspective != null)
        {
            bool defined = member == "yfov" || member == "znear"
                || member == "aspectRatio" && camera.Perspective.HasAspectRatio
                || member == "zfar" && camera.Perspective.HasFarPlane;
            if (defined)
            {
                SetTarget(result, AnimationPointerValueType.Float, 1, false);
            }
        }

        if (!result.IsValid)
        {
            result.Message = $"Animation pointer '{result.Pointer}' does not resolve to a mutable camera property.";
        }
    }

    internal void ResolveRootExtensionPointer(
        GltfDocument document,
        AnimationPointerTarget result)
    {
        if ((result.Segments.Count != 5 && result.Segments.Count != 6)
            || result.Segments[1] != GltfExtensionNames.KhrLightsPunctual
            || result.Segments[2] != "lights"
            || !HasExtension(document.PropertyJson, GltfExtensionNames.KhrLightsPunctual))
        {
            result.Message = "The animation pointer does not resolve to a mutable root extension property.";
            return;
        }

        int lightIndex = ReadCollectionIndex(result, 3);
        GltfPunctualLight? light = GetPunctualLight(document, lightIndex);
        if (light == null)
        {
            result.Message = "The animation pointer punctual-light index does not resolve.";
            return;
        }

        result.TargetCollection = "extensions/KHR_lights_punctual/lights";
        result.TargetIndex = lightIndex;
        if (result.Segments.Count == 5)
        {
            string member = result.Segments[4];
            bool defined = member == "color" || member == "intensity"
                || member == "range" && light.HasRange;
            if (defined)
            {
                AnimationPointerValueType valueType =
                    AnimationPointerValueType.Float;
                int count = 1;
                if (member == "color")
                {
                    valueType = AnimationPointerValueType.Float3;
                    count = 3;
                }

                SetTarget(result, valueType, count, false);
            }
        }
        else if (result.Segments[4] == "spot" && light.Spot != null
            && (result.Segments[5] == "innerConeAngle"
                || result.Segments[5] == "outerConeAngle"))
        {
            SetTarget(result, AnimationPointerValueType.Float, 1, false);
        }

        if (!result.IsValid)
        {
            result.Message = $"Animation pointer '{result.Pointer}' does not resolve to a mutable punctual-light property.";
        }
    }

    internal GltfPunctualLight? GetPunctualLight(GltfDocument document, int index)
    {
        if (index < 0
            || !document.PropertyJson.TypedExtensions.ContainsKey(
                GltfExtensionNames.KhrLightsPunctual))
        {
            return null;
        }

        GltfTypedExtensionData typed = document.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrLightsPunctual];
        if (typed.LightsPunctual == null
            || index >= typed.LightsPunctual.Lights.Count)
        {
            return null;
        }

        return typed.LightsPunctual.Lights[index];
    }

    internal int FindMorphTargetCount(GltfDocument document, GltfNode node)
    {
        if (!node.HasMesh || node.Mesh < 0 || node.Mesh >= document.Meshes.Count)
        {
            return 0;
        }

        GltfMesh mesh = document.Meshes[node.Mesh];
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

    internal int ReadCollectionIndex(AnimationPointerTarget target, int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= target.Segments.Count)
        {
            return GltfConstants.AbsentIndex;
        }

        string value = target.Segments[segmentIndex];
        if (value.Length == 0 || value.Length > 1 && value[0] == '0')
        {
            return GltfConstants.AbsentIndex;
        }

        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            int digit = value[index] - '0';
            if (digit < 0 || digit > 9 || result > (int.MaxValue - digit) / 10)
            {
                return GltfConstants.AbsentIndex;
            }

            result = result * 10 + digit;
            index++;
        }

        return result;
    }

    internal bool HasExtension(GltfPropertyJson property, string name)
    {
        return property != null
            && (property.TypedExtensions.ContainsKey(name)
                || property.Extensions.ContainsKey(name));
    }

    internal void SetTarget(
        AnimationPointerTarget result,
        AnimationPointerValueType valueType,
        int componentCount,
        bool quaternion)
    {
        result.IsValid = true;
        result.ValueType = valueType;
        result.ComponentCount = componentCount;
        result.Quaternion = quaternion;
        result.IssueCode = "";
        result.Message = "";
    }

    internal string? GetTargetPointer(GltfAnimationTarget target)
    {
        if (target.PropertyJson.TypedExtensions.ContainsKey(
            GltfExtensionNames.KhrAnimationPointer))
        {
            GltfTypedExtensionData typed = target.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrAnimationPointer];
            if (typed.AnimationPointer != null)
            {
                return typed.AnimationPointer.Pointer;
            }
        }

        if (!target.PropertyJson.Extensions.ContainsKey(
            GltfExtensionNames.KhrAnimationPointer))
        {
            return null;
        }

        JsonNode? extension = target.PropertyJson.Extensions[
            GltfExtensionNames.KhrAnimationPointer];
        if (extension == null || extension.GetValueKind() != JsonValueKind.Object)
        {
            return null;
        }

        JsonNode? pointer = extension["pointer"];
        if (pointer == null || pointer.GetValueKind() != JsonValueKind.String)
        {
            return null;
        }

        return pointer.GetValue<string>();
    }

    internal string EncodePointer(List<string> segments)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < segments.Count)
        {
            result.Append('/');
            result.Append(segments[index]
                .Replace("~", "~0", StringComparison.Ordinal)
                .Replace("/", "~1", StringComparison.Ordinal));
            index++;
        }

        return result.ToString();
    }
}
