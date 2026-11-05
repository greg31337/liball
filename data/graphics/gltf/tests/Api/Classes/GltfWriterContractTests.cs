using System.Text;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfWriterContractTests
{
    private static readonly byte[] MinimalJson = Encoding.UTF8.GetBytes(
        "{\"asset\":{\"version\":\"2.0\"}}");

    [Test]
    public void InterfaceMethods_ProduceByteIdenticalJsonAcrossEveryDestination()
    {
        IGltfWriter writer = new GltfWriter();
        GltfDocument document = new GltfDocument();
        document.Asset.Generator = "contract-writer";
        GltfWriteOptions options = CreateOptions(GltfContainerFormat.Json);

        GltfWriteResult bytes = writer.WriteBytes(document, options);
        GltfWriteResult text = writer.WriteJsonText(document, options);
        using MemoryStream documentStream = new MemoryStream();
        GltfWriteResult streamed = writer.WriteStream(document, documentStream, options);

        string path = Path.Combine(
            Path.GetTempPath(),
            $"gltf-writer-contract-{Guid.NewGuid():N}.gltf");
        try
        {
            GltfWriteResult filed = writer.WriteFile(document, path, options);
            GltfContainerData container = new GltfContainerData();
            container.Format = GltfContainerFormat.Json;
            container.JsonBytes = bytes.PrimaryBytes;
            GltfWriteResult containerBytes = writer.WriteContainerBytes(container, options);
            using MemoryStream containerStream = new MemoryStream();
            GltfWriteResult containerStreamed = writer.WriteContainerStream(
                container,
                containerStream,
                options);

            Assert.Multiple(() =>
            {
                Assert.That(text.PrimaryBytes, Is.EqualTo(bytes.PrimaryBytes));
                Assert.That(text.JsonText, Is.EqualTo(Encoding.UTF8.GetString(bytes.PrimaryBytes)));
                Assert.That(documentStream.ToArray(), Is.EqualTo(bytes.PrimaryBytes));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes.PrimaryBytes));
                Assert.That(containerBytes.PrimaryBytes, Is.EqualTo(bytes.PrimaryBytes));
                Assert.That(containerStream.ToArray(), Is.EqualTo(bytes.PrimaryBytes));
                Assert.That(streamed.PrimaryBytes, Is.Empty);
                Assert.That(filed.PrimaryBytes, Is.Empty);
                Assert.That(containerStreamed.PrimaryBytes, Is.Empty);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void WriteJsonText_ProducesExactCompactAndIndentedUtf8()
    {
        GltfDocument document = new GltfDocument();
        document.Asset.Generator = "writer";
        GltfWriteOptions compactOptions = CreateOptions(GltfContainerFormat.Json);
        GltfWriteOptions indentedOptions = CreateOptions(GltfContainerFormat.Json);
        indentedOptions.Indented = true;
        IGltfWriter writer = new GltfWriter();

        GltfWriteResult compact = writer.WriteJsonText(document, compactOptions);
        GltfWriteResult indented = writer.WriteJsonText(document, indentedOptions);
        const string expectedCompact =
            "{\"asset\":{\"version\":\"2.0\",\"generator\":\"writer\"}}";
        const string expectedIndented = "{\n"
            + "  \"asset\": {\n"
            + "    \"version\": \"2.0\",\n"
            + "    \"generator\": \"writer\"\n"
            + "  }\n"
            + "}";

        Assert.Multiple(() =>
        {
            Assert.That(compact.JsonText, Is.EqualTo(expectedCompact));
            Assert.That(compact.PrimaryBytes, Is.EqualTo(Encoding.UTF8.GetBytes(expectedCompact)));
            Assert.That(indented.JsonText, Is.EqualTo(expectedIndented));
            Assert.That(indented.PrimaryBytes, Is.EqualTo(Encoding.UTF8.GetBytes(expectedIndented)));
            Assert.That(indented.JsonText, Does.Not.EndWith("\n"));
            Assert.That(
                JsonNode.DeepEquals(JsonNode.Parse(compact.JsonText!), JsonNode.Parse(indented.JsonText!)),
                Is.True);
        });
    }

    [Test]
    public void WriteBytes_DistinguishesAbsentScalarsFromExplicitDefaults()
    {
        GltfDocument document = new GltfDocument();
        document.DefaultScene = 0;
        document.Scenes.Add(new GltfScene());
        GltfMeshPrimitive primitive = new GltfMeshPrimitive();
        primitive.Mode = GltfPrimitiveMode.Triangles;
        GltfMesh mesh = new GltfMesh();
        mesh.Primitives.Add(primitive);
        document.Meshes.Add(mesh);
        IGltfWriter writer = new GltfWriter();
        GltfWriteOptions preserveOptions = CreateOptions(GltfContainerFormat.Json);

        JsonObject absent = ParseObject(writer.WriteBytes(document, preserveOptions).PrimaryBytes);
        document.HasDefaultScene = true;
        primitive.HasMode = true;
        JsonObject explicitDefaults = ParseObject(
            writer.WriteBytes(document, preserveOptions).PrimaryBytes);
        GltfWriteOptions omitDefaultsOptions = CreateOptions(GltfContainerFormat.Json);
        omitDefaultsOptions.PreserveExplicitDefaults = false;
        JsonObject omittedDefaults = ParseObject(
            writer.WriteBytes(document, omitDefaultsOptions).PrimaryBytes);
        primitive.Mode = GltfPrimitiveMode.Points;
        JsonObject nonDefault = ParseObject(
            writer.WriteBytes(document, omitDefaultsOptions).PrimaryBytes);

        Assert.Multiple(() =>
        {
            Assert.That(absent.ContainsKey("scene"), Is.False);
            Assert.That(absent["meshes"]![0]!["primitives"]![0]!["mode"], Is.Null);
            Assert.That(explicitDefaults["scene"]!.GetValue<int>(), Is.Zero);
            Assert.That(
                explicitDefaults["meshes"]![0]!["primitives"]![0]!["mode"]!.GetValue<int>(),
                Is.EqualTo((int)GltfPrimitiveMode.Triangles));
            Assert.That(omittedDefaults["scene"]!.GetValue<int>(), Is.Zero);
            Assert.That(omittedDefaults["meshes"]![0]!["primitives"]![0]!["mode"], Is.Null);
            Assert.That(
                nonDefault["meshes"]![0]!["primitives"]![0]!["mode"]!.GetValue<int>(),
                Is.EqualTo((int)GltfPrimitiveMode.Points));
        });
    }

    [Test]
    public void WriteBytes_PreservesExtensionsAndExtrasIndependentlyFromUnknownMembers()
    {
        GltfDocument document = new GltfDocument();
        document.PropertyJson.Extensions["EXT_contract"] = new JsonObject
        {
            ["enabled"] = true
        };
        document.PropertyJson.HasExtras = true;
        document.PropertyJson.Extras = null;
        document.PropertyJson.UnknownMembers["future"] = new JsonObject
        {
            ["value"] = 7
        };
        GltfWriteOptions preserveOptions = CreateOptions(GltfContainerFormat.Json);
        GltfWriteOptions omitOptions = CreateOptions(GltfContainerFormat.Json);
        omitOptions.PreserveUnknownMembers = false;
        IGltfWriter writer = new GltfWriter();

        JsonObject preserved = ParseObject(writer.WriteBytes(document, preserveOptions).PrimaryBytes);
        JsonObject omitted = ParseObject(writer.WriteBytes(document, omitOptions).PrimaryBytes);

        Assert.Multiple(() =>
        {
            Assert.That(preserved["extensions"]!["EXT_contract"]!["enabled"]!.GetValue<bool>(), Is.True);
            Assert.That(preserved.ContainsKey("extras"), Is.True);
            Assert.That(preserved["extras"], Is.Null);
            Assert.That(preserved["future"]!["value"]!.GetValue<int>(), Is.EqualTo(7));
            Assert.That(omitted["extensions"]!["EXT_contract"]!["enabled"]!.GetValue<bool>(), Is.True);
            Assert.That(omitted.ContainsKey("extras"), Is.True);
            Assert.That(omitted["extras"], Is.Null);
            Assert.That(omitted.ContainsKey("future"), Is.False);
        });
    }

    [Test]
    public void StreamMethods_ApplyOwnershipOnSuccessAndPreflightFailure()
    {
        IGltfWriter writer = new GltfWriter();
        GltfWriteOptions leaveOpen = CreateOptions(GltfContainerFormat.Json);
        leaveOpen.LeaveStreamOpen = true;
        GltfWriteOptions dispose = CreateOptions(GltfContainerFormat.Json);
        dispose.LeaveStreamOpen = false;
        TrackingNonSeekableStream openDocument = new TrackingNonSeekableStream([], false, true, 1);
        TrackingNonSeekableStream ownedDocument = new TrackingNonSeekableStream([], false, true, 1);
        writer.WriteStream(new GltfDocument(), openDocument, leaveOpen);
        writer.WriteStream(new GltfDocument(), ownedDocument, dispose);

        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Json;
        container.JsonBytes = MinimalJson;
        TrackingNonSeekableStream openContainer = new TrackingNonSeekableStream([], false, true, 1);
        TrackingNonSeekableStream ownedContainer = new TrackingNonSeekableStream([], false, true, 1);
        writer.WriteContainerStream(container, openContainer, leaveOpen);
        writer.WriteContainerStream(container, ownedContainer, dispose);

        GltfDocument invalidDocument = new GltfDocument();
        invalidDocument.HasDefaultScene = true;
        invalidDocument.DefaultScene = 0;
        TrackingNonSeekableStream failedOpen = new TrackingNonSeekableStream([9], false, true, 1);
        TrackingNonSeekableStream failedOwned = new TrackingNonSeekableStream([8], false, true, 1);
        Assert.That(
            () => writer.WriteStream(invalidDocument, failedOpen, leaveOpen),
            Throws.TypeOf<GltfWriteException>());
        Assert.That(
            () => writer.WriteStream(invalidDocument, failedOwned, dispose),
            Throws.TypeOf<GltfWriteException>());

        Assert.Multiple(() =>
        {
            Assert.That(openDocument.Disposed, Is.False);
            Assert.That(ownedDocument.Disposed, Is.True);
            Assert.That(openContainer.Disposed, Is.False);
            Assert.That(ownedContainer.Disposed, Is.True);
            Assert.That(failedOpen.Disposed, Is.False);
            Assert.That(failedOwned.Disposed, Is.True);
            Assert.That(openDocument.GetBytes(), Is.EqualTo(MinimalJson));
            Assert.That(ownedDocument.GetBytes(), Is.EqualTo(MinimalJson));
            Assert.That(openContainer.GetBytes(), Is.EqualTo(MinimalJson));
            Assert.That(ownedContainer.GetBytes(), Is.EqualTo(MinimalJson));
            Assert.That(failedOpen.GetBytes(), Is.EqualTo(new byte[] { 9 }));
            Assert.That(failedOwned.GetBytes(), Is.EqualTo(new byte[] { 8 }));
        });
    }

    [Test]
    public void WriteContainerBytes_WritesExactGlbLengthsChunksAndPadding()
    {
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Glb;
        container.JsonBytes = [0x7B, 0x7D];
        container.HasBinChunk = true;
        container.BinBytes = [1, 2, 3];
        GltfGlbChunk unknown = new GltfGlbChunk();
        unknown.Type = 0xA1B2C3D4U;
        unknown.Data = [4, 5, 6, 7];
        container.UnknownChunks.Add(unknown);
        GltfWriteOptions options = CreateOptions(GltfContainerFormat.Glb);
        IGltfWriter writer = new GltfWriter();

        byte[] first = writer.WriteContainerBytes(container, options).PrimaryBytes;
        byte[] second = writer.WriteContainerBytes(container, options).PrimaryBytes;
        options.PreserveUnknownChunks = false;
        byte[] omitted = writer.WriteContainerBytes(container, options).PrimaryBytes;

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Has.Length.EqualTo(48));
            Assert.That(ReadUInt32(first, 0), Is.EqualTo(0x46546C67U));
            Assert.That(ReadUInt32(first, 4), Is.EqualTo(2U));
            Assert.That(ReadUInt32(first, 8), Is.EqualTo((uint)first.Length));
            Assert.That(ReadUInt32(first, 12), Is.EqualTo(4U));
            Assert.That(ReadUInt32(first, 16), Is.EqualTo(0x4E4F534AU));
            Assert.That(first[20..24], Is.EqualTo(new byte[] { 0x7B, 0x7D, 0x20, 0x20 }));
            Assert.That(ReadUInt32(first, 24), Is.EqualTo(4U));
            Assert.That(ReadUInt32(first, 28), Is.EqualTo(0x004E4942U));
            Assert.That(first[32..36], Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
            Assert.That(ReadUInt32(first, 36), Is.EqualTo(4U));
            Assert.That(ReadUInt32(first, 40), Is.EqualTo(0xA1B2C3D4U));
            Assert.That(first[44..48], Is.EqualTo(new byte[] { 4, 5, 6, 7 }));
            Assert.That(omitted, Has.Length.EqualTo(36));
            Assert.That(ReadUInt32(omitted, 8), Is.EqualTo((uint)omitted.Length));
        });
    }

    [Test]
    public void DocumentMethods_CompletePreflightBeforeModifyingDestinations()
    {
        GltfDocument document = new GltfDocument();
        document.HasDefaultScene = true;
        document.DefaultScene = 0;
        GltfWriteOptions options = CreateOptions(GltfContainerFormat.Json);
        IGltfWriter writer = new GltfWriter();
        using MemoryStream stream = new MemoryStream();
        stream.WriteByte(9);
        string path = Path.Combine(
            Path.GetTempPath(),
            $"gltf-writer-preflight-contract-{Guid.NewGuid():N}.gltf");
        File.WriteAllBytes(path, [8]);

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    () => writer.WriteBytes(document, options),
                    Throws.TypeOf<GltfWriteException>());
                Assert.That(
                    () => writer.WriteJsonText(document, options),
                    Throws.TypeOf<GltfWriteException>());
                Assert.That(
                    () => writer.WriteStream(document, stream, options),
                    Throws.TypeOf<GltfWriteException>());
                Assert.That(
                    () => writer.WriteFile(document, path, options),
                    Throws.TypeOf<GltfWriteException>());
            });
            Assert.Multiple(() =>
            {
                Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 9 }));
                Assert.That(File.ReadAllBytes(path), Is.EqualTo(new byte[] { 8 }));
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void WriteBytes_RepeatsByteIdenticalJsonAndGlbOutput()
    {
        GltfDocument document = new GltfDocument();
        document.Asset.Generator = "deterministic";
        document.PropertyJson.Extras = new JsonObject
        {
            ["z"] = 2,
            ["a"] = 1
        };
        document.PropertyJson.HasExtras = true;
        IGltfWriter writer = new GltfWriter();
        GltfWriteOptions jsonOptions = CreateOptions(GltfContainerFormat.Json);
        GltfWriteOptions glbOptions = CreateOptions(GltfContainerFormat.Glb);

        byte[] json = writer.WriteBytes(document, jsonOptions).PrimaryBytes;
        byte[] repeatedJson = writer.WriteBytes(document, jsonOptions).PrimaryBytes;
        byte[] glb = writer.WriteBytes(document, glbOptions).PrimaryBytes;
        byte[] repeatedGlb = writer.WriteBytes(document, glbOptions).PrimaryBytes;

        Assert.Multiple(() =>
        {
            Assert.That(repeatedJson, Is.EqualTo(json));
            Assert.That(repeatedJson, Is.Not.SameAs(json));
            Assert.That(repeatedGlb, Is.EqualTo(glb));
            Assert.That(repeatedGlb, Is.Not.SameAs(glb));
            Assert.That(
                Encoding.UTF8.GetString(json),
                Is.EqualTo(
                    "{\"asset\":{\"version\":\"2.0\",\"generator\":\"deterministic\"},"
                    + "\"extras\":{\"a\":1,\"z\":2}}"));
        });
    }

    private static GltfWriteOptions CreateOptions(GltfContainerFormat format)
    {
        GltfWriteOptions result = new GltfWriteOptions();
        result.Format = format;
        result.Validate = false;
        return result;
    }

    private static JsonObject ParseObject(byte[] bytes)
    {
        return JsonNode.Parse(Encoding.UTF8.GetString(bytes))!.AsObject();
    }

    private static uint ReadUInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset]
            | ((uint)bytes[offset + 1] << 8)
            | ((uint)bytes[offset + 2] << 16)
            | ((uint)bytes[offset + 3] << 24);
    }
}
