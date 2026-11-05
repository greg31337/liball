using System;
using System.Collections.Generic;

namespace gltf;

internal enum MaterialTextureSlot
{
    BaseColor = 0,
    MetallicRoughness = 1,
    Normal = 2,
    Occlusion = 3,
    Emissive = 4
}

internal enum MaterialTextureColorSpace
{
    Linear = 0,
    Srgb = 1
}

internal enum MaterialTextureChannelUse
{
    Unused = 0,
    BaseColorRed = 1,
    BaseColorGreen = 2,
    BaseColorBlue = 3,
    Alpha = 4,
    Roughness = 5,
    Metallic = 6,
    NormalX = 7,
    NormalY = 8,
    NormalZ = 9,
    Occlusion = 10,
    EmissiveRed = 11,
    EmissiveGreen = 12,
    EmissiveBlue = 13
}

internal class MaterialTextureBinding
{
    internal MaterialTextureSlot Slot = MaterialTextureSlot.BaseColor;
    internal int Texture = GltfConstants.AbsentIndex;
    internal int TextureCoordinate;
    internal MaterialTextureColorSpace ColorSpace = MaterialTextureColorSpace.Linear;
    internal MaterialTextureChannelUse Red = MaterialTextureChannelUse.Unused;
    internal MaterialTextureChannelUse Green = MaterialTextureChannelUse.Unused;
    internal MaterialTextureChannelUse Blue = MaterialTextureChannelUse.Unused;
    internal MaterialTextureChannelUse Alpha = MaterialTextureChannelUse.Unused;
    internal bool HasTextureTransform;
    internal double[] TextureOffset = [0.0, 0.0];
    internal double TextureRotation;
    internal double[] TextureScale = [1.0, 1.0];
}

internal class MaterialEvaluation
{
    internal bool UsesDefaultMaterial;
    internal double[] BaseColorFactor = [1.0, 1.0, 1.0, 1.0];
    internal double MetallicFactor = 1.0;
    internal double RoughnessFactor = 1.0;
    internal double[] EmissiveFactor = [0.0, 0.0, 0.0];
    internal GltfAlphaMode AlphaMode = GltfAlphaMode.Opaque;
    internal double AlphaCutoff = 0.5;
    internal bool DoubleSided;
    internal bool FullyOpaque = true;
    internal bool UsesAlphaMask;
    internal bool UsesAlphaBlend;
    internal double NormalScale = 1.0;
    internal double OcclusionStrength = 1.0;
    internal List<MaterialTextureBinding> Textures = new List<MaterialTextureBinding>();
}

internal class MaterialAlphaEvaluation
{
    internal double SourceAlpha = 1.0;
    internal double OutputAlpha = 1.0;
    internal bool Discarded;
    internal bool RequiresBlending;
}

internal class MaterialEvaluator
{
    internal MaterialEvaluation EvaluateMaterial(GltfMaterial? material)
    {
        MaterialEvaluation result = new MaterialEvaluation();
        if (material == null)
        {
            result.UsesDefaultMaterial = true;
            return result;
        }

        EvaluatePbrFactors(material.PbrMetallicRoughness, result);
        if (material.HasEmissiveFactor
            && material.EmissiveFactor != null
            && material.EmissiveFactor.Length == 3)
        {
            result.EmissiveFactor = CopyMaterialFactor(
                material.EmissiveFactor,
                3);
        }

        if (material.HasAlphaMode)
        {
            result.AlphaMode = material.AlphaMode;
        }

        if (material.HasAlphaCutoff)
        {
            result.AlphaCutoff = material.AlphaCutoff;
        }

        if (material.HasDoubleSided)
        {
            result.DoubleSided = material.DoubleSided;
        }

        SetAlphaBehavior(result);
        AddPbrTextureBindings(material.PbrMetallicRoughness, result);
        if (material.NormalTexture != null)
        {
            GltfNormalTextureInfo normal = material.NormalTexture;
            if (normal.HasScale)
            {
                result.NormalScale = normal.Scale;
            }

            result.Textures.Add(CreateTextureBinding(
                MaterialTextureSlot.Normal,
                normal.Index,
                GetNormalTextureCoordinate(normal),
                normal.PropertyJson));
        }

        if (material.OcclusionTexture != null)
        {
            GltfOcclusionTextureInfo occlusion = material.OcclusionTexture;
            if (occlusion.HasStrength)
            {
                result.OcclusionStrength = occlusion.Strength;
            }

            result.Textures.Add(CreateTextureBinding(
                MaterialTextureSlot.Occlusion,
                occlusion.Index,
                GetOcclusionTextureCoordinate(occlusion),
                occlusion.PropertyJson));
        }

        if (material.EmissiveTexture != null)
        {
            result.Textures.Add(CreateTextureBinding(
                MaterialTextureSlot.Emissive,
                material.EmissiveTexture.Index,
                GetTextureCoordinate(material.EmissiveTexture),
                material.EmissiveTexture.PropertyJson));
        }

        return result;
    }

