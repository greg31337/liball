namespace sdp.Tests.Api;

public class SdpSessionFactoryTests
{
    [Test]
    public void Factory_CreatesIndependentCoreValidSessionAndMediaModels()
    {
        ISdpSessionFactory factory = new SdpSessionFactory();
        SdpOrigin origin = ValidOrigin();
        SdpConnectionData connection = ValidConnection();
        SdpTiming timing = new SdpTiming
        {
            StartTime = "0",
            StopTime = "0"
        };
        timing.RepeatTimes.Add(new SdpRepeatTime
        {
            RepeatInterval = new SdpTypedTime { Value = 7, Unit = "d" },
            ActiveDuration = new SdpTypedTime { Value = 1, Unit = "h" },
            Offsets = { new SdpTypedTime { Value = 0 } }
        });
        List<string> formats = new List<string> { "111", "0" };

        SdpSessionDescription description = factory.CreateSession(origin, "Factory session", connection, timing);
        SdpMediaDescription media = factory.CreateMedia(SdpMediaTypes.Audio, 49170, SdpTransportProtocols.RtpAvp, formats);
        description.MediaDescriptions.Add(media);
        SdpValidationResult validation = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.Rfc8866 });

        origin.SessionId = "99";
        connection.Address = "198.51.100.2";
        timing.StartTime = "9999999999";
        timing.RepeatTimes[0].RepeatInterval.Value = 14;
        timing.RepeatTimes[0].Offsets[0].Value = 60;
        formats[0] = "112";
        formats.Add("8");

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(validation.Issues, Is.Empty);
            Assert.That(description.HasProtocolVersion, Is.True);
            Assert.That(description.ProtocolVersion, Is.Zero);
            Assert.That(description.HasSessionName, Is.True);
            Assert.That(description.SessionName, Is.EqualTo("Factory session"));
            Assert.That(description.Origin, Is.Not.SameAs(origin));
            Assert.That(description.Origin!.SessionId, Is.EqualTo("1"));
            Assert.That(description.Connection, Is.Not.SameAs(connection));
            Assert.That(description.Connection!.Address, Is.EqualTo("192.0.2.1"));
            Assert.That(description.Timings[0], Is.Not.SameAs(timing));
            Assert.That(description.Timings[0].StartTime, Is.EqualTo("0"));
            Assert.That(description.Timings[0].RepeatTimes[0], Is.Not.SameAs(timing.RepeatTimes[0]));
            Assert.That(description.Timings[0].RepeatTimes[0].RepeatInterval.Value, Is.EqualTo(7));
            Assert.That(description.Timings[0].RepeatTimes[0].Offsets[0].Value, Is.Zero);
            Assert.That(media.MediaType, Is.EqualTo(SdpMediaTypes.Audio));
            Assert.That(media.MediaKind, Is.EqualTo(SdpMediaKind.Audio));
            Assert.That(media.Port, Is.EqualTo(49170));
            Assert.That(media.HasPortCount, Is.False);
            Assert.That(media.PortCount, Is.EqualTo(1));
            Assert.That(media.Protocol, Is.EqualTo(SdpTransportProtocols.RtpAvp));
            Assert.That(media.Formats, Is.EqualTo(new[] { "111", "0" }));
        });
    }

    [Test]
    public void Factory_PreservesExplicitExtensionAndRejectedMediaChoices()
    {
        ISdpSessionFactory factory = new SdpSessionFactory();

        SdpMediaDescription media = factory.CreateMedia("x-sensor", 0, "X/SECURE/TRANSPORT", new List<string> { "x-format" });

        Assert.Multiple(() =>
        {
            Assert.That(media.MediaType, Is.EqualTo("x-sensor"));
            Assert.That(media.MediaKind, Is.EqualTo(SdpMediaKind.Unknown));
            Assert.That(media.Port, Is.Zero);
            Assert.That(media.Protocol, Is.EqualTo("X/SECURE/TRANSPORT"));
            Assert.That(media.Formats, Is.EqualTo(new[] { "x-format" }));
            Assert.That(media.Connections, Is.Empty);
            Assert.That(media.Attributes, Is.Empty);
            Assert.That(media.RawLines, Is.Empty);
        });
    }

    [Test]
    public void CreateSession_RejectsMissingOrInvalidRequiredChoices()
    {
        ISdpSessionFactory factory = new SdpSessionFactory();
        SdpOrigin origin = ValidOrigin();
        SdpConnectionData connection = ValidConnection();
        SdpTiming timing = ValidTiming();
        SdpOrigin invalidOrigin = ValidOrigin();
        invalidOrigin.SessionVersion = "not-a-number";
        SdpConnectionData invalidConnection = ValidConnection();
        invalidConnection.Address = "999.0.0.1";
        SdpTiming invalidTiming = ValidTiming();
        invalidTiming.StopTime = "9999999999";
        invalidTiming.StartTime = "9999999999";

        Assert.Multiple(() =>
        {
            Assert.That(() => factory.CreateSession(null!, "Session", connection, timing), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("origin"));
            Assert.That(() => factory.CreateSession(origin, null!, connection, timing), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("sessionName"));
            Assert.That(() => factory.CreateSession(origin, "Session", null!, timing), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("connection"));
            Assert.That(() => factory.CreateSession(origin, "Session", connection, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("timing"));
            Assert.That(() => factory.CreateSession(invalidOrigin, "Session", connection, timing), Throws.ArgumentException.With.Property("ParamName").EqualTo("origin"));
            Assert.That(() => factory.CreateSession(origin, string.Empty, connection, timing), Throws.ArgumentException.With.Property("ParamName").EqualTo("sessionName"));
            Assert.That(() => factory.CreateSession(origin, "bad\r\nname", connection, timing), Throws.ArgumentException.With.Property("ParamName").EqualTo("sessionName"));
            Assert.That(() => factory.CreateSession(origin, "Session", invalidConnection, timing), Throws.ArgumentException.With.Property("ParamName").EqualTo("connection"));
            Assert.That(() => factory.CreateSession(origin, "Session", connection, invalidTiming), Throws.ArgumentException.With.Property("ParamName").EqualTo("timing"));
        });
    }

    [Test]
    public void CreateMedia_RejectsMissingOrInvalidRequiredChoices()
    {
        ISdpSessionFactory factory = new SdpSessionFactory();
        List<string> formats = new List<string> { "0" };

        Assert.Multiple(() =>
        {
            Assert.That(() => factory.CreateMedia(null!, 9, SdpTransportProtocols.RtpAvp, formats), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mediaType"));
            Assert.That(() => factory.CreateMedia(SdpMediaTypes.Audio, 9, null!, formats), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("protocol"));
            Assert.That(() => factory.CreateMedia(SdpMediaTypes.Audio, 9, SdpTransportProtocols.RtpAvp, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("formats"));
            Assert.That(() => factory.CreateMedia(SdpMediaTypes.Audio, -1, SdpTransportProtocols.RtpAvp, formats), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("port"));
            Assert.That(() => factory.CreateMedia(SdpMediaTypes.Audio, 65536, SdpTransportProtocols.RtpAvp, formats), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("port"));
            Assert.That(() => factory.CreateMedia("bad media", 9, SdpTransportProtocols.RtpAvp, formats), Throws.ArgumentException.With.Property("ParamName").EqualTo("mediaType"));
            Assert.That(() => factory.CreateMedia(SdpMediaTypes.Audio, 9, "RTP//AVP", formats), Throws.ArgumentException.With.Property("ParamName").EqualTo("protocol"));
            Assert.That(() => factory.CreateMedia(SdpMediaTypes.Audio, 9, SdpTransportProtocols.RtpAvp, new List<string>()), Throws.ArgumentException.With.Property("ParamName").EqualTo("formats"));
            Assert.That(() => factory.CreateMedia(SdpMediaTypes.Audio, 9, SdpTransportProtocols.RtpAvp, new List<string> { null! }), Throws.ArgumentException.With.Property("ParamName").EqualTo("formats"));
            Assert.That(() => factory.CreateMedia(SdpMediaTypes.Audio, 9, SdpTransportProtocols.RtpAvp, new List<string> { "0", "0" }), Throws.ArgumentException.With.Property("ParamName").EqualTo("formats"));
        });
    }

    static SdpOrigin ValidOrigin()
    {
        return new SdpOrigin
        {
            Username = "-",
            SessionId = "1",
            SessionVersion = "1",
            NetworkType = SdpNetworkTypes.Internet,
            AddressType = SdpAddressTypes.Ip4,
            UnicastAddress = "192.0.2.1",
            AddressKind = SdpAddressKind.Ipv4
        };
    }

    static SdpConnectionData ValidConnection()
    {
        return new SdpConnectionData
        {
            NetworkType = SdpNetworkTypes.Internet,
            AddressType = SdpAddressTypes.Ip4,
            Address = "192.0.2.1",
            AddressKind = SdpAddressKind.Ipv4
        };
    }

    static SdpTiming ValidTiming()
    {
        return new SdpTiming { StartTime = "0", StopTime = "0" };
    }
}
