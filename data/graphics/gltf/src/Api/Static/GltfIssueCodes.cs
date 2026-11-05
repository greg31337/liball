namespace gltf;

/// <summary>Provides stable validation and operation diagnostic issue codes.</summary>
public static class GltfIssueCodes
{
    /// <summary>The <c>ARCHIVED_EXTENSION</c> issue code.</summary>
    public const string ArchivedExtension = "ARCHIVED_EXTENSION";

    /// <summary>The <c>ACCESSOR_ANIMATION_INPUT_NEGATIVE</c> issue code.</summary>
    public const string AccessorAnimationInputNegative = "ACCESSOR_ANIMATION_INPUT_NEGATIVE";

    /// <summary>The <c>ACCESSOR_ANIMATION_INPUT_NON_INCREASING</c> issue code.</summary>
    public const string AccessorAnimationInputNonIncreasing = "ACCESSOR_ANIMATION_INPUT_NON_INCREASING";

    /// <summary>The <c>ACCESSOR_ANIMATION_SAMPLER_OUTPUT_NON_NORMALIZED_QUATERNION</c> issue code.</summary>
    public const string AccessorAnimationSamplerOutputNonNormalizedQuaternion = "ACCESSOR_ANIMATION_SAMPLER_OUTPUT_NON_NORMALIZED_QUATERNION";

    /// <summary>The <c>ACCESSOR_ELEMENT_OUT_OF_MAX_BOUND</c> issue code.</summary>
    public const string AccessorElementOutOfMaxBound = "ACCESSOR_ELEMENT_OUT_OF_MAX_BOUND";

    /// <summary>The <c>ACCESSOR_ELEMENT_OUT_OF_MIN_BOUND</c> issue code.</summary>
    public const string AccessorElementOutOfMinBound = "ACCESSOR_ELEMENT_OUT_OF_MIN_BOUND";

    /// <summary>The <c>ACCESSOR_INDEX_OOB</c> issue code.</summary>
    public const string AccessorIndexOob = "ACCESSOR_INDEX_OOB";

    /// <summary>The <c>ACCESSOR_INDEX_PRIMITIVE_RESTART</c> issue code.</summary>
    public const string AccessorIndexPrimitiveRestart = "ACCESSOR_INDEX_PRIMITIVE_RESTART";

    /// <summary>The <c>ACCESSOR_INDEX_TRIANGLE_DEGENERATE</c> issue code.</summary>
    public const string AccessorIndexTriangleDegenerate = "ACCESSOR_INDEX_TRIANGLE_DEGENERATE";

    /// <summary>The <c>ACCESSOR_INVALID_FLOAT</c> issue code.</summary>
    public const string AccessorInvalidFloat = "ACCESSOR_INVALID_FLOAT";

    /// <summary>The <c>ACCESSOR_INVALID_IBM</c> issue code.</summary>
    public const string AccessorInvalidIbm = "ACCESSOR_INVALID_IBM";

    /// <summary>The <c>ACCESSOR_INVALID_SIGN</c> issue code.</summary>
    public const string AccessorInvalidSign = "ACCESSOR_INVALID_SIGN";

    /// <summary>The <c>ACCESSOR_JOINTS_INDEX_DUPLICATE</c> issue code.</summary>
    public const string AccessorJointsIndexDuplicate = "ACCESSOR_JOINTS_INDEX_DUPLICATE";

    /// <summary>The <c>ACCESSOR_JOINTS_INDEX_OOB</c> issue code.</summary>
    public const string AccessorJointsIndexOob = "ACCESSOR_JOINTS_INDEX_OOB";

    /// <summary>The <c>ACCESSOR_JOINTS_USED_ZERO_WEIGHT</c> issue code.</summary>
    public const string AccessorJointsUsedZeroWeight = "ACCESSOR_JOINTS_USED_ZERO_WEIGHT";

    /// <summary>The <c>ACCESSOR_MATRIX_ALIGNMENT</c> issue code.</summary>
    public const string AccessorMatrixAlignment = "ACCESSOR_MATRIX_ALIGNMENT";

    /// <summary>The <c>ACCESSOR_MAX_MISMATCH</c> issue code.</summary>
    public const string AccessorMaxMismatch = "ACCESSOR_MAX_MISMATCH";

    /// <summary>The <c>ACCESSOR_MIN_MISMATCH</c> issue code.</summary>
    public const string AccessorMinMismatch = "ACCESSOR_MIN_MISMATCH";

    /// <summary>The <c>ACCESSOR_NON_CLAMPED</c> issue code.</summary>
    public const string AccessorNonClamped = "ACCESSOR_NON_CLAMPED";

    /// <summary>The <c>ACCESSOR_NORMALIZED_INVALID</c> issue code.</summary>
    public const string AccessorNormalizedInvalid = "ACCESSOR_NORMALIZED_INVALID";

    /// <summary>The <c>ACCESSOR_OFFSET_ALIGNMENT</c> issue code.</summary>
    public const string AccessorOffsetAlignment = "ACCESSOR_OFFSET_ALIGNMENT";

    /// <summary>The <c>ACCESSOR_SMALL_BYTESTRIDE</c> issue code.</summary>
    public const string AccessorSmallBytestride = "ACCESSOR_SMALL_BYTESTRIDE";

    /// <summary>The <c>ACCESSOR_SPARSE_COUNT_OUT_OF_RANGE</c> issue code.</summary>
    public const string AccessorSparseCountOutOfRange = "ACCESSOR_SPARSE_COUNT_OUT_OF_RANGE";

    /// <summary>The <c>ACCESSOR_SPARSE_INDEX_OOB</c> issue code.</summary>
    public const string AccessorSparseIndexOob = "ACCESSOR_SPARSE_INDEX_OOB";

