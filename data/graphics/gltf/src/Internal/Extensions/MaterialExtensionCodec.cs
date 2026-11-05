using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class MaterialExtensionCodec
{
    internal MaterialExtensionJsonReader Reader = new MaterialExtensionJsonReader();
    internal MaterialExtensionJsonWriter Writer = new MaterialExtensionJsonWriter();
    internal MaterialExtensionValidator Validator = new MaterialExtensionValidator();
    internal ExtensionFallback Fallback = new ExtensionFallback();

    internal bool HandlesMaterialExtension(string name)
    {
        return Reader.HandlesMaterialExtension(name);
    }

    internal GltfExtensionResult ReadTypedMaterialExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        return Reader.ReadMaterialExtension(context, registry);
    }

    internal GltfExtensionResult WriteTypedMaterialExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null && context.Json != null)
        {
            GltfExtensionResult parsed = Reader.ReadMaterialExtension(context, registry);
            typed = parsed.TypedData;
            CopyDiagnostics(parsed.Diagnostics, result.Diagnostics);
            result.Status = parsed.Status;
        }

        if (typed == null)
        {
            return result;
        }

        JsonNode? json = Writer.WriteMaterialExtension(context, typed);
        if (json != null)
        {
            result.Json = json;
            result.TypedData = typed;
            PopulateReferences(context, result, registry, json);
        }

        return result;
    }

    internal GltfExtensionResult ValidateTypedMaterialExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Validator.ValidateMaterialExtension(context, registry);
        if (context.TypedData != null)
        {
            JsonNode? json = Writer.WriteMaterialExtension(context, context.TypedData);
            if (json != null)
            {
                PopulateReferences(context, result, registry, json);
            }
        }

        return result;
    }

    internal GltfExtensionResult RemapTypedMaterialExtension(
        GltfExtensionContext context,
        GltfIndexRemap remap,
        ExtensionRegistry registry)
    {
        GltfTypedExtensionData? typed = context.TypedData;
        if (typed == null)
        {
            GltfExtensionResult parsed = Reader.ReadMaterialExtension(context, registry);
            typed = parsed.TypedData;
            if (typed == null)
            {
                return parsed;
            }
        }

        JsonNode? copiedJson = Writer.WriteMaterialExtension(context, typed);
        if (copiedJson == null)
        {
            return Fallback.CreatePreservedResult(context, registry);
        }

        GltfExtensionContext copyContext = CopyContext(context, copiedJson);
        GltfExtensionResult copied = Reader.ReadMaterialExtension(copyContext, registry);
        if (copied.TypedData == null)
        {
            return copied;
        }

        ApplyIndexRemap(copied.TypedData, remap);
        JsonNode? remappedJson = Writer.WriteMaterialExtension(copyContext, copied.TypedData);
        if (remappedJson == null)
        {
            return copied;
        }

        GltfExtensionContext resultContext = CopyContext(context, remappedJson);
        return Reader.ReadMaterialExtension(resultContext, registry);
    }

    internal GltfExtensionContext CopyContext(
        GltfExtensionContext source,
        JsonNode json)
    {
        GltfExtensionContext result = new GltfExtensionContext();
        result.Document = source.Document;
        result.ObjectJsonPointer = source.ObjectJsonPointer;
        result.Name = source.Name;
        result.ObjectLocation = source.ObjectLocation;
        result.HasObjectIndex = source.HasObjectIndex;
        result.ObjectIndex = source.ObjectIndex;
        result.HasSubObjectIndex = source.HasSubObjectIndex;
        result.SubObjectIndex = source.SubObjectIndex;
        result.Json = json;
        result.Resources = source.Resources;
        return result;
    }

    internal void ApplyIndexRemap(
        GltfTypedExtensionData typed,
        GltfIndexRemap remap)
    {
        if (typed.MaterialsAnisotropy != null)
        {
            RemapTexture(typed.MaterialsAnisotropy.AnisotropyTexture, remap);
        }
        else if (typed.MaterialsClearcoat != null)
        {
            RemapTexture(typed.MaterialsClearcoat.ClearcoatTexture, remap);
            RemapTexture(typed.MaterialsClearcoat.ClearcoatRoughnessTexture, remap);
            RemapNormalTexture(typed.MaterialsClearcoat.ClearcoatNormalTexture, remap);
        }
        else if (typed.MaterialsDiffuseTransmission != null)
        {
            RemapTexture(
                typed.MaterialsDiffuseTransmission.DiffuseTransmissionTexture,
                remap);
            RemapTexture(
                typed.MaterialsDiffuseTransmission.DiffuseTransmissionColorTexture,
                remap);
        }
        else if (typed.MaterialsPbrSpecularGlossiness != null)
        {
            RemapTexture(typed.MaterialsPbrSpecularGlossiness.DiffuseTexture, remap);
            RemapTexture(
                typed.MaterialsPbrSpecularGlossiness.SpecularGlossinessTexture,
                remap);
        }
        else if (typed.MaterialsIridescence != null)
        {
            RemapTexture(typed.MaterialsIridescence.IridescenceTexture, remap);
            RemapTexture(typed.MaterialsIridescence.IridescenceThicknessTexture, remap);
        }
        else if (typed.MaterialsSheen != null)
        {
            RemapTexture(typed.MaterialsSheen.SheenColorTexture, remap);
            RemapTexture(typed.MaterialsSheen.SheenRoughnessTexture, remap);
        }
        else if (typed.MaterialsSpecular != null)
        {
            RemapTexture(typed.MaterialsSpecular.SpecularTexture, remap);
            RemapTexture(typed.MaterialsSpecular.SpecularColorTexture, remap);
        }
        else if (typed.MaterialsTransmission != null)
        {
            RemapTexture(typed.MaterialsTransmission.TransmissionTexture, remap);
        }
        else if (typed.MaterialsVolume != null)
        {
            RemapTexture(typed.MaterialsVolume.ThicknessTexture, remap);
        }
        else if (typed.MaterialsVariantsPrimitive != null)
        {
            int index = 0;
            while (typed.MaterialsVariantsPrimitive.Mappings != null
                && index < typed.MaterialsVariantsPrimitive.Mappings.Count)
            {
                GltfMaterialVariantMapping mapping =
                    typed.MaterialsVariantsPrimitive.Mappings[index];
                if (mapping != null && remap.Materials.ContainsKey(mapping.Material))
                {
                    mapping.Material = remap.Materials[mapping.Material];
                }

                int variantIndex = 0;
                while (mapping != null
                    && mapping.Variants != null
                    && variantIndex < mapping.Variants.Count)
                {
                    int oldIndex = mapping.Variants[variantIndex];
                    if (remap.MaterialVariants.ContainsKey(oldIndex))
                    {
                        mapping.Variants[variantIndex] = remap.MaterialVariants[oldIndex];
                    }

                    variantIndex++;
                }

                index++;
            }
        }
    }

    internal void RemapTexture(GltfTextureInfo? texture, GltfIndexRemap remap)
    {
        if (texture != null && remap.Textures.ContainsKey(texture.Index))
        {
            texture.Index = remap.Textures[texture.Index];
        }
    }

    internal void RemapNormalTexture(
        GltfNormalTextureInfo? texture,
        GltfIndexRemap remap)
    {
        if (texture != null && remap.Textures.ContainsKey(texture.Index))
        {
            texture.Index = remap.Textures[texture.Index];
        }
    }

    internal void PopulateReferences(
        GltfExtensionContext context,
        GltfExtensionResult result,
        ExtensionRegistry registry,
        JsonNode json)
    {
        GltfExtensionContext referenceContext = CopyContext(context, json);
        GltfExtensionResult parsed = Reader.ReadMaterialExtension(referenceContext, registry);
        int index = 0;
        while (index < parsed.References.Count)
        {
            result.References.Add(parsed.References[index]);
            index++;
        }
    }

    internal void CopyDiagnostics(
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
