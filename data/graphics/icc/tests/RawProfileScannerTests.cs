using icc;

namespace icc.Tests;

public class RawProfileScannerTests
{
    [Test]
    public void ScannerReportsEveryValidProfileInCarrierOrder()
    {
        byte[] first = CreateProfile(132, 0);
        byte[] second = CreateProfile(152, 1);
        WriteEntry(second, 0, IccTagSignatures.Copyright, 144, 8);
        byte[] carrier = new byte[3 + first.Length + 5 + second.Length + 2];
        System.Array.Copy(first, 0, carrier, 3, first.Length);
        System.Array.Copy(second, 0, carrier, 3 + first.Length + 5, second.Length);

        IccByteLocation[] locations = RawProfileScanner.FindProfileLocations(carrier, new IccResourceLimits());

        Assert.Multiple(() =>
        {
            Assert.That(locations, Has.Length.EqualTo(2));
            Assert.That(locations[0].Offset, Is.EqualTo(3));
            Assert.That(locations[0].Length, Is.EqualTo(132));
            Assert.That(locations[1].Offset, Is.EqualTo(140));
            Assert.That(locations[1].Length, Is.EqualTo(152));
            Assert.That(locations[0], Is.Not.SameAs(locations[1]));
        });
    }

    [Test]
    public void ScannerIgnoresSignaturesWithoutABoundedValidEnvelope()
    {
        byte[] earlySignature = new byte[40];
        WriteUInt32(earlySignature, 2, IccConstants.ProfileFileSignature);
        byte[] truncated = CreateProfile(132, 0);
        WriteUInt32(truncated, 0, 200);
        byte[] shortTable = CreateProfile(132, 1);
        byte[] invalidPayload = CreateProfile(152, 1);
        WriteEntry(invalidPayload, 0, IccTagSignatures.Copyright, 1000, 8);
        byte[] carrier = Join(earlySignature, truncated, shortTable, invalidPayload);

        IccByteLocation[] locations = RawProfileScanner.FindProfileLocations(carrier, new IccResourceLimits());

        Assert.That(locations, Is.Empty);
    }

    [Test]
    public void CandidateEndingAtTheCarrierBoundaryIsAcceptedButTruncationIsNot()
    {
        byte[] complete = CreateProfile(152, 1);
        WriteEntry(complete, 0, IccTagSignatures.Copyright, 144, 8);
        byte[] truncated = complete[..151];

        IccByteLocation[] completeLocations = RawProfileScanner.FindProfileLocations(complete, new IccResourceLimits());
        IccByteLocation[] truncatedLocations = RawProfileScanner.FindProfileLocations(truncated, new IccResourceLimits());

        Assert.Multiple(() =>
        {
            Assert.That(completeLocations, Has.Length.EqualTo(1));
            Assert.That(completeLocations[0].Offset, Is.Zero);
            Assert.That(completeLocations[0].Length, Is.EqualTo(complete.Length));
            Assert.That(truncatedLocations, Is.Empty);
        });
    }

    [Test]
    public void ScannerAcceptsExactSharedPayloadsAndRejectsPartialOverlaps()
    {
        byte[] shared = CreateProfile(164, 2);
        WriteEntry(shared, 0, IccTagSignatures.Copyright, 156, 8);
        WriteEntry(shared, 1, IccTagSignatures.ProfileDescription, 156, 8);
        byte[] overlapping = CreateProfile(172, 2);
        WriteEntry(overlapping, 0, IccTagSignatures.Copyright, 156, 12);
        WriteEntry(overlapping, 1, IccTagSignatures.ProfileDescription, 160, 12);

        IccByteLocation[] sharedLocations = RawProfileScanner.FindProfileLocations(shared, new IccResourceLimits());
        IccByteLocation[] overlappingLocations = RawProfileScanner.FindProfileLocations(overlapping, new IccResourceLimits());

        Assert.Multiple(() =>
        {
            Assert.That(sharedLocations, Has.Length.EqualTo(1));
            Assert.That(sharedLocations[0].Length, Is.EqualTo(164));
            Assert.That(overlappingLocations, Is.Empty);
        });
    }