    internal void EvaluatePbrFactors(
        GltfPbrMetallicRoughness? pbr,
        MaterialEvaluation result)
    {
        if (pbr == null)
        {
            return;
        }

        if (pbr.HasBaseColorFactor
            && pbr.BaseColorFactor != null
            && pbr.BaseColorFactor.Length == 4)
        {
            result.BaseColorFactor = CopyMaterialFactor(
                pbr.BaseColorFactor,
                4);
        }

        if (pbr.HasMetallicFactor)
        {
            result.MetallicFactor = pbr.MetallicFactor;
        }

        if (pbr.HasRoughnessFactor)
        {
            result.RoughnessFactor = pbr.RoughnessFactor;
        }
    }

    internal void AddPbrTextureBindings(
        GltfPbrMetallicRoughness? pbr,
        MaterialEvaluation result)
    {
        if (pbr == null)
        {
            return;
        }

        if (pbr.BaseColorTexture != null)
        {
            result.Textures.Add(CreateTextureBinding(
                MaterialTextureSlot.BaseColor,
                pbr.BaseColorTexture.Index,
                GetTextureCoordinate(pbr.BaseColorTexture),
                pbr.BaseColorTexture.PropertyJson));
        }

        if (pbr.MetallicRoughnessTexture != null)
        {
            result.Textures.Add(CreateTextureBinding(
                MaterialTextureSlot.MetallicRoughness,
                pbr.MetallicRoughnessTexture.Index,
                GetTextureCoordinate(pbr.MetallicRoughnessTexture),
                pbr.MetallicRoughnessTexture.PropertyJson));
        }
    }

    internal MaterialTextureBinding CreateTextureBinding(
        MaterialTextureSlot slot,
        int texture,
        int textureCoordinate,
        GltfPropertyJson propertyJson)
    {
        MaterialTextureBinding result = new MaterialTextureBinding();
        result.Slot = slot;
        result.Texture = texture;
        result.TextureCoordinate = textureCoordinate;
        switch (slot)
        {
            case MaterialTextureSlot.BaseColor:
                result.ColorSpace = MaterialTextureColorSpace.Srgb;
                result.Red = MaterialTextureChannelUse.BaseColorRed;
                result.Green = MaterialTextureChannelUse.BaseColorGreen;
                result.Blue = MaterialTextureChannelUse.BaseColorBlue;
                result.Alpha = MaterialTextureChannelUse.Alpha;
                break;
            case MaterialTextureSlot.MetallicRoughness:
                result.Green = MaterialTextureChannelUse.Roughness;
                result.Blue = MaterialTextureChannelUse.Metallic;
                break;
            case MaterialTextureSlot.Normal:
                result.Red = MaterialTextureChannelUse.NormalX;
                result.Green = MaterialTextureChannelUse.NormalY;
                result.Blue = MaterialTextureChannelUse.NormalZ;
                break;
            case MaterialTextureSlot.Occlusion:
                result.Red = MaterialTextureChannelUse.Occlusion;
                break;
            case MaterialTextureSlot.Emissive:
                result.ColorSpace = MaterialTextureColorSpace.Srgb;
                result.Red = MaterialTextureChannelUse.EmissiveRed;
                result.Green = MaterialTextureChannelUse.EmissiveGreen;
                result.Blue = MaterialTextureChannelUse.EmissiveBlue;
                break;
            default:
                throw new ArgumentOutOfRangeException("slot");
        }

        ApplyTextureTransform(propertyJson, result);
        return result;
    }

