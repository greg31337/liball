namespace sip.Tests;

public class SipCapabilityNegotiatorTests
{
    [Test]
    public void InspectsAllCapabilityFamiliesAndPreservesExtensionValues()
    {
        SipMessage message = ParseMessage(RequestText(
            "Allow: INVITE, X-CUSTOM\r\n"
            + "Supported: timer, vendor-option\r\n"
            + "Require: timer\r\n"
            + "Proxy-Require: path\r\n"
            + "Unsupported: old-option\r\n"
            + "Accept: application/sdp, application/vnd.example+json\r\n"
            + "u: presence, vendor.event\r\n"
            + "a: *;audio;methods=INVITE;require\r\n"
            + "j: *;+vendor.unwanted=TRUE\r\n"
            + "d: proxy, x-route\r\n"
            + "Feature-Caps: *;+sip.pns=apns;+vendor.future=enabled\r\n"));

        SipCapabilityNegotiation result =
            new SipCapabilityNegotiator().InspectCapabilityMessage(message);
        result.FeatureCapabilities[0].Parameters[1].Name = "changed";
        SipCapabilityNegotiation repeated =
            new SipCapabilityNegotiator().InspectCapabilityMessage(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Compatible, Is.True, result.ErrorMessage);
            Assert.That(result.RemoteCapabilities!.Methods,
                Is.EqualTo(new string[] { "INVITE", "X-CUSTOM" }));
            Assert.That(result.RemoteCapabilities.OptionTags,
                Is.EqualTo(new string[] { "timer", "vendor-option" }));
            Assert.That(result.RemoteCapabilities.MediaTypes,
                Is.EqualTo(new string[] {
                    "application/sdp", "application/vnd.example+json" }));
            Assert.That(result.RemoteCapabilities.EventPackages,
                Is.EqualTo(new string[] { "presence", "vendor.event" }));
            Assert.That(result.RequiredOptionTags,
                Is.EqualTo(new string[] { "timer" }));
            Assert.That(result.ProxyRequiredOptionTags,
                Is.EqualTo(new string[] { "path" }));
            Assert.That(result.UnsupportedOptionTags,
                Is.EqualTo(new string[] { "old-option" }));
            Assert.That(result.CallerPreferences!.AcceptContacts, Has.Length.EqualTo(1));
            Assert.That(result.CallerPreferences.RejectContacts, Has.Length.EqualTo(1));
            Assert.That(result.CallerPreferences.RequestDispositions,
                Is.EqualTo(new string[] { "proxy", "x-route" }));
            Assert.That(result.FeatureCapabilities[0].OriginalText,
                Does.Contain("+vendor.future"));
            Assert.That(repeated.FeatureCapabilities[0].Parameters[1].Name,
                Is.EqualTo("+vendor.future"));
        });
    }

    [Test]
    public void NegotiatesMutualCapabilitiesIncludingMediaWildcards()
    {
        SipMessage request = ParseMessage(RequestText(
            "Allow: INVITE, BYE\r\n"
            + "Supported: timer, 100rel, remote-only\r\n"
            + "Require: timer\r\n"
            + "Accept: application/*, text/plain\r\n"
            + "Allow-Events: presence, dialog\r\n"
            + "Event: presence\r\n"));
        SipCapabilitySet local = Capabilities(
            new string[] { "INVITE", "OPTIONS" },
            new string[] { "timer", "100rel" },
            new string[] { "application/sdp", "application/pidf+xml" },
            new string[] { "presence" });

        SipCapabilityNegotiation result =
            new SipCapabilityNegotiator().NegotiateCapabilityMessage(
                request, local);

        Assert.Multiple(() =>
        {
            Assert.That(result.Compatible, Is.True, result.ErrorMessage);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.Ok));
            Assert.That(result.NegotiatedCapabilities!.Methods,
                Is.EqualTo(new string[] { "INVITE" }));
            Assert.That(result.NegotiatedCapabilities.OptionTags,
                Is.EqualTo(new string[] { "timer", "100rel" }));
            Assert.That(result.NegotiatedCapabilities.MediaTypes,
                Is.EqualTo(new string[] {
                    "application/sdp", "application/pidf+xml" }));
            Assert.That(result.NegotiatedCapabilities.EventPackages,
                Is.EqualTo(new string[] { "presence" }));
        });
    }

    [Test]
    public void NegotiationReportsUnsupportedMethodBeforeOtherChecks()
    {
        SipMessage request = ParseMessage(RequestText("Require: future\r\n"));
        SipCapabilitySet local = Capabilities(
            new string[] { "OPTIONS" }, new string[0],
            new string[0], new string[0]);

        SipCapabilityNegotiation result =
            new SipCapabilityNegotiator().NegotiateCapabilityMessage(
                request, local);

        Assert.That(result.Compatible, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.MethodNotAllowed));
    }

    [Test]
    public void NegotiationReportsEveryMissingRequiredOption()
    {
        SipMessage request = ParseMessage(RequestText(
            "Require: timer, future-one\r\nRequire: FUTURE-TWO\r\n"));
        SipCapabilitySet local = Capabilities(
            new string[] { "INVITE" }, new string[] { "timer" },
            new string[0], new string[0]);

        SipCapabilityNegotiation result =
            new SipCapabilityNegotiator().NegotiateCapabilityMessage(
                request, local);

        Assert.Multiple(() =>
        {
            Assert.That(result.Compatible, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.BadExtension));
            Assert.That(result.UnsupportedOptionTags,
                Is.EqualTo(new string[] { "future-one", "FUTURE-TWO" }));
        });
    }

    [Test]
    public void NegotiationRejectsUnsupportedRequiredBodyMediaType()
    {
        SipMessage request = ParseMessage(RequestTextWithBody(
            "Content-Type: application/json\r\n", "{}"));
        SipCapabilitySet local = Capabilities(
            new string[] { "INVITE" }, new string[0],
            new string[] { "application/sdp" }, new string[0]);

        SipCapabilityNegotiation result =
            new SipCapabilityNegotiator().NegotiateCapabilityMessage(
                request, local);

        Assert.That(result.Compatible, Is.False);
        Assert.That(result.StatusCode,
            Is.EqualTo(SipStatusCodes.UnsupportedMediaType));
    }

    [Test]
    public void NegotiationRejectsUnsupportedEventPackage()
    {
        SipMessage request = ParseMessage(RequestText("Event: dialog\r\n"));
        SipCapabilitySet local = Capabilities(
            new string[] { "INVITE" }, new string[0],
            new string[0], new string[] { "presence" });

        SipCapabilityNegotiation result =
            new SipCapabilityNegotiator().NegotiateCapabilityMessage(
                request, local);

        Assert.That(result.Compatible, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.BadEvent));
    }

    [Test]
    public void NegotiatesRequiredOptionsOnResponses()
    {
        SipMessage response = ParseMessage(
            "SIP/2.0 180 Ringing\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-cap\r\n"
            + "From: <sip:alice@example.com>;tag=from\r\n"
            + "To: <sip:bob@example.com>;tag=to\r\n"
            + "Call-ID: capability@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Supported: timer\r\n"
            + "Require: timer, response-feature\r\n"
            + "Content-Length: 0\r\n\r\n");
        SipCapabilitySet local = Capabilities(
            new string[0], new string[] { "timer" },
            new string[0], new string[0]);

        SipCapabilityNegotiation result =
            new SipCapabilityNegotiator().NegotiateCapabilityMessage(
                response, local);

        Assert.That(result.Compatible, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.BadExtension));
        Assert.That(result.UnsupportedOptionTags,
            Is.EqualTo(new string[] { "response-feature" }));
    }

    [Test]
    public void AddsNormalizedAdvertisementsAndPrependsFeatureCaps()
    {
        SipMessage request = ParseMessage(RequestText(
            "Feature-Caps: *;+existing=value\r\n"));
        SipCapabilitySet capabilities = Capabilities(
            new string[] { "INVITE", "INVITE", "bad method" },
            new string[] { "timer", "TIMER", "bad option" },
            new string[] { "Application/SDP", "bad" },
            new string[] { "presence", "PRESENCE" });
        SipExtensionHeaderValue[] featureCaps = ParseHeader(
            "Feature-Caps: *;+sip.pns=apns;+vendor.future=enabled")
            .ExtensionValues;

        SipCapabilityMessageResult result =
            new SipCapabilityNegotiator().AddCapabilityAdvertisement(
                request, capabilities, featureCaps);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(HeaderValues(result.Message!, SipHeaderNames.Allow),
                Is.EqualTo(new string[] { "INVITE" }));
            Assert.That(HeaderValues(result.Message!, SipHeaderNames.Supported),
                Is.EqualTo(new string[] { "timer" }));
            Assert.That(HeaderValues(result.Message!, SipHeaderNames.Accept),
                Is.EqualTo(new string[] { "Application/SDP" }));
            Assert.That(HeaderValues(result.Message!, SipHeaderNames.AllowEvents),
                Is.EqualTo(new string[] { "presence" }));
            Assert.That(HeaderValues(result.Message!, SipHeaderNames.FeatureCaps),
                Is.EqualTo(new string[] {
                    "*;+sip.pns=apns;+vendor.future=enabled",
                    "*;+existing=value" }));
            Assert.That(HeaderValues(request, SipHeaderNames.Allow), Is.Empty);
            Assert.That(request.OriginalBytes, Is.Not.Empty);
            Assert.That(result.Message!.OriginalBytes, Is.Empty);
        });
    }

    [Test]
    public void AddsCallerPreferencesAndRetainsUnknownDirectivesAndTags()
    {
        SipMessage request = ParseMessage(RequestText("X-Trace: keep\r\n"));
        SipCallerPreferences preferences = new SipCallerPreferences();
        preferences.AcceptContacts = ParseHeader(
            "Accept-Contact: *;audio;+vendor.mode=fast;require")
            .ExtensionValues;
        preferences.RejectContacts = ParseHeader(
            "Reject-Contact: *;actor=msg-taker")
            .ExtensionValues;
        preferences.RequestDispositions =
            new string[] { "proxy", "x-route" };

        SipCapabilityMessageResult result =
            new SipCapabilityNegotiator().AddCallerPreferenceHeaders(
                request, preferences);
        SipCapabilityNegotiation inspected =
            new SipCapabilityNegotiator().InspectCapabilityMessage(
                result.Message!);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(inspected.CallerPreferences!.AcceptContacts[0]
                .Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new string[] {
                    "audio", "+vendor.mode", "require" }));
            Assert.That(inspected.CallerPreferences.RequestDispositions,
                Is.EqualTo(new string[] { "proxy", "x-route" }));
            Assert.That(HeaderValues(request, SipHeaderNames.AcceptContact),
                Is.Empty);
            Assert.That(HeaderValues(result.Message!, "X-Trace"),
                Is.EqualTo(new string[] { "keep" }));
        });
    }

    [Test]
    public void RejectsInvalidCallerPreferenceModelsWithoutMutatingMessage()
    {
        SipMessage request = ParseMessage(RequestText("X-Trace: keep\r\n"));
        SipCallerPreferences preferences = new SipCallerPreferences();
        SipExtensionHeaderValue invalid = new SipExtensionHeaderValue();
        invalid.Token = "not-a-star";
        preferences.AcceptContacts =
            new SipExtensionHeaderValue[] { invalid };

        SipCapabilityMessageResult result =
            new SipCapabilityNegotiator().AddCallerPreferenceHeaders(
                request, preferences);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Is.Null);
        Assert.That(HeaderValues(request, SipHeaderNames.AcceptContact),
            Is.Empty);
    }

    [Test]
    public void MatchesCallerPreferencesUsingRfc3841Scoring()
    {
        SipMessage request = ParseMessage(RequestText(
            "Reject-Contact: *;actor=msg-taker;video\r\n"
            + "Accept-Contact: *;audio;require\r\n"
            + "Accept-Contact: *;video;explicit\r\n"
            + "Accept-Contact: *;methods=BYE;class=business;q=1.0\r\n"));
        SipContactValue u1 = ParseContact(
            "Contact: <sip:u1@h.example.com>;audio;video;methods=\"INVITE,BYE\";q=0.2");
        SipContactValue u4 = ParseContact(
            "Contact: <sip:u4@h.example.com>;audio;methods=\"INVITE,OPTIONS\";q=0.2");

        SipCallerPreferenceMatch first =
            new SipCapabilityNegotiator().MatchCallerPreferences(request, u1);
        SipCallerPreferenceMatch fourth =
            new SipCapabilityNegotiator().MatchCallerPreferences(request, u4);

        Assert.Multiple(() =>
        {
            Assert.That(first.Eligible, Is.True, first.ErrorMessage);
            Assert.That(first.ScoreThousandths, Is.EqualTo(833));
            Assert.That(first.MatchedAcceptContacts, Has.Length.EqualTo(3));
            Assert.That(fourth.Eligible, Is.True, fourth.ErrorMessage);
            Assert.That(fourth.ScoreThousandths, Is.EqualTo(500));
            Assert.That(fourth.MatchedAcceptContacts, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void CallerPreferenceMatchingRejectsAndRequiresAsSpecified()
    {
        SipMessage request = ParseMessage(RequestText(
            "Reject-Contact: *;actor=msg-taker;video\r\n"
            + "Accept-Contact: *;audio;require\r\n"));
        SipContactValue rejected = ParseContact(
            "Contact: <sip:u3@h.example.com>;audio;actor=msg-taker;video;q=0.3");
        SipContactValue contradicted = ParseContact(
            "Contact: <sip:u2@h.example.com>;audio=FALSE;methods=INVITE;q=0.2");
        SipContactValue immune = ParseContact(
            "Contact: <sip:u5@h.example.com>;q=0.5");

        SipCapabilityNegotiator negotiator = new SipCapabilityNegotiator();
        SipCallerPreferenceMatch rejectedResult =
            negotiator.MatchCallerPreferences(request, rejected);
        SipCallerPreferenceMatch contradictedResult =
            negotiator.MatchCallerPreferences(request, contradicted);
        SipCallerPreferenceMatch immuneResult =
            negotiator.MatchCallerPreferences(request, immune);

        Assert.Multiple(() =>
        {
            Assert.That(rejectedResult.Eligible, Is.False);
            Assert.That(rejectedResult.Rejected, Is.True);
            Assert.That(rejectedResult.MatchedRejectContacts,
                Has.Length.EqualTo(1));
            Assert.That(contradictedResult.Eligible, Is.False);
            Assert.That(contradictedResult.ErrorMessage,
                Does.Contain("required"));
            Assert.That(immuneResult.Eligible, Is.True);
            Assert.That(immuneResult.Immune, Is.True);
            Assert.That(immuneResult.ScoreThousandths, Is.EqualTo(1000));
        });
    }

    [Test]
    public void CallerPreferenceMatchingSupportsNumericRangesAndNegation()
    {
        SipMessage request = ParseMessage(RequestText(
            "Accept-Contact: *;+level=\"#>=3\";+mode=\"!silent\";require\r\n"));
        SipContactValue contact = ParseContact(
            "Contact: <sip:target@example.com>;+level=\"#2:5\";+mode=active");

        SipCallerPreferenceMatch result =
            new SipCapabilityNegotiator().MatchCallerPreferences(
                request, contact);

        Assert.That(result.Eligible, Is.True, result.ErrorMessage);
        Assert.That(result.ScoreThousandths, Is.EqualTo(1000));
    }

    [Test]
    public void CallerPreferencesPreserveAndIgnoreGenericParameters()
    {
        SipMessage request = ParseMessage(RequestText(
            "Accept-Contact: *;audio;x-vendor=keep;require\r\n"
            + "Reject-Contact: *;video;x-policy=observe\r\n"));
        SipCapabilityNegotiator negotiator = new SipCapabilityNegotiator();

        SipCapabilityNegotiation inspected =
            negotiator.InspectCapabilityMessage(request);
        SipCallerPreferenceMatch matched = negotiator.MatchCallerPreferences(
            request, ParseContact("Contact: <sip:u@example.com>;audio"));
        SipCapabilityMessageResult added = negotiator.AddCallerPreferenceHeaders(
            ParseMessage(RequestText("X-Trace: retained\r\n")),
            inspected.CallerPreferences!);

        Assert.Multiple(() =>
        {
            Assert.That(inspected.Compatible, Is.True, inspected.ErrorMessage);
            Assert.That(inspected.CallerPreferences!.AcceptContacts[0].Parameters
                .Select(parameter => parameter.Name),
                Is.EqualTo(new string[] { "audio", "x-vendor", "require" }));
            Assert.That(matched.Eligible, Is.True, matched.ErrorMessage);
            Assert.That(matched.ScoreThousandths, Is.EqualTo(1000));
            Assert.That(HeaderValues(added.Message!, SipHeaderNames.AcceptContact)[0],
                Does.Contain("x-vendor=keep"));
            Assert.That(HeaderValues(added.Message!, "X-Trace"),
                Is.EqualTo(new string[] { "retained" }));
        });
    }

    [TestCase("Feature-Caps: *;audio\r\n", "Feature-Caps")]
    [TestCase("Accept-Contact: *;audio;require=TRUE\r\n", "Accept-Contact")]
    [TestCase("Reject-Contact: *;video;explicit\r\n", "Reject-Contact")]
    public void InspectionRejectsMalformedCapabilityExtensions(
        string headers, string expectedName)
    {
        SipCapabilityNegotiation result = new SipCapabilityNegotiator()
            .InspectCapabilityMessage(ParseMessage(RequestText(headers)));

        Assert.Multiple(() =>
        {
            Assert.That(result.Compatible, Is.False);
            Assert.That(result.StatusCode, Is.EqualTo(SipStatusCodes.BadRequest));
            Assert.That(result.ErrorMessage, Does.Contain(expectedName));
        });
    }

    static SipCapabilitySet Capabilities(
        string[] methods, string[] options, string[] media, string[] events)
    {
        SipCapabilitySet result = new SipCapabilitySet();
        result.Methods = methods;
        result.OptionTags = options;
        result.MediaTypes = media;
        result.EventPackages = events;
        return result;
    }

    static SipContactValue ParseContact(string text)
    {
        SipHeaderParseResult parsed = ParseHeader(text);
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.ContactValues[0];
    }

    static SipHeaderParseResult ParseHeader(string text)
    {
        return new SipHeaderCodec().ParseHeader(
            System.Text.Encoding.UTF8.GetBytes(text),
            new SipParseOptions());
    }

    static SipMessage ParseMessage(string text)
    {
        SipParseResult parsed = new SipMessageCodec().ParseDatagramMessage(
            System.Text.Encoding.UTF8.GetBytes(text),
            new SipParseOptions());
        Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete),
            parsed.ErrorMessage);
        return parsed.Message!;
    }

    static string RequestText(string extraHeaders)
    {
        return "INVITE sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-cap\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: capability@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + extraHeaders
            + "Content-Length: 0\r\n\r\n";
    }

    static string RequestTextWithBody(string extraHeaders, string body)
    {
        return "INVITE sip:bob@example.com SIP/2.0\r\n"
            + "Via: SIP/2.0/UDP client.example.com;branch=z9hG4bK-cap\r\n"
            + "Max-Forwards: 70\r\n"
            + "From: <sip:alice@example.com>;tag=from\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "Call-ID: capability@example.com\r\n"
            + "CSeq: 1 INVITE\r\n"
            + "Contact: <sip:alice@client.example.com>\r\n"
            + extraHeaders
            + "Content-Length: "
            + System.Text.Encoding.UTF8.GetByteCount(body)
            + "\r\n\r\n" + body;
    }

    static string[] HeaderValues(SipMessage message, string name)
    {
        return message.Headers
            .Where(header => string.Equals(header.Name, name,
                System.StringComparison.OrdinalIgnoreCase))
            .Select(header => header.Value!)
            .ToArray();
    }
}
