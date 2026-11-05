namespace random.Tests;

public class RandomExceptionTests
{
    [Test]
    public void ArgumentExceptionCarriesDiagnosticContext()
    {
        Exception inner = new InvalidOperationException("inner");
        RandomArgumentException error = new RandomArgumentException("bound", "invalid bound", inner);

        Assert.Multiple(() =>
        {
            Assert.That(error.ArgumentName(), Is.EqualTo("bound"));
            Assert.That(error.Message, Is.EqualTo("invalid bound"));
            Assert.That(error.InnerException, Is.SameAs(inner));
        });
    }

    [Test]
    public void CapabilityExceptionCarriesAlgorithmAndOperation()
    {
        Exception inner = new InvalidOperationException("inner");
        RandomCapabilityException error = new RandomCapabilityException(RandomAlgorithm.SplitMix64V1, RandomCapability.Jump, "jump is unsupported", inner);

        Assert.Multiple(() =>
        {
            Assert.That(error.Algorithm(), Is.EqualTo(RandomAlgorithm.SplitMix64V1));
            Assert.That(error.Capability(), Is.EqualTo(RandomCapability.Jump));
            Assert.That(error.Message, Is.EqualTo("jump is unsupported"));
            Assert.That(error.InnerException, Is.SameAs(inner));
        });
    }

    [Test]
    public void DedicatedFailuresPreserveMessagesAndCauses()
    {
        Exception inner = new InvalidOperationException("inner");
        Exception[] errors =
        [
            new RandomStateException("state", inner),
            new RandomExhaustedException("exhausted", inner),
            new RandomReseedRequiredException("reseed", inner),
            new RandomSecurityException("security", inner)
        ];

        Assert.Multiple(() =>
        {
            Assert.That(errors[0].Message, Is.EqualTo("state"));
            Assert.That(errors[1].Message, Is.EqualTo("exhausted"));
            Assert.That(errors[2].Message, Is.EqualTo("reseed"));
            Assert.That(errors[3].Message, Is.EqualTo("security"));
            Assert.That(errors, Has.All.Property("InnerException").SameAs(inner));
        });
    }
}
