namespace random.Tests;

public class SystemCryptoEngineTests
{
    [Test]
    public void MetadataMarksEngineCryptographicAndStateless()
    {
        SystemCryptoEngine engine = new SystemCryptoEngine();
        RandomAlgorithmInfo info = engine.Info();

        Assert.Multiple(() =>
        {
            Assert.That(info.Algorithm(), Is.EqualTo(RandomAlgorithm.SystemCryptoV1));
            Assert.That(info.Name(), Is.EqualTo("System cryptographic random source"));
            Assert.That(info.Version(), Is.EqualTo(SystemCryptoEngine.AlgorithmVersion));
            Assert.That(info.WordBits(), Is.EqualTo(8));
            Assert.That(info.StateBytes(), Is.EqualTo(0));
            Assert.That(info.IsCryptographic(), Is.True);
            Assert.That(info.Capabilities(), Is.Empty);
        });
    }

    [Test]
    public void ChunkLengthCapsRequestsAtConfiguredBoundary()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SystemCryptoEngine.ChunkLength(0), Is.EqualTo(0));
            Assert.That(SystemCryptoEngine.ChunkLength(1), Is.EqualTo(1));
            Assert.That(SystemCryptoEngine.ChunkLength(SystemCryptoEngine.MaximumChunkBytes), Is.EqualTo(SystemCryptoEngine.MaximumChunkBytes));
            Assert.That(SystemCryptoEngine.ChunkLength(SystemCryptoEngine.MaximumChunkBytes + 1), Is.EqualTo(SystemCryptoEngine.MaximumChunkBytes));
            Assert.That(SystemCryptoEngine.ChunkLength(int.MaxValue), Is.EqualTo(SystemCryptoEngine.MaximumChunkBytes));
            RandomArgumentException error = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => SystemCryptoEngine.ChunkLength(-1)));
            Assert.That(error.ArgumentName(), Is.EqualTo("remainingBytes"));
        });
    }

    [Test]
    public void EmptyFillPerformsNoWork()
    {
        SystemCryptoEngine engine = new SystemCryptoEngine();

        Assert.DoesNotThrow(() => engine.FillBytes(Span<byte>.Empty));
    }

    [Test]
    public void FillWritesOnlyRequestedSpan()
    {
        SystemCryptoEngine engine = new SystemCryptoEngine();
        byte[] buffer = new byte[4096 + 34];
        Array.Fill(buffer, (byte)0xA5);

        engine.FillBytes(buffer.AsSpan(17, 4096));

        Assert.Multiple(() =>
        {
            Assert.That(buffer.AsSpan(0, 17).ToArray(), Is.All.EqualTo(0xA5));
            Assert.That(buffer.AsSpan(17, 4096).ToArray(), Has.Some.Not.EqualTo(0xA5));
            Assert.That(buffer.AsSpan(4113, 17).ToArray(), Is.All.EqualTo(0xA5));
        });
    }

    [Test]
    public void MultiChunkFillCoversCompleteDestination()
    {
        SystemCryptoEngine engine = new SystemCryptoEngine();
        int chunk = SystemCryptoEngine.MaximumChunkBytes;
        byte[] destination = new byte[chunk * 2 + 17];

        engine.FillBytes(destination);

        Assert.Multiple(() =>
        {
            Assert.That(destination.AsSpan(0, chunk).ToArray(), Has.Some.Not.EqualTo(0));
            Assert.That(destination.AsSpan(chunk, chunk).ToArray(), Has.Some.Not.EqualTo(0));
            Assert.That(destination.AsSpan(chunk * 2, 17).ToArray(), Has.Some.Not.EqualTo(0));
        });
    }

    [Test]
    public void SeparateFillsDoNotReplayDeterministicOutput()
    {
        SystemCryptoEngine engine = new SystemCryptoEngine();
        byte[] first = new byte[64];
        byte[] second = new byte[64];

        engine.FillBytes(first);
        engine.FillBytes(second);

        Assert.That(second, Is.Not.EqualTo(first));
    }
}
