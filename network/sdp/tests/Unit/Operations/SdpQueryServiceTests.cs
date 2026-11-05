namespace sdp.Tests.Unit.Operations;

public class SdpQueryServiceTests
{
    [Test]
    public void MediaLookups_ReturnStoredMediaOrExplicitNotFoundResults()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription audio = Media("audio", 5000, "audio");
        SdpMediaDescription video = Media("video", 6000, "Video");
        video.Attributes.Insert(0, ValueAttribute(SdpAttributeNames.MediaId, "bad id"));
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(video);
        ISdpQueryService service = new SdpQueryService();

        Assert.Multiple(() =>
        {
            Assert.That(service.FindMediaByIndex(description, 0), Is.SameAs(audio));
            Assert.That(service.FindMediaByIndex(description, -1), Is.Null);
            Assert.That(service.FindMediaByIndex(description, 2), Is.Null);
            Assert.That(service.FindMediaByMid(description, "Video"), Is.SameAs(video));
            Assert.That(service.FindMediaByMid(description, "video"), Is.Null);
            Assert.That(service.FindMediaIndexByMid(description, "audio"), Is.Zero);
            Assert.That(service.FindMediaIndexByMid(description, "missing"), Is.EqualTo(-1));
            Assert.That(service.ResolveEffectiveMediaByMid(description, "missing"), Is.Null);
        });
    }

    [Test]
    public void EffectiveScopeQueries_ApplyOverridesDefaultsAndIndependentCopies()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Connection = Connection("192.0.2.1");
        description.Bandwidths.Add(Bandwidth("CT", 1000));
        description.Bandwidths.Add(Bandwidth("AS", 64));
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.SendOnly));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Language, "en"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Language, "fr"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.SessionDescriptionLanguage, "en-US"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "session"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "abcdefghijklmnopqrstuv"));
        description.Attributes.Add(FingerprintAttribute("sha-256", "11", 32));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Setup, "actpass"));
        description.Attributes.Add(ExtMap("1 urn:example:session"));

        SdpMediaDescription inherited = Media("audio", 5000, "audio");
        inherited.Attributes.Add(ValueAttribute(SdpAttributeNames.Language, "not_a_tag"));
        SdpMediaDescription overridden = Media("video", 6000, "video");
        overridden.Connections.Add(Connection("198.51.100.2"));
        overridden.Bandwidths.Add(Bandwidth("AS", 128));
        overridden.Bandwidths.Add(Bandwidth("RR", 32));
        overridden.Attributes.Add(PropertyAttribute(SdpAttributeNames.ReceiveOnly));
        overridden.Attributes.Add(ValueAttribute(SdpAttributeNames.Language, "de"));
        overridden.Attributes.Add(ValueAttribute(SdpAttributeNames.SessionDescriptionLanguage, "fr-CA"));
        overridden.Attributes.Add(ExtMap("2 urn:example:media"));
        description.MediaDescriptions.Add(inherited);
        description.MediaDescriptions.Add(overridden);
        ISdpQueryService service = new SdpQueryService();

        SdpConnectionData? inheritedConnection = service.ResolveEffectiveConnection(description, 0);
        SdpConnectionData? overriddenConnection = service.ResolveEffectiveConnection(description, 1);
        List<SdpBandwidth> bandwidths = service.ResolveEffectiveBandwidths(description, 1);
        List<string> inheritedLanguages = service.ResolveEffectiveLanguages(description, 0);
        List<string> overriddenLanguages = service.ResolveEffectiveLanguages(description, 1);
        SdpIceDescription ice = service.ResolveEffectiveIce(description, 1);
        SdpDtlsDescription dtls = service.ResolveEffectiveDtls(description, 1);
        List<SdpExtMapAttribute> inheritedExtMaps = service.ResolveEffectiveExtMaps(description, 0);
        List<SdpExtMapAttribute> overriddenExtMaps = service.ResolveEffectiveExtMaps(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(inheritedConnection!.Address, Is.EqualTo("192.0.2.1"));
            Assert.That(overriddenConnection!.Address, Is.EqualTo("198.51.100.2"));
            Assert.That(inheritedConnection, Is.Not.SameAs(description.Connection));
            Assert.That(service.ResolveEffectiveDirection(description, 0), Is.EqualTo(SdpDirection.SendOnly));
            Assert.That(service.ResolveEffectiveDirection(description, 1), Is.EqualTo(SdpDirection.ReceiveOnly));
            Assert.That(bandwidths.Select(value => value.Type), Is.EqualTo(new[] { "CT", "AS", "RR" }));
            Assert.That(bandwidths.Select(value => value.Value), Is.EqualTo(new long[] { 1000, 128, 32 }));
            Assert.That(bandwidths[0], Is.Not.SameAs(description.Bandwidths[0]));
            Assert.That(inheritedLanguages, Is.EqualTo(new[] { "en", "fr" }));
            Assert.That(overriddenLanguages, Is.EqualTo(new[] { "de" }));
            Assert.That(service.ResolveEffectiveDescriptionLanguages(description, 0), Is.EqualTo(new[] { "en-US" }));
            Assert.That(service.ResolveEffectiveDescriptionLanguages(description, 1), Is.EqualTo(new[] { "fr-CA" }));
            Assert.That(ice.Credentials!.UsernameFragment, Is.EqualTo("session"));
            Assert.That(dtls.Fingerprints[0].Algorithm, Is.EqualTo("sha-256"));
            Assert.That(dtls.SetupRole, Is.EqualTo(SdpDtlsSetupRole.ActPass));
            Assert.That(inheritedExtMaps.Select(value => value.Uri), Is.EqualTo(new[] { "urn:example:session" }));
            Assert.That(overriddenExtMaps.Select(value => value.Uri), Is.EqualTo(new[] { "urn:example:media" }));
        });

        inheritedConnection!.Address = "changed";
        bandwidths[0].Value = 1;
        overriddenExtMaps[0].Uri = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(service.ResolveEffectiveConnection(description, 0)!.Address, Is.EqualTo("192.0.2.1"));
            Assert.That(service.ResolveEffectiveBandwidths(description, 1)[0].Value, Is.EqualTo(1000));
            Assert.That(service.ResolveEffectiveExtMaps(description, 1)[0].Uri, Is.EqualTo("urn:example:media"));
        });
    }

    [Test]
    public void RejectedMedia_AlwaysResolvesInactiveDirection()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(PropertyAttribute(SdpAttributeNames.SendOnly));
        SdpMediaDescription media = Media("audio", 0, "audio");
        media.Attributes.Add(PropertyAttribute(SdpAttributeNames.ReceiveOnly));
        description.MediaDescriptions.Add(media);
        ISdpQueryService service = new SdpQueryService();

        Assert.That(service.ResolveEffectiveDirection(description, 0), Is.EqualTo(SdpDirection.Inactive));
    }

    [Test]
    public void EffectiveFiltersAndClocks_ApplyConnectionMatchingAndScopeOverrides()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpConnectionData connection = Connection("232.3.4.5");
        connection.AddressKind = SdpAddressKind.Ipv4Multicast;
        connection.HasAddressCount = true;
        connection.AddressCount = 3;
        description.Connection = connection;
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.SourceFilter, "incl IN IP4 232.3.4.6 192.0.2.10"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.SourceFilter, "incl IN IP4 232.3.4.9 192.0.2.11"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.TimestampReferenceClock, "local"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaClock, "sender"));

        SdpMediaDescription inherited = Media("audio", 5000, "audio");
        SdpMediaDescription overridden = Media("video", 6000, "video");
        overridden.Attributes.Add(ValueAttribute(SdpAttributeNames.SourceFilter, "excl IN IP4 * 198.51.100.1"));
        overridden.Attributes.Add(ValueAttribute(SdpAttributeNames.TimestampReferenceClock, "gps"));
        overridden.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaClock, "direct=42 rate=1000/1001"));
        description.MediaDescriptions.Add(inherited);
        description.MediaDescriptions.Add(overridden);
        ISdpQueryService service = new SdpQueryService();

        List<SdpSourceFilterAttribute> inheritedFilters = service.ResolveEffectiveSourceFilters(description, 0);
        List<SdpSourceFilterAttribute> overriddenFilters = service.ResolveEffectiveSourceFilters(description, 1);
        List<SdpClockReferenceAttribute> inheritedReferences = service.ResolveEffectiveClockReferences(description, 0);
        List<SdpClockReferenceAttribute> overriddenReferences = service.ResolveEffectiveClockReferences(description, 1);
        List<SdpMediaClockAttribute> overriddenClocks = service.ResolveEffectiveMediaClocks(description, 1);
        SdpEffectiveMedia effective = service.ResolveEffectiveMedia(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(inheritedFilters.Select(filter => filter.DestinationAddress), Is.EqualTo(new[] { "232.3.4.6" }));
            Assert.That(overriddenFilters.Select(filter => filter.FilterMode), Is.EqualTo(new[] { "excl" }));
            Assert.That(inheritedReferences.Select(reference => reference.SourceKind), Is.EqualTo(new[] { SdpClockSourceKind.Local }));
            Assert.That(overriddenReferences.Select(reference => reference.SourceKind), Is.EqualTo(new[] { SdpClockSourceKind.Gps }));
            Assert.That(overriddenClocks.Select(clock => clock.ClockName), Is.EqualTo(new[] { "direct" }));
            Assert.That(overriddenClocks[0].RateNumerator, Is.EqualTo(1000));
            Assert.That(effective.SourceFilters.Count, Is.EqualTo(1));
            Assert.That(effective.ClockReferences[0].SourceKind, Is.EqualTo(SdpClockSourceKind.Gps));
            Assert.That(effective.MediaClocks[0].ClockValue, Is.EqualTo("42"));
        });

        inheritedFilters[0].SourceAddresses[0] = "changed";
        inheritedReferences[0].SourceToken = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(service.ResolveEffectiveSourceFilters(description, 0)[0].SourceAddresses[0], Is.EqualTo("192.0.2.10"));
            Assert.That(service.ResolveEffectiveClockReferences(description, 0)[0].SourceToken, Is.EqualTo("local"));
        });
    }

    [Test]
    public void EffectiveClocks_AssumeRfcDefaultsOnlyWhenSignalingIsAbsent()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(Media("audio", 5000, "audio"));
        description.MediaDescriptions.Add(Media("video", 6000, "video"));
        ISdpQueryService service = new SdpQueryService();

        List<SdpClockReferenceAttribute> assumedReferences = service.ResolveEffectiveClockReferences(description, 0);
        List<SdpMediaClockAttribute> assumedMediaClocks = service.ResolveEffectiveMediaClocks(description, 0);

        description.Attributes.Add(ValueAttribute(SdpAttributeNames.TimestampReferenceClock, "local"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaClock, "sender"));
        description.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.TimestampReferenceClock, "invalid value"));
        description.MediaDescriptions[1].Attributes.Add(ValueAttribute(SdpAttributeNames.MediaClock, "direct=invalid"));

        Assert.Multiple(() =>
        {
            Assert.That(assumedReferences.Count, Is.EqualTo(1));
            Assert.That(assumedReferences[0].SourceKind, Is.EqualTo(SdpClockSourceKind.Local));
            Assert.That(assumedReferences[0].SourceAttribute, Is.Null);
            Assert.That(assumedMediaClocks.Count, Is.EqualTo(1));
            Assert.That(assumedMediaClocks[0].ClockName, Is.EqualTo("sender"));
            Assert.That(assumedMediaClocks[0].SourceAttribute, Is.Null);
            Assert.That(service.ResolveEffectiveClockReferences(description, 1), Is.Empty);
            Assert.That(service.ResolveEffectiveMediaClocks(description, 1), Is.Empty);
        });
    }

    [Test]
    public void EffectiveSourceFilters_MatchIpv6MulticastAddressCounts()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpConnectionData connection = Connection("FF0E::11A");
        connection.AddressType = SdpAddressTypes.Ip6;
        connection.AddressKind = SdpAddressKind.Ipv6Multicast;
        connection.HasAddressCount = true;
        connection.AddressCount = 3;
        description.Connection = connection;
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.SourceFilter, "incl IN IP6 FF0E::11C 2001:DB8::1"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.SourceFilter, "incl IN IP6 FF0E::11D 2001:DB8::2"));
        description.MediaDescriptions.Add(Media("audio", 5000, "audio"));
        ISdpQueryService service = new SdpQueryService();

        List<SdpSourceFilterAttribute> filters = service.ResolveEffectiveSourceFilters(description, 0);

        Assert.That(filters.Select(filter => filter.DestinationAddress), Is.EqualTo(new[] { "FF0E::11C" }));
    }

    [Test]
    public void EffectiveMedia_UsesBundleOwnerEndpointAndSelectedMediaValues()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));
        description.Bandwidths.Add(Bandwidth("CT", 900));

        SdpMediaDescription audio = Media("audio", 5000, "audio");
        audio.Connections.Add(Connection("192.0.2.10"));
        audio.Bandwidths.Add(Bandwidth("AS", 64));
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "owner"));
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "abcdefghijklmnopqrstuv"));
        audio.Attributes.Add(FingerprintAttribute("sha-256", "22", 32));
        audio.Attributes.Add(ValueAttribute(SdpAttributeNames.Setup, "actpass"));
        audio.Attributes.Add(ExtMap("1 urn:example:audio"));

        SdpMediaDescription video = Media("video", 6000, "video");
        video.Connections.Add(Connection("198.51.100.20"));
        video.Bandwidths.Add(Bandwidth("AS", 256));
        video.Attributes.Add(PropertyAttribute(SdpAttributeNames.ReceiveOnly));
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.Language, "de"));
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.IceUsernameFragment, "fallback"));
        video.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, "zyxwvutsrqponmlkjihgfe"));
        video.Attributes.Add(FingerprintAttribute("sha-384", "33", 48));
        video.Attributes.Add(ExtMap("2 urn:example:video"));
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(video);
        ISdpQueryService service = new SdpQueryService();

        SdpEffectiveMedia effective = service.ResolveEffectiveMediaByMid(description, "video") ?? new SdpEffectiveMedia();

        Assert.Multiple(() =>
        {
            Assert.That(effective.MediaIndex, Is.EqualTo(1));
            Assert.That(effective.TransportMediaIndex, Is.Zero);
            Assert.That(effective.MediaId, Is.EqualTo("video"));
            Assert.That(effective.Port, Is.EqualTo(5000));
            Assert.That(effective.PortCount, Is.EqualTo(1));
            Assert.That(effective.Connection!.Address, Is.EqualTo("192.0.2.10"));
            Assert.That(effective.Direction, Is.EqualTo(SdpDirection.ReceiveOnly));
            Assert.That(effective.Languages, Is.EqualTo(new[] { "de" }));
            Assert.That(effective.Ice!.Credentials!.UsernameFragment, Is.EqualTo("owner"));
            Assert.That(effective.Dtls!.Fingerprints[0].Algorithm, Is.EqualTo("sha-256"));
            Assert.That(effective.HeaderExtensions.Select(value => value.Uri), Is.EqualTo(new[] { "urn:example:video" }));
            Assert.That(effective.BundleTransport, Is.Not.Null);
            Assert.That(effective.BundleTransport!.TaggedMediaId, Is.EqualTo("audio"));
            Assert.That(effective.Bandwidths.Select(value => value.Type), Does.Contain("CT"));
        });

        effective.Connection!.Address = "changed";
        effective.Ice!.Credentials!.UsernameFragment = "changed";
        SdpEffectiveMedia repeated = service.ResolveEffectiveMedia(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Connection!.Address, Is.EqualTo("192.0.2.10"));
            Assert.That(repeated.Ice!.Credentials!.UsernameFragment, Is.EqualTo("owner"));
        });
    }

    [Test]
    public void EffectiveQueries_RejectInvalidSelectionArguments()
    {
        ISdpQueryService service = new SdpQueryService();
        SdpSessionDescription description = new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.FindMediaByIndex(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.FindMediaByMid(description, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mediaId"));
            Assert.That(() => service.FindMediaIndexByMid(description, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mediaId"));
            Assert.That(() => service.ResolveEffectiveConnection(description, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveBandwidths(description, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveDirection(description, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveLanguages(description, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveDescriptionLanguages(description, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveIce(description, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveDtls(description, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveExtMaps(description, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveSourceFilters(description, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveClockReferences(description, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveMediaClocks(description, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveMedia(description, -1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveEffectiveMediaByMid(description, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mediaId"));
        });
    }

    private static SdpMediaDescription Media(string mediaType, int port, string mediaId)
    {
        SdpMediaDescription media = new SdpMediaDescription();
        media.MediaType = mediaType;
        media.Port = port;
        media.PortCount = 1;
        media.Protocol = SdpTransportProtocols.RtpAvp;
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, mediaId));
        return media;
    }

    private static SdpConnectionData Connection(string address)
    {
        SdpConnectionData connection = new SdpConnectionData();
        connection.NetworkType = SdpNetworkTypes.Internet;
        connection.AddressType = SdpAddressTypes.Ip4;
        connection.Address = address;
        connection.RawAddress = address;
        connection.RawText = "IN IP4 " + address;
        connection.AddressKind = SdpAddressKind.Ipv4;
        return connection;
    }

    private static SdpBandwidth Bandwidth(string type, long value)
    {
        SdpBandwidth bandwidth = new SdpBandwidth();
        bandwidth.Type = type;
        bandwidth.Value = value;
        bandwidth.RawValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        bandwidth.RawText = type + ":" + bandwidth.RawValue;
        return bandwidth;
    }

    private static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Property, Name = name, RawText = name };
    }

    private static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    private static SdpAttribute ExtMap(string value)
    {
        return ValueAttribute(SdpAttributeNames.ExtMap, value);
    }

    private static SdpAttribute FingerprintAttribute(string algorithm, string pair, int pairCount)
    {
        string fingerprint = string.Join(":", Enumerable.Repeat(pair, pairCount));
        return ValueAttribute(SdpAttributeNames.Fingerprint, algorithm + " " + fingerprint);
    }
}
