using raw;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace raw.Tests;

[NonParallelizable]
public class PublicCorpusIntegrationTests
{
    static readonly PublicCorpusCase[] Cases = new PublicCorpusCase[]
    {
        new PublicCorpusCase("files/canon-eos-6d-mark-ii-raw-3x2-1.0.2.cr2", "canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.read", RawFormat.Cr2, "Canon", "Canon EOS 6D Mark II", "980f8520ece3e4ee4f859853591a4d3cc5b037fdb67fd017362417de52b825de", "e8a08c3e49f56e1ef84a0e686f3d5e3dc521f9a36f28606644a20956fd673cbe"),
        new PublicCorpusCase("files/canon-eos-6d-mark-ii-mraw-3x2-1.0.2.cr2", "canon.eos-6d-mark-ii.cr2.mraw-3x2.firmware-1.0.2.read", RawFormat.Cr2, "Canon", "Canon EOS 6D Mark II", "277271c3eb14a58790e2d6a9c349690d155fbaa823bad78585f3b5c02470a904", "7e22a735305bd47f2e8a13a42d7ad43b4c081703548746a049883cabf184d021"),
        new PublicCorpusCase("files/canon-eos-6d-mark-ii-sraw-3x2-1.0.2.cr2", "canon.eos-6d-mark-ii.cr2.sraw-3x2.firmware-1.0.2.read", RawFormat.Cr2, "Canon", "Canon EOS 6D Mark II", "a4362c81695fceae3649c98d37bd4ce60eda813efabf23eed66c99fb7247d1c2", "98657dcf08601989a63658ad3e35c9a8f9d43308586f66b396b623e5d393d0df"),
        new PublicCorpusCase("files/canon-eos-20d-raw-3x2-2.0.3.cr2", "canon.eos-20d.cr2.raw-3x2.firmware-2.0.3.read", RawFormat.Cr2, "Canon", "Canon EOS 20D", "3091a2dff8a88ce85d17fcd0edabeb2143f053afc25028f023657978707686fc", "be96c25e76190aacef3222a95d68135d3fb135a088e12b2a3bb25e7ccf4d04e0"),
        new PublicCorpusCase("files/canon-eos-1d-raw-3x2-1.4.0.tif", "canon.eos-1d.tiff.raw-3x2.firmware-1.4.0.read", RawFormat.Tiff, "Canon", "Canon EOS-1D", "616d501e21bf228be781333e7fa1fb5cca88e836262d439348f74b18b9f3f7ad", "2694c0b8e45e99c1c957a9517acca59adde69499d35b9db1b1b8c8cbd8f47a18"),
        new PublicCorpusCase("generated/canon-eos-6d-mark-ii-raw-3x2-write-golden.cr2", "canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.read", RawFormat.Cr2, "Canon", "Canon EOS 6D Mark II", "54a247a35ce8f32cc3b9b70187510252158c4c8e9e00a406da1d76134373589e", "00cb8b872819236ba12b2a9b869458086a61f4a24434fd58c90a26449990664e"),
        new PublicCorpusCase("generated/nikon-z-7-uncompressed-14bit-fx-write-golden.nef", "nikon.z-7.nef.uncompressed-14bit.fx.read.experimental", RawFormat.Nef, "NIKON CORPORATION", "NIKON Z 7", "d91b5934dc0e01c057178348eb48ab1ca42831fb4200b0c61065e16377e305c1", "70f846c1cc208186539e35e737303916ba187f3ec259e2a4bd5a77016ce17478"),
        new PublicCorpusCase("generated/nikon-z-8-lossless-14bit-fx-write-golden.nef", "nikon.z-8.nef.lossless-14bit.fx.read.experimental", RawFormat.Nef, "NIKON CORPORATION", "NIKON Z 8", "f63e4105f9805c435548e174a5d69c6d32929c8e7a639922aec0ff762c95629d", "d6b3e84d00ea9b95da0e881f018295f9e4db2bc8bc5086b3fa75706462eef414")
    };

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void MaterializedCorpusDecodesAndRendersThroughPublicContracts(int caseIndex)
    {
        PublicCorpusCase item = Cases[caseIndex];
        IRawCodec codec = new RawCodecService();
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.AllowExperimentalProfiles = true;
        openOptions.ReadPreviews = true;

        RawDocument document = codec.OpenPath(CorpusPath(item.RelativePath), null, openOptions);
        RawSensorImage decoded = codec.Decode(document, new RawDecodeOptions());
        RawRgbImage rendered = codec.RenderSensorImage(decoded, RenderOptions(decoded.Info.Cfa != null));
        RawCapabilities capabilities = document.Capabilities;
        RawValidationOptions validationOptions = new RawValidationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(document.Format, Is.EqualTo(item.Format));
            Assert.That(document.ProfileName, Is.EqualTo(item.ProfileId));
            Assert.That(document.Metadata.Make, Is.EqualTo(item.Make));
            Assert.That(document.Metadata.Model, Is.EqualTo(item.Model));
            Assert.That(HashSamples(decoded.IntegerSamples), Is.EqualTo(item.DecodedHash));
            Assert.That(HashBytes(rendered.ByteSamples), Is.EqualTo(item.RenderHash));
            Assert.That(capabilities.Recognized, Is.True);
            Assert.That(capabilities.Inspected, Is.True);
            Assert.That(capabilities.Decoded, Is.True);
            Assert.That(capabilities.Rendered, Is.True);
            Assert.That(capabilities.PreservingRewrite, Is.EqualTo(item.Format == RawFormat.Cr2 || item.Format == RawFormat.Nef));
            Assert.That(capabilities.VendorConfirmed, Is.False);
            Assert.That(codec.ValidateSensorImage(decoded, validationOptions).IsValid(), Is.True);
            Assert.That(codec.ValidateRgbImage(rendered, validationOptions).IsValid(), Is.True);
        });
    }

    [Test]
    public void EveryBuiltInReadAndWriteProfileMatchesExactlyThroughPublicCatalogue()
    {
        IRawCodec codec = new RawCodecService();
        RawProfileCatalogue concrete = codec.CreateBuiltInProfileCatalogue(new RawLimits());
        IRawProfileCatalogue catalogue = concrete;
        IRawProfileRegistry registry = concrete;
        RawReadProfile[] reads = catalogue.GetReadProfiles();
        RawWriteProfile[] writes = catalogue.GetWriteProfiles();

        Assert.Multiple(() =>
        {
            Assert.That(reads, Has.Length.EqualTo(7));
            Assert.That(writes, Has.Length.EqualTo(3));
            Assert.That(reads.Select(profile => profile.Id), Is.Unique);
            Assert.That(writes.Select(profile => profile.Id), Is.Unique);
            Assert.That(registry.GetRegistrationDiagnostics().Succeeded, Is.True);
        });

        foreach (RawReadProfile profile in reads)
        {
            RawProfileMatchInput enabledInput = MatchInput(profile.Identity, profile.Fingerprint, true);
            RawProfileMatchInput blockedInput = MatchInput(profile.Identity, profile.Fingerprint, false);
            RawProfileMatchResult enabled = catalogue.MatchReadProfile(enabledInput);
            RawProfileMatchResult blocked = catalogue.MatchReadProfile(blockedInput);

            Assert.Multiple(() =>
            {
                Assert.That(profile.Experimental, Is.True, profile.Id);
                Assert.That(enabled.Matched, Is.True, profile.Id);
                Assert.That((enabled.ReadProfile ?? throw new InvalidOperationException()).Id, Is.EqualTo(profile.Id));
                Assert.That(blocked.Matched, Is.False, profile.Id);
                Assert.That(blocked.Candidates.Single(candidate => candidate.ProfileId == profile.Id).Eligible, Is.False, profile.Id);
                Assert.That(blocked.Diagnostics.Any(diagnostic => diagnostic.Code == RawDiagnosticCode.UnsupportedProfile), Is.True, profile.Id);
            });
        }

        foreach (RawWriteProfile profile in writes)
        {
            RawProfileMatchResult enabled = catalogue.MatchWriteProfile(MatchInput(profile.Identity, profile.Fingerprint, true));
            RawProfileMatchResult blocked = catalogue.MatchWriteProfile(MatchInput(profile.Identity, profile.Fingerprint, false));
            RawWriteCompatibilityMatrix compatibility = catalogue.GetWriteCompatibilityMatrix(profile.Identity.Format);

            Assert.Multiple(() =>
            {
                Assert.That(profile.Experimental, Is.True, profile.Id);
                Assert.That(catalogue.FindReadProfile(profile.ReadProfileId), Is.Not.Null, profile.Id);
                Assert.That(enabled.Matched, Is.True, profile.Id);
                Assert.That((enabled.WriteProfile ?? throw new InvalidOperationException()).Id, Is.EqualTo(profile.Id));
                Assert.That(blocked.Matched, Is.False, profile.Id);
                Assert.That(blocked.Candidates.Single(candidate => candidate.ProfileId == profile.Id).Eligible, Is.False, profile.Id);
                Assert.That(compatibility.FindProfile(profile.Id), Is.Not.Empty, profile.Id);
                Assert.That(compatibility.FindProfile(profile.Id), Is.All.Matches<RawWriteCompatibilityEntry>(entry => entry.Experimental));
            });
        }
    }

    [Test]
    public void GenericRawAdvertisedCapabilitiesCompleteOnePublicPlanDecodeAndRenderCycle()
    {
        IRawCodec codec = new RawCodecService();
        IRawWriter writer = new RawWriterService();
        RawDescriptor descriptor = codec.CreateBayerDescriptor(4, 4, 12, 16, RawByteOrder.LittleEndian, RawCfaLayout.Rggb);
        uint[] samples = new uint[] { 0, 512, 1024, 1536, 256, 768, 1280, 1792, 2048, 2304, 2560, 2816, 3072, 3328, 3584, 4095 };
        RawWritePlan plan = writer.PlanGenericUnsigned(descriptor, samples, 0, 4, new RawGenericEncodeOptions());
        byte[] encoded = writer.EncodeGenericUnsigned(descriptor, samples, 0, 4, new RawGenericEncodeOptions());
        RawOpenOptions openOptions = new RawOpenOptions();
        openOptions.ReadPreviews = false;
        RawDocument document = codec.Open(encoded, descriptor, openOptions);
        RawDecodeOptions decodeOptions = new RawDecodeOptions();
        decodeOptions.PreserveStoredSamples = true;
        RawSensorImage decoded = codec.Decode(document, decodeOptions);
        RawRgbImage rendered = codec.RenderSensorImage(decoded, RenderOptions(true));
        RawCapabilities capabilities = codec.GetCapabilities(RawFormat.GenericRaw, descriptor);

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanWrite, Is.True);
            Assert.That(plan.PredictedSize, Is.EqualTo(encoded.Length));
            Assert.That((decoded.StoredImage ?? throw new InvalidOperationException()).UnsignedSamples, Is.EqualTo(samples));
            Assert.That(rendered.Width, Is.EqualTo(64));
            Assert.That(rendered.Height, Is.EqualTo(64));
            Assert.That(capabilities.Recognized, Is.True);
            Assert.That(capabilities.Inspected, Is.True);
            Assert.That(capabilities.Decoded, Is.True);
            Assert.That(capabilities.Rendered, Is.True);
            Assert.That(capabilities.Encoded, Is.True);
            Assert.That(capabilities.PreservingRewrite, Is.False);
            Assert.That(capabilities.VendorConfirmed, Is.False);
        });
    }

    [Test]
    public void IndependentRawDecoderIdentifiesMaterializedCorpusWhenAvailable()
    {
        ExternalToolAdapter tool = IndependentToolAdapters.LibRaw();
        if (!tool.IsAvailable())
        {
            tool = IndependentToolAdapters.Dcraw();
        }
        tool.IgnoreIfUnavailable();
        string executable = Path.GetFileNameWithoutExtension(tool.ExecutablePath ?? string.Empty);
        string[] arguments = executable.Equals("raw-identify", StringComparison.OrdinalIgnoreCase) ? Array.Empty<string>() : new string[] { "-i", "-v" };

        foreach (PublicCorpusCase item in Cases)
        {
            string path = CorpusPath(item.RelativePath);
            ExternalToolResult result = tool.RunOnPath(arguments, path, Path.GetDirectoryName(path) ?? CorpusRoot());

            Assert.Multiple(() =>
            {
                Assert.That(result.TimedOut, Is.False, item.RelativePath);
                Assert.That(result.ExitCode, Is.Zero, result.StandardError);
                Assert.That(result.StandardOutput + result.StandardError, Is.Not.Empty, item.RelativePath);
            });
        }
    }

    [Test]
    public void ExifToolIdentifiesCorpusCameraMetadataWhenAvailable()
    {
        ExternalToolAdapter tool = IndependentToolAdapters.ExifTool();
        tool.IgnoreIfUnavailable();

        foreach (PublicCorpusCase item in Cases)
        {
            ExternalToolResult result = tool.RunOnBytes(new string[] { "-s", "-Make", "-Model" }, File.ReadAllBytes(CorpusPath(item.RelativePath)), Path.GetExtension(item.RelativePath));

            Assert.Multiple(() =>
            {
                Assert.That(result.TimedOut, Is.False, item.RelativePath);
                Assert.That(result.ExitCode, Is.Zero, result.StandardError);
                Assert.That(result.StandardOutput, Does.Contain(item.Make), item.RelativePath);
                Assert.That(result.StandardOutput, Does.Contain(item.Model), item.RelativePath);
            });
        }
    }

    static RawProfileMatchInput MatchInput(RawProfileIdentity identity, RawProfileFingerprint fingerprint, bool allowExperimental)
    {
        RawProfileMatchInput input = new RawProfileMatchInput();
        input.Format = identity.Format;
        input.Make = identity.Make;
        input.Model = identity.Model ?? string.Empty;
        input.CameraId = identity.CameraId;
        input.Firmware = identity.FirmwareMinimum;
        input.HasByteOrder = true;
        input.ByteOrder = fingerprint.ByteOrder;
        input.HasBigTiff = true;
        input.BigTiff = fingerprint.BigTiff;
        input.Width = fingerprint.Width;
        input.Height = fingerprint.Height;
        input.BitDepth = fingerprint.BitDepth;
        input.Compression = fingerprint.Compression;
        input.SamplesPerPixel = fingerprint.SamplesPerPixel;
        input.IfdPaths = fingerprint.RequiredIfds;
        input.Tags = fingerprint.RequiredTags;
        input.MakerNoteSignature = fingerprint.MakerNoteSignature;
        input.StructuralHash = fingerprint.StructuralHash;
        input.AllowExperimental = allowExperimental;
        return input;
    }

    static RawRenderOptions RenderOptions(bool halfSize)
    {
        RawRenderOptions options = new RawRenderOptions();
        options.DemosaicMode = RawDemosaicMode.Bilinear;
        options.WhiteBalanceMode = RawWhiteBalanceMode.None;
        options.ColorSpace = RawColorSpace.RawCamera;
        options.TransferFunction = RawTransferFunction.Linear;
        options.GamutMode = RawGamutMode.Clip;
        options.OutputBitDepth = 8;
        options.OutputWidth = 64;
        options.OutputHeight = 64;
        options.ApplyOrientation = true;
        options.HalfSize = halfSize;
        return options;
    }

    static string HashSamples(ushort[] samples)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] bytes = new byte[8192];
        int sample = 0;
        while (sample < samples.Length)
        {
            int count = Math.Min(bytes.Length / 2, samples.Length - sample);
            int index = 0;
            while (index < count)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(index * 2, 2), samples[sample + index]);
                index++;
            }
            hash.AppendData(bytes, 0, count * 2);
            sample += count;
        }
        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

    static string HashBytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    static string CorpusPath(string relativePath)
    {
        return Path.Combine(CorpusRoot(), relativePath);
    }

    static string CorpusRoot()
    {
        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../corpus"));
    }
}

internal class PublicCorpusCase
{
    internal string RelativePath;
    internal string ProfileId;
    internal RawFormat Format;
    internal string Make;
    internal string Model;
    internal string DecodedHash;
    internal string RenderHash;

    internal PublicCorpusCase(string relativePath, string profileId, RawFormat format, string make, string model, string decodedHash, string renderHash)
    {
        RelativePath = relativePath;
        ProfileId = profileId;
        Format = format;
        Make = make;
        Model = model;
        DecodedHash = decodedHash;
        RenderHash = renderHash;
    }
}
