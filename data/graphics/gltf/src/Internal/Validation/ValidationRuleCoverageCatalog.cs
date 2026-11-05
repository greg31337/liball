using System;
using System.Collections.Generic;

namespace gltf;

internal class ValidationRuleCoverage
{
    internal string Code = "";
    internal string Category = "";
    internal string Rule = "";
    internal bool Implemented;
    internal bool ExtensionRule;
    internal string? TrackingTask;
}

internal class ValidationRuleCoverageCatalog
{
    internal static readonly string[] IoErrorCodes =
        "IO_ERROR".Split('|');

    internal static readonly string[] SchemaErrorCodes =
        "ARRAY_LENGTH_NOT_IN_LIST|ARRAY_TYPE_MISMATCH|DUPLICATE_ELEMENTS|EMPTY_ENTITY|INVALID_INDEX|INVALID_JSON|INVALID_URI|ONE_OF_MISMATCH|PATTERN_MISMATCH|TYPE_MISMATCH|UNDEFINED_PROPERTY|UNEXPECTED_PROPERTY|UNSATISFIED_DEPENDENCY|VALUE_MULTIPLE_OF|VALUE_NOT_IN_LIST|VALUE_NOT_IN_RANGE".Split('|');

    internal static readonly string[] SemanticErrorCodes =
        "ACCESSOR_MATRIX_ALIGNMENT|ACCESSOR_NORMALIZED_INVALID|ACCESSOR_OFFSET_ALIGNMENT|ACCESSOR_SPARSE_COUNT_OUT_OF_RANGE|ANIMATION_CHANNEL_TARGET_NODE_SKIN|ASSET_MIN_VERSION_GREATER_THAN_VERSION|BUFFER_DATA_URI_MIME_TYPE_INVALID|BUFFER_VIEW_INVALID_BYTE_STRIDE|BUFFER_VIEW_TOO_BIG_BYTE_STRIDE|CAMERA_XMAG_YMAG_NEGATIVE|CAMERA_XMAG_YMAG_ZERO|CAMERA_YFOV_GEQUAL_PI|CAMERA_ZFAR_LEQUAL_ZNEAR|EXTRA_PROPERTY|INVALID_EXTENSION_NAME_FORMAT|INVALID_GL_VALUE|KHR_ANIMATION_POINTER_ANIMATION_CHANNEL_TARGET_NODE|KHR_ANIMATION_POINTER_ANIMATION_CHANNEL_TARGET_PATH|KHR_LIGHTS_PUNCTUAL_LIGHT_SPOT_ANGLES|KHR_MATERIALS_ANISOTROPY_ANISOTROPY_TEXTURE_TEXCOORD|KHR_MATERIALS_CLEARCOAT_CLEARCOAT_NORMAL_TEXTURE_TEXCOORD|KHR_MATERIALS_DISPERSION_NO_VOLUME|KHR_MATERIALS_EMISSIVE_STRENGTH_ZERO_FACTOR|KHR_MATERIALS_IRIDESCENCE_THICKNESS_RANGE_WITHOUT_TEXTURE|KHR_MATERIALS_IRIDESCENCE_THICKNESS_TEXTURE_UNUSED|KHR_MATERIALS_VOLUME_DOUBLE_SIDED|KHR_MATERIALS_VOLUME_NO_TRANSMISSION|MATERIAL_ALPHA_CUTOFF_INVALID_MODE|MESH_INVALID_WEIGHTS_COUNT|MESH_PRIMITIVES_UNEQUAL_TARGETS_COUNT|MESH_PRIMITIVE_INDEXED_SEMANTIC_CONTINUITY|MESH_PRIMITIVE_INVALID_ATTRIBUTE|MESH_PRIMITIVE_JOINTS_WEIGHTS_MISMATCH|MESH_PRIMITIVE_NO_POSITION|MESH_PRIMITIVE_TANGENT_WITHOUT_NORMAL|MULTIPLE_EXTENSIONS|NODE_EMPTY|NODE_MATRIX_DEFAULT|NODE_MATRIX_NON_TRS|NODE_MATRIX_TRS|NODE_SKINNED_MESH_LOCAL_TRANSFORMS|NODE_SKINNED_MESH_NON_ROOT|NODE_SKINNED_MESH_PARENT_TRANSFORMS|NODE_SKIN_NO_SCENE|NON_OBJECT_EXTRAS|NON_RELATIVE_URI|NON_REQUIRED_EXTENSION|ROTATION_NON_UNIT|SKIN_NO_COMMON_ROOT|SKIN_SKELETON_INVALID|UNKNOWN_ASSET_MAJOR_VERSION|UNKNOWN_ASSET_MINOR_VERSION|UNUSED_EXTENSION_REQUIRED".Split('|');

