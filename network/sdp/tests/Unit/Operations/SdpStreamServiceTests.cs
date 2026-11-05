namespace sdp.Tests.Unit.Operations;

public class SdpStreamServiceTests
{
    [Test]
    public void StreamResolvers_ProjectAllSupportedSignalingInSourceOrder()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpAttribute semanticSource = ValueAttribute("MSID-SEMANTIC", "WMS stream-media stream-source");
        SdpAttribute extensionSemanticSource = ValueAttribute("msid-semantic", "X-STREAM extension-stream");
        description.Attributes.Add(PropertyAttribute("x-session"));
        description.Attributes.Add(semanticSource);
        description.Attributes.Add(PropertyAttribute("msid-semantic"));
        description.Attributes.Add(extensionSemanticSource);

        SdpMediaDescription media = description.MediaDescriptions[0];
        SdpAttribute cnameSource = ValueAttribute("SSRC", "11 cname:user@example.test");
        SdpAttribute groupSource = ValueAttribute("SSRC-GROUP", "FID 11 12");
        SdpAttribute sourceMsid = ValueAttribute("ssrc", "11 msid:stream-source track-source");
        SdpAttribute mediaMsid = ValueAttribute("MSID", "stream-media track-media");
        SdpAttribute highRid = ValueAttribute("RID", "high send pt=96;max-width=1280");
        SdpAttribute lowRid = ValueAttribute("rid", "low send pt=97;vendor-limit=small");
        SdpAttribute simulcastSource = ValueAttribute("SIMULCAST", "recv remote send high;~low");
        SdpAttribute imageSource = ValueAttribute("IMAGEATTR", "96 send [x=1280,y=720] recv *");
        SdpAttribute wildcardImageSource = ValueAttribute("imageattr", "* send *");
        media.Attributes.Add(PropertyAttribute("x-before"));
        media.Attributes.Add(cnameSource);
        media.Attributes.Add(PropertyAttribute("ssrc"));
        media.Attributes.Add(groupSource);
        media.Attributes.Add(sourceMsid);
        media.Attributes.Add(mediaMsid);
        media.Attributes.Add(highRid);
        media.Attributes.Add(PropertyAttribute("rid"));
        media.Attributes.Add(lowRid);
        media.Attributes.Add(PropertyAttribute("simulcast"));
        media.Attributes.Add(simulcastSource);
        media.Attributes.Add(ValueAttribute("simulcast", "send ignored"));
        media.Attributes.Add(imageSource);
        media.Attributes.Add(PropertyAttribute("imageattr"));
        media.Attributes.Add(wildcardImageSource);
        ISdpStreamService service = new SdpStreamService();

