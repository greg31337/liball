namespace barcode.Tests;

public class Pdf417CodewordTableTests
{
    [Test]
    public void ConstantsAndReferencePatternsMatchThePublishedTable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Pdf417CodewordTable.CodewordCount, Is.EqualTo(929));
            Assert.That(Pdf417CodewordTable.ClusterCount, Is.EqualTo(3));
            Assert.That(Pdf417CodewordTable.ModulesPerCodeword, Is.EqualTo(17));
            Assert.That(Pdf417CodewordTable.RunsPerCodeword, Is.EqualTo(8));
            Assert.That(Pdf417CodewordTable.GetPattern(0, 0), Is.EqualTo(0x1d5c0));
            Assert.That(Pdf417CodewordTable.GetPattern(0, 464), Is.EqualTo(0x13340));
            Assert.That(Pdf417CodewordTable.GetPattern(0, 928), Is.EqualTo(0x1bef4));
            Assert.That(Pdf417CodewordTable.GetPattern(1, 0), Is.EqualTo(0x1f560));
            Assert.That(Pdf417CodewordTable.GetPattern(1, 464), Is.EqualTo(0x1e2d8));
            Assert.That(Pdf417CodewordTable.GetPattern(1, 928), Is.EqualTo(0x13f26));
            Assert.That(Pdf417CodewordTable.GetPattern(2, 0), Is.EqualTo(0x1abe0));
            Assert.That(Pdf417CodewordTable.GetPattern(2, 464), Is.EqualTo(0x10b7c));
            Assert.That(Pdf417CodewordTable.GetPattern(2, 928), Is.EqualTo(0x1c7ea));
        });
    }

    [Test]
    public void EveryCodewordHasEightLegalRunsAndTheExpectedClusterNumber()
    {
        global::System.Collections.Generic.HashSet<int> allPatterns = new global::System.Collections.Generic.HashSet<int>();
        int clusterIndex = 0;
        while (clusterIndex < Pdf417CodewordTable.ClusterCount)
        {
            int codeword = 0;
            while (codeword < Pdf417CodewordTable.CodewordCount)
            {
                int pattern = Pdf417CodewordTable.GetPattern(clusterIndex, codeword);
                int[] widths = Pdf417CodewordTable.GetRunWidths(pattern);
                Assert.That(widths, Has.Length.EqualTo(8));
                Assert.That(widths.Sum(), Is.EqualTo(17));
                Assert.That(widths, Has.All.InRange(1, 6));
                Assert.That(Pdf417CodewordTable.GetClusterNumber(pattern), Is.EqualTo(clusterIndex * 3));
                Assert.That(allPatterns.Add(pattern), Is.True, $"duplicate pattern {pattern:x} at cluster {clusterIndex}, codeword {codeword}");
                codeword++;
            }

            clusterIndex++;
        }

        Assert.That(allPatterns, Has.Count.EqualTo(2787));
    }

    [Test]
    public void CompleteClusterDigestsMatchAnIndependentReferenceTable()
    {
        string[] expectedDigests = new string[]
        {
            "9a32ba3c1823d6a97432c72983e93e5f05db6afc456cfdf498acc39aa7e8b12e",
            "48a7cd30488d2dc22d3e7d1f2b51361a62f498500d7acdb79fc6490a750ccb12",
            "6121158ff3333f33c3b5bfc497897edb40ea820e7d2433fa1306b9b8dcc79107"
        };
        int clusterIndex = 0;
        while (clusterIndex < Pdf417CodewordTable.ClusterCount)
        {
            int[] patterns = Pdf417CodewordTable.CopyClusterPatterns(clusterIndex);
            byte[] bytes = ConvertPatternsToBigEndianBytes(patterns);
            string digest = global::System.Convert.ToHexString(
                global::System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
            Assert.That(digest, Is.EqualTo(expectedDigests[clusterIndex]));
            clusterIndex++;
        }
    }

    [Test]
    public void ForwardAndInverseLookupsRoundTripEveryCodewordAndCopiesAreOwned()
    {
        int clusterIndex = 0;
        while (clusterIndex < Pdf417CodewordTable.ClusterCount)
        {
            int[] copy = Pdf417CodewordTable.CopyClusterPatterns(clusterIndex);
            int codeword = 0;
            while (codeword < copy.Length)
            {
                Assert.That(
                    Pdf417CodewordTable.FindCodeword(clusterIndex, copy[codeword]),
                    Is.EqualTo(codeword));
                codeword++;
            }

            int expected = Pdf417CodewordTable.GetPattern(clusterIndex, 0);
            copy[0] = 0;
            Assert.That(Pdf417CodewordTable.GetPattern(clusterIndex, 0), Is.EqualTo(expected));
            clusterIndex++;
        }

        Assert.That(Pdf417CodewordTable.FindCodeword(0, 0x10000), Is.EqualTo(-1));
    }

    [Test]
    public void StartAndStopPatternsMatchTheirExactModuleSequences()
    {
        int[] startRuns = GetRunWidths(
            Pdf417CodewordTable.StartPatternBits,
            Pdf417CodewordTable.StartPatternModuleCount);
        int[] stopRuns = GetRunWidths(
            Pdf417CodewordTable.StopPatternBits,
            Pdf417CodewordTable.StopPatternModuleCount);

        Assert.Multiple(() =>
        {
            Assert.That(Pdf417CodewordTable.StartPatternBits, Is.EqualTo(0x1fea8));
            Assert.That(Pdf417CodewordTable.StartPatternModuleCount, Is.EqualTo(17));
            Assert.That(startRuns, Is.EqualTo(new int[] { 8, 1, 1, 1, 1, 1, 1, 3 }));
            Assert.That(Pdf417CodewordTable.StopPatternBits, Is.EqualTo(0x3fa29));
            Assert.That(Pdf417CodewordTable.StopPatternModuleCount, Is.EqualTo(18));
            Assert.That(stopRuns, Is.EqualTo(new int[] { 7, 1, 1, 3, 1, 1, 1, 2, 1 }));
        });
    }

    [Test]
    public void InvalidClusterCodewordAndPatternInputsAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Pdf417CodewordTable.GetPattern(-1, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417CodewordTable.GetPattern(3, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417CodewordTable.GetPattern(0, -1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417CodewordTable.GetPattern(0, 929), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417CodewordTable.CopyClusterPatterns(3), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417CodewordTable.FindCodeword(-1, 0), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417CodewordTable.GetRunWidths(0xffff), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417CodewordTable.GetRunWidths(0x1ffff), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Pdf417CodewordTable.GetRunWidths(0x10000), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    static byte[] ConvertPatternsToBigEndianBytes(int[] patterns)
    {
        byte[] bytes = new byte[patterns.Length * 4];
        int index = 0;
        while (index < patterns.Length)
        {
            int value = patterns[index];
            int offset = index * 4;
            bytes[offset] = (byte)(value >> 24);
            bytes[offset + 1] = (byte)(value >> 16);
            bytes[offset + 2] = (byte)(value >> 8);
            bytes[offset + 3] = (byte)value;
            index++;
        }

        return bytes;
    }

    static int[] GetRunWidths(int pattern, int moduleCount)
    {
        global::System.Collections.Generic.List<int> widths = new global::System.Collections.Generic.List<int>();
        int current = (pattern >> (moduleCount - 1)) & 1;
        int width = 0;
        int bitIndex = moduleCount - 1;
        while (bitIndex >= 0)
        {
            int bit = (pattern >> bitIndex) & 1;
            if (bit == current)
            {
                width++;
            }
            else
            {
                widths.Add(width);
                current = bit;
                width = 1;
            }

            bitIndex--;
        }

        widths.Add(width);
        return widths.ToArray();
    }
}