    internal static readonly string[] LinkErrorCodes =
        "ACCESSOR_SMALL_BYTESTRIDE|ACCESSOR_TOO_LONG|ACCESSOR_TOTAL_OFFSET_ALIGNMENT|ACCESSOR_USAGE_OVERRIDE|ANIMATION_CHANNEL_TARGET_NODE_MATRIX|ANIMATION_CHANNEL_TARGET_NODE_WEIGHTS_NO_MORPHS|ANIMATION_DUPLICATE_TARGETS|ANIMATION_SAMPLER_ACCESSOR_WITH_BYTESTRIDE|ANIMATION_SAMPLER_INPUT_ACCESSOR_INVALID_FORMAT|ANIMATION_SAMPLER_INPUT_ACCESSOR_TOO_FEW_ELEMENTS|ANIMATION_SAMPLER_INPUT_ACCESSOR_WITHOUT_BOUNDS|ANIMATION_SAMPLER_OUTPUT_ACCESSOR_INVALID_COUNT|ANIMATION_SAMPLER_OUTPUT_ACCESSOR_INVALID_FORMAT|BUFFER_MISSING_GLB_DATA|BUFFER_VIEW_TARGET_MISSING|BUFFER_VIEW_TARGET_OVERRIDE|BUFFER_VIEW_TOO_LONG|IMAGE_BUFFER_VIEW_WITH_BYTESTRIDE|INCOMPLETE_EXTENSION_SUPPORT|INVALID_IBM_ACCESSOR_COUNT|KHR_MATERIALS_VARIANTS_NON_UNIQUE_VARIANT|MESH_PRIMITIVE_ACCESSOR_UNALIGNED|MESH_PRIMITIVE_ACCESSOR_WITHOUT_BYTESTRIDE|MESH_PRIMITIVE_ATTRIBUTES_ACCESSOR_INVALID_FORMAT|MESH_PRIMITIVE_ATTRIBUTES_ACCESSOR_UNSIGNED_INT|MESH_PRIMITIVE_GENERATED_TANGENT_SPACE|MESH_PRIMITIVE_INCOMPATIBLE_MODE|MESH_PRIMITIVE_INDICES_ACCESSOR_INVALID_FORMAT|MESH_PRIMITIVE_INDICES_ACCESSOR_WITH_BYTESTRIDE|MESH_PRIMITIVE_MORPH_TARGET_INVALID_ATTRIBUTE_COUNT|MESH_PRIMITIVE_MORPH_TARGET_NO_BASE_ACCESSOR|MESH_PRIMITIVE_NO_TANGENT_SPACE|MESH_PRIMITIVE_POSITION_ACCESSOR_WITHOUT_BOUNDS|MESH_PRIMITIVE_TOO_FEW_TEXCOORDS|MESH_PRIMITIVE_UNEQUAL_ACCESSOR_COUNT|NODE_LOOP|NODE_PARENT_OVERRIDE|NODE_SKINNED_MESH_WITHOUT_SKIN|NODE_SKIN_WITH_NON_SKINNED_MESH|NODE_WEIGHTS_INVALID|SCENE_NON_ROOT_NODE|SKIN_IBM_ACCESSOR_WITH_BYTESTRIDE|SKIN_IBM_INVALID_FORMAT|TEXTURE_INVALID_IMAGE_MIME_TYPE|UNDECLARED_EXTENSION|UNEXPECTED_EXTENSION_OBJECT|UNRESOLVED_REFERENCE|UNSUPPORTED_EXTENSION|UNUSED_MESH_TANGENT|UNUSED_MESH_WEIGHTS|UNUSED_OBJECT".Split('|');

    internal static readonly string[] DataErrorCodes =
        "ACCESSOR_ANIMATION_INPUT_NEGATIVE|ACCESSOR_ANIMATION_INPUT_NON_INCREASING|ACCESSOR_ANIMATION_SAMPLER_OUTPUT_NON_NORMALIZED_QUATERNION|ACCESSOR_ELEMENT_OUT_OF_MAX_BOUND|ACCESSOR_ELEMENT_OUT_OF_MIN_BOUND|ACCESSOR_INDEX_OOB|ACCESSOR_INDEX_PRIMITIVE_RESTART|ACCESSOR_INDEX_TRIANGLE_DEGENERATE|ACCESSOR_INVALID_FLOAT|ACCESSOR_INVALID_IBM|ACCESSOR_INVALID_SIGN|ACCESSOR_JOINTS_INDEX_DUPLICATE|ACCESSOR_JOINTS_INDEX_OOB|ACCESSOR_JOINTS_USED_ZERO_WEIGHT|ACCESSOR_MAX_MISMATCH|ACCESSOR_MIN_MISMATCH|ACCESSOR_NON_CLAMPED|ACCESSOR_SPARSE_INDEX_OOB|ACCESSOR_SPARSE_INDICES_NON_INCREASING|ACCESSOR_VECTOR3_NON_UNIT|ACCESSOR_WEIGHTS_NEGATIVE|ACCESSOR_WEIGHTS_NON_NORMALIZED|BUFFER_BYTE_LENGTH_MISMATCH|BUFFER_GLB_CHUNK_TOO_BIG|DATA_URI_GLB|IMAGE_DATA_INVALID|IMAGE_FEATURES_UNSUPPORTED|IMAGE_MIME_TYPE_INVALID|IMAGE_NON_ENABLED_MIME_TYPE|IMAGE_NPOT_DIMENSIONS|IMAGE_UNEXPECTED_EOS|IMAGE_UNRECOGNIZED_FORMAT|URI_GLB".Split('|');

