using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class AdvancedExtensionTests
{
    [Test]
    public void GaussianSplatting_RoundTripsPinnedTypedMetadataAndUnknownValues()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"extensionsUsed\":[\"KHR_gaussian_splatting\"],"
            + "\"accessors\":["
            + "{\"componentType\":5126,\"count\":1,\"type\":\"VEC3\",\"min\":[0,0,0],\"max\":[0,0,0]},"
            + "{\"componentType\":5126,\"count\":1,\"type\":\"VEC4\"},"
            + "{\"componentType\":5126,\"count\":1,\"type\":\"VEC3\"},"
            + "{\"componentType\":5126,\"count\":1,\"type\":\"SCALAR\"},"
            + "{\"componentType\":5126,\"count\":1,\"type\":\"VEC3\"}],"
            + "\"meshes\":[{\"primitives\":[{\"mode\":0,\"attributes\":{"
            + "\"POSITION\":0,\"KHR_gaussian_splatting:ROTATION\":1,"
            + "\"KHR_gaussian_splatting:SCALE\":2,"
            + "\"KHR_gaussian_splatting:OPACITY\":3,"
            + "\"KHR_gaussian_splatting:SH_DEGREE_0_COEF_0\":4},"
            + "\"extensions\":{\"KHR_gaussian_splatting\":{"
            + "\"kernel\":\"ellipse\",\"colorSpace\":\"lin_rec709_display\","
            + "\"projection\":\"perspective\",\"sortingMethod\":\"cameraDistance\","
            + "\"future\":{\"kept\":true}}}}]}]}";

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            new GltfReadOptions());
        GltfGaussianSplattingExtension value = read.Document!.Meshes[0].Primitives[0]
            .PropertyJson.TypedExtensions[GltfExtensionNames.KhrGaussianSplatting]
            .GaussianSplatting!;
        GltfWriteOptions options = new GltfWriteOptions { Validate = false };
        JsonNode output = JsonNode.Parse(
            new GltfWriter().WriteJsonText(read.Document, options).JsonText!)!;
        JsonNode payload = output["meshes"]![0]!["primitives"]![0]!["extensions"]![
            GltfExtensionNames.KhrGaussianSplatting]!;

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(value.HasKernel, Is.True);
            Assert.That(value.Kernel, Is.EqualTo("ellipse"));
            Assert.That(value.HasColorSpace, Is.True);
            Assert.That(value.ColorSpace, Is.EqualTo("lin_rec709_display"));
            Assert.That(value.HasProjection, Is.True);
            Assert.That(value.HasSortingMethod, Is.True);
            Assert.That(payload["kernel"]!.GetValue<string>(), Is.EqualTo("ellipse"));
            Assert.That(payload["future"]!["kept"]!.GetValue<bool>(), Is.True);
        });
    }

    [Test]
    public void GaussianValidation_EnforcesEllipsePrimitiveRequirements()
    {
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        GltfDocument document = new GltfDocument();
        document.Meshes.Add(mesh);
        GltfExtensionContext context = new GltfExtensionContext
        {
            Document = document,
            Name = GltfExtensionNames.KhrGaussianSplatting,
            ObjectLocation = "mesh.primitive",
            HasObjectIndex = true,
            ObjectIndex = 0,
            HasSubObjectIndex = true,
            SubObjectIndex = 0,
            Json = new JsonObject
            {
                ["kernel"] = "ellipse",
                ["colorSpace"] = "srgb_rec709_display"
            }
        };

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.ValueNotInList
                && issue.JsonPointer == "/meshes/0/primitives/0/mode"), Is.True);
            Assert.That(result.Diagnostics.Count(issue =>
                issue.Code == GltfIssueCodes.UndefinedProperty
                && issue.JsonPointer.Contains("/attributes/", StringComparison.Ordinal)),
                Is.EqualTo(5));
        });
    }

    [Test]
    public void GaussianValidation_RequiresNestedExtensionForSnapshotExternalValues()
    {
        GltfExtensionContext context = new GltfExtensionContext
        {
            Name = GltfExtensionNames.KhrGaussianSplatting,
            ObjectLocation = "mesh.primitive",
            Json = new JsonObject
            {
                ["kernel"] = "customShape",
                ["colorSpace"] = "customSpace"
            }
        };
        GltfExtensionHandler handler = new GltfExtensionHandler();

        GltfExtensionResult missingExtension = handler.ValidateExtension(context);
        context.Json!["extensions"] = new JsonObject
        {
            ["VENDOR_gaussian"] = new JsonObject { ["setting"] = 1 }
        };
        GltfExtensionResult definedByExtension = handler.ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(missingExtension.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.UnsatisfiedDependency), Is.True);
            Assert.That(definedByExtension.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(definedByExtension.TypedData!.GaussianSplatting!.PropertyJson
                .Extensions["VENDOR_gaussian"]!["setting"]!.GetValue<int>(), Is.EqualTo(1));
        });
    }

    [Test]
    public void Interactivity_RoundTripsEntryPointWithoutExecutingOperations()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"extensionsUsed\":[\"KHR_interactivity\",\"VENDOR_runtime\"],"
            + "\"extensions\":{\"KHR_interactivity\":{\"graph\":0,\"graphs\":[{"
            + "\"types\":[{\"signature\":\"float\"}],"
            + "\"variables\":[{\"type\":0,\"value\":[1.0],\"name\":\"speed\"}],"
            + "\"events\":[{\"id\":\"ready\",\"values\":{\"amount\":{"
            + "\"type\":0,\"value\":[2.0]}}}],"
            + "\"declarations\":[{\"op\":\"vendor/run\",\"extension\":"
            + "\"VENDOR_runtime\",\"inputValueSockets\":{\"value\":{\"type\":0}}}],"
            + "\"nodes\":[{\"declaration\":0,\"configuration\":{\"limit\":{"
            + "\"value\":[3]}},\"values\":{\"value\":{\"type\":0,"
            + "\"value\":[4.0]}},\"futureNode\":true}],\"name\":\"Graph\"}],"
            + "\"futureRoot\":{\"kept\":true}}}}";
        OperationProbeHandler probe = new OperationProbeHandler();
        GltfReadOptions readOptions = new GltfReadOptions();
        readOptions.ExtensionHandlers.Add(probe);

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            readOptions);
        GltfInteractivityExtension value = read.Document!.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrInteractivity].Interactivity!;
        GltfExtensionContext validationContext = new GltfExtensionContext
        {
            Document = read.Document,
            Name = GltfExtensionNames.KhrInteractivity,
            ObjectLocation = "glTF",
            TypedData = read.Document.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrInteractivity]
        };
        GltfExtensionResult validation = new GltfExtensionHandler().ValidateExtension(
            validationContext);
        GltfWriteOptions writeOptions = new GltfWriteOptions { Validate = false };
        JsonNode output = JsonNode.Parse(
            new GltfWriter().WriteJsonText(read.Document, writeOptions).JsonText!)!;
        JsonNode originalPayload = JsonNode.Parse(json)!["extensions"]![
            GltfExtensionNames.KhrInteractivity]!;
        JsonNode outputPayload = output["extensions"]![GltfExtensionNames.KhrInteractivity]!;

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(validation.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(validation.Diagnostics, Is.Empty);
            Assert.That(probe.ReadCalls, Is.Zero);
            Assert.That(value.HasGraphs, Is.True);
            Assert.That(value.HasDefaultGraph, Is.True);
            Assert.That(value.Graphs[0].Types[0].HasSignature, Is.True);
            Assert.That(value.Graphs[0].Variables[0].Value![0]!.GetValue<double>(),
                Is.EqualTo(1.0));
            Assert.That(value.Graphs[0].Declarations[0].Extension,
                Is.EqualTo("VENDOR_runtime"));
            Assert.That(value.Graphs[0].Nodes[0].PropertyJson.UnknownMembers[
                "futureNode"]!.GetValue<bool>(), Is.True);
            Assert.That(JsonNode.DeepEquals(originalPayload, outputPayload), Is.True);
        });
    }

    [Test]
    public void InteractivityValidation_RejectsInvalidReferencesValuesAndCycles()
    {
        JsonObject json = new JsonObject
        {
            ["graphs"] = new JsonArray
            {
                new JsonObject
                {
                    ["types"] = new JsonArray
                    {
                        new JsonObject { ["signature"] = "unknown" }
                    },
                    ["variables"] = new JsonArray
                    {
                        new JsonObject { ["type"] = 2, ["value"] = new JsonArray() }
                    },
                    ["declarations"] = new JsonArray
                    {
                        new JsonObject { ["op"] = "bad" }
                    },
                    ["nodes"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["declaration"] = 2,
                            ["flows"] = new JsonObject
                            {
                                ["next"] = new JsonObject { ["node"] = 1 }
                            }
                        },
                        new JsonObject
                        {
                            ["declaration"] = 0,
                            ["flows"] = new JsonObject
                            {
                                ["back"] = new JsonObject { ["node"] = 0 }
                            }
                        }
                    }
                }
            },
            ["graph"] = 4
        };
        GltfExtensionContext context = new GltfExtensionContext
        {
            Document = new GltfDocument(),
            Name = GltfExtensionNames.KhrInteractivity,
            ObjectLocation = "glTF",
            Json = json
        };

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.ValueNotInList), Is.True);
            Assert.That(result.Diagnostics.Count(issue =>
                issue.Code == GltfIssueCodes.UnresolvedReference), Is.GreaterThanOrEqualTo(3));
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.ArrayLengthNotInList), Is.True);
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Message.Contains("directed cycle", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.PatternMismatch), Is.True);
        });
    }

    [Test]
    public void Remap_DeepCopiesInternalGraphReferencesWithoutExecutingThem()
    {
        GltfInteractivityFlowSocket flow = new GltfInteractivityFlowSocket
        {
            HasNode = true,
            Node = 1
        };
        GltfInteractivityNode node = new GltfInteractivityNode
        {
            HasDeclaration = true,
            Declaration = 0,
            HasFlows = true
        };
        node.Flows["next"] = flow;
        GltfInteractivityGraph graph = new GltfInteractivityGraph
        {
            HasDeclarations = true,
            HasNodes = true
        };
        graph.Declarations.Add(new GltfInteractivityDeclaration
        {
            HasOperation = true,
            Operation = "flow/sequence"
        });
        graph.Nodes.Add(node);
        graph.Nodes.Add(new GltfInteractivityNode
        {
            HasDeclaration = true,
            Declaration = 0
        });
        GltfInteractivityExtension extension = new GltfInteractivityExtension
        {
            HasGraphs = true
        };
        extension.Graphs.Add(graph);
        GltfExtensionContext context = new GltfExtensionContext
        {
            Name = GltfExtensionNames.KhrInteractivity,
            ObjectLocation = "glTF",
            TypedData = new GltfTypedExtensionData { Interactivity = extension }
        };

        GltfExtensionResult result = new GltfExtensionHandler()
            .RemapExtensionReferences(context, new GltfIndexRemap());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Json!["graphs"]![0]!["nodes"]![0]!["flows"]![
                "next"]!["node"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(result.TypedData!.Interactivity, Is.Not.SameAs(extension));
            Assert.That(flow.Node, Is.EqualTo(1));
        });
    }

    private class OperationProbeHandler : IGltfExtensionHandler
    {
        public int ReadCalls;

        public GltfExtensionDescriptor DescribeExtension()
        {
            return new GltfExtensionDescriptor { Name = "VENDOR_runtime" };
        }

        public GltfExtensionResult ReadExtension(GltfExtensionContext context)
        {
            ReadCalls++;
            return new GltfExtensionResult();
        }

        public GltfExtensionResult WriteExtension(GltfExtensionContext context)
        {
            return new GltfExtensionResult();
        }

        public GltfExtensionResult ValidateExtension(GltfExtensionContext context)
        {
            return new GltfExtensionResult();
        }

        public GltfExtensionResult RemapExtensionReferences(
            GltfExtensionContext context,
            GltfIndexRemap remap)
        {
            return new GltfExtensionResult();
        }
    }
}
