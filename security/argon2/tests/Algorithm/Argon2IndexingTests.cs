using System.Buffers.Binary;
using System.Security.Cryptography;
using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2IndexingTests
{
    [Test]
    public void AddressingSchedulesMatchAllVariants()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.D, 0L, 0), Is.False);
            Assert.That(Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.D, 2L, 3), Is.False);
            Assert.That(Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.I, 0L, 0), Is.True);
            Assert.That(Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.I, 2L, 3), Is.True);
            Assert.That(Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.Id, 0L, 0), Is.True);
            Assert.That(Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.Id, 0L, 1), Is.True);
            Assert.That(Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.Id, 0L, 2), Is.False);
            Assert.That(Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.Id, 0L, 3), Is.False);
            Assert.That(Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.Id, 1L, 0), Is.False);
            Assert.That(Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.Id, 2L, 3), Is.False);
        });
    }

    [Test]
    public void DataDependentSchedulesUsePreviousBlockWithoutChangingAddressState()
    {
        Argon2Parameters parameters = CreateAddressParameters(Argon2Variant.D);
        parameters.MemorySizeKiB = 64L;
        ulong[] previousBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] inputBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] addressBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] temporaryBlock = new ulong[Argon2Limits.WordsPerBlock];
        previousBlock[0] = 0x0123456789ABCDEFUL;
        inputBlock[6] = 7UL;
        addressBlock[0] = 8UL;
        temporaryBlock[0] = 9UL;

        ulong pseudoRandom = Argon2Indexing.GetPseudoRandomValue(
            parameters,
            0L,
            1,
            0,
            2L,
            previousBlock,
            inputBlock,
            addressBlock,
            temporaryBlock);

        parameters.Variant = Argon2Variant.Id;
        ulong hybridPseudoRandom = Argon2Indexing.GetPseudoRandomValue(
            parameters,
            0L,
            1,
            2,
            0L,
            previousBlock,
            inputBlock,
            addressBlock,
            temporaryBlock);

        Assert.Multiple(() =>
        {
            Assert.That(pseudoRandom, Is.EqualTo(previousBlock[0]));
            Assert.That(hybridPseudoRandom, Is.EqualTo(previousBlock[0]));
            Assert.That(inputBlock[6], Is.EqualTo(7UL));
            Assert.That(addressBlock[0], Is.EqualTo(8UL));
            Assert.That(temporaryBlock[0], Is.EqualTo(9UL));
        });
    }

    [Test]
    public void FirstIndependentSegmentGeneratesAtIndexTwo()
    {
        Argon2Parameters parameters = CreateAddressParameters(Argon2Variant.I);
        parameters.MemorySizeKiB = 64L;
        ulong[] inputBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] addressBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] temporaryBlock = new ulong[Argon2Limits.WordsPerBlock];

        ulong first = Argon2Indexing.GetPseudoRandomValue(
            parameters,
            0L,
            1,
            0,
            2L,
            ReadOnlySpan<ulong>.Empty,
            inputBlock,
            addressBlock,
            temporaryBlock);
        ulong next = Argon2Indexing.GetPseudoRandomValue(
            parameters,
            0L,
            1,
            0,
            3L,
            ReadOnlySpan<ulong>.Empty,
            inputBlock,
            addressBlock,
            temporaryBlock);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(0xE5BFED443F556171UL));
            Assert.That(next, Is.EqualTo(0x051B122E45BA28EFUL));
            Assert.That(inputBlock[6], Is.EqualTo(1UL));
        });
    }

    [Test]
    public void IndependentScheduleAdvancesAtAddressBlockBoundary()
    {
        Argon2Parameters parameters = CreateAddressParameters(Argon2Variant.I);
        parameters.MemorySizeKiB = 4096L;
        ulong[] inputBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] addressBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] temporaryBlock = new ulong[Argon2Limits.WordsPerBlock];
        Argon2Indexing.GetPseudoRandomValue(
            parameters,
            0L,
            1,
            0,
            2L,
            ReadOnlySpan<ulong>.Empty,
            inputBlock,
            addressBlock,
            temporaryBlock);

        ulong pseudoRandom = Argon2Indexing.GetPseudoRandomValue(
            parameters,
            0L,
            1,
            0,
            128L,
            ReadOnlySpan<ulong>.Empty,
            inputBlock,
            addressBlock,
            temporaryBlock);

        Assert.Multiple(() =>
        {
            Assert.That(inputBlock[6], Is.EqualTo(2UL));
            Assert.That(pseudoRandom, Is.EqualTo(0x28357F1C0F65537EUL));
        });
    }

    [Test]
    public void Argon2idFirstHalfUsesIndependentAddresses()
    {
        Argon2Parameters parameters = CreateAddressParameters(Argon2Variant.Id);
        parameters.MemorySizeKiB = 64L;
        ulong[] inputBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] addressBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] temporaryBlock = new ulong[Argon2Limits.WordsPerBlock];

        ulong pseudoRandom = Argon2Indexing.GetPseudoRandomValue(
            parameters,
            0L,
            1,
            1,
            0L,
            ReadOnlySpan<ulong>.Empty,
            inputBlock,
            addressBlock,
            temporaryBlock);

        Assert.Multiple(() =>
        {
            Assert.That(pseudoRandom, Is.EqualTo(0x85BEBA2C450CB34EUL));
            Assert.That(inputBlock[5], Is.EqualTo(2UL));
            Assert.That(inputBlock[6], Is.EqualTo(1UL));
        });
    }

    [Test]
    public void InvalidAddressingScheduleInputsAreRejected()
    {
        Argon2Parameters parameters = CreateAddressParameters(Argon2Variant.I);
        parameters.MemorySizeKiB = 64L;
        ulong[] block = new ulong[Argon2Limits.WordsPerBlock];

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.UsesDataIndependentAddressing((Argon2Variant)99, 0L, 0));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.I, -1L, 0));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.UsesDataIndependentAddressing(Argon2Variant.I, 0L, 4));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.GetPseudoRandomValue(
                    parameters,
                    0L,
                    0,
                    0,
                    1L,
                    block,
                    block,
                    new ulong[Argon2Limits.WordsPerBlock],
                    new ulong[Argon2Limits.WordsPerBlock]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.GetPseudoRandomValue(
                    parameters,
                    0L,
                    0,
                    1,
                    4L,
                    block,
                    block,
                    new ulong[Argon2Limits.WordsPerBlock],
                    new ulong[Argon2Limits.WordsPerBlock]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.GetPseudoRandomValue(
                    parameters,
                    3L,
                    0,
                    1,
                    0L,
                    block,
                    block,
                    new ulong[Argon2Limits.WordsPerBlock],
                    new ulong[Argon2Limits.WordsPerBlock]));
        });
    }

    [Test]
    public void AddressInputContainsRfcWordsAndClearsUnusedWords()
    {
        Argon2Parameters parameters = CreateAddressParameters(Argon2Variant.I);
        parameters.MemorySizeKiB = 35L;
        ulong[] inputBlock = new ulong[Argon2Limits.WordsPerBlock];
        Array.Fill(inputBlock, ulong.MaxValue);

        Argon2Indexing.InitializeAddressInputBlock(parameters, 2L, 1, 3, inputBlock);

        Assert.Multiple(() =>
        {
            Assert.That(inputBlock[0], Is.EqualTo(2UL));
            Assert.That(inputBlock[1], Is.EqualTo(1UL));
            Assert.That(inputBlock[2], Is.EqualTo(3UL));
            Assert.That(inputBlock[3], Is.EqualTo(32UL));
            Assert.That(inputBlock[4], Is.EqualTo(3UL));
            Assert.That(inputBlock[5], Is.EqualTo(1UL));
            Assert.That(inputBlock[6], Is.Zero);
            Assert.That(inputBlock[7..], Is.All.Zero);
        });
    }

    [Test]
    public void Argon2idAddressInputUsesHybridVariantIdentifier()
    {
        Argon2Parameters parameters = CreateAddressParameters(Argon2Variant.Id);
        ulong[] inputBlock = new ulong[Argon2Limits.WordsPerBlock];

        Argon2Indexing.InitializeAddressInputBlock(parameters, 0L, 0, 1, inputBlock);

        Assert.That(inputBlock[5], Is.EqualTo(2UL));
    }

    [Test]
    public void ConsecutiveAddressBlocksMatchIndependentFixtures()
    {
        Argon2Parameters parameters = CreateAddressParameters(Argon2Variant.I);
        ulong[] inputBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] addressBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] temporaryBlock = new ulong[Argon2Limits.WordsPerBlock];
        Argon2Indexing.InitializeAddressInputBlock(parameters, 0L, 1, 0, inputBlock);

        Argon2Indexing.GenerateNextAddressBlock(inputBlock, addressBlock, temporaryBlock);

        Assert.Multiple(() =>
        {
            Assert.That(inputBlock[6], Is.EqualTo(1UL));
            Assert.That(GetBlockDigest(addressBlock), Is.EqualTo(
                "2691f65137f1b786660eeec1e44fbce50c335fc624a5bd5ddb2a839f8cb2fb96"));
            Assert.That(addressBlock[0], Is.EqualTo(0xB728C4187C327F44UL));
            Assert.That(addressBlock[127], Is.EqualTo(0xA9B63EE05E87F697UL));
            Assert.That(temporaryBlock, Is.All.Zero);
        });

        Argon2Indexing.GenerateNextAddressBlock(inputBlock, addressBlock, temporaryBlock);

        Assert.Multiple(() =>
        {
            Assert.That(inputBlock[6], Is.EqualTo(2UL));
            Assert.That(GetBlockDigest(addressBlock), Is.EqualTo(
                "4960e7ee71646b3403d54dbee2b9991a0d4826313732d17d823950e44663aa72"));
            Assert.That(addressBlock[0], Is.EqualTo(0xE685B07E13BD6D44UL));
            Assert.That(addressBlock[127], Is.EqualTo(0x04821F2034C4D725UL));
            Assert.That(temporaryBlock, Is.All.Zero);
        });
    }

    [Test]
    public void AddressCounterUsesUnsignedModularProgression()
    {
        Argon2Parameters parameters = CreateAddressParameters(Argon2Variant.I);
        ulong[] inputBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] addressBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] temporaryBlock = new ulong[Argon2Limits.WordsPerBlock];
        Argon2Indexing.InitializeAddressInputBlock(parameters, 0L, 0, 0, inputBlock);
        inputBlock[6] = ulong.MaxValue;

        Argon2Indexing.GenerateNextAddressBlock(inputBlock, addressBlock, temporaryBlock);

        Assert.That(inputBlock[6], Is.Zero);
    }

    [Test]
    public void InvalidAddressGenerationInputsAreRejectedBeforeMutation()
    {
        Argon2Parameters parameters = CreateAddressParameters(Argon2Variant.I);
        Argon2Parameters dependentParameters = CreateAddressParameters(Argon2Variant.D);
        ulong[] inputBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] addressBlock = new ulong[Argon2Limits.WordsPerBlock];
        ulong[] temporaryBlock = new ulong[Argon2Limits.WordsPerBlock];

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.InitializeAddressInputBlock(parameters, 3L, 0, 0, inputBlock));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.InitializeAddressInputBlock(parameters, 0L, 4, 0, inputBlock));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.InitializeAddressInputBlock(
                    dependentParameters,
                    0L,
                    0,
                    0,
                    inputBlock));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.InitializeAddressInputBlock(
                    parameters,
                    0L,
                    0,
                    0,
                    new ulong[127]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.GenerateNextAddressBlock(
                    new ulong[127],
                    addressBlock,
                    temporaryBlock));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.GenerateNextAddressBlock(
                    inputBlock,
                    inputBlock,
                    temporaryBlock));
        });

        Assert.That(inputBlock[6], Is.Zero);
    }

    [Test]
    public void J1AndJ2UseLowAndHighLittleEndianWords()
    {
        const ulong PseudoRandomValue = 0xFEDCBA9876543210UL;

        Assert.Multiple(() =>
        {
            Assert.That(Argon2Indexing.GetJ1(PseudoRandomValue), Is.EqualTo(0x76543210U));
            Assert.That(Argon2Indexing.GetJ2(PseudoRandomValue), Is.EqualTo(0xFEDCBA98U));
        });
    }

    [Test]
    public void ReferenceLaneUsesJ2ModuloExceptInFirstSlice()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Argon2Indexing.GetReferenceLane(0U, 4, 2, 0L, 0), Is.EqualTo(2));
            Assert.That(Argon2Indexing.GetReferenceLane(uint.MaxValue, 4, 2, 0L, 0), Is.EqualTo(2));
            Assert.That(Argon2Indexing.GetReferenceLane(0U, 4, 2, 0L, 1), Is.EqualTo(0));
            Assert.That(Argon2Indexing.GetReferenceLane(4U, 4, 2, 1L, 0), Is.EqualTo(0));
            Assert.That(Argon2Indexing.GetReferenceLane(uint.MaxValue, 4, 2, 2L, 3), Is.EqualTo(3));
        });
    }

    [Test]
    public void FirstPassCurrentLaneMappingMatchesIndependentTable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetReference(0U, 0L, 0, 2L, true), Is.EqualTo(0L));
            Assert.That(GetReference(uint.MaxValue, 0L, 0, 7L, true), Is.EqualTo(0L));
            Assert.That(GetReference(0x80000000U, 0L, 1, 0L, true), Is.EqualTo(5L));
            Assert.That(GetReference(0x12345678U, 0L, 1, 3L, true), Is.EqualTo(9L));
        });
    }

    [Test]
    public void FirstPassCrossLaneExclusionsMatchIndependentTable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetReference(0x80000000U, 0L, 1, 0L, false), Is.EqualTo(5L));
            Assert.That(GetReference(0x12345678U, 0L, 1, 3L, false), Is.EqualTo(7L));
            Assert.That(GetReference(uint.MaxValue, 0L, 3, 7L, false), Is.EqualTo(0L));
        });
    }

    [Test]
    public void LaterPassMappingsWrapFromTheNextSlice()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetReference(0U, 1L, 0, 0L, true), Is.EqualTo(30L));
            Assert.That(GetReference(uint.MaxValue, 1L, 0, 0L, false), Is.EqualTo(8L));
            Assert.That(GetReference(0x80000000U, 1L, 0, 3L, true), Is.EqualTo(27L));
            Assert.That(GetReference(0x12345678U, 1L, 2, 0L, false), Is.EqualTo(14L));
            Assert.That(GetReference(0xABCDEF01U, 1L, 2, 7L, true), Is.EqualTo(8L));
            Assert.That(GetReference(uint.MaxValue, 1L, 3, 0L, true), Is.EqualTo(0L));
            Assert.That(GetReference(0x01020304U, 2L, 3, 7L, false), Is.EqualTo(23L));
        });
    }

    [Test]
    public void MaximumLaneGeometryDoesNotOverflowMappingArithmetic()
    {
        const long LaneLength = 4294967292L;
        const long SegmentLength = 1073741823L;

        long reference = Argon2Indexing.GetReferenceBlockIndex(
            uint.MaxValue,
            1L,
            2,
            SegmentLength - 1L,
            false,
            LaneLength,
            SegmentLength);

        Assert.That(reference, Is.InRange(0L, LaneLength - 1L));
    }

    [Test]
    public void InvalidPositionsAndGeometryAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.GetReferenceLane(0U, 0, 0, 0L, 0));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.GetReferenceLane(0U, 4, 4, 0L, 0));
            Assert.Throws<Argon2ParameterException>(
                () => GetReference(0U, 0L, 0, 1L, true));
            Assert.Throws<Argon2ParameterException>(
                () => GetReference(0U, 0L, 0, 2L, false));
            Assert.Throws<Argon2ParameterException>(
                () => GetReference(0U, -1L, 0, 2L, true));
            Assert.Throws<Argon2ParameterException>(
                () => GetReference(0U, 0L, 4, 2L, true));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.GetReferenceBlockIndex(0U, 0L, 1, 8L, true, 32L, 8L));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.GetReferenceBlockIndex(0U, 0L, 1, 0L, true, 31L, 8L));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Indexing.GetReferenceBlockIndex(
                    0U,
                    0L,
                    1,
                    0L,
                    true,
                    long.MaxValue,
                    long.MaxValue));
        });
    }

    static long GetReference(
        uint j1,
        long passIndex,
        int sliceIndex,
        long indexInSegment,
        bool sameLane)
    {
        return Argon2Indexing.GetReferenceBlockIndex(
            j1,
            passIndex,
            sliceIndex,
            indexInSegment,
            sameLane,
            32L,
            8L);
    }

    static Argon2Parameters CreateAddressParameters(Argon2Variant variant)
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.Variant = variant;
        parameters.Version = Argon2Version.Version13;
        parameters.Iterations = 3L;
        parameters.MemorySizeKiB = 32L;
        parameters.DegreeOfParallelism = 4;
        return parameters;
    }

    static string GetBlockDigest(ulong[] block)
    {
        byte[] bytes = new byte[Argon2Limits.BlockSizeInBytes];
        int index = 0;
        while (index < block.Length)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(index * 8, 8), block[index]);
            index = index + 1;
        }

        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
