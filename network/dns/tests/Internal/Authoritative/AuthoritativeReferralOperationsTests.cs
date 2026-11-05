using System.Text;

namespace dns.Tests.Internal.Authoritative;

public class AuthoritativeReferralOperationsTests
{
    [Test]
    public void ReturnsReferralWithMandatoryInBailiwickAddressGlue()
    {
        DnsZone zone = LoadZone(
            "example",
            "child 300 NS ns.child.example.\n" +
            "ns.child 300 A 192.0.2.1\n" +
            "ns.child 300 AAAA 2001:db8::1\n");
        DnsAuthoritativeService service = Service(zone);
        DnsAuthoritativeRequest request = Request(
            "host.child.example", DnsRecordType.A);
        request.Options.IncludeAdditionalRecords = false;
        request.Options.MinimalResponses = true;

        DnsAuthoritativeResult result =
            service.AnswerAuthoritatively(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Referral));
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(result.Authoritative, Is.False);
            Assert.That(result.AnswerRecordSets, Is.Empty);
            Assert.That(result.AuthorityRecordSets, Has.Count.EqualTo(1));
            Assert.That(result.AuthorityRecordSets[0].RecordTypeValue,
                Is.EqualTo(2));
            Assert.That(Text(result.AuthorityRecordSets[0].Name),
                Is.EqualTo("child.example."));
            Assert.That(result.AdditionalRecordSets.Select(
                set => set.RecordTypeValue),
                Is.EqualTo(new[] { 1, 28 }));
            Assert.That(result.Response!.Header.AnswerCount, Is.EqualTo(0));
            Assert.That(result.Response.Header.AuthorityCount, Is.EqualTo(1));
            Assert.That(result.Response.Header.AdditionalCount, Is.EqualTo(2));
            Assert.That(result.Response.Header.FlagsValue &
                (int)DnsHeaderFlag.AuthoritativeAnswer, Is.Zero);
        });
    }

    [Test]
    public void OmitsOutOfBailiwickAddressesFromReferralGlue()
    {
        DnsZone zone = LoadZone(
            "example",
            "child 300 NS ns.child.example.\n" +
            "child 300 NS ns.example.\n" +
            "ns.child 300 A 192.0.2.4\n" +
            "ns 300 A 192.0.2.5\n");

        DnsAuthoritativeResult result = Service(zone).AnswerAuthoritatively(
            Request("host.child.example", DnsRecordType.A));

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Referral));
            Assert.That(result.Authoritative, Is.False);
            Assert.That(result.AuthorityRecordSets[0].Records,
                Has.Count.EqualTo(2));
            Assert.That(result.AdditionalRecordSets,
                Has.Count.EqualTo(1));
            Assert.That(Text(result.AdditionalRecordSets[0].Name),
                Is.EqualTo("ns.child.example."));
            Assert.That(result.Response!.Additionals, Has.Count.EqualTo(1));
            Assert.That(Text(result.Response.Additionals[0].Name),
                Is.EqualTo("ns.child.example."));
            Assert.That(result.Response.Header.AdditionalCount,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void AnswersDsAtDelegationCutButRefersOtherCutTypes()
    {
        DnsZone zone = LoadZone(
            "example",
            "child 300 NS ns.child.example.\n" +
            "child 300 DS 12345 8 2 " +
            "0123456789abcdef0123456789abcdef" +
            "0123456789abcdef0123456789abcdef\n" +
            "ns.child 300 A 192.0.2.2\n");
        DnsAuthoritativeService service = Service(zone);

        DnsAuthoritativeResult ds = service.AnswerAuthoritatively(
            Request("child.example", DnsRecordType.Ds));
        DnsAuthoritativeResult address = service.AnswerAuthoritatively(
            Request("child.example", DnsRecordType.A));

        Assert.Multiple(() =>
        {
            Assert.That(ds.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Positive));
            Assert.That(ds.Authoritative, Is.True);
            Assert.That(ds.AnswerRecordSets, Has.Count.EqualTo(1));
            Assert.That(ds.AnswerRecordSets[0].RecordTypeValue,
                Is.EqualTo(43));
            Assert.That(ds.AuthorityRecordSets, Is.Empty);
            Assert.That(ds.Response!.Header.FlagsValue &
                (int)DnsHeaderFlag.AuthoritativeAnswer, Is.Not.Zero);
            Assert.That(address.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Referral));
            Assert.That(address.AnswerRecordSets, Is.Empty);
            Assert.That(address.AuthorityRecordSets[0].RecordTypeValue,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void KeepsAliasAnswerAuthoritativeWhenTargetEndsAtReferral()
    {
        DnsZone zone = LoadZone(
            "example",
            "alias 300 CNAME host.child.example.\n" +
            "child 300 NS ns.child.example.\n" +
            "ns.child 300 A 192.0.2.3\n");
        DnsAuthoritativeService service = Service(zone);

        DnsAuthoritativeResult result = service.AnswerAuthoritatively(
            Request("alias.example", DnsRecordType.A));

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Alias));
            Assert.That(result.Authoritative, Is.True);
            Assert.That(result.AnswerRecordSets.Select(
                set => set.RecordTypeValue), Is.EqualTo(new[] { 5 }));
            Assert.That(result.AuthorityRecordSets.Select(
                set => set.RecordTypeValue), Is.EqualTo(new[] { 2 }));
            Assert.That(result.AdditionalRecordSets.Select(
                set => set.RecordTypeValue), Is.EqualTo(new[] { 1 }));
            Assert.That(result.Response!.Header.FlagsValue &
                (int)DnsHeaderFlag.AuthoritativeAnswer, Is.Not.Zero);
        });
    }

    [Test]
    public void AddsAddressesForNsMxSrvSvcbAndHttpsTargets()
    {
        DnsZone zone = LoadZone(
            "example",
            "@ 300 NS ns.example.\n" +
            "ns 300 A 192.0.2.10\n" +
            "mail 300 MX 10 mx.example.\n" +
            "mail 300 MX 20 mx.example.\n" +
            "mx 300 A 192.0.2.20\n" +
            "_service._tcp 300 SRV 0 0 443 service.example.\n" +
            "service 300 AAAA 2001:db8::30\n" +
            "binding 300 SVCB 1 svc.example.\n" +
            "svc 300 A 192.0.2.40\n" +
            "secure 300 HTTPS 1 web.example.\n" +
            "web 300 AAAA 2001:db8::50\n");
        DnsAuthoritativeService service = Service(zone);

        DnsAuthoritativeResult ns = service.AnswerAuthoritatively(
            Request("example", DnsRecordType.Ns));
        DnsAuthoritativeResult mx = service.AnswerAuthoritatively(
            Request("mail.example", DnsRecordType.Mx));
        DnsAuthoritativeResult srv = service.AnswerAuthoritatively(
            Request("_service._tcp.example", DnsRecordType.Srv));
        DnsAuthoritativeResult svcb = service.AnswerAuthoritatively(
            Request("binding.example", DnsRecordType.Svcb));
        DnsAuthoritativeResult https = service.AnswerAuthoritatively(
            Request("secure.example", DnsRecordType.Https));

        Assert.Multiple(() =>
        {
            Assert.That(AdditionalNames(ns),
                Does.Contain("ns.example."));
            Assert.That(AdditionalNames(mx),
                Is.EqualTo(new[] { "mx.example." }));
            Assert.That(mx.AdditionalRecordSets, Has.Count.EqualTo(1));
            Assert.That(AdditionalNames(srv),
                Is.EqualTo(new[] { "service.example." }));
            Assert.That(srv.AdditionalRecordSets[0].RecordTypeValue,
                Is.EqualTo(28));
            Assert.That(AdditionalNames(svcb),
                Is.EqualTo(new[] { "svc.example." }));
            Assert.That(AdditionalNames(https),
                Is.EqualTo(new[] { "web.example." }));
            Assert.That(mx.Response!.Header.AdditionalCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void OmitsOptionalAddressesUnderPolicyOrWithoutHostedTarget()
    {
        DnsZone zone = LoadZone(
            "example",
            "mail 300 MX 10 mx.example.\n" +
            "mx 300 A 192.0.2.60\n" +
            "outside 300 MX 10 mail.other.\n" +
            "nullmx 300 MX 0 .\n" +
            "_service._tcp 300 SRV 0 0 443 service.example.\n" +
            "service 300 A 192.0.2.70\n");
        DnsAuthoritativeService service = Service(zone);
        DnsAuthoritativeRequest disabledRequest = Request(
            "mail.example", DnsRecordType.Mx);
        disabledRequest.Options.IncludeAdditionalRecords = false;
        DnsAuthoritativeRequest minimalRequest = Request(
            "_service._tcp.example", DnsRecordType.Srv);
        minimalRequest.Options.MinimalResponses = true;

        DnsAuthoritativeResult disabled =
            service.AnswerAuthoritatively(disabledRequest);
        DnsAuthoritativeResult minimal =
            service.AnswerAuthoritatively(minimalRequest);
        DnsAuthoritativeResult outside = service.AnswerAuthoritatively(
            Request("outside.example", DnsRecordType.Mx));
        DnsAuthoritativeResult nullMx = service.AnswerAuthoritatively(
            Request("nullmx.example", DnsRecordType.Mx));

        Assert.Multiple(() =>
        {
            Assert.That(disabled.AnswerRecordSets, Has.Count.EqualTo(1));
            Assert.That(disabled.AdditionalRecordSets, Is.Empty);
            Assert.That(disabled.Response!.Additionals, Is.Empty);
            Assert.That(minimal.AdditionalRecordSets, Is.Empty);
            Assert.That(outside.AdditionalRecordSets, Is.Empty);
            Assert.That(nullMx.AdditionalRecordSets, Is.Empty);
        });
    }

    static string[] AdditionalNames(DnsAuthoritativeResult result)
    {
        return result.AdditionalRecordSets.Select(
            set => Text(set.Name)).ToArray();
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
