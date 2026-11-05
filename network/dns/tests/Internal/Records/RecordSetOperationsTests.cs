namespace dns.Tests.Internal.Records;

public class RecordSetOperationsTests
{
    private DnsRecordSetService service = null!;

    [SetUp]
    public void SetUp()
    {
        this.service = new DnsRecordSetService();
    }

    [Test]
    public void GroupingUsesCanonicalOwnerClassAndTypeWithMinimumTtl()
    {
        DnsResourceRecord first = Record("WwW.Example.", DnsRecordType.A, 1, 300, [2]);
        DnsResourceRecord second = Record("www.eXAMPLE.", DnsRecordType.A, 1, 120, [1]);
        DnsResourceRecord otherType = Record(
            "www.example.", DnsRecordType.Aaaa, 1, 200, [3]);

        List<DnsResourceRecordSet> sets = this.service.GroupRecordSets(
            new List<DnsResourceRecord> { first, second, otherType });
        first.Name.Labels[0][0] = 0x78;
        first.RawData[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(sets, Has.Count.EqualTo(2));
            Assert.That(Format(sets[0].Name), Is.EqualTo("www.example."));
            Assert.That(sets[0].RecordTypeValue, Is.EqualTo(1));
            Assert.That(sets[0].ClassValue, Is.EqualTo(1));
            Assert.That(sets[0].TimeToLive, Is.EqualTo(120));
            Assert.That(sets[0].Records, Has.Count.EqualTo(2));
            Assert.That(sets[0].Records[0].RawData, Is.EqualTo(new byte[] { 2 }));
            Assert.That(sets[1].RecordTypeValue, Is.EqualTo(28));
        });
    }

    [Test]
    public void NormalizationLowercasesSortsDeduplicatesAndNormalizesTtl()
    {
        DnsResourceRecordSet set = Set(
            Record("WwW.Example.", DnsRecordType.A, 1, 300, [3]),
            Record("www.example.", DnsRecordType.A, 1, 100, [1]),
            Record("WWW.EXAMPLE.", DnsRecordType.A, 1, 200, [1]),
            Record("www.example.", DnsRecordType.A, 1, 150, [2]));
        DnsCanonicalOrderOptions options = new DnsCanonicalOrderOptions();
        options.NormalizeRecordTtl = true;

        DnsResourceRecordSet result = this.service.NormalizeRecordSet(set, options);

        Assert.Multiple(() =>
        {
            Assert.That(Format(result.Name), Is.EqualTo("www.example."));
            Assert.That(result.TimeToLive, Is.EqualTo(100));
            Assert.That(result.Records, Has.Count.EqualTo(3));
            Assert.That(result.Records[0].RawData, Is.EqualTo(new byte[] { 1 }));
            Assert.That(result.Records[1].RawData, Is.EqualTo(new byte[] { 2 }));
            Assert.That(result.Records[2].RawData, Is.EqualTo(new byte[] { 3 }));
            Assert.That(result.Records, Has.All.Matches<DnsResourceRecord>(
                record => record.TimeToLive == 100));
            Assert.That(set.Records, Has.Count.EqualTo(4));
        });
    }

    [Test]
    public void DirectComparisonAndDuplicateSuppressionUseCanonicalOrder()
    {
        DnsResourceRecordSet first = Set(
            Record("a.example.", DnsRecordType.A, 1, 100, [2]),
            Record("A.EXAMPLE.", DnsRecordType.A, 1, 200, [2]));
        DnsResourceRecordSet second = Set(
            Record("z.example.", DnsRecordType.A, 1, 100, [1]));

        int forward = this.service.CompareRecordSets(first, second);
        int reverse = this.service.CompareRecordSets(second, first);
        DnsResourceRecordSet distinct =
            this.service.SuppressDuplicateRecords(first);

        Assert.Multiple(() =>
        {
            Assert.That(forward, Is.LessThan(0));
            Assert.That(reverse, Is.GreaterThan(0));
            Assert.That(distinct.Records, Has.Count.EqualTo(1));
            Assert.That(distinct.Records[0].RawData,
                Is.EqualTo(new byte[] { 2 }));
            Assert.That(first.Records, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void EqualityIsOrderIndependentAndHonorsTtlPolicy()
    {
        DnsResourceRecordSet left = Set(
            Record("Example.", DnsRecordType.A, 1, 100, [1]),
            Record("Example.", DnsRecordType.A, 1, 100, [2]));
        DnsResourceRecordSet right = Set(
            Record("example.", DnsRecordType.A, 1, 100, [2]),
            Record("example.", DnsRecordType.A, 1, 100, [1]));
        DnsCanonicalOrderOptions options = new DnsCanonicalOrderOptions();
        options.SortRecords = false;

        bool equal = this.service.RecordSetsEqual(left, right, options);
        right.Records[0].TimeToLive = 90;
        bool differentTtl = this.service.RecordSetsEqual(left, right, options);

        Assert.Multiple(() =>
        {
            Assert.That(equal, Is.True);
            Assert.That(differentTtl, Is.False);
        });
    }

    [Test]
    public void IntersectionAndDifferenceUseByteEquivalentSetMembers()
    {
        DnsResourceRecordSet left = Set(
            Record("example.", DnsRecordType.A, 1, 100, [1]),
            Record("example.", DnsRecordType.A, 1, 100, [2]),
            Record("example.", DnsRecordType.A, 1, 100, [3]));
        DnsResourceRecordSet right = Set(
            Record("EXAMPLE.", DnsRecordType.A, 1, 200, [2]),
            Record("EXAMPLE.", DnsRecordType.A, 1, 200, [4]));

        DnsResourceRecordSet intersection = this.service.IntersectRecordSets(left, right);
        DnsResourceRecordSet difference = this.service.DifferenceRecordSets(left, right);

        Assert.Multiple(() =>
        {
            Assert.That(intersection.Records, Has.Count.EqualTo(1));
            Assert.That(intersection.Records[0].RawData, Is.EqualTo(new byte[] { 2 }));
            Assert.That(difference.Records, Has.Count.EqualTo(2));
            Assert.That(difference.Records[0].RawData, Is.EqualTo(new byte[] { 1 }));
            Assert.That(difference.Records[1].RawData, Is.EqualTo(new byte[] { 3 }));
        });
    }

    [Test]
    public void CanonicalSetSortingUsesOwnerThenTypeThenClass()
    {
        DnsResourceRecordSet zA = Set(Record("z.example.", DnsRecordType.A, 1, 1, [1]));
        DnsResourceRecordSet aAaaa = Set(Record("a.example.", DnsRecordType.Aaaa, 1, 1, [1]));
        DnsResourceRecordSet aA = Set(Record("a.example.", DnsRecordType.A, 1, 1, [1]));

        List<DnsResourceRecordSet> sorted = this.service.SortRecordSets(
            new List<DnsResourceRecordSet> { zA, aAaaa, aA });

        Assert.Multiple(() =>
        {
            Assert.That(sorted[0], Is.SameAs(aA));
            Assert.That(sorted[1], Is.SameAs(aAaaa));
            Assert.That(sorted[2], Is.SameAs(zA));
        });
    }

    [Test]
    public void SectionSearchMatchesCanonicalOwnerAndRawCodes()
    {
        DnsMessage message = new DnsMessage();
        message.Answers.Add(Record("WwW.Example.", DnsRecordType.A, 1, 30, [1]));
        message.Answers.Add(Record("www.example.", DnsRecordType.Aaaa, 1, 30, [2]));
        message.Authorities.Add(Record("www.example.", DnsRecordType.A, 1, 30, [3]));

        List<DnsResourceRecordSet> found = this.service.FindRecordSets(
            message,
            DnsMessageSection.Answer,
            Parse("www.eXAMPLE."),
            DnsRecordType.A,
            0,
            DnsClass.Internet,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(found, Has.Count.EqualTo(1));
            Assert.That(found[0].Records[0].RawData, Is.EqualTo(new byte[] { 1 }));
        });
    }

    [Test]
    public void DirectAnswerProducesSuccessfulCanonicalResultAndTtl()
    {
        DnsMessage message = new DnsMessage();
        message.Answers.Add(Record("WwW.Example.", DnsRecordType.A, 1, 60, [192, 0, 2, 1]));

        DnsResolveResult result = this.service.ExtractAnswerChain(
            message,
            Question("www.example.", DnsRecordType.A),
            4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(Format(result.CanonicalName!), Is.EqualTo("www.example."));
            Assert.That(result.RecordSets, Has.Count.EqualTo(1));
            Assert.That(result.AliasSteps, Is.Empty);
            Assert.That(result.HasTimeToLive, Is.True);
            Assert.That(result.TimeToLive, Is.EqualTo(60));
        });
    }

    [Test]
    public void CnameChainFollowsTypedTargetsAndDetectsLoopsAndDepth()
    {
        DnsMessage message = new DnsMessage();
        message.Answers.Add(Alias("a.example.", DnsRecordType.Cname, "b.example."));
        message.Answers[0].TimeToLive = 10;
        message.Answers.Add(Alias("b.example.", DnsRecordType.Cname, "c.example."));
        message.Answers.Add(Record("c.example.", DnsRecordType.A, 1, 30, [1, 2, 3, 4]));

        DnsResolveResult success = this.service.ExtractAnswerChain(
            message, Question("a.example.", DnsRecordType.A), 2);
        DnsResolveResult depth = this.service.ExtractAnswerChain(
            message, Question("a.example.", DnsRecordType.A), 1);
        DnsMessage loopMessage = new DnsMessage();
        loopMessage.Answers.Add(Alias("a.example.", DnsRecordType.Cname, "b.example."));
        loopMessage.Answers.Add(Alias("b.example.", DnsRecordType.Cname, "a.example."));
        DnsResolveResult loop = this.service.ExtractAnswerChain(
            loopMessage, Question("a.example.", DnsRecordType.A), 4);

        Assert.Multiple(() =>
        {
            Assert.That(success.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(success.AliasSteps, Has.Count.EqualTo(2));
            Assert.That(Format(success.CanonicalName!), Is.EqualTo("c.example."));
            Assert.That(success.TimeToLive, Is.EqualTo(10));
            Assert.That(depth.Status,
                Is.EqualTo(DnsResolveStatus.DepthLimitExceeded));
            Assert.That(loop.Status, Is.EqualTo(DnsResolveStatus.Loop));
        });
    }

    [Test]
    public void ClosestDnameAncestorSynthesizesReplacementName()
    {
        DnsMessage message = new DnsMessage();
        message.Answers.Add(Alias("example.", DnsRecordType.Dname, "example.net."));
        message.Answers.Add(Record(
            "www.sub.example.net.", DnsRecordType.A, 1, 20, [1]));

        DnsResolveResult result = this.service.ExtractAnswerChain(
            message,
            Question("www.sub.example.", DnsRecordType.A),
            2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.Success));
            Assert.That(result.AliasSteps, Has.Count.EqualTo(1));
            Assert.That(result.AliasSteps[0].Synthesized, Is.True);
            Assert.That(Format(result.AliasSteps[0].Target),
                Is.EqualTo("www.sub.example.net."));
            Assert.That(Format(result.CanonicalName!),
                Is.EqualTo("www.sub.example.net."));
        });
    }

    [Test]
    public void MissingTypedAliasTargetIsAProtocolError()
    {
        DnsMessage message = new DnsMessage();
        message.Answers.Add(Record("a.example.", DnsRecordType.Cname, 1, 10, [1]));

        DnsResolveResult result = this.service.ExtractAnswerChain(
            message,
            Question("a.example.", DnsRecordType.A),
            2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DnsResolveStatus.ProtocolError));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("answerChain"));
        });
    }

    private static DnsResourceRecord Record(
        string owner,
        DnsRecordType type,
        int classValue,
        uint ttl,
        byte[] rawData)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Parse(owner);
        record.RecordType = type;
        record.Class = (DnsClass)classValue;
        record.TimeToLive = ttl;
        record.RawData = rawData;
        return record;
    }

    private static DnsResourceRecord Alias(
        string owner,
        DnsRecordType type,
        string target)
    {
        DnsResourceRecord record = Record(owner, type, 1, 60, [1]);
        record.Data = new DnsRecordData();
        record.Data.Name = new DnsNameData();
        record.Data.Name.Name = Parse(target);
        return record;
    }

    private static DnsResourceRecordSet Set(params DnsResourceRecord[] records)
    {
        DnsResourceRecordSet set = new DnsResourceRecordSet();
        set.Name = records[0].Name;
        set.RecordType = records[0].RecordType;
        set.Class = records[0].Class;
        set.TimeToLive = records[0].TimeToLive;
        set.Records.AddRange(records);
        return set;
    }

    private static DnsQuestion Question(string name, DnsRecordType type)
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse(name);
        question.RecordType = type;
        question.Class = DnsClass.Internet;
        return question;
    }

    private static DnsName Parse(string text)
    {
        return PresentationDnsName.ParseName(text, null).Name!;
    }

    private static string Format(DnsName name)
    {
        return PresentationDnsName.FormatName(name).Text!;
    }
}
