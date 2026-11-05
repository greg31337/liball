using icc;
using System.Security.Cryptography;

namespace icc.Tests;

public class ProfileIdCalculationTests
{
    [TestCase("v4.0-embedded-v5-display.icc", "C1107730F79047A152E2344929548E62")]
    [TestCase("v4.2-colorspace-srgb.icc", "34562ABF994CCD066D2C5721D0D68C5D")]
    [TestCase("v4.4-display-parametric.icc", "689322B8473DF7337564667BAF7261B5")]
    [TestCase("v5.0-display-srgb-d65.icc", "33174F15ABB4D791A2ACE1BF694996C1")]
    [TestCase("v5.0.1-colorspace-cat8-lab.icc", "EDEDC018031F0C39B75A3B7D05D7A55F")]
    public void ComputesLicensedCorpusProfileIds(string fileName, string expectedHex)
    {
        byte[] profile = File.ReadAllBytes(Path.Combine(CorpusDirectory(), "valid", fileName));

        IccProfileId result = new IccProfileIdCalculator().ComputeProfileIdBytes(profile);

        Assert.That(result.Bytes, Is.EqualTo(Convert.FromHexString(expectedHex)));
    }

    [Test]
    public void ByteCalculationMatchesKnownRfc1321PreparedDigestWithoutMutation()
    {
        IIccProfileIdCalculator calculator = new IccProfileIdCalculator();
        byte[] profile = CreateDigestVector(IccVersionKind.Version4);
        byte[] original = profile.ToArray();

        IccProfileId result = calculator.ComputeProfileIdBytes(profile);

        Assert.Multiple(() =>
        {
            Assert.That(result.Bytes, Is.EqualTo(Convert.FromHexString("0758D62B2AFC917CEC36BB1C6F1E8E14")));
            Assert.That(result.Bytes, Has.Length.EqualTo(IccConstants.ProfileIdSize));
            Assert.That(result.IsPresent, Is.True);
            Assert.That(profile, Is.EqualTo(original));
        });
    }

    [Test]
    public void DigestMatchesIndependentlyPreparedRfc1321Input()
    {
        byte[] profile = CreateDigestVector(IccVersionKind.Version5);
        byte[] prepared = profile.ToArray();
        Array.Clear(prepared, 44, 4);
        Array.Clear(prepared, 64, 4);
        Array.Clear(prepared, 84, 16);
        byte[] expected;
        using (MD5 reference = MD5.Create())
        {
            expected = reference.ComputeHash(prepared);
        }

        IccProfileId actual = new IccProfileIdCalculator().ComputeProfileIdBytes(profile);

        Assert.That(actual.Bytes, Is.EqualTo(expected));
    }

    [Test]
    public void RequiredZeroedFieldsDoNotInfluenceTheDigest()
    {
        IIccProfileIdCalculator calculator = new IccProfileIdCalculator();
        byte[] first = CreateDigestVector(IccVersionKind.Version5);
        byte[] zeroFieldChanges = first.ToArray();
        Fill(zeroFieldChanges, 44, 4, 0xa5);
        Fill(zeroFieldChanges, 64, 4, 0x5a);
        Fill(zeroFieldChanges, 84, 16, 0xcc);
        byte[] materialChange = first.ToArray();
        materialChange[40] = (byte)(materialChange[40] ^ 1);

        IccProfileId baseline = calculator.ComputeProfileIdBytes(first);
        IccProfileId ignoredFields = calculator.ComputeProfileIdBytes(zeroFieldChanges);
        IccProfileId changed = calculator.ComputeProfileIdBytes(materialChange);

        Assert.Multiple(() =>
        {
            Assert.That(ignoredFields.Bytes, Is.EqualTo(baseline.Bytes));
            Assert.That(changed.Bytes, Is.Not.EqualTo(baseline.Bytes));
        });
    }

