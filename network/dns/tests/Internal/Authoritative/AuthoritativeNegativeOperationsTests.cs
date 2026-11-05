using System.Text;

namespace dns.Tests.Internal.Authoritative;

public class AuthoritativeNegativeOperationsTests
{
    [Test]
    public void ReturnsNoDataWithSoaForExistingName()
    {
        DnsZone zone = LoadZone(
            "example",
            "www 300 A 192.0.2.1\n");
        DnsAuthoritativeResult result = Service(zone).AnswerAuthoritatively(
            Request("www.example", DnsRecordType.Aaaa));

        AssertNegative(
            result,
            DnsAuthoritativeAnswerKind.NoData,
            DnsResponseCode.NoError);
    }

    [Test]
    public void TreatsEmptyNonterminalAsExistingNameWithNoData()
    {
        DnsZone zone = LoadZone(
            "example",
            "host.branch 300 A 192.0.2.2\n");
        DnsAuthoritativeResult result = Service(zone).AnswerAuthoritatively(
            Request("branch.example", DnsRecordType.Aaaa));

        AssertNegative(
            result,
            DnsAuthoritativeAnswerKind.NoData,
            DnsResponseCode.NoError);
    }

    [Test]
    public void ReturnsNameErrorWithSoaForNonexistentName()
    {
        DnsZone zone = LoadZone("example", string.Empty);
        DnsAuthoritativeResult result = Service(zone).AnswerAuthoritatively(
            Request("missing.example", DnsRecordType.A));

        AssertNegative(
            result,
            DnsAuthoritativeAnswerKind.NameError,
            DnsResponseCode.NameError);
    }

