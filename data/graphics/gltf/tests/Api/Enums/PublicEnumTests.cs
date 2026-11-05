namespace gltf.Tests.Api.Enums;

[TestFixture]
public class PublicEnumTests
{
    [Test]
    public void PublicEnumInventory_ContainsEveryExpectedEnum()
    {
        string[] expected =
        [
            nameof(GltfAccessorType),
            nameof(GltfAlphaMode),
            nameof(GltfAnimationInterpolation),
            nameof(GltfAnimationPath),
            nameof(GltfAnimationWrapMode),
            nameof(GltfBufferViewTarget),
            nameof(GltfCameraType),
            nameof(GltfComponentType),
            nameof(GltfContainerFormat),
            nameof(GltfConversionDecisionAction),
            nameof(GltfConversionDecisionKind),
            nameof(GltfDuplicateMemberPolicy),
            nameof(GltfExtensionStatus),
            nameof(GltfImageFormat),
            nameof(GltfOperationStatus),
            nameof(GltfPrimitiveMode),
            nameof(GltfResourceKind),
            nameof(GltfResourceMode),
            nameof(GltfSamplerFilter),
            nameof(GltfSamplerWrap),
            nameof(GltfTransformKind),
            nameof(GltfUriKind),
            nameof(GltfValidationSeverity)
        ];
        string[] actual = typeof(GltfDocument).Assembly.GetExportedTypes()
            .Where(type => type.IsEnum)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ContainerFormatValues_AreStable()
    {
        AssertValues(
            new[] { GltfContainerFormat.Auto, GltfContainerFormat.Json, GltfContainerFormat.Glb },
            new[] { 0, 1, 2 });
    }

    [Test]
    public void DuplicateMemberPolicyValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfDuplicateMemberPolicy.Reject,
                GltfDuplicateMemberPolicy.KeepFirst,
                GltfDuplicateMemberPolicy.KeepLast
            },
            new[] { 0, 1, 2 });
    }

    [Test]
    public void ConversionDecisionKindValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfConversionDecisionKind.Extension,
                GltfConversionDecisionKind.Codec
            },
            new[] { 0, 1 });
    }

    [Test]
    public void ConversionDecisionActionValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfConversionDecisionAction.Preserved,
                GltfConversionDecisionAction.Added,
                GltfConversionDecisionAction.Removed,
                GltfConversionDecisionAction.Available,
                GltfConversionDecisionAction.Unavailable,
                GltfConversionDecisionAction.Applied
            },
            new[] { 0, 1, 2, 3, 4, 5 });
    }


    [Test]
    public void ComponentTypeValues_MatchGltfConstants()
    {
        AssertValues(
            new[]
            {
                GltfComponentType.SignedByte,
                GltfComponentType.UnsignedByte,
                GltfComponentType.SignedShort,
                GltfComponentType.UnsignedShort,
                GltfComponentType.UnsignedInt,
                GltfComponentType.Float
            },
            new[] { 5120, 5121, 5122, 5123, 5125, 5126 });
    }

    [Test]
    public void AccessorTypeValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfAccessorType.Scalar,
                GltfAccessorType.Vector2,
                GltfAccessorType.Vector3,
                GltfAccessorType.Vector4,
                GltfAccessorType.Matrix2,
                GltfAccessorType.Matrix3,
                GltfAccessorType.Matrix4
            },
            new[] { 0, 1, 2, 3, 4, 5, 6 });
    }

    [Test]
    public void PrimitiveModeValues_MatchGltfConstants()
    {
        AssertValues(
            new[]
            {
                GltfPrimitiveMode.Points,
                GltfPrimitiveMode.Lines,
                GltfPrimitiveMode.LineLoop,
                GltfPrimitiveMode.LineStrip,
                GltfPrimitiveMode.Triangles,
                GltfPrimitiveMode.TriangleStrip,
                GltfPrimitiveMode.TriangleFan
            },
            new[] { 0, 1, 2, 3, 4, 5, 6 });
    }

    [Test]
    public void BufferViewTargetValues_MatchGltfConstants()
    {
        AssertValues(
            new[]
            {
                GltfBufferViewTarget.None,
                GltfBufferViewTarget.ArrayBuffer,
                GltfBufferViewTarget.ElementArrayBuffer
            },
            new[] { 0, 34962, 34963 });
    }

    [Test]
    public void AlphaModeValues_AreStable()
    {
        AssertValues(
            new[] { GltfAlphaMode.Opaque, GltfAlphaMode.Mask, GltfAlphaMode.Blend },
            new[] { 0, 1, 2 });
    }

    [Test]
    public void SamplerFilterValues_MatchGltfConstants()
    {
        AssertValues(
            new[]
            {
                GltfSamplerFilter.Nearest,
                GltfSamplerFilter.Linear,
                GltfSamplerFilter.NearestMipmapNearest,
                GltfSamplerFilter.LinearMipmapNearest,
                GltfSamplerFilter.NearestMipmapLinear,
                GltfSamplerFilter.LinearMipmapLinear
            },
            new[] { 9728, 9729, 9984, 9985, 9986, 9987 });
    }

    [Test]
    public void SamplerWrapValues_MatchGltfConstants()
    {
        AssertValues(
            new[]
            {
                GltfSamplerWrap.ClampToEdge,
                GltfSamplerWrap.MirroredRepeat,
                GltfSamplerWrap.Repeat
            },
            new[] { 33071, 33648, 10497 });
    }

    [Test]
    public void CameraTypeValues_AreStable()
    {
        AssertValues(
            new[] { GltfCameraType.Perspective, GltfCameraType.Orthographic },
            new[] { 0, 1 });
    }

    [Test]
    public void AnimationInterpolationValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfAnimationInterpolation.Linear,
                GltfAnimationInterpolation.Step,
                GltfAnimationInterpolation.CubicSpline
            },
            new[] { 0, 1, 2 });
    }

    [Test]
    public void AnimationPathValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfAnimationPath.Translation,
                GltfAnimationPath.Rotation,
                GltfAnimationPath.Scale,
                GltfAnimationPath.Weights,
                GltfAnimationPath.Pointer
            },
            new[] { 0, 1, 2, 3, 4 });
    }

    [Test]
    public void ValidationSeverityValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfValidationSeverity.Information,
                GltfValidationSeverity.Warning,
                GltfValidationSeverity.Error
            },
            new[] { 0, 1, 2 });
    }

    [Test]
    public void UriKindValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfUriKind.Absent,
                GltfUriKind.Relative,
                GltfUriKind.Absolute,
                GltfUriKind.File,
                GltfUriKind.Data,
                GltfUriKind.Network,
                GltfUriKind.Custom
            },
            new[] { 0, 1, 2, 3, 4, 5, 6 });
    }

    [Test]
    public void ResourceKindValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfResourceKind.Buffer,
                GltfResourceKind.Image,
                GltfResourceKind.Schema,
                GltfResourceKind.Extension,
                GltfResourceKind.Other
            },
            new[] { 0, 1, 2, 3, 4 });
    }

    [Test]
    public void ImageFormatValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfImageFormat.Unknown,
                GltfImageFormat.Png,
                GltfImageFormat.Jpeg,
                GltfImageFormat.Ktx2,
                GltfImageFormat.WebP,
                GltfImageFormat.Avif,
                GltfImageFormat.Dds,
                GltfImageFormat.Astc,
                GltfImageFormat.Custom
            },
            new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 });
    }

    [Test]
    public void ResourceModeValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfResourceMode.Preserve,
                GltfResourceMode.External,
                GltfResourceMode.DataUri,
                GltfResourceMode.BufferView,
                GltfResourceMode.GlbBin
            },
            new[] { 0, 1, 2, 3, 4 });
    }

    [Test]
    public void ExtensionStatusValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfExtensionStatus.Unknown,
                GltfExtensionStatus.Vendor,
                GltfExtensionStatus.MultiVendor,
                GltfExtensionStatus.Draft,
                GltfExtensionStatus.ReleaseCandidate,
                GltfExtensionStatus.Ratified,
                GltfExtensionStatus.Archived
            },
            new[] { 0, 1, 2, 3, 4, 5, 6 });
    }

    [Test]
    public void TransformKindValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfTransformKind.Prune,
                GltfTransformKind.Deduplicate,
                GltfTransformKind.Canonicalize,
                GltfTransformKind.Weld,
                GltfTransformKind.Unweld,
                GltfTransformKind.JoinPrimitives,
                GltfTransformKind.SplitPrimitives,
                GltfTransformKind.CreateGpuInstances,
                GltfTransformKind.ApplyNodeTransform,
                GltfTransformKind.BakeNodeTransforms,
                GltfTransformKind.FlattenHierarchy,
                GltfTransformKind.CenterScene,
                GltfTransformKind.GroundScene,
                GltfTransformKind.FlipWinding,
                GltfTransformKind.ConvertUnits,
                GltfTransformKind.ConvertAxes,
                GltfTransformKind.GenerateBounds,
                GltfTransformKind.GenerateNormals,
                GltfTransformKind.RemoveRedundantAnimationKeys,
                GltfTransformKind.Quantize,
                GltfTransformKind.Dequantize,
                GltfTransformKind.DenseToSparse,
                GltfTransformKind.SparseToDense,
                GltfTransformKind.ConsolidateBuffers,
                GltfTransformKind.PartitionBuffers,
                GltfTransformKind.PackBuffers,
                GltfTransformKind.EmbedResources,
                GltfTransformKind.ExtractResources,
                GltfTransformKind.GenerateTangents,
                GltfTransformKind.GenerateTextureCoordinates,
                GltfTransformKind.ReorderVertices,
                GltfTransformKind.Simplify,
                GltfTransformKind.GenerateLod,
                GltfTransformKind.EncodeDraco,
                GltfTransformKind.DecodeDraco,
                GltfTransformKind.EncodeMeshopt,
                GltfTransformKind.DecodeMeshopt,
                GltfTransformKind.ConvertImages,
                GltfTransformKind.CreatePalette,
                GltfTransformKind.Custom
            },
            new[]
            {
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
                10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
                20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
                30, 31, 32, 33, 34, 35, 36, 37, 38, 39
            });
    }

    [Test]
    public void AnimationWrapModeValues_AreStable()
    {
        AssertValues(
            new[] { GltfAnimationWrapMode.Clamp, GltfAnimationWrapMode.Loop },
            new[] { 0, 1 });
    }

    [Test]
    public void OperationStatusValues_AreStable()
    {
        AssertValues(
            new[]
            {
                GltfOperationStatus.Success,
                GltfOperationStatus.Skipped,
                GltfOperationStatus.Unavailable,
                GltfOperationStatus.Invalid,
                GltfOperationStatus.Failed
            },
            new[] { 0, 1, 2, 3, 4 });
    }

    private static void AssertValues<TEnum>(TEnum[] values, int[] expectedValues)
        where TEnum : struct, Enum
    {
        Assert.That(Enum.GetValues<TEnum>(), Has.Length.EqualTo(values.Length));
        Assert.That(values, Has.Length.EqualTo(expectedValues.Length));

        for (int index = 0; index < values.Length; index++)
        {
            Assert.That(Convert.ToInt32(values[index]), Is.EqualTo(expectedValues[index]));
        }
    }
}