    [Test]
    public void ModelCalculationUsesSerializedBytesAndLeavesTheProfileUntouched()
    {
        IIccProfileIdCalculator calculator = new IccProfileIdCalculator();
        IccProfile profile = ProfileCodec.ReadEnvelope(CreateMinimalProfile(), 0, 0, new IccReadOptions());
        int index = 0;
        while (index < profile.Header.ProfileId.Bytes.Length)
        {
            profile.Header.ProfileId.Bytes[index] = (byte)(index + 1);
            index = index + 1;
        }

        profile.Header.ProfileId.IsPresent = true;
        byte[] originalId = profile.Header.ProfileId.Bytes.ToArray();
        byte[] originalRawHeader = profile.Header.RawBytes.ToArray();
        IccWriteOptions options = new IccWriteOptions();
        byte[] encoded = new IccProfileWriter().WriteProfileBytes(profile, options);

        IccProfileId fromModel = calculator.ComputeProfileIdData(profile, options);
        IccProfileId fromBytes = calculator.ComputeProfileIdBytes(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(fromModel.Bytes, Is.EqualTo(fromBytes.Bytes));
            Assert.That(profile.Header.ProfileId.Bytes, Is.EqualTo(originalId));
            Assert.That(profile.Header.ProfileId.IsPresent, Is.True);
            Assert.That(profile.Header.RawBytes, Is.EqualTo(originalRawHeader));
            Assert.That(profile.Tags, Is.Empty);
        });
    }

    [Test]
    public void ModelCalculationDoesNotPopulateDerivedCalculatorFields()
    {
        byte[] source = CreateMinimalProfile();
        WriteUInt32(source, 8, 0x05000000);
        IccProfile profile = ProfileCodec.ReadEnvelope(source, 0, 0, new IccReadOptions());
        IccCalculatorInstruction input = new IccCalculatorInstruction
        {
            Signature = CalculatorOperationSignatures.Input,
            Operand = 0
        };
        IccCalculatorInstruction output = new IccCalculatorInstruction
        {
            Signature = CalculatorOperationSignatures.Output,
            Operand = 0
        };
        IccCalculatorProgram program = new IccCalculatorProgram
        {
            Instructions = new IccCalculatorInstruction[] { input, output }
        };
        IccProcessingElement element = new IccProcessingElement
        {
            Signature = IccProcessingElementSignatures.Calculator,
            Kind = IccProcessingElementKind.Calculator,
            InputChannels = 1,
            OutputChannels = 1,
            Calculator = program
        };
        IccTag tag = new IccTag();
        tag.Signature = IccTagSignatures.AToB0;
        tag.TableEntry.Signature = tag.Signature;
        tag.Data.Kind = IccTagDataKind.MultiProcessElements;
        tag.Data.MultiProcessElements = new IccMultiProcessElements
        {
            InputChannels = 1,
            OutputChannels = 1,
            Elements = new IccProcessingElement[] { element }
        };
        profile.Tags = new IccTag[] { tag };
        IccWriteOptions options = new IccWriteOptions();
        options.ComputeProfileId = false;

        IccProfileId id = new IccProfileIdCalculator().ComputeProfileIdData(profile, options);

        Assert.Multiple(() =>
        {
            Assert.That(id.Bytes, Has.Length.EqualTo(16));
            Assert.That(program.InstructionCount, Is.Zero);
            Assert.That(program.Bytecode, Is.Empty);
            Assert.That(program.RequiredStackDepth, Is.Zero);
            Assert.That(program.RequiredStorageValues, Is.Zero);
            Assert.That(program.IsExecutable, Is.False);
            Assert.That(input.IsKnown, Is.False);
            Assert.That(output.IsKnown, Is.False);
        });
    }

    [Test]
    public void CalculationRejectsIncompleteMismatchedAndUnsupportedProfiles()
    {
        IIccProfileIdCalculator calculator = new IccProfileIdCalculator();
        byte[] wrongSize = CreateDigestVector(IccVersionKind.Version4);
        WriteUInt32(wrongSize, 0, 131);
        byte[] wrongSignature = CreateDigestVector(IccVersionKind.Version4);
        WriteUInt32(wrongSignature, 36, 0);
        byte[] version2 = CreateDigestVector(IccVersionKind.Version2);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileFormatException>(() => calculator.ComputeProfileIdBytes(new byte[131]));
            Assert.Throws<IccProfileFormatException>(() => calculator.ComputeProfileIdBytes(wrongSize));
            Assert.Throws<IccProfileFormatException>(() => calculator.ComputeProfileIdBytes(wrongSignature));
            Assert.Throws<IccUnsupportedFeatureException>(() => calculator.ComputeProfileIdBytes(version2));
            Assert.Throws<IccProfileWriteException>(() => calculator.ComputeProfileIdData(null!, new IccWriteOptions()));
        });
    }

    [Test]
    public void VerificationReportsMatchMismatchAbsenceAndInconsistentState()
    {
        IIccProfileIdCalculator calculator = new IccProfileIdCalculator();
        IccProfile profile = ProfileCodec.ReadEnvelope(CreateMinimalProfile(), 0, 0, new IccReadOptions());
        IccWriteOptions options = new IccWriteOptions();
        options.ComputeProfileId = false;
        calculator.UpdateProfileId(profile, options);
        byte[] validId = profile.Header.ProfileId.Bytes.ToArray();

        IccOperationResult matching = calculator.VerifyProfileId(profile, options);
        profile.Header.ProfileId.Bytes[0] = (byte)(profile.Header.ProfileId.Bytes[0] ^ 1);
        IccOperationResult mismatch = calculator.VerifyProfileId(profile, options);
        System.Array.Clear(profile.Header.ProfileId.Bytes);
        profile.Header.ProfileId.IsPresent = false;
        IccOperationResult missing = calculator.VerifyProfileId(profile, options);
        profile.Header.ProfileId.Bytes[0] = 1;
        IccOperationResult inconsistent = calculator.VerifyProfileId(profile, options);
        IccOperationResult invalid = calculator.VerifyProfileId(null!, options);

        Assert.Multiple(() =>
        {
            Assert.That(matching.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(matching.Location.Offset, Is.EqualTo(84));
            Assert.That(matching.Location.Length, Is.EqualTo(16));
            Assert.That(validId, Has.Some.Not.Zero);
            Assert.That(mismatch.Status, Is.EqualTo(IccOperationStatus.Conflict));
            Assert.That(missing.Status, Is.EqualTo(IccOperationStatus.NotFound));
            Assert.That(inconsistent.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(invalid.Status, Is.EqualTo(IccOperationStatus.InvalidData));
        });
    }

    [Test]
    public void VerificationReportsUnsupportedVersionAndMalformedStoredId()
    {
        IIccProfileIdCalculator calculator = new IccProfileIdCalculator();
        byte[] version2Bytes = CreateMinimalProfile();
        WriteUInt32(version2Bytes, 8, 0x02100000);
        IccProfile version2 = ProfileCodec.ReadEnvelope(version2Bytes, 0, 0, new IccReadOptions());
        IccProfile malformed = ProfileCodec.ReadEnvelope(CreateMinimalProfile(), 0, 0, new IccReadOptions());
        malformed.Header.ProfileId.Bytes = new byte[15];
        IccProfile nullBytes = ProfileCodec.ReadEnvelope(CreateMinimalProfile(),
            0, 0, new IccReadOptions());
        nullBytes.Header.ProfileId.Bytes = null!;
        IccProfile zeroMarkedPresent = ProfileCodec.ReadEnvelope(CreateMinimalProfile(), 0, 0, new IccReadOptions());
        zeroMarkedPresent.Header.ProfileId.IsPresent = true;

        IccOperationResult unsupported = calculator.VerifyProfileId(version2, new IccWriteOptions());
        IccOperationResult invalidLength = calculator.VerifyProfileId(malformed, new IccWriteOptions());
        IccOperationResult missingBytes = calculator.VerifyProfileId(
            nullBytes, new IccWriteOptions());
        IccOperationResult inconsistent = calculator.VerifyProfileId(zeroMarkedPresent, new IccWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(invalidLength.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(missingBytes.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(inconsistent.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(unsupported.Location.Offset, Is.EqualTo(84));
            Assert.That(invalidLength.Location.Length, Is.EqualTo(16));
        });
    }

    [Test]
    public void ExplicitUpdateChangesOnlyTheModelProfileId()
    {
        IIccProfileIdCalculator calculator = new IccProfileIdCalculator();
        IccProfile profile = ProfileCodec.ReadEnvelope(CreateMinimalProfile(), 0, 0, new IccReadOptions());
        IccWriteOptions options = new IccWriteOptions();
        options.ComputeProfileId = false;
        byte[] rawHeader = profile.Header.RawBytes.ToArray();
        IccTag[] tags = profile.Tags;
        IccProfileId expected = calculator.ComputeProfileIdData(profile, options);

        calculator.UpdateProfileId(profile, options);

        Assert.Multiple(() =>
        {
            Assert.That(profile.Header.ProfileId.Bytes, Is.EqualTo(expected.Bytes));
            Assert.That(profile.Header.ProfileId.IsPresent, Is.True);
            Assert.That(profile.Header.RawBytes, Is.EqualTo(rawHeader));
            Assert.That(profile.Tags, Is.SameAs(tags));
            Assert.That(calculator.VerifyProfileId(profile, options).Status, Is.EqualTo(IccOperationStatus.Success));
        });
    }

    [Test]
    public void CanonicalWriterReplacesStoredIdsWithOneReproducibleDigest()
    {
        IIccProfileIdCalculator calculator = new IccProfileIdCalculator();
        IIccProfileWriter writer = new IccProfileWriter();
        IccProfile firstProfile = ProfileCodec.ReadEnvelope(CreateMinimalProfile(), 0, 0, new IccReadOptions());
        IccProfile secondProfile = ProfileCodec.ReadEnvelope(CreateMinimalProfile(), 0, 0, new IccReadOptions());
        Fill(firstProfile.Header.ProfileId.Bytes, 0, 16, 0x11);
        Fill(secondProfile.Header.ProfileId.Bytes, 0, 16, 0x77);
        firstProfile.Header.ProfileId.IsPresent = true;
        secondProfile.Header.ProfileId.IsPresent = true;
        byte[] firstModelId = firstProfile.Header.ProfileId.Bytes.ToArray();
        byte[] secondModelId = secondProfile.Header.ProfileId.Bytes.ToArray();
        IccWriteOptions options = new IccWriteOptions();

        byte[] first = writer.WriteProfileBytes(firstProfile, options);
        byte[] repeated = writer.WriteProfileBytes(firstProfile, options);
        byte[] second = writer.WriteProfileBytes(secondProfile, options);
        IccProfileId computed = calculator.ComputeProfileIdBytes(first);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(repeated));
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first[84..100], Is.EqualTo(computed.Bytes));
            Assert.That(first[84..100], Has.Some.Not.Zero);
            Assert.That(firstProfile.Header.ProfileId.Bytes, Is.EqualTo(firstModelId));
            Assert.That(secondProfile.Header.ProfileId.Bytes, Is.EqualTo(secondModelId));
        });
    }

    [Test]
    public void PreservationChangesTheStoredIdOnlyWhenComputationIsEnabled()
    {
        IIccProfileIdCalculator calculator = new IccProfileIdCalculator();
        IIccProfileWriter writer = new IccProfileWriter();
        byte[] original = CreateMinimalProfile();
        Fill(original, 84, 16, 0x5c);
        IccProfile profile = ProfileCodec.ReadEnvelope(original, 0, 0, new IccReadOptions());
        byte[] modelId = profile.Header.ProfileId.Bytes.ToArray();
        IccWriteOptions retain = new IccWriteOptions();
        retain.Mode = IccWriteMode.Preservation;
        retain.ComputeProfileId = false;
        IccWriteOptions recompute = new IccWriteOptions();
        recompute.Mode = IccWriteMode.Preservation;
        recompute.ComputeProfileId = true;

        byte[] retained = writer.WriteProfileBytes(profile, retain);
        byte[] updated = writer.WriteProfileBytes(profile, recompute);
        IccProfileId expected = calculator.ComputeProfileIdBytes(updated);

        Assert.Multiple(() =>
        {
            Assert.That(retained, Is.EqualTo(original));
            Assert.That(updated.Take(84), Is.EqualTo(original.Take(84)));
            Assert.That(updated.Skip(100), Is.EqualTo(original.Skip(100)));
            Assert.That(updated[84..100], Is.EqualTo(expected.Bytes));
            Assert.That(updated[84..100], Is.Not.EqualTo(original[84..100]));
            Assert.That(profile.Header.ProfileId.Bytes, Is.EqualTo(modelId));
        });
    }

    private static byte[] CreateDigestVector(IccVersionKind kind)
    {
        byte[] bytes = new byte[132];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)(((index * 37) + 11) & 255);
            index = index + 1;
        }

        WriteUInt32(bytes, 0, 132);
        WriteUInt32(bytes, 8, kind == IccVersionKind.Version2 ? 0x02100000u : kind == IccVersionKind.Version5 ? 0x05000000u : 0x04400000u);
        WriteUInt32(bytes, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(bytes, 128, 0);
        return bytes;
    }

    private static string CorpusDirectory()
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory, "..", "..", "..",
            "Corpus", "iccdev"));
    }

    private static byte[] CreateMinimalProfile()
    {
        byte[] bytes = new byte[132];
        WriteUInt32(bytes, 0, 132);
        WriteUInt32(bytes, 8, 0x04400000);
        WriteUInt32(bytes, 12, IccProfileClassSignatures.Display);
        WriteUInt32(bytes, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(bytes, 20, IccColorSpaceSignatures.Xyz);
        WriteUInt16(bytes, 24, 2026);
        WriteUInt16(bytes, 26, 9);
        WriteUInt16(bytes, 28, 12);
        WriteUInt16(bytes, 30, 12);
        WriteUInt32(bytes, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(bytes, 68, (uint)HeaderCodec.D50IlluminantX);
        WriteUInt32(bytes, 72, (uint)HeaderCodec.D50IlluminantY);
        WriteUInt32(bytes, 76, (uint)HeaderCodec.D50IlluminantZ);
        WriteUInt32(bytes, 128, 0);
        return bytes;
    }

    private static void Fill(byte[] destination, int offset, int count, byte value)
    {
        int end = offset + count;
        int index = offset;
        while (index < end)
        {
            destination[index] = value;
            index = index + 1;
        }
    }

    private static void WriteUInt16(byte[] destination, int offset, ushort value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
