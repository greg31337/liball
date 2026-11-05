using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class SceneExtensionCodecTests
{
    [Test]
    public void Reader_ParsesAllSceneExtensionPayloadsWithoutExplicitRegistration()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"accessors\":["
            + "{\"componentType\":5126,\"count\":2,\"type\":\"VEC3\"},"
            + "{\"componentType\":5122,\"normalized\":true,\"count\":2,\"type\":\"VEC4\"}],"
            + "\"materials\":[{\"pbrMetallicRoughness\":{\"baseColorTexture\":{\"index\":0,"
            + "\"extensions\":{\"KHR_texture_transform\":{\"offset\":[0.25,0.5],"
            + "\"rotation\":1.0,\"scale\":[2,3],\"texCoord\":1,\"future\":7}}}}}],"
            + "\"textures\":[{}],\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0}}]}],"
            + "\"nodes\":[{\"mesh\":0,\"extensions\":{"
            + "\"KHR_lights_punctual\":{\"light\":0},"
            + "\"KHR_node_visibility\":{\"visible\":false},"
            + "\"KHR_node_hoverability\":{\"hoverable\":false},"
            + "\"KHR_node_selectability\":{\"selectable\":true},"
            + "\"EXT_mesh_gpu_instancing\":{\"attributes\":{\"TRANSLATION\":0,\"ROTATION\":1}}}}],"
            + "\"extensionsUsed\":[\"KHR_texture_transform\",\"KHR_lights_punctual\","
            + "\"KHR_node_visibility\",\"KHR_node_hoverability\","
            + "\"KHR_node_selectability\",\"EXT_mesh_gpu_instancing\"],"
            + "\"extensions\":{\"KHR_lights_punctual\":{\"lights\":[{\"type\":\"spot\","
            + "\"color\":[1,0.5,0.25],\"intensity\":2,\"range\":10,"
            + "\"spot\":{\"innerConeAngle\":0.1,\"outerConeAngle\":0.5}}]}}}";

        GltfReadResult result = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            new GltfReadOptions());
        GltfDocument document = result.Document!;
        GltfTextureInfo textureInfo = document.Materials[0].PbrMetallicRoughness!.BaseColorTexture!;
        GltfTextureTransformExtension transform = textureInfo.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrTextureTransform].TextureTransform!;
        GltfTypedExtensionData nodeData = document.Nodes[0].PropertyJson.TypedExtensions[
            GltfExtensionNames.ExtMeshGpuInstancing];
        GltfLightsPunctualExtension lights = document.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrLightsPunctual].LightsPunctual!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(transform.Offset, Is.EqualTo(new[] { 0.25, 0.5 }));
            Assert.That(transform.Rotation, Is.EqualTo(1.0));
            Assert.That(transform.Scale, Is.EqualTo(new[] { 2.0, 3.0 }));
            Assert.That(transform.TextureCoordinate, Is.EqualTo(1));
            Assert.That(transform.PropertyJson.UnknownMembers["future"]!.GetValue<double>(), Is.EqualTo(7.0));
            Assert.That(lights.Lights[0].Type, Is.EqualTo("spot"));
            Assert.That(lights.Lights[0].Spot!.OuterConeAngle, Is.EqualTo(0.5));
            Assert.That(document.Nodes[0].PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrLightsPunctual].PunctualLightNode!.Light, Is.Zero);
            Assert.That(document.Nodes[0].PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrNodeVisibility].NodeVisibility!.Visible, Is.False);
            Assert.That(document.Nodes[0].PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrNodeHoverability].NodeHoverability!.Hoverable, Is.False);
            Assert.That(document.Nodes[0].PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrNodeSelectability].NodeSelectability!.Selectable, Is.True);
            Assert.That(nodeData.MeshGpuInstancing!.Attributes["ROTATION"], Is.EqualTo(1));
        });
    }

    [Test]
    public void Writer_RoundTripsAllSceneExtensionPayloadsFromTypedData()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\"},"
            + "\"accessors\":[{\"componentType\":5126,\"count\":1,\"type\":\"VEC3\"}],"
            + "\"materials\":[{\"emissiveTexture\":{\"index\":0,\"extensions\":{"
            + "\"KHR_texture_transform\":{\"offset\":[0.1,0.2],\"rotation\":0.3,"
            + "\"scale\":[0.4,0.5],\"texCoord\":2}}}}],\"textures\":[{}],"
            + "\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0}}]}],"
            + "\"nodes\":[{\"mesh\":0,\"extensions\":{"
            + "\"KHR_lights_punctual\":{\"light\":0},"
            + "\"KHR_node_visibility\":{\"visible\":false},"
            + "\"KHR_node_hoverability\":{\"hoverable\":false},"
            + "\"KHR_node_selectability\":{\"selectable\":false},"
            + "\"EXT_mesh_gpu_instancing\":{\"attributes\":{\"TRANSLATION\":0}}}}],"
            + "\"extensions\":{\"KHR_lights_punctual\":{\"lights\":[{\"type\":\"point\"}]}}}";
        GltfDocument document = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            new GltfReadOptions()).Document!;
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;

        string written = new GltfWriter().WriteJsonText(document, options).JsonText!;
        JsonNode root = JsonNode.Parse(written)!;

        Assert.Multiple(() =>
        {
            Assert.That(root["materials"]![0]!["emissiveTexture"]!["extensions"]![
                GltfExtensionNames.KhrTextureTransform]!["texCoord"]!.GetValue<int>(), Is.EqualTo(2));
            Assert.That(root["extensions"]![GltfExtensionNames.KhrLightsPunctual]![
                "lights"]![0]!["type"]!.GetValue<string>(), Is.EqualTo("point"));
            Assert.That(root["nodes"]![0]!["extensions"]![
                GltfExtensionNames.KhrLightsPunctual]!["light"]!.GetValue<int>(), Is.Zero);
            Assert.That(root["nodes"]![0]!["extensions"]![
                GltfExtensionNames.KhrNodeVisibility]!["visible"]!.GetValue<bool>(), Is.False);
            Assert.That(root["nodes"]![0]!["extensions"]![
                GltfExtensionNames.KhrNodeHoverability]!["hoverable"]!.GetValue<bool>(), Is.False);
            Assert.That(root["nodes"]![0]!["extensions"]![
                GltfExtensionNames.KhrNodeSelectability]!["selectable"]!.GetValue<bool>(), Is.False);
            Assert.That(root["nodes"]![0]!["extensions"]![
                GltfExtensionNames.ExtMeshGpuInstancing]!["attributes"]![
                "TRANSLATION"]!.GetValue<int>(), Is.Zero);
        });
    }

    [Test]
    public void Writer_EmitsTypedOnlyPayloadsAndPreservesTypedUnknownMembers()
    {
        GltfDocument document = new GltfDocument();
        document.ExtensionsUsed.Add(GltfExtensionNames.KhrNodeVisibility);
        GltfNode node = new GltfNode();
        GltfNodeVisibilityExtension visibility = new GltfNodeVisibilityExtension();
        visibility.HasVisible = true;
        visibility.Visible = false;
        visibility.PropertyJson.UnknownMembers["future"] = JsonValue.Create(3);
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.NodeVisibility = visibility;
        node.PropertyJson.TypedExtensions[GltfExtensionNames.KhrNodeVisibility] = typed;
        document.Nodes.Add(node);
        GltfWriteOptions options = new GltfWriteOptions();
        options.Validate = false;

        GltfWriteResult result = new GltfWriter().WriteJsonText(document, options);
        JsonNode extension = JsonNode.Parse(result.JsonText!)!["nodes"]![0]!["extensions"]![
            GltfExtensionNames.KhrNodeVisibility]!;

        Assert.Multiple(() =>
        {
            Assert.That(extension["visible"]!.GetValue<bool>(), Is.False);
            Assert.That(extension["future"]!.GetValue<int>(), Is.EqualTo(3));
            Assert.That(node.PropertyJson.Extensions, Is.Empty);
        });
    }

    [Test]
    public void Validation_ReportsPunctualLightAndGpuInstancingSemanticFailures()
    {
        GltfDocument document = new GltfDocument();
        GltfAccessor translation = new GltfAccessor();
        translation.ComponentType = GltfComponentType.SignedShort;
        translation.Type = GltfAccessorType.Vector2;
        translation.Count = 2;
        document.Accessors.Add(translation);
        document.Nodes.Add(new GltfNode());
        GltfExtensionHandler handler = new GltfExtensionHandler();
        GltfExtensionContext lightsContext = new GltfExtensionContext();
        lightsContext.Name = GltfExtensionNames.KhrLightsPunctual;
        lightsContext.ObjectLocation = "glTF";
        lightsContext.Json = new JsonObject
        {
            ["lights"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "spot",
                    ["range"] = 0,
                    ["spot"] = new JsonObject
                    {
                        ["innerConeAngle"] = 0.6,
                        ["outerConeAngle"] = 0.5
                    }
                }
            }
        };
        GltfExtensionContext gpuContext = new GltfExtensionContext();
        gpuContext.Document = document;
        gpuContext.Name = GltfExtensionNames.ExtMeshGpuInstancing;
        gpuContext.ObjectLocation = "node";
        gpuContext.HasObjectIndex = true;
        gpuContext.ObjectIndex = 0;
        gpuContext.Json = new JsonObject
        {
            ["attributes"] = new JsonObject { ["TRANSLATION"] = 0 }
        };

        GltfExtensionResult lights = handler.ValidateExtension(lightsContext);
        GltfExtensionResult gpu = handler.ValidateExtension(gpuContext);

        Assert.Multiple(() =>
        {
            Assert.That(lights.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(lights.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.KhrLightsPunctualLightSpotAngles), Is.True);
            Assert.That(lights.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.ValueNotInRange
                && issue.JsonPointer.EndsWith("/range", StringComparison.Ordinal)), Is.True);
            Assert.That(gpu.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(gpu.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.MeshPrimitiveAttributesAccessorInvalidFormat), Is.True);
            Assert.That(gpu.Diagnostics.Any(issue =>
                issue.Message.Contains("only valid on a node", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void Validation_AcceptsTypedDataWithoutRawJsonAndResolvesRawRootLights()
    {
        GltfDocument document = new GltfDocument();
        document.PropertyJson.Extensions[GltfExtensionNames.KhrLightsPunctual] =
            new JsonObject
            {
                ["lights"] = new JsonArray
                {
                    new JsonObject { ["type"] = "point" }
                }
            };
        GltfExtensionContext lightContext = new GltfExtensionContext();
        lightContext.Document = document;
        lightContext.Name = GltfExtensionNames.KhrLightsPunctual;
        lightContext.ObjectLocation = "node";
        GltfPunctualLightNodeExtension light = new GltfPunctualLightNodeExtension();
        light.Light = 0;
        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        typed.PunctualLightNode = light;
        lightContext.TypedData = typed;

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(lightContext);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.TypedData, Is.SameAs(typed));
        });
    }

    [Test]
    public void RemapExtensionReferences_UpdatesGpuAccessorIndicesWithoutMutatingInput()
    {
        GltfExtensionContext context = new GltfExtensionContext();
        context.Name = GltfExtensionNames.ExtMeshGpuInstancing;
        context.ObjectLocation = "node";
        context.HasObjectIndex = true;
        context.ObjectIndex = 2;
        context.Json = new JsonObject
        {
            ["attributes"] = new JsonObject
            {
                ["TRANSLATION"] = 1,
                ["_ID"] = 4
            }
        };
        GltfIndexRemap remap = new GltfIndexRemap();
        remap.Accessors[1] = 7;
        remap.Accessors[4] = 9;

        GltfExtensionResult result = new GltfExtensionHandler().RemapExtensionReferences(
            context,
            remap);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Json!["attributes"]!["TRANSLATION"]!.GetValue<int>(), Is.EqualTo(7));
            Assert.That(result.Json!["attributes"]!["_ID"]!.GetValue<int>(), Is.EqualTo(9));
            Assert.That(context.Json!["attributes"]!["TRANSLATION"]!.GetValue<int>(), Is.EqualTo(1));
            Assert.That(result.References.Select(value => value.TargetIndex), Is.EqualTo(new[] { 7, 9 }));
        });
    }
}