    /// <summary>The <c>ACCESSOR_SPARSE_INDICES_NON_INCREASING</c> issue code.</summary>
    public const string AccessorSparseIndicesNonIncreasing = "ACCESSOR_SPARSE_INDICES_NON_INCREASING";

    /// <summary>The <c>ACCESSOR_TOO_LONG</c> issue code.</summary>
    public const string AccessorTooLong = "ACCESSOR_TOO_LONG";

    /// <summary>The <c>ACCESSOR_TOTAL_OFFSET_ALIGNMENT</c> issue code.</summary>
    public const string AccessorTotalOffsetAlignment = "ACCESSOR_TOTAL_OFFSET_ALIGNMENT";

    /// <summary>The <c>ACCESSOR_USAGE_OVERRIDE</c> issue code.</summary>
    public const string AccessorUsageOverride = "ACCESSOR_USAGE_OVERRIDE";

    /// <summary>The <c>ACCESSOR_VECTOR3_NON_UNIT</c> issue code.</summary>
    public const string AccessorVector3NonUnit = "ACCESSOR_VECTOR3_NON_UNIT";

    /// <summary>The <c>ACCESSOR_WEIGHTS_NEGATIVE</c> issue code.</summary>
    public const string AccessorWeightsNegative = "ACCESSOR_WEIGHTS_NEGATIVE";

    /// <summary>The <c>ACCESSOR_WEIGHTS_NON_NORMALIZED</c> issue code.</summary>
    public const string AccessorWeightsNonNormalized = "ACCESSOR_WEIGHTS_NON_NORMALIZED";

    /// <summary>The <c>ANIMATION_CHANNEL_TARGET_NODE_MATRIX</c> issue code.</summary>
    public const string AnimationChannelTargetNodeMatrix = "ANIMATION_CHANNEL_TARGET_NODE_MATRIX";

    /// <summary>The <c>ANIMATION_CHANNEL_TARGET_NODE_SKIN</c> issue code.</summary>
    public const string AnimationChannelTargetNodeSkin = "ANIMATION_CHANNEL_TARGET_NODE_SKIN";

    /// <summary>The <c>ANIMATION_CHANNEL_TARGET_NODE_WEIGHTS_NO_MORPHS</c> issue code.</summary>
    public const string AnimationChannelTargetNodeWeightsNoMorphs = "ANIMATION_CHANNEL_TARGET_NODE_WEIGHTS_NO_MORPHS";

    /// <summary>The <c>ANIMATION_DUPLICATE_TARGETS</c> issue code.</summary>
    public const string AnimationDuplicateTargets = "ANIMATION_DUPLICATE_TARGETS";

    /// <summary>The <c>ANIMATION_SAMPLER_ACCESSOR_WITH_BYTESTRIDE</c> issue code.</summary>
    public const string AnimationSamplerAccessorWithBytestride = "ANIMATION_SAMPLER_ACCESSOR_WITH_BYTESTRIDE";

    /// <summary>The <c>ANIMATION_SAMPLER_INPUT_ACCESSOR_INVALID_FORMAT</c> issue code.</summary>
    public const string AnimationSamplerInputAccessorInvalidFormat = "ANIMATION_SAMPLER_INPUT_ACCESSOR_INVALID_FORMAT";

    /// <summary>The <c>ANIMATION_SAMPLER_INPUT_ACCESSOR_TOO_FEW_ELEMENTS</c> issue code.</summary>
    public const string AnimationSamplerInputAccessorTooFewElements = "ANIMATION_SAMPLER_INPUT_ACCESSOR_TOO_FEW_ELEMENTS";

    /// <summary>The <c>ANIMATION_SAMPLER_INPUT_ACCESSOR_WITHOUT_BOUNDS</c> issue code.</summary>
    public const string AnimationSamplerInputAccessorWithoutBounds = "ANIMATION_SAMPLER_INPUT_ACCESSOR_WITHOUT_BOUNDS";

    /// <summary>The <c>ANIMATION_SAMPLER_OUTPUT_ACCESSOR_INVALID_COUNT</c> issue code.</summary>
    public const string AnimationSamplerOutputAccessorInvalidCount = "ANIMATION_SAMPLER_OUTPUT_ACCESSOR_INVALID_COUNT";

    /// <summary>The <c>ANIMATION_SAMPLER_OUTPUT_ACCESSOR_INVALID_FORMAT</c> issue code.</summary>
    public const string AnimationSamplerOutputAccessorInvalidFormat = "ANIMATION_SAMPLER_OUTPUT_ACCESSOR_INVALID_FORMAT";

    /// <summary>The <c>ARRAY_LENGTH_NOT_IN_LIST</c> issue code.</summary>
    public const string ArrayLengthNotInList = "ARRAY_LENGTH_NOT_IN_LIST";

    /// <summary>The <c>ARRAY_TYPE_MISMATCH</c> issue code.</summary>
    public const string ArrayTypeMismatch = "ARRAY_TYPE_MISMATCH";

    /// <summary>The <c>ASSET_MIN_VERSION_GREATER_THAN_VERSION</c> issue code.</summary>
    public const string AssetMinVersionGreaterThanVersion = "ASSET_MIN_VERSION_GREATER_THAN_VERSION";

    /// <summary>The <c>ATOMIC_OUTPUT_FAILED</c> issue code.</summary>
    public const string AtomicOutputFailed = "ATOMIC_OUTPUT_FAILED";

    /// <summary>The <c>BASE64_INVALID</c> issue code.</summary>
    public const string Base64Invalid = "BASE64_INVALID";

