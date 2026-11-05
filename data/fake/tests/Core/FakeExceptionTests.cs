using fake;

namespace fake.Tests.Core;

[TestFixture]
public class FakeExceptionTests
{
    [Test]
    public void DefaultFactoryUsesStableMessage()
    {
        FakeException exception = FakeException.Create();

        Assert.That(exception.Message, Is.EqualTo("Fake data generation failed."));
    }

    [Test]
    public void MessageFactoryPreservesMessage()
    {
        FakeException exception = FakeException.Create("Generation stopped.");

        Assert.That(exception.Message, Is.EqualTo("Generation stopped."));
    }

    [Test]
    public void InnerExceptionFactoryPreservesCause()
    {
        InvalidOperationException cause = new InvalidOperationException("Cause");

        FakeException exception = FakeException.Create("Generation stopped.", cause);

        Assert.That(exception.Message, Is.EqualTo("Generation stopped."));
        Assert.That(exception.InnerException, Is.SameAs(cause));
    }
}
