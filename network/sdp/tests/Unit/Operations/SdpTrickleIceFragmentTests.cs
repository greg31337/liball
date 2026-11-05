namespace sdp.Tests.Unit.Operations;

public class SdpTrickleIceFragmentTests
{
    [Test]
    public void ParseTrickleIceFragment_ProjectsAssociatedMediaInSourceOrder()
    {
        string input =
            "a=ice-ufrag:sess\r\n" +
            "a=ice-pwd:abcdefghijklmnopqrstuv\r\n" +
            "a=ice-options:trickle\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "a=mid:audio\r\n" +
            "a=candidate:first 1 UDP 20 192.0.2.1 5000 typ host\r\n" +
            "a=candidate:second 1 UDP 10 192.0.2.2 5001 typ host generation 0\r\n" +
            "a=end-of-candidates\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "a=mid:video\r\n" +
            "a=candidate:third 1 UDP 30 2001:db8::1 6000 typ host\r\n";
        ISdpIceService service = new SdpIceService();

        SdpParseResult result = service.ParseTrickleIceFragment(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Description, Is.Not.Null);
            Assert.That(result.Description!.MediaDescriptions, Has.Count.EqualTo(2));
            Assert.That(result.Description.RawLines, Is.Empty);
            Assert.That(result.Description.MediaDescriptions[0].RawLines, Is.Empty);
            Assert.That(result.Description.MediaDescriptions[0].Attributes.Select(x => x.Name), Is.EqualTo(new[] { "mid", "candidate", "candidate", "end-of-candidates" }));
            Assert.That(service.ResolveMediaIce(result.Description, 0).Candidates.Select(x => x.Foundation), Is.EqualTo(new[] { "first", "second" }));
            Assert.That(service.ResolveMediaIce(result.Description, 0).EndOfCandidates, Is.True);
            Assert.That(service.ResolveMediaIce(result.Description, 1).Credentials!.UsernameFragment, Is.EqualTo("sess"));
            Assert.That(service.ResolveMediaIce(result.Description, 1).Candidates[0].Foundation, Is.EqualTo("third"));
        });
    }

    [Test]
    public void ParseTrickleIceFragment_AcceptsSessionCompletionWithoutPseudoMedia()
    {
        string input =
            "a=ice-ufrag:sess\r\n" +
            "a=ice-pwd:abcdefghijklmnopqrstuv\r\n" +
            "a=end-of-candidates\r\n";
        ISdpIceService service = new SdpIceService();

        SdpParseResult result = service.ParseTrickleIceFragment(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Description!.MediaDescriptions, Is.Empty);
            Assert.That(result.Description.Attributes[^1].Name, Is.EqualTo("end-of-candidates"));
        });
    }

    [TestCase("m=audio 9 RTP/AVP 0\r\na=candidate:a 1 UDP 1 192.0.2.1 5000 typ host\r\na=mid:audio\r\na=ice-ufrag:user\r\na=ice-pwd:abcdefghijklmnopqrstuv\r\n")]
    [TestCase("m=audio 9 RTP/AVP 0\r\na=mid:audio\r\na=ice-ufrag:user\r\na=ice-pwd:abcdefghijklmnopqrstuv\r\na=end-of-candidates\r\na=candidate:a 1 UDP 1 192.0.2.1 5000 typ host\r\n")]
    [TestCase("a=ice-ufrag:user\r\na=ice-pwd:abcdefghijklmnopqrstuv\r\na=end-of-candidates\r\nm=audio 9 RTP/AVP 0\r\na=mid:audio\r\na=candidate:a 1 UDP 1 192.0.2.1 5000 typ host\r\n")]
    public void ParseTrickleIceFragment_RejectsInvalidAssociationAndCompletionOrdering(string input)
    {
        ISdpIceService service = new SdpIceService();

        SdpParseResult result = service.ParseTrickleIceFragment(input, new SdpParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Any(x => x.Code == SdpDiagnosticCodes.ValidationIce), Is.True);
            Assert.That(result.Diagnostics.All(x => !x.Detail.Contains("abcdefghijklmnopqrstuv", StringComparison.Ordinal)), Is.True);
        });
    }

    [TestCase("v=0\r\na=ice-ufrag:user\r\na=ice-pwd:abcdefghijklmnopqrstuv\r\n")]
    [TestCase("m=video 9 RTP/AVP 0\r\na=mid:video\r\na=ice-ufrag:user\r\na=ice-pwd:abcdefghijklmnopqrstuv\r\n")]
    [TestCase("m=audio 9 RTP/AVP 0\r\na=mid:audio\r\na=ice-ufrag:user\r\n")]
    [TestCase("a=candidate:a 1 UDP 1 192.0.2.1 5000 typ host\r\na=ice-ufrag:user\r\na=ice-pwd:abcdefghijklmnopqrstuv\r\n")]
    [TestCase("a=ice-ufrag:user\r\na=ice-pwd:abcdefghijklmnopqrstuv\r\nm=audio 9 RTP/AVP 0\r\na=mid:same\r\nm=audio 9 RTP/AVP 0\r\na=mid:same\r\n")]
    public void ParseTrickleIceFragment_RejectsNonFragmentStructure(string input)
    {
        ISdpIceService service = new SdpIceService();

        SdpParseResult result = service.ParseTrickleIceFragment(input, new SdpParseOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Any(x => x.Code == SdpDiagnosticCodes.ValidationIce), Is.True);
    }

    [Test]
    public void ParseTrickleIceFragment_HonorsRawLineAndDiagnosticLimits()
    {
        ISdpIceService service = new SdpIceService();
        SdpParseOptions retainedOptions = new SdpParseOptions { PreserveRawLines = true };
        string valid = "a=ice-ufrag:user\r\na=ice-pwd:abcdefghijklmnopqrstuv\r\n";
        SdpParseOptions limitedOptions = new SdpParseOptions { MaximumDiagnosticCount = 1 };
        string invalid = "v=0\r\no=x\r\n";

        SdpParseResult retained = service.ParseTrickleIceFragment(valid, retainedOptions);
        SdpParseResult limited = service.ParseTrickleIceFragment(invalid, limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(retained.Success, Is.True);
            Assert.That(retained.Description!.RawLines, Has.Count.EqualTo(2));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(limited.DiagnosticLimitReached, Is.True);
        });
    }

    [Test]
    public void WriteTrickleIceFragment_GeneratesCanonicalPseudoMediaAndRoundTrips()
    {
        SdpSessionDescription fragment = new SdpSessionDescription();
        fragment.Attributes.Add(ValueAttribute("ice-ufrag", "sess"));
        fragment.Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));
        fragment.Attributes.Add(ValueAttribute("x-session-extension", "kept"));
        SdpMediaDescription media = new SdpMediaDescription { MediaType = "video", Port = 65535, Protocol = "ignored" };
        media.Attributes.Add(ValueAttribute("mid", "video"));
        media.Attributes.Add(ValueAttribute("candidate", "first 1 UDP 10 192.0.2.1 5000 typ host network-cost 5"));
        media.Attributes.Add(PropertyAttribute("end-of-candidates"));
        fragment.MediaDescriptions.Add(media);
        ISdpIceService service = new SdpIceService();
        SdpWriteOptions options = new SdpWriteOptions { LineEnding = SdpLineEnding.Lf, IncludeFinalLineEnding = false };

        SdpWriteResult written = service.WriteTrickleIceFragment(fragment, options);
        SdpParseOptions parseOptions = new SdpParseOptions { AllowLfLineEndings = true };
        SdpParseResult reparsed = service.ParseTrickleIceFragment(written.Text, parseOptions);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.Text, Is.EqualTo(
                "a=ice-ufrag:sess\n" +
                "a=ice-pwd:abcdefghijklmnopqrstuv\n" +
                "a=x-session-extension:kept\n" +
                "m=audio 9 RTP/AVP 0\n" +
                "a=mid:video\n" +
                "a=candidate:first 1 UDP 10 192.0.2.1 5000 typ host network-cost 5\n" +
                "a=end-of-candidates"));
            Assert.That(written.CharactersWritten, Is.EqualTo(written.Text.Length));
            Assert.That(reparsed.Success, Is.True);
            Assert.That(service.ResolveMediaIce(reparsed.Description!, 0).Candidates[0].Extensions[0].Name, Is.EqualTo("network-cost"));
            Assert.That(service.ResolveMediaIce(reparsed.Description!, 0).EndOfCandidates, Is.True);
        });
    }

    [Test]
    public void WriteTrickleIceFragment_RejectsInvalidModelsAndOutputOverflowWithoutPartialText()
    {
        ISdpIceService service = new SdpIceService();
        SdpSessionDescription invalid = new SdpSessionDescription();
        invalid.Attributes.Add(ValueAttribute("ice-ufrag", "user"));
        invalid.Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(ValueAttribute("mid", "audio"));
        media.Attributes.Add(PropertyAttribute("end-of-candidates"));
        media.Attributes.Add(ValueAttribute("candidate", "late 1 UDP 1 192.0.2.1 5000 typ host"));
        invalid.MediaDescriptions.Add(media);
        SdpSessionDescription valid = new SdpSessionDescription();
        valid.Attributes.Add(ValueAttribute("ice-ufrag", "user"));
        valid.Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));

        SdpWriteResult semanticFailure = service.WriteTrickleIceFragment(invalid, new SdpWriteOptions());
        SdpWriteResult overflow = service.WriteTrickleIceFragment(valid, new SdpWriteOptions { MaximumOutputCharacters = 5 });

        Assert.Multiple(() =>
        {
            Assert.That(semanticFailure.Success, Is.False);
            Assert.That(semanticFailure.Text, Is.Empty);
            Assert.That(semanticFailure.Diagnostics.Any(x => x.Code == SdpDiagnosticCodes.WriteInvalidDescription), Is.True);
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.Text, Is.Empty);
            Assert.That(overflow.Diagnostics.Single().Code, Is.EqualTo(SdpDiagnosticCodes.WriteOutputCharacterLimitExceeded));
        });
    }

    [Test]
    public void FragmentMethods_RejectNullArgumentsAndUnsupportedOptions()
    {
        ISdpIceService service = new SdpIceService();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ParseTrickleIceFragment(null!, new SdpParseOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("input"));
            Assert.That(() => service.ParseTrickleIceFragment(string.Empty, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => service.WriteTrickleIceFragment(null!, new SdpWriteOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("fragment"));
            Assert.That(() => service.WriteTrickleIceFragment(new SdpSessionDescription(), null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => service.WriteTrickleIceFragment(new SdpSessionDescription(), new SdpWriteOptions { LineEnding = SdpLineEnding.None }), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("options"));
        });
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