    /// <summary>The <c>BUFFER_BYTE_LENGTH_MISMATCH</c> issue code.</summary>
    public const string BufferByteLengthMismatch = "BUFFER_BYTE_LENGTH_MISMATCH";

    /// <summary>The <c>BUFFER_DATA_URI_MIME_TYPE_INVALID</c> issue code.</summary>
    public const string BufferDataUriMimeTypeInvalid = "BUFFER_DATA_URI_MIME_TYPE_INVALID";

    /// <summary>The <c>BUFFER_GLB_CHUNK_TOO_BIG</c> issue code.</summary>
    public const string BufferGlbChunkTooBig = "BUFFER_GLB_CHUNK_TOO_BIG";

    /// <summary>The <c>BUFFER_MISSING_GLB_DATA</c> issue code.</summary>
    public const string BufferMissingGlbData = "BUFFER_MISSING_GLB_DATA";

    /// <summary>The <c>BUFFER_VIEW_INVALID_BYTE_STRIDE</c> issue code.</summary>
    public const string BufferViewInvalidByteStride = "BUFFER_VIEW_INVALID_BYTE_STRIDE";

    /// <summary>The <c>BUFFER_VIEW_TARGET_MISSING</c> issue code.</summary>
    public const string BufferViewTargetMissing = "BUFFER_VIEW_TARGET_MISSING";

    /// <summary>The <c>BUFFER_VIEW_TARGET_OVERRIDE</c> issue code.</summary>
    public const string BufferViewTargetOverride = "BUFFER_VIEW_TARGET_OVERRIDE";

    /// <summary>The <c>BUFFER_VIEW_TOO_BIG_BYTE_STRIDE</c> issue code.</summary>
    public const string BufferViewTooBigByteStride = "BUFFER_VIEW_TOO_BIG_BYTE_STRIDE";

    /// <summary>The <c>BUFFER_VIEW_TOO_LONG</c> issue code.</summary>
    public const string BufferViewTooLong = "BUFFER_VIEW_TOO_LONG";

    /// <summary>The <c>CAMERA_XMAG_YMAG_NEGATIVE</c> issue code.</summary>
    public const string CameraXmagYmagNegative = "CAMERA_XMAG_YMAG_NEGATIVE";

    /// <summary>The <c>CAMERA_XMAG_YMAG_ZERO</c> issue code.</summary>
    public const string CameraXmagYmagZero = "CAMERA_XMAG_YMAG_ZERO";

    /// <summary>The <c>CAMERA_YFOV_GEQUAL_PI</c> issue code.</summary>
    public const string CameraYfovGequalPi = "CAMERA_YFOV_GEQUAL_PI";

    /// <summary>The <c>CAMERA_ZFAR_LEQUAL_ZNEAR</c> issue code.</summary>
    public const string CameraZfarLequalZnear = "CAMERA_ZFAR_LEQUAL_ZNEAR";

    /// <summary>The <c>CONTAINER_FORMAT_MISMATCH</c> issue code.</summary>
    public const string ContainerFormatMismatch = "CONTAINER_FORMAT_MISMATCH";

    /// <summary>The <c>CONTAINER_FORMAT_UNKNOWN</c> issue code.</summary>
    public const string ContainerFormatUnknown = "CONTAINER_FORMAT_UNKNOWN";

    /// <summary>The <c>CONVERSION_RESOURCE_COLLISION</c> issue code.</summary>
    public const string ConversionResourceCollision = "CONVERSION_RESOURCE_COLLISION";

    /// <summary>The <c>DATA_URI_GLB</c> issue code.</summary>
    public const string DataUriGlb = "DATA_URI_GLB";

    /// <summary>The <c>DATA_URI_INVALID</c> issue code.</summary>
    public const string DataUriInvalid = "DATA_URI_INVALID";

    /// <summary>The <c>DUPLICATE_ELEMENTS</c> issue code.</summary>
    public const string DuplicateElements = "DUPLICATE_ELEMENTS";

    /// <summary>The <c>EMPTY_ENTITY</c> issue code.</summary>
    public const string EmptyEntity = "EMPTY_ENTITY";

    /// <summary>The <c>EXTENSION_HANDLER_UNAVAILABLE</c> issue code.</summary>
    public const string ExtensionHandlerUnavailable = "EXTENSION_HANDLER_UNAVAILABLE";

    /// <summary>The <c>EXTRA_PROPERTY</c> issue code.</summary>
    public const string ExtraProperty = "EXTRA_PROPERTY";

    /// <summary>The <c>GLB_CHUNK_LENGTH_UNALIGNED</c> issue code.</summary>
    public const string GlbChunkLengthUnaligned = "GLB_CHUNK_LENGTH_UNALIGNED";

    /// <summary>The <c>GLB_CHUNK_TOO_BIG</c> issue code.</summary>
    public const string GlbChunkTooBig = "GLB_CHUNK_TOO_BIG";

    /// <summary>The <c>GLB_DUPLICATE_CHUNK</c> issue code.</summary>
    public const string GlbDuplicateChunk = "GLB_DUPLICATE_CHUNK";

    /// <summary>The <c>GLB_EMPTY_BIN_CHUNK</c> issue code.</summary>
    public const string GlbEmptyBinChunk = "GLB_EMPTY_BIN_CHUNK";

    /// <summary>The <c>GLB_EMPTY_CHUNK</c> issue code.</summary>
    public const string GlbEmptyChunk = "GLB_EMPTY_CHUNK";

    /// <summary>The <c>GLB_EXTRA_DATA</c> issue code.</summary>
    public const string GlbExtraData = "GLB_EXTRA_DATA";

