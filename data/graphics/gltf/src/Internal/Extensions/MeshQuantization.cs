using System;
using System.Collections.Generic;

namespace gltf;

internal class MeshQuantization
{
    internal AttributeSemanticParser SemanticParser = new AttributeSemanticParser();
    internal AccessorNormalization Normalization = new AccessorNormalization();

    internal bool IsEnabled(GltfDocument document)
    {
        return document != null
            && document.ExtensionsUsed != null
            && document.ExtensionsRequired != null
            && document.ExtensionsUsed.Contains(GltfExtensionNames.KhrMeshQuantization)
            && document.ExtensionsRequired.Contains(GltfExtensionNames.KhrMeshQuantization);
    }

    internal string? FindFirstQuantizedAttribute(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (document.Meshes == null || document.Accessors == null)
        {
            return null;
        }

        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            if (mesh != null && mesh.Primitives != null)
            {
                string? pointer = FindQuantizedPrimitiveAttribute(
                    document,
                    mesh,
                    meshIndex);
                if (pointer != null)
                {
                    return pointer;
                }
            }

            meshIndex++;
        }

        return null;
    }

    internal string? FindQuantizedPrimitiveAttribute(
        GltfDocument document,
        GltfMesh mesh,
        int meshIndex)
    {
        int primitiveIndex = 0;
        while (primitiveIndex < mesh.Primitives.Count)
        {
            GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
            if (primitive != null)
            {
                string pointer = $"/meshes/{meshIndex}/primitives/{primitiveIndex}";
                string? attributePointer = FindQuantizedBaseAttribute(
                    document,
                    primitive,
                    pointer);
                if (attributePointer != null)
                {
                    return attributePointer;
                }

                attributePointer = FindQuantizedMorphAttribute(
                    document,
                    primitive,
                    pointer);
                if (attributePointer != null)
                {
                    return attributePointer;
                }
            }

            primitiveIndex++;
        }

        return null;
    }

    internal string? FindQuantizedBaseAttribute(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        string primitivePointer)
    {
        if (primitive.Attributes == null)
        {
            return null;
        }

        int attributeIndex = 0;
        while (attributeIndex < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[attributeIndex];
            if (attribute != null && attribute.Semantic != null)
            {
                GltfAccessor? accessor = ResolveAccessor(
                    document,
                    attribute.Accessor);
                AttributeSemantic semantic = SemanticParser.ParseAttributeSemantic(
                    attribute.Semantic);
                if (accessor != null
                    && semantic.Valid
                    && UsesExpandedBaseFormat(semantic, accessor))
                {
                    return $"{primitivePointer}/attributes/{attribute.Semantic}";
                }
            }

            attributeIndex++;
        }

        return null;
    }

    internal string? FindQuantizedMorphAttribute(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        string primitivePointer)
    {
        if (primitive.Targets == null)
        {
            return null;
        }

        int targetIndex = 0;
        while (targetIndex < primitive.Targets.Count)
        {
            GltfMorphTarget target = primitive.Targets[targetIndex];
            if (target != null && target.Attributes != null)
            {
                int attributeIndex = 0;
                while (attributeIndex < target.Attributes.Count)
                {
                    GltfAttribute attribute = target.Attributes[attributeIndex];
                    if (attribute != null && attribute.Semantic != null)
                    {
                        GltfAccessor? accessor = ResolveAccessor(
                            document,
                            attribute.Accessor);
                        AttributeSemantic semantic = SemanticParser.ParseAttributeSemantic(
                            attribute.Semantic);
                        if (accessor != null
                            && semantic.Valid
                            && UsesExpandedMorphFormat(semantic, accessor))
                        {
                            return $"{primitivePointer}/targets/{targetIndex}/{attribute.Semantic}";
                        }
                    }

                    attributeIndex++;
                }
            }

            targetIndex++;
        }

        return null;
    }

    internal bool UsesExpandedBaseFormat(
        AttributeSemantic semantic,
        GltfAccessor accessor)
    {
        switch (semantic.Kind)
        {
            case AttributeSemanticKind.Position:
                return accessor.Type == GltfAccessorType.Vector3
                    && IsByteOrShort(accessor.ComponentType);
            case AttributeSemanticKind.Normal:
                return accessor.Type == GltfAccessorType.Vector3
                    && IsSignedNormalized(accessor);
            case AttributeSemanticKind.Tangent:
                return accessor.Type == GltfAccessorType.Vector4
                    && IsSignedNormalized(accessor);
            case AttributeSemanticKind.TextureCoordinate:
                return accessor.Type == GltfAccessorType.Vector2
                    && (accessor.ComponentType == GltfComponentType.SignedByte
                        || accessor.ComponentType == GltfComponentType.SignedShort
                        || (!accessor.Normalized
                            && (accessor.ComponentType == GltfComponentType.UnsignedByte
                                || accessor.ComponentType == GltfComponentType.UnsignedShort)));
            default:
                return false;
        }
    }

    internal bool UsesExpandedMorphFormat(
        AttributeSemantic semantic,
        GltfAccessor accessor)
    {
        switch (semantic.Kind)
        {
            case AttributeSemanticKind.Position:
                return accessor.Type == GltfAccessorType.Vector3
                    && (accessor.ComponentType == GltfComponentType.SignedByte
                        || accessor.ComponentType == GltfComponentType.SignedShort);
            case AttributeSemanticKind.Normal:
            case AttributeSemanticKind.Tangent:
                return accessor.Type == GltfAccessorType.Vector3
                    && IsSignedNormalized(accessor);
            case AttributeSemanticKind.TextureCoordinate:
                return accessor.Type == GltfAccessorType.Vector2
                    && !accessor.Normalized
                    && (accessor.ComponentType == GltfComponentType.SignedByte
                        || accessor.ComponentType == GltfComponentType.SignedShort);
            default:
                return false;
        }
    }

    internal void UpdateDeclarations(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (document.ExtensionsUsed == null)
        {
            document.ExtensionsUsed = new List<string>();
        }

        if (document.ExtensionsRequired == null)
        {
            document.ExtensionsRequired = new List<string>();
        }

        if (FindFirstQuantizedAttribute(document) != null)
        {
            AddDeclaration(document.ExtensionsUsed);
            AddDeclaration(document.ExtensionsRequired);
        }
        else
        {
            RemoveDeclarations(document.ExtensionsUsed);
            RemoveDeclarations(document.ExtensionsRequired);
        }
    }

    internal double[] DequantizeStoredBounds(
        GltfAccessor accessor,
        double[] values)
    {
        if (accessor == null)
        {
            throw new ArgumentNullException("accessor");
        }

        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        double[] result = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            double value = values[index];
            if (accessor.Normalized
                && (accessor.ComponentType == GltfComponentType.SignedByte
                    || accessor.ComponentType == GltfComponentType.SignedShort))
            {
                result[index] = Normalization.DecodeSignedValue(
                    (int)value,
                    accessor.ComponentType);
            }
            else if (accessor.Normalized
                && (accessor.ComponentType == GltfComponentType.UnsignedByte
                    || accessor.ComponentType == GltfComponentType.UnsignedShort))
            {
                result[index] = Normalization.DecodeUnsignedValue(
                    (uint)value,
                    accessor.ComponentType);
            }
            else
            {
                result[index] = value;
            }

            index++;
        }

        return result;
    }

    internal GltfAccessor? ResolveAccessor(
        GltfDocument document,
        int accessorIndex)
    {
        if (document.Accessors == null
            || accessorIndex < 0
            || accessorIndex >= document.Accessors.Count)
        {
            return null;
        }

        return document.Accessors[accessorIndex];
    }

    internal bool IsByteOrShort(GltfComponentType componentType)
    {
        return componentType == GltfComponentType.SignedByte
            || componentType == GltfComponentType.UnsignedByte
            || componentType == GltfComponentType.SignedShort
            || componentType == GltfComponentType.UnsignedShort;
    }

    internal bool IsSignedNormalized(GltfAccessor accessor)
    {
        return accessor.Normalized
            && (accessor.ComponentType == GltfComponentType.SignedByte
                || accessor.ComponentType == GltfComponentType.SignedShort);
    }

    internal void AddDeclaration(List<string> declarations)
    {
        if (!declarations.Contains(GltfExtensionNames.KhrMeshQuantization))
        {
            declarations.Add(GltfExtensionNames.KhrMeshQuantization);
        }
    }

    internal void RemoveDeclarations(List<string> declarations)
    {
        while (declarations.Contains(GltfExtensionNames.KhrMeshQuantization))
        {
            declarations.Remove(GltfExtensionNames.KhrMeshQuantization);
        }
    }
}