    [Test]
    public void ScannerRejectsDuplicateSignaturesGapsAndNonZeroPadding()
    {
        byte[] duplicate = CreateProfile(164, 2);
        WriteEntry(duplicate, 0, IccTagSignatures.Copyright, 156, 8);
        WriteEntry(duplicate, 1, IccTagSignatures.Copyright, 156, 8);
        byte[] gap = CreateProfile(156, 1);
        WriteEntry(gap, 0, IccTagSignatures.Copyright, 148, 8);
        byte[] padding = CreateProfile(156, 1);
        WriteEntry(padding, 0, IccTagSignatures.Copyright, 144, 9);
        padding[153] = 1;

        Assert.Multiple(() =>
        {
            Assert.That(RawProfileScanner.FindProfileLocations(duplicate, new IccResourceLimits()), Is.Empty);
            Assert.That(RawProfileScanner.FindProfileLocations(gap, new IccResourceLimits()), Is.Empty);
            Assert.That(RawProfileScanner.FindProfileLocations(padding, new IccResourceLimits()), Is.Empty);
        });
    }

    [Test]
    public void ScannerAppliesProfileTagAndPayloadResourceLimits()
    {
        byte[] profile = CreateProfile(152, 1);
        WriteEntry(profile, 0, IccTagSignatures.Copyright, 144, 8);
        IccResourceLimits profileLimit = new IccResourceLimits();
        profileLimit.MaximumProfileBytes = 151;
        IccResourceLimits tagCountLimit = new IccResourceLimits();
        tagCountLimit.MaximumTagCount = 0;
        IccResourceLimits tagSizeLimit = new IccResourceLimits();
        tagSizeLimit.MaximumTagBytes = 7;
        IccResourceLimits totalSizeLimit = new IccResourceLimits();
        totalSizeLimit.MaximumTotalTagBytes = 7;

        Assert.Multiple(() =>
        {
            Assert.That(RawProfileScanner.FindProfileLocations(profile, profileLimit), Is.Empty);
            Assert.That(RawProfileScanner.FindProfileLocations(profile, tagCountLimit), Is.Empty);
            Assert.That(RawProfileScanner.FindProfileLocations(profile, tagSizeLimit), Is.Empty);
            Assert.That(RawProfileScanner.FindProfileLocations(profile, totalSizeLimit), Is.Empty);
        });
    }

    [Test]
    public void ScannerRejectsInvalidLimitConfigurationsBeforeScanning()
    {
        IccResourceLimits smallProfileLimit = new IccResourceLimits();
        smallProfileLimit.MaximumProfileBytes = 131;
        IccResourceLimits negativeTagLimit = new IccResourceLimits();
        negativeTagLimit.MaximumTagCount = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileValidationException>(() => RawProfileScanner.FindProfileLocations(new byte[0], null!));
            Assert.Throws<IccProfileValidationException>(() => RawProfileScanner.FindProfileLocations(new byte[0], smallProfileLimit));
            Assert.Throws<IccProfileValidationException>(() => RawProfileScanner.FindProfileLocations(new byte[0], negativeTagLimit));
        });
    }

    private static byte[] CreateProfile(int declaredSize, uint tagCount)
    {
        byte[] profile = new byte[declaredSize];
        WriteUInt32(profile, 0, (uint)declaredSize);
        WriteUInt32(profile, 8, 0x04400000);
        WriteUInt32(profile, 12, IccProfileClassSignatures.Display);
        WriteUInt32(profile, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(profile, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt32(profile, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(profile, 128, tagCount);
        return profile;
    }

    private static void WriteEntry(byte[] destination, int entryIndex, uint signature, uint payloadOffset, uint payloadSize)
    {
        int offset = 132 + (entryIndex * 12);
        WriteUInt32(destination, offset, signature);
        WriteUInt32(destination, offset + 4, payloadOffset);
        WriteUInt32(destination, offset + 8, payloadSize);
    }

    private static byte[] Join(byte[] first, byte[] second, byte[] third, byte[] fourth)
    {
        byte[] joined = new byte[first.Length + second.Length + third.Length + fourth.Length];
        int offset = 0;
        System.Array.Copy(first, 0, joined, offset, first.Length);
        offset += first.Length;
        System.Array.Copy(second, 0, joined, offset, second.Length);
        offset += second.Length;
        System.Array.Copy(third, 0, joined, offset, third.Length);
        offset += third.Length;
        System.Array.Copy(fourth, 0, joined, offset, fourth.Length);
        return joined;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