    /// <summary>The <c>GLB_INVALID_MAGIC</c> issue code.</summary>
    public const string GlbInvalidMagic = "GLB_INVALID_MAGIC";

    /// <summary>The <c>GLB_INVALID_VERSION</c> issue code.</summary>
    public const string GlbInvalidVersion = "GLB_INVALID_VERSION";

    /// <summary>The <c>GLB_LENGTH_MISMATCH</c> issue code.</summary>
    public const string GlbLengthMismatch = "GLB_LENGTH_MISMATCH";

    /// <summary>The <c>GLB_LENGTH_TOO_SMALL</c> issue code.</summary>
    public const string GlbLengthTooSmall = "GLB_LENGTH_TOO_SMALL";

    /// <summary>The <c>GLB_UNEXPECTED_BIN_CHUNK</c> issue code.</summary>
    public const string GlbUnexpectedBinChunk = "GLB_UNEXPECTED_BIN_CHUNK";

    /// <summary>The <c>GLB_UNEXPECTED_END_OF_CHUNK_DATA</c> issue code.</summary>
    public const string GlbUnexpectedEndOfChunkData = "GLB_UNEXPECTED_END_OF_CHUNK_DATA";

    /// <summary>The <c>GLB_UNEXPECTED_END_OF_CHUNK_HEADER</c> issue code.</summary>
    public const string GlbUnexpectedEndOfChunkHeader = "GLB_UNEXPECTED_END_OF_CHUNK_HEADER";

    /// <summary>The <c>GLB_UNEXPECTED_END_OF_HEADER</c> issue code.</summary>
    public const string GlbUnexpectedEndOfHeader = "GLB_UNEXPECTED_END_OF_HEADER";

    /// <summary>The <c>GLB_UNEXPECTED_FIRST_CHUNK</c> issue code.</summary>
    public const string GlbUnexpectedFirstChunk = "GLB_UNEXPECTED_FIRST_CHUNK";

    /// <summary>The <c>GLB_UNKNOWN_CHUNK_TYPE</c> issue code.</summary>
    public const string GlbUnknownChunkType = "GLB_UNKNOWN_CHUNK_TYPE";

    /// <summary>The <c>IMAGE_BUFFER_VIEW_WITH_BYTESTRIDE</c> issue code.</summary>
    public const string ImageBufferViewWithBytestride = "IMAGE_BUFFER_VIEW_WITH_BYTESTRIDE";

    /// <summary>The <c>IMAGE_CODEC_UNAVAILABLE</c> issue code.</summary>
    public const string ImageCodecUnavailable = "IMAGE_CODEC_UNAVAILABLE";

    /// <summary>The <c>IMAGE_DATA_INVALID</c> issue code.</summary>
    public const string ImageDataInvalid = "IMAGE_DATA_INVALID";

    /// <summary>The <c>IMAGE_FEATURES_UNSUPPORTED</c> issue code.</summary>
    public const string ImageFeaturesUnsupported = "IMAGE_FEATURES_UNSUPPORTED";

    /// <summary>The <c>IMAGE_MIME_TYPE_INVALID</c> issue code.</summary>
    public const string ImageMimeTypeInvalid = "IMAGE_MIME_TYPE_INVALID";

    /// <summary>The <c>IMAGE_NON_ENABLED_MIME_TYPE</c> issue code.</summary>
    public const string ImageNonEnabledMimeType = "IMAGE_NON_ENABLED_MIME_TYPE";

    /// <summary>The <c>IMAGE_NPOT_DIMENSIONS</c> issue code.</summary>
    public const string ImageNpotDimensions = "IMAGE_NPOT_DIMENSIONS";

    /// <summary>The <c>IMAGE_UNEXPECTED_EOS</c> issue code.</summary>
    public const string ImageUnexpectedEos = "IMAGE_UNEXPECTED_EOS";

    /// <summary>The <c>IMAGE_UNRECOGNIZED_FORMAT</c> issue code.</summary>
    public const string ImageUnrecognizedFormat = "IMAGE_UNRECOGNIZED_FORMAT";

    /// <summary>The <c>INCOMPLETE_EXTENSION_SUPPORT</c> issue code.</summary>
    public const string IncompleteExtensionSupport = "INCOMPLETE_EXTENSION_SUPPORT";

    /// <summary>The <c>INPUT_EMPTY</c> issue code.</summary>
    public const string InputEmpty = "INPUT_EMPTY";

    /// <summary>The <c>INPUT_LIMIT_EXCEEDED</c> issue code.</summary>
    public const string InputLimitExceeded = "INPUT_LIMIT_EXCEEDED";

    /// <summary>The <c>INVALID_EXTENSION_NAME_FORMAT</c> issue code.</summary>
    public const string InvalidExtensionNameFormat = "INVALID_EXTENSION_NAME_FORMAT";

    /// <summary>The <c>INVALID_GL_VALUE</c> issue code.</summary>
    public const string InvalidGlValue = "INVALID_GL_VALUE";

    /// <summary>The <c>INVALID_IBM_ACCESSOR_COUNT</c> issue code.</summary>
    public const string InvalidIbmAccessorCount = "INVALID_IBM_ACCESSOR_COUNT";

    /// <summary>The <c>INVALID_INDEX</c> issue code.</summary>
    public const string InvalidIndex = "INVALID_INDEX";

    /// <summary>The <c>INVALID_JSON</c> issue code.</summary>
    public const string InvalidJson = "INVALID_JSON";

    /// <summary>The <c>INVALID_URI</c> issue code.</summary>
    public const string InvalidUri = "INVALID_URI";

    /// <summary>The <c>IO_ERROR</c> issue code.</summary>
    public const string IoError = "IO_ERROR";

