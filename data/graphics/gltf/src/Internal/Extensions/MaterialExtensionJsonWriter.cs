using System.Text.Json.Nodes;

namespace gltf;

internal class MaterialExtensionJsonWriter
{
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();
    internal SceneExtensionJsonWriter SceneWriter = new SceneExtensionJsonWriter();

    internal JsonNode? WriteMaterialExtension(
        GltfExtensionContext context,
        GltfTypedExtensionData typed)
    {
        if (context.Name == GltfExtensionNames.KhrMaterialsAnisotropy
            && typed.MaterialsAnisotropy != null)
        {
            return WriteAnisotropy(typed.MaterialsAnisotropy);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsClearcoat
            && typed.MaterialsClearcoat != null)
        {
            return WriteClearcoat(typed.MaterialsClearcoat);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsDispersion
            && typed.MaterialsDispersion != null)
        {
            return WriteDispersion(typed.MaterialsDispersion);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsDiffuseTransmission
            && typed.MaterialsDiffuseTransmission != null)
        {
            return WriteDiffuseTransmission(typed.MaterialsDiffuseTransmission);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsEmissiveStrength
            && typed.MaterialsEmissiveStrength != null)
        {
            return WriteEmissiveStrength(typed.MaterialsEmissiveStrength);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness
            && typed.MaterialsPbrSpecularGlossiness != null)
        {
            return WritePbrSpecularGlossiness(typed.MaterialsPbrSpecularGlossiness);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsIor
            && typed.MaterialsIor != null)
        {
            return WriteIor(typed.MaterialsIor);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsIridescence
            && typed.MaterialsIridescence != null)
        {
            return WriteIridescence(typed.MaterialsIridescence);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsSheen
            && typed.MaterialsSheen != null)
        {
            return WriteSheen(typed.MaterialsSheen);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsSpecular
            && typed.MaterialsSpecular != null)
        {
            return WriteSpecular(typed.MaterialsSpecular);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsTransmission
            && typed.MaterialsTransmission != null)
        {
            return WriteTransmission(typed.MaterialsTransmission);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsUnlit
            && typed.MaterialsUnlit != null)
        {
            return WriteUnlit(typed.MaterialsUnlit);
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsVariants)
        {
            if (context.ObjectLocation == "mesh.primitive"
                && typed.MaterialsVariantsPrimitive != null)
            {
                return WritePrimitiveVariants(typed.MaterialsVariantsPrimitive);
            }

            if (typed.MaterialsVariants != null)
            {
                return WriteRootVariants(typed.MaterialsVariants);
            }
        }

        if (context.Name == GltfExtensionNames.KhrMaterialsVolume
            && typed.MaterialsVolume != null)
        {
            return WriteVolume(typed.MaterialsVolume);
        }

        return null;
    }

    internal JsonObject WriteAnisotropy(GltfMaterialsAnisotropyExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasAnisotropyStrength)
        {
            result["anisotropyStrength"] = JsonValue.Create(source.AnisotropyStrength);
        }

        if (source.HasAnisotropyRotation)
        {
            result["anisotropyRotation"] = JsonValue.Create(source.AnisotropyRotation);
        }

