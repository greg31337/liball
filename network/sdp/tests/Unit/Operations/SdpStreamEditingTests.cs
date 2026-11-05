namespace sdp.Tests.Unit.Operations;

public class SdpStreamEditingTests
{
    [Test]
    public void RenameRid_UpdatesDependenciesSimulcastAndBackingAttributes()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpMediaDescription media = description.MediaDescriptions[0];
        SdpAttribute highSource = AddRid(media, "high send pt=96;max-width=1280");
        AddRid(media, "base send");
        AddRid(media, "top send depend=high,base");
        SdpAttribute simulcastSource = AddSimulcast(media, "send high,~base;top");
        ISdpStreamService service = new SdpStreamService();

        SdpRidAttribute renamed = service.RenameRid(description, 0, "high", "main");
        List<SdpRidAttribute> rids = service.ResolveRids(description, 0);
        SdpSimulcastAttribute simulcast = service.ResolveSimulcast(description, 0)!;

        Assert.Multiple(() =>
        {
            Assert.That(renamed.Id, Is.EqualTo("main"));
            Assert.That(renamed.SourceAttribute, Is.SameAs(highSource));
            Assert.That(highSource.Value, Is.EqualTo("main send pt=96;max-width=1280"));
            Assert.That(highSource.RawText, Is.EqualTo("rid:main send pt=96;max-width=1280"));
            Assert.That(highSource.Location.LineNumber, Is.EqualTo(7));
            Assert.That(rids.Select(rid => rid.Id), Is.EqualTo(new[] { "main", "base", "top" }));
            Assert.That(rids[2].Restrictions[0].Value, Is.EqualTo("main,base"));
            Assert.That(rids[2].SourceAttribute!.Value, Is.EqualTo("top send depend=main,base"));
            Assert.That(simulcast.SendStreams.SelectMany(stream => stream.Alternatives).Select(alternative => alternative.RidId), Is.EqualTo(new[] { "main", "base", "top" }));
            Assert.That(simulcast.SendStreams[0].Alternatives[1].Paused, Is.True);
            Assert.That(simulcastSource.Value, Is.EqualTo("send main,~base;top"));
        });
    }

    [Test]
    public void RenameRid_RejectsCollisionsMissingIdsAndInvalidGraphsAtomically()
    {
        SdpSessionDescription collision = DescriptionWithMedia();
        AddRid(collision.MediaDescriptions[0], "one send");
        AddRid(collision.MediaDescriptions[0], "two send depend=one");
        AddSimulcast(collision.MediaDescriptions[0], "send one;two");
        string[] collisionBefore = collision.MediaDescriptions[0].Attributes.Select(attribute => attribute.RawText).ToArray();
        SdpSessionDescription dangling = DescriptionWithMedia();
        AddRid(dangling.MediaDescriptions[0], "one send");
        AddRid(dangling.MediaDescriptions[0], "two send depend=missing");
        AddSimulcast(dangling.MediaDescriptions[0], "send one;two");
        string[] danglingBefore = dangling.MediaDescriptions[0].Attributes.Select(attribute => attribute.RawText).ToArray();
        ISdpStreamService service = new SdpStreamService();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.RenameRid(collision, 0, "one", "two"), Throws.ArgumentException.With.Property("ParamName").EqualTo("replacementId"));
            Assert.That(() => service.RenameRid(collision, 0, "missing", "new"), Throws.ArgumentException.With.Property("ParamName").EqualTo("currentId"));
            Assert.That(() => service.RenameRid(dangling, 0, "one", "new"), Throws.ArgumentException.With.Property("ParamName").EqualTo("description"));
            Assert.That(collision.MediaDescriptions[0].Attributes.Select(attribute => attribute.RawText), Is.EqualTo(collisionBefore));
            Assert.That(dangling.MediaDescriptions[0].Attributes.Select(attribute => attribute.RawText), Is.EqualTo(danglingBefore));
        });
    }

    [Test]
    public void RemoveRid_CleansDependenciesAndSimulcastWithoutChangingOrder()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpMediaDescription media = description.MediaDescriptions[0];
        AddRid(media, "base send");
        SdpAttribute removedSource = AddRid(media, "middle send depend=base");
        AddRid(media, "high send depend=middle,base");
        AddRid(media, "remote recv");
        AddSimulcast(media, "send base;middle,high recv remote");
        ISdpStreamService service = new SdpStreamService();

        bool removed = service.RemoveRid(description, 0, "middle");
        bool removedAgain = service.RemoveRid(description, 0, "middle");
        List<SdpRidAttribute> rids = service.ResolveRids(description, 0);
        SdpSimulcastAttribute simulcast = service.ResolveSimulcast(description, 0)!;

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(removedAgain, Is.False);
            Assert.That(media.Attributes.Contains(removedSource), Is.False);
            Assert.That(rids.Select(rid => rid.Id), Is.EqualTo(new[] { "base", "high", "remote" }));
            Assert.That(rids[1].Restrictions[0].Value, Is.EqualTo("base"));
            Assert.That(rids[1].SourceAttribute!.Value, Is.EqualTo("high send depend=base"));
            Assert.That(simulcast.SendStreams.SelectMany(stream => stream.Alternatives).Select(alternative => alternative.RidId), Is.EqualTo(new[] { "base", "high" }));
            Assert.That(simulcast.ReceiveStreams[0].Alternatives[0].RidId, Is.EqualTo("remote"));
            Assert.That(simulcast.SourceAttribute!.Value, Is.EqualTo("send base;high recv remote"));
        });
    }

    [Test]
    public void RemoveRid_RemovesEmptyDependencyAndLastSimulcastAttribute()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpMediaDescription media = description.MediaDescriptions[0];
        SdpAttribute marker = PropertyAttribute("x-before");
        media.Attributes.Add(marker);
        AddRid(media, "only send");
        AddRid(media, "unrelated send depend=only");
        SdpAttribute simulcastSource = AddSimulcast(media, "send only");
        ISdpStreamService service = new SdpStreamService();

        bool removed = service.RemoveRid(description, 0, "only");
        List<SdpRidAttribute> rids = service.ResolveRids(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(rids.Select(rid => rid.Id), Is.EqualTo(new[] { "unrelated" }));
            Assert.That(rids[0].Restrictions, Is.Empty);
            Assert.That(rids[0].SourceAttribute!.Value, Is.EqualTo("unrelated send"));
            Assert.That(service.ResolveSimulcast(description, 0), Is.Null);
            Assert.That(media.Attributes.Contains(simulcastSource), Is.False);
            Assert.That(media.Attributes[0], Is.SameAs(marker));
        });
    }

    [Test]
    public void FilterSimulcastLayers_RetainsOrderPauseAndUnrelatedRids()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpMediaDescription media = description.MediaDescriptions[0];
        AddRid(media, "base send");
        SdpAttribute middleSource = AddRid(media, "middle send depend=base");
        AddRid(media, "high send depend=middle,base");
        SdpAttribute remoteSource = AddRid(media, "remote recv");
        AddRid(media, "unrelated send depend=middle");
        AddSimulcast(media, "send high;~middle,base recv remote");
        ISdpStreamService service = new SdpStreamService();

        SdpSimulcastAttribute filtered = service.FilterSimulcastLayers(description, 0, new List<string> { "high", "base" })!;
        List<SdpRidAttribute> rids = service.ResolveRids(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(rids.Select(rid => rid.Id), Is.EqualTo(new[] { "base", "high", "unrelated" }));
            Assert.That(media.Attributes.Contains(middleSource), Is.False);
            Assert.That(media.Attributes.Contains(remoteSource), Is.False);
            Assert.That(rids[1].Restrictions[0].Value, Is.EqualTo("base"));
            Assert.That(rids[2].Restrictions, Is.Empty);
            Assert.That(filtered.SendStreams, Has.Count.EqualTo(2));
            Assert.That(filtered.SendStreams.SelectMany(stream => stream.Alternatives).Select(alternative => alternative.RidId), Is.EqualTo(new[] { "high", "base" }));
            Assert.That(filtered.SendStreams[1].Alternatives[0].Paused, Is.False);
            Assert.That(filtered.ReceiveStreams, Is.Empty);
            Assert.That(filtered.SourceAttribute!.Value, Is.EqualTo("send high;base"));
        });
    }

    [Test]
    public void FilterSimulcastLayers_WithNoRetainedLayersRemovesOnlySimulcastRids()
    {
        SdpSessionDescription description = DescriptionWithMedia();
        SdpMediaDescription media = description.MediaDescriptions[0];
        AddRid(media, "one send");
        AddRid(media, "two recv");
        AddRid(media, "unrelated send");
        AddSimulcast(media, "send one recv two");
        ISdpStreamService service = new SdpStreamService();

        SdpSimulcastAttribute? filtered = service.FilterSimulcastLayers(description, 0, new List<string>());

        Assert.Multiple(() =>
        {
            Assert.That(filtered, Is.Null);
            Assert.That(service.ResolveRids(description, 0).Select(rid => rid.Id), Is.EqualTo(new[] { "unrelated" }));
            Assert.That(service.ResolveSimulcast(description, 0), Is.Null);
        });
    }

    [Test]
    public void FilterSimulcastLayers_RejectsUnknownAndLegacyLayersAtomically()
    {
        SdpSessionDescription modern = DescriptionWithMedia();
        AddRid(modern.MediaDescriptions[0], "one send");
        AddSimulcast(modern.MediaDescriptions[0], "send one");
        string[] modernBefore = modern.MediaDescriptions[0].Attributes.Select(attribute => attribute.RawText).ToArray();
        SdpSessionDescription legacy = DescriptionWithMedia();
        AddRid(legacy.MediaDescriptions[0], "one send");
        AddSimulcast(legacy.MediaDescriptions[0], " send rid=one");
        string[] legacyBefore = legacy.MediaDescriptions[0].Attributes.Select(attribute => attribute.RawText).ToArray();
        ISdpStreamService service = new SdpStreamService();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.FilterSimulcastLayers(modern, 0, new List<string> { "missing" }), Throws.ArgumentException.With.Property("ParamName").EqualTo("retainedRidIds"));
            Assert.That(() => service.FilterSimulcastLayers(legacy, 0, new List<string> { "one" }), Throws.ArgumentException.With.Property("ParamName").EqualTo("description"));
            Assert.That(modern.MediaDescriptions[0].Attributes.Select(attribute => attribute.RawText), Is.EqualTo(modernBefore));
            Assert.That(legacy.MediaDescriptions[0].Attributes.Select(attribute => attribute.RawText), Is.EqualTo(legacyBefore));
        });
    }

    [Test]
    public void EditingMethods_RejectInvalidArgumentsWithoutMutation()
    {
        ISdpStreamService service = new SdpStreamService();
        SdpSessionDescription empty = new SdpSessionDescription();
        SdpSessionDescription description = DescriptionWithMedia();
        AddRid(description.MediaDescriptions[0], "one send");
        string[] before = description.MediaDescriptions[0].Attributes.Select(attribute => attribute.RawText).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.RenameRid(null!, 0, "one", "two"), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.RenameRid(empty, 0, "one", "two"), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.RenameRid(description, 0, null!, "two"), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("currentId"));
            Assert.That(() => service.RenameRid(description, 0, "one", "bad.id"), Throws.ArgumentException.With.Property("ParamName").EqualTo("replacementId"));
            Assert.That(() => service.RemoveRid(description, 0, "bad id"), Throws.ArgumentException.With.Property("ParamName").EqualTo("ridId"));
            Assert.That(() => service.FilterSimulcastLayers(description, 0, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("retainedRidIds"));
            Assert.That(() => service.FilterSimulcastLayers(description, 0, new List<string> { "one", "one" }), Throws.ArgumentException.With.Property("ParamName").EqualTo("retainedRidIds"));
            Assert.That(description.MediaDescriptions[0].Attributes.Select(attribute => attribute.RawText), Is.EqualTo(before));
        });
    }

    static SdpSessionDescription DescriptionWithMedia()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(new SdpMediaDescription());
        return description;
    }

    static SdpAttribute AddRid(SdpMediaDescription media, string value)
    {
        SdpAttribute attribute = ValueAttribute(SdpAttributeNames.Rid, value);
        media.Attributes.Add(attribute);
        return attribute;
    }

    static SdpAttribute AddSimulcast(SdpMediaDescription media, string value)
    {
        SdpAttribute attribute = ValueAttribute(SdpAttributeNames.Simulcast, value);
        media.Attributes.Add(attribute);
        return attribute;
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name,
            Value = value,
            RawText = name + ":" + value,
            Location = new SdpSourceLocation { LineNumber = 7, ColumnNumber = 1 }
        };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Property, Name = name, RawText = name };
    }
}