    [Test]
    public void UnknownQuestionTypeUsesNormalNoDataAndNameErrorSemantics()
    {
        DnsZone zone = LoadZone(
            "example",
            "www 300 A 192.0.2.9\n");
        DnsAuthoritativeService service = Service(zone);
        DnsAuthoritativeRequest existingRequest = Request(
            "www.example", (DnsRecordType)65400);
        existingRequest.Message.Questions[0].RecordTypeValue = 65400;
        DnsAuthoritativeRequest missingRequest = Request(
            "missing.example", (DnsRecordType)65400);
        missingRequest.Message.Questions[0].RecordTypeValue = 65400;

        DnsAuthoritativeResult existing =
            service.AnswerAuthoritatively(existingRequest);
        DnsAuthoritativeResult missing =
            service.AnswerAuthoritatively(missingRequest);

        Assert.Multiple(() =>
        {
            Assert.That(existing.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.NoData));
            Assert.That(existing.ResponseCode,
                Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(existing.Response!.Questions[0].RecordTypeValue,
                Is.EqualTo(65400));
            Assert.That(existing.AuthorityRecordSets, Has.Count.EqualTo(1));
            Assert.That(missing.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.NameError));
            Assert.That(missing.ResponseCode,
                Is.EqualTo(DnsResponseCode.NameError));
            Assert.That(missing.Response!.Questions[0].RecordTypeValue,
                Is.EqualTo(65400));
            Assert.That(missing.AuthorityRecordSets, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void DistinguishesWildcardNoDataFromBlockedWildcardNameError()
    {
        DnsZone zone = LoadZone(
            "example",
            "* 300 A 192.0.2.3\n" +
            "branch 300 TXT \"exists\"\n");
        DnsAuthoritativeService service = Service(zone);

        DnsAuthoritativeResult wildcardNoData =
            service.AnswerAuthoritatively(
                Request("host.example", DnsRecordType.Aaaa));
        DnsAuthoritativeResult blockedWildcard =
            service.AnswerAuthoritatively(
                Request("host.branch.example", DnsRecordType.A));

        Assert.Multiple(() =>
        {
            Assert.That(wildcardNoData.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.NoData));
            Assert.That(wildcardNoData.ResponseCode,
                Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(wildcardNoData.AuthorityRecordSets,
                Has.Count.EqualTo(1));
            Assert.That(blockedWildcard.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.NameError));
            Assert.That(blockedWildcard.ResponseCode,
                Is.EqualTo(DnsResponseCode.NameError));
            Assert.That(blockedWildcard.AuthorityRecordSets,
                Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void PreservesAliasKindForNegativeAliasTargets()
    {
        DnsZone zone = LoadZone(
            "example",
            "missing-alias 300 CNAME missing.example.\n" +
            "nodata 300 TXT \"exists\"\n" +
            "nodata-alias 300 CNAME nodata.example.\n");
        DnsAuthoritativeService service = Service(zone);

        DnsAuthoritativeResult nameError = service.AnswerAuthoritatively(
            Request("missing-alias.example", DnsRecordType.A));
        DnsAuthoritativeResult noData = service.AnswerAuthoritatively(
            Request("nodata-alias.example", DnsRecordType.A));

        Assert.Multiple(() =>
        {
            Assert.That(nameError.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Alias));
            Assert.That(nameError.ResponseCode,
                Is.EqualTo(DnsResponseCode.NameError));
            Assert.That(nameError.AnswerRecordSets.Select(
                set => set.RecordTypeValue), Is.EqualTo(new[] { 5 }));
            Assert.That(nameError.AuthorityRecordSets[0].RecordTypeValue,
                Is.EqualTo(6));
            Assert.That(noData.Kind,
                Is.EqualTo(DnsAuthoritativeAnswerKind.Alias));
            Assert.That(noData.ResponseCode,
                Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(noData.AnswerRecordSets.Select(
                set => set.RecordTypeValue), Is.EqualTo(new[] { 5 }));
            Assert.That(noData.AuthorityRecordSets[0].RecordTypeValue,
                Is.EqualTo(6));
        });
    }

    [Test]
    public void ReturnsAuthoritativeNoDataForMissingDsAtDelegationCut()
    {
        DnsZone zone = LoadZone(
            "example",
            "child 300 NS ns.child.example.\n" +
            "ns.child 300 A 192.0.2.4\n");
        DnsAuthoritativeResult result = Service(zone).AnswerAuthoritatively(
            Request("child.example", DnsRecordType.Ds));

        AssertNegative(
            result,
            DnsAuthoritativeAnswerKind.NoData,
            DnsResponseCode.NoError);
    }

    static void AssertNegative(
        DnsAuthoritativeResult result,
        DnsAuthoritativeAnswerKind kind,
        DnsResponseCode responseCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Kind, Is.EqualTo(kind));
            Assert.That(result.ResponseCode, Is.EqualTo(responseCode));
            Assert.That(result.Authoritative, Is.True);
            Assert.That(result.AnswerRecordSets, Is.Empty);
            Assert.That(result.AuthorityRecordSets, Has.Count.EqualTo(1));
            Assert.That(result.AuthorityRecordSets[0].RecordTypeValue,
                Is.EqualTo(6));
            Assert.That(result.AuthorityRecordSets[0].TimeToLive,
                Is.EqualTo(120));
            Assert.That(result.AuthorityRecordSets[0].Records[0].TimeToLive,
                Is.EqualTo(120));
            Assert.That(Text(result.AuthorityRecordSets[0].Name),
                Is.EqualTo("example."));
            Assert.That(result.Response!.Header.ResponseCode,
                Is.EqualTo(responseCode));
            Assert.That(result.Response.Header.AnswerCount, Is.EqualTo(0));
            Assert.That(result.Response.Header.AuthorityCount, Is.EqualTo(1));
            Assert.That(result.Response.Header.FlagsValue &
                (int)DnsHeaderFlag.AuthoritativeAnswer, Is.Not.Zero);
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
            "@ 600 SOA ns.outside. hostmaster.outside. " +
            "1 3600 600 86400 120\n" +
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
