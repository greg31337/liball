using System.Security.Cryptography;
using argon2;

namespace argon2.Tests.Algorithm;

public class Argon2FinalizationTests
{
    [Test]
    public void FinalLaneBlocksAreXoredAndSerializedLittleEndian()
    {
        Argon2Memory memory = CreateInitializedMemory();
        ulong[] finalBlock = new ulong[Argon2Limits.WordsPerBlock];
        byte[] serialized = new byte[Argon2Limits.BlockSizeInBytes];
        Array.Fill(finalBlock, ulong.MaxValue);
        Array.Fill(serialized, byte.MaxValue);

        Argon2Finalization.CombineFinalLaneBlocks(memory, finalBlock);
        Argon2Finalization.SerializeFinalBlock(finalBlock, serialized);

        Assert.Multiple(() =>
        {
            Assert.That(finalBlock[0], Is.EqualTo(0x0000010000000255UL));
            Assert.That(finalBlock[1], Is.EqualTo(0x000001000000027FUL));
            Assert.That(finalBlock[127], Is.EqualTo(0x00000300000001B3UL));
            Assert.That(serialized[..8], Is.EqualTo(new byte[] { 0x55, 0x02, 0, 0, 0, 0x01, 0, 0 }));
            Assert.That(serialized[1016..], Is.EqualTo(new byte[] { 0xB3, 0x01, 0, 0, 0, 0x03, 0, 0 }));
            Assert.That(ToHex(SHA256.HashData(serialized)), Is.EqualTo(
                "6929ca7482e30958572b4103dd50595e8d5a7de2c5abcc66b671824a55d5a2d4"));
        });
    }

    [TestCase(4, "b9a715c5")]
    [TestCase(32, "a82ab40e3dfacdebb5b73a1fdd1b5650a1aaef9076372b64d3824025196546cb")]
    [TestCase(
        96,
        "a93f8d229048c6e1c5f18e7f35062f670d6f5162edd8e681178b5b82bb59592a" +
        "632ffec4b267795e0de3db94f0f27caf0017ae68a278d7a43bd9248d0bd4df53" +
        "f31647c837ad9ac076cd659c90d28fffaaac28c982bb09b085567b87d17b75a3")]
    public void TagGenerationMatchesIndependentFixturesAndClearsFinalBlock(
        int outputLength,
        string expectedTag)
    {
        Argon2Memory memory = CreateInitializedMemory();
        ulong[] originalLastBlock = memory.GetLaneBlock(0, 31L).ToArray();
        ulong[] finalBlock = new ulong[Argon2Limits.WordsPerBlock];
        byte[] destination = new byte[outputLength];
        Array.Fill(finalBlock, ulong.MaxValue);

        Argon2Finalization.ComputeTag(memory, finalBlock, destination);

        Assert.Multiple(() =>
        {
            Assert.That(ToHex(destination), Is.EqualTo(expectedTag));
            Assert.That(finalBlock, Is.All.Zero);
            Assert.That(memory.GetLaneBlock(0, 31L).ToArray(), Is.EqualTo(originalLastBlock));
        });
    }

    [Test]
    public void InvalidFinalizationInputsAreRejectedBeforeFinalBlockMutation()
    {
        Argon2Memory memory = CreateInitializedMemory();
        ulong[] finalBlock = new ulong[Argon2Limits.WordsPerBlock];
        Array.Fill(finalBlock, ulong.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Finalization.ComputeTag(memory, new ulong[127], new byte[32]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Finalization.ComputeTag(memory, finalBlock, new byte[3]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Finalization.CombineFinalLaneBlocks(memory, new ulong[129]));
            Assert.Throws<Argon2ParameterException>(
                () => Argon2Finalization.SerializeFinalBlock(new ulong[128], new byte[1023]));
        });

        Assert.That(finalBlock, Is.All.EqualTo(ulong.MaxValue));
    }

    [Test]
    public void SecureWordGuardClearsOnExceptionalUsingExit()
    {
        ulong[] words = new ulong[Argon2Limits.WordsPerBlock];

        Assert.Throws<InvalidOperationException>(() => FillWordsAndThrow(words));

        Assert.That(words, Is.All.Zero);
    }

    static void FillWordsAndThrow(ulong[] words)
    {
        using (SecureWordBuffer buffer = new SecureWordBuffer(words))
        {
            buffer.GetSpan().Fill(ulong.MaxValue);
            throw new InvalidOperationException("Expected test failure.");
        }
    }

    static Argon2Memory CreateInitializedMemory()
    {
        Argon2Parameters parameters = new Argon2Parameters();
        parameters.MemorySizeKiB = 71L;
        parameters.DegreeOfParallelism = 2;
        Argon2Memory memory = new Argon2Memory(parameters, 3);
        int laneIndex = 0;
        while (laneIndex < memory.GetLaneCount())
        {
            ulong laneValue = (ulong)(laneIndex + 1) * 0x100000001B3UL;
            ulong blockValue = laneValue;
            long blockIndex = 0L;
            while (blockIndex < memory.GetBlocksPerLane())
            {
                blockValue = ModularArithmetic.Add64(blockValue, 0x9E3779B97F4A7C15UL);
                ulong wordValue = blockValue;
                Span<ulong> block = memory.GetLaneBlock(laneIndex, blockIndex);
                int wordIndex = 0;
                while (wordIndex < block.Length)
                {
                    block[wordIndex] = wordValue;
                    wordValue = ModularArithmetic.Add64(wordValue, 0xD6E8FEB86659FD93UL);
                    wordIndex = wordIndex + 1;
                }

                blockIndex = blockIndex + 1L;
            }

            laneIndex = laneIndex + 1;
        }

        return memory;
    }

    static string ToHex(byte[] value)
    {
        return Convert.ToHexString(value).ToLowerInvariant();
    }
}
