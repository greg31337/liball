using System.Text;

namespace dns.Tests.Internal.Security;

public class TsigStreamOperationsTests
{
    [Test]
    public void SignsAndVerifiesFirstAndSubsequentTransferMessages()
    {
        DnsTsigKey key = Key();
        DnsClockValue now = Clock(1700000000);
        DnsTsigResult request = TsigOperations.Sign(
            Query(), key, [], now);
        DnsTsigStreamState signer = TsigStreamOperations.CreateState(
            key, request.Mac);
        DnsTsigStreamState verifier = TsigStreamOperations.CreateState(
            key, request.Mac);

        DnsTsigResult first = TsigStreamOperations.SignNext(
            signer, Response(1), now);
        DnsTsigResult second = TsigStreamOperations.SignNext(
            signer, Response(2), Clock(1700000001));
        DnsTsigResult third = TsigStreamOperations.SignNext(
            signer, Response(3), Clock(1700000002));
        DnsTsigResult firstCheck = TsigStreamOperations.VerifyNext(
            verifier, first.Message!, now);
        DnsTsigResult secondCheck = TsigStreamOperations.VerifyNext(
            verifier, second.Message!, Clock(1700000001));
        DnsTsigResult thirdCheck = TsigStreamOperations.VerifyNext(
            verifier, third.Message!, Clock(1700000002));
        DnsTsigResult completed = TsigStreamOperations.Complete(verifier);
        DnsTsigResult wrongFormula = TsigOperations.Verify(
            second.Message!, key, first.Mac, Clock(1700000001));

        Assert.Multiple(() =>
        {
            Assert.That(firstCheck.Authenticated, Is.True);
            Assert.That(secondCheck.Authenticated, Is.True);
            Assert.That(thirdCheck.Authenticated, Is.True);
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Authenticated, Is.True);
            Assert.That(completed.Mac, Is.EqualTo(third.Mac));
            Assert.That(verifier.MessageCount, Is.EqualTo(3));
            Assert.That(wrongFormula.Authenticated, Is.False);
            Assert.That(wrongFormula.Error,
                Is.EqualTo(DnsTsigError.BadSignature));
        });
    }

    [Test]
    public void CoversUnsignedIntermediatesWithTheNextMac()
    {
        DnsTsigKey key = Key();
        DnsClockValue now = Clock(1700000000);
        DnsTsigResult request = TsigOperations.Sign(
            Query(), key, [], now);
        DnsTsigStreamState signer = TsigStreamOperations.CreateState(
            key, request.Mac);
        DnsTsigStreamState verifier = TsigStreamOperations.CreateState(
            key, request.Mac);
        DnsTsigResult first = TsigStreamOperations.SignNext(
            signer, Response(1), now);
        TsigStreamOperations.VerifyNext(verifier, first.Message!, now);
        DnsMessage unsigned = Response(2);
        DnsTsigResult intermediary = TsigStreamOperations.VerifyNext(
            verifier, unsigned, Clock(1700000001));
        DnsTsigResult incomplete = TsigStreamOperations.Complete(verifier);
        DnsEncodeResult encodedUnsigned = DnsMessageEncoder.EncodeMessage(
            unsigned, new DnsEncodeOptions());
        DnsTsigResult final = TsigOperations.SignChained(
            Response(3),
            key,
            first.Mac,
            new List<byte[]> { encodedUnsigned.Bytes },
            Clock(1700000002));
        DnsTsigResult finalCheck = TsigStreamOperations.VerifyNext(
            verifier, final.Message!, Clock(1700000002));
        DnsTsigResult completed = TsigStreamOperations.Complete(verifier);

        Assert.Multiple(() =>
        {
            Assert.That(intermediary.Success, Is.True);
            Assert.That(intermediary.Authenticated, Is.False);
            Assert.That(incomplete.Success, Is.False);
            Assert.That(finalCheck.Authenticated, Is.True);
            Assert.That(completed.Success, Is.True);
            Assert.That(verifier.MessagesSinceSignature, Is.Zero);
            Assert.That(verifier.UnsignedMessages, Is.Empty);
        });
    }

    [Test]
    public void EnforcesSignedBoundariesIntervalAndMonotonicTime()
    {
        DnsTsigKey key = Key();
        DnsClockValue now = Clock(1700000000);
        DnsTsigResult request = TsigOperations.Sign(
            Query(), key, [], now);
        DnsTsigStreamState missingFirst = TsigStreamOperations.CreateState(
            key, request.Mac);
        DnsTsigResult firstFailure = TsigStreamOperations.VerifyNext(
            missingFirst, Response(1), now);
        DnsTsigStreamState verifier = TsigStreamOperations.CreateState(
            key, request.Mac);
        DnsTsigStreamState signer = TsigStreamOperations.CreateState(
            key, request.Mac);
        DnsTsigResult first = TsigStreamOperations.SignNext(
            signer, Response(1), Clock(1700000100));
        TsigStreamOperations.VerifyNext(
            verifier, first.Message!, Clock(1700000100));
        int index = 0;
        while (index < 99)
        {
            DnsTsigResult accepted = TsigStreamOperations.VerifyNext(
                verifier, Response(2), Clock(1700000100));
            Assert.That(accepted.Success, Is.True);
            index++;
        }

        DnsTsigResult intervalFailure = TsigStreamOperations.VerifyNext(
            verifier, Response(2), Clock(1700000100));
        DnsTsigResult decreasing = TsigStreamOperations.SignNext(
            signer, Response(2), Clock(1700000099));

        Assert.Multiple(() =>
        {
            Assert.That(firstFailure.Success, Is.False);
            Assert.That(firstFailure.Diagnostics[0].Field,
                Is.EqualTo("tsig.chain.first"));
            Assert.That(intervalFailure.Success, Is.False);
            Assert.That(intervalFailure.Diagnostics[0].Field,
                Is.EqualTo("tsig.chain.interval"));
            Assert.That(decreasing.Error, Is.EqualTo(DnsTsigError.BadTime));
        });
    }

    static DnsMessage Query()
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = Name("example");
        question.RecordType = DnsRecordType.Axfr;
        question.RecordTypeValue = 252;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = 0x4321;
        result.Questions.Add(question);
        return result;
    }

    static DnsMessage Response(int serial)
    {
        DnsMessage result = Query();
        result.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.AuthoritativeAnswer;
        result.Header.FlagsValue = (int)result.Header.Flags;
        DnsCharacterStringsData strings = new DnsCharacterStringsData();
        strings.Strings.Add(Encoding.ASCII.GetBytes(serial.ToString()));
        DnsRecordData data = new DnsRecordData();
        data.CharacterStrings = strings;
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = Name("example");
        record.RecordType = DnsRecordType.Txt;
        record.RecordTypeValue = 16;
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.TimeToLive = 300;
        record.Data = data;
        result.Answers.Add(record);
        return result;
    }

    static DnsTsigKey Key()
    {
        DnsTsigKey result = new DnsTsigKey();
        result.Name = Name("transfer-key", "example");
        result.AlgorithmName = Name("hmac-sha256");
        result.Secret = Encoding.ASCII.GetBytes("transfer secret material");
        return result;
    }

    static DnsClockValue Clock(long seconds)
    {
        DnsClockValue result = new DnsClockValue();
        result.UnixTimeMilliseconds = seconds * 1000;
        return result;
    }

    static DnsName Name(params string[] labels)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        foreach (string label in labels)
        {
            result.Labels.Add(Encoding.ASCII.GetBytes(label));
        }

        return result;
    }
}
