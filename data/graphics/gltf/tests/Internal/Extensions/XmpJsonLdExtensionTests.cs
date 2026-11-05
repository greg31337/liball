using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Internal.Extensions;

[TestFixture]
public class XmpJsonLdExtensionTests
{
    [Test]
    public void ReaderAndWriter_PreservePacketsAndDoNotResolveContextUris()
    {
        const string json = "{\"asset\":{\"version\":\"2.0\",\"extensions\":{"
            + "\"KHR_xmp_json_ld\":{\"packet\":0,\"futureAssignment\":true}}},"
            + "\"extensionsUsed\":[\"KHR_xmp_json_ld\"],\"extensions\":{"
            + "\"KHR_xmp_json_ld\":{\"packets\":[{\"@context\":{"
            + "\"dc\":\"https://example.invalid/dc/\","
            + "\"rdf\":\"https://example.invalid/rdf/\"},\"@id\":\"\","
            + "\"dc:title\":{\"@type\":\"rdf:Alt\",\"rdf:_1\":{"
            + "\"@language\":\"en-us\",\"@value\":\"Example\"}},"
            + "\"dc:creator\":{\"@list\":[\"A\",\"B\"]},"
            + "\"dc:rating\":4.5}],\"futureRoot\":{\"kept\":true}}}}";
        CountingResolver resolver = new CountingResolver();
        GltfReadOptions readOptions = new GltfReadOptions
        {
            ResolveResources = true,
            ResourceResolver = resolver
        };

        GltfReadResult read = new GltfReader().ReadBytes(
            Encoding.UTF8.GetBytes(json),
            readOptions);
        GltfDocument document = read.Document!;
        GltfTypedExtensionData rootTyped = document.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrXmpJsonLd];
        GltfTypedExtensionData assignmentTyped = document.Asset.PropertyJson.TypedExtensions[
            GltfExtensionNames.KhrXmpJsonLd];
        GltfXmpJsonLdPacket packet = rootTyped.XmpJsonLd!.Packets[0];
        GltfWriteOptions writeOptions = new GltfWriteOptions { Validate = false };
        GltfWriteResult written = new GltfWriter().WriteJsonText(document, writeOptions);
        JsonNode output = JsonNode.Parse(written.JsonText!)!;
        JsonNode originalPacket = JsonNode.Parse(json)!["extensions"]![
            GltfExtensionNames.KhrXmpJsonLd]!["packets"]![0]!;
        JsonNode outputPacket = output["extensions"]![
            GltfExtensionNames.KhrXmpJsonLd]!["packets"]![0]!;

        Assert.Multiple(() =>
        {
            Assert.That(read.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(resolver.Calls, Is.Zero);
            Assert.That(rootTyped.XmpJsonLd.HasPackets, Is.True);
            Assert.That(packet.HasContext, Is.True);
            Assert.That(packet.Context["dc"], Is.EqualTo("https://example.invalid/dc/"));
            Assert.That(packet.Properties["dc:creator"]!["@list"]!.AsArray(), Has.Count.EqualTo(2));
            Assert.That(assignmentTyped.XmpJsonLdAssignment!.HasPacket, Is.True);
            Assert.That(assignmentTyped.XmpJsonLdAssignment.Packet, Is.Zero);
            Assert.That(JsonNode.DeepEquals(originalPacket, outputPacket), Is.True);
            Assert.That(output["extensions"]![GltfExtensionNames.KhrXmpJsonLd]![
                "futureRoot"]!["kept"]!.GetValue<bool>(), Is.True);
            Assert.That(output["asset"]!["extensions"]![GltfExtensionNames.KhrXmpJsonLd]![
                "futureAssignment"]!.GetValue<bool>(), Is.True);
        });
    }

