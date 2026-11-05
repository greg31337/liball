using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfCoreSchemaValidationTests
{
    private const string Asset = "\"asset\":{\"version\":\"2.0\"}";

    private static readonly object[] RequiredMemberCases =
    {
        new object[] { "{}", "/asset" },
        new object[] { "{\"asset\":{}}", "/asset/version" },
        new object[] { Wrap("\"accessors\":[{\"count\":1,\"type\":\"SCALAR\"}]"), "/accessors/0/componentType" },
        new object[] { Wrap("\"accessors\":[{\"componentType\":5126,\"type\":\"SCALAR\"}]"), "/accessors/0/count" },
        new object[] { Wrap("\"accessors\":[{\"componentType\":5126,\"count\":1}]"), "/accessors/0/type" },
        new object[] { Wrap("\"accessors\":[{" + AccessorPrefix + "\"sparse\":{\"indices\":{" + SparseIndices + "},\"values\":{" + SparseValues + "}}}]"), "/accessors/0/sparse/count" },
        new object[] { Wrap("\"accessors\":[{" + AccessorPrefix + "\"sparse\":{\"count\":1,\"values\":{" + SparseValues + "}}}]"), "/accessors/0/sparse/indices" },
        new object[] { Wrap("\"accessors\":[{" + AccessorPrefix + "\"sparse\":{\"count\":1,\"indices\":{" + SparseIndices + "}}}]"), "/accessors/0/sparse/values" },
        new object[] { Wrap("\"accessors\":[{" + AccessorPrefix + "\"sparse\":{\"count\":1,\"indices\":{\"componentType\":5121},\"values\":{" + SparseValues + "}}}]"), "/accessors/0/sparse/indices/bufferView" },
        new object[] { Wrap("\"accessors\":[{" + AccessorPrefix + "\"sparse\":{\"count\":1,\"indices\":{\"bufferView\":0},\"values\":{" + SparseValues + "}}}]"), "/accessors/0/sparse/indices/componentType" },
        new object[] { Wrap("\"accessors\":[{" + AccessorPrefix + "\"sparse\":{\"count\":1,\"indices\":{" + SparseIndices + "},\"values\":{}}}]"), "/accessors/0/sparse/values/bufferView" },
        new object[] { Wrap("\"animations\":[{\"samplers\":[{" + AnimationSampler + "}]}]"), "/animations/0/channels" },
        new object[] { Wrap("\"animations\":[{\"channels\":[{" + AnimationChannel + "}]}]"), "/animations/0/samplers" },
        new object[] { Wrap("\"animations\":[{\"channels\":[{\"target\":{\"path\":\"translation\"}}],\"samplers\":[{" + AnimationSampler + "}]}]"), "/animations/0/channels/0/sampler" },
        new object[] { Wrap("\"animations\":[{\"channels\":[{\"sampler\":0}],\"samplers\":[{" + AnimationSampler + "}]}]"), "/animations/0/channels/0/target" },
        new object[] { Wrap("\"animations\":[{\"channels\":[{\"sampler\":0,\"target\":{}}],\"samplers\":[{" + AnimationSampler + "}]}]"), "/animations/0/channels/0/target/path" },
        new object[] { Wrap("\"animations\":[{\"channels\":[{" + AnimationChannel + "}],\"samplers\":[{\"output\":0}]}]"), "/animations/0/samplers/0/input" },
        new object[] { Wrap("\"animations\":[{\"channels\":[{" + AnimationChannel + "}],\"samplers\":[{\"input\":0}]}]"), "/animations/0/samplers/0/output" },
        new object[] { Wrap("\"buffers\":[{}]"), "/buffers/0/byteLength" },
        new object[] { Wrap("\"bufferViews\":[{\"byteLength\":4}]"), "/bufferViews/0/buffer" },
        new object[] { Wrap("\"bufferViews\":[{\"buffer\":0}]"), "/bufferViews/0/byteLength" },
        new object[] { Wrap("\"cameras\":[{\"perspective\":{\"yfov\":1,\"znear\":0.1}}]"), "/cameras/0/type" },
        new object[] { Wrap("\"cameras\":[{\"type\":\"perspective\",\"perspective\":{\"znear\":0.1}}]"), "/cameras/0/perspective/yfov" },
        new object[] { Wrap("\"cameras\":[{\"type\":\"perspective\",\"perspective\":{\"yfov\":1}}]"), "/cameras/0/perspective/znear" },
        new object[] { Wrap("\"cameras\":[{\"type\":\"orthographic\",\"orthographic\":{\"ymag\":1,\"zfar\":2,\"znear\":0}}]"), "/cameras/0/orthographic/xmag" },
        new object[] { Wrap("\"cameras\":[{\"type\":\"orthographic\",\"orthographic\":{\"xmag\":1,\"zfar\":2,\"znear\":0}}]"), "/cameras/0/orthographic/ymag" },
        new object[] { Wrap("\"cameras\":[{\"type\":\"orthographic\",\"orthographic\":{\"xmag\":1,\"ymag\":1,\"znear\":0}}]"), "/cameras/0/orthographic/zfar" },
        new object[] { Wrap("\"cameras\":[{\"type\":\"orthographic\",\"orthographic\":{\"xmag\":1,\"ymag\":1,\"zfar\":2}}]"), "/cameras/0/orthographic/znear" },
        new object[] { Wrap("\"meshes\":[{}]"), "/meshes/0/primitives" },
        new object[] { Wrap("\"meshes\":[{\"primitives\":[{}]}]"), "/meshes/0/primitives/0/attributes" },
        new object[] { Wrap("\"skins\":[{}]"), "/skins/0/joints" },
        new object[] { Wrap("\"materials\":[{\"pbrMetallicRoughness\":{\"baseColorTexture\":{}}}]"), "/materials/0/pbrMetallicRoughness/baseColorTexture/index" }
    };

    private static readonly object[] ConstraintCases =
    {
        new object[] { "{\"asset\":{\"version\":2}}", GltfIssueCodes.TypeMismatch, "/asset/version" },
        new object[] { "{\"asset\":{\"version\":\"two\"}}", GltfIssueCodes.PatternMismatch, "/asset/version" },
        new object[] { Wrap("\"accessors\":[{\"componentType\":5126,\"normalized\":0,\"count\":1,\"type\":\"SCALAR\"}]"), GltfIssueCodes.TypeMismatch, "/accessors/0/normalized" },
        new object[] { Wrap("\"buffers\":[{\"byteLength\":0}]"), GltfIssueCodes.ValueNotInRange, "/buffers/0/byteLength" },
        new object[] { Wrap("\"bufferViews\":[{\"buffer\":0,\"byteLength\":4,\"byteStride\":256}]"), GltfIssueCodes.ValueNotInRange, "/bufferViews/0/byteStride" },
        new object[] { Wrap("\"bufferViews\":[{\"buffer\":0,\"byteLength\":4,\"byteStride\":6}]"), GltfIssueCodes.ValueMultipleOf, "/bufferViews/0/byteStride" },
        new object[] { Wrap("\"accessors\":[{\"componentType\":5126,\"count\":1,\"type\":\"SCALAR\",\"max\":[]}]"), GltfIssueCodes.ArrayLengthNotInList, "/accessors/0/max" },
        new object[] { Wrap("\"nodes\":[{\"matrix\":[0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0]}]"), GltfIssueCodes.ArrayLengthNotInList, "/nodes/0/matrix" },
        new object[] { Wrap("\"extensionsUsed\":[\"EXT_a\",\"EXT_a\"]"), GltfIssueCodes.DuplicateElements, "/extensionsUsed/1" },
        new object[] { Wrap("\"meshes\":[{\"primitives\":[{\"attributes\":{}}]}]"), GltfIssueCodes.ValueNotInRange, "/meshes/0/primitives/0/attributes" },
        new object[] { Wrap("\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0},\"targets\":[{}]}]}]"), GltfIssueCodes.ValueNotInRange, "/meshes/0/primitives/0/targets/0" },
        new object[] { Wrap("\"accessors\":[{\"byteOffset\":0,\"componentType\":5126,\"count\":1,\"type\":\"SCALAR\"}]"), GltfIssueCodes.UnsatisfiedDependency, "/accessors/0/bufferView" },
        new object[] { Wrap("\"scene\":0"), GltfIssueCodes.UnsatisfiedDependency, "/scenes" },
        new object[] { Wrap("\"images\":[{\"bufferView\":0}]"), GltfIssueCodes.UnsatisfiedDependency, "/images/0/mimeType" },
        new object[] { Wrap("\"materials\":[{\"alphaCutoff\":0.5}]"), GltfIssueCodes.UnsatisfiedDependency, "/materials/0/alphaMode" },
        new object[] { Wrap("\"nodes\":[{\"weights\":[0]}]"), GltfIssueCodes.UnsatisfiedDependency, "/nodes/0/mesh" },
        new object[] { Wrap("\"nodes\":[{\"skin\":0}]"), GltfIssueCodes.UnsatisfiedDependency, "/nodes/0/mesh" },
        new object[] { Wrap("\"images\":[{}]"), GltfIssueCodes.OneOfMismatch, "/images/0" },
        new object[] { Wrap("\"images\":[{\"uri\":\"a.png\",\"bufferView\":0,\"mimeType\":\"image/png\"}]"), GltfIssueCodes.OneOfMismatch, "/images/0" },
        new object[] { Wrap("\"cameras\":[{\"type\":\"perspective\",\"perspective\":{\"yfov\":1,\"znear\":0.1},\"orthographic\":{\"xmag\":1,\"ymag\":1,\"zfar\":2,\"znear\":0}}]"), GltfIssueCodes.OneOfMismatch, "/cameras/0" },
        new object[] { Wrap("\"nodes\":[{\"matrix\":[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1],\"translation\":[0,0,0]}]"), GltfIssueCodes.OneOfMismatch, "/nodes/0" },
        new object[] { Wrap("\"materials\":[{\"pbrMetallicRoughness\":{\"baseColorFactor\":[0,0,0,2]}}]"), GltfIssueCodes.ValueNotInRange, "/materials/0/pbrMetallicRoughness/baseColorFactor/3" },
        new object[] { Wrap("\"cameras\":[{\"type\":\"perspective\",\"perspective\":{\"aspectRatio\":0,\"yfov\":1,\"znear\":0.1}}]"), GltfIssueCodes.ValueNotInRange, "/cameras/0/perspective/aspectRatio" },
        new object[] { Wrap("\"extensions\":{\"EXT_bad\":4}"), GltfIssueCodes.TypeMismatch, "/extensions/EXT_bad" }
    };

    private const string AccessorPrefix =
        "\"componentType\":5126,\"count\":1,\"type\":\"SCALAR\",";

    private const string SparseIndices =
        "\"bufferView\":0,\"componentType\":5121";

    private const string SparseValues = "\"bufferView\":0";

    private const string AnimationSampler = "\"input\":0,\"output\":0";

    private const string AnimationChannel =
        "\"sampler\":0,\"target\":{\"path\":\"translation\"}";

    [TestCaseSource(nameof(RequiredMemberCases))]
    public void ValidateBytes_MapsEveryCoreRequiredMemberToAnExactIssue(
        string json,
        string pointer)
    {
        AssertSingleStructuralIssue(
            Validate(json),
            GltfIssueCodes.UndefinedProperty,
            pointer);
    }

    [Test]
    public void ValidateBytes_RejectsEveryEmptyNormativeRootCollection()
    {
        string[] collectionNames =
        {
            "accessors",
            "animations",
            "buffers",
            "bufferViews",
            "cameras",
            "images",
            "materials",
            "meshes",
            "nodes",
            "samplers",
            "scenes",
            "skins",
            "textures",
            "extensionsUsed",
            "extensionsRequired"
        };

        Assert.Multiple(() =>
        {
            foreach (string collectionName in collectionNames)
            {
                AssertSingleStructuralIssue(
                    Validate(Wrap("\"" + collectionName + "\":[]")),
                    GltfIssueCodes.ArrayLengthNotInList,
                    "/" + collectionName);
            }
        });
    }

    [TestCaseSource(nameof(ConstraintCases))]
    public void ValidateBytes_MapsEveryCoreSchemaConstraintFamily(
        string json,
        string code,
        string pointer)
    {
        AssertSingleStructuralIssue(Validate(json), code, pointer);
    }

    [Test]
    public void CoreSchema_AcceptsNormativeForwardCompatibleEnumFallbacks()
    {
        string json = Wrap(
            "\"accessors\":[{\"componentType\":9999,\"count\":1,\"type\":\"FUTURE\"}],"
            + "\"animations\":[{\"channels\":[{\"sampler\":0,\"target\":{\"path\":\"future\"}}],"
            + "\"samplers\":[{\"input\":0,\"interpolation\":\"FUTURE\",\"output\":0}]}],"
            + "\"bufferViews\":[{\"buffer\":0,\"byteLength\":4,\"target\":9999}],"
            + "\"cameras\":[{\"type\":\"future\"}],"
            + "\"images\":[{\"uri\":\"image.future\",\"mimeType\":\"image/future\"}],"
            + "\"materials\":[{\"alphaMode\":\"FUTURE\"}],"
            + "\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0},\"mode\":99}]}],"
            + "\"samplers\":[{\"magFilter\":9999,\"minFilter\":9999,\"wrapS\":9999,\"wrapT\":9999}]");

        GltfJsonReadResult parsed = new GltfJsonReader().Read(
            Encoding.UTF8.GetBytes(json),
            new GltfReadLimits(),
            GltfDuplicateMemberPolicy.Reject);
        GltfValidationReport report = new StructuralValidator().ValidateParsedStructure(parsed);

        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [Test]
    public void ValidateBytes_OrdersCompoundSchemaIssuesDeterministically()
    {
        string json = Wrap(
            "\"accessors\":[],"
            + "\"buffers\":[{\"byteLength\":0}],"
            + "\"extensionsUsed\":[\"EXT_a\",\"EXT_a\"],"
            + "\"images\":[{}],"
            + "\"materials\":[{\"alphaCutoff\":0.5}],"
            + "\"nodes\":[{\"matrix\":[1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1],\"translation\":[0,0,0]}],"
            + "\"scene\":0");
        json = json.Replace("\"version\":\"2.0\"", "\"version\":\"two\"", StringComparison.Ordinal);

        GltfValidationReport first = Validate(json);
        GltfValidationReport second = Validate(json);
        string[] expected =
        {
            GltfIssueCodes.ArrayLengthNotInList + "|Error|/accessors",
            GltfIssueCodes.PatternMismatch + "|Error|/asset/version",
            GltfIssueCodes.ValueNotInRange + "|Error|/buffers/0/byteLength",
            GltfIssueCodes.DuplicateElements + "|Error|/extensionsUsed/1",
            GltfIssueCodes.OneOfMismatch + "|Error|/images/0",
            GltfIssueCodes.UnsatisfiedDependency + "|Error|/materials/0/alphaMode",
            GltfIssueCodes.OneOfMismatch + "|Error|/nodes/0",
            GltfIssueCodes.UnsatisfiedDependency + "|Error|/scenes"
        };

        Assert.Multiple(() =>
        {
            Assert.That(DescribeIssues(first), Is.EqualTo(expected));
            Assert.That(DescribeIssues(second), Is.EqualTo(expected));
            Assert.That(first.IsValid, Is.False);
            Assert.That(second.IsValid, Is.False);
        });
    }

    private static GltfValidationReport Validate(string json)
    {
        GltfValidationOptions options = new GltfValidationOptions();
        options.ValidateSemantics = false;
        options.ValidateResources = false;
        options.ValidateExtensions = false;
        return new GltfValidator().ValidateBytes(
            Encoding.UTF8.GetBytes(json),
            options);
    }

    private static void AssertSingleStructuralIssue(
        GltfValidationReport report,
        string code,
        string pointer)
    {
        Assert.Multiple(() =>
        {
            Assert.That(report.IsValid, Is.False);
            Assert.That(report.Issues, Has.Count.EqualTo(1));
            Assert.That(report.Issues[0].Code, Is.EqualTo(code));
            Assert.That(report.Issues[0].Severity, Is.EqualTo(GltfValidationSeverity.Error));
            Assert.That(report.Issues[0].JsonPointer, Is.EqualTo(pointer));
            Assert.That(report.Issues[0].HasByteOffset, Is.True);
        });
    }

    private static string[] DescribeIssues(GltfValidationReport report)
    {
        return report.Issues.Select(issue =>
            issue.Code + "|" + issue.Severity + "|" + issue.JsonPointer).ToArray();
    }

    private static string Wrap(string members)
    {
        return "{" + Asset + "," + members + "}";
    }
}
