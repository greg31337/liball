namespace sdp.Tests.Unit.Operations;

public class SdpGroupServiceTests
{
    [Test]
    public void ResolveGroups_PreservesOrderSourcesAndExtensionSemantics()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpAttribute bundleSource = ValueAttribute("GROUP", " BUNDLE  audio video ");
        SdpAttribute malformed = PropertyAttribute(SdpAttributeNames.Group);
        SdpAttribute extensionSource = ValueAttribute(SdpAttributeNames.Group, "X-Custom data control");
        description.Attributes.Add(PropertyAttribute("x-before"));
        description.Attributes.Add(bundleSource);
        description.Attributes.Add(malformed);
        description.Attributes.Add(extensionSource);
        ISdpGroupService service = new SdpGroupService();

        List<SdpGroupAttribute> first = service.ResolveGroups(description);
        first[0].IdentificationTags[0] = "changed";
        List<SdpGroupAttribute> second = service.ResolveGroups(description);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Count.EqualTo(2));
            Assert.That(second, Has.Count.EqualTo(2));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first[0], Is.Not.SameAs(second[0]));
            Assert.That(second[0].Semantic, Is.EqualTo(SdpGroupSemantic.Bundle));
            Assert.That(second[0].SemanticToken, Is.EqualTo(SdpGroupSemantics.Bundle));
            Assert.That(second[0].IdentificationTags, Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(second[0].SourceAttribute, Is.SameAs(bundleSource));
            Assert.That(second[1].Semantic, Is.EqualTo(SdpGroupSemantic.Unknown));
            Assert.That(second[1].SemanticToken, Is.EqualTo("X-Custom"));
            Assert.That(second[1].IdentificationTags, Is.EqualTo(new[] { "data", "control" }));
            Assert.That(second[1].SourceAttribute, Is.SameAs(extensionSource));
            Assert.That(bundleSource.Value, Is.EqualTo(" BUNDLE  audio video "));
        });
    }

    [Test]
    public void MediaIdLookup_SkipsMalformedValuesAndUsesExactIdentity()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription first = new SdpMediaDescription();
        first.Attributes.Add(PropertyAttribute(SdpAttributeNames.MediaId));
        SdpAttribute firstSource = ValueAttribute("MID", " audio ");
        first.Attributes.Add(firstSource);
        SdpMediaDescription second = new SdpMediaDescription();
        second.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "Audio"));
        description.MediaDescriptions.Add(first);
        description.MediaDescriptions.Add(second);
        ISdpGroupService service = new SdpGroupService();

        SdpMediaIdAttribute? resolved = service.ResolveMediaId(description, 0);
        SdpMediaIdAttribute? absent = service.ResolveMediaId(new SdpSessionDescription { MediaDescriptions = { new SdpMediaDescription() } }, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved!.Id, Is.EqualTo("audio"));
            Assert.That(resolved.SourceAttribute, Is.SameAs(firstSource));
            Assert.That(service.FindMediaIndex(description, "audio"), Is.Zero);
            Assert.That(service.FindMediaIndex(description, "Audio"), Is.EqualTo(1));
            Assert.That(service.FindMediaIndex(description, "missing"), Is.EqualTo(-1));
            Assert.That(absent, Is.Null);
        });
    }

    [Test]
    public void AddGroup_AppendsCanonicalKnownAndExactExtensionSemantics()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(PropertyAttribute("x-before"));
        List<string> bundleTags = new List<string> { "audio", "video" };
        ISdpGroupService service = new SdpGroupService();

        SdpGroupAttribute bundle = service.AddGroup(description, SdpGroupSemantic.Bundle, string.Empty, bundleTags);
        SdpGroupAttribute extension = service.AddGroup(description, SdpGroupSemantic.Unknown, "X-Custom", new List<string> { "data" });
        bundleTags[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(description.Attributes, Has.Count.EqualTo(3));
            Assert.That(bundle.SemanticToken, Is.EqualTo(SdpGroupSemantics.Bundle));
            Assert.That(bundle.IdentificationTags, Is.EqualTo(new[] { "audio", "video" }));
            Assert.That(bundle.SourceAttribute, Is.SameAs(description.Attributes[1]));
            Assert.That(bundle.SourceAttribute!.RawText, Is.EqualTo("group:BUNDLE audio video"));
            Assert.That(extension.Semantic, Is.EqualTo(SdpGroupSemantic.Unknown));
            Assert.That(extension.SemanticToken, Is.EqualTo("X-Custom"));
            Assert.That(extension.SourceAttribute, Is.SameAs(description.Attributes[2]));
            Assert.That(extension.SourceAttribute!.RawText, Is.EqualTo("group:X-Custom data"));
        });
    }

    [Test]
    public void UpdateGroup_IsAtomicAndSynchronizesTheExistingSource()
    {
        SdpAttribute source = ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video");
        source.Location.LineNumber = 19;
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(source);
        ISdpGroupService service = new SdpGroupService();
        SdpGroupAttribute group = service.ResolveGroups(description)[0];
        List<string> replacementTags = new List<string> { "data", "control" };

        service.UpdateGroup(group, SdpGroupSemantic.Unknown, "X-New", replacementTags);
        replacementTags[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(group.Semantic, Is.EqualTo(SdpGroupSemantic.Unknown));
            Assert.That(group.SemanticToken, Is.EqualTo("X-New"));
            Assert.That(group.IdentificationTags, Is.EqualTo(new[] { "data", "control" }));
            Assert.That(group.SourceAttribute, Is.SameAs(source));
            Assert.That(source.Value, Is.EqualTo("X-New data control"));
            Assert.That(source.RawText, Is.EqualTo("group:X-New data control"));
            Assert.That(source.Location.LineNumber, Is.EqualTo(19));
        });

        Assert.That(() => service.UpdateGroup(group, SdpGroupSemantic.Bundle, "FID", new List<string> { "invalid" }), Throws.ArgumentException);

        Assert.Multiple(() =>
        {
            Assert.That(group.SemanticToken, Is.EqualTo("X-New"));
            Assert.That(group.IdentificationTags, Is.EqualTo(new[] { "data", "control" }));
            Assert.That(source.Value, Is.EqualTo("X-New data control"));
        });
    }

    [Test]
    public void RemoveGroup_RemovesOnlyTheResolvedSourceAndIsIdempotent()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpAttribute firstSource = ValueAttribute(SdpAttributeNames.Group, "FID primary repair");
        SdpAttribute secondSource = ValueAttribute(SdpAttributeNames.Group, "FID primary repair");
        description.Attributes.Add(firstSource);
        description.Attributes.Add(PropertyAttribute("x-between"));
        description.Attributes.Add(secondSource);
        ISdpGroupService service = new SdpGroupService();
        SdpGroupAttribute second = service.ResolveGroups(description)[1];

        bool removed = service.RemoveGroup(description, second);
        bool removedAgain = service.RemoveGroup(description, second);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(removedAgain, Is.False);
            Assert.That(description.Attributes, Has.Count.EqualTo(2));
            Assert.That(description.Attributes[0], Is.SameAs(firstSource));
            Assert.That(description.Attributes[1].Name, Is.EqualTo("x-between"));
            Assert.That(description.Attributes.Contains(secondSource), Is.False);
            Assert.That(service.RemoveGroup(description, new SdpGroupAttribute()), Is.False);
        });
    }

    [Test]
    public void PublicMethods_RejectInvalidArgumentsWithoutMutation()
    {
        ISdpGroupService service = new SdpGroupService();
        SdpSessionDescription empty = new SdpSessionDescription();
        SdpSessionDescription withMedia = new SdpSessionDescription();
        withMedia.MediaDescriptions.Add(new SdpMediaDescription());
        SdpGroupAttribute existing = new SdpGroupAttribute
        {
            Semantic = SdpGroupSemantic.Unknown,
            SemanticToken = "X-Existing"
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ResolveGroups(null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.ResolveMediaId(empty, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.FindMediaIndex(null!, "audio"), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.FindMediaIndex(empty, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("mediaId"));
            Assert.That(() => service.FindMediaIndex(empty, "two ids"), Throws.ArgumentException.With.Property("ParamName").EqualTo("mediaId"));
            Assert.That(() => service.AddGroup(null!, SdpGroupSemantic.Bundle, string.Empty, new List<string>()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.AddGroup(empty, (SdpGroupSemantic)99, "X-Test", new List<string>()), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("semantic"));
            Assert.That(() => service.AddGroup(empty, SdpGroupSemantic.Bundle, "FID", new List<string>()), Throws.ArgumentException.With.Property("ParamName").EqualTo("semanticToken"));
            Assert.That(() => service.AddGroup(empty, SdpGroupSemantic.Unknown, "BUNDLE", new List<string>()), Throws.ArgumentException.With.Property("ParamName").EqualTo("semanticToken"));
            Assert.That(() => service.AddGroup(empty, SdpGroupSemantic.Unknown, "X-Test", new List<string> { "bad/tag" }), Throws.ArgumentException.With.Property("ParamName").EqualTo("identificationTags"));
            Assert.That(() => service.UpdateGroup(existing, SdpGroupSemantic.Unknown, "bad/token", new List<string>()), Throws.ArgumentException.With.Property("ParamName").EqualTo("semanticToken"));
            Assert.That(() => service.RemoveGroup(withMedia, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("group"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(empty.Attributes, Is.Empty);
            Assert.That(existing.SemanticToken, Is.EqualTo("X-Existing"));
            Assert.That(existing.IdentificationTags, Is.Empty);
        });
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name,
            Value = value,
            RawText = name + ":" + value
        };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Property,
            Name = name,
            RawText = name
        };
    }
}
