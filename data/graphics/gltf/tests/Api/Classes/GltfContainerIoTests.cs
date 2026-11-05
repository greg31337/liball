using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfContainerIoTests
{
    [Test]
    public void ContainerBytes_ReadsAndRepackagesGlbWithoutConstructingDocument()
    {
        GltfContainerData source = CreateGlbContainer();
        byte[] input = new GlbWriter().WriteBytes(source, true);
        byte[] expectedInput = (byte[])input.Clone();
        GltfReadOptions readOptions = new GltfReadOptions();
        GltfReadResult readResult = new GltfReader().ReadContainerBytes(input, readOptions);
        Assert.That(readResult.Container, Is.Not.Null);
        GltfContainerData parsed = readResult.Container!;
        input[20] = 0;

        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Format = GltfContainerFormat.Auto;
        GltfWriteResult writeResult = new GltfWriter().WriteContainerBytes(parsed, writeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(readResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(readResult.Document, Is.Null);
            Assert.That(parsed.Format, Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(parsed.JsonBytes, Is.EqualTo(new byte[] { 0x7B, 0x7D, 0x20, 0x20 }));
            Assert.That(parsed.HasBinChunk, Is.True);
            Assert.That(parsed.BinBytes, Is.EqualTo(new byte[] { 1, 2, 3, 0 }));
            Assert.That(parsed.UnknownChunks, Has.Count.EqualTo(1));
            Assert.That(parsed.UnknownChunks[0].Type, Is.EqualTo(0xA1B2C3D4U));
            Assert.That(parsed.OriginalByteLength, Is.EqualTo(expectedInput.Length));
            Assert.That(writeResult.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(writeResult.PrimaryBytes, Is.EqualTo(expectedInput));
        });
    }

    [Test]
    public void ContainerBytes_PreservesExactOwnedJsonBytes()
    {
        byte[] input = Encoding.UTF8.GetBytes("\uFEFF \n{\"name\":\"é\"}");
        byte[] expected = (byte[])input.Clone();
        GltfReadResult readResult = new GltfReader().ReadContainerBytes(input, new GltfReadOptions());
        Assert.That(readResult.Container, Is.Not.Null);
        GltfContainerData parsed = readResult.Container!;
        input[0] = 0;

        GltfWriteOptions writeOptions = new GltfWriteOptions();
        writeOptions.Format = GltfContainerFormat.Auto;
        GltfWriteResult writeResult = new GltfWriter().WriteContainerBytes(parsed, writeOptions);
        parsed.JsonBytes[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Format, Is.EqualTo(GltfContainerFormat.Json));
            Assert.That(parsed.OriginalByteLength, Is.EqualTo(expected.Length));
            Assert.That(writeResult.PrimaryBytes, Is.EqualTo(expected));
        });
    }

    [Test]
    public void ReadContainerBytes_AppliesFormatAndSizePoliciesBeforeDocumentParsing()
    {
        GltfReader reader = new GltfReader();
        GltfReadOptions inputLimit = new GltfReadOptions();
        inputLimit.Limits.MaximumInputByteLength = 1;
        GltfReadOptions jsonLimit = new GltfReadOptions();
        jsonLimit.Limits.MaximumJsonByteLength = 1;
        GltfReadOptions omitUnknown = new GltfReadOptions();
        omitUnknown.PreserveUnknownChunks = false;
        byte[] glb = new GlbWriter().WriteBytes(CreateGlbContainer(), true);

        GltfReadResult omittedResult = reader.ReadContainerBytes(glb, omitUnknown);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadContainerBytes([0x7B, 0x7D], inputLimit),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("Input length 2 exceeds the configured maximum 1 at byte 1."));
            Assert.That(
                () => reader.ReadContainerBytes([0x7B, 0x7D], jsonLimit),
                Throws.TypeOf<GltfParseException>()
                    .With.Message.EqualTo("JSON content length 2 exceeds the configured maximum 1."));
            Assert.That(omittedResult.Container, Is.Not.Null);
            Assert.That(omittedResult.Container!.UnknownChunks, Is.Empty);
        });
    }

    [Test]
    public void WriteContainerBytes_UsesExplicitOutputFormatAndUnknownChunkPolicy()
    {
        GltfContainerData source = CreateGlbContainer();
        GltfWriteOptions jsonOptions = new GltfWriteOptions();
        jsonOptions.Format = GltfContainerFormat.Json;
        GltfWriteOptions glbOptions = new GltfWriteOptions();
        glbOptions.Format = GltfContainerFormat.Glb;
        glbOptions.PreserveUnknownChunks = false;
        GltfWriter writer = new GltfWriter();

        GltfWriteResult json = writer.WriteContainerBytes(source, jsonOptions);
        GltfWriteResult glb = writer.WriteContainerBytes(source, glbOptions);
        GlbReader glbReader = new GlbReader();
        GltfContainerData parsed = glbReader.ReadChunks(
            glb.PrimaryBytes,
            glbReader.ReadHeader(glb.PrimaryBytes, glb.PrimaryBytes.Length),
            true);

        Assert.Multiple(() =>
        {
            Assert.That(json.PrimaryBytes, Is.EqualTo(source.JsonBytes));
            Assert.That(glb.PrimaryBytes[0..4], Is.EqualTo(new byte[] { 0x67, 0x6C, 0x54, 0x46 }));
            Assert.That(parsed.UnknownChunks, Is.Empty);
        });
    }

    [Test]
    public void ContainerBytes_RejectsUnspecifiedOutputAndInvalidArguments()
    {
        GltfReader reader = new GltfReader();
        GltfWriter writer = new GltfWriter();
        GltfContainerData unspecified = new GltfContainerData();
        unspecified.JsonBytes = [0x7B, 0x7D];
        GltfWriteOptions auto = new GltfWriteOptions();
        auto.Format = GltfContainerFormat.Auto;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => reader.ReadContainerBytes(null!, new GltfReadOptions()),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("bytes"));
            Assert.That(
                () => reader.ReadContainerBytes([0x7B, 0x7D], null!),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("options"));
            Assert.That(
                () => writer.WriteContainerBytes(unspecified, auto),
                Throws.TypeOf<GltfWriteException>()
                    .With.Message.EqualTo("The raw container output format is not specified."));
        });
    }

    private static GltfContainerData CreateGlbContainer()
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
        return container;
    }
}
