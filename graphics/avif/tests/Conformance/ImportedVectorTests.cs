using System.IO;
using System.Security.Cryptography;

namespace avif.Tests;

public class ImportedVectorTests
{
    [TestCase(
        "fox.profile0.8bpc.yuv420.monochrome.avif",
        "f13aa066c84980708bf560a19768982c4e02a2f6c3f602f5dbf5193aab80fd4c",
        1204,
        800,
        1)]
    [TestCase(
        "fox.profile0.8bpc.yuv420.odd-width.odd-height.avif",
        "707a6a17554f0d3c89af3b39df00bfacc2b38dde2e9b2ac4900c886e50f0cfc6",
        1203,
        799,
        3)]
    public void OfficialAvifVectorsProbeValidateAndExposeExpectedItemModel(
        string fileName,
        string expectedHash,
        int expectedWidth,
        int expectedHeight,
        int expectedChannels)
    {
        byte[] source = File.ReadAllBytes(GetVectorPath(fileName));
        AvifProbeResult probe = Avif.Probe(source);
        AvifValidationResult validation = Avif.Validate(source);
        AvifDocument? document = validation.GetDocument();
        AvifFileInfo? fileInfo = (document ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo();
        AvifImageItem? item = document.FindItem((fileInfo ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId());
        AvifItemProperty? configuration = FindProperty(item, "av1C");
        AvifItemProperty? channels = FindProperty(item, "pixi");

        Assert.Multiple(() =>
        {
            Assert.That(GetSha256(source), Is.EqualTo(expectedHash));
            Assert.That(probe.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(probe.IsAvif(), Is.True);
            Assert.That(validation.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That(validation.GetFindingCount(), Is.Zero);
            Assert.That(fileInfo.HasBrand("avif"), Is.True);
            Assert.That(fileInfo.HasBrand("mif1"), Is.True);
            Assert.That(fileInfo.HasBrand("miaf"), Is.True);
            Assert.That(item, Is.Not.Null);
            Assert.That((item ?? throw new InvalidOperationException("Invalid internal state.")).IsPrimary(), Is.True);
            Assert.That(item.IsCodedImage(), Is.True);
            Assert.That(item.GetItemType(), Is.EqualTo("av01"));
            Assert.That(item.GetPixelWidth(), Is.EqualTo((uint)expectedWidth));
            Assert.That(item.GetPixelHeight(), Is.EqualTo((uint)expectedHeight));
            Assert.That(item.GetPixelBitDepth(), Is.EqualTo(8));
            Assert.That(configuration, Is.Not.Null);
            Assert.That((configuration ?? throw new InvalidOperationException("Invalid internal state.")).GetAv1Profile(), Is.Zero);
            Assert.That(configuration.GetBitDepth(), Is.EqualTo(8));
            Assert.That(channels, Is.Not.Null);
            Assert.That((channels ?? throw new InvalidOperationException("Invalid internal state.")).GetPixelChannelCount(), Is.EqualTo(expectedChannels));
        });

        AvifReader reader = AvifReader.Create(source);
        Assert.That(reader.Parse(), Is.EqualTo(AvifResultCode.Success));
        byte[] payload = reader.GetEncodedItem((item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemId());
        Assert.That(payload, Is.Not.Empty);
        Av1ObuReader obuReader = Av1ObuReader.Create(payload);
        Assert.That(obuReader.ReadNext(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(obuReader.GetObuType(), Is.EqualTo(1));
        Assert.That(obuReader.GetSequenceHeader(), Is.Not.Empty);
        Assert.That(obuReader.ReadNext(), Is.EqualTo(AvifResultCode.Success));
        Assert.That(obuReader.GetObuType(), Is.EqualTo(6));
        Assert.That(obuReader.ReadNext(), Is.EqualTo(AvifResultCode.EndOfData));

        obuReader.Close();
        reader.Close();
    }

    [Test]
    public void ImportedAv1VectorMatchesContainerPayloadAndHasExpectedObus()
    {
        byte[] avifSource = File.ReadAllBytes(GetVectorPath(
            "fox.profile0.8bpc.yuv420.monochrome.avif"));
        byte[] obuSource = File.ReadAllBytes(GetVectorPath(
            "fox.profile0.8bpc.yuv420.monochrome.obu"));
        AvifReader container = AvifReader.Create(avifSource);
        Assert.That(container.Parse(), Is.EqualTo(AvifResultCode.Success));
        uint itemId = ((container.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId();
        byte[] extracted = container.GetEncodedItem(itemId);

        Av1ObuReader reader = Av1ObuReader.Create(obuSource);
        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.Success));
        byte[] sequenceHeader = reader.GetSequenceHeader();
        sequenceHeader[0] ^= 255;
        byte[] unchangedHeader = reader.GetSequenceHeader();
        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.Success));

        Assert.Multiple(() =>
        {
            Assert.That(GetSha256(obuSource), Is.EqualTo(
                "2f9c57c38826e7eaf2ac3dd25193010418cb0af37943b326bad41d9a3cd1ffc4"));
            Assert.That(obuSource, Is.EqualTo(extracted));
            Assert.That(unchangedHeader[0], Is.Not.EqualTo(sequenceHeader[0]));
            Assert.That(reader.GetObuType(), Is.EqualTo(6));
            Assert.That(reader.GetOffset(), Is.EqualTo(9));
            Assert.That(reader.GetSize(), Is.EqualTo(55320));
            Assert.That(reader.GetPayload(), Is.Not.Empty);
        });
        Assert.That(reader.ReadNext(), Is.EqualTo(AvifResultCode.EndOfData));

        reader.Close();
        container.Close();
    }

    static AvifItemProperty? FindProperty(AvifImageItem? item, string type)
    {
        int index = 0;
        while (index < (item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount())
        {
            AvifItemProperty? property = item.GetProperty(index);
            if ((property ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName() == type)
            {
                return property;
            }
            index++;
        }
        return null;
    }

    static string GetVectorPath(string fileName)
    {
        DirectoryInfo? directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null
            && !File.Exists(Path.Combine(directory.FullName, "todo.md")))
        {
            directory = directory.Parent;
        }
        Assert.That(directory, Is.Not.Null, "Could not locate the repository root.");
        string path = Path.Combine(
            (directory ?? throw new InvalidOperationException("Invalid internal state.")).FullName,
            "tests",
            "Vectors",
            "AOMedia-Link-U",
            fileName);
        Assert.That(File.Exists(path), Is.True, "Missing imported vector: " + path);
        return path;
    }

    static string GetSha256(byte[] data)
    {
        return Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    }
}
