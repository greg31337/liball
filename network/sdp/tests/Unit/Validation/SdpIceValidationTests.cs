namespace sdp.Tests.Unit.Validation;

public class SdpIceValidationTests
{
    [Test]
    public void ValidIceDescription_UsesSessionCredentialsAndAcceptsRepeatedCandidates()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        description.Attributes.Add(PropertyAttribute("ice-lite"));
        AddSessionCredentials(description);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "1 1 UDP 2122260223 192.0.2.1 5000 typ host"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "2 2 TCP 2122260222 2001:db8::1 5001 typ host tcptype passive"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("remote-candidates", "1 192.0.2.2 6000 2 2001:db8::2 6001"));
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute("end-of-candidates"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.WarningCount, Is.Zero);
            Assert.That(result.ErrorCount, Is.Zero);
        });
    }

    [Test]
    public void Credentials_AreInheritedIndependentlyAndMayBeOverriddenAtMediaScope()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        description.Attributes.Add(ValueAttribute("ice-ufrag", "sess"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "1 1 UDP 1 192.0.2.1 5000 typ host"));

        SdpValidationResult mixed = ValidateIce(description);
        description.Attributes.Add(ValueAttribute("ice-pwd", "zyxwvutsrqponmlkjihgfe"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("ice-ufrag", "over"));
        SdpValidationResult overridden = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(mixed.Success, Is.True);
            Assert.That(mixed.Issues, Is.Empty);
            Assert.That(overridden.Success, Is.True);
            Assert.That(overridden.Issues, Is.Empty);
        });
    }

    [Test]
    public void ReusedUsernameFragments_RequireMatchingEffectivePasswords()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        description.MediaDescriptions.Add(new SdpMediaDescription { MediaType = "video", Port = 5002, Protocol = "RTP/AVP" });
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("ice-ufrag", "same"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));
        description.MediaDescriptions[1].Attributes.Add(ValueAttribute("ice-ufrag", "same"));
        description.MediaDescriptions[1].Attributes.Add(ValueAttribute("ice-pwd", "zyxwvutsrqponmlkjihgfe"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].AttributeName, Is.EqualTo("ice-pwd"));
            Assert.That(result.Issues[0].MediaIndex, Is.EqualTo(1));
            Assert.That(result.Issues[0].Detail, Does.Not.Contain("abcdefghijklmnopqrstuv"));
            Assert.That(result.Issues[0].Detail, Does.Not.Contain("zyxwvutsrqponmlkjihgfe"));
        });
    }

    [Test]
    public void MissingEffectiveCredentials_AreReportedPerMediaWithoutSensitiveData()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "1 1 UDP 1 192.0.2.1 5000 typ host"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.Select(x => x.AttributeName), Is.EqualTo(new[] { "ice-ufrag", "ice-pwd" }));
            Assert.That(result.Issues.All(x => x.Scope == SdpScope.Media && x.MediaIndex == 0), Is.True);
        });
    }

    [Test]
    public void IceAttributes_EnforceRegisteredScopes()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        AddSessionCredentials(description);
        description.Attributes.Add(ValueAttribute("candidate", "1 1 UDP 1 192.0.2.1 5000 typ host"));
        description.Attributes.Add(ValueAttribute("remote-candidates", "1 192.0.2.1 5000"));
        description.Attributes.Add(PropertyAttribute("ice-mismatch"));
        description.Attributes.Add(PropertyAttribute("end-of-candidates"));
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute("ice-lite"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationAttributeScope), Is.EqualTo(5));
            Assert.That(result.Issues.Any(x => x.Scope == SdpScope.Session && x.MediaIndex == -1), Is.True);
            Assert.That(result.Issues.Any(x => x.Scope == SdpScope.Media && x.MediaIndex == 0), Is.True);
        });
    }

    [Test]
    public void SingletonIceAttributes_RejectDuplicatesWhileCandidatesRemainRepeatable()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        AddSessionCredentials(description);
        description.Attributes.Add(PropertyAttribute("ice-lite"));
        description.Attributes.Add(PropertyAttribute("ICE-LITE"));
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute("ice-mismatch"));
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute("Ice-Mismatch"));
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute("end-of-candidates"));
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute("END-OF-CANDIDATES"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "1 1 UDP 1 192.0.2.1 5000 typ host"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "2 1 UDP 2 192.0.2.2 5001 typ host"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(3));
            Assert.That(result.Issues.Where(x => x.Code == SdpDiagnosticCodes.ValidationAttributeCardinality).Select(x => x.Path.OccurrenceIndex), Is.EqualTo(new[] { 1, 1, 1 }));
            Assert.That(result.Issues.Any(x => x.AttributeName == "candidate" && x.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.False);
        });
    }

    [Test]
    public void CredentialSyntaxAndLengths_AreValidatedWithoutEchoingPasswords()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        string secret = "private-password-value!";
        description.Attributes.Add(ValueAttribute("ice-ufrag", "bad-"));
        description.Attributes.Add(ValueAttribute("ice-pwd", secret));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Issues.Count(x => x.Code == SdpDiagnosticCodes.ValidationIce), Is.EqualTo(2));
            Assert.That(result.Issues.All(x => !x.Detail.Contains(secret, StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(x => x.AttributeName == "ice-ufrag"), Is.True);
            Assert.That(result.Issues.Any(x => x.AttributeName == "ice-pwd"), Is.True);
        });
    }

    [Test]
    public void CandidateNumericBounds_AreValidatedAfterSuccessfulProjection()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        AddSessionCredentials(description);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "zero 0 UDP 0 192.0.2.1 0 typ host"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "high 257 UDP 2147483648 192.0.2.2 5000 typ host"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(5));
            Assert.That(result.Issues.All(x => x.Code == SdpDiagnosticCodes.ValidationIce), Is.True);
            Assert.That(result.Issues.Count(x => x.Detail.Contains("component", StringComparison.Ordinal)), Is.EqualTo(2));
            Assert.That(result.Issues.Count(x => x.Detail.Contains("priority", StringComparison.Ordinal)), Is.EqualTo(2));
            Assert.That(result.Issues.Count(x => x.Detail.Contains("port", StringComparison.Ordinal)), Is.EqualTo(1));
        });
    }

    [Test]
    public void TcpRequirements_AreValidatedForTransportAndConnectionType()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        AddSessionCredentials(description);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "one 1 TCP 1 192.0.2.1 5000 typ host"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "two 1 TCP 2 192.0.2.2 5001 typ host tcptype extension"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "three 1 TCP 3 192.0.2.3 5002 typ host tcptype active"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "four 1 UDP 4 192.0.2.4 5003 typ host tcptype passive"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(4));
            Assert.That(result.Issues.Count(x => x.Detail.Contains("TCP", StringComparison.Ordinal)), Is.EqualTo(4));
        });
    }

    [Test]
    public void RelatedEndpoints_EnforceCandidateTypePresenceFamilyAndPortRules()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        AddSessionCredentials(description);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "host 1 UDP 1 192.0.2.1 5000 typ host raddr 192.0.2.2 rport 5001"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "srflx 1 UDP 2 192.0.2.3 5002 typ srflx"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "relay 1 UDP 3 192.0.2.4 5003 typ relay raddr 2001:db8::1 rport 5004"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "prflx 1 UDP 4 192.0.2.5 5005 typ prflx raddr 192.0.2.6 rport 0"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(4));
            Assert.That(result.Issues.Any(x => x.Detail.Contains("must not", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(x => x.Detail.Contains("requires both", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(x => x.Detail.Contains("same IP", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(x => x.Detail.Contains("related port", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void CandidateAddresses_RejectInvalidAndMulticastButRetainHostnameWarnings()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        AddSessionCredentials(description);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "one 1 UDP 1 233.252.0.1 5000 typ host"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "two 1 UDP 2 999.1.1.1 5001 typ host"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "three 1 UDP 3 peer.local 5002 typ host"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.WarningCount, Is.EqualTo(1));
            Assert.That(result.Issues.Single(x => x.Severity == SdpValidationSeverity.Warning).Detail, Does.Contain("extension support"));
        });
    }

    [Test]
    public void RemoteCandidates_ValidateBoundsAddressesAndCodecFailures()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        AddSessionCredentials(description);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("remote-candidates", "0 peer.local 0 257 233.252.0.1 6000"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("remote-candidates", "1 192.0.2.1 6000 1 192.0.2.2 6001"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(6));
            Assert.That(result.WarningCount, Is.EqualTo(1));
            Assert.That(result.Issues.Any(x => x.Detail.Contains("repeat", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void SeverityPolicy_SuppressesFiltersAndPromotesHostnameWarnings()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        AddSessionCredentials(description);
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "one 1 UDP 1 peer.local 5000 typ host"));
        ISdpValidator validator = new SdpValidator();

        SdpValidationOptions filteredOptions = IceOptions();
        filteredOptions.MinimumSeverity = SdpValidationSeverity.Error;
        SdpValidationResult filtered = validator.ValidateDescription(description, filteredOptions);
        SdpValidationOptions promotedOptions = IceOptions();
        promotedOptions.PromotedDiagnosticCodes.Add(SdpDiagnosticCodes.ValidationIce);
        SdpValidationResult promoted = validator.ValidateDescription(description, promotedOptions);
        SdpValidationOptions suppressedOptions = IceOptions();
        suppressedOptions.SuppressedDiagnosticCodes.Add(SdpDiagnosticCodes.ValidationIce);
        SdpValidationResult suppressed = validator.ValidateDescription(description, suppressedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(filtered.Success, Is.True);
            Assert.That(filtered.Issues, Is.Empty);
            Assert.That(promoted.Success, Is.False);
            Assert.That(promoted.ErrorCount, Is.EqualTo(1));
            Assert.That(promoted.Issues[0].Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(suppressed.Success, Is.True);
            Assert.That(suppressed.Issues, Is.Empty);
        });
    }

    [Test]
    public void DiagnosticLimit_IsBoundedAndEndsWithTerminalIssue()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "one 0 UDP 0 233.252.0.1 0 typ host"));
        SdpValidationOptions options = IceOptions();
        options.MaximumDiagnosticCount = 2;

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.DiagnosticLimitReached, Is.True);
            Assert.That(result.Issues, Has.Count.EqualTo(2));
            Assert.That(result.Issues[1].Code, Is.EqualTo(SdpDiagnosticCodes.DiagnosticLimitReached));
            Assert.That(result.Issues[1].Severity, Is.EqualTo(SdpValidationSeverity.Fatal));
            Assert.That(result.ErrorCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void DefaultDestinations_MustMatchComponentCandidatesIncludingExplicitRtcp()
    {
        SdpSessionDescription description = CreateCompleteRtpDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("rtcp", "6000 IN IP4 192.0.2.2"));
        media.Attributes.Add(ValueAttribute("candidate", "rtp 1 UDP 2 192.0.2.1 5000 typ host"));
        SdpAttribute rtcpCandidate = ValueAttribute("candidate", "rtcp 2 UDP 1 192.0.2.2 6000 typ host");
        media.Attributes.Add(rtcpCandidate);

        SdpValidationResult matching = ValidateIce(description);
        rtcpCandidate.Value = "rtcp 2 UDP 1 192.0.2.2 6001 typ host";
        SdpValidationResult mismatching = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(matching.Success, Is.True);
            Assert.That(matching.Issues, Is.Empty);
            Assert.That(mismatching.ErrorCount, Is.EqualTo(1));
            Assert.That(mismatching.Issues[0].Detail, Does.Contain("default RTCP destination"));
        });
    }

    [Test]
    public void PlaceholderAndIceMismatch_DefaultsPermitOmittedCandidates()
    {
        SdpSessionDescription placeholder = CreateCompleteRtpDescription();
        placeholder.Connection!.Address = "0.0.0.0";
        placeholder.MediaDescriptions[0].Port = 9;
        SdpValidationResult placeholderResult = ValidateIce(placeholder);

        SdpSessionDescription mismatch = CreateCompleteRtpDescription();
        mismatch.MediaDescriptions[0].Attributes.Add(PropertyAttribute("ice-mismatch"));
        SdpValidationResult omitted = ValidateIce(mismatch);
        mismatch.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "one 1 UDP 1 192.0.2.1 5000 typ host"));
        SdpValidationResult supplied = ValidateIce(mismatch);

        Assert.Multiple(() =>
        {
            Assert.That(placeholderResult.Success, Is.True);
            Assert.That(omitted.Success, Is.True);
            Assert.That(supplied.ErrorCount, Is.EqualTo(1));
            Assert.That(supplied.Issues[0].AttributeName, Is.EqualTo("ice-mismatch"));
        });
    }

    [Test]
    public void ComponentCoverage_AccountsForRtcpMultiplexing()
    {
        SdpSessionDescription description = CreateCompleteRtpDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(ValueAttribute("candidate", "one 1 UDP 1 192.0.2.1 5000 typ host"));

        SdpValidationResult separate = ValidateIce(description);
        media.Attributes.Add(PropertyAttribute("rtcp-mux"));
        SdpValidationResult multiplexed = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(separate.ErrorCount, Is.EqualTo(1));
            Assert.That(separate.Issues[0].Detail, Does.Contain("component 2"));
            Assert.That(multiplexed.Success, Is.True);
            Assert.That(multiplexed.Issues, Is.Empty);
        });
    }

    [Test]
    public void RtcpMuxOnly_RequiresMuxAndForbidsComponentTwo()
    {
        SdpSessionDescription description = CreateCompleteRtpDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Attributes.Add(PropertyAttribute("rtcp-mux-only"));
        media.Attributes.Add(ValueAttribute("candidate", "one 1 UDP 2 192.0.2.1 5000 typ host"));
        media.Attributes.Add(ValueAttribute("candidate", "two 2 UDP 1 192.0.2.1 5001 typ host"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.Any(x => x.Detail.Contains("requires the RTCP-mux", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(x => x.Detail.Contains("must not contain a component 2", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void TrickleAtMediaScope_MustCoverEveryIceMediaDescription()
    {
        SdpSessionDescription description = CreateCompleteRtpDescription();
        SdpMediaDescription first = description.MediaDescriptions[0];
        first.Protocol = "UDP";
        first.Attributes.Add(ValueAttribute("ice-options", "trickle"));
        first.Attributes.Add(ValueAttribute("candidate", "one 1 UDP 1 192.0.2.1 5000 typ host"));
        SdpMediaDescription second = new SdpMediaDescription { MediaType = "application", Port = 5002, Protocol = "UDP" };
        second.Attributes.Add(ValueAttribute("candidate", "two 1 UDP 1 192.0.2.1 5002 typ host"));
        description.MediaDescriptions.Add(second);

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].AttributeName, Is.EqualTo("ice-options"));
            Assert.That(result.Issues[0].MediaIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void EndOfCandidates_RequiresTrickleAndMustBeLastCandidateMarker()
    {
        SdpSessionDescription description = CreateCompleteRtpDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Protocol = "UDP";
        media.Attributes.Add(PropertyAttribute("end-of-candidates"));
        media.Attributes.Add(ValueAttribute("candidate", "one 1 UDP 1 192.0.2.1 5000 typ host"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.Any(x => x.Detail.Contains("requires the effective trickle", StringComparison.Ordinal)), Is.True);
            Assert.That(result.Issues.Any(x => x.Detail.Contains("must not appear after", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void TrickleAllowsPartialGatheringUntilCompletionButIceLiteDoesNot()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        AddSessionCredentials(description);
        SdpValidationResult gathering = ValidateIce(description);
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute("end-of-candidates"));
        SdpValidationResult complete = ValidateIce(description);
        description.MediaDescriptions[0].Attributes.RemoveAt(description.MediaDescriptions[0].Attributes.Count - 1);
        description.Attributes.Add(PropertyAttribute("ice-lite"));
        SdpValidationResult lite = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(gathering.Success, Is.True);
            Assert.That(complete.ErrorCount, Is.EqualTo(1));
            Assert.That(complete.Issues[0].Detail, Does.Contain("component 1"));
            Assert.That(lite.ErrorCount, Is.EqualTo(1));
            Assert.That(lite.Issues[0].Detail, Does.Contain("component 1"));
        });
    }

    [Test]
    public void IceLite_AllowsOnlyHostCandidates()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        AddSessionCredentials(description);
        description.Attributes.Add(PropertyAttribute("ice-lite"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute("candidate", "one 1 UDP 1 192.0.2.1 5000 typ srflx raddr 10.0.0.1 rport 4000"));

        SdpValidationResult result = ValidateIce(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Detail, Does.Contain("only host candidates"));
        });
    }

    [Test]
    public void NonIceProfiles_DoNotApplyIceSemanticRules()
    {
        SdpSessionDescription description = CreateDescriptionWithMedia();
        description.Attributes.Add(ValueAttribute("ice-pwd", "secret!"));
        description.Attributes.Add(ValueAttribute("candidate", "malformed"));
        SdpValidationOptions options = new SdpValidationOptions { Profile = SdpValidationProfile.Rfc8866 };

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, options);

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationIce), Is.False);
    }

    static SdpSessionDescription CreateDescriptionWithMedia()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription { MediaType = "audio", Port = 5000, Protocol = "RTP/AVP" };
        description.Attributes.Add(ValueAttribute("ice-options", "trickle"));
        media.Attributes.Add(PropertyAttribute("rtcp-mux"));
        description.MediaDescriptions.Add(media);
        return description;
    }

    static SdpSessionDescription CreateCompleteRtpDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Connection = new SdpConnectionData { NetworkType = "IN", AddressType = "IP4", Address = "192.0.2.1", AddressKind = SdpAddressKind.Ipv4 };
        description.MediaDescriptions.Add(new SdpMediaDescription { MediaType = "audio", MediaKind = SdpMediaKind.Audio, Port = 5000, Protocol = "RTP/AVP" });
        AddSessionCredentials(description);
        return description;
    }

    static void AddSessionCredentials(SdpSessionDescription description)
    {
        description.Attributes.Add(ValueAttribute("ice-ufrag", "8hhY"));
        description.Attributes.Add(ValueAttribute("ice-pwd", "asd88fgpdd777uzjYhagZg"));
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Property, Name = name, RawText = name };
    }

    static SdpValidationOptions IceOptions()
    {
        return new SdpValidationOptions { Profile = SdpValidationProfile.Ice };
    }

    static SdpValidationResult ValidateIce(SdpSessionDescription description)
    {
        return new SdpValidator().ValidateDescription(description, IceOptions());
    }
}
