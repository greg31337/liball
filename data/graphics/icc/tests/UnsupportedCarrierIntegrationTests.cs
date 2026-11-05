using icc;

namespace icc.Tests;

public class UnsupportedCarrierIntegrationTests
{
    [Test]
    public void DetectsEveryIntegrationOnlyCarrierAcrossInputForms()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[][] carriers = CreateIntegrationCarriers();

        Assert.Multiple(() =>
        {
            int index = 0;
            while (index < carriers.Length)
            {
                byte[] carrier = carriers[index];
                using MemoryStream stream = new MemoryStream(carrier);
                Assert.That(reader.DetectCarrierBytes(carrier),
                    Is.EqualTo(IccEmbeddedFormat.Unsupported));
                Assert.That(reader.DetectCarrierMemory(carrier),
                    Is.EqualTo(IccEmbeddedFormat.Unsupported));
                Assert.That(reader.DetectCarrierStream(stream),
                    Is.EqualTo(IccEmbeddedFormat.Unsupported));
                Assert.That(stream.Position, Is.Zero);
                index = index + 1;
            }

            Assert.That(reader.DetectCarrierBytes(new byte[]
            {
                0, 0, 0, 12, 0x4a, 0x58, 0x4c, 0x20,
                0x0d, 0x0a, 0x87, 0x0a
            }), Is.EqualTo(IccEmbeddedFormat.Unsupported));
            Assert.That(reader.DetectCarrierBytes(new byte[]
            {
                0xc5, 0xd0, 0xd3, 0xc6
            }), Is.EqualTo(IccEmbeddedFormat.Unsupported));
            Assert.That(reader.DetectCarrierBytes(
                System.Text.Encoding.Unicode.GetBytes("<svg/>")),
                Is.EqualTo(IccEmbeddedFormat.Unsupported));

            byte[] delayedOpenXps = new byte[8192];
            System.Array.Copy(Ascii("PK\u0003\u0004"), delayedOpenXps, 4);
            byte[] marker = Ascii("Documents/1/FixedDocument.fdoc");
            System.Array.Copy(marker, 0, delayedOpenXps, 5000,
                marker.Length);
            using MemoryStream delayedStream =
                new MemoryStream(delayedOpenXps);
            Assert.That(reader.DetectCarrierStream(delayedStream),
                Is.EqualTo(IccEmbeddedFormat.Unsupported));
            Assert.That(delayedStream.Position, Is.Zero);
        });
    }

    [Test]
    public void IntegrationCarriersReturnNamedUnsupportedResultsBeforeRawScanning()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();
        byte[][] carriers = CreateIntegrationCarriers();
        string[] names = new string[]
        {
            "PDF", "OpenXPS", "SVG", "CSS", "EPS", "JPEG XL"
        };
        byte[] profile = CreateProfile(2026);

        int index = 0;
        while (index < carriers.Length)
        {
            byte[] carrier = Combine(carriers[index], profile);
            IccEmbeddedProfileResult result = reader.EnumerateProfilesBytes(
                carrier, new IccEmbeddingOptions());
            IccOperationResult probe = reader.ProbeEmbeddedProfiles(carrier,
                new IccResourceLimits());
            IccEmbeddingException exception = Assert.Throws<IccEmbeddingException>(
                () => reader.ExtractProfilesBytes(carrier,
                    new IccReadOptions()))!;

            Assert.Multiple(() =>
            {
                Assert.That(result.Options.Format,
                    Is.EqualTo(IccEmbeddedFormat.Unsupported));
                Assert.That(result.Result.Status,
                    Is.EqualTo(IccOperationStatus.UnsupportedFeature));
                Assert.That(result.Result.Message, Does.Contain(names[index]));
                Assert.That(result.Result.Message,
                    Does.Contain("IccProfileReader.ReadProfileBytes"));
                Assert.That(result.Result.Location.Offset, Is.Zero);
                Assert.That(result.Result.Location.Length,
                    Is.EqualTo(carrier.Length));
                Assert.That(result.Profiles, Is.Empty);
                Assert.That(probe.Status,
                    Is.EqualTo(IccOperationStatus.UnsupportedFeature));
                Assert.That(probe.Message, Does.Contain(names[index]));
                Assert.That(exception.Message, Does.Contain(names[index]));
            });
            index = index + 1;
        }
    }

    [Test]
    public void ExternallyExtractedRawProfileBytesRemainAcceptedByCoreReaders()
    {
        byte[] profile = CreateProfile(2025);
        byte[] pdfPrefix = Ascii("%PDF-1.7\n% externally parsed stream\n");
        byte[] pdf = Combine(pdfPrefix, profile);
        byte[] externallyExtracted = pdf.AsSpan(pdfPrefix.Length).ToArray();
        IIccProfileReader profileReader = new IccProfileReader();
        IIccEmbeddedProfileReader embeddedReader =
            new IccEmbeddedProfileReader();

        IccProfile decoded = profileReader.ReadProfileBytes(
            externallyExtracted, new IccReadOptions());
        IccEmbeddedProfileResult raw = embeddedReader.EnumerateProfilesBytes(
            externallyExtracted, new IccEmbeddingOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.OriginalBytes, Is.EqualTo(profile));
            Assert.That(raw.Options.Format,
                Is.EqualTo(IccEmbeddedFormat.RawIcc));
            Assert.That(raw.Result.Status,
                Is.EqualTo(IccOperationStatus.Success));
            Assert.That(raw.Profiles, Has.Length.EqualTo(1));
            Assert.That(raw.Profiles[0].Profile.OriginalBytes,
                Is.EqualTo(profile));
        });
    }

    [Test]
    public void WriterExplainsTheIntegrationBoundaryForUnsupportedCarriers()
    {
        IIccEmbeddedProfileWriter writer = new IccEmbeddedProfileWriter();
        IccOperationResult probe = writer.ProbeEmbeddingOperation(
            IccEmbeddedFormat.Unsupported, IccEmbeddedOperation.Replace);
        IccEmbeddingException exception = Assert.Throws<IccEmbeddingException>(
            () => writer.InsertProfileBytes(new byte[0], new IccProfile(),
                IccEmbeddedFormat.Unsupported, new IccWriteOptions()))!;

        Assert.Multiple(() =>
        {
            Assert.That(probe.Status,
                Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(probe.Message, Does.Contain("format-specific parser"));
            Assert.That(probe.Message, Does.Contain("raw ICC profile bytes"));
            Assert.That(exception.Message,
                Does.Contain("format-specific parser"));
            Assert.That(exception.Message,
                Does.Contain("raw ICC profile bytes"));
        });
    }

    [Test]
    public void SimilarButUnrecognizedInputsRemainUnknown()
    {
        IIccEmbeddedProfileReader reader = new IccEmbeddedProfileReader();

        Assert.Multiple(() =>
        {
            Assert.That(reader.DetectCarrierBytes(
                Ascii("PK\u0003\u0004ordinary/archive.txt")),
                Is.EqualTo(IccEmbeddedFormat.Unknown));
            Assert.That(reader.DetectCarrierBytes(
                Ascii("<!doctype html><html><style>p { color: red; }</style><svg></svg></html>")),
                Is.EqualTo(IccEmbeddedFormat.Unknown));
            Assert.That(reader.DetectCarrierBytes(
                Ascii("%!PS-Adobe-3.0\nshowpage")),
                Is.EqualTo(IccEmbeddedFormat.Unknown));
        });
    }

    private static byte[][] CreateIntegrationCarriers()
    {
        return new byte[][]
        {
            Ascii("prefix\n%PDF-2.0\n"),
            Ascii("PK\u0003\u0004Documents/1/FixedDocumentSequence.fdseq"),
            Combine(new byte[] { 0xef, 0xbb, 0xbf },
                Ascii(" <?xml version=\"1.0\"?><!--x--><svg/>")),
            Ascii("/* profile */\n@color-profile --press { src: url(p.icc); }"),
            Ascii("%!PS-Adobe-3.0 EPSF-3.0\n%%BoundingBox: 0 0 1 1"),
            new byte[] { 0xff, 0x0a, 0x00, 0x00 }
        };
    }

    private static byte[] CreateProfile(int year)
    {
        IccProfileBuildOptions options = new IccProfileBuildOptions();
        options.ProfileClass = IccProfileClass.ColorSpace;
        options.DataColorSpaceSignature = IccColorSpaceSignatures.Xyz;
        options.CreationDateTime.Year = year;
        return new IccProfileBuilder().CreateXyzIdentityProfile(
            options).OriginalBytes;
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] result = new byte[first.Length + second.Length];
        System.Array.Copy(first, 0, result, 0, first.Length);
        System.Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }

    private static byte[] Ascii(string value)
    {
        return System.Text.Encoding.ASCII.GetBytes(value);
    }
}
