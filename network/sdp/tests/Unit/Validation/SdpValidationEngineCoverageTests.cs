using System.Text;
using System.Text.Json;

namespace sdp.Tests.Unit.Validation;

public class SdpValidationEngineCoverageTests
{
    [TestCase(SdpValidationProfile.SyntaxOnly, "")]
    [TestCase(SdpValidationProfile.Rfc8866, SdpDiagnosticCodes.ParseMissingRequiredField)]
    [TestCase(SdpValidationProfile.Ice, SdpDiagnosticCodes.ValidationIce)]
    [TestCase(SdpValidationProfile.TlsDtls, SdpDiagnosticCodes.ValidationDtls)]
    [TestCase(SdpValidationProfile.Rtp, SdpDiagnosticCodes.ValidationDynamicPayloadMissingRtpMap)]
    [TestCase(SdpValidationProfile.WebRtc, SdpDiagnosticCodes.ValidationIce)]
    [TestCase(SdpValidationProfile.Bundle, SdpDiagnosticCodes.ValidationGroup)]
    [TestCase(SdpValidationProfile.DataChannel, SdpDiagnosticCodes.ValidationDataChannel)]
    [TestCase(SdpValidationProfile.Bfcp, SdpDiagnosticCodes.ValidationBfcp)]
    [TestCase(SdpValidationProfile.CombinedStrict, SdpDiagnosticCodes.ValidationIce)]
    public void EveryValidationProfile_RunsItsExpectedRuleFamily(SdpValidationProfile profile, string expectedCode)
    {
        SdpSessionDescription description = DescriptionWithIndependentFailures();

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = profile });

        Assert.Multiple(() =>
        {
            Assert.That(result.AppliedOptions.Profile, Is.EqualTo(profile));
            if (profile == SdpValidationProfile.SyntaxOnly)
            {
                Assert.That(result.Issues, Is.Empty);
                Assert.That(result.Success, Is.True);
            }
            else
            {
                Assert.That(result.Issues.Any(issue => issue.Code == expectedCode), Is.True);
                Assert.That(result.Success, Is.False);
            }
        });
    }

    [Test]
    public void SeverityPolicies_FilterSuppressAndPromoteStableCodes()
    {
        SdpSessionDescription description = ValidCoreDescription();
        description.EncryptionKey = new SdpEncryptionKey
        {
            Method = "prompt",
            RawText = "prompt"
        };
        SdpValidationOptions baselineOptions = CoreOptions();
        SdpValidationOptions filteredOptions = CoreOptions();
        filteredOptions.MinimumSeverity = SdpValidationSeverity.Error;
        SdpValidationOptions suppressedOptions = CoreOptions();
        suppressedOptions.SuppressedDiagnosticCodes.Add(SdpDiagnosticCodes.ValidationObsoleteSyntax);
        SdpValidationOptions promotedOptions = CoreOptions();
        promotedOptions.PromotedDiagnosticCodes.Add(SdpDiagnosticCodes.ValidationObsoleteSyntax);
        ISdpValidator validator = new SdpValidator();

        SdpValidationResult baseline = validator.ValidateDescription(description, baselineOptions);
        SdpValidationResult filtered = validator.ValidateDescription(description, filteredOptions);
        SdpValidationResult suppressed = validator.ValidateDescription(description, suppressedOptions);
        SdpValidationResult promoted = validator.ValidateDescription(description, promotedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(baseline.Issues, Has.Count.EqualTo(1));
            Assert.That(baseline.Issues[0].Severity, Is.EqualTo(SdpValidationSeverity.Warning));
            Assert.That(baseline.WarningCount, Is.EqualTo(1));
            Assert.That(baseline.Success, Is.True);
            Assert.That(filtered.Issues, Is.Empty);
            Assert.That(filtered.Success, Is.True);
            Assert.That(suppressed.Issues, Is.Empty);
            Assert.That(suppressed.Success, Is.True);
            Assert.That(promoted.Issues, Has.Count.EqualTo(1));
            Assert.That(promoted.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationObsoleteSyntax));
            Assert.That(promoted.Issues[0].Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(promoted.ErrorCount, Is.EqualTo(1));
            Assert.That(promoted.Success, Is.False);
        });
    }

    [Test]
    public void CombinedValidation_RespectsTheIssueLimitAndCompletesTheTerminalIssue()
    {
        SdpValidationOptions options = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.CombinedStrict,
            MaximumDiagnosticCount = 3
        };

        SdpValidationResult result = new SdpValidator().ValidateDescription(DescriptionWithIndependentFailures(), options);
        SdpValidationIssue terminal = result.Issues[2];

        Assert.Multiple(() =>
        {
            Assert.That(result.DiagnosticLimitReached, Is.True);
            Assert.That(result.Issues, Has.Count.EqualTo(3));
            Assert.That(terminal.Code, Is.EqualTo(SdpDiagnosticCodes.DiagnosticLimitReached));
            Assert.That(terminal.Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(terminal.Scope, Is.EqualTo(terminal.Path.Scope));
            Assert.That(terminal.MediaIndex, Is.EqualTo(terminal.Path.MediaIndex));
            Assert.That(terminal.FieldName, Is.EqualTo(terminal.Path.FieldName));
            Assert.That(terminal.AttributeName, Is.EqualTo(terminal.Path.AttributeName));
            Assert.That(terminal.Detail, Is.Not.Empty);
        });
    }

    [Test]
    public void CombinedValidation_DoesNotMutateTheDescriptionOfferOrOptions()
    {
        SdpSessionDescription description = DescriptionWithIndependentFailures();
        SdpSessionDescription offer = DescriptionWithIndependentFailures();
        SdpValidationOptions options = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.CombinedStrict,
            NegotiationRole = SdpNegotiationRole.Answerer,
            OfferDescription = offer,
            MaximumDiagnosticCount = 64
        };
        options.SuppressedDiagnosticCodes.Add("UNUSED_SUPPRESSION");
        options.PromotedDiagnosticCodes.Add("UNUSED_PROMOTION");
        string descriptionBefore = Snapshot(description);
        string offerBefore = Snapshot(offer);
        string optionsBefore = Snapshot(options);

        new SdpValidator().ValidateDescription(description, options);

        Assert.Multiple(() =>
        {
            Assert.That(Snapshot(description), Is.EqualTo(descriptionBefore));
            Assert.That(Snapshot(offer), Is.EqualTo(offerBefore));
            Assert.That(Snapshot(options), Is.EqualTo(optionsBefore));
        });
    }

    [Test]
    public void Utf8ParsedValidationIssues_RetainPreciseAttributePathsAndLocations()
    {
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=R\u00e9sum\u00e9\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "t=0 0\r\n" +
            "m=audio 5000 RTP/AVP 0\r\n" +
            "a=mid:audio\r\n" +
            "a=candidate:malformed\r\n";
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        SdpParseResult parsed = new SdpParser().ParseUtf8Bytes(bytes, new SdpParseOptions());
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Description, Is.Not.Null);

        SdpValidationResult result = new SdpValidator().ValidateDescription(parsed.Description!, new SdpValidationOptions { Profile = SdpValidationProfile.Ice });
        SdpValidationIssue issue = result.Issues.Single(item => item.AttributeName == SdpAttributeNames.Candidate && item.Path.OccurrenceIndex == 0);

        Assert.Multiple(() =>
        {
            Assert.That(issue.Code, Is.EqualTo(SdpDiagnosticCodes.ValidationIce));
            Assert.That(issue.Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(issue.MediaIndex, Is.Zero);
            Assert.That(issue.FieldName, Is.EqualTo('a'));
            Assert.That(issue.Path.Scope, Is.EqualTo(issue.Scope));
            Assert.That(issue.Path.MediaIndex, Is.EqualTo(issue.MediaIndex));
            Assert.That(issue.Path.MediaId, Is.EqualTo("audio"));
            Assert.That(issue.Path.FieldName, Is.EqualTo(issue.FieldName));
            Assert.That(issue.Path.AttributeName, Is.EqualTo(issue.AttributeName));
            Assert.That(issue.Location.LineNumber, Is.EqualTo(8));
            Assert.That(issue.Location.ColumnNumber, Is.EqualTo(1));
            Assert.That(issue.Location.CharacterOffset, Is.GreaterThan(0));
            Assert.That(issue.Location.ByteOffset, Is.GreaterThan(issue.Location.CharacterOffset));
        });
    }

    [Test]
    public void CombinedValidation_DeduplicatesRootsAndRedactsSensitiveAttributeValues()
    {
        string password = "short-password-secret";
        string fingerprint = "DE:AD:BE:EF:PRIVATE-FINGERPRINT";
        string keyMaterial = "private-inline-key-material";
        SdpSessionDescription description = DescriptionWithIndependentFailures();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.PacketTime, "20"));
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, password));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Fingerprint, "sha-256 " + fingerprint));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Crypto, "invalid inline:" + keyMaterial));

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.CombinedStrict });
        List<SdpValidationIssue> duplicateRoots = result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope && issue.AttributeName == SdpAttributeNames.PacketTime).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(duplicateRoots, Has.Count.EqualTo(1));
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.IcePassword), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.Fingerprint), Is.True);
            Assert.That(result.Issues.All(issue => !issue.Detail.Contains(password, StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.All(issue => !issue.Detail.Contains(fingerprint, StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.All(issue => !issue.Detail.Contains(keyMaterial, StringComparison.Ordinal)), Is.True);
        });
    }

    static SdpSessionDescription DescriptionWithIndependentFailures()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE missing"));
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Audio,
            MediaKind = SdpMediaKind.Audio,
            Port = 5000,
            Protocol = SdpTransportProtocols.RtpAvp
        };
        media.Formats.Add("96");
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "audio"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Candidate, "malformed"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Setup, "invalid-role"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.SctpPort, "invalid"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorControl, "invalid"));
        description.MediaDescriptions.Add(media);
        return description;
    }

    static SdpSessionDescription ValidCoreDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription
        {
            HasProtocolVersion = true,
            ProtocolVersion = 0,
            Origin = new SdpOrigin
            {
                Username = "-",
                SessionId = "1",
                SessionVersion = "1",
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                UnicastAddress = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            HasSessionName = true,
            SessionName = "Validation",
            Connection = new SdpConnectionData
            {
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                Address = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            }
        };
        description.Timings.Add(new SdpTiming { StartTime = "0", StopTime = "0" });
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Audio,
            MediaKind = SdpMediaKind.Audio,
            Port = 9,
            Protocol = SdpTransportProtocols.RtpAvp
        };
        media.Formats.Add("0");
        description.MediaDescriptions.Add(media);
        return description;
    }

    static SdpValidationOptions CoreOptions()
    {
        return new SdpValidationOptions { Profile = SdpValidationProfile.Rfc8866 };
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

    static string Snapshot(object value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions { IncludeFields = true });
    }
}
