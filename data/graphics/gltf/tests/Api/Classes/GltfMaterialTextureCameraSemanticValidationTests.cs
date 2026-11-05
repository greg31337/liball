using System.Numerics;
using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfMaterialTextureCameraSemanticValidationTests
{
    [Test]
    public void ValidateDocument_AcceptsCorePbrTextureAndSamplerBindings()
    {
        GltfDocument document = new GltfDocument();
        document.Images.Add(CreateResolvedImage(
            "albedo.png",
            GltfMimeTypes.Png,
            BuildPng(4, 4, false, false)));
        GltfSampler sampler = new GltfSampler();
        sampler.HasMagnificationFilter = true;
        sampler.MagnificationFilter = GltfSamplerFilter.Linear;
        sampler.HasMinificationFilter = true;
        sampler.MinificationFilter = GltfSamplerFilter.LinearMipmapLinear;
        sampler.HasWrapS = true;
        sampler.WrapS = GltfSamplerWrap.MirroredRepeat;
        sampler.HasWrapT = true;
        sampler.WrapT = GltfSamplerWrap.ClampToEdge;
        document.Samplers.Add(sampler);
        GltfTexture texture = new GltfTexture();
        texture.HasSampler = true;
        texture.Sampler = 0;
        texture.HasSource = true;
        texture.Source = 0;
        document.Textures.Add(texture);

        GltfPbrMetallicRoughness pbr = new GltfPbrMetallicRoughness();
        pbr.HasBaseColorFactor = true;
        pbr.BaseColorFactor = new double[] { 0.2, 0.4, 0.6, 0.8 };
        pbr.HasMetallicFactor = true;
        pbr.MetallicFactor = 0.25;
        pbr.HasRoughnessFactor = true;
        pbr.RoughnessFactor = 0.75;
        pbr.BaseColorTexture = CreateTextureInfo(0, 0);
        pbr.MetallicRoughnessTexture = CreateTextureInfo(0, 1);
        GltfMaterial material = new GltfMaterial();
        material.PbrMetallicRoughness = pbr;
        material.HasEmissiveFactor = true;
        material.EmissiveFactor = new double[] { 0.1, 0.2, 0.3 };
        material.HasAlphaMode = true;
        material.AlphaMode = GltfAlphaMode.Mask;
        material.HasAlphaCutoff = true;
        material.AlphaCutoff = 0.4;
        material.NormalTexture = new GltfNormalTextureInfo();
        material.NormalTexture.Index = 0;
        material.NormalTexture.HasTextureCoordinate = true;
        material.NormalTexture.TextureCoordinate = 1;
        material.NormalTexture.HasScale = true;
        material.NormalTexture.Scale = 0.5;
        material.OcclusionTexture = new GltfOcclusionTextureInfo();
        material.OcclusionTexture.Index = 0;
        material.OcclusionTexture.HasStrength = true;
        material.OcclusionTexture.Strength = 0.75;
        material.EmissiveTexture = CreateTextureInfo(0, 0);
        document.Materials.Add(material);

        GltfValidationReport report = Validate(document, true);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True, FormatIssues(report));
            Assert.That(report.Issues, Is.Empty, FormatIssues(report));
        });
    }

    [Test]
    public void ValidateDocument_ReportsPbrAlphaAndSamplerViolations()
    {
        GltfDocument document = new GltfDocument();
        GltfPbrMetallicRoughness pbr = new GltfPbrMetallicRoughness();
        pbr.HasBaseColorFactor = true;
        pbr.BaseColorFactor = new double[] { 1.0, 0.0, 0.0 };
        pbr.HasMetallicFactor = true;
        pbr.MetallicFactor = 1.5;
        pbr.HasRoughnessFactor = true;
        pbr.RoughnessFactor = double.NaN;
        GltfMaterial material = new GltfMaterial();
        material.PbrMetallicRoughness = pbr;
        material.HasEmissiveFactor = true;
        material.EmissiveFactor = new double[] { 0.0, -0.1, 0.0 };
        material.HasAlphaCutoff = true;
        material.AlphaCutoff = -1.0;
        material.NormalTexture = new GltfNormalTextureInfo();
        material.NormalTexture.HasTextureCoordinate = true;
        material.NormalTexture.TextureCoordinate = -1;
        material.NormalTexture.HasScale = true;
        material.NormalTexture.Scale = double.PositiveInfinity;
        material.OcclusionTexture = new GltfOcclusionTextureInfo();
        material.OcclusionTexture.HasStrength = true;
        material.OcclusionTexture.Strength = 2.0;
        document.Materials.Add(material);

        GltfSampler sampler = new GltfSampler();
        sampler.HasMagnificationFilter = true;
        sampler.MagnificationFilter = GltfSamplerFilter.LinearMipmapLinear;
        sampler.HasMinificationFilter = true;
        sampler.MinificationFilter = (GltfSamplerFilter)10;
        sampler.HasWrapS = true;
        sampler.WrapS = (GltfSamplerWrap)20;
        document.Samplers.Add(sampler);

        GltfValidationReport report = Validate(document, false);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.ArrayLengthNotInList,
                GltfValidationSeverity.Error,
                "/materials/0/pbrMetallicRoughness/baseColorFactor");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/materials/0/pbrMetallicRoughness/metallicFactor");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/materials/0/pbrMetallicRoughness/roughnessFactor");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/materials/0/emissiveFactor/1");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/materials/0/alphaCutoff");
            AssertIssue(
                report,
                GltfIssueCodes.MaterialAlphaCutoffInvalidMode,
                GltfValidationSeverity.Warning,
                "/materials/0/alphaCutoff");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/materials/0/normalTexture/texCoord");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/materials/0/normalTexture/scale");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/materials/0/occlusionTexture/strength");
            AssertIssue(
                report,
                GltfIssueCodes.InvalidGlValue,
                GltfValidationSeverity.Error,
                "/samplers/0/magFilter");
            AssertIssue(
                report,
                GltfIssueCodes.InvalidGlValue,
                GltfValidationSeverity.Error,
                "/samplers/0/minFilter");
            AssertIssue(
                report,
                GltfIssueCodes.InvalidGlValue,
                GltfValidationSeverity.Error,
                "/samplers/0/wrapS");
        });
    }

    [Test]
    public void ValidateDocument_CrossChecksImageMimeHeadersAndFeatures()
    {
        GltfDocument document = new GltfDocument();
        document.Images.Add(CreateResolvedImage(
            "profile.png",
            GltfMimeTypes.Jpeg,
            BuildPng(3, 5, true, true)));
        document.Images.Add(CreateResolvedImage(
            "short.png",
            GltfMimeTypes.Png,
            new byte[] { 137, 80, 78, 71 }));
        document.Images.Add(CreateResolvedImage(
            "unknown.bin",
            null,
            new byte[] { 1, 2, 3, 4 }));
        GltfImage conflicting = new GltfImage();
        conflicting.Uri = "both.png";
        conflicting.HasBufferView = true;
        conflicting.BufferView = 0;
        document.Images.Add(conflicting);

        GltfValidationReport report = Validate(document, false);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.ImageMimeTypeInvalid,
                GltfValidationSeverity.Error,
                "/images/0/mimeType");
            AssertIssue(
                report,
                GltfIssueCodes.ImageFeaturesUnsupported,
                GltfValidationSeverity.Warning,
                "/images/0");
            AssertIssue(
                report,
                GltfIssueCodes.ImageNpotDimensions,
                GltfValidationSeverity.Information,
                "/images/0");
            AssertIssue(
                report,
                GltfIssueCodes.ImageUnexpectedEos,
                GltfValidationSeverity.Error,
                "/images/1");
            AssertIssue(
                report,
                GltfIssueCodes.ImageUnrecognizedFormat,
                GltfValidationSeverity.Warning,
                "/images/2");
            AssertIssue(
                report,
                GltfIssueCodes.OneOfMismatch,
                GltfValidationSeverity.Error,
                "/images/3");
            AssertIssue(
                report,
                GltfIssueCodes.UnsatisfiedDependency,
                GltfValidationSeverity.Error,
                "/images/3/mimeType");
        });
    }

    [TestCase(GltfExtensionNames.KhrTextureBasisu, GltfMimeTypes.Ktx2)]
    [TestCase(GltfExtensionNames.ExtTextureWebp, GltfMimeTypes.WebP)]
    public void ValidateDocument_AcceptsRequiredAndOptionalExtensionImageFallbacks(
        string extension,
        string mimeType)
    {
        GltfDocument optional = CreateExtensionImageDocument(
            extension,
            mimeType,
            false,
            true);
        GltfDocument required = CreateExtensionImageDocument(
            extension,
            mimeType,
            true,
            false);
        GltfDocument missingFallback = CreateExtensionImageDocument(
            extension,
            mimeType,
            false,
            false);

        GltfValidationReport optionalReport = Validate(optional, true);
        GltfValidationReport requiredReport = Validate(required, true);
        GltfValidationReport missingReport = Validate(missingFallback, true);

        Assert.Multiple(() =>
        {
            Assert.That(optionalReport.IsValid, Is.True, FormatIssues(optionalReport));
            Assert.That(requiredReport.IsValid, Is.True, FormatIssues(requiredReport));
            AssertNoIssue(optionalReport, GltfIssueCodes.TextureInvalidImageMimeType);
            AssertNoIssue(requiredReport, GltfIssueCodes.TextureInvalidImageMimeType);
            AssertIssue(
                missingReport,
                GltfIssueCodes.UnsatisfiedDependency,
                GltfValidationSeverity.Error,
                "/extensionsRequired");
        });
    }

    [Test]
    public void ValidateDocument_ChecksMaterialTextureCoordinatesAndTangentSpace()
    {
        GltfDocument document = new GltfDocument();
        document.Images.Add(CreateResolvedImage(
            "normal.png",
            GltfMimeTypes.Png,
            BuildPng(2, 2, false, false)));
        GltfTexture texture = new GltfTexture();
        texture.HasSource = true;
        texture.Source = 0;
        document.Textures.Add(texture);
        GltfMaterial material = new GltfMaterial();
        material.NormalTexture = new GltfNormalTextureInfo();
        material.NormalTexture.Index = 0;
        material.NormalTexture.HasTextureCoordinate = true;
        material.NormalTexture.TextureCoordinate = 1;
        document.Materials.Add(material);

        AddMeshAccessors(document);
        GltfMesh mesh = new GltfMesh();
        GltfMeshPrimitive missing = CreatePrimitive(true, false, false, false);
        GltfMeshPrimitive generated = CreatePrimitive(true, true, false, true);
        GltfMeshPrimitive unused = CreatePrimitive(false, true, true, true);
        mesh.Primitives.Add(missing);
        mesh.Primitives.Add(generated);
        mesh.Primitives.Add(unused);
        document.Meshes.Add(mesh);

        GltfValidationReport report = Validate(document, false);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveTooFewTexcoords,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/attributes");
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveNoTangentSpace,
                GltfValidationSeverity.Error,
                "/meshes/0/primitives/0/attributes");
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveGeneratedTangentSpace,
                GltfValidationSeverity.Warning,
                "/meshes/0/primitives/1/attributes");
            AssertIssue(
                report,
                GltfIssueCodes.UnusedMeshTangent,
                GltfValidationSeverity.Information,
                "/meshes/0/primitives/2/attributes/TANGENT");
        });
    }

    [Test]
    public void ValidateDocument_RejectsNonFiniteCamerasAndCreatesFiniteProjections()
    {
        GltfDocument valid = new GltfDocument();
        GltfPerspectiveCamera perspective = new GltfPerspectiveCamera();
        perspective.HasAspectRatio = true;
        perspective.AspectRatio = 2.0;
        perspective.VerticalFieldOfView = Math.PI / 2.0;
        perspective.NearPlane = 1.0;
        perspective.HasFarPlane = true;
        perspective.FarPlane = 101.0;
        GltfCamera perspectiveCamera = new GltfCamera();
        perspectiveCamera.Type = GltfCameraType.Perspective;
        perspectiveCamera.Perspective = perspective;
        valid.Cameras.Add(perspectiveCamera);
        GltfOrthographicCamera orthographic = new GltfOrthographicCamera();
        orthographic.HorizontalMagnification = 4.0;
        orthographic.VerticalMagnification = 2.0;
        orthographic.NearPlane = 1.0;
        orthographic.FarPlane = 11.0;
        GltfCamera orthographicCamera = new GltfCamera();
        orthographicCamera.Type = GltfCameraType.Orthographic;
        orthographicCamera.Orthographic = orthographic;
        valid.Cameras.Add(orthographicCamera);

        GltfValidationReport validReport = Validate(valid, false);
        IGltfSceneEvaluator evaluator = new GltfSceneEvaluator();
        GltfProjectionResult perspectiveResult = evaluator.CalculateProjection(valid, 0);
        GltfProjectionResult orthographicResult = evaluator.CalculateProjection(valid, 1);

        GltfDocument invalid = new GltfDocument();
        GltfPerspectiveCamera invalidPerspective = new GltfPerspectiveCamera();
        invalidPerspective.HasAspectRatio = true;
        invalidPerspective.AspectRatio = double.NaN;
        invalidPerspective.VerticalFieldOfView = double.PositiveInfinity;
        invalidPerspective.NearPlane = 0.0;
        invalidPerspective.HasFarPlane = true;
        invalidPerspective.FarPlane = 0.0;
        GltfCamera invalidPerspectiveCamera = new GltfCamera();
        invalidPerspectiveCamera.Perspective = invalidPerspective;
        invalid.Cameras.Add(invalidPerspectiveCamera);
        GltfOrthographicCamera invalidOrthographic = new GltfOrthographicCamera();
        invalidOrthographic.HorizontalMagnification = double.NegativeInfinity;
        invalidOrthographic.VerticalMagnification = 0.0;
        invalidOrthographic.NearPlane = 2.0;
        invalidOrthographic.FarPlane = 1.0;
        GltfCamera invalidOrthographicCamera = new GltfCamera();
        invalidOrthographicCamera.Type = GltfCameraType.Orthographic;
        invalidOrthographicCamera.Orthographic = invalidOrthographic;
        invalid.Cameras.Add(invalidOrthographicCamera);

        GltfValidationReport invalidReport = Validate(invalid, false);

        Assert.Multiple(() =>
        {
            Assert.That(validReport.IsValid, Is.True, FormatIssues(validReport));
            Assert.That(perspectiveResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(orthographicResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(IsFiniteMatrix(perspectiveResult.Projection), Is.True);
            Assert.That(IsFiniteMatrix(orthographicResult.Projection), Is.True);
            AssertIssue(
                invalidReport,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/cameras/0/perspective/aspectRatio");
            AssertIssue(
                invalidReport,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/cameras/0/perspective/yfov");
            AssertIssue(
                invalidReport,
                GltfIssueCodes.ValueNotInRange,
                GltfValidationSeverity.Error,
                "/cameras/1/orthographic/xmag");
            AssertIssue(
                invalidReport,
                GltfIssueCodes.CameraXmagYmagZero,
                GltfValidationSeverity.Error,
                "/cameras/1/orthographic/ymag");
            AssertIssue(
                invalidReport,
                GltfIssueCodes.CameraZfarLequalZnear,
                GltfValidationSeverity.Error,
                "/cameras/1/orthographic/zfar");
        });
    }

    private static GltfDocument CreateExtensionImageDocument(
        string extension,
        string mimeType,
        bool required,
        bool coreFallback)
    {
        GltfDocument result = new GltfDocument();
        result.ExtensionsUsed.Add(extension);
        if (required)
        {
            result.ExtensionsRequired.Add(extension);
        }

        int extensionImage = 0;
        if (coreFallback)
        {
            result.Images.Add(CreateResolvedImage(
                "fallback.png",
                GltfMimeTypes.Png,
                BuildPng(2, 2, false, false)));
            extensionImage = 1;
        }

        result.Images.Add(CreateResolvedImage(
            extension == GltfExtensionNames.KhrTextureBasisu
                ? "texture.ktx2"
                : "texture.webp",
            mimeType,
            CreateExtensionImageBytes(extension)));
        GltfTexture texture = new GltfTexture();
        if (coreFallback)
        {
            texture.HasSource = true;
            texture.Source = 0;
        }

        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        if (extension == GltfExtensionNames.KhrTextureBasisu)
        {
            typed.TextureBasisu = new GltfTextureBasisuExtension();
            typed.TextureBasisu.HasSource = true;
            typed.TextureBasisu.Source = extensionImage;
        }
        else
        {
            typed.TextureWebp = new GltfTextureWebpExtension();
            typed.TextureWebp.HasSource = true;
            typed.TextureWebp.Source = extensionImage;
        }

        texture.PropertyJson.TypedExtensions[extension] = typed;
        result.Textures.Add(texture);
        return result;
    }

    private static GltfImage CreateResolvedImage(
        string uri,
        string? mimeType,
        byte[] bytes)
    {
        GltfImage result = new GltfImage();
        result.Uri = uri;
        result.MimeType = mimeType;
        result.Resolved = true;
        result.ResolvedBytes = bytes;
        return result;
    }

    private static GltfTextureInfo CreateTextureInfo(int texture, int coordinate)
    {
        GltfTextureInfo result = new GltfTextureInfo();
        result.Index = texture;
        result.HasTextureCoordinate = true;
        result.TextureCoordinate = coordinate;
        return result;
    }

    private static void AddMeshAccessors(GltfDocument document)
    {
        GltfAccessor position = new GltfAccessor();
        position.Type = GltfAccessorType.Vector3;
        position.ComponentType = GltfComponentType.Float;
        position.Count = 3;
        position.Minimum = new double[] { 0.0, 0.0, 0.0 };
        position.Maximum = new double[] { 1.0, 1.0, 1.0 };
        document.Accessors.Add(position);
        document.Accessors.Add(CreateAccessor(GltfAccessorType.Vector3));
        document.Accessors.Add(CreateAccessor(GltfAccessorType.Vector4));
        document.Accessors.Add(CreateAccessor(GltfAccessorType.Vector2));
    }

    private static GltfAccessor CreateAccessor(GltfAccessorType type)
    {
        GltfAccessor result = new GltfAccessor();
        result.Type = type;
        result.ComponentType = GltfComponentType.Float;
        result.Count = 3;
        return result;
    }

    private static GltfMeshPrimitive CreatePrimitive(
        bool material,
        bool normal,
        bool tangent,
        bool textureCoordinate)
    {
        GltfMeshPrimitive result = new GltfMeshPrimitive();
        if (material)
        {
            result.HasMaterial = true;
            result.Material = 0;
        }

        AddAttribute(result, GltfSemantics.Position, 0);
        if (normal)
        {
            AddAttribute(result, GltfSemantics.Normal, 1);
        }

        if (tangent)
        {
            AddAttribute(result, GltfSemantics.Tangent, 2);
        }

        if (textureCoordinate)
        {
            AddAttribute(result, "TEXCOORD_1", 3);
        }

        return result;
    }

    private static void AddAttribute(
        GltfMeshPrimitive primitive,
        string semantic,
        int accessor)
    {
        GltfAttribute attribute = new GltfAttribute();
        attribute.Semantic = semantic;
        attribute.Accessor = accessor;
        primitive.Attributes.Add(attribute);
    }

    private static byte[] CreateExtensionImageBytes(string extension)
    {
        if (extension == GltfExtensionNames.KhrTextureBasisu)
        {
            return new byte[]
            {
                0xAB, 0x4B, 0x54, 0x58,
                0x20, 0x32, 0x30, 0xBB,
                0x0D, 0x0A, 0x1A, 0x0A
            };
        }

        return new byte[]
        {
            (byte)'R', (byte)'I', (byte)'F', (byte)'F',
            4, 0, 0, 0,
            (byte)'W', (byte)'E', (byte)'B', (byte)'P'
        };
    }

    private static byte[] BuildPng(
        int width,
        int height,
        bool animated,
        bool colorProfile)
    {
        List<byte> bytes = new List<byte>(new byte[]
        {
            137, 80, 78, 71, 13, 10, 26, 10
        });
        byte[] header = new byte[13];
        WriteUInt32BigEndian(header, 0, width);
        WriteUInt32BigEndian(header, 4, height);
        header[8] = 8;
        header[9] = 6;
        AddPngChunk(bytes, "IHDR", header);
        if (animated)
        {
            AddPngChunk(bytes, "acTL", new byte[8]);
        }

        if (colorProfile)
        {
            AddPngChunk(bytes, "iCCP", new byte[] { 0 });
        }

        AddPngChunk(bytes, "IEND", []);
        return bytes.ToArray();
    }

    private static void AddPngChunk(List<byte> bytes, string type, byte[] data)
    {
        byte[] length = new byte[4];
        WriteUInt32BigEndian(length, 0, data.Length);
        bytes.AddRange(length);
        bytes.AddRange(Encoding.ASCII.GetBytes(type));
        bytes.AddRange(data);
        bytes.AddRange(new byte[4]);
    }

    private static void WriteUInt32BigEndian(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 24) & 0xFF);
        bytes[offset + 1] = (byte)((value >> 16) & 0xFF);
        bytes[offset + 2] = (byte)((value >> 8) & 0xFF);
        bytes[offset + 3] = (byte)(value & 0xFF);
    }

    private static bool IsFiniteMatrix(Matrix4x4 value)
    {
        return float.IsFinite(value.M11)
            && float.IsFinite(value.M12)
            && float.IsFinite(value.M13)
            && float.IsFinite(value.M14)
            && float.IsFinite(value.M21)
            && float.IsFinite(value.M22)
            && float.IsFinite(value.M23)
            && float.IsFinite(value.M24)
            && float.IsFinite(value.M31)
            && float.IsFinite(value.M32)
            && float.IsFinite(value.M33)
            && float.IsFinite(value.M34)
            && float.IsFinite(value.M41)
            && float.IsFinite(value.M42)
            && float.IsFinite(value.M43)
            && float.IsFinite(value.M44);
    }

    private static GltfValidationReport Validate(
        GltfDocument document,
        bool validateExtensions)
    {
        GltfValidationOptions options = new GltfValidationOptions();
        options.ValidateStructure = false;
        options.ValidateResources = false;
        options.ValidateExtensions = validateExtensions;
        return new GltfValidator().ValidateDocument(document, options);
    }

    private static void AssertNoIssue(GltfValidationReport report, string code)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code),
            Is.False,
            FormatIssues(report));
    }

    private static void AssertIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string pointer)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code
                && issue.Severity == severity
                && issue.JsonPointer == pointer),
            Is.True,
            $"Expected {severity} {code} at '{pointer}', got: {FormatIssues(report)}");
    }

    private static string FormatIssues(GltfValidationReport report)
    {
        return string.Join(
            ", ",
            report.Issues.Select(
                issue => $"{issue.Severity}:{issue.Code}@{issue.JsonPointer}"));
    }
}
