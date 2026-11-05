namespace sdp.Tests.Unit.Validation;

public class SdpGroupValidationTests
{
    [Test]
    public void RegisteredAndExtensionGroups_AcceptExistingUniqueMediaIdentifiers()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", 5000);
        description.MediaDescriptions.Add(Media("video", 5002));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "LS audio video"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "FID audio video"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "X-Capability"));

        SdpValidationResult result = ValidateGroups(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void MediaIdentifiers_MustBeUniqueAndAppearAtMostOncePerMedia()
    {
        SdpSessionDescription description = DescriptionWithMedia("shared", 5000);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "alternate"));
        description.MediaDescriptions.Add(Media("shared", 5002));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "LS shared"));

        SdpValidationResult result = ValidateGroups(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttributeCardinality));
            Assert.That(result.Issues[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Issues[0].MediaIndex, Is.Zero);
            Assert.That(result.Issues[0].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Issues[1].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationGroup));
            Assert.That(result.Issues[1].MediaIndex, Is.EqualTo(1));
            Assert.That(result.Issues[1].Detail, Does.Contain("unique"));
        });
    }

    [Test]
    public void Grouping_RequiresEveryMediaIdAndEveryTagToResolve()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", 5000);
        description.MediaDescriptions.Add(new SdpMediaDescription { Port = 5002 });
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "FID audio missing"));

        SdpValidationResult result = ValidateGroups(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationGroup));
            Assert.That(result.Issues[0].Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(result.Issues[0].MediaIndex, Is.EqualTo(1));
            Assert.That(result.Issues[0].AttributeName, Is.EqualTo(SdpAttributeNames.MediaId));
            Assert.That(result.Issues[1].Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(result.Issues[1].AttributeName, Is.EqualTo(SdpAttributeNames.Group));
            Assert.That(result.Issues[1].Detail, Does.Contain("existing"));
        });
    }

    [Test]
    public void DuplicateTagsAndBundleMembership_AreRejectedIndependently()
    {
        SdpSessionDescription description = DescriptionWithMedia("audio", 5000);
        description.MediaDescriptions.Add(Media("video", 5002));
        description.MediaDescriptions.Add(Media("data", 5004));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio audio video"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE video data"));

        SdpValidationResult result = ValidateGroups(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationGroup), Is.True);
            Assert.That(result.Issues[0].Path.OccurrenceIndex, Is.Zero);
            Assert.That(result.Issues[0].Detail, Does.Contain("repeated"));
            Assert.That(result.Issues[1].Path.OccurrenceIndex, Is.EqualTo(1));
            Assert.That(result.Issues[1].Detail, Does.Contain("more than one BUNDLE"));
        });
    }

    [Test]
    public void EmptyGroupsAreValidAndBundleSpecificRulesHandleRejectedMedia()
    {
        SdpSessionDescription empty = DescriptionWithMedia("audio", 5000);
        empty.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "X-Capability"));
        SdpSessionDescription genericRejected = DescriptionWithMedia("audio", 5000);
        genericRejected.MediaDescriptions.Add(Media("video", 0));
        genericRejected.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "FID audio video"));
        SdpSessionDescription bundleRejected = DescriptionWithMedia("audio", 5000);
        bundleRejected.MediaDescriptions.Add(Media("video", 0));
        bundleRejected.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE audio video"));

        SdpValidationResult emptyResult = ValidateGroups(empty);
        SdpValidationResult genericResult = ValidateGroups(genericRejected);
        SdpValidationResult bundleResult = ValidateGroups(bundleRejected);

        Assert.Multiple(() =>
        {
            Assert.That(emptyResult.Success, Is.True);
            Assert.That(emptyResult.Issues, Is.Empty);
            Assert.That(genericResult.ErrorCount, Is.EqualTo(1));
            Assert.That(genericResult.Issues[0].Detail, Does.Contain("port is zero"));
            Assert.That(bundleResult.ErrorCount, Is.EqualTo(1));
            Assert.That(bundleResult.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationBundle));
            Assert.That(bundleResult.Issues[0].Detail, Does.Contain("zero-port"));
        });
    }

    [Test]
    public void GroupAndMidSyntaxAndScopes_ProduceLocatedDiagnostics()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpAttribute sessionMid = ValueAttribute(SdpAttributeNames.MediaId, "session");
        sessionMid.Location.LineNumber = 3;
        SdpAttribute malformedGroup = PropertyAttribute(SdpAttributeNames.Group);
        malformedGroup.Location.LineNumber = 4;
        description.Attributes.Add(sessionMid);
        description.Attributes.Add(malformedGroup);
        SdpMediaDescription media = new SdpMediaDescription();
        SdpAttribute mediaGroup = ValueAttribute(SdpAttributeNames.Group, "LS media");
        mediaGroup.Location.LineNumber = 7;
        SdpAttribute malformedMid = PropertyAttribute(SdpAttributeNames.MediaId);
        malformedMid.Location.LineNumber = 8;
        media.Attributes.Add(mediaGroup);
        media.Attributes.Add(malformedMid);
        description.MediaDescriptions.Add(media);

        SdpValidationResult result = ValidateGroups(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(4));
            Assert.That(result.Issues.Select(issue => issue.Code), Is.EqualTo(new[]
            {
                SdpDiagnosticCodes.ValidationAttributeScope,
                SdpDiagnosticCodes.ValidationGroup,
                SdpDiagnosticCodes.ValidationAttributeScope,
                SdpDiagnosticCodes.ValidationGroup
            }));
            Assert.That(result.Issues.Select(issue => issue.Location.LineNumber), Is.EqualTo(new[] { 3, 4, 7, 8 }));
            Assert.That(result.Issues.Select(issue => issue.Scope), Is.EqualTo(new[] { SdpScope.Session, SdpScope.Session, SdpScope.Media, SdpScope.Media }));
        });
    }

    [TestCase(SdpValidationProfile.Bundle, true)]
    [TestCase(SdpValidationProfile.WebRtc, true)]
    [TestCase(SdpValidationProfile.CombinedStrict, true)]
    [TestCase(SdpValidationProfile.SyntaxOnly, false)]
    [TestCase(SdpValidationProfile.Rtp, false)]
    public void GroupRules_RunOnlyForProfilesThatIncludeGrouping(SdpValidationProfile profile, bool expectedIssue)
    {
        SdpSessionDescription description = DescriptionWithMedia("duplicate", 5000);
        description.MediaDescriptions.Add(Media("duplicate", 5002));
        SdpValidationOptions options = new SdpValidationOptions { Profile = profile };

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, options);

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationGroup), Is.EqualTo(expectedIssue));
    }

    static SdpValidationResult ValidateGroups(SdpSessionDescription description)
    {
        SdpValidationOptions options = new SdpValidationOptions { Profile = SdpValidationProfile.Bundle };
        return new SdpValidator().ValidateDescription(description, options);
    }

    static SdpSessionDescription DescriptionWithMedia(string mediaId, int port)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(Media(mediaId, port));
        return description;
    }

    static SdpMediaDescription Media(string mediaId, int port)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            Port = port,
            Location = new SdpSourceLocation { LineNumber = 6, ColumnNumber = 1 }
        };
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, mediaId));
        return media;
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name,
            Value = value,
            RawText = name + ":" + value,
            Location = new SdpSourceLocation { LineNumber = 5, ColumnNumber = 1 }
        };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Property,
            Name = name,
            RawText = name,
            Location = new SdpSourceLocation { LineNumber = 5, ColumnNumber = 1 }
        };
    }
}