    /// <summary>The <c>JSON_DEPTH_LIMIT_EXCEEDED</c> issue code.</summary>
    public const string JsonDepthLimitExceeded = "JSON_DEPTH_LIMIT_EXCEEDED";

    /// <summary>The <c>JSON_DUPLICATE_MEMBER</c> issue code.</summary>
    public const string JsonDuplicateMember = "JSON_DUPLICATE_MEMBER";

    /// <summary>The <c>JSON_NUMBER_INVALID</c> issue code.</summary>
    public const string JsonNumberInvalid = "JSON_NUMBER_INVALID";

    /// <summary>The <c>JSON_STRING_LIMIT_EXCEEDED</c> issue code.</summary>
    public const string JsonStringLimitExceeded = "JSON_STRING_LIMIT_EXCEEDED";

    /// <summary>The <c>JSON_TOKEN_LIMIT_EXCEEDED</c> issue code.</summary>
    public const string JsonTokenLimitExceeded = "JSON_TOKEN_LIMIT_EXCEEDED";

    /// <summary>The <c>KHR_ANIMATION_POINTER_ANIMATION_CHANNEL_TARGET_NODE</c> issue code.</summary>
    public const string KhrAnimationPointerAnimationChannelTargetNode = "KHR_ANIMATION_POINTER_ANIMATION_CHANNEL_TARGET_NODE";

    /// <summary>The <c>KHR_ANIMATION_POINTER_ANIMATION_CHANNEL_TARGET_PATH</c> issue code.</summary>
    public const string KhrAnimationPointerAnimationChannelTargetPath = "KHR_ANIMATION_POINTER_ANIMATION_CHANNEL_TARGET_PATH";

    /// <summary>The <c>KHR_LIGHTS_PUNCTUAL_LIGHT_SPOT_ANGLES</c> issue code.</summary>
    public const string KhrLightsPunctualLightSpotAngles = "KHR_LIGHTS_PUNCTUAL_LIGHT_SPOT_ANGLES";

    /// <summary>The <c>KHR_MATERIALS_ANISOTROPY_ANISOTROPY_TEXTURE_TEXCOORD</c> issue code.</summary>
    public const string KhrMaterialsAnisotropyAnisotropyTextureTexcoord = "KHR_MATERIALS_ANISOTROPY_ANISOTROPY_TEXTURE_TEXCOORD";

    /// <summary>The <c>KHR_MATERIALS_CLEARCOAT_CLEARCOAT_NORMAL_TEXTURE_TEXCOORD</c> issue code.</summary>
    public const string KhrMaterialsClearcoatClearcoatNormalTextureTexcoord = "KHR_MATERIALS_CLEARCOAT_CLEARCOAT_NORMAL_TEXTURE_TEXCOORD";

    /// <summary>The <c>KHR_MATERIALS_DISPERSION_NO_VOLUME</c> issue code.</summary>
    public const string KhrMaterialsDispersionNoVolume = "KHR_MATERIALS_DISPERSION_NO_VOLUME";

    /// <summary>The <c>KHR_MATERIALS_EMISSIVE_STRENGTH_ZERO_FACTOR</c> issue code.</summary>
    public const string KhrMaterialsEmissiveStrengthZeroFactor = "KHR_MATERIALS_EMISSIVE_STRENGTH_ZERO_FACTOR";

    /// <summary>The <c>KHR_MATERIALS_IRIDESCENCE_THICKNESS_RANGE_WITHOUT_TEXTURE</c> issue code.</summary>
    public const string KhrMaterialsIridescenceThicknessRangeWithoutTexture = "KHR_MATERIALS_IRIDESCENCE_THICKNESS_RANGE_WITHOUT_TEXTURE";

    /// <summary>The <c>KHR_MATERIALS_IRIDESCENCE_THICKNESS_TEXTURE_UNUSED</c> issue code.</summary>
    public const string KhrMaterialsIridescenceThicknessTextureUnused = "KHR_MATERIALS_IRIDESCENCE_THICKNESS_TEXTURE_UNUSED";

    /// <summary>The <c>KHR_MATERIALS_VARIANTS_NON_UNIQUE_VARIANT</c> issue code.</summary>
    public const string KhrMaterialsVariantsNonUniqueVariant = "KHR_MATERIALS_VARIANTS_NON_UNIQUE_VARIANT";

    /// <summary>The <c>KHR_MATERIALS_VOLUME_DOUBLE_SIDED</c> issue code.</summary>
    public const string KhrMaterialsVolumeDoubleSided = "KHR_MATERIALS_VOLUME_DOUBLE_SIDED";

    /// <summary>The <c>KHR_MATERIALS_VOLUME_NO_TRANSMISSION</c> issue code.</summary>
    public const string KhrMaterialsVolumeNoTransmission = "KHR_MATERIALS_VOLUME_NO_TRANSMISSION";

    /// <summary>The <c>MATERIAL_ALPHA_CUTOFF_INVALID_MODE</c> issue code.</summary>
    public const string MaterialAlphaCutoffInvalidMode = "MATERIAL_ALPHA_CUTOFF_INVALID_MODE";

    /// <summary>The <c>MESH_INVALID_WEIGHTS_COUNT</c> issue code.</summary>
    public const string MeshInvalidWeightsCount = "MESH_INVALID_WEIGHTS_COUNT";

    /// <summary>The <c>MESH_PRIMITIVES_UNEQUAL_TARGETS_COUNT</c> issue code.</summary>
    public const string MeshPrimitivesUnequalTargetsCount = "MESH_PRIMITIVES_UNEQUAL_TARGETS_COUNT";

