using icc;

namespace icc.Tests;

public class TagDecoderTests
{
    [Test]
    public void LazyDecodeReadsOnlyTheBoundedPayloadAndReturnsTheCachedValue()
    {
        byte[] source = CreateProfile(172, 2);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 156, 8);
        WriteEntry(source, 1, IccTagSignatures.ProfileDescription, 164, 8);
        WriteUInt32(source, 156, 0x70726976);
        WriteUInt32(source, 164, IccTypeSignatures.Xyz);
        IccReadOptions options = CreateBorrowedLazyOptions();

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 700L, options);
        WriteUInt32(source, 156, 0x70726977);
        IccTagData first = TagDecoder.GetDecodedData(profile.Tags[0]);
        IccOperationResult firstResult = profile.Tags[0].DecodeResult;
        WriteUInt32(source, 156, 0x70726978);
        IccTagData second = TagDecoder.GetDecodedData(profile.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.SameAs(second));
            Assert.That(first.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(first.RawData.TypeSignature, Is.EqualTo(0x70726977));
            Assert.That(first.RawData.Bytes, Has.Length.EqualTo(8));
            Assert.That(first.RawData.Bytes, Is.EqualTo(new byte[] { 0x70, 0x72, 0x69, 0x77, 0, 0, 0, 0 }));
            Assert.That(first.RawData.Bytes, Does.Not.Contain((byte)0x58));
            Assert.That(profile.Tags[0].IsDecodeAttempted, Is.True);
            Assert.That(profile.Tags[0].DecodeResult, Is.SameAs(firstResult));
            Assert.That(firstResult.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(firstResult.Location.Offset, Is.EqualTo(856));
            Assert.That(firstResult.Location.Length, Is.EqualTo(8));
        });
    }

    [Test]
    public void LazyParseLeavesPayloadDataUnallocatedUntilDecode()
    {
        byte[] source = CreateProfile(152, 1);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 144, 8);
        WriteUInt32(source, 144, 0x70726976);
        IccReadOptions options = CreateBorrowedLazyOptions();

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 0L, options);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Tags[0].IsDecodeAttempted, Is.False);
            Assert.That(profile.Tags[0].Data.Kind, Is.EqualTo(IccTagDataKind.Unknown));
            Assert.That(profile.Tags[0].Data.RawData.Bytes, Is.Empty);
            Assert.That(profile.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.Unspecified));
        });
    }

    [Test]
    public void CopiedInputOwnsBytesUsedByDeferredDecode()
    {
        byte[] source = CreateProfile(152, 1);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 144, 8);
        WriteUInt32(source, 144, 0x70726976);
        IccReadOptions options = new IccReadOptions();
        options.PreserveOriginalBytes = false;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 0L, options);
        WriteUInt32(source, 144, 0x70726977);
        IccTagData data = TagDecoder.GetDecodedData(profile.Tags[0]);

        Assert.That(data.RawData.TypeSignature, Is.EqualTo(0x70726976));
    }

    [Test]
    public void DecodeFailureIsRetainedAndReturnedStably()
    {
        byte[] source = CreateProfile(148, 1);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 144, 4);
        WriteUInt32(source, 144, 0x70726976);
        IccReadOptions options = CreateBorrowedLazyOptions();
        options.Mode = IccReadMode.Recovery;
        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 300L, options);

        IccTagData first = TagDecoder.GetDecodedData(profile.Tags[0]);
        IccOperationResult firstResult = profile.Tags[0].DecodeResult;
        WriteUInt32(source, 144, IccTypeSignatures.Data);
        IccTagData second = TagDecoder.GetDecodedData(profile.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.SameAs(second));
            Assert.That(first.Kind, Is.EqualTo(IccTagDataKind.Unknown));
            Assert.That(profile.Tags[0].DecodeResult, Is.SameAs(firstResult));
            Assert.That(firstResult.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(firstResult.Message, Does.Contain("4 bytes").And.Contain("at least 8 bytes"));
            Assert.That(firstResult.Location.Offset, Is.EqualTo(444));
            Assert.That(firstResult.Location.Length, Is.EqualTo(4));
        });
    }

    [Test]
    public void EagerModeDecodesTagsDuringEnvelopeRead()
    {
        byte[] source = CreateProfile(152, 1);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 144, 8);
        WriteUInt32(source, 144, 0x70726976);
        IccReadOptions options = new IccReadOptions();
        options.DecodeTagsLazily = false;
        options.PreserveOriginalBytes = false;

        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 0L, options);
        IccTagData repeated = TagDecoder.GetDecodedData(profile.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Tags[0].IsDecodeAttempted, Is.True);
            Assert.That(profile.Tags[0].Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(profile.Tags[0].Data.RawData.TypeSignature, Is.EqualTo(0x70726976));
            Assert.That(profile.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(repeated, Is.SameAs(profile.Tags[0].Data));
        });
    }

    [Test]
    public void SharedPayloadEntriesReuseOneStableDecodeOutcome()
    {
        byte[] source = CreateProfile(164, 2);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 156, 8);
        WriteEntry(source, 1, IccTagSignatures.ProfileDescription, 156, 8);
        WriteUInt32(source, 156, 0x70726976);
        IccReadOptions options = CreateBorrowedLazyOptions();
        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 0L, options);

        IccTagData first = TagDecoder.GetDecodedData(profile.Tags[0]);
        WriteUInt32(source, 156, 0x70726977);
        IccTagData second = TagDecoder.GetDecodedData(profile.Tags[1]);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(profile.Tags[1].DecodeResult, Is.SameAs(profile.Tags[0].DecodeResult));
            Assert.That(second.RawData.TypeSignature, Is.EqualTo(0x70726976));
            Assert.That(profile.Tags[1].IsDecodeAttempted, Is.True);
        });
    }

    [Test]
    public void CloningAnUndecodedTagRetainsAnIndependentLazySource()
    {
        byte[] source = CreateProfile(152, 1);
        WriteEntry(source, 0, IccTagSignatures.Copyright, 144, 8);
        WriteUInt32(source, 144, 0x70726976);
        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 0L, CreateBorrowedLazyOptions());

        IccProfile clone = ProfileClone.CloneProfile(profile);
        WriteUInt32(source, 144, 0x70726977);
        IccTagData sourceData = TagDecoder.GetDecodedData(profile.Tags[0]);
        IccTagData cloneData = TagDecoder.GetDecodedData(clone.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(sourceData.RawData.TypeSignature, Is.EqualTo(0x70726977));
            Assert.That(cloneData.RawData.TypeSignature, Is.EqualTo(0x70726976));
            Assert.That(cloneData, Is.Not.SameAs(sourceData));
            Assert.That(cloneData.RawData.Bytes, Is.Not.SameAs(sourceData.RawData.Bytes));
        });
    }

    private static IccReadOptions CreateBorrowedLazyOptions()
    {
        IccReadOptions options = new IccReadOptions();
        options.CopyInputBytes = false;
        options.DecodeTagsLazily = true;
        options.PreserveOriginalBytes = false;
        return options;
    }

    private static byte[] CreateProfile(int declaredSize, uint tagCount)
    {
        byte[] source = new byte[declaredSize];
        WriteUInt32(source, 0, (uint)declaredSize);
        WriteUInt32(source, 8, 0x04400000);
        WriteUInt32(source, 12, IccProfileClassSignatures.Display);
        WriteUInt32(source, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(source, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(source, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(source, 128, tagCount);
        return source;
    }

    private static void WriteEntry(byte[] destination, int entryIndex, uint signature, uint payloadOffset, uint payloadSize)
    {
        int offset = 132 + (entryIndex * 12);
        WriteUInt32(destination, offset, signature);
        WriteUInt32(destination, offset + 4, payloadOffset);
        WriteUInt32(destination, offset + 8, payloadSize);
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
