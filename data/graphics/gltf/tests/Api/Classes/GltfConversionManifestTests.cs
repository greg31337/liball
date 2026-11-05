using System.Security.Cryptography;
using System.Text;

namespace gltf.Tests.Api.Classes;

[TestFixture]
public class GltfConversionManifestTests
{
    [Test]
    public void ConvertDocument_ReportsEffectiveExternalResourceFactsAndPrimaryHash()
    {
        GltfDocument source = CreateResolvedJsonDocument();
        GltfConversionOptions options = CreateOptions(
            GltfContainerFormat.Json,
            GltfResourceMode.Preserve);

        GltfConversionResult result = new GltfConverter().ConvertDocument(
            source,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Manifest, Has.Count.EqualTo(1));
            Assert.That(result.Manifest[0].Kind,
                Is.EqualTo(GltfResourceKind.Buffer));
            Assert.That(result.Manifest[0].Uri, Is.EqualTo("geometry.bin"));
            Assert.That(result.Manifest[0].EffectiveUri,
                Is.EqualTo("geometry.bin"));
            Assert.That(result.Manifest[0].MimeType,
                Is.EqualTo(GltfMimeTypes.GltfBuffer));
            Assert.That(result.Manifest[0].ByteLength, Is.EqualTo(4));
            Assert.That(result.Manifest[0].ContentHash,
                Is.EqualTo(Hash([1, 2, 3, 4])));
            Assert.That(result.Manifest[0].Embedded, Is.False);
            Assert.That(result.Manifest[0].External, Is.True);
            Assert.That(result.Manifest[0].Mode,
                Is.EqualTo(GltfResourceMode.Preserve));
            Assert.That(result.PrimaryContentHash,
                Is.EqualTo(Hash(result.PrimaryBytes)));
        });
    }

    [Test]
    public void ConvertDocument_ReportsDataUriAndGlbBinAsEmbedded()
    {
        GltfConversionOptions dataOptions = CreateOptions(
            GltfContainerFormat.Json,
            GltfResourceMode.DataUri);
        GltfConversionOptions glbOptions = CreateOptions(
            GltfContainerFormat.Glb,
            GltfResourceMode.GlbBin);

        GltfConversionResult dataResult = new GltfConverter().ConvertDocument(
            CreateResolvedJsonDocument(),
            dataOptions);
        GltfConversionResult glbResult = new GltfConverter().ConvertDocument(
            CreateResolvedJsonDocument(),
            glbOptions);

        Assert.Multiple(() =>
        {
            Assert.That(dataResult.Manifest, Has.Count.EqualTo(1));
            Assert.That(dataResult.Manifest[0].EffectiveUri,
                Is.EqualTo("data:application/gltf-buffer;base64,AQIDBA=="));
            Assert.That(dataResult.Manifest[0].Embedded, Is.True);
            Assert.That(dataResult.Manifest[0].External, Is.False);
            Assert.That(dataResult.Manifest[0].Mode,
                Is.EqualTo(GltfResourceMode.DataUri));
            Assert.That(glbResult.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(glbResult.Document!.Container!.Format,
                Is.EqualTo(GltfContainerFormat.Glb));
            Assert.That(glbResult.Document.Container.HasBinChunk, Is.True);
            Assert.That(glbResult.Manifest, Has.Count.EqualTo(1));
            Assert.That(glbResult.Manifest[0].EffectiveUri, Is.Null);
            Assert.That(glbResult.Manifest[0].Embedded, Is.True);
            Assert.That(glbResult.Manifest[0].External, Is.False);
            Assert.That(glbResult.Manifest[0].Mode,
                Is.EqualTo(GltfResourceMode.GlbBin));
            Assert.That(glbResult.Manifest[0].MimeType,
                Is.EqualTo(GltfMimeTypes.GltfBuffer));
            Assert.That(glbResult.Manifest[0].ContentHash,
                Is.EqualTo(Hash([1, 2, 3, 4])));
        });
    }

    [Test]
    public void ConvertDocument_ReportsPreservedExtensionAndUnavailableCodec()
    {
        GltfDocument source = CreateExtensionDocument();
        GltfConversionOptions options = CreateOptions(
            GltfContainerFormat.Json,
            GltfResourceMode.Preserve);

        GltfConversionResult result = new GltfConverter().ConvertDocument(
            source,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Decisions, Has.Count.EqualTo(2));
            Assert.That(result.Decisions[0].Kind,
                Is.EqualTo(GltfConversionDecisionKind.Extension));
            Assert.That(result.Decisions[0].Name,
                Is.EqualTo(GltfExtensionNames.KhrTextureBasisu));
            Assert.That(result.Decisions[0].Action,
                Is.EqualTo(GltfConversionDecisionAction.Preserved));
            Assert.That(result.Decisions[0].Required, Is.False);
            Assert.That(result.Decisions[1].Kind,
                Is.EqualTo(GltfConversionDecisionKind.Codec));
            Assert.That(result.Decisions[1].Name,
                Is.EqualTo(GltfExtensionNames.KhrTextureBasisu));
            Assert.That(result.Decisions[1].RelatedExtension,
                Is.EqualTo(GltfExtensionNames.KhrTextureBasisu));
            Assert.That(result.Decisions[1].Action,
                Is.EqualTo(GltfConversionDecisionAction.Unavailable));
            Assert.That(result.Decisions[1].Status,
                Is.EqualTo(GltfOperationStatus.Unavailable));
        });
    }

    [Test]
    public void ConvertDocument_ReportsRegisteredCodecCapabilityAsAvailable()
    {
        GltfDocument source = CreateExtensionDocument();
        GltfConversionOptions options = CreateOptions(
            GltfContainerFormat.Json,
            GltfResourceMode.Preserve);
        ImageCodecProbe codec = new ImageCodecProbe();
        options.ReadOptions.ImageCodecs.Add(codec);

        GltfConversionResult result = new GltfConverter().ConvertDocument(
            source,
            options);

        GltfConversionDecision decision = result.Decisions.Single(value =>
            value.Kind == GltfConversionDecisionKind.Codec);
        Assert.Multiple(() =>
        {
            Assert.That(decision.Action,
                Is.EqualTo(GltfConversionDecisionAction.Available));
            Assert.That(decision.Status,
                Is.EqualTo(GltfOperationStatus.Success));
            Assert.That(codec.Calls, Is.Zero);
        });
    }

    private static GltfDocument CreateResolvedJsonDocument()
    {
        byte[] bytes = [1, 2, 3, 4];
        GltfDocument result = new GltfDocument();
        result.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Json,
            JsonBytes = Encoding.UTF8.GetBytes(
                "{\"asset\":{\"version\":\"2.0\"}}")
        };
        result.Buffers.Add(new GltfBuffer
        {
            Uri = "geometry.bin",
            ByteLength = bytes.Length,
            Resolved = true,
            ResolvedBytes = bytes,
            HasResource = true,
            Resource = 0
        });
        result.BufferViews.Add(new GltfBufferView
        {
            Buffer = 0,
            ByteLength = bytes.Length
        });
        result.Resources.Resources.Add(new GltfResource
        {
            Kind = GltfResourceKind.Buffer,
            UriKind = GltfUriKind.Relative,
            SourceUri = "geometry.bin",
            EffectiveUri = "/private/input/geometry.bin",
            Bytes = bytes
        });
        return result;
    }

    private static GltfDocument CreateExtensionDocument()
    {
        GltfDocument result = new GltfDocument();
        result.Container = new GltfContainerData
        {
            Format = GltfContainerFormat.Json
        };
        result.ExtensionsUsed.Add(GltfExtensionNames.KhrTextureBasisu);
        return result;
    }

    private static GltfConversionOptions CreateOptions(
        GltfContainerFormat format,
        GltfResourceMode mode)
    {
        GltfConversionOptions result = new GltfConversionOptions();
        result.InputFormat = GltfContainerFormat.Json;
        result.OutputFormat = format;
        result.ResourceMode = mode;
        result.WriteOptions.Validate = false;
        return result;
    }

    private static string Hash(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private sealed class ImageCodecProbe : IGltfImageCodec
    {
        public int Calls;

        public GltfImageInfo InspectImage(GltfImageRequest request)
        {
            Calls++;
            return new GltfImageInfo();
        }

        public GltfImageResult DecodeImage(GltfImageRequest request)
        {
            Calls++;
            return new GltfImageResult();
        }

        public GltfImageResult EncodeImage(GltfImageRequest request)
        {
            Calls++;
            return new GltfImageResult();
        }

        public GltfImageResult TranscodeImage(GltfImageRequest request)
        {
            Calls++;
            return new GltfImageResult();
        }
    }
}
