using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class MaterialExtensionJsonReader
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal SceneExtensionJsonReader Values = new SceneExtensionJsonReader();
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();

    internal bool HandlesMaterialExtension(string name)
    {
        return name == GltfExtensionNames.KhrMaterialsAnisotropy
            || name == GltfExtensionNames.KhrMaterialsClearcoat
            || name == GltfExtensionNames.KhrMaterialsDispersion
            || name == GltfExtensionNames.KhrMaterialsDiffuseTransmission
            || name == GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness
            || name == GltfExtensionNames.KhrMaterialsEmissiveStrength
            || name == GltfExtensionNames.KhrMaterialsIor
            || name == GltfExtensionNames.KhrMaterialsIridescence
            || name == GltfExtensionNames.KhrMaterialsSheen
            || name == GltfExtensionNames.KhrMaterialsSpecular
            || name == GltfExtensionNames.KhrMaterialsTransmission
            || name == GltfExtensionNames.KhrMaterialsUnlit
            || name == GltfExtensionNames.KhrMaterialsVariants
            || name == GltfExtensionNames.KhrMaterialsVolume;
    }

    internal GltfExtensionResult ReadMaterialExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result = Fallback.CreatePreservedResult(context, registry);
        JsonObject? source = ReadObject(context.Json, result, Fallback.CreateExtensionPointer(context));
        if (source == null)
        {
            return result;
        }

        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (context.Name == GltfExtensionNames.KhrMaterialsAnisotropy)
        {
            typed.MaterialsAnisotropy = ReadAnisotropy(source, context, result, registry);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsClearcoat)
        {
            typed.MaterialsClearcoat = ReadClearcoat(source, context, result, registry);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsDispersion)
        {
            typed.MaterialsDispersion = ReadDispersion(source, result, pointer);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsDiffuseTransmission)
        {
            typed.MaterialsDiffuseTransmission = ReadDiffuseTransmission(
                source,
                context,
                result,
                registry);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness)
        {
            typed.MaterialsPbrSpecularGlossiness = ReadPbrSpecularGlossiness(
                source,
                context,
                result,
                registry);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsEmissiveStrength)
        {
            typed.MaterialsEmissiveStrength = ReadEmissiveStrength(source, result, pointer);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsIor)
        {
            typed.MaterialsIor = ReadIor(source, result, pointer);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsIridescence)
        {
            typed.MaterialsIridescence = ReadIridescence(source, context, result, registry);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsSheen)
        {
            typed.MaterialsSheen = ReadSheen(source, context, result, registry);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsSpecular)
        {
            typed.MaterialsSpecular = ReadSpecular(source, context, result, registry);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsTransmission)
        {
            typed.MaterialsTransmission = ReadTransmission(source, context, result, registry);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsUnlit)
        {
            typed.MaterialsUnlit = ReadUnlit(source);
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsVariants)
        {
            if (context.ObjectLocation == "mesh.primitive")
            {
                typed.MaterialsVariantsPrimitive = ReadPrimitiveVariants(
                    source,
                    context,
                    result);
            }
            else
            {
                typed.MaterialsVariants = ReadRootVariants(source, result, pointer);
            }
        }
        else if (context.Name == GltfExtensionNames.KhrMaterialsVolume)
        {
            typed.MaterialsVolume = ReadVolume(source, context, result, registry);
        }

        result.TypedData = typed;
        return result;
    }

    internal GltfMaterialsAnisotropyExtension ReadAnisotropy(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result,
        ExtensionRegistry registry)
    {
        GltfMaterialsAnisotropyExtension value = new GltfMaterialsAnisotropyExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (source.ContainsKey("anisotropyStrength"))
        {
            value.HasAnisotropyStrength = true;
            value.AnisotropyStrength = Values.ReadNumberValue(
                source["anisotropyStrength"],
                result,
                pointer + "/anisotropyStrength");
        }

        if (source.ContainsKey("anisotropyRotation"))
        {
            value.HasAnisotropyRotation = true;
            value.AnisotropyRotation = Values.ReadNumberValue(
                source["anisotropyRotation"],
                result,
                pointer + "/anisotropyRotation");
        }

        if (source.ContainsKey("anisotropyTexture"))
        {
            value.AnisotropyTexture = ReadTextureInfo(
                source["anisotropyTexture"],
                context,
                result,
                pointer + "/anisotropyTexture",
                "anisotropyTexture",
                registry);
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["anisotropyStrength", "anisotropyRotation", "anisotropyTexture"]);
        return value;
    }

    internal GltfMaterialsClearcoatExtension ReadClearcoat(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result,
        ExtensionRegistry registry)
    {
        GltfMaterialsClearcoatExtension value = new GltfMaterialsClearcoatExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (source.ContainsKey("clearcoatFactor"))
        {
            value.HasClearcoatFactor = true;
            value.ClearcoatFactor = Values.ReadNumberValue(
                source["clearcoatFactor"], result, pointer + "/clearcoatFactor");
        }

        if (source.ContainsKey("clearcoatTexture"))
        {
            value.ClearcoatTexture = ReadTextureInfo(
                source["clearcoatTexture"], context, result,
                pointer + "/clearcoatTexture", "clearcoatTexture", registry);
        }

        if (source.ContainsKey("clearcoatRoughnessFactor"))
        {
            value.HasClearcoatRoughnessFactor = true;
            value.ClearcoatRoughnessFactor = Values.ReadNumberValue(
                source["clearcoatRoughnessFactor"],
                result,
                pointer + "/clearcoatRoughnessFactor");
        }

        if (source.ContainsKey("clearcoatRoughnessTexture"))
        {
            value.ClearcoatRoughnessTexture = ReadTextureInfo(
                source["clearcoatRoughnessTexture"], context, result,
                pointer + "/clearcoatRoughnessTexture", "clearcoatRoughnessTexture", registry);
        }

        if (source.ContainsKey("clearcoatNormalTexture"))
        {
            value.ClearcoatNormalTexture = ReadNormalTextureInfo(
                source["clearcoatNormalTexture"], context, result,
                pointer + "/clearcoatNormalTexture", "clearcoatNormalTexture", registry);
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["clearcoatFactor", "clearcoatTexture", "clearcoatRoughnessFactor",
                "clearcoatRoughnessTexture", "clearcoatNormalTexture"]);
        return value;
    }

    internal GltfMaterialsDispersionExtension ReadDispersion(
        JsonObject source,
        GltfExtensionResult result,
        string pointer)
    {
        GltfMaterialsDispersionExtension value = new GltfMaterialsDispersionExtension();
        if (source.ContainsKey("dispersion"))
        {
            value.HasDispersion = true;
            value.Dispersion = Values.ReadNumberValue(
                source["dispersion"], result, pointer + "/dispersion");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["dispersion"]);
        return value;
    }

    internal GltfMaterialsEmissiveStrengthExtension ReadEmissiveStrength(
        JsonObject source,
        GltfExtensionResult result,
        string pointer)
    {
        GltfMaterialsEmissiveStrengthExtension value =
            new GltfMaterialsEmissiveStrengthExtension();
        if (source.ContainsKey("emissiveStrength"))
        {
            value.HasEmissiveStrength = true;
            value.EmissiveStrength = Values.ReadNumberValue(
                source["emissiveStrength"], result, pointer + "/emissiveStrength");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["emissiveStrength"]);
        return value;
    }

    internal GltfMaterialsDiffuseTransmissionExtension ReadDiffuseTransmission(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result,
        ExtensionRegistry registry)
    {
        GltfMaterialsDiffuseTransmissionExtension value =
            new GltfMaterialsDiffuseTransmissionExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (source.ContainsKey("diffuseTransmissionFactor"))
        {
            value.HasDiffuseTransmissionFactor = true;
            value.DiffuseTransmissionFactor = Values.ReadNumberValue(
                source["diffuseTransmissionFactor"],
                result,
                pointer + "/diffuseTransmissionFactor");
        }

        if (source.ContainsKey("diffuseTransmissionTexture"))
        {
            value.DiffuseTransmissionTexture = ReadTextureInfo(
                source["diffuseTransmissionTexture"],
                context,
                result,
                pointer + "/diffuseTransmissionTexture",
                "diffuseTransmissionTexture",
                registry);
        }

        if (source.ContainsKey("diffuseTransmissionColorFactor"))
        {
            value.HasDiffuseTransmissionColorFactor = true;
            value.DiffuseTransmissionColorFactor = Values.ReadNumberArray(
                source["diffuseTransmissionColorFactor"],
                3,
                result,
                pointer + "/diffuseTransmissionColorFactor");
        }

        if (source.ContainsKey("diffuseTransmissionColorTexture"))
        {
            value.DiffuseTransmissionColorTexture = ReadTextureInfo(
                source["diffuseTransmissionColorTexture"],
                context,
                result,
                pointer + "/diffuseTransmissionColorTexture",
                "diffuseTransmissionColorTexture",
                registry);
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["diffuseTransmissionFactor", "diffuseTransmissionTexture",
                "diffuseTransmissionColorFactor", "diffuseTransmissionColorTexture"]);
        return value;
    }

    internal GltfMaterialsIorExtension ReadIor(
        JsonObject source,
        GltfExtensionResult result,
        string pointer)
    {
        GltfMaterialsIorExtension value = new GltfMaterialsIorExtension();
        if (source.ContainsKey("ior"))
        {
            value.HasIor = true;
            value.Ior = Values.ReadNumberValue(source["ior"], result, pointer + "/ior");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["ior"]);
        return value;
    }

    internal GltfMaterialsPbrSpecularGlossinessExtension ReadPbrSpecularGlossiness(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result,
        ExtensionRegistry registry)
    {
        GltfMaterialsPbrSpecularGlossinessExtension value =
            new GltfMaterialsPbrSpecularGlossinessExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (source.ContainsKey("diffuseFactor"))
        {
            value.HasDiffuseFactor = true;
            value.DiffuseFactor = Values.ReadNumberArray(
                source["diffuseFactor"],
                4,
                result,
                pointer + "/diffuseFactor");
        }

        if (source.ContainsKey("diffuseTexture"))
        {
            value.DiffuseTexture = ReadTextureInfo(
                source["diffuseTexture"],
                context,
                result,
                pointer + "/diffuseTexture",
                "diffuseTexture",
                registry);
        }

        if (source.ContainsKey("specularFactor"))
        {
            value.HasSpecularFactor = true;
            value.SpecularFactor = Values.ReadNumberArray(
                source["specularFactor"],
                3,
                result,
                pointer + "/specularFactor");
        }

        if (source.ContainsKey("glossinessFactor"))
        {
            value.HasGlossinessFactor = true;
            value.GlossinessFactor = Values.ReadNumberValue(
                source["glossinessFactor"],
                result,
                pointer + "/glossinessFactor");
        }

        if (source.ContainsKey("specularGlossinessTexture"))
        {
            value.SpecularGlossinessTexture = ReadTextureInfo(
                source["specularGlossinessTexture"],
                context,
                result,
                pointer + "/specularGlossinessTexture",
                "specularGlossinessTexture",
                registry);
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["diffuseFactor", "diffuseTexture", "specularFactor", "glossinessFactor",
                "specularGlossinessTexture"]);
        return value;
    }

    internal GltfMaterialsIridescenceExtension ReadIridescence(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result,
        ExtensionRegistry registry)
    {
        GltfMaterialsIridescenceExtension value = new GltfMaterialsIridescenceExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (source.ContainsKey("iridescenceFactor"))
        {
            value.HasIridescenceFactor = true;
            value.IridescenceFactor = Values.ReadNumberValue(
                source["iridescenceFactor"], result, pointer + "/iridescenceFactor");
        }

        if (source.ContainsKey("iridescenceTexture"))
        {
            value.IridescenceTexture = ReadTextureInfo(
                source["iridescenceTexture"], context, result,
                pointer + "/iridescenceTexture", "iridescenceTexture", registry);
        }

        if (source.ContainsKey("iridescenceIor"))
        {
            value.HasIridescenceIor = true;
            value.IridescenceIor = Values.ReadNumberValue(
                source["iridescenceIor"], result, pointer + "/iridescenceIor");
        }

        if (source.ContainsKey("iridescenceThicknessMinimum"))
        {
            value.HasIridescenceThicknessMinimum = true;
            value.IridescenceThicknessMinimum = Values.ReadNumberValue(
                source["iridescenceThicknessMinimum"],
                result,
                pointer + "/iridescenceThicknessMinimum");
        }

        if (source.ContainsKey("iridescenceThicknessMaximum"))
        {
            value.HasIridescenceThicknessMaximum = true;
            value.IridescenceThicknessMaximum = Values.ReadNumberValue(
                source["iridescenceThicknessMaximum"],
                result,
                pointer + "/iridescenceThicknessMaximum");
        }

        if (source.ContainsKey("iridescenceThicknessTexture"))
        {
            value.IridescenceThicknessTexture = ReadTextureInfo(
                source["iridescenceThicknessTexture"], context, result,
                pointer + "/iridescenceThicknessTexture", "iridescenceThicknessTexture", registry);
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["iridescenceFactor", "iridescenceTexture", "iridescenceIor",
                "iridescenceThicknessMinimum", "iridescenceThicknessMaximum",
                "iridescenceThicknessTexture"]);
        return value;
    }

    internal GltfMaterialsSheenExtension ReadSheen(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result,
        ExtensionRegistry registry)
    {
        GltfMaterialsSheenExtension value = new GltfMaterialsSheenExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (source.ContainsKey("sheenColorFactor"))
        {
            value.HasSheenColorFactor = true;
            value.SheenColorFactor = Values.ReadNumberArray(
                source["sheenColorFactor"], 3, result, pointer + "/sheenColorFactor");
        }

        if (source.ContainsKey("sheenColorTexture"))
        {
            value.SheenColorTexture = ReadTextureInfo(
                source["sheenColorTexture"], context, result,
                pointer + "/sheenColorTexture", "sheenColorTexture", registry);
        }

        if (source.ContainsKey("sheenRoughnessFactor"))
        {
            value.HasSheenRoughnessFactor = true;
            value.SheenRoughnessFactor = Values.ReadNumberValue(
                source["sheenRoughnessFactor"], result, pointer + "/sheenRoughnessFactor");
        }

        if (source.ContainsKey("sheenRoughnessTexture"))
        {
            value.SheenRoughnessTexture = ReadTextureInfo(
                source["sheenRoughnessTexture"], context, result,
                pointer + "/sheenRoughnessTexture", "sheenRoughnessTexture", registry);
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["sheenColorFactor", "sheenColorTexture", "sheenRoughnessFactor",
                "sheenRoughnessTexture"]);
        return value;
    }

    internal GltfMaterialsSpecularExtension ReadSpecular(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result,
        ExtensionRegistry registry)
    {
        GltfMaterialsSpecularExtension value = new GltfMaterialsSpecularExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (source.ContainsKey("specularFactor"))
        {
            value.HasSpecularFactor = true;
            value.SpecularFactor = Values.ReadNumberValue(
                source["specularFactor"], result, pointer + "/specularFactor");
        }

        if (source.ContainsKey("specularTexture"))
        {
            value.SpecularTexture = ReadTextureInfo(
                source["specularTexture"], context, result,
                pointer + "/specularTexture", "specularTexture", registry);
        }

        if (source.ContainsKey("specularColorFactor"))
        {
            value.HasSpecularColorFactor = true;
            value.SpecularColorFactor = Values.ReadNumberArray(
                source["specularColorFactor"], 3, result, pointer + "/specularColorFactor");
        }

        if (source.ContainsKey("specularColorTexture"))
        {
            value.SpecularColorTexture = ReadTextureInfo(
                source["specularColorTexture"], context, result,
                pointer + "/specularColorTexture", "specularColorTexture", registry);
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["specularFactor", "specularTexture", "specularColorFactor",
                "specularColorTexture"]);
        return value;
    }

    internal GltfMaterialsTransmissionExtension ReadTransmission(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result,
        ExtensionRegistry registry)
    {
        GltfMaterialsTransmissionExtension value = new GltfMaterialsTransmissionExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (source.ContainsKey("transmissionFactor"))
        {
            value.HasTransmissionFactor = true;
            value.TransmissionFactor = Values.ReadNumberValue(
                source["transmissionFactor"], result, pointer + "/transmissionFactor");
        }

        if (source.ContainsKey("transmissionTexture"))
        {
            value.TransmissionTexture = ReadTextureInfo(
                source["transmissionTexture"], context, result,
                pointer + "/transmissionTexture", "transmissionTexture", registry);
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["transmissionFactor", "transmissionTexture"]);
        return value;
    }

    internal GltfMaterialsUnlitExtension ReadUnlit(JsonObject source)
    {
        GltfMaterialsUnlitExtension value = new GltfMaterialsUnlitExtension();
        value.PropertyJson = PropertyJson.ReadPropertyData(source, []);
        return value;
    }

    internal GltfMaterialsVolumeExtension ReadVolume(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result,
        ExtensionRegistry registry)
    {
        GltfMaterialsVolumeExtension value = new GltfMaterialsVolumeExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        if (source.ContainsKey("thicknessFactor"))
        {
            value.HasThicknessFactor = true;
            value.ThicknessFactor = Values.ReadNumberValue(
                source["thicknessFactor"], result, pointer + "/thicknessFactor");
        }

        if (source.ContainsKey("thicknessTexture"))
        {
            value.ThicknessTexture = ReadTextureInfo(
                source["thicknessTexture"], context, result,
                pointer + "/thicknessTexture", "thicknessTexture", registry);
        }

        if (source.ContainsKey("attenuationDistance"))
        {
            value.HasAttenuationDistance = true;
            value.AttenuationDistance = Values.ReadNumberValue(
                source["attenuationDistance"], result, pointer + "/attenuationDistance");
        }

        if (source.ContainsKey("attenuationColor"))
        {
            value.HasAttenuationColor = true;
            value.AttenuationColor = Values.ReadNumberArray(
                source["attenuationColor"], 3, result, pointer + "/attenuationColor");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["thicknessFactor", "thicknessTexture", "attenuationDistance", "attenuationColor"]);
        return value;
    }

    internal GltfMaterialsVariantsExtension ReadRootVariants(
        JsonObject source,
        GltfExtensionResult result,
        string pointer)
    {
        GltfMaterialsVariantsExtension value = new GltfMaterialsVariantsExtension();
        JsonArray? variants = ReadRequiredArray(
            source,
            "variants",
            result,
            pointer + "/variants");
        if (variants != null)
        {
            int index = 0;
            while (index < variants.Count)
            {
                string itemPointer = pointer + "/variants/" + index;
                JsonObject? item = ReadObject(variants[index], result, itemPointer);
                if (item != null)
                {
                    GltfMaterialVariant variant = new GltfMaterialVariant();
                    JsonNode? name = item["name"];
                    if (!item.ContainsKey("name")
                        || name == null
                        || name.GetValueKind() != JsonValueKind.String)
                    {
                        Values.AddReadIssue(
                            result,
                            GltfIssueCodes.TypeMismatch,
                            "A material variant must contain a string name.",
                            itemPointer + "/name");
                    }
                    else
                    {
                        variant.Name = name.GetValue<string>();
                    }

                    variant.PropertyJson = PropertyJson.ReadPropertyData(item, ["name"]);
                    value.Variants.Add(variant);
                }

                index++;
            }
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["variants"]);
        return value;
    }

    internal GltfMaterialsVariantsPrimitiveExtension ReadPrimitiveVariants(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        GltfMaterialsVariantsPrimitiveExtension value =
            new GltfMaterialsVariantsPrimitiveExtension();
        string pointer = Fallback.CreateExtensionPointer(context);
        JsonArray? mappings = ReadRequiredArray(
            source,
            "mappings",
            result,
            pointer + "/mappings");
        if (mappings != null)
        {
            int index = 0;
            while (index < mappings.Count)
            {
                string itemPointer = pointer + "/mappings/" + index;
                JsonObject? item = ReadObject(mappings[index], result, itemPointer);
                if (item != null)
                {
                    value.Mappings.Add(ReadVariantMapping(
                        item,
                        context,
                        result,
                        itemPointer,
                        index));
                }

                index++;
            }
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["mappings"]);
        return value;
    }

    internal GltfMaterialVariantMapping ReadVariantMapping(
        JsonObject source,
        GltfExtensionContext context,
        GltfExtensionResult result,
        string pointer,
        int mappingIndex)
    {
        GltfMaterialVariantMapping value = new GltfMaterialVariantMapping();
        JsonArray? variants = ReadRequiredArray(
            source,
            "variants",
            result,
            pointer + "/variants");
        if (variants != null)
        {
            int index = 0;
            while (index < variants.Count)
            {
                int variant = Values.ReadNonNegativeInteger(
                    variants[index], result, pointer + "/variants/" + index);
                value.Variants.Add(variant);
                AddVariantReference(context, result, variant, mappingIndex, index);
                index++;
            }
        }

        if (!source.ContainsKey("material"))
        {
            Values.AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "A variant mapping must contain a material index.",
                pointer + "/material");
        }
        else
        {
            value.Material = Values.ReadNonNegativeInteger(
                source["material"], result, pointer + "/material");
            AddMaterialReference(context, result, value.Material, mappingIndex);
        }

        if (source.ContainsKey("name"))
        {
            JsonNode? name = source["name"];
            if (name == null || name.GetValueKind() != JsonValueKind.String)
            {
                Values.AddReadIssue(
                    result,
                    GltfIssueCodes.TypeMismatch,
                    "A variant mapping name must be a string.",
                    pointer + "/name");
            }
            else
            {
                value.Name = name.GetValue<string>();
            }
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["variants", "material", "name"]);
        return value;
    }

    internal GltfTextureInfo ReadTextureInfo(
        JsonNode? node,
        GltfExtensionContext context,
        GltfExtensionResult result,
        string pointer,
        string memberName,
        ExtensionRegistry registry)
    {
        GltfTextureInfo value = new GltfTextureInfo();
        JsonObject? source = ReadObject(node, result, pointer);
        if (source == null)
        {
            return value;
        }

        if (!source.ContainsKey("index"))
        {
            Values.AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "Texture info must contain a texture index.",
                pointer + "/index");
        }
        else
        {
            value.Index = Values.ReadNonNegativeInteger(
                source["index"], result, pointer + "/index");
            AddTextureReference(context, result, value.Index, memberName);
        }

        if (source.ContainsKey("texCoord"))
        {
            value.HasTextureCoordinate = true;
            value.TextureCoordinate = Values.ReadNonNegativeInteger(
                source["texCoord"], result, pointer + "/texCoord");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(source, ["index", "texCoord"]);
        ReadTextureTransform(value.PropertyJson, context, result, registry);
        return value;
    }

    internal GltfNormalTextureInfo ReadNormalTextureInfo(
        JsonNode? node,
        GltfExtensionContext context,
        GltfExtensionResult result,
        string pointer,
        string memberName,
        ExtensionRegistry registry)
    {
        GltfNormalTextureInfo value = new GltfNormalTextureInfo();
        JsonObject? source = ReadObject(node, result, pointer);
        if (source == null)
        {
            return value;
        }

        if (!source.ContainsKey("index"))
        {
            Values.AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "Normal texture info must contain a texture index.",
                pointer + "/index");
        }
        else
        {
            value.Index = Values.ReadNonNegativeInteger(
                source["index"], result, pointer + "/index");
            AddTextureReference(context, result, value.Index, memberName);
        }

        if (source.ContainsKey("texCoord"))
        {
            value.HasTextureCoordinate = true;
            value.TextureCoordinate = Values.ReadNonNegativeInteger(
                source["texCoord"], result, pointer + "/texCoord");
        }

        if (source.ContainsKey("scale"))
        {
            value.HasScale = true;
            value.Scale = Values.ReadNumberValue(source["scale"], result, pointer + "/scale");
        }

        value.PropertyJson = PropertyJson.ReadPropertyData(
            source,
            ["index", "texCoord", "scale"]);
        ReadTextureTransform(value.PropertyJson, context, result, registry);
        return value;
    }

    internal void ReadTextureTransform(
        GltfPropertyJson propertyJson,
        GltfExtensionContext parentContext,
        GltfExtensionResult result,
        ExtensionRegistry registry)
    {
        if (!propertyJson.Extensions.ContainsKey(GltfExtensionNames.KhrTextureTransform))
        {
            return;
        }

        GltfExtensionContext context = new GltfExtensionContext();
        context.Document = parentContext.Document;
        context.Name = GltfExtensionNames.KhrTextureTransform;
        context.ObjectLocation = "textureInfo";
        context.Json = propertyJson.Extensions[GltfExtensionNames.KhrTextureTransform];
        context.Resources = parentContext.Resources;
        GltfExtensionResult parsed = Values.ReadSceneExtension(context, registry);
        if (parsed.TypedData != null)
        {
            propertyJson.TypedExtensions[GltfExtensionNames.KhrTextureTransform] = parsed.TypedData;
        }

        CopyDiagnostics(parsed.Diagnostics, result.Diagnostics);
        if ((int)parsed.Status > (int)result.Status)
        {
            result.Status = parsed.Status;
        }
    }

    internal JsonObject? ReadObject(
        JsonNode? node,
        GltfExtensionResult result,
        string pointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Object)
        {
            Values.AddReadIssue(
                result,
                GltfIssueCodes.TypeMismatch,
                "Expected a JSON object.",
                pointer);
            return null;
        }

        return node.AsObject();
    }

    internal JsonArray? ReadRequiredArray(
        JsonObject source,
        string name,
        GltfExtensionResult result,
        string pointer)
    {
        JsonNode? node = source[name];
        if (!source.ContainsKey(name)
            || node == null
            || node.GetValueKind() != JsonValueKind.Array)
        {
            Values.AddReadIssue(
                result,
                GltfIssueCodes.ArrayTypeMismatch,
                $"The required '{name}' value must be an array.",
                pointer);
            return null;
        }

        JsonArray value = node.AsArray();
        if (value.Count == 0)
        {
            Values.AddReadIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                $"The required '{name}' array must not be empty.",
                pointer);
        }

        return value;
    }

    internal void AddTextureReference(
        GltfExtensionContext context,
        GltfExtensionResult result,
        int texture,
        string memberName)
    {
        if (texture < 0)
        {
            return;
        }

        GltfDependency reference = new GltfDependency();
        reference.SourceCollection = "materials";
        reference.SourceIndex = context.ObjectIndex;
        reference.SourceMember = "extensions/" + context.Name + "/" + memberName + "/index";
        reference.TargetCollection = "textures";
        reference.TargetIndex = texture;
        result.References.Add(reference);
    }

    internal void AddMaterialReference(
        GltfExtensionContext context,
        GltfExtensionResult result,
        int material,
        int mappingIndex)
    {
        if (material < 0)
        {
            return;
        }

        GltfDependency reference = new GltfDependency();
        reference.SourceCollection = "meshes";
        reference.SourceIndex = context.ObjectIndex;
        reference.SourceMember = "extensions/KHR_materials_variants/mappings/"
            + mappingIndex + "/material";
        reference.TargetCollection = "materials";
        reference.TargetIndex = material;
        result.References.Add(reference);
    }

    internal void AddVariantReference(
        GltfExtensionContext context,
        GltfExtensionResult result,
        int variant,
        int mappingIndex,
        int variantIndex)
    {
        if (variant < 0)
        {
            return;
        }

        GltfDependency reference = new GltfDependency();
        reference.SourceCollection = "meshes";
        reference.SourceIndex = context.ObjectIndex;
        reference.SourceMember = "extensions/KHR_materials_variants/mappings/"
            + mappingIndex + "/variants/" + variantIndex;
        reference.TargetCollection = "extensions/KHR_materials_variants/variants";
        reference.TargetIndex = variant;
        result.References.Add(reference);
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
