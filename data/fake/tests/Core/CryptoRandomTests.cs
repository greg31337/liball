using fake;

namespace fake.Tests;

[TestFixture]
public class CryptoRandomTests
{
    [Test]
    public void DeterministicBytesHaveStableLittleEndianLayout()
    {
        byte[] expected = new byte[]
        {
            180, 242, 117, 203, 54, 95, 236, 153,
            42, 69, 86, 73, 120, 31, 110, 191
        };

        byte[] actual = Fake.Create(0).Random().Bytes(expected.Length);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void DeterministicBytesAndArrayFillProduceEqualResults()
    {
        FakeRandom allocated = Fake.Create(72).Random();
        FakeRandom filled = Fake.Create(72).Random();
        byte[] destination = new byte[37];

        filled.Fill(destination);

        Assert.That(destination, Is.EqualTo(allocated.Bytes(destination.Length)));
    }

    [Test]
    public void PartialDeterministicWordConsumesTheWholeWord()
    {
        FakeRandom random = Fake.Create(0).Random();

        random.Bytes(9);

        Assert.That(random.ULong(), Is.EqualTo(1900383378846508768UL));
    }

    [Test]
    public void SpanFillOnlyChangesTheRequestedRegion()
    {
        FakeRandom random = Fake.Create(0).Random();
        byte[] destination = Enumerable.Repeat((byte)255, 12).ToArray();
        byte[] expected = new byte[] { 180, 242, 117, 203, 54, 95, 236, 153 };

        random.FillFromDestination(destination.AsSpan(2, 8));

        Assert.That(destination[0], Is.EqualTo(255));
        Assert.That(destination[1], Is.EqualTo(255));
        Assert.That(destination[10], Is.EqualTo(255));
        Assert.That(destination[11], Is.EqualTo(255));
        Assert.That(destination[2..10], Is.EqualTo(expected));
    }

    [Test]
    public void ZeroLengthByteOperationsAreSupported()
    {
        FakeRandom random = Fake.Create().Random();
        byte[] destination = Array.Empty<byte>();

        random.Fill(destination);
        random.FillFromDestination(destination.AsSpan());

        Assert.That(random.Bytes(0), Is.Empty);
    }

    [Test]
    public void BytesRejectsNegativeCount()
    {
        FakeRandom random = Fake.Create().Random();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => random.Bytes(-1));

        Assert.That(exception.ParamName, Is.EqualTo("count"));
    }

    [Test]
    public void ByteOperationsEnforceConfiguredOutputLimit()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumOutputSize = 4;
        FakeRandom random = Fake.Create(options).Random();

        ArgumentOutOfRangeException bytesException = Assert.Throws<ArgumentOutOfRangeException>(() => random.Bytes(5));
        ArgumentOutOfRangeException arrayException = Assert.Throws<ArgumentOutOfRangeException>(() => random.Fill(new byte[5]));
        ArgumentOutOfRangeException spanException = Assert.Throws<ArgumentOutOfRangeException>(() => random.FillFromDestination(new byte[5].AsSpan()));

        Assert.That(bytesException.ParamName, Is.EqualTo("count"));
        Assert.That(arrayException.ParamName, Is.EqualTo("destination"));
        Assert.That(spanException.ParamName, Is.EqualTo("destination"));
    }

    [Test]
    public void ArrayFillRejectsNullDestination()
    {
        FakeRandom random = Fake.Create().Random();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => random.Fill((byte[])null));

        Assert.That(exception.ParamName, Is.EqualTo("destination"));
    }

    [Test]
    public void CryptographicBytesDoNotReplayAfterReset()
    {
        FakeRandom random = CreateCryptographicFake().Random();
        byte[] first = random.Bytes(64);

        random.Reset();
        byte[] second = random.Bytes(64);

        Assert.That(second, Is.Not.EqualTo(first));
        Assert.That(first, Has.Some.Not.EqualTo((byte)0));
        Assert.That(second, Has.Some.Not.EqualTo((byte)0));
    }

    [Test]
    public void CryptographicULongDoesNotReplayAfterReset()
    {
        FakeRandom random = CreateCryptographicFake().Random();
        ulong[] first = Take(random, 4);

        random.Reset();
        ulong[] second = Take(random, 4);

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void CryptographicSpanFillOnlyChangesRequestedRegion()
    {
        FakeRandom random = CreateCryptographicFake().Random();
        byte[] destination = Enumerable.Repeat((byte)165, 20).ToArray();

        random.FillFromDestination(destination.AsSpan(2, 16));

        Assert.That(destination[0], Is.EqualTo(165));
        Assert.That(destination[1], Is.EqualTo(165));
        Assert.That(destination[18], Is.EqualTo(165));
        Assert.That(destination[19], Is.EqualTo(165));
        Assert.That(destination[2..18], Has.Some.Not.EqualTo((byte)165));
    }

    [Test]
    public void CryptographicSnapshotIsRejected()
    {
        FakeRandom random = CreateCryptographicFake().Random();

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => random.Snapshot());

        Assert.That(exception.Message, Does.Contain("Snapshot"));
        Assert.That(exception.Message, Does.Contain("cryptographic random mode"));
    }

    [Test]
    public void CryptographicRestoreIsRejectedBeforeStateValidation()
    {
        FakeRandom random = CreateCryptographicFake().Random();

        NotSupportedException exception = Assert.Throws<NotSupportedException>(() => random.Restore(null));

        Assert.That(exception.Message, Does.Contain("Restore"));
        Assert.That(exception.Message, Does.Contain("cryptographic random mode"));
    }

    [Test]
    public void RootSnapshotAndRestoreAreRejectedInCryptographicMode()
    {
        Fake value = CreateCryptographicFake();
        FakeState deterministicState = Fake.Create(1).Snapshot();

        Assert.Throws<NotSupportedException>(() => value.Snapshot());
        Assert.Throws<NotSupportedException>(() => value.Restore(deterministicState));
    }

    [Test]
    public void CryptographicModePropagatesToForksAndProviders()
    {
        Fake value = CreateCryptographicFake();
        Fake child = value.Fork("child");
        FakeRandom randomChild = value.Random().Fork("random-child");

        Assert.That(child.OptionsSnapshot().RandomMode, Is.EqualTo(RandomMode.Cryptographic));
        Assert.Throws<NotSupportedException>(() => child.Random().Snapshot());
        Assert.Throws<NotSupportedException>(() => randomChild.Snapshot());
        Assert.Throws<NotSupportedException>(() => value.Person().RandomValue.Snapshot());
    }

    static Fake CreateCryptographicFake()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        return Fake.Create(options);
    }

    static ulong[] Take(FakeRandom random, int count)
    {
        ulong[] values = new ulong[count];
        int index = 0;
        while (index < count)
        {
            values[index] = random.ULong();
            index = index + 1;
        }
        return values;
    }
}
