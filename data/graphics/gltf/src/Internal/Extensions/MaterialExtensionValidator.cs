using System;
using System.Collections.Generic;

namespace gltf;

internal class MaterialExtensionValidator
{
    internal ExtensionFallback Fallback = new ExtensionFallback();
    internal MaterialExtensionJsonReader Reader = new MaterialExtensionJsonReader();

    internal GltfExtensionResult ValidateMaterialExtension(
        GltfExtensionContext context,
        ExtensionRegistry registry)
    {
        GltfExtensionResult result;
        GltfTypedExtensionData? typed;
        if (context.TypedData == null)
        {
            result = Reader.ReadMaterialExtension(context, registry);
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

        ValidateLocation(context, result);
        if (typed.MaterialsAnisotropy != null)
        {
            ValidateAnisotropy(context, result, typed.MaterialsAnisotropy);
        }
        else if (typed.MaterialsClearcoat != null)
        {
            ValidateClearcoat(context, result, typed.MaterialsClearcoat);
        }
        else if (typed.MaterialsDispersion != null)
        {
            ValidateDispersion(context, result, typed.MaterialsDispersion);
        }
        else if (typed.MaterialsDiffuseTransmission != null)
        {
            ValidateDiffuseTransmission(
                context,
                result,
                typed.MaterialsDiffuseTransmission);
        }
        else if (typed.MaterialsEmissiveStrength != null)
        {
            ValidateEmissiveStrength(context, result, typed.MaterialsEmissiveStrength);
        }
        else if (typed.MaterialsPbrSpecularGlossiness != null)
        {
            ValidatePbrSpecularGlossiness(
                context,
                result,
                typed.MaterialsPbrSpecularGlossiness);
        }
        else if (typed.MaterialsIor != null)
        {
            ValidateIor(context, result, typed.MaterialsIor);
        }
        else if (typed.MaterialsIridescence != null)
        {
            ValidateIridescence(context, result, typed.MaterialsIridescence);
        }
        else if (typed.MaterialsSheen != null)
        {
            ValidateSheen(context, result, typed.MaterialsSheen);
        }
        else if (typed.MaterialsSpecular != null)
        {
            ValidateSpecular(context, result, typed.MaterialsSpecular);
        }
        else if (typed.MaterialsTransmission != null)
        {
            ValidateTransmission(context, result, typed.MaterialsTransmission);
        }
        else if (typed.MaterialsVolume != null)
        {
            ValidateVolume(context, result, typed.MaterialsVolume);
        }
        else if (typed.MaterialsVariants != null)
        {
            ValidateRootVariants(context, result, typed.MaterialsVariants);
        }
        else if (typed.MaterialsVariantsPrimitive != null)
        {
            ValidatePrimitiveVariants(context, result, typed.MaterialsVariantsPrimitive);
        }

        return result;
    }

    internal void ValidateLocation(
        GltfExtensionContext context,
        GltfExtensionResult result)
    {
        bool valid;
        if (context.Name == GltfExtensionNames.KhrMaterialsVariants)
        {
            valid = context.ObjectLocation == "glTF"
                || context.ObjectLocation == "mesh.primitive";
        }
        else
        {
            valid = context.ObjectLocation == "material";
        }

        if (!valid)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnexpectedExtensionObject,
                GltfValidationSeverity.Error,
                $"Extension '{context.Name}' is not valid on {context.ObjectLocation}.",
                Fallback.CreateExtensionPointer(context));
        }
    }

    internal void ValidateAnisotropy(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsAnisotropyExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasAnisotropyStrength)
        {
            ValidateScalarRange(
                result, value.AnisotropyStrength, 0.0, true, 1.0,
                pointer + "/anisotropyStrength");
        }

        if (value.HasAnisotropyRotation
            && !FiniteNumber.IsFiniteDouble(value.AnisotropyRotation))
        {
            AddRangeIssue(result, pointer + "/anisotropyRotation");
        }

        ValidateTextureInfo(
            context, result, value.AnisotropyTexture, pointer + "/anisotropyTexture");
        GltfMaterial? material = GetContextMaterial(context);
        if (material != null
            && material.NormalTexture != null
            && value.AnisotropyTexture != null
            && GetNormalTextureCoordinate(material.NormalTexture)
                != GetTextureCoordinate(value.AnisotropyTexture))
        {
            AddIssue(
                result,
                GltfIssueCodes.KhrMaterialsAnisotropyAnisotropyTextureTexcoord,
                GltfValidationSeverity.Warning,
                "Normal and anisotropy textures should use the same texture coordinates.",
                pointer + "/anisotropyTexture/texCoord");
        }
    }

    internal void ValidateClearcoat(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsClearcoatExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasClearcoatFactor)
        {
            ValidateScalarRange(
                result, value.ClearcoatFactor, 0.0, true, 1.0,
                pointer + "/clearcoatFactor");
        }

        if (value.HasClearcoatRoughnessFactor)
        {
            ValidateScalarRange(
                result, value.ClearcoatRoughnessFactor, 0.0, true, 1.0,
                pointer + "/clearcoatRoughnessFactor");
        }

        ValidateTextureInfo(
            context, result, value.ClearcoatTexture, pointer + "/clearcoatTexture");
        ValidateTextureInfo(
            context, result, value.ClearcoatRoughnessTexture,
            pointer + "/clearcoatRoughnessTexture");
        ValidateNormalTextureInfo(
            context, result, value.ClearcoatNormalTexture,
            pointer + "/clearcoatNormalTexture");
        GltfMaterial? material = GetContextMaterial(context);
        if (material != null
            && material.NormalTexture != null
            && value.ClearcoatNormalTexture != null
            && GetNormalTextureCoordinate(material.NormalTexture)
                != GetNormalTextureCoordinate(value.ClearcoatNormalTexture))
        {
            AddIssue(
                result,
                GltfIssueCodes.KhrMaterialsClearcoatClearcoatNormalTextureTexcoord,
                GltfValidationSeverity.Warning,
                "Normal and clearcoat normal textures should use the same texture coordinates.",
                pointer + "/clearcoatNormalTexture/texCoord");
        }
    }

    internal void ValidateDispersion(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsDispersionExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasDispersion)
        {
            ValidateScalarRange(
                result, value.Dispersion, 0.0, false, 0.0,
                pointer + "/dispersion");
        }

        GltfMaterial? material = GetContextMaterial(context);
        if (material != null
            && !HasMaterialExtension(material, GltfExtensionNames.KhrMaterialsVolume))
        {
            AddIssue(
                result,
                GltfIssueCodes.KhrMaterialsDispersionNoVolume,
                GltfValidationSeverity.Warning,
                "The dispersion extension needs to be combined with the volume extension.",
                pointer);
        }
    }

    internal void ValidateEmissiveStrength(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsEmissiveStrengthExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasEmissiveStrength)
        {
            ValidateScalarRange(
                result, value.EmissiveStrength, 0.0, false, 0.0,
                pointer + "/emissiveStrength");
        }

        GltfMaterial? material = GetContextMaterial(context);
        if (material != null && IsEmissiveFactorZero(material))
        {
            AddIssue(
                result,
                GltfIssueCodes.KhrMaterialsEmissiveStrengthZeroFactor,
                GltfValidationSeverity.Warning,
                "Emissive strength has no effect when the emissive factor is zero or undefined.",
                pointer + "/emissiveStrength");
        }
    }

    internal void ValidateDiffuseTransmission(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsDiffuseTransmissionExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasDiffuseTransmissionFactor)
        {
            ValidateScalarRange(
                result,
                value.DiffuseTransmissionFactor,
                0.0,
                true,
                1.0,
                pointer + "/diffuseTransmissionFactor");
        }

        if (value.HasDiffuseTransmissionColorFactor)
        {
            ValidateColor(
                result,
                value.DiffuseTransmissionColorFactor,
                true,
                pointer + "/diffuseTransmissionColorFactor");
        }

        ValidateTextureInfo(
            context,
            result,
            value.DiffuseTransmissionTexture,
            pointer + "/diffuseTransmissionTexture");
        ValidateTextureInfo(
            context,
            result,
            value.DiffuseTransmissionColorTexture,
            pointer + "/diffuseTransmissionColorTexture");
        GltfMaterial? material = GetContextMaterial(context);
        if (material != null
            && (HasMaterialExtension(
                    material,
                    GltfExtensionNames.KhrMaterialsPbrSpecularGlossiness)
                || HasMaterialExtension(material, GltfExtensionNames.KhrMaterialsUnlit)))
        {
            AddIssue(
                result,
                GltfIssueCodes.MultipleExtensions,
                GltfValidationSeverity.Warning,
                "Diffuse transmission is incompatible with unlit and specular-glossiness materials.",
                pointer);
        }
    }

    internal void ValidateIor(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsIorExtension value)
    {
        if (value.HasIor
            && (!FiniteNumber.IsFiniteDouble(value.Ior)
                || (value.Ior != 0.0 && value.Ior < 1.0)))
        {
            AddRangeIssue(result, Fallback.CreateExtensionPointer(context) + "/ior");
        }
    }

    internal void ValidatePbrSpecularGlossiness(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsPbrSpecularGlossinessExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasDiffuseFactor)
        {
            ValidateBoundedFactor(
                result,
                value.DiffuseFactor,
                4,
                pointer + "/diffuseFactor");
        }

        if (value.HasSpecularFactor)
        {
            ValidateBoundedFactor(
                result,
                value.SpecularFactor,
                3,
                pointer + "/specularFactor");
        }

        if (value.HasGlossinessFactor)
        {
            ValidateScalarRange(
                result,
                value.GlossinessFactor,
                0.0,
                true,
                1.0,
                pointer + "/glossinessFactor");
        }

        ValidateTextureInfo(
            context,
            result,
            value.DiffuseTexture,
            pointer + "/diffuseTexture");
        ValidateTextureInfo(
            context,
            result,
            value.SpecularGlossinessTexture,
            pointer + "/specularGlossinessTexture");
    }

    internal void ValidateIridescence(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsIridescenceExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasIridescenceFactor)
        {
            ValidateScalarRange(
                result, value.IridescenceFactor, 0.0, true, 1.0,
                pointer + "/iridescenceFactor");
        }

        if (value.HasIridescenceIor)
        {
            ValidateScalarRange(
                result, value.IridescenceIor, 1.0, false, 0.0,
                pointer + "/iridescenceIor");
        }

        if (value.HasIridescenceThicknessMinimum)
        {
            ValidateScalarRange(
                result, value.IridescenceThicknessMinimum, 0.0, false, 0.0,
                pointer + "/iridescenceThicknessMinimum");
        }

        if (value.HasIridescenceThicknessMaximum)
        {
            ValidateScalarRange(
                result, value.IridescenceThicknessMaximum, 0.0, false, 0.0,
                pointer + "/iridescenceThicknessMaximum");
        }

        ValidateTextureInfo(
            context, result, value.IridescenceTexture, pointer + "/iridescenceTexture");
        ValidateTextureInfo(
            context, result, value.IridescenceThicknessTexture,
            pointer + "/iridescenceThicknessTexture");
        if (value.HasIridescenceThicknessMinimum
            && value.IridescenceThicknessTexture == null)
        {
            AddIssue(
                result,
                GltfIssueCodes.KhrMaterialsIridescenceThicknessRangeWithoutTexture,
                GltfValidationSeverity.Information,
                "Thickness minimum has no effect when a thickness texture is not defined.",
                pointer + "/iridescenceThicknessMinimum");
        }

        if (value.IridescenceThicknessTexture != null
            && value.IridescenceThicknessMinimum == value.IridescenceThicknessMaximum)
        {
            AddIssue(
                result,
                GltfIssueCodes.KhrMaterialsIridescenceThicknessTextureUnused,
                GltfValidationSeverity.Information,
                "Thickness texture has no effect when minimum equals maximum.",
                pointer + "/iridescenceThicknessTexture");
        }
    }

    internal void ValidateSheen(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsSheenExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasSheenColorFactor)
        {
            ValidateColor(
                result, value.SheenColorFactor, true, pointer + "/sheenColorFactor");
        }

        if (value.HasSheenRoughnessFactor)
        {
            ValidateScalarRange(
                result, value.SheenRoughnessFactor, 0.0, true, 1.0,
                pointer + "/sheenRoughnessFactor");
        }

        ValidateTextureInfo(
            context, result, value.SheenColorTexture, pointer + "/sheenColorTexture");
        ValidateTextureInfo(
            context, result, value.SheenRoughnessTexture, pointer + "/sheenRoughnessTexture");
    }

    internal void ValidateSpecular(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsSpecularExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasSpecularFactor)
        {
            ValidateScalarRange(
                result, value.SpecularFactor, 0.0, true, 1.0,
                pointer + "/specularFactor");
        }

        if (value.HasSpecularColorFactor)
        {
            ValidateColor(
                result, value.SpecularColorFactor, false,
                pointer + "/specularColorFactor");
        }

        ValidateTextureInfo(
            context, result, value.SpecularTexture, pointer + "/specularTexture");
        ValidateTextureInfo(
            context, result, value.SpecularColorTexture,
            pointer + "/specularColorTexture");
    }

    internal void ValidateTransmission(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsTransmissionExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasTransmissionFactor)
        {
            ValidateScalarRange(
                result, value.TransmissionFactor, 0.0, true, 1.0,
                pointer + "/transmissionFactor");
        }

        ValidateTextureInfo(
            context, result, value.TransmissionTexture, pointer + "/transmissionTexture");
    }

    internal void ValidateVolume(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsVolumeExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context);
        if (value.HasThicknessFactor)
        {
            ValidateScalarRange(
                result, value.ThicknessFactor, 0.0, false, 0.0,
                pointer + "/thicknessFactor");
        }

        if (value.HasAttenuationDistance
            && (!FiniteNumber.IsFiniteDouble(value.AttenuationDistance)
                || value.AttenuationDistance <= 0.0))
        {
            AddRangeIssue(result, pointer + "/attenuationDistance");
        }

        if (value.HasAttenuationColor)
        {
            ValidateColor(
                result, value.AttenuationColor, true, pointer + "/attenuationColor");
        }

        ValidateTextureInfo(
            context, result, value.ThicknessTexture, pointer + "/thicknessTexture");
        GltfMaterial? material = GetContextMaterial(context);
        if (material != null && material.HasDoubleSided && material.DoubleSided)
        {
            AddIssue(
                result,
                GltfIssueCodes.KhrMaterialsVolumeDoubleSided,
                GltfValidationSeverity.Warning,
                "The volume extension should not be used with double-sided materials.",
                pointer);
        }

        if (material != null
            && !HasMaterialExtension(material, GltfExtensionNames.KhrMaterialsTransmission)
            && !HasMaterialExtension(
                material,
                GltfExtensionNames.KhrMaterialsDiffuseTransmission))
        {
            AddIssue(
                result,
                GltfIssueCodes.KhrMaterialsVolumeNoTransmission,
                GltfValidationSeverity.Warning,
                "The volume extension needs an extension that transmits light through the surface.",
                pointer);
        }
    }

    internal void ValidateRootVariants(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsVariantsExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context) + "/variants";
        if (value.Variants == null || value.Variants.Count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                GltfValidationSeverity.Error,
                "The material variants array must not be empty.",
                pointer);
        }
        else
        {
            int index = 0;
            while (index < value.Variants.Count)
            {
                GltfMaterialVariant variant = value.Variants[index];
                if (variant == null || variant.Name == null)
                {
                    AddIssue(
                        result,
                        GltfIssueCodes.TypeMismatch,
                        GltfValidationSeverity.Error,
                        "A material variant must contain a string name.",
                        pointer + "/" + index + "/name");
                }

                index++;
            }
        }
    }

    internal void ValidatePrimitiveVariants(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialsVariantsPrimitiveExtension value)
    {
        string pointer = Fallback.CreateExtensionPointer(context) + "/mappings";
        if (value.Mappings == null || value.Mappings.Count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                GltfValidationSeverity.Error,
                "The material variant mappings array must not be empty.",
                pointer);
            return;
        }

        int variantCount = GetVariantCount(context.Document);
        List<int> usedVariants = new List<int>();
        int mappingIndex = 0;
        while (mappingIndex < value.Mappings.Count)
        {
            GltfMaterialVariantMapping mapping = value.Mappings[mappingIndex];
            string mappingPointer = pointer + "/" + mappingIndex;
            if (mapping == null)
            {
                AddIssue(
                    result,
                    GltfIssueCodes.TypeMismatch,
                    GltfValidationSeverity.Error,
                    "A material variant mapping must be an object.",
                    mappingPointer);
            }
            else
            {
                ValidateVariantMapping(
                    context,
                    result,
                    mapping,
                    mappingPointer,
                    variantCount,
                    usedVariants);
            }

            mappingIndex++;
        }
    }

    internal void ValidateVariantMapping(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfMaterialVariantMapping mapping,
        string pointer,
        int variantCount,
        List<int> usedVariants)
    {
        if (mapping.Variants == null || mapping.Variants.Count == 0)
        {
            AddIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                GltfValidationSeverity.Error,
                "A material variant mapping must reference at least one variant.",
                pointer + "/variants");
        }
        else
        {
            int index = 0;
            while (index < mapping.Variants.Count)
            {
                int variant = mapping.Variants[index];
                string variantPointer = pointer + "/variants/" + index;
                if (variant < 0 || variant >= variantCount)
                {
                    AddIssue(
                        result,
                        GltfIssueCodes.UnresolvedReference,
                        GltfValidationSeverity.Error,
                        $"Material variant index {variant} does not resolve.",
                        variantPointer);
                }

                if (usedVariants.Contains(variant))
                {
                    AddIssue(
                        result,
                        GltfIssueCodes.KhrMaterialsVariantsNonUniqueVariant,
                        GltfValidationSeverity.Error,
                        "A material variant is used more than once for this mesh primitive.",
                        variantPointer);
                }
                else
                {
                    usedVariants.Add(variant);
                }

                index++;
            }
        }

        int materialCount = 0;
        if (context.Document != null && context.Document.Materials != null)
        {
            materialCount = context.Document.Materials.Count;
        }

        if (mapping.Material < 0 || mapping.Material >= materialCount)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                $"Material index {mapping.Material} does not resolve.",
                pointer + "/material");
        }
    }

    internal void ValidateTextureInfo(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfTextureInfo? value,
        string pointer)
    {
        if (value == null)
        {
            return;
        }

        ValidateTextureReference(context, result, value.Index, pointer + "/index");
        if (value.HasTextureCoordinate && value.TextureCoordinate < 0)
        {
            AddRangeIssue(result, pointer + "/texCoord");
        }
    }

    internal void ValidateNormalTextureInfo(
        GltfExtensionContext context,
        GltfExtensionResult result,
        GltfNormalTextureInfo? value,
        string pointer)
    {
        if (value == null)
        {
            return;
        }

        ValidateTextureReference(context, result, value.Index, pointer + "/index");
        if (value.HasTextureCoordinate && value.TextureCoordinate < 0)
        {
            AddRangeIssue(result, pointer + "/texCoord");
        }

        if (value.HasScale && !FiniteNumber.IsFiniteDouble(value.Scale))
        {
            AddRangeIssue(result, pointer + "/scale");
        }
    }

    internal void ValidateTextureReference(
        GltfExtensionContext context,
        GltfExtensionResult result,
        int texture,
        string pointer)
    {
        int count = 0;
        if (context.Document != null && context.Document.Textures != null)
        {
            count = context.Document.Textures.Count;
        }

        if (texture < 0 || texture >= count)
        {
            AddIssue(
                result,
                GltfIssueCodes.UnresolvedReference,
                GltfValidationSeverity.Error,
                $"Texture index {texture} does not resolve.",
                pointer);
        }
    }

    internal void ValidateScalarRange(
        GltfExtensionResult result,
        double value,
        double minimum,
        bool hasMaximum,
        double maximum,
        string pointer)
    {
        if (!FiniteNumber.IsFiniteDouble(value)
            || value < minimum
            || (hasMaximum && value > maximum))
        {
            AddRangeIssue(result, pointer);
        }
    }

    internal void ValidateColor(
        GltfExtensionResult result,
        double[] values,
        bool hasMaximum,
        string pointer)
    {
        if (values == null || values.Length != 3)
        {
            AddIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                GltfValidationSeverity.Error,
                "The color factor must contain three values.",
                pointer);
            return;
        }

        int index = 0;
        while (index < values.Length)
        {
            if (!FiniteNumber.IsFiniteDouble(values[index])
                || values[index] < 0.0
                || (hasMaximum && values[index] > 1.0))
            {
                AddRangeIssue(result, pointer + "/" + index);
            }

            index++;
        }
    }

    internal void ValidateBoundedFactor(
        GltfExtensionResult result,
        double[] values,
        int expectedLength,
        string pointer)
    {
        if (values == null || values.Length != expectedLength)
        {
            AddIssue(
                result,
                GltfIssueCodes.ArrayLengthNotInList,
                GltfValidationSeverity.Error,
                $"The factor must contain {expectedLength} values.",
                pointer);
            return;
        }

        int index = 0;
        while (index < values.Length)
        {
            if (!FiniteNumber.IsFiniteDouble(values[index])
                || values[index] < 0.0
                || values[index] > 1.0)
            {
                AddRangeIssue(result, pointer + "/" + index);
            }

            index++;
        }
    }

    internal GltfMaterial? GetContextMaterial(GltfExtensionContext context)
    {
        if (context.Document == null
            || !context.HasObjectIndex
            || context.ObjectIndex < 0
            || context.ObjectIndex >= context.Document.Materials.Count)
        {
            return null;
        }

        return context.Document.Materials[context.ObjectIndex];
    }

    internal bool HasMaterialExtension(GltfMaterial material, string name)
    {
        return material.PropertyJson.Extensions.ContainsKey(name)
            || material.PropertyJson.TypedExtensions.ContainsKey(name);
    }

    internal bool IsEmissiveFactorZero(GltfMaterial material)
    {
        if (!material.HasEmissiveFactor
            || material.EmissiveFactor == null
            || material.EmissiveFactor.Length != 3)
        {
            return true;
        }

        return material.EmissiveFactor[0] == 0.0
            && material.EmissiveFactor[1] == 0.0
            && material.EmissiveFactor[2] == 0.0;
    }

    internal int GetTextureCoordinate(GltfTextureInfo value)
    {
        int result = value.HasTextureCoordinate ? value.TextureCoordinate : 0;
        if (value.PropertyJson.TypedExtensions.ContainsKey(
            GltfExtensionNames.KhrTextureTransform))
        {
            GltfTypedExtensionData typed = value.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrTextureTransform];
            if (typed.TextureTransform != null && typed.TextureTransform.HasTextureCoordinate)
            {
                result = typed.TextureTransform.TextureCoordinate;
            }
        }

        return result;
    }

    internal int GetNormalTextureCoordinate(GltfNormalTextureInfo value)
    {
        int result = value.HasTextureCoordinate ? value.TextureCoordinate : 0;
        if (value.PropertyJson.TypedExtensions.ContainsKey(
            GltfExtensionNames.KhrTextureTransform))
        {
            GltfTypedExtensionData typed = value.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrTextureTransform];
            if (typed.TextureTransform != null && typed.TextureTransform.HasTextureCoordinate)
            {
                result = typed.TextureTransform.TextureCoordinate;
            }
        }

        return result;
    }

    internal int GetVariantCount(GltfDocument? document)
    {
        if (document == null)
        {
            return 0;
        }

        if (document.PropertyJson.TypedExtensions.ContainsKey(
            GltfExtensionNames.KhrMaterialsVariants))
        {
            GltfTypedExtensionData typed = document.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrMaterialsVariants];
            if (typed.MaterialsVariants != null && typed.MaterialsVariants.Variants != null)
            {
                return typed.MaterialsVariants.Variants.Count;
            }
        }

        if (document.PropertyJson.Extensions.ContainsKey(
            GltfExtensionNames.KhrMaterialsVariants))
        {
            System.Text.Json.Nodes.JsonNode? raw = document.PropertyJson.Extensions[
                GltfExtensionNames.KhrMaterialsVariants];
            if (raw != null
                && raw.GetValueKind() == System.Text.Json.JsonValueKind.Object)
            {
                System.Text.Json.Nodes.JsonNode? variants = raw["variants"];
                if (variants != null
                    && variants.GetValueKind() == System.Text.Json.JsonValueKind.Array)
                {
                    return variants.AsArray().Count;
                }
            }
        }

        return 0;
    }

    internal void AddRangeIssue(GltfExtensionResult result, string pointer)
    {
        AddIssue(
            result,
            GltfIssueCodes.ValueNotInRange,
            GltfValidationSeverity.Error,
            "The material extension value is outside its allowed range.",
            pointer);
    }

    internal void AddIssue(
        GltfExtensionResult result,
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
        if (severity == GltfValidationSeverity.Error)
        {
            result.Status = GltfOperationStatus.Invalid;
        }
    }
}
