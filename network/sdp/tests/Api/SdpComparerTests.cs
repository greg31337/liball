namespace sdp.Tests.Api;

public class SdpComparerTests
{
    [Test]
    public void CompareDescriptions_ReportsDeepCloneAsEqualAndCopiesAppliedOptions()
    {
        SdpSessionDescription original = FullDescription();
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        SdpComparisonOptions options = new SdpComparisonOptions();
        options.IgnoredAttributeNames.Add("x-ignored-later");
        ISdpComparer comparer = new SdpComparer();

        SdpComparisonResult result = comparer.CompareDescriptions(original, compared, options);
        result.AppliedOptions.IgnoredAttributeNames.Add("result-only");

        Assert.Multiple(() =>
        {
            Assert.That(result.Equal, Is.True);
            Assert.That(result.Differences, Is.Empty);
            Assert.That(result.AppliedOptions, Is.Not.SameAs(options));
            Assert.That(result.AppliedOptions.IgnoredAttributeNames, Is.Not.SameAs(options.IgnoredAttributeNames));
            Assert.That(options.IgnoredAttributeNames, Is.EqualTo(new[] { "x-ignored-later" }));
        });
    }

    [Test]
    public void CompareDescriptions_ReportsStablePathsAcrossTheCompleteCoreGraph()
    {
        const string originalSessionSecret = "session-secret";
        const string comparedSessionSecret = "changed-session-secret";
        const string originalMediaSecret = "media-secret";
        const string comparedMediaSecret = "changed-media-secret";
        SdpSessionDescription original = FullDescription();
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        compared.Origin!.Username = "changed-user";
        compared.Emails[0] = "changed@example.test";
        compared.Connection!.TimeToLive = 126;
        compared.Bandwidths[0].Value = 256;
        compared.Timings[0].RepeatTimes[0].Offsets[0].Value = 2;
        compared.TimeZoneAdjustments[0].Offset.Unit = "m";
        compared.EncryptionKey!.Value = comparedSessionSecret;
        compared.Attributes[0].Value = "changed-tool";
        compared.MediaDescriptions[0].MediaType = "x-audio";
        compared.MediaDescriptions[0].Formats[0] = "8";
        compared.MediaDescriptions[0].Information = "Changed media information";
        compared.MediaDescriptions[0].Connections[0].Address = "192.0.2.20";
        compared.MediaDescriptions[0].Bandwidths[0].Value = 32000;
        compared.MediaDescriptions[0].EncryptionKey!.Value = comparedMediaSecret;
        compared.MediaDescriptions[0].Attributes[1].Value = "changed-extension";
        compared.RawLines[0].Text = "v=changed";
        compared.LineEnding = SdpLineEnding.Lf;
        ISdpComparer comparer = new SdpComparer();

        SdpComparisonResult result = comparer.CompareDescriptions(original, compared, new SdpComparisonOptions());
        SdpComparisonResult repeated = comparer.CompareDescriptions(original, compared, new SdpComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Differences, Is.Not.Empty);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Session && difference.Path.FieldName == 'o' && difference.Detail.Contains("username")), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Session && difference.Path.FieldName == 'e' && difference.Path.OccurrenceIndex == 0), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Session && difference.Path.FieldName == 'c'), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Session && difference.Path.FieldName == 'b'), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.FieldName == 'r' && difference.Path.ValueIndex == 2), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.FieldName == 'z' && difference.Path.ValueIndex == 0), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Session && difference.Path.FieldName == 'a' && difference.Path.AttributeName == "tool"), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Media && difference.Path.MediaIndex == 0 && difference.Path.MediaId == "audio" && difference.Path.FieldName == 'm'), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Media && difference.Path.MediaIndex == 0 && difference.Path.FieldName == 'm' && difference.Path.ValueIndex == 0), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Media && difference.Path.FieldName == 'i'), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Media && difference.Path.FieldName == 'c'), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Media && difference.Path.FieldName == 'b'), Is.True);
            Assert.That(result.Differences.Where(difference => difference.ValuesRedacted), Is.Not.Empty);
            Assert.That(result.Differences.Where(difference => difference.ValuesRedacted), Has.All.Matches<SdpDifference>(difference => difference.OriginalValue.Length == 0 && difference.ComparedValue.Length == 0));
            Assert.That(result.Differences.Select(difference => difference.Detail), Is.EqualTo(repeated.Differences.Select(difference => difference.Detail)));
            Assert.That(ReadDifferenceText(result), Does.Not.Contain(originalSessionSecret));
            Assert.That(ReadDifferenceText(result), Does.Not.Contain(comparedSessionSecret));
            Assert.That(ReadDifferenceText(result), Does.Not.Contain(originalMediaSecret));
            Assert.That(ReadDifferenceText(result), Does.Not.Contain(comparedMediaSecret));
        });
    }

    [Test]
    public void CompareDescriptions_ReportsOrderedCollectionMovesWithStructuralPaths()
    {
        SdpSessionDescription original = MinimalDescription();
        original.Emails.Add("removed@example.test");
        original.Phones.Add("+1 555 0100");
        SdpMediaDescription media = Media("audio", "audio");
        media.Formats.Add("96");
        media.Attributes.Add(ValueAttribute("x-first", "1"));
        media.Attributes.Add(ValueAttribute("x-second", "2"));
        original.MediaDescriptions.Add(media);
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        compared.Emails.Add("added@example.test");
        compared.Phones.Clear();
        compared.MediaDescriptions[0].Formats[0] = "96";
        compared.MediaDescriptions[0].Formats[1] = "0";
        SdpAttribute first = compared.MediaDescriptions[0].Attributes[0];
        compared.MediaDescriptions[0].Attributes[0] = compared.MediaDescriptions[0].Attributes[1];
        compared.MediaDescriptions[0].Attributes[1] = first;
        compared.MediaDescriptions.Add(Media("video", "video"));

        SdpComparisonResult result = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Differences.Any(difference => difference.Kind == SdpDifferenceKind.Added && difference.Path.FieldName == 'e' && difference.Path.OccurrenceIndex == 1), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Kind == SdpDifferenceKind.Removed && difference.Path.FieldName == 'p' && difference.Path.OccurrenceIndex == 0), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Kind == SdpDifferenceKind.Reordered && difference.Path.FieldName == 'm' && difference.Path.ValueIndex == 0), Is.True);
            Assert.That(result.Differences.Count(difference => difference.Kind == SdpDifferenceKind.Reordered && difference.Path.FieldName == 'a' && difference.Path.Scope == SdpScope.Media), Is.GreaterThanOrEqualTo(2));
            Assert.That(result.Differences.Any(difference => difference.Kind == SdpDifferenceKind.Added && difference.Path.FieldName == 'm' && difference.Path.MediaIndex == 1 && difference.Path.MediaId == "video" && difference.ComparedValue == "video;mid=video"), Is.True);
        });
    }

    [Test]
    public void CompareDescriptions_MatchesMovedMediaByMidAndReportsSafeValues()
    {
        SdpSessionDescription original = MinimalDescription();
        SdpMediaDescription audio = Media("audio", "audio");
        audio.Formats.Add("96");
        audio.Attributes.Add(ValueAttribute("x-first", "1"));
        audio.Attributes.Add(ValueAttribute("x-second", "2"));
        SdpMediaDescription video = Media("video", "video");
        original.MediaDescriptions.Add(audio);
        original.MediaDescriptions.Add(video);
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        SdpMediaDescription movedAudio = compared.MediaDescriptions[0];
        compared.MediaDescriptions[0] = compared.MediaDescriptions[1];
        compared.MediaDescriptions[1] = movedAudio;
        string movedFormat = compared.MediaDescriptions[1].Formats[0];
        compared.MediaDescriptions[1].Formats[0] = compared.MediaDescriptions[1].Formats[1];
        compared.MediaDescriptions[1].Formats[1] = movedFormat;
        SdpAttribute movedAttribute = compared.MediaDescriptions[1].Attributes[1];
        compared.MediaDescriptions[1].Attributes[1] = compared.MediaDescriptions[1].Attributes[2];
        compared.MediaDescriptions[1].Attributes[2] = movedAttribute;

        SdpComparisonResult result = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Differences.Any(difference => difference.Kind == SdpDifferenceKind.Reordered && difference.Path.FieldName == 'm' && difference.Path.MediaIndex == 1 && difference.Path.MediaId == "audio" && difference.OriginalValue == "audio;mid=audio" && difference.ComparedValue == "audio;mid=audio"), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Kind == SdpDifferenceKind.Reordered && difference.Path.FieldName == 'm' && difference.Path.MediaIndex == 1 && difference.Path.MediaId == "audio" && difference.Path.ValueIndex == 0 && difference.OriginalValue == "96" && difference.ComparedValue == "96"), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Kind == SdpDifferenceKind.Reordered && difference.Path.AttributeName == "x-first" && difference.Path.MediaIndex == 1 && difference.OriginalValue == "x-first:1" && difference.ComparedValue == "x-first:1"), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Kind == SdpDifferenceKind.Added || difference.Kind == SdpDifferenceKind.Removed), Is.False);
        });
    }

    [Test]
    public void CompareDescriptions_UsesPayloadAwareAttributePathsAndRedactsSensitiveValues()
    {
        const string originalSecret = "original-comparison-secret";
        const string comparedSecret = "compared-comparison-secret";
        SdpSessionDescription original = MinimalDescription();
        SdpMediaDescription media = Media("audio", "audio");
        media.Formats.Add("96");
        media.Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        media.Attributes.Add(ValueAttribute("x-removed", "old"));
        media.Attributes.Add(ValueAttribute("ice-pwd", originalSecret));
        original.MediaDescriptions.Add(media);
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        compared.MediaDescriptions[0].Attributes[1].Value = "96 opus/16000/1";
        compared.MediaDescriptions[0].Attributes.RemoveAt(2);
        compared.MediaDescriptions[0].Attributes[2].Value = comparedSecret;
        compared.MediaDescriptions[0].Attributes.Add(ValueAttribute("x-added", "new"));

        SdpComparisonResult result = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions());
        SdpDifference payloadChange = result.Differences.Single(difference => difference.Kind == SdpDifferenceKind.Changed && difference.Path.AttributeName == "rtpmap" && difference.Detail.Contains("value differs", StringComparison.Ordinal));
        SdpDifference removed = result.Differences.Single(difference => difference.Kind == SdpDifferenceKind.Removed && difference.Path.AttributeName == "x-removed");
        SdpDifference added = result.Differences.Single(difference => difference.Kind == SdpDifferenceKind.Added && difference.Path.AttributeName == "x-added");
        List<SdpDifference> secretDifferences = result.Differences.Where(difference => difference.Path.AttributeName == "ice-pwd").ToList();

        Assert.Multiple(() =>
        {
            Assert.That(payloadChange.Path.ValueIndex, Is.EqualTo(1));
            Assert.That(payloadChange.Path.OccurrenceIndex, Is.Zero);
            Assert.That(payloadChange.OriginalValue, Is.EqualTo("96 opus/48000/2"));
            Assert.That(payloadChange.ComparedValue, Is.EqualTo("96 opus/16000/1"));
            Assert.That(removed.OriginalValue, Is.EqualTo("x-removed:old"));
            Assert.That(removed.ComparedValue, Is.Empty);
            Assert.That(added.OriginalValue, Is.Empty);
            Assert.That(added.ComparedValue, Is.EqualTo("x-added:new"));
            Assert.That(secretDifferences, Is.Not.Empty);
            Assert.That(secretDifferences, Has.All.Matches<SdpDifference>(difference => difference.ValuesRedacted && difference.OriginalValue.Length == 0 && difference.ComparedValue.Length == 0));
            Assert.That(ReadDifferenceText(result), Does.Not.Contain(originalSecret));
            Assert.That(ReadDifferenceText(result), Does.Not.Contain(comparedSecret));
        });
    }

    [Test]
    public void CompareDescriptions_RejectsNullArgumentsAndInvalidOptions()
    {
        ISdpComparer comparer = new SdpComparer();
        SdpSessionDescription description = MinimalDescription();
        SdpComparisonOptions invalidCollections = new SdpComparisonOptions();
        invalidCollections.IgnoredAttributeNames = null!;

        Assert.Multiple(() =>
        {
            Assert.That(() => comparer.CompareDescriptions(null!, description, new SdpComparisonOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("originalDescription"));
            Assert.That(() => comparer.CompareDescriptions(description, null!, new SdpComparisonOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("comparedDescription"));
            Assert.That(() => comparer.CompareDescriptions(description, description, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => comparer.CompareDescriptions(description, description, invalidCollections), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("options"));
            Assert.That(() => comparer.CompareDescriptions(description, description, new SdpComparisonOptions { IgnoredAttributeNames = { "not a token" } }), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("options"));
        });
    }

    [Test]
    public void SemanticComparison_IgnoresDefinedCasingWhitespaceTimingAndRawFormattingDifferences()
    {
        SdpSessionDescription original = MinimalDescription();
        original.Origin!.SessionId = "0001";
        original.Origin.SessionVersion = "0002";
        original.Origin.NetworkType = "in";
        original.Origin.AddressType = "ip4";
        original.Origin.UnicastAddress = "HOST.EXAMPLE";
        original.Origin.AddressKind = SdpAddressKind.Unknown;
        original.Connection!.NetworkType = "in";
        original.Connection.AddressType = "ip4";
        original.Connection.Address = "HOST.EXAMPLE";
        original.Connection.AddressKind = SdpAddressKind.Unknown;
        original.Timings[0].RepeatTimes.Add(new SdpRepeatTime
        {
            RepeatInterval = new SdpTypedTime { Value = 1, Unit = "d", RawValue = "1d" },
            ActiveDuration = new SdpTypedTime { Value = 1, Unit = "h", RawValue = "1h" },
            Offsets = { new SdpTypedTime { Value = 0, Unit = "s", RawValue = "0s" } }
        });
        SdpMediaDescription originalMedia = Media("VIDEO", "video");
        originalMedia.Protocol = "rtp/avp";
        originalMedia.Formats[0] = "096";
        originalMedia.Attributes.Add(ValueAttribute("RTPMAP", "96 OPUS/48000/2"));
        originalMedia.Attributes.Add(ValueAttribute("GROUP", "X-REL   first second"));
        original.MediaDescriptions.Add(originalMedia);
        original.RawLines.Add(new SdpRawLine { FieldName = 's', Text = "s=Comparison  test", Value = "Comparison  test", LineEnding = SdpLineEnding.Lf });
        original.LineEnding = SdpLineEnding.Lf;
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        compared.Origin!.SessionId = "1";
        compared.Origin.SessionVersion = "2";
        compared.Origin.NetworkType = "IN";
        compared.Origin.AddressType = "IP4";
        compared.Origin.UnicastAddress = "host.example";
        compared.Origin.AddressKind = SdpAddressKind.HostName;
        compared.Connection!.NetworkType = "IN";
        compared.Connection.AddressType = "IP4";
        compared.Connection.Address = "host.example";
        compared.Connection.AddressKind = SdpAddressKind.HostName;
        compared.Timings[0].RepeatTimes[0].RepeatInterval = new SdpTypedTime { Value = 86400 };
        compared.Timings[0].RepeatTimes[0].ActiveDuration = new SdpTypedTime { Value = 3600 };
        compared.Timings[0].RepeatTimes[0].Offsets[0] = new SdpTypedTime { Value = 0 };
        compared.MediaDescriptions[0].MediaType = "video";
        compared.MediaDescriptions[0].MediaKind = SdpMediaKind.Video;
        compared.MediaDescriptions[0].Protocol = "RTP/AVP";
        compared.MediaDescriptions[0].Formats[0] = "96";
        compared.MediaDescriptions[0].Attributes[1].Name = "rtpmap";
        compared.MediaDescriptions[0].Attributes[1].Value = "96 opus/48000/2";
        compared.MediaDescriptions[0].Attributes[2].Name = "group";
        compared.MediaDescriptions[0].Attributes[2].Value = "X-REL first second";
        compared.RawLines.Clear();
        compared.LineEnding = SdpLineEnding.CrLf;

        SdpComparisonResult structural = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions());
        SdpComparisonResult semantic = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions { CompareSemanticValues = true });

        Assert.Multiple(() =>
        {
            Assert.That(structural.Equal, Is.False);
            Assert.That(semantic.Equal, Is.True, ReadDifferenceText(semantic));
            Assert.That(semantic.Differences, Is.Empty);
            Assert.That(original.Origin.SessionId, Is.EqualTo("0001"));
            Assert.That(original.MediaDescriptions[0].Attributes[1].Value, Is.EqualTo("96 OPUS/48000/2"));
            Assert.That(original.RawLines, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void SemanticComparison_UsesEffectiveInheritedConnectionBandwidthAndAttributes()
    {
        SdpSessionDescription inherited = MinimalDescription();
        inherited.Bandwidths.Add(new SdpBandwidth { Type = "AS", Value = 128 });
        inherited.Attributes.Add(PropertyAttribute("sendonly"));
        inherited.Attributes.Add(ValueAttribute("lang", "EN-US"));
        inherited.Attributes.Add(ValueAttribute("ice-ufrag", "abcd"));
        inherited.Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));
        inherited.Attributes.Add(ValueAttribute("extmap", "1 urn:example:extension"));
        inherited.MediaDescriptions.Add(Media("audio", "audio"));
        SdpSessionDescription overridden = new SdpEditor().CloneDescription(inherited);
        overridden.MediaDescriptions[0].Connections.Add(overridden.Connection!);
        overridden.Connection = null;
        overridden.MediaDescriptions[0].Bandwidths.Add(overridden.Bandwidths[0]);
        overridden.Bandwidths.Clear();
        int attributeIndex = 0;
        while (attributeIndex < overridden.Attributes.Count)
        {
            overridden.MediaDescriptions[0].Attributes.Add(overridden.Attributes[attributeIndex]);
            attributeIndex = attributeIndex + 1;
        }
        overridden.Attributes.Clear();

        SdpComparisonResult structural = new SdpComparer().CompareDescriptions(inherited, overridden, new SdpComparisonOptions());
        SdpComparisonResult semantic = new SdpComparer().CompareDescriptions(inherited, overridden, new SdpComparisonOptions { CompareSemanticValues = true });

        Assert.Multiple(() =>
        {
            Assert.That(structural.Equal, Is.False);
            Assert.That(semantic.Equal, Is.True, ReadDifferenceText(semantic));
            Assert.That(inherited.Connection, Is.Not.Null);
            Assert.That(inherited.Attributes, Has.Count.EqualTo(5));
            Assert.That(overridden.Connection, Is.Null);
            Assert.That(overridden.MediaDescriptions[0].Attributes, Has.Count.EqualTo(6));
        });
    }

    [TestCase(SdpAttributeNames.IceUsernameFragment, "abcd")]
    [TestCase(SdpAttributeNames.IcePassword, "abcdefghijklmnopqrstuv")]
    public void SemanticComparison_HandlesOneSidedIceCredentialsWithoutThrowing(string attributeName, string attributeValue)
    {
        SdpSessionDescription original = MinimalDescription();
        original.Attributes.Add(ValueAttribute(attributeName, attributeValue));
        original.MediaDescriptions.Add(Media("audio", "audio"));
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);

        SdpComparisonResult result = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions
        {
            CompareSemanticValues = true,
            IgnoreRawFormatting = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Equal, Is.True, ReadDifferenceText(result));
            Assert.That(result.Differences, Is.Empty);
        });
    }

    [Test]
    public void SemanticComparison_NormalizesUnboundedRfcTimesWithoutNumericOverflow()
    {
        SdpSessionDescription original = MinimalDescription();
        original.Timings[0].StartTime = "000184467440737095516160";
        original.Timings[0].StopTime = "000184467440737095516161";
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        compared.Timings[0].StartTime = "184467440737095516160";
        compared.Timings[0].StopTime = "184467440737095516161";

        SdpComparisonResult result = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions
        {
            CompareSemanticValues = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Equal, Is.True, ReadDifferenceText(result));
            Assert.That(result.Differences, Is.Empty);
        });
    }

    [Test]
    public void ComparisonOptions_ControlVersionRawAttributeAndInsignificantOrderingDifferences()
    {
        SdpSessionDescription original = MinimalDescription();
        original.Origin!.SessionVersion = "7";
        original.Origin.RawText = "- 1 7 IN IP4 192.0.2.1";
        original.Attributes.Add(ValueAttribute("x-second", "2"));
        original.Attributes.Add(ValueAttribute("x-first", "1"));
        original.Attributes.Add(ValueAttribute("x-trace", "original"));
        original.RawLines.Add(new SdpRawLine { FieldName = 'o', Text = "o=- 1 7 IN IP4 192.0.2.1", Value = "- 1 7 IN IP4 192.0.2.1", LineEnding = SdpLineEnding.CrLf });
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        compared.Origin!.SessionVersion = "8";
        compared.Origin.RawText = "- 1 8 IN IP4 192.0.2.1";
        compared.RawLines[0].Text = "o=- 1 8 IN IP4 192.0.2.1";
        compared.RawLines[0].Value = "- 1 8 IN IP4 192.0.2.1";
        compared.Attributes[2].Value = "compared";
        SdpAttribute first = compared.Attributes[0];
        compared.Attributes[0] = compared.Attributes[1];
        compared.Attributes[1] = first;
        SdpComparisonOptions options = new SdpComparisonOptions
        {
            IgnoreOriginSessionVersion = true,
            IgnoreRawFormatting = true,
            IgnoreInsignificantOrdering = true
        };
        options.IgnoredAttributeNames.Add("X-Trace");

        SdpComparisonResult exact = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions());
        SdpComparisonResult configured = new SdpComparer().CompareDescriptions(original, compared, options);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Equal, Is.False);
            Assert.That(configured.Equal, Is.True, ReadDifferenceText(configured));
            Assert.That(configured.AppliedOptions.IgnoreOriginSessionVersion, Is.True);
            Assert.That(configured.AppliedOptions.IgnoreRawFormatting, Is.True);
            Assert.That(configured.AppliedOptions.IgnoreInsignificantOrdering, Is.True);
            Assert.That(configured.AppliedOptions.IgnoredAttributeNames, Is.EqualTo(new[] { "X-Trace" }));
        });
    }

    [Test]
    public void SemanticComparison_OnlyIgnoresAttributeOrderingWhenRequested()
    {
        SdpSessionDescription original = MinimalDescription();
        original.Attributes.Add(ValueAttribute("x-second", "2"));
        original.Attributes.Add(ValueAttribute("x-first", "1"));
        original.MediaDescriptions.Add(Media("audio", "audio"));
        SdpSessionDescription reordered = new SdpEditor().CloneDescription(original);
        SdpAttribute first = reordered.Attributes[0];
        reordered.Attributes[0] = reordered.Attributes[1];
        reordered.Attributes[1] = first;

        SdpComparisonResult ordered = new SdpComparer().CompareDescriptions(original, reordered, new SdpComparisonOptions { CompareSemanticValues = true });
        SdpComparisonResult unordered = new SdpComparer().CompareDescriptions(original, reordered, new SdpComparisonOptions
        {
            CompareSemanticValues = true,
            IgnoreInsignificantOrdering = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(ordered.Equal, Is.False);
            Assert.That(unordered.Equal, Is.True, ReadDifferenceText(unordered));
        });
    }

    [Test]
    public void SemanticComparison_ReportsEffectiveMediaChangesAtMediaPaths()
    {
        SdpSessionDescription original = MinimalDescription();
        original.MediaDescriptions.Add(Media("audio", "audio"));
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        compared.Connection!.Address = "203.0.113.1";
        compared.Connection.AddressKind = SdpAddressKind.Ipv4;

        SdpComparisonResult result = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions { CompareSemanticValues = true });

        Assert.Multiple(() =>
        {
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Media && difference.Path.MediaIndex == 0 && difference.Path.MediaId == "audio" && difference.Path.FieldName == 'c'), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Path.Scope == SdpScope.Session && difference.Path.FieldName == 'c'), Is.False);
        });
    }

    [Test]
    public void CompareDescriptions_ReportsEveryDifferenceKindIncludingMovedAndChangedAttributes()
    {
        SdpSessionDescription original = MinimalDescription();
        original.Phones.Add("+1 555 0100");
        SdpMediaDescription media = Media("audio", "audio");
        media.Formats.Add("96");
        media.Attributes.Add(ValueAttribute("rtpmap", "96 opus/48000/2"));
        media.Attributes.Add(ValueAttribute("x-marker", "stable"));
        original.MediaDescriptions.Add(media);
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        compared.SessionName = "Changed comparison";
        compared.Emails.Add("added@example.test");
        compared.Phones.Clear();
        SdpAttribute mapping = compared.MediaDescriptions[0].Attributes[1];
        compared.MediaDescriptions[0].Attributes[1] = compared.MediaDescriptions[0].Attributes[2];
        compared.MediaDescriptions[0].Attributes[2] = mapping;
        compared.MediaDescriptions[0].Attributes[2].Value = "96 opus/16000/1";

        SdpComparisonResult result = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Differences.Select(difference => difference.Kind), Does.Contain(SdpDifferenceKind.Added));
            Assert.That(result.Differences.Select(difference => difference.Kind), Does.Contain(SdpDifferenceKind.Removed));
            Assert.That(result.Differences.Select(difference => difference.Kind), Does.Contain(SdpDifferenceKind.Changed));
            Assert.That(result.Differences.Select(difference => difference.Kind), Does.Contain(SdpDifferenceKind.Reordered));
            Assert.That(result.Differences.Any(difference => difference.Kind == SdpDifferenceKind.Changed && difference.Path.AttributeName == "rtpmap" && difference.Path.ValueIndex == 1), Is.True);
            Assert.That(result.Differences.Any(difference => difference.Kind == SdpDifferenceKind.Reordered && difference.Path.AttributeName == "rtpmap" && difference.Path.ValueIndex == 1), Is.True);
        });
    }

    [Test]
    public void CompareDescriptions_DetectsEveryPopulatedCoreModelScalarField()
    {
        SdpSessionDescription working = FullDescription();
        SdpSessionDescription reference = new SdpEditor().CloneDescription(working);

        int checkedFieldCount = AssertScalarMutationsDetected(working, working, reference, new SdpComparer());

        Assert.That(checkedFieldCount, Is.GreaterThan(100));
    }

    [Test]
    public void CompareDescriptions_HandlesEveryTypedAttributeRepresentation()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpSessionDescription original = MinimalDescription();
        SdpMediaDescription media = new SdpSessionFactory().CreateMedia(SdpMediaTypes.Audio, 9, SdpTransportProtocols.RtpSavpf, new List<string> { "0", "96", "98", "99", "111" });
        List<SdpAttribute> sourceAttributes = SpecializedAttributeSamples();
        int index = 0;
        while (index < sourceAttributes.Count)
        {
            SdpAttributeDecodeResult decoded = codec.DecodeAttribute(sourceAttributes[index]);
            Assert.That(decoded.Success && decoded.Recognized && decoded.TypedAttribute != null, Is.True, sourceAttributes[index].Name);
            SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.TypedAttribute!);
            Assert.That(encoded.Success && encoded.Attribute != null, Is.True, sourceAttributes[index].Name);
            media.Attributes.Add(encoded.Attribute!);
            index = index + 1;
        }
        original.MediaDescriptions.Add(media);
        SdpComparisonResult equal = new SdpComparer().CompareDescriptions(original, new SdpEditor().CloneDescription(original), new SdpComparisonOptions());

        Assert.That(equal.Equal, Is.True, ReadDifferenceText(equal));
        index = 0;
        while (index < media.Attributes.Count)
        {
            SdpSessionDescription changed = new SdpEditor().CloneDescription(original);
            changed.MediaDescriptions[0].Attributes[index].Value = changed.MediaDescriptions[0].Attributes[index].Value + " changed";
            SdpComparisonResult difference = new SdpComparer().CompareDescriptions(original, changed, new SdpComparisonOptions());
            Assert.That(difference.Equal, Is.False, media.Attributes[index].Name);
            Assert.That(difference.Differences.Any(item => item.Path.AttributeName == media.Attributes[index].Name), Is.True, media.Attributes[index].Name);
            index = index + 1;
        }
    }

    [Test]
    public void SemanticComparison_UsesBundleTaggedTransportValuesForEveryMember()
    {
        SdpSessionDescription original = MinimalDescription();
        original.Attributes.Add(ValueAttribute("group", "BUNDLE audio video"));
        SdpMediaDescription audio = Media("audio", "audio");
        audio.Port = 5004;
        audio.Connections.Add(new SdpConnectionData
        {
            NetworkType = "IN",
            AddressType = "IP4",
            Address = "198.51.100.10",
            AddressKind = SdpAddressKind.Ipv4
        });
        audio.Attributes.Add(ValueAttribute("ice-ufrag", "audioTag"));
        audio.Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));
        audio.Attributes.Add(ValueAttribute("fingerprint", "sha-256 AA:BB:CC:DD"));
        audio.Attributes.Add(ValueAttribute("setup", "actpass"));
        SdpMediaDescription video = Media("video", "video");
        original.MediaDescriptions.Add(audio);
        original.MediaDescriptions.Add(video);
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        compared.MediaDescriptions[1].Port = 6000;
        compared.MediaDescriptions[1].Connections.Add(new SdpConnectionData
        {
            NetworkType = "IN",
            AddressType = "IP4",
            Address = "203.0.113.99",
            AddressKind = SdpAddressKind.Ipv4
        });
        compared.MediaDescriptions[1].Attributes.Add(ValueAttribute("ice-ufrag", "ignoredVideo"));
        compared.MediaDescriptions[1].Attributes.Add(ValueAttribute("ice-pwd", "zyxwvutsrqponmlkjihgfe"));
        compared.MediaDescriptions[1].Attributes.Add(ValueAttribute("fingerprint", "sha-256 11:22:33:44"));
        compared.MediaDescriptions[1].Attributes.Add(ValueAttribute("setup", "passive"));

        SdpComparisonResult structural = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions());
        SdpComparisonResult semantic = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions { CompareSemanticValues = true });

        Assert.Multiple(() =>
        {
            Assert.That(structural.Equal, Is.False);
            Assert.That(semantic.Equal, Is.True, ReadDifferenceText(semantic));
            Assert.That(ReadDifferenceText(structural), Does.Not.Contain("abcdefghijklmnopqrstuv"));
            Assert.That(ReadDifferenceText(structural), Does.Not.Contain("zyxwvutsrqponmlkjihgfe"));
        });
    }

    [Test]
    public void ComparisonOptions_CanIgnoreUnknownAttributeAndRawDataDifferences()
    {
        SdpSessionDescription original = MinimalDescription();
        original.Attributes.Add(ValueAttribute("x-future", "opaque-original"));
        original.RawLines.Add(new SdpRawLine { FieldName = 'x', Text = "x=raw-original", Value = "raw-original" });
        SdpSessionDescription compared = new SdpEditor().CloneDescription(original);
        compared.Attributes[0].Value = "opaque-compared";
        compared.RawLines[0].Text = "x=raw-compared";
        compared.RawLines[0].Value = "raw-compared";
        SdpComparisonOptions options = new SdpComparisonOptions { IgnoreRawFormatting = true };
        options.IgnoredAttributeNames.Add("X-FUTURE");

        SdpComparisonResult exact = new SdpComparer().CompareDescriptions(original, compared, new SdpComparisonOptions());
        SdpComparisonResult ignored = new SdpComparer().CompareDescriptions(original, compared, options);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Equal, Is.False);
            Assert.That(exact.Differences.Any(difference => difference.Path.AttributeName == "x-future" && !difference.ValuesRedacted), Is.True);
            Assert.That(exact.Differences.Any(difference => difference.Path.FieldName == 'x' && difference.ValuesRedacted), Is.True);
            Assert.That(ignored.Equal, Is.True, ReadDifferenceText(ignored));
        });
    }

    static int AssertScalarMutationsDetected(object value, SdpSessionDescription working, SdpSessionDescription reference, ISdpComparer comparer)
    {
        if (value is System.Collections.IEnumerable sequence && value is not string)
        {
            int sequenceCount = 0;
            foreach (object? item in sequence)
            {
                if (item != null)
                {
                    sequenceCount = sequenceCount + AssertScalarMutationsDetected(item, working, reference, comparer);
                }
            }
            return sequenceCount;
        }
        int count = 0;
        System.Reflection.FieldInfo[] fields = value.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        int index = 0;
        while (index < fields.Length)
        {
            System.Reflection.FieldInfo field = fields[index];
            object? originalValue = field.GetValue(value);
            if (CanMutateScalarType(field.FieldType))
            {
                field.SetValue(value, ChangedScalarValue(field.FieldType, originalValue));
                SdpComparisonResult result = comparer.CompareDescriptions(reference, working, new SdpComparisonOptions());
                Assert.That(result.Equal, Is.False, field.DeclaringType!.Name + "." + field.Name);
                Assert.That(result.Differences, Is.Not.Empty, field.DeclaringType!.Name + "." + field.Name);
                field.SetValue(value, originalValue);
                count = count + 1;
            }
            else if (originalValue != null)
            {
                count = count + AssertScalarMutationsDetected(originalValue, working, reference, comparer);
            }
            index = index + 1;
        }
        return count;
    }

    static bool CanMutateScalarType(Type type)
    {
        return type == typeof(string) || type == typeof(bool) || type == typeof(char) || type == typeof(int) || type == typeof(long) || type.IsEnum;
    }

    static object ChangedScalarValue(Type type, object? value)
    {
        if (type == typeof(string))
        {
            return (value == null ? string.Empty : (string)value) + "~changed";
        }
        if (type == typeof(bool))
        {
            return !(bool)value!;
        }
        if (type == typeof(char))
        {
            return (char)value! == 'x' ? 'y' : 'x';
        }
        if (type == typeof(int))
        {
            return (int)value! + 1;
        }
        if (type == typeof(long))
        {
            return (long)value! + 1;
        }
        Array values = Enum.GetValues(type);
        int index = 0;
        while (index < values.Length)
        {
            object candidate = values.GetValue(index)!;
            if (!candidate.Equals(value))
            {
                return candidate;
            }
            index = index + 1;
        }
        return value!;
    }

    static List<SdpAttribute> SpecializedAttributeSamples()
    {
        return new List<SdpAttribute>
        {
            PropertyAttribute("sendrecv"),
            ValueAttribute("rtpmap", "98 L16/16000/2"),
            ValueAttribute("fmtp", "111 minptime=10;X-Vendor=opaque;flag"),
            ValueAttribute("rtcp", "53020 IN IP4 126.16.64.4"),
            ValueAttribute("rtcp-fb", "96 ccm fir"),
            ValueAttribute("rtcp-xr", "pkt-loss-rle=120 stat-summary=loss,jitt x-future=opaque"),
            ValueAttribute("ssrc", "42 x-note:opaque value"),
            ValueAttribute("ssrc-group", "X-DUP 4294967295 0 4294967295"),
            ValueAttribute("msid", "stream-1 track-1"),
            ValueAttribute("msid-semantic", "WMS *"),
            ValueAttribute("source-filter", "incl IN IP4 232.3.4.5 192.0.2.10"),
            ValueAttribute("ts-refclk", "gps"),
            ValueAttribute("mediaclk", "direct=963214424 rate=1000/1001"),
            ValueAttribute("imageattr", "99 send [x=800,y=640] recv [x=330,y=250]"),
            ValueAttribute("group", "X-REL audio video"),
            ValueAttribute("mid", "audio"),
            ValueAttribute("ice-ufrag", "8hhY"),
            ValueAttribute("ice-pwd", "asd88fgpdd777uzjYhagZg"),
            ValueAttribute("ice-options", "trickle XFuture rtp+ecn"),
            ValueAttribute("candidate", "842163049 1 X-QUIC 1677734910 peer.local 60769 typ x-nat raddr 10.0.0.2 rport 8998 generation 0 network-cost 999"),
            ValueAttribute("remote-candidates", "2 192.0.2.3 45665 1 192.0.2.3 45664"),
            ValueAttribute("fingerprint", "SHA-256 DE:AD:BE:EF"),
            ValueAttribute("setup", "passive"),
            ValueAttribute("connection", "new"),
            ValueAttribute("tls-id", "abc3de65cddef001be82"),
            ValueAttribute("crypto", "1 AES_CM_128_HMAC_SHA1_80 inline:key UNENCRYPTED_SRTCP"),
            ValueAttribute("extmap", "2/sendonly urn:example:x-extension short opaque data"),
            ValueAttribute("rid", "f-1_send send pt=96,0,127;max-width=1280;vendor-mode=alpha beta=gamma"),
            ValueAttribute("simulcast", "send high;mid,~mid-vp9;~low recv remote-a,remote-b;~remote-low"),
            ValueAttribute("sctp-port", "5000"),
            ValueAttribute("max-message-size", "65536"),
            ValueAttribute("sctpmap", "5000 webrtc-datachannel 16"),
            ValueAttribute("dcmap", "4 label=\"foo%09bar\";subprotocol=\"\";X-Future=opaque"),
            ValueAttribute("dcsa", "3 path:msrp://example.test/id;dc")
        };
    }

    static SdpSessionDescription FullDescription()
    {
        string input =
            "v=0\r\n" +
            "o=user 1 2 IN IP4 192.0.2.1\r\n" +
            "s=Comparison source\r\n" +
            "i=Session information\r\n" +
            "u=https://example.test/session\r\n" +
            "e=person@example.test\r\n" +
            "p=+1 555 0100\r\n" +
            "c=IN IP4 233.252.0.1/127/3\r\n" +
            "b=AS:128\r\n" +
            "t=0 0\r\n" +
            "r=7d 1h 0\r\n" +
            "z=2882844526 -1h\r\n" +
            "k=clear:session-secret\r\n" +
            "a=tool:comparison-test\r\n" +
            "m=audio 49170 RTP/AVP 0\r\n" +
            "i=Media information\r\n" +
            "c=IN IP4 192.0.2.2\r\n" +
            "b=TIAS:64000\r\n" +
            "k=clear:media-secret\r\n" +
            "a=mid:audio\r\n" +
            "a=x-media:extension\r\n";
        return new SdpParser().ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Lossless }).Description!;
    }

    static SdpSessionDescription MinimalDescription()
    {
        return new SdpSessionFactory().CreateSession(
            new SdpOrigin
            {
                Username = "-",
                SessionId = "1",
                SessionVersion = "1",
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                UnicastAddress = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            "Comparison test",
            new SdpConnectionData
            {
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                Address = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            new SdpTiming { StartTime = "0", StopTime = "0" });
    }

    static SdpMediaDescription Media(string mediaType, string mediaId)
    {
        SdpMediaDescription media = new SdpSessionFactory().CreateMedia(mediaType, 9, SdpTransportProtocols.RtpAvp, new List<string> { "0" });
        media.Attributes.Add(ValueAttribute("mid", mediaId));
        return media;
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

    static string ReadDifferenceText(SdpComparisonResult result)
    {
        return string.Join("|", result.Differences.Select(difference => difference.Detail + difference.OriginalValue + difference.ComparedValue));
    }
}