    /// <summary>The <c>MESH_PRIMITIVE_ACCESSOR_UNALIGNED</c> issue code.</summary>
    public const string MeshPrimitiveAccessorUnaligned = "MESH_PRIMITIVE_ACCESSOR_UNALIGNED";

    /// <summary>The <c>MESH_PRIMITIVE_ACCESSOR_WITHOUT_BYTESTRIDE</c> issue code.</summary>
    public const string MeshPrimitiveAccessorWithoutBytestride = "MESH_PRIMITIVE_ACCESSOR_WITHOUT_BYTESTRIDE";

    /// <summary>The <c>MESH_PRIMITIVE_ATTRIBUTES_ACCESSOR_INVALID_FORMAT</c> issue code.</summary>
    public const string MeshPrimitiveAttributesAccessorInvalidFormat = "MESH_PRIMITIVE_ATTRIBUTES_ACCESSOR_INVALID_FORMAT";

    /// <summary>The <c>MESH_PRIMITIVE_ATTRIBUTES_ACCESSOR_UNSIGNED_INT</c> issue code.</summary>
    public const string MeshPrimitiveAttributesAccessorUnsignedInt = "MESH_PRIMITIVE_ATTRIBUTES_ACCESSOR_UNSIGNED_INT";

    /// <summary>The <c>MESH_PRIMITIVE_GENERATED_TANGENT_SPACE</c> issue code.</summary>
    public const string MeshPrimitiveGeneratedTangentSpace = "MESH_PRIMITIVE_GENERATED_TANGENT_SPACE";

    /// <summary>The <c>MESH_PRIMITIVE_INCOMPATIBLE_MODE</c> issue code.</summary>
    public const string MeshPrimitiveIncompatibleMode = "MESH_PRIMITIVE_INCOMPATIBLE_MODE";

    /// <summary>The <c>MESH_PRIMITIVE_INDEXED_SEMANTIC_CONTINUITY</c> issue code.</summary>
    public const string MeshPrimitiveIndexedSemanticContinuity = "MESH_PRIMITIVE_INDEXED_SEMANTIC_CONTINUITY";

    /// <summary>The <c>MESH_PRIMITIVE_INDICES_ACCESSOR_INVALID_FORMAT</c> issue code.</summary>
    public const string MeshPrimitiveIndicesAccessorInvalidFormat = "MESH_PRIMITIVE_INDICES_ACCESSOR_INVALID_FORMAT";

    /// <summary>The <c>MESH_PRIMITIVE_INDICES_ACCESSOR_WITH_BYTESTRIDE</c> issue code.</summary>
    public const string MeshPrimitiveIndicesAccessorWithBytestride = "MESH_PRIMITIVE_INDICES_ACCESSOR_WITH_BYTESTRIDE";

    /// <summary>The <c>MESH_PRIMITIVE_INVALID_ATTRIBUTE</c> issue code.</summary>
    public const string MeshPrimitiveInvalidAttribute = "MESH_PRIMITIVE_INVALID_ATTRIBUTE";

    /// <summary>The <c>MESH_PRIMITIVE_JOINTS_WEIGHTS_MISMATCH</c> issue code.</summary>
    public const string MeshPrimitiveJointsWeightsMismatch = "MESH_PRIMITIVE_JOINTS_WEIGHTS_MISMATCH";

    /// <summary>The <c>MESH_PRIMITIVE_MORPH_TARGET_INVALID_ATTRIBUTE_COUNT</c> issue code.</summary>
    public const string MeshPrimitiveMorphTargetInvalidAttributeCount = "MESH_PRIMITIVE_MORPH_TARGET_INVALID_ATTRIBUTE_COUNT";

    /// <summary>The <c>MESH_PRIMITIVE_MORPH_TARGET_NO_BASE_ACCESSOR</c> issue code.</summary>
    public const string MeshPrimitiveMorphTargetNoBaseAccessor = "MESH_PRIMITIVE_MORPH_TARGET_NO_BASE_ACCESSOR";

    /// <summary>The <c>MESH_PRIMITIVE_NO_POSITION</c> issue code.</summary>
    public const string MeshPrimitiveNoPosition = "MESH_PRIMITIVE_NO_POSITION";

    /// <summary>The <c>MESH_PRIMITIVE_NO_TANGENT_SPACE</c> issue code.</summary>
    public const string MeshPrimitiveNoTangentSpace = "MESH_PRIMITIVE_NO_TANGENT_SPACE";

    /// <summary>The <c>MESH_PRIMITIVE_POSITION_ACCESSOR_WITHOUT_BOUNDS</c> issue code.</summary>
    public const string MeshPrimitivePositionAccessorWithoutBounds = "MESH_PRIMITIVE_POSITION_ACCESSOR_WITHOUT_BOUNDS";

    /// <summary>The <c>MESH_PRIMITIVE_TANGENT_WITHOUT_NORMAL</c> issue code.</summary>
    public const string MeshPrimitiveTangentWithoutNormal = "MESH_PRIMITIVE_TANGENT_WITHOUT_NORMAL";

    /// <summary>The <c>MESH_PRIMITIVE_TOO_FEW_TEXCOORDS</c> issue code.</summary>
    public const string MeshPrimitiveTooFewTexcoords = "MESH_PRIMITIVE_TOO_FEW_TEXCOORDS";

    /// <summary>The <c>MESH_PRIMITIVE_UNEQUAL_ACCESSOR_COUNT</c> issue code.</summary>
    public const string MeshPrimitiveUnequalAccessorCount = "MESH_PRIMITIVE_UNEQUAL_ACCESSOR_COUNT";

    /// <summary>The <c>MULTIPLE_EXTENSIONS</c> issue code.</summary>
    public const string MultipleExtensions = "MULTIPLE_EXTENSIONS";