        List<SdpSourceAttribute> sources = service.ResolveSourceAttributes(description, 0);
        List<SdpSourceGroupAttribute> groups = service.ResolveSourceGroups(description, 0);
        List<SdpMediaStreamId> streamIds = service.ResolveMediaStreamIds(description, 0);
        List<SdpMediaStreamSemanticAttribute> semantics = service.ResolveMediaStreamSemantics(description);
        List<SdpRidAttribute> rids = service.ResolveRids(description, 0);
        SdpSimulcastAttribute? simulcast = service.ResolveSimulcast(description, 0);
        List<SdpImageAttribute> images = service.ResolveImageAttributes(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(sources.Select(item => item.Name), Is.EqualTo(new[] { "cname", "msid" }));
            Assert.That(sources[0].SourceAttribute, Is.SameAs(cnameSource));
            Assert.That(sources[1].SynchronizationSource, Is.EqualTo(11u));
            Assert.That(groups, Has.Count.EqualTo(1));
            Assert.That(groups[0].SourceAttribute, Is.SameAs(groupSource));
            Assert.That(groups[0].SynchronizationSources, Is.EqualTo(new uint[] { 11u, 12u }));
            Assert.That(streamIds.Select(item => item.StreamId), Is.EqualTo(new[] { "stream-source", "stream-media" }));
            Assert.That(streamIds[0].SourceLevel, Is.True);
            Assert.That(streamIds[0].SynchronizationSource, Is.EqualTo(11u));
            Assert.That(streamIds[1].SourceLevel, Is.False);
            Assert.That(streamIds[1].SourceAttribute, Is.SameAs(mediaMsid));
            Assert.That(semantics.Select(item => item.Semantic), Is.EqualTo(new[] { "WMS", "X-STREAM" }));
            Assert.That(semantics[0].SourceAttribute, Is.SameAs(semanticSource));
            Assert.That(semantics[1].SourceAttribute, Is.SameAs(extensionSemanticSource));
            Assert.That(rids.Select(item => item.Id), Is.EqualTo(new[] { "high", "low" }));
            Assert.That(rids[0].PayloadTypes, Is.EqualTo(new[] { 96 }));
            Assert.That(rids[0].Restrictions[0].RawText, Is.EqualTo("max-width=1280"));
            Assert.That(rids[1].SourceAttribute, Is.SameAs(lowRid));
            Assert.That(simulcast, Is.Not.Null);
            Assert.That(simulcast!.SourceAttribute, Is.SameAs(simulcastSource));
            Assert.That(simulcast.SendFirst, Is.False);
            Assert.That(simulcast.SendStreams.SelectMany(stream => stream.Alternatives).Select(item => item.RidId), Is.EqualTo(new[] { "high", "low" }));
            Assert.That(simulcast.SendStreams[1].Alternatives[0].Paused, Is.True);
            Assert.That(images.Select(item => item.PayloadType), Is.EqualTo(new[] { "96", "*" }));
            Assert.That(images[0].SourceAttribute, Is.SameAs(imageSource));
            Assert.That(images[0].SendConstraints, Is.EqualTo("[x=1280,y=720]"));
            Assert.That(images[1].SourceAttribute, Is.SameAs(wildcardImageSource));
        });
    }

    [Test]
    public void ResolvedGraphs_AreDeeplyIndependentAcrossCalls()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        description.Attributes.Add(ValueAttribute("msid-semantic", "WMS stream-one stream-two"));
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("ssrc-group", "FID 1 2"));
        media.Attributes.Add(ValueAttribute("rid", "high send pt=96,97;max-width=1280"));
        media.Attributes.Add(ValueAttribute("simulcast", "send high,~backup;low"));
        ISdpStreamService service = new SdpStreamService();

        List<SdpSourceGroupAttribute> firstGroups = service.ResolveSourceGroups(description, 0);
        List<SdpRidAttribute> firstRids = service.ResolveRids(description, 0);
        SdpSimulcastAttribute firstSimulcast = service.ResolveSimulcast(description, 0)!;
        List<SdpMediaStreamSemanticAttribute> firstSemantics = service.ResolveMediaStreamSemantics(description);
        firstGroups[0].SynchronizationSources[0] = 99u;
        firstRids[0].PayloadTypes[0] = 100;
        firstRids[0].Restrictions[0].Value = "changed";
        firstSimulcast.SendStreams[0].Alternatives[0].RidId = "changed";
        firstSemantics[0].StreamIds[0] = "changed";

        List<SdpSourceGroupAttribute> secondGroups = service.ResolveSourceGroups(description, 0);
        List<SdpRidAttribute> secondRids = service.ResolveRids(description, 0);
        SdpSimulcastAttribute secondSimulcast = service.ResolveSimulcast(description, 0)!;
        List<SdpMediaStreamSemanticAttribute> secondSemantics = service.ResolveMediaStreamSemantics(description);

        Assert.Multiple(() =>
        {
            Assert.That(secondGroups, Is.Not.SameAs(firstGroups));
            Assert.That(secondGroups[0], Is.Not.SameAs(firstGroups[0]));
            Assert.That(secondGroups[0].SynchronizationSources, Is.EqualTo(new uint[] { 1u, 2u }));
            Assert.That(secondRids[0].PayloadTypes, Is.EqualTo(new[] { 96, 97 }));
            Assert.That(secondRids[0].Restrictions[0], Is.Not.SameAs(firstRids[0].Restrictions[0]));
            Assert.That(secondRids[0].Restrictions[0].Value, Is.EqualTo("1280"));
            Assert.That(secondSimulcast, Is.Not.SameAs(firstSimulcast));
            Assert.That(secondSimulcast.SendStreams[0], Is.Not.SameAs(firstSimulcast.SendStreams[0]));
            Assert.That(secondSimulcast.SendStreams[0].Alternatives[0].RidId, Is.EqualTo("high"));
            Assert.That(secondSemantics[0].StreamIds, Is.EqualTo(new[] { "stream-one", "stream-two" }));
        });
    }

    [Test]
    public void ResolveSimulcast_SkipsMalformedInputAndCopiesLegacyFallback()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpMediaDescription media = description.MediaDescriptions[0];
        SdpAttribute malformed = PropertyAttribute("simulcast");
        SdpAttribute legacy = ValueAttribute("simulcast", " send rid=high;low paused=low");
        media.Attributes.Add(malformed);
        media.Attributes.Add(legacy);
        ISdpStreamService service = new SdpStreamService();

        SdpSimulcastAttribute? first = service.ResolveSimulcast(description, 0);
        SdpSimulcastAttribute? second = service.ResolveSimulcast(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(first!.SourceAttribute, Is.SameAs(legacy));
            Assert.That(first.LegacySyntax, Is.True);
            Assert.That(first.RawValue, Is.EqualTo(" send rid=high;low paused=low"));
            Assert.That(first.SendStreams, Is.Empty);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second!.RawValue, Is.EqualTo(first.RawValue));
        });
    }

    [Test]
    public void Resolvers_UseOnlyTheSelectedMediaAndReturnEmptyCollectionsWhenAbsent()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpMediaDescription secondMedia = new SdpMediaDescription();
        secondMedia.Attributes.Add(ValueAttribute("rid", "second recv pt=98"));
        description.MediaDescriptions.Add(secondMedia);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("rid", "first send pt=96"));
        ISdpStreamService service = new SdpStreamService();

        List<SdpRidAttribute> secondRids = service.ResolveRids(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(secondRids.Select(item => item.Id), Is.EqualTo(new[] { "second" }));
            Assert.That(service.ResolveSourceAttributes(description, 1), Is.Empty);
            Assert.That(service.ResolveSourceGroups(description, 1), Is.Empty);
            Assert.That(service.ResolveMediaStreamIds(description, 1), Is.Empty);
            Assert.That(service.ResolveMediaStreamSemantics(description), Is.Empty);
            Assert.That(service.ResolveSimulcast(description, 1), Is.Null);
            Assert.That(service.ResolveImageAttributes(description, 1), Is.Empty);
        });
    }

    [Test]
    public void PublicMethods_RejectInvalidMediaSelections()
    {
        ISdpStreamService service = new SdpStreamService();
        SdpSessionDescription empty = new SdpSessionDescription();
        SdpSessionDescription description = DescriptionWithMedia();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ResolveSourceAttributes(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.ResolveSourceGroups(empty, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveMediaStreamIds(description, -1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveMediaStreamSemantics(null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.ResolveRids(description, 1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveSimulcast(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.ResolveImageAttributes(description, 2), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
        });
    }

    static SdpSessionDescription DescriptionWithMedia()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(new SdpMediaDescription());
        return description;
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Property, Name = name, RawText = name };
    }
}
