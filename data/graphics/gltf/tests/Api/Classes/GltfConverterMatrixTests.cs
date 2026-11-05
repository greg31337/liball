using System.Security.Cryptography;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfConverterMatrixTests
{
    [TestCase(GltfContainerFormat.Json, GltfContainerFormat.Json, GltfResourceMode.Preserve)]
    [TestCase(GltfContainerFormat.Json, GltfContainerFormat.Json, GltfResourceMode.External)]
    [TestCase(GltfContainerFormat.Json, GltfContainerFormat.Json, GltfResourceMode.DataUri)]
    [TestCase(GltfContainerFormat.Json, GltfContainerFormat.Json, GltfResourceMode.BufferView)]
    [TestCase(GltfContainerFormat.Json, GltfContainerFormat.Glb, GltfResourceMode.Preserve)]
    [TestCase(GltfContainerFormat.Json, GltfContainerFormat.Glb, GltfResourceMode.GlbBin)]
    [TestCase(GltfContainerFormat.Glb, GltfContainerFormat.Json, GltfResourceMode.Preserve)]
    [TestCase(GltfContainerFormat.Glb, GltfContainerFormat.Json, GltfResourceMode.External)]
    [TestCase(GltfContainerFormat.Glb, GltfContainerFormat.Json, GltfResourceMode.DataUri)]
    [TestCase(GltfContainerFormat.Glb, GltfContainerFormat.Glb, GltfResourceMode.Preserve)]
    [TestCase(GltfContainerFormat.Glb, GltfContainerFormat.Glb, GltfResourceMode.External)]
    [TestCase(GltfContainerFormat.Glb, GltfContainerFormat.Glb, GltfResourceMode.DataUri)]
    [TestCase(GltfContainerFormat.Glb, GltfContainerFormat.Glb, GltfResourceMode.BufferView)]
    [TestCase(GltfContainerFormat.Glb, GltfContainerFormat.Glb, GltfResourceMode.GlbBin)]
    public void ConvertDocument_SupportsEveryValidContainerResourceModePair(
        GltfContainerFormat inputFormat,
        GltfContainerFormat outputFormat,
        GltfResourceMode resourceMode)
    {
        GltfDocument source = CreateResolvedDocument(inputFormat);
        GltfConversionOptions options = CreateOptions(
            inputFormat,
            outputFormat,
            resourceMode);
        string? sourceUri = source.Buffers[0].Uri;
        byte[] sourceBytes = (byte[])source.Buffers[0].ResolvedBytes.Clone();

        GltfConversionResult first = new GltfConverter().ConvertDocument(
            source,
            options);
        GltfConversionResult second = new GltfConverter().ConvertDocument(
            source,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(first.Document, Is.Not.SameAs(source));
            Assert.That(first.Document!.Container!.Format, Is.EqualTo(outputFormat));
            Assert.That(first.PrimaryBytes, Is.Not.Empty);
            Assert.That(first.PrimaryBytes, Is.EqualTo(second.PrimaryBytes));
            Assert.That(first.PrimaryContentHash,
                Is.EqualTo(Convert.ToHexString(
                    SHA256.HashData(first.PrimaryBytes)).ToLowerInvariant()));
            Assert.That(first.Manifest, Is.Not.Empty);
            Assert.That(first.Manifest.Select(item => item.ContentHash),
                Is.All.Match("^[0-9a-f]{64}$"));
            Assert.That(second.Manifest.Select(DescribeManifestEntry),
                Is.EqualTo(first.Manifest.Select(DescribeManifestEntry)));
            Assert.That(source.Container!.Format, Is.EqualTo(inputFormat));
            Assert.That(source.Buffers[0].Uri, Is.EqualTo(sourceUri));
            Assert.That(source.Buffers[0].ResolvedBytes, Is.EqualTo(sourceBytes));
        });

        if (outputFormat == GltfContainerFormat.Glb)
        {
            Assert.That(first.PrimaryBytes.Take(4),
                Is.EqualTo(new byte[] { 0x67, 0x6c, 0x54, 0x46 }));
        }
        else
        {
            Assert.That(first.PrimaryBytes[0], Is.EqualTo((byte)'{'));
        }
    }

    [TestCase(GltfContainerFormat.Json, GltfContainerFormat.Json, GltfResourceMode.GlbBin)]
    [TestCase(GltfContainerFormat.Json, GltfContainerFormat.Glb, GltfResourceMode.External)]
    [TestCase(GltfContainerFormat.Json, GltfContainerFormat.Glb, GltfResourceMode.DataUri)]
    [TestCase(GltfContainerFormat.Json, GltfContainerFormat.Glb, GltfResourceMode.BufferView)]
    [TestCase(GltfContainerFormat.Glb, GltfContainerFormat.Json, GltfResourceMode.BufferView)]
    [TestCase(GltfContainerFormat.Glb, GltfContainerFormat.Json, GltfResourceMode.GlbBin)]
    public void ConvertDocument_RejectsEveryInvalidContainerResourceModePair(
        GltfContainerFormat inputFormat,
        GltfContainerFormat outputFormat,
        GltfResourceMode resourceMode)
    {
        GltfDocument source = CreateResolvedDocument(inputFormat);
        GltfConversionOptions options = CreateOptions(
            inputFormat,
            outputFormat,
            resourceMode);

        Assert.That(
            () => new GltfConverter().ConvertDocument(source, options),
            Throws.TypeOf<GltfWriteException>());
        Assert.Multiple(() =>
        {
            Assert.That(source.Container!.Format, Is.EqualTo(inputFormat));
            Assert.That(source.Buffers[0].ResolvedBytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(source.Resources.Resources[0].Bytes,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        });
    }

    private static GltfDocument CreateResolvedDocument(
        GltfContainerFormat inputFormat)
    {
        GltfDocument result = new GltfDocument();
        result.Container = new GltfContainerData { Format = inputFormat };
        if (inputFormat == GltfContainerFormat.Glb)
        {
            result.Container.HasBinChunk = true;
            result.Container.BinBytes = [1, 2, 3, 4];
        }

        string? uri = inputFormat == GltfContainerFormat.Json
            ? "geometry.bin"
            : null;
        GltfResource resource = new GltfResource
        {
            Kind = GltfResourceKind.Buffer,
            UriKind = uri == null ? GltfUriKind.Absent : GltfUriKind.Relative,
            SourceUri = uri,
            EffectiveUri = uri,
            MimeType = GltfMimeTypes.GltfBuffer,
            Bytes = [1, 2, 3, 4],
            Embedded = inputFormat == GltfContainerFormat.Glb
        };
        result.Resources.Resources.Add(resource);
        result.Buffers.Add(new GltfBuffer
        {
            Uri = uri,
            ByteLength = 4,
            Resolved = true,
            ResolvedBytes = [1, 2, 3, 4],
            HasResource = true,
            Resource = 0
        });
        result.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            ByteLength = 4
        });
        return result;
    }

    private static GltfConversionOptions CreateOptions(
        GltfContainerFormat inputFormat,
        GltfContainerFormat outputFormat,
        GltfResourceMode resourceMode)
    {
        GltfConversionOptions result = new GltfConversionOptions();
        result.InputFormat = inputFormat;
        result.OutputFormat = outputFormat;
        result.ResourceMode = resourceMode;
        result.ReadOptions.ResolveResources = true;
        result.WriteOptions.Validate = false;
        return result;
    }

    private static string DescribeManifestEntry(GltfResourceManifestEntry entry)
    {
        return $"{entry.Kind}|{entry.Mode}|{entry.Uri}|{entry.ByteLength}|"
            + $"{entry.ContentHash}|{entry.Embedded}|{entry.External}";
    }
}
