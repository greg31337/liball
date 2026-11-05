using System;
using fake;

namespace fake.Tests;

[TestFixture]
public class RandomSourceInterfaceTests
{
    [Test]
    public void UuidGenerationCanBeVerifiedWithExactScriptedEntropy()
    {
        ScriptedRandomSource random = new ScriptedRandomSource(0, byte.MaxValue);

        byte[] value = UuidGenerator.GenerateVersion4Bytes(random, 16);

        int index = 0;
        while (index < value.Length)
        {
            byte expected = byte.MaxValue;
            if (index == 6)
                expected = 0x4f;
            if (index == 8)
                expected = 0xbf;
            Assert.That(value[index], Is.EqualTo(expected), $"byte index {index}");
            index++;
        }
    }

    [Test]
    public void DecimalIdentifierGenerationCanBeVerifiedWithoutASeedSearch()
    {
        ScriptedRandomSource random = new ScriptedRandomSource(0, 0);

        string imei = ImeiGenerator.GenerateImei(random, 20, 20);

        Assert.That(imei, Is.EqualTo("000000000000000"));
        Assert.That(ImeiGenerator.ValidateImei(imei.AsSpan(), 20), Is.True);
    }
}

internal class ScriptedRandomSource : IRandomSource
{
    internal int IntegerValue;
    internal byte ByteValue;

    internal ScriptedRandomSource(int integerValue, byte byteValue)
    {
        IntegerValue = integerValue;
        ByteValue = byteValue;
    }

    public int IntFromMaximumExclusive(int maximumExclusive)
    {
        if (IntegerValue < 0 || IntegerValue >= maximumExclusive)
            throw new InvalidOperationException("The scripted integer is outside the requested range.");
        return IntegerValue;
    }

    public void FillFromDestination(Span<byte> destination)
    {
        destination.Fill(ByteValue);
    }
}
