using System.Text;

namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class MaterialTextureValidatorTests
{
    [Test]
    public void ValidateDocumentMaterialTextureRules_AcceptsValidCoreBindings()
    {
        GltfDocument document = new GltfDocument();
        GltfImage image = new GltfImage();
        image.Uri = "textures/albedo.png";
        image.MimeType = GltfMimeTypes.Png;
        image.Resolved = true;
        image.ResolvedBytes = BuildPng(2, 2, false, false);
        document.Images.Add(image);
        byte[] embeddedPng = BuildPng(4, 4, false, false);
        GltfBuffer buffer = new GltfBuffer();
        buffer.Resolved = true;
        buffer.ByteLength = embeddedPng.Length + 4;
        buffer.ResolvedBytes = new byte[buffer.ByteLength];
        Array.Copy(embeddedPng, 0, buffer.ResolvedBytes, 4, embeddedPng.Length);
        document.Buffers.Add(buffer);
        GltfBufferView view = new GltfBufferView();
        view.Buffer = 0;
        view.HasByteOffset = true;
        view.ByteOffset = 4;
        view.ByteLength = embeddedPng.Length;
        document.BufferViews.Add(view);
        GltfImage embedded = new GltfImage();
        embedded.HasBufferView = true;
        embedded.BufferView = 0;
        embedded.MimeType = GltfMimeTypes.Png;
        document.Images.Add(embedded);
        GltfSampler sampler = new GltfSampler();
        sampler.HasMagnificationFilter = true;
        sampler.MagnificationFilter = GltfSamplerFilter.Linear;
        sampler.HasMinificationFilter = true;
        sampler.MinificationFilter = GltfSamplerFilter.LinearMipmapLinear;
        sampler.HasWrapS = true;
        sampler.WrapS = GltfSamplerWrap.Repeat;
        sampler.HasWrapT = true;
        sampler.WrapT = GltfSamplerWrap.ClampToEdge;
        document.Samplers.Add(sampler);
        GltfTexture texture = new GltfTexture();
        texture.HasSampler = true;
        texture.Sampler = 0;
        texture.HasSource = true;
        texture.Source = 0;
        document.Textures.Add(texture);
        GltfMaterial material = new GltfMaterial();
        material.PbrMetallicRoughness = new GltfPbrMetallicRoughness();
        material.PbrMetallicRoughness.BaseColorTexture = new GltfTextureInfo();
        material.PbrMetallicRoughness.BaseColorTexture.Index = 0;
        document.Materials.Add(material);
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.HasMaterial = true;
        primitive.Material = 0;
        AddAttribute(primitive, "TEXCOORD_0");
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);

        GltfValidationReport report = new MaterialTextureValidator()
            .ValidateDocumentMaterialTextureRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [Test]
    public void ValidateDocumentMaterialTextureRules_ReportsMaterialFactorAndAlphaErrors()
    {
        GltfDocument document = new GltfDocument();
        GltfMaterial material = new GltfMaterial();
        material.PbrMetallicRoughness = new GltfPbrMetallicRoughness();
        material.PbrMetallicRoughness.HasBaseColorFactor = true;
        material.PbrMetallicRoughness.BaseColorFactor = new double[] { 1.0, 0.0, 0.0 };
        material.PbrMetallicRoughness.HasMetallicFactor = true;
        material.PbrMetallicRoughness.MetallicFactor = 1.5;
        material.PbrMetallicRoughness.HasRoughnessFactor = true;
        material.PbrMetallicRoughness.RoughnessFactor = double.NaN;
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

        GltfValidationReport report = new MaterialTextureValidator()
            .ValidateDocumentMaterialTextureRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.ArrayLengthNotInList,
                "/materials/0/pbrMetallicRoughness/baseColorFactor");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                "/materials/0/pbrMetallicRoughness/metallicFactor");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                "/materials/0/pbrMetallicRoughness/roughnessFactor");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                "/materials/0/emissiveFactor/1");
            AssertIssue(
                report,
                GltfIssueCodes.MaterialAlphaCutoffInvalidMode,
                "/materials/0/alphaCutoff");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                "/materials/0/normalTexture/texCoord");
            AssertIssue(
                report,
                GltfIssueCodes.ValueNotInRange,
                "/materials/0/occlusionTexture/strength");
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentMaterialTextureRules_RejectsIllegalSamplerConstants()
    {
        GltfDocument document = new GltfDocument();
        GltfSampler sampler = new GltfSampler();
        sampler.HasMagnificationFilter = true;
        sampler.MagnificationFilter = GltfSamplerFilter.LinearMipmapLinear;
        sampler.HasMinificationFilter = true;
        sampler.MinificationFilter = (GltfSamplerFilter)10;
        sampler.HasWrapS = true;
        sampler.WrapS = (GltfSamplerWrap)20;
        sampler.HasWrapT = true;
        sampler.WrapT = (GltfSamplerWrap)30;
        document.Samplers.Add(sampler);

        GltfValidationReport report = new MaterialTextureValidator()
            .ValidateDocumentMaterialTextureRules(document);

        Assert.Multiple(() =>
        {
            Assert.That(
                report.Issues.Count(issue => issue.Code == GltfIssueCodes.InvalidGlValue),
                Is.EqualTo(4));
            AssertIssue(report, GltfIssueCodes.InvalidGlValue, "/samplers/0/magFilter");
            AssertIssue(report, GltfIssueCodes.InvalidGlValue, "/samplers/0/minFilter");
            AssertIssue(report, GltfIssueCodes.InvalidGlValue, "/samplers/0/wrapS");
            AssertIssue(report, GltfIssueCodes.InvalidGlValue, "/samplers/0/wrapT");
            Assert.That(report.IsValid, Is.False);
        });
    }

    [Test]
    public void ValidateDocumentMaterialTextureRules_ValidatesImageSourcesHeadersAndFeatures()
    {
        GltfDocument document = new GltfDocument();
        GltfImage conflicting = new GltfImage();
        conflicting.Uri = "both.png";
        conflicting.HasBufferView = true;
        conflicting.BufferView = 0;
        document.Images.Add(conflicting);
        GltfImage mismatched = new GltfImage();
        mismatched.Uri = "profile.png";
        mismatched.MimeType = GltfMimeTypes.Jpeg;
        mismatched.Resolved = true;
        mismatched.ResolvedBytes = BuildPng(3, 5, true, true);
        document.Images.Add(mismatched);
        GltfImage truncated = new GltfImage();
        truncated.Uri = "short.png";
        truncated.MimeType = GltfMimeTypes.Png;
        truncated.Resolved = true;
        truncated.ResolvedBytes = new byte[] { 137, 80, 78, 71 };
        document.Images.Add(truncated);
        GltfImage extensionImage = new GltfImage();
        extensionImage.Uri = "texture.ktx2";
        extensionImage.MimeType = GltfMimeTypes.Ktx2;
        document.Images.Add(extensionImage);
        GltfImage invalidUri = new GltfImage();
        invalidUri.Uri = "bad%XZ.png";
        document.Images.Add(invalidUri);

        GltfValidationReport report = new MaterialTextureValidator()
            .ValidateDocumentMaterialTextureRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(report, GltfIssueCodes.OneOfMismatch, "/images/0");
            AssertIssue(report, GltfIssueCodes.UnsatisfiedDependency, "/images/0/mimeType");
            AssertIssue(report, GltfIssueCodes.ImageMimeTypeInvalid, "/images/1/mimeType");
            AssertIssue(report, GltfIssueCodes.ImageFeaturesUnsupported, "/images/1");
            AssertIssue(report, GltfIssueCodes.ImageNpotDimensions, "/images/1");
            AssertIssue(report, GltfIssueCodes.ImageUnexpectedEos, "/images/2");
            AssertIssue(report, GltfIssueCodes.ImageNonEnabledMimeType, "/images/3/mimeType");
            AssertIssue(report, GltfIssueCodes.InvalidUri, "/images/4/uri");
        });
    }

    [Test]
    public void ValidateDocumentMaterialTextureRules_CrossChecksTextureFormatsAndMeshBindings()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrTextureBasisu);
        GltfImage image = new GltfImage();
        image.Uri = "texture.ktx2";
        image.MimeType = GltfMimeTypes.Ktx2;
        document.Images.Add(image);
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
        GltfMesh mesh = new GltfMesh();
        GltfMeshPrimitive missingSpace = new GltfMeshPrimitive();
        missingSpace.HasMaterial = true;
        missingSpace.Material = 0;
        AddAttribute(missingSpace, GltfSemantics.Normal);
        mesh.Primitives.Add(missingSpace);
        GltfMeshPrimitive generatedSpace = new GltfMeshPrimitive();
        generatedSpace.HasMaterial = true;
        generatedSpace.Material = 0;
        AddAttribute(generatedSpace, GltfSemantics.Normal);
        AddAttribute(generatedSpace, "TEXCOORD_1");
        mesh.Primitives.Add(generatedSpace);
        GltfMeshPrimitive unusedTangent = new GltfMeshPrimitive();
        AddAttribute(unusedTangent, GltfSemantics.Tangent);
        mesh.Primitives.Add(unusedTangent);
        document.Meshes.Add(mesh);

        GltfValidationReport report = new MaterialTextureValidator()
            .ValidateDocumentMaterialTextureRules(document);

        Assert.Multiple(() =>
        {
            AssertIssue(
                report,
                GltfIssueCodes.TextureInvalidImageMimeType,
                "/textures/0/source");
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveTooFewTexcoords,
                "/meshes/0/primitives/0/attributes");
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveNoTangentSpace,
                "/meshes/0/primitives/0/attributes");
            AssertIssue(
                report,
                GltfIssueCodes.MeshPrimitiveGeneratedTangentSpace,
                "/meshes/0/primitives/1/attributes");
            AssertIssue(
                report,
                GltfIssueCodes.UnusedMeshTangent,
                "/meshes/0/primitives/2/attributes/TANGENT");
        });
    }

    private static void AddAttribute(GltfMeshPrimitive primitive, string semantic)
    {
        GltfAttribute attribute = new GltfAttribute();
        attribute.Semantic = semantic;
        attribute.Accessor = 0;
        primitive.Attributes.Add(attribute);
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

        AddPngChunk(bytes, "IEND", Array.Empty<byte>());
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

    private static void AssertIssue(
        GltfValidationReport report,
        string code,
        string pointer)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code && issue.JsonPointer == pointer),
            Is.True,
            $"Expected {code} at {pointer}; got "
                + string.Join(", ", report.Issues.Select(
                    issue => $"{issue.Code}@{issue.JsonPointer}")));
    }
}
