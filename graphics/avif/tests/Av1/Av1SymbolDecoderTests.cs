namespace avif.Tests;

public class Av1SymbolDecoderTests
{
    [Test]
    public void BooleanLiteralAndNonSymmetricPathsDecodeKnownVectors()
    {
        Av1SymbolDecoder booleanDecoder = new Av1SymbolDecoder(
            new byte[] { 0x44, 0x00 }, 0, 2, false);
        int[] booleans = new int[4];
        int index = 0;
        while (index < booleans.Length)
        {
            booleans[index] = booleanDecoder.ReadBoolean();
            index++;
        }

        Av1ResultSnapshot booleanResult = Exit(booleanDecoder);
        Av1SymbolDecoder literalDecoder = new Av1SymbolDecoder(
            new byte[] { 0xb8, 0x00 }, 0, 2, false);
        ulong literal = literalDecoder.ReadLiteral(4);
        Av1ResultSnapshot literalResult = Exit(literalDecoder);
        Av1SymbolDecoder nsDecoder = new Av1SymbolDecoder(
            new byte[] { 0xf0, 0x00 }, 0, 2, false);
        uint nonSymmetric = nsDecoder.ReadNs(5);
        Av1ResultSnapshot nsResult = Exit(nsDecoder);

        Assert.Multiple(() =>
        {
            Assert.That(booleans, Is.EqualTo(new int[] { 0, 1, 0, 0 }));
            Assert.That(booleanResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(booleanResult.BitPosition, Is.EqualTo(16));
            Assert.That(literal, Is.EqualTo(11));
            Assert.That(literalResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(nonSymmetric, Is.EqualTo(4));
            Assert.That(nsResult.Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void AdaptiveSymbolsUpdateOnlyTheSelectedCdfRow()
    {
        ushort[] values = new ushort[]
        {
            8192, 16384, 24576, 32768, 0,
            4096, 11264, 19328, 32768, 0
        };
        Av1Cdf selected = new Av1Cdf(values, 5, 4);
        Av1SymbolDecoder decoder = new Av1SymbolDecoder(
            new byte[] { 0xff, 0x16, 0x40, 0x00, 0xff }, 1, 3, false);

        int[] symbols = new int[4];
        int index = 0;
        while (index < symbols.Length)
        {
            symbols[index] = decoder.ReadCdfSymbol(selected);
            index++;
        }

        AvifResultCode code = decoder.Exit();

        Assert.Multiple(() =>
        {
            Assert.That(symbols, Is.EqualTo(new int[] { 0, 3, 1, 2 }));
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(values[..5],
                Is.EqualTo(new ushort[] { 8192, 16384, 24576, 32768, 0 }));
            Assert.That(values[5..],
                Is.EqualTo(new ushort[] { 4539, 11843, 19969, 32768, 4 }));
        });
    }

    [Test]
    public void DisabledAdaptationLeavesTheCdfUnchanged()
    {
        ushort[] cdf = new ushort[] { 4096, 11264, 19328, 32768, 0 };
        ushort[] original = (ushort[])cdf.Clone();
        Av1SymbolDecoder decoder = new Av1SymbolDecoder(
            new byte[] { 0x70, 0x00 }, 0, 2, true);

        int symbol = decoder.ReadSymbol(cdf);
        AvifResultCode code = decoder.Exit();

        Assert.Multiple(() =>
        {
            Assert.That(symbol, Is.EqualTo(2));
            Assert.That(cdf, Is.EqualTo(original));
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void AdaptationRateChangesAndCountSaturatesAtThirtyTwo()
    {
        byte[] payload = new byte[]
        {
            0x23, 0x24, 0x6f, 0x0e, 0xaf, 0x06, 0x65, 0xce, 0xec, 0x00
        };
        int[] expected = new int[]
        {
            1, 0, 2, 2, 0, 2, 1, 0, 3, 0, 2,
            3, 2, 3, 2, 0, 3, 3, 3, 0, 0, 3,
            1, 2, 3, 1, 1, 3, 3, 1, 2, 0, 3
        };
        ushort[] cdf = new ushort[] { 4096, 11264, 19328, 32768, 0 };
        Av1SymbolDecoder decoder = new Av1SymbolDecoder(payload, 0, payload.Length, false);
        int[] actual = new int[expected.Length];
        int index = 0;
        while (index < actual.Length)
        {
            actual[index] = decoder.ReadSymbol(cdf);
            index++;
        }

        AvifResultCode code = decoder.Exit();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(cdf,
                Is.EqualTo(new ushort[] { 6382, 12648, 21129, 32768, 32 }));
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    [Test]
    public void ExitValidatesTrailingOnePaddingAndOverreadLimit()
    {
        Av1SymbolDecoder empty = new Av1SymbolDecoder(
            System.Array.Empty<byte>(), 0, 0, false);
        Av1SymbolDecoder missingOne = new Av1SymbolDecoder(
            new byte[] { 0x00, 0x00 }, 0, 2, false);
        Av1SymbolDecoder nonzeroPadding = new Av1SymbolDecoder(
            new byte[] { 0x80, 0x01 }, 0, 2, false);
        Av1SymbolDecoder overread = new Av1SymbolDecoder(
            new byte[] { 0xff }, 0, 1, false);
        int index = 0;
        while (index < 16)
        {
            overread.ReadBoolean();
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(empty.Exit(), Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(missingOne.Exit(), Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(nonzeroPadding.Exit(), Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(overread.Exit(), Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
        });
    }

    [Test]
    public void InvalidBoundsCdfsAndLifecycleCallsAreRejected()
    {
        Av1SymbolDecoder truncated = new Av1SymbolDecoder(
            new byte[1], 0, 2, false);
        Av1SymbolDecoder invalidCdf = new Av1SymbolDecoder(
            new byte[] { 0x80, 0x00 }, 0, 2, false);
        long initialPosition = invalidCdf.BitPosition;
        int symbol = invalidCdf.ReadSymbol(
            new ushort[] { 20000, 10000, 32768, 0 });
        int later = invalidCdf.ReadBoolean();
        Av1SymbolDecoder exited = new Av1SymbolDecoder(
            new byte[] { 0x80, 0x00 }, 0, 2, false);
        AvifResultCode firstExit = exited.Exit();
        int afterExit = exited.ReadBoolean();

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(symbol, Is.Zero);
            Assert.That(later, Is.Zero);
            Assert.That(invalidCdf.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(invalidCdf.BitPosition, Is.EqualTo(initialPosition));
            Assert.That(firstExit, Is.EqualTo(AvifResultCode.Success));
            Assert.That(afterExit, Is.Zero);
            Assert.That(exited.Code, Is.EqualTo(AvifResultCode.InvalidState));
        });
    }

    static Av1ResultSnapshot Exit(Av1SymbolDecoder decoder)
    {
        AvifResultCode code = decoder.Exit();
        return new Av1ResultSnapshot(code, decoder.BitPosition);
    }

    class Av1ResultSnapshot
    {
        internal AvifResultCode Code;
        internal long BitPosition;

        internal Av1ResultSnapshot(AvifResultCode code, long bitPosition)
        {
            Code = code;
            BitPosition = bitPosition;
        }
    }
}