    [Test]
    public void Assignment_EnumeratesAndRemapsPacketReferenceWithoutMutation()
    {
        GltfTypedExtensionData typed = new GltfTypedExtensionData
        {
            XmpJsonLdAssignment = new GltfXmpJsonLdAssignmentExtension
            {
                HasPacket = true,
                Packet = 1
            }
        };
        typed.XmpJsonLdAssignment.PropertyJson.UnknownMembers["future"] = "kept";
        GltfExtensionContext context = new GltfExtensionContext
        {
            Name = GltfExtensionNames.KhrXmpJsonLd,
            ObjectLocation = "node",
            HasObjectIndex = true,
            ObjectIndex = 4,
            TypedData = typed
        };
        GltfIndexRemap remap = new GltfIndexRemap();
        remap.XmpJsonLdPackets[1] = 3;

        GltfExtensionResult result = new GltfExtensionHandler()
            .RemapExtensionReferences(context, remap);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(result.Json!["packet"]!.GetValue<int>(), Is.EqualTo(3));
            Assert.That(result.Json["future"]!.GetValue<string>(), Is.EqualTo("kept"));
            Assert.That(result.References, Has.Count.EqualTo(1));
            Assert.That(result.References[0].SourceCollection, Is.EqualTo("nodes"));
            Assert.That(result.References[0].SourceIndex, Is.EqualTo(4));
            Assert.That(result.References[0].TargetCollection, Is.EqualTo("xmpJsonLdPackets"));
            Assert.That(result.References[0].TargetIndex, Is.EqualTo(3));
            Assert.That(typed.XmpJsonLdAssignment.Packet, Is.EqualTo(1));
        });
    }

    [Test]
    public void Validation_EnforcesPacketReferencesAndSupportedAssignmentLocations()
    {
        GltfDocument document = CreateDocumentWithPackets(1);
        GltfExtensionHandler handler = new GltfExtensionHandler();
        GltfExtensionContext unresolved = CreateAssignmentContext(document, "node", 2);
        GltfExtensionContext wrongLocation = CreateAssignmentContext(document, "texture", 0);
        GltfExtensionContext valid = CreateAssignmentContext(document, "material", 0);

        GltfExtensionResult unresolvedResult = handler.ValidateExtension(unresolved);
        GltfExtensionResult wrongLocationResult = handler.ValidateExtension(wrongLocation);
        GltfExtensionResult validResult = handler.ValidateExtension(valid);

        Assert.Multiple(() =>
        {
            Assert.That(unresolvedResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.UnresolvedReference), Is.True);
            Assert.That(wrongLocationResult.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.UnexpectedExtensionObject), Is.True);
            Assert.That(validResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(validResult.References.Single().TargetIndex, Is.Zero);
        });
    }

    [Test]
    public void Validation_EnforcesLocalJsonLdPacketRestrictions()
    {
        JsonObject json = new JsonObject
        {
            ["packets"] = new JsonArray
            {
                new JsonObject
                {
                    ["@context"] = new JsonObject
                    {
                        ["dc"] = "https://example.invalid/same/",
                        ["duplicate"] = "https://example.invalid/same/"
                    },
                    ["missing:title"] = "Title",
                    ["dc:values"] = new JsonArray(1, 2),
                    ["dc:nested"] = new JsonObject
                    {
                        ["@context"] = new JsonObject { ["x"] = "https://example.invalid/x/" }
                    }
                }
            }
        };
        GltfExtensionContext context = new GltfExtensionContext
        {
            Name = GltfExtensionNames.KhrXmpJsonLd,
            ObjectLocation = "glTF",
            Json = json
        };

        GltfExtensionResult result = new GltfExtensionHandler().ValidateExtension(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(GltfOperationStatus.Invalid));
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.DuplicateElements), Is.True);
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Code == GltfIssueCodes.UnsatisfiedDependency
                && issue.JsonPointer.EndsWith("/missing:title", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Message.Contains("children of @list or @set", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Diagnostics.Any(issue =>
                issue.Message.Contains("Local JSON-LD contexts", StringComparison.Ordinal)), Is.True);
        });
    }

    private static GltfDocument CreateDocumentWithPackets(int count)
    {
        GltfXmpJsonLdExtension root = new GltfXmpJsonLdExtension { HasPackets = true };
        int index = 0;
        while (index < count)
        {
            root.Packets.Add(new GltfXmpJsonLdPacket { HasContext = true });
            index++;
        }

        GltfTypedExtensionData typed = new GltfTypedExtensionData { XmpJsonLd = root };
        GltfDocument document = new GltfDocument();
        document.PropertyJson.TypedExtensions[GltfExtensionNames.KhrXmpJsonLd] = typed;
        return document;
    }

    private static GltfExtensionContext CreateAssignmentContext(
        GltfDocument document,
        string location,
        int packet)
    {
        return new GltfExtensionContext
        {
            Document = document,
            Name = GltfExtensionNames.KhrXmpJsonLd,
            ObjectLocation = location,
            HasObjectIndex = true,
            ObjectIndex = 0,
            Json = new JsonObject { ["packet"] = packet }
        };
    }

    private class CountingResolver : IGltfResourceResolver
    {
        public int Calls;

        public GltfResource ResolveResource(GltfResourceRequest request)
        {
            Calls++;
            return new GltfResource { Status = GltfOperationStatus.Unavailable };
        }
    }
}
