using token;

namespace token.Tests;

public class RandomBytesTests
{
    [Test]
    public void GenerateReturnsRequestedNumberOfBytes()
    {
        byte[] bytes = RandomBytes.Generate(32);

        Assert.That(bytes, Has.Length.EqualTo(32));
    }

    [Test]
    public void GenerateReturnsEmptyArrayForZeroLength()
    {
        byte[] bytes = RandomBytes.Generate(0);

        Assert.That(bytes, Is.Empty);
    }

    [Test]
    public void GenerateRejectsNegativeLength()
    {
        Assert.That(
            () => RandomBytes.Generate(-1),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("length"));
    }

    [Test]
    public void GenerateProducesIndependentRandomValues()
    {
        byte[] first = RandomBytes.Generate(32);
        byte[] second = RandomBytes.Generate(32);

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void FillWritesRandomValuesWithinDestinationSpan()
    {
        byte[] bytes = new byte[34];
        bytes[0] = 91;
        bytes[33] = 92;

        RandomBytes.Fill(bytes.AsSpan(1, 32));

        Assert.Multiple(() =>
        {
            Assert.That(bytes[0], Is.EqualTo(91));
            Assert.That(bytes[33], Is.EqualTo(92));
            Assert.That(bytes.AsSpan(1, 32).ToArray(), Is.Not.EqualTo(new byte[32]));
        });
    }

    [Test]
    public void FillAcceptsEmptySpan()
    {
        Assert.That(() => RandomBytes.Fill(Span<byte>.Empty), Throws.Nothing);
    }
}