    internal void ApplyTextureTransform(
        GltfPropertyJson propertyJson,
        MaterialTextureBinding result)
    {
        if (!propertyJson.TypedExtensions.ContainsKey(
            GltfExtensionNames.KhrTextureTransform))
        {
            return;
        }

        GltfTypedExtensionData typed = propertyJson.TypedExtensions[
            GltfExtensionNames.KhrTextureTransform];
        if (typed.TextureTransform == null)
        {
            return;
        }

        GltfTextureTransformExtension transform = typed.TextureTransform;
        result.HasTextureTransform = true;
        if (transform.Offset != null && transform.Offset.Length == 2)
        {
            result.TextureOffset = CopyMaterialFactor(transform.Offset, 2);
        }

        result.TextureRotation = transform.Rotation;
        if (transform.Scale != null && transform.Scale.Length == 2)
        {
            result.TextureScale = CopyMaterialFactor(transform.Scale, 2);
        }

        if (transform.HasTextureCoordinate)
        {
            result.TextureCoordinate = transform.TextureCoordinate;
        }
    }

    internal MaterialAlphaEvaluation EvaluateMaterialAlpha(
        MaterialEvaluation material,
        double textureAlpha,
        double vertexAlpha)
    {
        if (material == null)
        {
            throw new ArgumentNullException("material");
        }

        MaterialAlphaEvaluation result = new MaterialAlphaEvaluation();
        result.SourceAlpha = material.BaseColorFactor[3]
            * textureAlpha
            * vertexAlpha;
        switch (material.AlphaMode)
        {
            case GltfAlphaMode.Opaque:
                result.OutputAlpha = 1.0;
                break;
            case GltfAlphaMode.Mask:
                if (result.SourceAlpha < material.AlphaCutoff)
                {
                    result.OutputAlpha = 0.0;
                    result.Discarded = true;
                }
                else
                {
                    result.OutputAlpha = 1.0;
                }

                break;
            case GltfAlphaMode.Blend:
                result.OutputAlpha = result.SourceAlpha;
                result.RequiresBlending = true;
                break;
            default:
                result.OutputAlpha = 1.0;
                break;
        }

        return result;
    }

    internal double[] EvaluateNormalTextureSample(
        MaterialEvaluation material,
        double red,
        double green,
        double blue)
    {
        if (material == null)
        {
            throw new ArgumentNullException("material");
        }

        double x = (red * 2.0 - 1.0) * material.NormalScale;
        double y = (green * 2.0 - 1.0) * material.NormalScale;
        double z = blue * 2.0 - 1.0;
        double length = Math.Sqrt(x * x + y * y + z * z);
        if (length == 0.0
            || double.IsNaN(length)
            || double.IsInfinity(length))
        {
            return [0.0, 0.0, 1.0];
        }

        return [x / length, y / length, z / length];
    }

    internal double EvaluateOcclusionTextureSample(
        MaterialEvaluation material,
        double sample)
    {
        if (material == null)
        {
            throw new ArgumentNullException("material");
        }

        return 1.0 + material.OcclusionStrength * (sample - 1.0);
    }

    internal void SetAlphaBehavior(MaterialEvaluation result)
    {
        result.FullyOpaque = result.AlphaMode == GltfAlphaMode.Opaque;
        result.UsesAlphaMask = result.AlphaMode == GltfAlphaMode.Mask;
        result.UsesAlphaBlend = result.AlphaMode == GltfAlphaMode.Blend;
    }

    internal int GetTextureCoordinate(GltfTextureInfo texture)
    {
        if (texture.HasTextureCoordinate)
        {
            return texture.TextureCoordinate;
        }

        return 0;
    }

    internal int GetNormalTextureCoordinate(GltfNormalTextureInfo texture)
    {
        if (texture.HasTextureCoordinate)
        {
            return texture.TextureCoordinate;
        }

        return 0;
    }

    internal int GetOcclusionTextureCoordinate(GltfOcclusionTextureInfo texture)
    {
        if (texture.HasTextureCoordinate)
        {
            return texture.TextureCoordinate;
        }

        return 0;
    }

    internal double[] CopyMaterialFactor(double[] source, int length)
    {
        double[] result = new double[length];
        int index = 0;
        while (index < length)
        {
            result[index] = source[index];
            index++;
        }

        return result;
    }
}
