using rtp;

namespace rtp.Tests.Api;

public class RtpExceptionTests
{
    [Test]
    public void RtpPacketFormatExceptionPreservesItsMessage()
    {
        RtpPacketFormatException exception =
            new RtpPacketFormatException("The RTP header is truncated.");

        RtpPacketFormatException? thrown = Assert.Throws<RtpPacketFormatException>(() =>
            throw exception);

        Assert.Multiple(() =>
        {
            Assert.That(thrown, Is.SameAs(exception));
            Assert.That(exception.Message, Is.EqualTo("The RTP header is truncated."));
            Assert.That(exception.InnerException, Is.Null);
        });
    }

    [Test]
    public void RtcpPacketFormatExceptionPreservesItsMessage()
    {
        RtcpPacketFormatException exception =
            new RtcpPacketFormatException("The RTCP length is invalid.");

        RtcpPacketFormatException? thrown = Assert.Throws<RtcpPacketFormatException>(() =>
            throw exception);

        Assert.Multiple(() =>
        {
            Assert.That(thrown, Is.SameAs(exception));
            Assert.That(exception.Message, Is.EqualTo("The RTCP length is invalid."));
            Assert.That(exception.InnerException, Is.Null);
        });
    }

    [Test]
    public void ConfigurationExceptionPreservesItsMessage()
    {
        RtpConfigurationException exception =
            new RtpConfigurationException("The payload type conflicts with RTCP multiplexing.");

        RtpConfigurationException? thrown = Assert.Throws<RtpConfigurationException>(() =>
            throw exception);

        Assert.Multiple(() =>
        {
            Assert.That(thrown, Is.SameAs(exception));
            Assert.That(exception.Message,
                Is.EqualTo("The payload type conflicts with RTCP multiplexing."));
            Assert.That(exception.InnerException, Is.Null);
        });
    }

    [Test]
    public void SessionExceptionPreservesItsMessage()
    {
        RtpSessionException exception =
            new RtpSessionException("The session has already ended.");

        RtpSessionException? thrown = Assert.Throws<RtpSessionException>(() =>
            throw exception);

        Assert.Multiple(() =>
        {
            Assert.That(thrown, Is.SameAs(exception));
            Assert.That(exception.Message, Is.EqualTo("The session has already ended."));
            Assert.That(exception.InnerException, Is.Null);
        });
    }

    [Test]
    public void BufferCapacityExceptionPreservesItsMessage()
    {
        RtpBufferCapacityException exception =
            new RtpBufferCapacityException("The output buffer is too small.");

        RtpBufferCapacityException? thrown = Assert.Throws<RtpBufferCapacityException>(() =>
            throw exception);

        Assert.Multiple(() =>
        {
            Assert.That(thrown, Is.SameAs(exception));
            Assert.That(exception.Message, Is.EqualTo("The output buffer is too small."));
            Assert.That(exception.InnerException, Is.Null);
        });
    }

    [Test]
    public void ExceptionTypesRemainDistinctFailureCategories()
    {
        Exception[] exceptions = new Exception[]
        {
            new RtpPacketFormatException("rtp"),
            new RtcpPacketFormatException("rtcp"),
            new RtpConfigurationException("configuration"),
            new RtpSessionException("session"),
            new RtpBufferCapacityException("capacity")
        };

        Assert.Multiple(() =>
        {
            Assert.That(exceptions[0], Is.TypeOf<RtpPacketFormatException>());
            Assert.That(exceptions[1], Is.TypeOf<RtcpPacketFormatException>());
            Assert.That(exceptions[2], Is.TypeOf<RtpConfigurationException>());
            Assert.That(exceptions[3], Is.TypeOf<RtpSessionException>());
            Assert.That(exceptions[4], Is.TypeOf<RtpBufferCapacityException>());
        });
    }
}
