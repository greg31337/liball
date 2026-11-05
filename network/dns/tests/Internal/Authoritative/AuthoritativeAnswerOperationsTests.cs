using System.Text;

namespace dns.Tests.Internal.Authoritative;

public class AuthoritativeAnswerOperationsTests
{
    [Test]
    public void ReturnsExactRrsetAndSuppressesDuplicateRecords()
    {
        DnsZone zone = LoadZone(
            "example",
            "www 300 A 192.0.2.10\n");
        DnsZoneNode node = zone.Nodes.Single(item =>
            Text(item.Name) == "www.example.");
        node.RecordSets[0].Records.Add(node.RecordSets[0].Records[0]);
        DnsAuthoritativeService service = Service(zone);

        DnsAuthoritativeResult result = service.AnswerAuthoritatively(
            Request("www.example", DnsRecordType.A));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Positive));
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(result.AnswerRecordSets, Has.Count.EqualTo(1));
            Assert.That(result.AnswerRecordSets[0].Records,
                Has.Count.EqualTo(1));
            Assert.That(result.Response!.Answers, Has.Count.EqualTo(1));
            Assert.That(result.Response.Header.AnswerCount, Is.EqualTo(1));
            Assert.That(Text(result.Response.Answers[0].Name),
                Is.EqualTo("www.example."));
            Assert.That(result.ResponseBytes, Is.Not.Empty);
        });
    }

    [Test]
    public void FollowsCnameChainAcrossHostedZones()
    {
        DnsZone first = LoadZone(
            "example",
            "alias 300 CNAME middle.example.\n" +
            "middle 300 CNAME final.other.\n");
        DnsZone second = LoadZone(
            "other",
            "final 300 A 192.0.2.20\n");
        DnsAuthoritativeService service = Service(first, second);

        DnsAuthoritativeResult result = service.AnswerAuthoritatively(
            Request("alias.example", DnsRecordType.A));
        DnsAuthoritativeResult cnameOnly = service.AnswerAuthoritatively(
            Request("alias.example", DnsRecordType.Cname));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Alias));
            Assert.That(result.AnswerRecordSets.Select(
                set => set.RecordTypeValue),
                Is.EqualTo(new[] { 5, 5, 1 }));
            Assert.That(result.AnswerRecordSets.Select(
                set => Text(set.Name)),
                Is.EqualTo(new[]
                {
                    "alias.example.",
                    "middle.example.",
                    "final.other."
                }));
            Assert.That(result.Response!.Answers, Has.Count.EqualTo(3));
            Assert.That(cnameOnly.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Positive));
            Assert.That(cnameOnly.AnswerRecordSets, Has.Count.EqualTo(1));
            Assert.That(cnameOnly.AnswerRecordSets[0].RecordTypeValue,
                Is.EqualTo(5));
        });
    }

    [Test]
    public void AddsDnameAndSynthesizedCnameBeforeTargetAnswer()
    {
        DnsZone zone = LoadZone(
            "example",
            "branch 300 DNAME mapped.example.\n" +
            "host.mapped 300 A 192.0.2.30\n");
        DnsAuthoritativeService service = Service(zone);

        DnsAuthoritativeResult result = service.AnswerAuthoritatively(
            Request("host.branch.example", DnsRecordType.A));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Alias));
            Assert.That(result.AnswerRecordSets.Select(
                set => set.RecordTypeValue),
                Is.EqualTo(new[] { 39, 5, 1 }));
            Assert.That(Text(result.AnswerRecordSets[0].Name),
                Is.EqualTo("branch.example."));
            Assert.That(Text(result.AnswerRecordSets[1].Name),
                Is.EqualTo("host.branch.example."));
            Assert.That(Text(result.AnswerRecordSets[1]
                .Records[0].Data!.Name!.Name),
                Is.EqualTo("host.mapped.example."));
            Assert.That(Text(result.AnswerRecordSets[2].Name),
                Is.EqualTo("host.mapped.example."));
            Assert.That(result.Response!.Header.AnswerCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void SynthesizesWildcardOwnerButDoesNotOverrideExistingName()
    {
        DnsZone zone = LoadZone(
            "example",
            "*.wild 300 A 192.0.2.40\n" +
            "existing.wild 300 TXT \"present\"\n");
        DnsAuthoritativeService service = Service(zone);

        DnsAuthoritativeResult wildcard = service.AnswerAuthoritatively(
            Request("host.wild.example", DnsRecordType.A));
        DnsAuthoritativeResult existing = service.AnswerAuthoritatively(
            Request("existing.wild.example", DnsRecordType.A));

        Assert.Multiple(() =>
        {
            Assert.That(wildcard.AnswerRecordSets, Has.Count.EqualTo(1));
            Assert.That(Text(wildcard.AnswerRecordSets[0].Name),
                Is.EqualTo("host.wild.example."));
            Assert.That(Text(wildcard.Response!.Answers[0].Name),
                Is.EqualTo("host.wild.example."));
            Assert.That(existing.AnswerRecordSets, Is.Empty);
            Assert.That(existing.Response!.Answers, Is.Empty);
        });
    }

    [Test]
    public void FollowsSynthesizedWildcardCnameToExactTarget()
    {
        DnsZone zone = LoadZone(
            "example",
            "*.alias 300 CNAME target.example.\n" +
            "target 300 A 192.0.2.50\n");
        DnsAuthoritativeService service = Service(zone);

        DnsAuthoritativeResult result = service.AnswerAuthoritatively(
            Request("host.alias.example", DnsRecordType.A));

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Alias));
            Assert.That(result.AnswerRecordSets, Has.Count.EqualTo(2));
            Assert.That(result.AnswerRecordSets[0].RecordTypeValue,
                Is.EqualTo(5));
            Assert.That(Text(result.AnswerRecordSets[0].Name),
                Is.EqualTo("host.alias.example."));
            Assert.That(Text(result.AnswerRecordSets[1].Name),
                Is.EqualTo("target.example."));
        });
    }

    [Test]
    public void MinimizesAnyByDefaultAndCanReturnAllOwnerRecordSets()
    {
        DnsZone zone = LoadZone(
            "example",
            "www 300 A 192.0.2.51\n" +
            "www 300 TXT \"available\"\n");
        DnsAuthoritativeService service = Service(zone);
        DnsAuthoritativeRequest fullRequest = Request(
            "www.example", DnsRecordType.Any);
        fullRequest.Options.MinimizeAnyResponses = false;

        DnsAuthoritativeResult minimal = service.AnswerAuthoritatively(
            Request("www.example", DnsRecordType.Any));
        DnsAuthoritativeResult full = service.AnswerAuthoritatively(
            fullRequest);

        Assert.Multiple(() =>
        {
            Assert.That(minimal.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Positive));
            Assert.That(minimal.AnswerRecordSets, Has.Count.EqualTo(1));
            Assert.That(minimal.AnswerRecordSets[0].RecordType,
                Is.EqualTo(DnsRecordType.Hinfo));
            Assert.That(minimal.AnswerRecordSets[0].TimeToLive,
                Is.EqualTo(3600));
            Assert.That(minimal.AnswerRecordSets[0].Records[0]
                .Data!.CharacterStrings!.Strings[0],
                Is.EqualTo(Encoding.ASCII.GetBytes("RFC8482")));
            Assert.That(minimal.AnswerRecordSets[0].Records[0]
                .Data!.CharacterStrings!.Strings[1], Is.Empty);
            Assert.That(minimal.Response!.Answers, Has.Count.EqualTo(1));
            Assert.That(full.AnswerRecordSets.Select(
                set => set.RecordTypeValue),
                Is.EqualTo(new[] { 1, 16 }));
            Assert.That(full.Response!.Answers, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void SynthesizesAddressAdditionalsOnceForRepeatedMxTargets()
    {
        DnsZone zone = LoadZone(
            "example",
            "@ 300 MX 10 mail.example.\n" +
            "@ 300 MX 20 mail.example.\n" +
            "mail 300 A 192.0.2.52\n" +
            "mail 300 AAAA 2001:db8::52\n");
        DnsAuthoritativeService service = Service(zone);

        DnsAuthoritativeResult result = service.AnswerAuthoritatively(
            Request("example", DnsRecordType.Mx));

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Positive));
            Assert.That(result.AnswerRecordSets, Has.Count.EqualTo(1));
            Assert.That(result.AnswerRecordSets[0].Records,
                Has.Count.EqualTo(2));
            Assert.That(result.AdditionalRecordSets.Select(
                set => set.RecordTypeValue),
                Is.EqualTo(new[] { 1, 28 }));
            Assert.That(result.AdditionalRecordSets.Select(
                set => Text(set.Name)),
                Is.EqualTo(new[] { "mail.example.", "mail.example." }));
            Assert.That(result.Response!.Additionals, Has.Count.EqualTo(2));
            Assert.That(result.Response.Header.AdditionalCount,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void RecordCountLimitDropsAnEntireRrsetAndSynchronizesMetadata()
    {
        DnsZone zone = LoadZone(
            "example",
            "multi 300 TXT \"first\"\n" +
            "multi 300 TXT \"second\"\n");
        DnsAuthoritativeRequest request = Request(
            "multi.example", DnsRecordType.Txt);
        request.Options.MaximumResponseRecords = 1;

        DnsAuthoritativeResult result = Service(zone).AnswerAuthoritatively(
            request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Truncated, Is.True);
            Assert.That(result.AnswerRecordSets, Is.Empty);
            Assert.That(result.Response!.Answers, Is.Empty);
            Assert.That(result.Response.Header.AnswerCount, Is.EqualTo(0));
            Assert.That(result.Response.Header.Flags &
                DnsHeaderFlag.Truncated,
                Is.EqualTo(DnsHeaderFlag.Truncated));
            Assert.That(result.ResponseBytes, Is.Not.Empty);
        });
    }

    [Test]
    public void ReturnsServerFailureWithoutPartialAnswersForLoopOrDepthLimit()
    {
        DnsZone loopZone = LoadZone(
            "example",
            "a 300 CNAME b.example.\n" +
            "b 300 CNAME a.example.\n");
        DnsAuthoritativeService loopService = Service(loopZone);
        DnsAuthoritativeResult loop = loopService.AnswerAuthoritatively(
            Request("a.example", DnsRecordType.A));
        DnsZone depthZone = LoadZone(
            "depth",
            "a 300 CNAME b.depth.\n" +
            "b 300 CNAME c.depth.\n" +
            "c 300 A 192.0.2.60\n");
        DnsAuthoritativeService depthService = Service(depthZone);
        DnsAuthoritativeRequest depthRequest = Request(
            "a.depth", DnsRecordType.A);
        depthRequest.Options.MaximumAliasDepth = 1;
        DnsAuthoritativeResult depth =
            depthService.AnswerAuthoritatively(depthRequest);

        Assert.Multiple(() =>
        {
            Assert.That(loop.Success, Is.True);
            Assert.That(loop.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.ServerFailure));
            Assert.That(loop.ResponseCode,
                Is.EqualTo(DnsResponseCode.ServerFailure));
            Assert.That(loop.AnswerRecordSets, Is.Empty);
            Assert.That(loop.Response!.Answers, Is.Empty);
            Assert.That(loop.Diagnostics, Is.Not.Empty);
            Assert.That(depth.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.ServerFailure));
            Assert.That(depth.ResponseCode,
                Is.EqualTo(DnsResponseCode.ServerFailure));
            Assert.That(depth.Response!.Answers, Is.Empty);
        });
    }

    static DnsAuthoritativeService Service(params DnsZone[] zones)
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        foreach (DnsZone zone in zones)
        {
            DnsZoneValidationResult added = store.AddZone(zone);
            Assert.That(added.Valid, Is.True,
                string.Join("; ", added.Diagnostics.Select(
                    item => item.Reason)));
        }

        return new DnsAuthoritativeService(store);
    }

    static DnsZone LoadZone(string apex, string records)
    {
        string source =
            "$ORIGIN " + apex + ".\n" +
            "@ 300 SOA ns.outside. hostmaster.outside. " +
            "1 3600 600 86400 300\n" +
            "@ 300 NS ns.outside.\n" +
            records;
        DnsZoneLoadOptions options = new DnsZoneLoadOptions();
        options.ValidateZone = true;
        DnsZoneLoadResult loaded = new DnsZoneCodec().ParseZone(
            source,
            options);
        Assert.That(loaded.Success, Is.True,
            string.Join("; ", loaded.Diagnostics.Select(
                item => item.Reason)));
        return loaded.Zone!;
    }

    static DnsAuthoritativeRequest Request(
        string name,
        DnsRecordType recordType)
    {
        DnsAuthoritativeRequest result = new DnsAuthoritativeRequest();
        result.Message.Header.Opcode = DnsOpcode.Query;
        result.Message.Header.OpcodeValue = 0;
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse(name);
        question.RecordType = recordType;
        question.RecordTypeValue = (int)recordType;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        result.Message.Questions.Add(question);
        result.Message.Header.QuestionCount = 1;
        return result;
    }

    static DnsName Parse(string text)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        string[] labels = text.Split('.');
        foreach (string label in labels)
        {
            if (label.Length > 0)
            {
                result.Labels.Add(Encoding.ASCII.GetBytes(label));
            }
        }

        return result;
    }

    static string Text(DnsName name)
    {
        if (name.Labels.Count == 0)
        {
            return ".";
        }

        return string.Join(
            ".",
            name.Labels.Select(Encoding.ASCII.GetString)) + ".";
    }
}
