using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Validation;

[TestFixture]
public class StructuralValidatorTests
{
    [Test]
    public void ValidateParsedStructure_ReportsExactTypeRangeAndEscapedPointers()
    {
        string source =
            "{\"asset\":{\"version\":2},\"buffers\":[{\"byteLength\":0}],"
            + "\"extensions\":{\"EXT/bad\":5}}";
        GltfJsonReadResult parsed = ParseJson(source);

        GltfValidationReport report = new StructuralValidator().ValidateParsedStructure(parsed);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            AssertIssue(report, GltfIssueCodes.TypeMismatch, "/asset/version");
            AssertIssue(report, GltfIssueCodes.ValueNotInRange, "/buffers/0/byteLength");
            AssertIssue(report, GltfIssueCodes.TypeMismatch, "/extensions/EXT~1bad");
            Assert.That(FindIssue(report, "/asset/version").HasByteOffset, Is.True);
            Assert.That(FindIssue(report, "/asset/version").ByteOffset,
                Is.EqualTo(source.IndexOf("\"version\"", StringComparison.Ordinal)));
            Assert.That(FindIssue(report, "/buffers/0/byteLength").ByteOffset,
                Is.EqualTo(source.IndexOf("\"byteLength\"", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void ValidateParsedStructure_UsesTheNearestExistingOffsetForMissingMembers()
    {
        string source = "{}";
        GltfJsonReadResult parsed = ParseJson(source);

        GltfValidationReport report = new StructuralValidator().ValidateParsedStructure(parsed);

        GltfValidationIssue issue = FindIssue(report, "/asset");
        Assert.Multiple(() =>
        {
            Assert.That(issue.Code, Is.EqualTo(GltfIssueCodes.UndefinedProperty));
            Assert.That(issue.HasByteOffset, Is.True);
            Assert.That(issue.ByteOffset, Is.Zero);
        });
    }

    [Test]
    public void ValidateModelStructure_DistinguishesOmittedAndExplicitDefaultMaterialMembers()
    {
        GltfDocument document = new GltfDocument();
        GltfMaterial material = new GltfMaterial();
        document.Materials.Add(material);
        StructuralValidator validator = new StructuralValidator();

        GltfValidationReport omitted = validator.ValidateModelStructure(document);
        material.HasAlphaCutoff = true;
        GltfValidationReport explicitCutoff = validator.ValidateModelStructure(document);
        material.HasAlphaMode = true;
        GltfValidationReport explicitDependency = validator.ValidateModelStructure(document);

        Assert.Multiple(() =>
        {
            Assert.That(omitted.IsValid, Is.True);
            AssertIssue(
                explicitCutoff,
                GltfIssueCodes.UnsatisfiedDependency,
                "/materials/0/alphaMode");
            Assert.That(explicitDependency.IsValid, Is.True);
            Assert.That(material.AlphaCutoff, Is.EqualTo(0.5));
            Assert.That(material.AlphaMode, Is.EqualTo(GltfAlphaMode.Opaque));
        });
    }

    [Test]
    public void ValidateModelStructure_DistinguishesOmittedAndExplicitZeroAccessorOffsets()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessor accessor = new GltfAccessor();
        accessor.Count = 1;
        document.Accessors.Add(accessor);
        StructuralValidator validator = new StructuralValidator();

        GltfValidationReport omitted = validator.ValidateModelStructure(document);
        accessor.HasByteOffset = true;
        GltfValidationReport explicitOffset = validator.ValidateModelStructure(document);
        accessor.HasBufferView = true;
        accessor.BufferView = 0;
        GltfValidationReport dependencyPresent = validator.ValidateModelStructure(document);

        Assert.Multiple(() =>
        {
            Assert.That(omitted.IsValid, Is.True);
            AssertIssue(
                explicitOffset,
                GltfIssueCodes.UnsatisfiedDependency,
                "/accessors/0/bufferView");
            Assert.That(dependencyPresent.IsValid, Is.True);
            Assert.That(accessor.ByteOffset, Is.Zero);
        });
    }

    [Test]
    public void ProjectDocumentPresence_PreservesFlagsUnknownJsonAndDocumentState()
    {
        GltfDocument document = new GltfDocument();
        document.HasDefaultScene = true;
        document.DefaultScene = 0;
        document.Scenes.Add(new GltfScene());
        GltfSampler sampler = new GltfSampler();
        sampler.HasWrapS = true;
        document.Samplers.Add(sampler);
        JsonObject unknown = new JsonObject { ["kept"] = true };
        document.PropertyJson.UnknownMembers.Add("future/member", unknown);
        StructuralValidator validator = new StructuralValidator();

        JsonObject projected = validator.ProjectDocumentPresence(document);

        Assert.Multiple(() =>
        {
            Assert.That(projected["scene"]!.GetValue<int>(), Is.Zero);
            Assert.That(projected["samplers"]![0]!["wrapS"]!.GetValue<int>(),
                Is.EqualTo((int)GltfSamplerWrap.Repeat));
            Assert.That(projected["future/member"]!["kept"]!.GetValue<bool>(), Is.True);
            Assert.That(projected["future/member"], Is.Not.SameAs(unknown));
            Assert.That(document.HasDefaultScene, Is.True);
            Assert.That(sampler.HasWrapS, Is.True);
            Assert.That(document.PropertyJson.UnknownMembers["future/member"], Is.SameAs(unknown));
        });
    }

    [Test]
    public void ValidateModelStructure_ReportsConstructedModelRangesAtExactPointers()
    {
        GltfDocument document = new GltfDocument();
        document.Buffers.Add(new GltfBuffer());

        GltfValidationReport report = new StructuralValidator().ValidateModelStructure(document);

        AssertIssue(report, GltfIssueCodes.ValueNotInRange, "/buffers/0/byteLength");
        Assert.That(FindIssue(report, "/buffers/0/byteLength").HasByteOffset, Is.False);
    }

    [Test]
    public void StructuralValidator_RejectsNullInputs()
    {
        StructuralValidator validator = new StructuralValidator();

        Assert.Multiple(() =>
        {
            Assert.That(() => validator.ValidateParsedStructure(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("json"));
            Assert.That(() => validator.ValidateModelStructure(null!),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("document"));
        });
    }

    private static GltfJsonReadResult ParseJson(string source)
    {
        return new GltfJsonReader().Read(
            Encoding.UTF8.GetBytes(source),
            new GltfReadLimits(),
            GltfDuplicateMemberPolicy.Reject);
    }

    private static void AssertIssue(
        GltfValidationReport report,
        string code,
        string pointer)
    {
        Assert.That(
            report.Issues.Any(issue => issue.Code == code && issue.JsonPointer == pointer),
            Is.True,
            $"Expected {code} at '{pointer}', got: "
                + string.Join(", ", report.Issues.Select(
                    issue => $"{issue.Code}@{issue.JsonPointer}")));
    }

    private static GltfValidationIssue FindIssue(GltfValidationReport report, string pointer)
    {
        return report.Issues.First(issue => issue.JsonPointer == pointer);
    }
}