    /// <summary>The <c>NODE_EMPTY</c> issue code.</summary>
    public const string NodeEmpty = "NODE_EMPTY";

    /// <summary>The <c>NODE_LOOP</c> issue code.</summary>
    public const string NodeLoop = "NODE_LOOP";

    /// <summary>The <c>NODE_MATRIX_DEFAULT</c> issue code.</summary>
    public const string NodeMatrixDefault = "NODE_MATRIX_DEFAULT";

    /// <summary>The <c>NODE_MATRIX_NON_TRS</c> issue code.</summary>
    public const string NodeMatrixNonTrs = "NODE_MATRIX_NON_TRS";

    /// <summary>The <c>NODE_MATRIX_TRS</c> issue code.</summary>
    public const string NodeMatrixTrs = "NODE_MATRIX_TRS";

    /// <summary>The <c>NODE_PARENT_OVERRIDE</c> issue code.</summary>
    public const string NodeParentOverride = "NODE_PARENT_OVERRIDE";

    /// <summary>The <c>NODE_SKINNED_MESH_LOCAL_TRANSFORMS</c> issue code.</summary>
    public const string NodeSkinnedMeshLocalTransforms = "NODE_SKINNED_MESH_LOCAL_TRANSFORMS";

    /// <summary>The <c>NODE_SKINNED_MESH_NON_ROOT</c> issue code.</summary>
    public const string NodeSkinnedMeshNonRoot = "NODE_SKINNED_MESH_NON_ROOT";

    /// <summary>The <c>NODE_SKINNED_MESH_PARENT_TRANSFORMS</c> issue code.</summary>
    public const string NodeSkinnedMeshParentTransforms = "NODE_SKINNED_MESH_PARENT_TRANSFORMS";

    /// <summary>The <c>NODE_SKINNED_MESH_WITHOUT_SKIN</c> issue code.</summary>
    public const string NodeSkinnedMeshWithoutSkin = "NODE_SKINNED_MESH_WITHOUT_SKIN";

    /// <summary>The <c>NODE_SKIN_NO_SCENE</c> issue code.</summary>
    public const string NodeSkinNoScene = "NODE_SKIN_NO_SCENE";

    /// <summary>The <c>NODE_SKIN_WITH_NON_SKINNED_MESH</c> issue code.</summary>
    public const string NodeSkinWithNonSkinnedMesh = "NODE_SKIN_WITH_NON_SKINNED_MESH";

    /// <summary>The <c>NODE_WEIGHTS_INVALID</c> issue code.</summary>
    public const string NodeWeightsInvalid = "NODE_WEIGHTS_INVALID";

    /// <summary>The <c>NON_OBJECT_EXTRAS</c> issue code.</summary>
    public const string NonObjectExtras = "NON_OBJECT_EXTRAS";

    /// <summary>The <c>NON_RELATIVE_URI</c> issue code.</summary>
    public const string NonRelativeUri = "NON_RELATIVE_URI";

    /// <summary>The <c>NON_REQUIRED_EXTENSION</c> issue code.</summary>
    public const string NonRequiredExtension = "NON_REQUIRED_EXTENSION";

    /// <summary>The <c>ONE_OF_MISMATCH</c> issue code.</summary>
    public const string OneOfMismatch = "ONE_OF_MISMATCH";

    /// <summary>The <c>OPERATION_FAILED</c> issue code.</summary>
    public const string OperationFailed = "OPERATION_FAILED";

    /// <summary>The <c>OPERATION_SKIPPED</c> issue code.</summary>
    public const string OperationSkipped = "OPERATION_SKIPPED";

    /// <summary>The <c>OPERATION_UNAVAILABLE</c> issue code.</summary>
    public const string OperationUnavailable = "OPERATION_UNAVAILABLE";

    /// <summary>The <c>PATTERN_MISMATCH</c> issue code.</summary>
    public const string PatternMismatch = "PATTERN_MISMATCH";

    /// <summary>The <c>RESOURCE_LENGTH_MISMATCH</c> issue code.</summary>
    public const string ResourceLengthMismatch = "RESOURCE_LENGTH_MISMATCH";

    /// <summary>The <c>RESOURCE_LIMIT_EXCEEDED</c> issue code.</summary>
    public const string ResourceLimitExceeded = "RESOURCE_LIMIT_EXCEEDED";

    /// <summary>The <c>RESOURCE_POLICY_DENIED</c> issue code.</summary>
    public const string ResourcePolicyDenied = "RESOURCE_POLICY_DENIED";

    /// <summary>The <c>RESOURCE_UNAVAILABLE</c> issue code.</summary>
    public const string ResourceUnavailable = "RESOURCE_UNAVAILABLE";

    /// <summary>The <c>REQUIRED_EXTENSION_METADATA_UNAVAILABLE</c> issue code.</summary>
    public const string RequiredExtensionMetadataUnavailable = "REQUIRED_EXTENSION_METADATA_UNAVAILABLE";

    /// <summary>The <c>REQUIRED_EXTENSION_REMAP_UNAVAILABLE</c> issue code.</summary>
    public const string RequiredExtensionRemapUnavailable = "REQUIRED_EXTENSION_REMAP_UNAVAILABLE";

    /// <summary>The <c>REQUIRED_EXTENSION_RESOURCE_DECODE_UNAVAILABLE</c> issue code.</summary>
    public const string RequiredExtensionResourceDecodeUnavailable = "REQUIRED_EXTENSION_RESOURCE_DECODE_UNAVAILABLE";

