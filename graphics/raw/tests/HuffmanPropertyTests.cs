using raw;

namespace raw.Tests;

public class HuffmanPropertyTests
{
    [Test]
    public void EverySmallFourLevelCountDistributionIsClassifiedAndRoundTrips()
    {
        int validCount = 0;
        int invalidCount = 0;
        int caseIndex = 0;
        int count1 = 0;
        while (count1 <= 4)
        {
            int count2 = 0;
            while (count2 <= 4)
            {
                int count3 = 0;
                while (count3 <= 4)
                {
                    int count4 = 0;
                    while (count4 <= 4)
                    {
                        byte[] counts = new byte[16];
                        counts[0] = (byte)count1;
                        counts[1] = (byte)count2;
                        counts[2] = (byte)count3;
                        counts[3] = (byte)count4;
                        int symbolCount = count1 + count2 + count3 + count4;
                        bool complete;
                        bool oversubscribed = IsOversubscribed(counts, out complete);
                        byte[] symbols = CreateSymbols(symbolCount);

                        if (symbolCount == 0 || oversubscribed)
                        {
                            RawFormatException? exception = Assert.Throws<RawFormatException>(() => new HuffmanTable(counts, symbols, 1 + caseIndex % 4));
                            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData), $"Counts {count1},{count2},{count3},{count4}");
                            invalidCount++;
                        }
                        else
                        {
                            HuffmanTable table = new HuffmanTable(counts, symbols, 1 + caseIndex % 4);
                            Assert.That(table.IsComplete(), Is.EqualTo(complete), $"Counts {count1},{count2},{count3},{count4}");
                            RoundTripAllSymbols(table, symbols);
                            validCount++;
                        }
                        caseIndex++;
                        count4++;
                    }
                    count3++;
                }
                count2++;
            }
            count1++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(validCount, Is.GreaterThan(0));
            Assert.That(invalidCount, Is.GreaterThan(0));
            Assert.That(validCount + invalidCount, Is.EqualTo(625));
        });
    }

    [Test]
    public void EverySmallIncompleteTreeRejectsAnUnusedPrefix()
    {
        int tested = 0;
        int count1 = 0;
        while (count1 <= 3)
        {
            int count2 = 0;
            while (count2 <= 3)
            {
                int count3 = 0;
                while (count3 <= 3)
                {
                    int count4 = 0;
                    while (count4 <= 3)
                    {
                        byte[] counts = new byte[16];
                        counts[0] = (byte)count1;
                        counts[1] = (byte)count2;
                        counts[2] = (byte)count3;
                        counts[3] = (byte)count4;
                        int symbolCount = count1 + count2 + count3 + count4;
                        bool complete;
                        bool oversubscribed = IsOversubscribed(counts, out complete);
                        if (symbolCount != 0 && !oversubscribed && !complete)
                        {
                            HuffmanTable table = new HuffmanTable(counts, CreateSymbols(symbolCount), 4);
                            int codeLength = table.GetMaxCodeLength();
                            int invalidCode = FindInvalidCode(table, codeLength);
                            Assert.That(invalidCode, Is.GreaterThanOrEqualTo(0));
                            byte[] bytes = new byte[(codeLength + 7) / 8];
                            MsbBitWriter writer = new MsbBitWriter(bytes);
                            writer.WriteBits((uint)invalidCode, codeLength);
                            writer.Finish(0);
                            MsbBitReader reader = CreateReader(bytes);

                            RawFormatException? exception = Assert.Throws<RawFormatException>(() => table.DecodeSymbol(reader));

                            Assert.Multiple(() =>
                            {
                                Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
                                Assert.That(reader.GetBitPosition(), Is.EqualTo(codeLength));
                            });
                            tested++;
                        }
                        count4++;
                    }
                    count3++;
                }
                count2++;
            }
            count1++;
        }

        Assert.That(tested, Is.GreaterThan(0));
    }

    [Test]
    public void EveryCanonicalCodeLengthReportsByteTruncation()
    {
        byte[] counts = new byte[16];
        byte[] symbols = new byte[16];
        int index = 0;
        while (index < 16)
        {
            counts[index] = 1;
            symbols[index] = (byte)index;
            index++;
        }
        HuffmanTable table = new HuffmanTable(counts, symbols, 8);

        index = 0;
        while (index < symbols.Length)
        {
            int codeLength = table.GetEncodeCodeLength(symbols[index]);
            int completeByteCount = (codeLength + 7) / 8;
            byte[] complete = new byte[completeByteCount];
            MsbBitWriter writer = new MsbBitWriter(complete);
            table.EncodeSymbol(writer, symbols[index]);
            writer.Finish(0);
            byte[] truncated = complete.Take(completeByteCount - 1).ToArray();
            MsbBitReader reader = CreateReader(truncated);

            RawFormatException? exception = Assert.Throws<RawFormatException>(() => table.DecodeSymbol(reader));

            Assert.Multiple(() =>
            {
                Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput), $"Code length {codeLength}");
                Assert.That(reader.GetBitPosition(), Is.EqualTo(truncated.Length * 8), $"Code length {codeLength}");
            });
            index++;
        }
    }

    [Test]
    public void EverySingleBitMutationEitherChangesSymbolsOrReturnsCodecDiagnostic()
    {
        byte[] counts = new byte[16];
        counts[0] = 1;
        counts[1] = 1;
        counts[2] = 1;
        byte[] symbols = new byte[] { 1, 2, 3 };
        byte[] sequence = new byte[] { 1, 2, 3, 1, 3, 2 };
        HuffmanTable table = new HuffmanTable(counts, symbols, 3);
        int logicalBitCount = 0;
        int index = 0;
        while (index < sequence.Length)
        {
            logicalBitCount += table.GetEncodeCodeLength(sequence[index]);
            index++;
        }
        byte[] encoded = new byte[(logicalBitCount + 7) / 8];
        MsbBitWriter writer = new MsbBitWriter(encoded);
        index = 0;
        while (index < sequence.Length)
        {
            table.EncodeSymbol(writer, sequence[index]);
            index++;
        }
        writer.Finish(0);

        int diagnosticMutations = 0;
        int changedMutations = 0;
        int bitIndex = 0;
        while (bitIndex < logicalBitCount)
        {
            byte[] mutated = encoded.ToArray();
            mutated[bitIndex / 8] ^= (byte)(1 << (7 - bitIndex % 8));
            MsbBitReader reader = CreateReader(mutated);
            byte[] decoded = new byte[sequence.Length];
            bool failed = false;
            try
            {
                index = 0;
                while (index < decoded.Length)
                {
                    decoded[index] = table.DecodeSymbol(reader);
                    index++;
                }
            }
            catch (RawFormatException exception)
            {
                Assert.That(exception.Code == RawDiagnosticCode.InvalidCodecData || exception.Code == RawDiagnosticCode.TruncatedInput, Is.True);
                failed = true;
                diagnosticMutations++;
            }

            if (!failed)
            {
                Assert.That(decoded, Is.Not.EqualTo(sequence), $"Mutated bit {bitIndex}");
                changedMutations++;
            }
            bitIndex++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(diagnosticMutations, Is.GreaterThan(0));
            Assert.That(changedMutations, Is.GreaterThan(0));
            Assert.That(diagnosticMutations + changedMutations, Is.EqualTo(logicalBitCount));
        });
    }

    [Test]
    public void IncrementingAnyCountInACompleteTreeIsOversubscribed()
    {
        byte[] completeCounts = new byte[16];
        completeCounts[0] = 1;
        completeCounts[1] = 1;
        completeCounts[2] = 2;
        int lengthIndex = 0;
        while (lengthIndex < completeCounts.Length)
        {
            byte[] mutatedCounts = completeCounts.ToArray();
            mutatedCounts[lengthIndex]++;
            int symbolCount = mutatedCounts.Sum(value => value);
            byte[] symbols = CreateSymbols(symbolCount);

            RawFormatException? exception = Assert.Throws<RawFormatException>(() => new HuffmanTable(mutatedCounts, symbols, 8));

            Assert.That((exception ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData), $"Mutated length {lengthIndex + 1}");
            lengthIndex++;
        }
    }

    static void RoundTripAllSymbols(HuffmanTable table, byte[] symbols)
    {
        int bitCount = 0;
        int index = 0;
        while (index < symbols.Length)
        {
            bitCount += table.GetEncodeCodeLength(symbols[index]);
            index++;
        }
        byte[] encoded = new byte[(bitCount + 7) / 8];
        MsbBitWriter writer = new MsbBitWriter(encoded);
        index = 0;
        while (index < symbols.Length)
        {
            table.EncodeSymbol(writer, symbols[index]);
            index++;
        }
        writer.Finish(0);
        MsbBitReader reader = CreateReader(encoded);
        index = 0;
        while (index < symbols.Length)
        {
            Assert.That(table.DecodeSymbol(reader), Is.EqualTo(symbols[index]));
            index++;
        }
        reader.ValidatePaddingToEnd(0);
        Assert.That(reader.IsAtEnd(), Is.True);
    }

    static bool IsOversubscribed(byte[] counts, out bool complete)
    {
        int available = 1;
        int index = 0;
        while (index < counts.Length)
        {
            available = available * 2 - counts[index];
            if (available < 0)
            {
                complete = false;
                return true;
            }
            index++;
        }
        complete = available == 0;
        return false;
    }

    static int FindInvalidCode(HuffmanTable table, int codeLength)
    {
        int candidate = 0;
        int candidateCount = 1 << codeLength;
        while (candidate < candidateCount)
        {
            bool matches = false;
            int length = 1;
            while (length <= codeLength)
            {
                int prefix = candidate >> (codeLength - length);
                int offset = prefix - table.FirstCodes[length];
                if (offset >= 0 && offset < table.CodeLengthCounts[length])
                {
                    matches = true;
                    break;
                }
                length++;
            }
            if (!matches)
            {
                return candidate;
            }
            candidate++;
        }
        return -1;
    }

    static byte[] CreateSymbols(int count)
    {
        byte[] symbols = new byte[count];
        int index = 0;
        while (index < count)
        {
            symbols[index] = (byte)index;
            index++;
        }
        return symbols;
    }

    static MsbBitReader CreateReader(byte[] bytes)
    {
        BoundedInput input = new BoundedInput(new ReadOnlyMemory<byte>(bytes), 0, bytes.Length, new RawLimits());
        return new MsbBitReader(input);
    }
}