        if (source.AnisotropyTexture != null)
        {
            result["anisotropyTexture"] = WriteTextureInfo(source.AnisotropyTexture);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteClearcoat(GltfMaterialsClearcoatExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasClearcoatFactor)
        {
            result["clearcoatFactor"] = JsonValue.Create(source.ClearcoatFactor);
        }

        if (source.ClearcoatTexture != null)
        {
            result["clearcoatTexture"] = WriteTextureInfo(source.ClearcoatTexture);
        }

        if (source.HasClearcoatRoughnessFactor)
        {
            result["clearcoatRoughnessFactor"] = JsonValue.Create(
                source.ClearcoatRoughnessFactor);
        }

        if (source.ClearcoatRoughnessTexture != null)
        {
            result["clearcoatRoughnessTexture"] = WriteTextureInfo(
                source.ClearcoatRoughnessTexture);
        }

        if (source.ClearcoatNormalTexture != null)
        {
            result["clearcoatNormalTexture"] = WriteNormalTextureInfo(
                source.ClearcoatNormalTexture);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteDispersion(GltfMaterialsDispersionExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasDispersion)
        {
            result["dispersion"] = JsonValue.Create(source.Dispersion);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteEmissiveStrength(GltfMaterialsEmissiveStrengthExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasEmissiveStrength)
        {
            result["emissiveStrength"] = JsonValue.Create(source.EmissiveStrength);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteDiffuseTransmission(
        GltfMaterialsDiffuseTransmissionExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasDiffuseTransmissionFactor)
        {
            result["diffuseTransmissionFactor"] = JsonValue.Create(
                source.DiffuseTransmissionFactor);
        }

        if (source.DiffuseTransmissionTexture != null)
        {
            result["diffuseTransmissionTexture"] = WriteTextureInfo(
                source.DiffuseTransmissionTexture);
        }

        if (source.HasDiffuseTransmissionColorFactor)
        {
            result["diffuseTransmissionColorFactor"] = WriteDoubleArray(
                source.DiffuseTransmissionColorFactor);
        }

        if (source.DiffuseTransmissionColorTexture != null)
        {
            result["diffuseTransmissionColorTexture"] = WriteTextureInfo(
                source.DiffuseTransmissionColorTexture);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteIor(GltfMaterialsIorExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasIor)
        {
            result["ior"] = JsonValue.Create(source.Ior);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WritePbrSpecularGlossiness(
        GltfMaterialsPbrSpecularGlossinessExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasDiffuseFactor)
        {
            result["diffuseFactor"] = WriteDoubleArray(source.DiffuseFactor);
        }

        if (source.DiffuseTexture != null)
        {
            result["diffuseTexture"] = WriteTextureInfo(source.DiffuseTexture);
        }

        if (source.HasSpecularFactor)
        {
            result["specularFactor"] = WriteDoubleArray(source.SpecularFactor);
        }

        if (source.HasGlossinessFactor)
        {
            result["glossinessFactor"] = JsonValue.Create(source.GlossinessFactor);
        }

        if (source.SpecularGlossinessTexture != null)
        {
            result["specularGlossinessTexture"] = WriteTextureInfo(
                source.SpecularGlossinessTexture);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteIridescence(GltfMaterialsIridescenceExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasIridescenceFactor)
        {
            result["iridescenceFactor"] = JsonValue.Create(source.IridescenceFactor);
        }

        if (source.IridescenceTexture != null)
        {
            result["iridescenceTexture"] = WriteTextureInfo(source.IridescenceTexture);
        }

        if (source.HasIridescenceIor)
        {
            result["iridescenceIor"] = JsonValue.Create(source.IridescenceIor);
        }

        if (source.HasIridescenceThicknessMinimum)
        {
            result["iridescenceThicknessMinimum"] = JsonValue.Create(
                source.IridescenceThicknessMinimum);
        }

        if (source.HasIridescenceThicknessMaximum)
        {
            result["iridescenceThicknessMaximum"] = JsonValue.Create(
                source.IridescenceThicknessMaximum);
        }

        if (source.IridescenceThicknessTexture != null)
        {
            result["iridescenceThicknessTexture"] = WriteTextureInfo(
                source.IridescenceThicknessTexture);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteSheen(GltfMaterialsSheenExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasSheenColorFactor)
        {
            result["sheenColorFactor"] = WriteDoubleArray(source.SheenColorFactor);
        }

        if (source.SheenColorTexture != null)
        {
            result["sheenColorTexture"] = WriteTextureInfo(source.SheenColorTexture);
        }

        if (source.HasSheenRoughnessFactor)
        {
            result["sheenRoughnessFactor"] = JsonValue.Create(source.SheenRoughnessFactor);
        }

        if (source.SheenRoughnessTexture != null)
        {
            result["sheenRoughnessTexture"] = WriteTextureInfo(
                source.SheenRoughnessTexture);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteSpecular(GltfMaterialsSpecularExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasSpecularFactor)
        {
            result["specularFactor"] = JsonValue.Create(source.SpecularFactor);
        }

        if (source.SpecularTexture != null)
        {
            result["specularTexture"] = WriteTextureInfo(source.SpecularTexture);
        }

        if (source.HasSpecularColorFactor)
        {
            result["specularColorFactor"] = WriteDoubleArray(source.SpecularColorFactor);
        }

        if (source.SpecularColorTexture != null)
        {
            result["specularColorTexture"] = WriteTextureInfo(
                source.SpecularColorTexture);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteTransmission(GltfMaterialsTransmissionExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasTransmissionFactor)
        {
            result["transmissionFactor"] = JsonValue.Create(source.TransmissionFactor);
        }

        if (source.TransmissionTexture != null)
        {
            result["transmissionTexture"] = WriteTextureInfo(
                source.TransmissionTexture);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteUnlit(GltfMaterialsUnlitExtension source)
    {
        JsonObject result = new JsonObject();
        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteVolume(GltfMaterialsVolumeExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasThicknessFactor)
        {
            result["thicknessFactor"] = JsonValue.Create(source.ThicknessFactor);
        }

        if (source.ThicknessTexture != null)
        {
            result["thicknessTexture"] = WriteTextureInfo(source.ThicknessTexture);
        }

        if (source.HasAttenuationDistance)
        {
            result["attenuationDistance"] = JsonValue.Create(source.AttenuationDistance);
        }

        if (source.HasAttenuationColor)
        {
            result["attenuationColor"] = WriteDoubleArray(source.AttenuationColor);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteRootVariants(GltfMaterialsVariantsExtension source)
    {
        JsonObject result = new JsonObject();
        JsonArray variants = new JsonArray();
        int index = 0;
        while (source.Variants != null && index < source.Variants.Count)
        {
            GltfMaterialVariant variant = source.Variants[index];
            if (variant == null)
            {
                variants.Add(new JsonObject());
            }
            else
            {
                variants.Add(WriteVariant(variant));
            }

            index++;
        }

        result["variants"] = variants;
        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteVariant(GltfMaterialVariant source)
    {
        JsonObject result = new JsonObject();
        result["name"] = JsonValue.Create(source.Name);
        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WritePrimitiveVariants(
        GltfMaterialsVariantsPrimitiveExtension source)
    {
        JsonObject result = new JsonObject();
        JsonArray mappings = new JsonArray();
        int index = 0;
        while (source.Mappings != null && index < source.Mappings.Count)
        {
            GltfMaterialVariantMapping mapping = source.Mappings[index];
            if (mapping == null)
            {
                mappings.Add(new JsonObject());
            }
            else
            {
                mappings.Add(WriteVariantMapping(mapping));
            }

            index++;
        }

        result["mappings"] = mappings;
        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteVariantMapping(GltfMaterialVariantMapping source)
    {
        JsonObject result = new JsonObject();
        JsonArray variants = new JsonArray();
        int index = 0;
        while (source.Variants != null && index < source.Variants.Count)
        {
            variants.Add(JsonValue.Create(source.Variants[index]));
            index++;
        }

        result["variants"] = variants;
        result["material"] = JsonValue.Create(source.Material);
        if (source.Name != null)
        {
            result["name"] = JsonValue.Create(source.Name);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteTextureInfo(GltfTextureInfo source)
    {
        JsonObject result = new JsonObject();
        result["index"] = JsonValue.Create(source.Index);
        if (source.HasTextureCoordinate)
        {
            result["texCoord"] = JsonValue.Create(source.TextureCoordinate);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        AppendTypedTextureTransform(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteNormalTextureInfo(GltfNormalTextureInfo source)
    {
        JsonObject result = new JsonObject();
        result["index"] = JsonValue.Create(source.Index);
        if (source.HasTextureCoordinate)
        {
            result["texCoord"] = JsonValue.Create(source.TextureCoordinate);
        }

        if (source.HasScale)
        {
            result["scale"] = JsonValue.Create(source.Scale);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        AppendTypedTextureTransform(result, source.PropertyJson);
        return result;
    }

    internal void AppendTypedTextureTransform(
        JsonObject target,
        GltfPropertyJson propertyJson)
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

        JsonObject extensions;
        JsonNode? existing = target["extensions"];
        if (existing != null
            && existing.GetValueKind() == System.Text.Json.JsonValueKind.Object)
        {
            extensions = existing.AsObject();
        }
        else
        {
            extensions = new JsonObject();
            target["extensions"] = extensions;
        }

        extensions[GltfExtensionNames.KhrTextureTransform] =
            SceneWriter.WriteTextureTransform(typed.TextureTransform);
    }

    internal JsonArray WriteDoubleArray(double[]? source)
    {
        JsonArray result = new JsonArray();
        int index = 0;
        while (source != null && index < source.Length)
        {
            result.Add(JsonValue.Create(source[index]));
            index++;
        }

        return result;
    }
}
