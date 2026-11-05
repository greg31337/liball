namespace cookies.Tests;

public class CookieExceptionTests
{
    [Test]
    public void ParseExceptionCanBeThrownWithItsSafeMessage()
    {
        CookieParseException exception = new CookieParseException("parse failed");

        Assert.Throws<CookieParseException>(() => throw exception);
        Assert.That(exception.Message, Is.EqualTo("parse failed"));
    }

    [Test]
    public void SerializationExceptionCanBeThrownWithItsSafeMessage()
    {
        CookieSerializationException exception = new CookieSerializationException("serialization failed");

        Assert.Throws<CookieSerializationException>(() => throw exception);
        Assert.That(exception.Message, Is.EqualTo("serialization failed"));
    }

    [Test]
    public void StoreExceptionCanBeThrownWithItsSafeMessage()
    {
        CookieStoreException exception = new CookieStoreException("store failed");

        Assert.Throws<CookieStoreException>(() => throw exception);
        Assert.That(exception.Message, Is.EqualTo("store failed"));
    }

    [Test]
    public void PolicyExceptionCanBeThrownWithItsSafeMessage()
    {
        CookiePolicyException exception = new CookiePolicyException("policy failed");

        Assert.Throws<CookiePolicyException>(() => throw exception);
        Assert.That(exception.Message, Is.EqualTo("policy failed"));
    }

    [Test]
    public void SnapshotExceptionCanBeThrownWithItsSafeMessage()
    {
        CookieSnapshotException exception = new CookieSnapshotException("snapshot failed");

        Assert.Throws<CookieSnapshotException>(() => throw exception);
        Assert.That(exception.Message, Is.EqualTo("snapshot failed"));
    }
}
