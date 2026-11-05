namespace sdp.Tests.Unit.Operations;

public class SdpIceServiceTests
{
    [Test]
    public void ResolveMediaIce_AppliesIndependentSessionDefaultsAndMediaOverrides()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute("ice-ufrag", "sess"));
        description.Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));
        description.Attributes.Add(ValueAttribute("ice-options", "ice2 trickle"));
        description.Attributes.Add(PropertyAttribute("ice-lite"));
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(ValueAttribute("ice-ufrag", "media"));
        media.Attributes.Add(ValueAttribute("ice-options", "renomination"));
        media.Attributes.Add(ValueAttribute("candidate", "one 1 UDP 2 192.0.2.1 5000 typ host"));
        media.Attributes.Add(ValueAttribute("candidate", "two 2 TCP 1 2001:db8::1 9 typ host tcptype active"));
        media.Attributes.Add(ValueAttribute("remote-candidates", "1 192.0.2.2 6000 2 2001:db8::2 6001"));
        media.Attributes.Add(PropertyAttribute("ice-mismatch"));
        media.Attributes.Add(PropertyAttribute("end-of-candidates"));
        description.MediaDescriptions.Add(media);
        ISdpIceService service = new SdpIceService();

        SdpIceDescription resolved = service.ResolveMediaIce(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Credentials, Is.Not.Null);
            Assert.That(resolved.Credentials!.UsernameFragment, Is.EqualTo("media"));
            Assert.That(resolved.Credentials.Password, Is.EqualTo("abcdefghijklmnopqrstuv"));
            Assert.That(resolved.Options, Is.EqualTo(new[] { "renomination" }));
            Assert.That(resolved.IceLite, Is.True);
            Assert.That(resolved.IceMismatch, Is.True);
            Assert.That(resolved.EndOfCandidates, Is.True);
            Assert.That(resolved.Candidates.Select(x => x.Foundation), Is.EqualTo(new[] { "one", "two" }));
            Assert.That(resolved.RemoteCandidates.Select(x => x.Component), Is.EqualTo(new[] { 1, 2 }));
        });
    }

    [Test]
    public void ResolveMediaIce_UsesSessionOptionsAndSkipsMalformedTypedValues()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute("ice-options", "ice2 trickle"));
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(ValueAttribute("candidate", "malformed"));
        media.Attributes.Add(ValueAttribute("candidate", "valid 1 UDP 1 192.0.2.1 5000 typ host"));
        media.Attributes.Add(ValueAttribute("remote-candidates", "broken"));
        media.Attributes.Add(new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "ice-mismatch", Value = "invalid" });
        description.MediaDescriptions.Add(media);
        ISdpIceService service = new SdpIceService();

        SdpIceDescription resolved = service.ResolveMediaIce(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Credentials, Is.Null);
            Assert.That(resolved.Options, Is.EqualTo(new[] { "ice2", "trickle" }));
            Assert.That(resolved.Candidates, Has.Count.EqualTo(1));
            Assert.That(resolved.Candidates[0].Foundation, Is.EqualTo("valid"));
            Assert.That(resolved.RemoteCandidates, Is.Empty);
            Assert.That(resolved.IceMismatch, Is.False);
        });
    }

    [Test]
    public void ResolveEffectiveIce_UsesTheBundleTaggedMediaTransport()
    {
        SdpSessionDescription description = CreateBundledDescription("BUNDLE audio video");
        SdpMediaDescription audio = description.MediaDescriptions[0];
        audio.Attributes.Add(ValueAttribute("ice-ufrag", "owner"));
        audio.Attributes.Add(ValueAttribute("ice-pwd", "zyxwvutsrqponmlkjihgfe"));
        audio.Attributes.Add(ValueAttribute("candidate", "owner 1 UDP 1 192.0.2.1 5000 typ host"));
        audio.Attributes.Add(ValueAttribute("remote-candidates", "1 192.0.2.3 7000"));
        audio.Attributes.Add(PropertyAttribute("end-of-candidates"));
        SdpMediaDescription video = description.MediaDescriptions[1];
        video.Attributes.Add(ValueAttribute("ice-ufrag", "video"));
        video.Attributes.Add(ValueAttribute("ice-pwd", "qwertyuiopasdfghjklzxc"));
        video.Attributes.Add(ValueAttribute("candidate", "video 1 UDP 1 192.0.2.2 6000 typ host"));
        video.Attributes.Add(PropertyAttribute("ice-mismatch"));
        ISdpIceService service = new SdpIceService();

        SdpIceDescription direct = service.ResolveMediaIce(description, 1);
        SdpIceDescription effective = service.ResolveEffectiveIce(description, 1);
        int ownerIndex = service.ResolveIceTransportMediaIndex(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Credentials!.UsernameFragment, Is.EqualTo("video"));
            Assert.That(direct.Candidates[0].Foundation, Is.EqualTo("video"));
            Assert.That(direct.IceMismatch, Is.True);
            Assert.That(ownerIndex, Is.Zero);
            Assert.That(effective.Credentials!.UsernameFragment, Is.EqualTo("owner"));
            Assert.That(effective.Candidates[0].Foundation, Is.EqualTo("owner"));
            Assert.That(effective.RemoteCandidates[0].Port, Is.EqualTo(7000));
            Assert.That(effective.EndOfCandidates, Is.True);
            Assert.That(effective.IceMismatch, Is.False);
            Assert.That(effective.IceLite, Is.True);
            Assert.That(effective.Options, Is.EqualTo(new[] { "trickle" }));
        });
    }

    [Test]
    public void ResolveEffectiveIce_FallsBackWhenBundleOwnershipCannotBeResolved()
    {
        SdpSessionDescription description = CreateBundledDescription("BUNDLE missing video");
        description.MediaDescriptions[1].Attributes.Add(ValueAttribute("ice-ufrag", "video"));
        description.MediaDescriptions[1].Attributes.Add(ValueAttribute("ice-pwd", "qwertyuiopasdfghjklzxc"));
        ISdpIceService service = new SdpIceService();

        int ownerIndex = service.ResolveIceTransportMediaIndex(description, 1);
        SdpIceDescription resolved = service.ResolveEffectiveIce(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(ownerIndex, Is.EqualTo(1));
            Assert.That(resolved.Credentials!.UsernameFragment, Is.EqualTo("video"));
        });
    }

    [Test]
    public void ResolveEffectiveIce_ReturnsIndependentProjectionGraphs()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(ValueAttribute("candidate", "one 1 UDP 1 192.0.2.1 5000 typ host network-cost 10"));
        description.MediaDescriptions.Add(media);
        ISdpIceService service = new SdpIceService();

        SdpIceDescription first = service.ResolveEffectiveIce(description, 0);
        SdpIceDescription second = service.ResolveEffectiveIce(description, 0);
        first.Candidates[0].Address = "203.0.113.1";
        first.Candidates[0].Extensions[0].Value = "20";

        Assert.Multiple(() =>
        {
            Assert.That(second.Candidates[0].Address, Is.EqualTo("192.0.2.1"));
            Assert.That(second.Candidates[0].Extensions[0].Value, Is.EqualTo("10"));
            Assert.That(media.Attributes[0].Value, Does.Contain("192.0.2.1"));
        });
    }

    [Test]
    public void ResolutionMethods_RejectNullDescriptionsAndInvalidMediaIndexes()
    {
        ISdpIceService service = new SdpIceService();
        SdpSessionDescription empty = new SdpSessionDescription();

        Assert.Multiple(() =>
        {
            Assert.That(() => service.ResolveMediaIce(null!, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => service.ResolveEffectiveIce(empty, -1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
            Assert.That(() => service.ResolveIceTransportMediaIndex(empty, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("mediaIndex"));
        });
    }

    [Test]
    public void AddCandidate_DeduplicatesAndEditsTheBundleTransportOwner()
    {
        SdpSessionDescription description = CreateBundledDescription("BUNDLE audio video");
        description.MediaDescriptions[0].Attributes.Add(PropertyAttribute("end-of-candidates"));
        ISdpIceService service = new SdpIceService();
        SdpIceCandidate candidate = HostCandidate("one", 1, 100u, "192.0.2.1", 5000);

        bool added = service.AddCandidate(description, 1, candidate);
        bool duplicate = service.AddCandidate(description, 1, candidate);
        SdpIceDescription resolved = service.ResolveEffectiveIce(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(resolved.Candidates, Has.Count.EqualTo(1));
            Assert.That(description.MediaDescriptions[0].Attributes[^2].Name, Is.EqualTo("candidate"));
            Assert.That(description.MediaDescriptions[0].Attributes[^1].Name, Is.EqualTo("end-of-candidates"));
            Assert.That(description.MediaDescriptions[1].Attributes.Any(x => x.Name == "candidate"), Is.False);
        });
    }

    [Test]
    public void RemoveCandidate_RemovesEveryEquivalentCandidateOnly()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(ValueAttribute("candidate", "one 1 UDP 100 192.0.2.1 5000 typ host"));
        media.Attributes.Add(ValueAttribute("candidate", "one 1 udp 100 192.0.2.1 5000 typ HOST"));
        media.Attributes.Add(ValueAttribute("candidate", "other 1 UDP 99 192.0.2.2 5001 typ host"));
        description.MediaDescriptions.Add(media);
        ISdpIceService service = new SdpIceService();

        int removed = service.RemoveCandidate(description, 0, HostCandidate("one", 1, 100u, "192.0.2.1", 5000));
        SdpIceDescription resolved = service.ResolveMediaIce(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(resolved.Candidates, Has.Count.EqualTo(1));
            Assert.That(resolved.Candidates[0].Foundation, Is.EqualTo("other"));
        });
    }

    [Test]
    public void RemoveCandidatesByFilter_CombinesFiltersAndPreservesMalformedCandidates()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(ValueAttribute("candidate", "udphost 1 UDP 30 192.0.2.1 5000 typ host"));
        media.Attributes.Add(ValueAttribute("candidate", "tcphost 1 TCP 20 192.0.2.2 5001 typ host tcptype passive"));
        media.Attributes.Add(ValueAttribute("candidate", "udprelay 2 UDP 10 192.0.2.3 5002 typ relay raddr 10.0.0.1 rport 4000"));
        media.Attributes.Add(ValueAttribute("candidate", "malformed"));
        description.MediaDescriptions.Add(media);
        ISdpIceService service = new SdpIceService();

        int removed = service.RemoveCandidatesByFilter(description, 0, 1, SdpIceTransportProtocol.Udp, SdpIceCandidateType.Host);
        SdpIceDescription resolved = service.ResolveMediaIce(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(resolved.Candidates.Select(x => x.Foundation), Is.EqualTo(new[] { "tcphost", "udprelay" }));
            Assert.That(media.Attributes.Any(x => x.Value == "malformed"), Is.True);
        });
    }

    [Test]
    public void SortCandidatesByPriority_IsDescendingStableAndPreservesOpaqueSlots()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(ValueAttribute("candidate", "low 1 UDP 10 192.0.2.1 5000 typ host"));
        media.Attributes.Add(ValueAttribute("candidate", "malformed"));
        media.Attributes.Add(ValueAttribute("candidate", "first 1 UDP 30 192.0.2.2 5001 typ host"));
        media.Attributes.Add(PropertyAttribute("x-marker"));
        media.Attributes.Add(ValueAttribute("candidate", "second 1 UDP 30 192.0.2.3 5002 typ host"));
        media.Attributes.Add(ValueAttribute("candidate", "middle 1 UDP 20 192.0.2.4 5003 typ host"));
        description.MediaDescriptions.Add(media);
        ISdpIceService service = new SdpIceService();

        service.SortCandidatesByPriority(description, 0);
        SdpIceDescription resolved = service.ResolveMediaIce(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Candidates.Select(x => x.Foundation), Is.EqualTo(new[] { "first", "second", "middle", "low" }));
            Assert.That(media.Attributes[1].Value, Is.EqualTo("malformed"));
            Assert.That(media.Attributes[3].Name, Is.EqualTo("x-marker"));
        });
    }

    [Test]
    public void MarkGatheringComplete_IsIdempotentAndPlacesOneMarkerLast()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(PropertyAttribute("end-of-candidates"));
        media.Attributes.Add(ValueAttribute("candidate", "one 1 UDP 10 192.0.2.1 5000 typ host"));
        media.Attributes.Add(PropertyAttribute("END-OF-CANDIDATES"));
        description.MediaDescriptions.Add(media);
        ISdpIceService service = new SdpIceService();

        service.MarkGatheringComplete(description, 0);
        service.MarkGatheringComplete(description, 0);
        SdpIceDescription resolved = service.ResolveMediaIce(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.EndOfCandidates, Is.True);
            Assert.That(media.Attributes.Count(x => x.Name.Equals("end-of-candidates", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(1));
            Assert.That(media.Attributes[^1].Name, Is.EqualTo("end-of-candidates"));
        });
    }

    [Test]
    public void SetRemoteCandidates_ReplacesAtomicallyAndCanClearBundleOwnerState()
    {
        SdpSessionDescription description = CreateBundledDescription("BUNDLE audio video");
        SdpMediaDescription owner = description.MediaDescriptions[0];
        owner.Attributes.Add(ValueAttribute("remote-candidates", "1 192.0.2.1 5000"));
        owner.Attributes.Add(ValueAttribute("remote-candidates", "2 192.0.2.2 5001"));
        ISdpIceService service = new SdpIceService();
        List<SdpRemoteCandidate> replacement = new List<SdpRemoteCandidate>
        {
            new SdpRemoteCandidate { Component = 1, Address = "203.0.113.1", Port = 6000 },
            new SdpRemoteCandidate { Component = 2, Address = "2001:db8::1", Port = 6001 }
        };

        service.SetRemoteCandidates(description, 1, replacement);
        SdpIceDescription replaced = service.ResolveEffectiveIce(description, 1);
        List<SdpRemoteCandidate> invalid = new List<SdpRemoteCandidate>
        {
            new SdpRemoteCandidate { Component = 1, Address = "203.0.113.2", Port = 7000 },
            new SdpRemoteCandidate { Component = 1, Address = "203.0.113.3", Port = 7001 }
        };
        Assert.That(() => service.SetRemoteCandidates(description, 1, invalid), Throws.ArgumentException);
        SdpIceDescription afterFailure = service.ResolveEffectiveIce(description, 1);
        service.SetRemoteCandidates(description, 1, new List<SdpRemoteCandidate>());
        SdpIceDescription cleared = service.ResolveEffectiveIce(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(replaced.RemoteCandidates.Select(x => x.Port), Is.EqualTo(new[] { 6000, 6001 }));
            Assert.That(owner.Attributes.Count(x => x.Name == "remote-candidates"), Is.Zero);
            Assert.That(afterFailure.RemoteCandidates.Select(x => x.Port), Is.EqualTo(new[] { 6000, 6001 }));
            Assert.That(cleared.RemoteCandidates, Is.Empty);
        });
    }

    [Test]
    public void CandidateMutations_RejectInvalidArgumentsWithoutChangingAttributes()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(new SdpMediaDescription());
        ISdpIceService service = new SdpIceService();
        SdpIceCandidate invalid = HostCandidate(string.Empty, 1, 1u, "192.0.2.1", 5000);

        Assert.Multiple(() =>
        {
            Assert.That(() => service.AddCandidate(description, 0, invalid), Throws.ArgumentException.With.Property("ParamName").EqualTo("candidate"));
            Assert.That(() => service.RemoveCandidate(description, 0, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("candidate"));
            Assert.That(() => service.RemoveCandidatesByFilter(description, 0, 257, SdpIceTransportProtocol.Unknown, SdpIceCandidateType.Unknown), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("component"));
            Assert.That(() => service.SetRemoteCandidates(description, 0, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("candidates"));
            Assert.That(description.MediaDescriptions[0].Attributes, Is.Empty);
        });
    }

    [Test]
    public void IsIceRestart_RequiresBothEffectiveCredentialValuesToChange()
    {
        SdpSessionDescription previous = new SdpSessionDescription();
        previous.Attributes.Add(ValueAttribute("ice-ufrag", "old1"));
        previous.Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));
        previous.MediaDescriptions.Add(new SdpMediaDescription());
        SdpSessionDescription current = new SdpSessionDescription();
        SdpAttribute currentUsername = ValueAttribute("ice-ufrag", "old1");
        SdpAttribute currentPassword = ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv");
        current.MediaDescriptions.Add(new SdpMediaDescription());
        current.MediaDescriptions[0].Attributes.Add(currentUsername);
        current.MediaDescriptions[0].Attributes.Add(currentPassword);
        ISdpIceService service = new SdpIceService();

        bool scopeChange = service.IsIceRestart(previous, 0, current, 0);
        currentPassword.Value = "zyxwvutsrqponmlkjihgfe";
        bool passwordOnly = service.IsIceRestart(previous, 0, current, 0);
        currentUsername.Value = "new1";
        bool bothChanged = service.IsIceRestart(previous, 0, current, 0);

        Assert.Multiple(() =>
        {
            Assert.That(scopeChange, Is.False);
            Assert.That(passwordOnly, Is.False);
            Assert.That(bothChanged, Is.True);
        });
    }

    [Test]
    public void RestartIce_ReplacesMediaCredentialsAndClearsStaleGatheringState()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute("ice-ufrag", "sess"));
        description.Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(PropertyAttribute("x-marker"));
        media.Attributes.Add(ValueAttribute("ice-ufrag", "old1"));
        media.Attributes.Add(ValueAttribute("ice-pwd", "zyxwvutsrqponmlkjihgfe"));
        media.Attributes.Add(ValueAttribute("candidate", "one 1 UDP 10 192.0.2.1 5000 typ host"));
        media.Attributes.Add(ValueAttribute("candidate", "malformed"));
        media.Attributes.Add(ValueAttribute("remote-candidates", "1 192.0.2.2 6000"));
        media.Attributes.Add(PropertyAttribute("end-of-candidates"));
        media.Attributes.Add(ValueAttribute("ice-options", "trickle"));
        media.Attributes.Add(PropertyAttribute("ice-mismatch"));
        description.MediaDescriptions.Add(media);
        ISdpIceService service = new SdpIceService();
        SdpIceCredentials credentials = new SdpIceCredentials { UsernameFragment = "new1", Password = "qwertyuiopasdfghjklzxc" };

        service.RestartIce(description, 0, credentials);
        SdpIceDescription resolved = service.ResolveMediaIce(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Credentials!.UsernameFragment, Is.EqualTo("new1"));
            Assert.That(resolved.Credentials.Password, Is.EqualTo("qwertyuiopasdfghjklzxc"));
            Assert.That(resolved.Candidates, Is.Empty);
            Assert.That(resolved.RemoteCandidates, Is.Empty);
            Assert.That(resolved.EndOfCandidates, Is.False);
            Assert.That(resolved.Options, Is.EqualTo(new[] { "trickle" }));
            Assert.That(resolved.IceMismatch, Is.True);
            Assert.That(media.Attributes.Count(x => x.Name == "ice-ufrag"), Is.EqualTo(1));
            Assert.That(media.Attributes.Count(x => x.Name == "ice-pwd"), Is.EqualTo(1));
            Assert.That(description.Attributes.Any(x => x.Value == "sess"), Is.True);
            Assert.That(media.Attributes[0].Name, Is.EqualTo("x-marker"));
        });
    }

    [Test]
    public void RestartIce_UpdatesOnlyTheBundleTransportOwner()
    {
        SdpSessionDescription description = CreateBundledDescription("BUNDLE audio video");
        SdpMediaDescription owner = description.MediaDescriptions[0];
        owner.Attributes.Add(ValueAttribute("ice-ufrag", "old1"));
        owner.Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));
        owner.Attributes.Add(ValueAttribute("candidate", "owner 1 UDP 10 192.0.2.1 5000 typ host"));
        owner.Attributes.Add(ValueAttribute("remote-candidates", "1 192.0.2.2 6000"));
        owner.Attributes.Add(PropertyAttribute("end-of-candidates"));
        SdpMediaDescription video = description.MediaDescriptions[1];
        video.Attributes.Add(ValueAttribute("ice-ufrag", "local"));
        video.Attributes.Add(ValueAttribute("ice-pwd", "zyxwvutsrqponmlkjihgfe"));
        video.Attributes.Add(ValueAttribute("candidate", "video 1 UDP 9 192.0.2.3 5001 typ host"));
        ISdpIceService service = new SdpIceService();

        service.RestartIce(description, 1, new SdpIceCredentials { UsernameFragment = "new1", Password = "qwertyuiopasdfghjklzxc" });
        SdpIceDescription effective = service.ResolveEffectiveIce(description, 1);
        SdpIceDescription directVideo = service.ResolveMediaIce(description, 1);

        Assert.Multiple(() =>
        {
            Assert.That(effective.Credentials!.UsernameFragment, Is.EqualTo("new1"));
            Assert.That(effective.Candidates, Is.Empty);
            Assert.That(effective.RemoteCandidates, Is.Empty);
            Assert.That(effective.EndOfCandidates, Is.False);
            Assert.That(directVideo.Credentials!.UsernameFragment, Is.EqualTo("local"));
            Assert.That(directVideo.Candidates[0].Foundation, Is.EqualTo("video"));
            Assert.That(owner.Attributes.Any(x => x.Value.Contains("owner", StringComparison.Ordinal)), Is.False);
        });
    }

    [Test]
    public void RestartIce_RejectsInvalidOrPartiallyChangedCredentialsAtomically()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(ValueAttribute("ice-ufrag", "old1"));
        media.Attributes.Add(ValueAttribute("ice-pwd", "abcdefghijklmnopqrstuv"));
        media.Attributes.Add(ValueAttribute("candidate", "one 1 UDP 10 192.0.2.1 5000 typ host"));
        description.MediaDescriptions.Add(media);
        ISdpIceService service = new SdpIceService();

        Assert.That(() => service.RestartIce(description, 0, new SdpIceCredentials { UsernameFragment = "old1", Password = "zyxwvutsrqponmlkjihgfe" }), Throws.ArgumentException.With.Property("ParamName").EqualTo("credentials"));
        Assert.That(() => service.RestartIce(description, 0, new SdpIceCredentials { UsernameFragment = "bad-", Password = "zyxwvutsrqponmlkjihgfe" }), Throws.ArgumentException.With.Property("ParamName").EqualTo("credentials"));
        SdpIceDescription resolved = service.ResolveMediaIce(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Credentials!.UsernameFragment, Is.EqualTo("old1"));
            Assert.That(resolved.Credentials.Password, Is.EqualTo("abcdefghijklmnopqrstuv"));
            Assert.That(resolved.Candidates, Has.Count.EqualTo(1));
            Assert.That(() => service.RestartIce(description, 0, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("credentials"));
            Assert.That(() => service.IsIceRestart(null!, 0, description, 0), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
        });
    }

    static SdpSessionDescription CreateBundledDescription(string groupValue)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.Attributes.Add(ValueAttribute("group", groupValue));
        description.Attributes.Add(ValueAttribute("ice-options", "trickle"));
        description.Attributes.Add(PropertyAttribute("ice-lite"));
        SdpMediaDescription audio = new SdpMediaDescription();
        audio.Attributes.Add(ValueAttribute("mid", "audio"));
        SdpMediaDescription video = new SdpMediaDescription();
        video.Attributes.Add(ValueAttribute("mid", "video"));
        description.MediaDescriptions.Add(audio);
        description.MediaDescriptions.Add(video);
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

    static SdpIceCandidate HostCandidate(string foundation, int component, uint priority, string address, int port)
    {
        return new SdpIceCandidate
        {
            Foundation = foundation,
            Component = component,
            TransportProtocol = SdpIceTransportProtocol.Udp,
            Priority = priority,
            Address = address,
            AddressKind = SdpAddressKind.Ipv4,
            Port = port,
            CandidateType = SdpIceCandidateType.Host
        };
    }
}