    /// <summary>The <c>REQUIRED_EXTENSION_SEMANTICS_UNAVAILABLE</c> issue code.</summary>
    public const string RequiredExtensionSemanticsUnavailable = "REQUIRED_EXTENSION_SEMANTICS_UNAVAILABLE";

    /// <summary>The <c>ROTATION_NON_UNIT</c> issue code.</summary>
    public const string RotationNonUnit = "ROTATION_NON_UNIT";

    /// <summary>The <c>SCENE_NON_ROOT_NODE</c> issue code.</summary>
    public const string SceneNonRootNode = "SCENE_NON_ROOT_NODE";

    /// <summary>The <c>SKIN_IBM_ACCESSOR_WITH_BYTESTRIDE</c> issue code.</summary>
    public const string SkinIbmAccessorWithBytestride = "SKIN_IBM_ACCESSOR_WITH_BYTESTRIDE";

    /// <summary>The <c>SKIN_IBM_INVALID_FORMAT</c> issue code.</summary>
    public const string SkinIbmInvalidFormat = "SKIN_IBM_INVALID_FORMAT";

    /// <summary>The <c>SKIN_NO_COMMON_ROOT</c> issue code.</summary>
    public const string SkinNoCommonRoot = "SKIN_NO_COMMON_ROOT";

    /// <summary>The <c>SKIN_SKELETON_INVALID</c> issue code.</summary>
    public const string SkinSkeletonInvalid = "SKIN_SKELETON_INVALID";

    /// <summary>The <c>TEXTURE_INVALID_IMAGE_MIME_TYPE</c> issue code.</summary>
    public const string TextureInvalidImageMimeType = "TEXTURE_INVALID_IMAGE_MIME_TYPE";

    /// <summary>The <c>TRANSFORM_HOOK_UNAVAILABLE</c> issue code.</summary>
    public const string TransformHookUnavailable = "TRANSFORM_HOOK_UNAVAILABLE";

    /// <summary>The <c>TRANSFORM_UNSAFE_UNKNOWN_EXTENSION</c> issue code.</summary>
    public const string TransformUnsafeUnknownExtension = "TRANSFORM_UNSAFE_UNKNOWN_EXTENSION";

    /// <summary>The <c>TYPE_MISMATCH</c> issue code.</summary>
    public const string TypeMismatch = "TYPE_MISMATCH";

    /// <summary>The <c>UNDECLARED_EXTENSION</c> issue code.</summary>
    public const string UndeclaredExtension = "UNDECLARED_EXTENSION";

    /// <summary>The <c>UNDEFINED_PROPERTY</c> issue code.</summary>
    public const string UndefinedProperty = "UNDEFINED_PROPERTY";

    /// <summary>The <c>UNEXPECTED_EXTENSION_OBJECT</c> issue code.</summary>
    public const string UnexpectedExtensionObject = "UNEXPECTED_EXTENSION_OBJECT";

    /// <summary>The <c>UNEXPECTED_PROPERTY</c> issue code.</summary>
    public const string UnexpectedProperty = "UNEXPECTED_PROPERTY";

    /// <summary>The <c>UNKNOWN_ASSET_MAJOR_VERSION</c> issue code.</summary>
    public const string UnknownAssetMajorVersion = "UNKNOWN_ASSET_MAJOR_VERSION";

    /// <summary>The <c>UNKNOWN_ASSET_MINOR_VERSION</c> issue code.</summary>
    public const string UnknownAssetMinorVersion = "UNKNOWN_ASSET_MINOR_VERSION";

    /// <summary>The <c>UNRESOLVED_REFERENCE</c> issue code.</summary>
    public const string UnresolvedReference = "UNRESOLVED_REFERENCE";

    /// <summary>The <c>UNSATISFIED_DEPENDENCY</c> issue code.</summary>
    public const string UnsatisfiedDependency = "UNSATISFIED_DEPENDENCY";

    /// <summary>The <c>UNSUPPORTED_EXTENSION</c> issue code.</summary>
    public const string UnsupportedExtension = "UNSUPPORTED_EXTENSION";

    /// <summary>The <c>UNUSED_EXTENSION_REQUIRED</c> issue code.</summary>
    public const string UnusedExtensionRequired = "UNUSED_EXTENSION_REQUIRED";

    /// <summary>The <c>UNUSED_MESH_TANGENT</c> issue code.</summary>
    public const string UnusedMeshTangent = "UNUSED_MESH_TANGENT";

    /// <summary>The <c>UNUSED_MESH_WEIGHTS</c> issue code.</summary>
    public const string UnusedMeshWeights = "UNUSED_MESH_WEIGHTS";

    /// <summary>The <c>UNUSED_OBJECT</c> issue code.</summary>
    public const string UnusedObject = "UNUSED_OBJECT";

    /// <summary>The <c>URI_GLB</c> issue code.</summary>
    public const string UriGlb = "URI_GLB";

    /// <summary>The <c>VALUE_MULTIPLE_OF</c> issue code.</summary>
    public const string ValueMultipleOf = "VALUE_MULTIPLE_OF";

    /// <summary>The <c>VALUE_NOT_IN_LIST</c> issue code.</summary>
    public const string ValueNotInList = "VALUE_NOT_IN_LIST";

    /// <summary>The <c>VALUE_NOT_IN_RANGE</c> issue code.</summary>
    public const string ValueNotInRange = "VALUE_NOT_IN_RANGE";

    /// <summary>The <c>WRITE_PREFLIGHT_FAILED</c> issue code.</summary>
    public const string WritePreflightFailed = "WRITE_PREFLIGHT_FAILED";

    /// <summary>The <c>WRITE_SIZE_OVERFLOW</c> issue code.</summary>
    public const string WriteSizeOverflow = "WRITE_SIZE_OVERFLOW";
}
