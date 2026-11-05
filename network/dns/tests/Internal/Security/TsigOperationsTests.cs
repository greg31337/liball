using System.Text;

namespace dns.Tests.Internal.Security;

public class TsigOperationsTests
{
    [TestCase("hmac-sha1", 20)]
    [TestCase("hmac-sha256", 32)]
    [TestCase("hmac-sha256-128", 16)]
    [TestCase("hmac-sha384", 48)]
    [TestCase("hmac-sha384-192", 24)]
    [TestCase("hmac-sha512", 64)]
    [TestCase("hmac-sha512-256", 32)]
    public void SignsAndVerifiesEverySupportedHmac(
        string algorithm,
        int expectedLength)
    {
        DnsMessage message = Query();
        DnsTsigKey key = Key("KEY", algorithm.ToUpperInvariant());
        DnsClockValue now = Clock(1700000000);
        DnsTsigService service = new DnsTsigService();

        DnsTsigResult signed = service.SignTsig(message, key, [], now);
        DnsTsigResult verified = service.VerifyTsig(
            signed.Message!,
            key,
            [],
            now);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(signed.Mac, Has.Length.EqualTo(expectedLength));
            Assert.That(message.Additionals, Is.Empty);
            Assert.That(signed.Message!.Additionals, Has.Count.EqualTo(1));
            Assert.That(signed.Message.Additionals[0].Class,
                Is.EqualTo(DnsClass.Any));
            Assert.That(signed.Message.Additionals[0].TimeToLive,
                Is.Zero);
            Assert.That(Text(signed.KeyName!), Is.EqualTo("key.example."));
            Assert.That(Text(signed.Tsig!.AlgorithmName),
                Is.EqualTo(algorithm + "."));
            Assert.That(verified.Success, Is.True);
            Assert.That(verified.Authenticated, Is.True);
            Assert.That(verified.Error, Is.EqualTo(DnsTsigError.NoError));
            Assert.That(verified.Mac, Is.EqualTo(signed.Mac));
        });
    }

    [Test]
    public void ProducesByteExactSha256RequestMac()
    {
        DnsMessage message = Query();
        DnsTsigKey key = Key("key", "hmac-sha256");

        DnsTsigResult result = new DnsTsigService().SignTsig(
            message,
            key,
            [],
            Clock(1700000000));

        Assert.That(result.Mac, Is.EqualTo(Convert.FromHexString(
            "307a328352f2668c8ae687ee4e9e2d9" +
            "e49a1459cc0aa9c73b0ee5094a6f897ed")));
    }

    [Test]
    public void IncludesRequestMacWhenSigningAndVerifyingResponse()
    {
        DnsTsigService service = new DnsTsigService();
        DnsTsigKey key = Key("key", "hmac-sha256");
        DnsClockValue now = Clock(1700000000);
        DnsTsigResult request = service.SignTsig(Query(), key, [], now);
        DnsMessage response = Query();
        response.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.RecursionDesired |
            DnsHeaderFlag.RecursionAvailable;

        DnsTsigResult signed = service.SignTsig(
            response,
            key,
            request.Mac,
            now);
        DnsTsigResult verified = service.VerifyTsig(
            signed.Message!,
            key,
            request.Mac,
            now);
        DnsTsigResult missingPrior = service.VerifyTsig(
            signed.Message!,
            key,
            [],
            now);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Success, Is.True);
            Assert.That(signed.PriorMac, Is.EqualTo(request.Mac));
            Assert.That(verified.Authenticated, Is.True);
            Assert.That(missingPrior.Success, Is.True);
            Assert.That(missingPrior.Authenticated, Is.False);
            Assert.That(missingPrior.Error,
                Is.EqualTo(DnsTsigError.BadSignature));
        });
    }

    [Test]
    public void DetectsMessageMacKeyAndOriginalIdChanges()
    {
        DnsTsigService service = new DnsTsigService();
        DnsTsigKey key = Key("key", "hmac-sha256");
        DnsClockValue now = Clock(1700000000);
        DnsTsigResult signed = service.SignTsig(Query(), key, [], now);
        DnsMessage changedMessage = MessageOperations.CloneMessage(
            signed.Message!);
        changedMessage.Questions[0].RecordType = DnsRecordType.Aaaa;
        changedMessage.Questions[0].RecordTypeValue = 28;
        DnsMessage changedMac = MessageOperations.CloneMessage(
            signed.Message!);
        changedMac.Additionals[0].Data!.Tsig!.Mac[0] =
            (byte)(changedMac.Additionals[0].Data!.Tsig!.Mac[0] ^ 1);
        DnsMessage changedLastMac = MessageOperations.CloneMessage(
            signed.Message!);
        byte[] lastMac = changedLastMac.Additionals[0].Data!.Tsig!.Mac;
        lastMac[lastMac.Length - 1] =
            (byte)(lastMac[lastMac.Length - 1] ^ 1);
        DnsMessage changedId = MessageOperations.CloneMessage(
            signed.Message!);
        changedId.Additionals[0].Data!.Tsig!.OriginalIdentifier++;

        DnsTsigResult messageFailure = service.VerifyTsig(
            changedMessage, key, [], now);
        DnsTsigResult macFailure = service.VerifyTsig(
            changedMac, key, [], now);
        DnsTsigResult lastMacFailure = service.VerifyTsig(
            changedLastMac, key, [], now);
        DnsTsigResult idFailure = service.VerifyTsig(
            changedId, key, [], now);
        DnsTsigResult keyFailure = service.VerifyTsig(
            signed.Message!, Key("other", "hmac-sha256"), [], now);

        Assert.Multiple(() =>
        {
            Assert.That(messageFailure.Error,
                Is.EqualTo(DnsTsigError.BadSignature));
            Assert.That(macFailure.Error,
                Is.EqualTo(DnsTsigError.BadSignature));
            Assert.That(lastMacFailure.Error,
                Is.EqualTo(DnsTsigError.BadSignature));
            Assert.That(idFailure.Error,
                Is.EqualTo(DnsTsigError.BadSignature));
            Assert.That(keyFailure.Error, Is.EqualTo(DnsTsigError.BadKey));
            Assert.That(messageFailure.Authenticated, Is.False);
            Assert.That(macFailure.Authenticated, Is.False);
            Assert.That(lastMacFailure.Authenticated, Is.False);
            Assert.That(idFailure.Authenticated, Is.False);
            Assert.That(keyFailure.Authenticated, Is.False);
            Assert.That(lastMacFailure.Diagnostics[0].Code,
                Is.EqualTo(macFailure.Diagnostics[0].Code));
            Assert.That(lastMacFailure.Diagnostics[0].Field,
                Is.EqualTo(macFailure.Diagnostics[0].Field));
            Assert.That(lastMacFailure.Diagnostics[0].Reason,
                Is.EqualTo(macFailure.Diagnostics[0].Reason));
        });
    }

    [Test]
    public void AppliesFudgeKeyValidityAndTruncationPolicies()
    {
        DnsTsigService service = new DnsTsigService();
        DnsTsigKey key = Key("key", "hmac-sha256");
        key.MacLength = 16;
        DnsClockValue signedAt = Clock(1700000000);
        DnsTsigResult signed = service.SignTsig(
            Query(), key, [], signedAt);
        DnsTsigResult boundary = service.VerifyTsig(
            signed.Message!, key, [], Clock(1700000300));
        DnsTsigResult expired = service.VerifyTsig(
            signed.Message!, key, [], Clock(1700000301));
        DnsTsigKey strongerPolicy = Key("key", "hmac-sha256");
        strongerPolicy.MacLength = 24;
        DnsTsigResult truncated = service.VerifyTsig(
            signed.Message!, strongerPolicy, [], signedAt);
        DnsTsigKey unavailable = Key("key", "hmac-sha256");
        unavailable.HasValidFrom = true;
        unavailable.ValidFrom = 1700000001;
        DnsTsigResult invalidKey = service.VerifyTsig(
            signed.Message!, unavailable, [], signedAt);

        Assert.Multiple(() =>
        {
            Assert.That(signed.Mac, Has.Length.EqualTo(16));
            Assert.That(boundary.Authenticated, Is.True);
            Assert.That(expired.Success, Is.True);
            Assert.That(expired.Authenticated, Is.True);
            Assert.That(expired.Error, Is.EqualTo(DnsTsigError.BadTime));
            Assert.That(truncated.Error,
                Is.EqualTo(DnsTsigError.BadTruncation));
            Assert.That(invalidKey.Error, Is.EqualTo(DnsTsigError.BadKey));
        });
    }

    [Test]
    public void RejectsUnsupportedAlgorithmsAndMissingTsig()
    {
        DnsTsigService service = new DnsTsigService();
        DnsTsigResult unsupported = service.SignTsig(
            Query(),
            Key("key", "hmac-md5.sig-alg.reg.int"),
            [],
            Clock(1700000000));
        DnsTsigResult missing = service.VerifyTsig(
            Query(),
            Key("key", "hmac-sha256"),
            [],
            Clock(1700000000));

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupported.Error, Is.EqualTo(DnsTsigError.BadKey));
            Assert.That(unsupported.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.UnsupportedAlgorithm));
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidTsigPlacement));
        });
    }

    static DnsMessage Query()
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = Name("www", "example");
        question.RecordType = DnsRecordType.A;
        question.RecordTypeValue = 1;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = 0x1234;
        result.Header.Flags = DnsHeaderFlag.RecursionDesired;
        result.Questions.Add(question);
        return result;
    }

    static DnsTsigKey Key(string keyLabel, string algorithm)
    {
        DnsTsigKey result = new DnsTsigKey();
        result.Name = Name(keyLabel, "example");
        result.AlgorithmName = Name(algorithm.Split('.'));
        result.Secret = Encoding.ASCII.GetBytes("topsecret");
        result.Fudge = 300;
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

    static string Text(DnsName name)
    {
        return string.Join(".", name.Labels.Select(
            label => Encoding.ASCII.GetString(label))) + ".";
    }
}