    internal static readonly string[] GlbErrorCodes =
        "GLB_CHUNK_LENGTH_UNALIGNED|GLB_CHUNK_TOO_BIG|GLB_DUPLICATE_CHUNK|GLB_EMPTY_BIN_CHUNK|GLB_EMPTY_CHUNK|GLB_EXTRA_DATA|GLB_INVALID_MAGIC|GLB_INVALID_VERSION|GLB_LENGTH_MISMATCH|GLB_LENGTH_TOO_SMALL|GLB_UNEXPECTED_BIN_CHUNK|GLB_UNEXPECTED_END_OF_CHUNK_DATA|GLB_UNEXPECTED_END_OF_CHUNK_HEADER|GLB_UNEXPECTED_END_OF_HEADER|GLB_UNEXPECTED_FIRST_CHUNK|GLB_UNKNOWN_CHUNK_TYPE".Split('|');

    internal List<ValidationRuleCoverage> CreateCoverageCatalog()
    {
        List<ValidationRuleCoverage> result = new List<ValidationRuleCoverage>();
        AddCategory(result, IoErrorCodes, "IoError", "Validator input handling");
        AddCategory(result, SchemaErrorCodes, "SchemaError", "StructuralValidator");
        AddCategory(result, SemanticErrorCodes, "SemanticError", "Semantic validators");
        AddCategory(result, LinkErrorCodes, "LinkError", "Reference validators");
        AddCategory(result, DataErrorCodes, "DataError", "Decoded data validators");
        AddCategory(result, GlbErrorCodes, "GlbError", "GlbReader");
        ConfigureExtensionRules(result);
        return result;
    }

    internal void AddCategory(
        List<ValidationRuleCoverage> result,
        string[] codes,
        string category,
        string rule)
    {
        int index = 0;
        while (index < codes.Length)
        {
            ValidationRuleCoverage entry = new ValidationRuleCoverage();
            entry.Code = codes[index];
            entry.Category = category;
            entry.Rule = rule;
            entry.Implemented = true;
            result.Add(entry);
            index++;
        }
    }

    internal void ConfigureExtensionRules(List<ValidationRuleCoverage> entries)
    {
        int index = 0;
        while (index < entries.Count)
        {
            ValidationRuleCoverage entry = entries[index];
            if (entry.Code.StartsWith("KHR_", StringComparison.Ordinal))
            {
                entry.ExtensionRule = true;
                if (entry.Code == GltfIssueCodes.KhrAnimationPointerAnimationChannelTargetNode
                    || entry.Code == GltfIssueCodes.KhrAnimationPointerAnimationChannelTargetPath)
                {
                    entry.Rule = "AnimationValidator";
                }
                else if (entry.Code == GltfIssueCodes.KhrLightsPunctualLightSpotAngles)
                {
                    entry.Rule = "SceneExtensionValidator";
                }
                else if (IsMaterialExtensionRule(entry.Code))
                {
                    entry.Rule = "MaterialExtensionValidator";
                }
                else
                {
                    entry.Implemented = false;
                    entry.Rule = "Tracked registered extension rule";
                    entry.TrackingTask = GetExtensionTrackingTask(entry.Code);
                }
            }

            index++;
        }
    }

    internal string GetExtensionTrackingTask(string code)
    {
        if (code == GltfIssueCodes.KhrLightsPunctualLightSpotAngles)
        {
            return "T082";
        }

        return "T083";
    }

    internal bool IsMaterialExtensionRule(string code)
    {
        return code == GltfIssueCodes.KhrMaterialsAnisotropyAnisotropyTextureTexcoord
            || code == GltfIssueCodes.KhrMaterialsClearcoatClearcoatNormalTextureTexcoord
            || code == GltfIssueCodes.KhrMaterialsDispersionNoVolume
            || code == GltfIssueCodes.KhrMaterialsEmissiveStrengthZeroFactor
            || code == GltfIssueCodes.KhrMaterialsIridescenceThicknessRangeWithoutTexture
            || code == GltfIssueCodes.KhrMaterialsIridescenceThicknessTextureUnused
            || code == GltfIssueCodes.KhrMaterialsVariantsNonUniqueVariant
            || code == GltfIssueCodes.KhrMaterialsVolumeDoubleSided
            || code == GltfIssueCodes.KhrMaterialsVolumeNoTransmission;
    }

    internal ValidationRuleCoverage? FindCoverage(
        List<ValidationRuleCoverage> entries,
        string code)
    {
        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].Code == code)
            {
                return entries[index];
            }

            index++;
        }

        return null;
    }
}
