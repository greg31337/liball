using System.Text;

namespace dns.Tests.Internal.Authoritative;

public class AuthoritativeEdnsOperationsTests
{
    [Test]
    public void NegotiatesPayloadCopiesDoAndReturnsMinimalBadVersion()
    {
        DnsAuthoritativeService service = Service(LoadZone(
            "www 300 A 192.0.2.1\n"));
        DnsAuthoritativeRequest supported = Request(
            "www.example",
            DnsRecordType.A);
        AddOpt(supported, 900, 0, (int)DnsEdnsFlag.DnssecAnswerOk);
        DnsAuthoritativeRequest unsupported = Request(
            "www.example",
            DnsRecordType.A);
        AddOpt(unsupported, 900, 1, 0);

        DnsAuthoritativeResult supportedResult =
            service.AnswerAuthoritatively(supported);
        DnsAuthoritativeResult unsupportedResult =
            service.AnswerAuthoritatively(unsupported);
        DnsEdnsData supportedEdns = ResponseEdns(supportedResult);
        DnsEdnsData unsupportedEdns = ResponseEdns(unsupportedResult);

        Assert.Multiple(() =>
        {
            Assert.That(supportedResult.Success, Is.True);
            Assert.That(supportedEdns.UdpPayloadSize, Is.EqualTo(1232));
            Assert.That(supportedEdns.FlagsValue &
                (int)DnsEdnsFlag.DnssecAnswerOk, Is.Not.Zero);
            Assert.That(supportedResult.ResponseBytes.Length,
                Is.LessThanOrEqualTo(900));
            Assert.That(unsupportedResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.BadVersion));
            Assert.That(unsupportedResult.ResponseCodeValue,
                Is.EqualTo(16));
            Assert.That(unsupportedResult.Authoritative, Is.False);
            Assert.That(unsupportedResult.Response!.Answers, Is.Empty);
            Assert.That(unsupportedResult.Response.Authorities, Is.Empty);
            Assert.That(unsupportedEdns.Version, Is.Zero);
            Assert.That(unsupportedEdns.ExtendedResponseCode, Is.EqualTo(1));
            Assert.That(ExtendedError(unsupportedResult)!.Code,
                Is.EqualTo(DnsExtendedErrorCode.NotSupported));
        });
    }

    [Test]
    public void AddsExtendedErrorForEdnsProtocolFailure()
    {
        DnsAuthoritativeService service = Service(LoadZone(string.Empty));
        DnsAuthoritativeRequest request = Request(
            "outside.invalid",
            DnsRecordType.A);
        AddOpt(request, 1232, 0, 0);

        DnsAuthoritativeResult result =
            service.AnswerAuthoritatively(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.Refused));
            Assert.That(ExtendedError(result)!.Code,
                Is.EqualTo(DnsExtendedErrorCode.NotAuthoritative));
            Assert.That(ResponseEdns(result).Options.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void MapsPolicyFailuresToExtendedErrors()
    {
        DnsAuthoritativeService service = Service(LoadZone(
            "a 300 CNAME b.example.\n" +
            "b 300 CNAME a.example.\n"));
        DnsAuthoritativeRequest malformed = Request(
            "example",
            DnsRecordType.Reserved);
        AddOpt(malformed, 1232, 0, 0);
        DnsAuthoritativeRequest unsupported = Request(
            "example",
            DnsRecordType.Axfr);
        AddOpt(unsupported, 1232, 0, 0);
        DnsAuthoritativeRequest failed = Request(
            "a.example",
            DnsRecordType.A);
        AddOpt(failed, 1232, 0, 0);

        DnsAuthoritativeResult malformedResult =
            service.AnswerAuthoritatively(malformed);
        DnsAuthoritativeResult unsupportedResult =
            service.AnswerAuthoritatively(unsupported);
        DnsAuthoritativeResult failedResult =
            service.AnswerAuthoritatively(failed);

        Assert.Multiple(() =>
        {
            Assert.That(malformedResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.FormatError));
            Assert.That(ExtendedError(malformedResult)!.Code,
                Is.EqualTo(DnsExtendedErrorCode.InvalidData));
            Assert.That(unsupportedResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.NotImplemented));
            Assert.That(ExtendedError(unsupportedResult)!.Code,
                Is.EqualTo(DnsExtendedErrorCode.NotSupported));
            Assert.That(failedResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.ServerFailure));
            Assert.That(ExtendedError(failedResult)!.Code,
                Is.EqualTo(DnsExtendedErrorCode.Other));
        });
    }

    [Test]
    public void IssuesRefreshesAndRejectsServerCookies()
    {
        DnsAuthoritativeService service = Service(LoadZone(
            "www 300 A 192.0.2.2\n"));
        DnsAuthoritativeRequest initial = Request(
            "www.example",
            DnsRecordType.A);
        SetClient(initial, [192, 0, 2, 10], 1000000);
        AddCookie(initial, ClientCookie(), []);

        DnsAuthoritativeResult issued =
            service.AnswerAuthoritatively(initial);
        DnsEdnsCookie issuedCookie = ResponseCookie(issued);
        DnsAuthoritativeRequest valid = Request(
            "www.example",
            DnsRecordType.A);
        SetClient(valid, [192, 0, 2, 10], 1001000);
        AddCookie(
            valid,
            issuedCookie.ClientCookie,
            issuedCookie.ServerCookie);
        DnsAuthoritativeRequest tampered = Request(
            "www.example",
            DnsRecordType.A);
        SetClient(tampered, [192, 0, 2, 10], 1001000);
        byte[] changed = issuedCookie.ServerCookie.ToArray();
        changed[15] = (byte)(changed[15] ^ 1);
        AddCookie(tampered, issuedCookie.ClientCookie, changed);

        DnsAuthoritativeResult accepted =
            service.AnswerAuthoritatively(valid);
        DnsAuthoritativeResult rejected =
            service.AnswerAuthoritatively(tampered);

        Assert.Multiple(() =>
        {
            Assert.That(issuedCookie.ClientCookie,
                Is.EqualTo(ClientCookie()));
            Assert.That(issuedCookie.ServerCookie, Has.Length.EqualTo(16));
            Assert.That(accepted.ResponseCode,
                Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(accepted.Response!.Answers, Is.Not.Empty);
            Assert.That(ResponseCookie(accepted).ServerCookie,
                Has.Length.EqualTo(16));
            Assert.That(rejected.ResponseCode,
                Is.EqualTo(DnsResponseCode.BadCookie));
            Assert.That(rejected.ResponseCodeValue, Is.EqualTo(23));
            Assert.That(rejected.Authoritative, Is.False);
            Assert.That(rejected.Response!.Answers, Is.Empty);
            Assert.That(ResponseCookie(rejected).ServerCookie,
                Has.Length.EqualTo(16));
            Assert.That(ExtendedError(rejected)!.Code,
                Is.EqualTo(DnsExtendedErrorCode.InvalidData));
        });
    }

    [Test]
    public void ValidCookieLiftsAmplificationCapAndTruncationKeepsOpt()
    {
        string text = new string('x', 200);
        DnsZone zone = LoadZone(
            "large 300 TXT \"" + text + "\"\n" +
            "large 300 TXT \"" + text.Replace('x', 'y') + "\"\n" +
            "large 300 TXT \"" + text.Replace('x', 'z') + "\"\n");
        DnsAuthoritativeService service = Service(zone);
        DnsAuthoritativeRequest limited = Request(
            "large.example",
            DnsRecordType.Txt);
        limited.Options.RequireCookieForAmplification = true;
        limited.MessageBytes = new byte[80];
        SetClient(limited, [192, 0, 2, 11], 2000000);
        AddCookie(limited, ClientCookie(), []);

        DnsAuthoritativeResult limitedResult =
            service.AnswerAuthoritatively(limited);
        DnsEdnsCookie serverCookie = ResponseCookie(limitedResult);
        DnsAuthoritativeRequest authenticated = Request(
            "large.example",
            DnsRecordType.Txt);
        authenticated.Options.RequireCookieForAmplification = true;
        authenticated.MessageBytes = new byte[80];
        SetClient(authenticated, [192, 0, 2, 11], 2001000);
        AddCookie(
            authenticated,
            serverCookie.ClientCookie,
            serverCookie.ServerCookie);

        DnsAuthoritativeResult authenticatedResult =
            service.AnswerAuthoritatively(authenticated);

        Assert.Multiple(() =>
        {
            Assert.That(limitedResult.Truncated, Is.True);
            Assert.That(limitedResult.ResponseBytes.Length,
                Is.LessThanOrEqualTo(240));
            Assert.That(limitedResult.Response!.Header.FlagsValue &
                (int)DnsHeaderFlag.Truncated, Is.Not.Zero);
            Assert.That(MessageOperations.FindOptRecord(
                limitedResult.Response), Is.Not.Null);
            Assert.That(authenticatedResult.Truncated, Is.False);
            Assert.That(authenticatedResult.ResponseBytes.Length,
                Is.GreaterThan(240));
            Assert.That(authenticatedResult.Response!.Answers,
                Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void DisabledEdnsTreatsOptAsClassicQuery()
    {
        DnsAuthoritativeService service = Service(LoadZone(
            "www 300 A 192.0.2.3\n"));
        DnsAuthoritativeRequest request = Request(
            "www.example",
            DnsRecordType.A);
        request.Options.EnableEdns = false;
        AddOpt(request, 4096, 1, (int)DnsEdnsFlag.DnssecAnswerOk);

        DnsAuthoritativeResult result =
            service.AnswerAuthoritatively(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.ResponseCode,
                Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(result.Response!.Answers, Has.Count.EqualTo(1));
            Assert.That(MessageOperations.FindOptRecord(result.Response),
                Is.Null);
        });
    }

    [Test]
    public void ClassicUdpUses512WhileEdnsUsesNegotiatedCeiling()
    {
        string text = new string('q', 200);
        DnsAuthoritativeService service = Service(LoadZone(
            "large 300 TXT \"" + text + "\"\n" +
            "large 300 TXT \"" + text.Replace('q', 'r') + "\"\n" +
            "large 300 TXT \"" + text.Replace('q', 's') + "\"\n"));
        DnsAuthoritativeRequest classic = Request(
            "large.example",
            DnsRecordType.Txt);
        DnsAuthoritativeRequest edns = Request(
            "large.example",
            DnsRecordType.Txt);
        AddOpt(edns, 900, 0, 0);

        DnsAuthoritativeResult classicResult =
            service.AnswerAuthoritatively(classic);
        DnsAuthoritativeResult ednsResult =
            service.AnswerAuthoritatively(edns);

        Assert.Multiple(() =>
        {
            Assert.That(classicResult.Truncated, Is.True);
            Assert.That(classicResult.ResponseBytes.Length,
                Is.LessThanOrEqualTo(512));
            Assert.That(ednsResult.Truncated, Is.False);
            Assert.That(ednsResult.ResponseBytes.Length,
                Is.GreaterThan(512));
            Assert.That(ednsResult.ResponseBytes.Length,
                Is.LessThanOrEqualTo(900));
        });
    }

    [Test]
    public void RejectsDuplicateOptAndMalformedCookieWithoutThrowing()
    {
        DnsAuthoritativeService service = Service(LoadZone(string.Empty));
        DnsAuthoritativeRequest duplicate = Request(
            "example",
            DnsRecordType.Soa);
        AddOpt(duplicate, 1232, 0, 0);
        AddOpt(duplicate, 1232, 0, 0);
        DnsAuthoritativeRequest malformed = Request(
            "example",
            DnsRecordType.Soa);
        AddCookie(malformed, new byte[7], []);

        DnsAuthoritativeResult duplicateResult =
            service.AnswerAuthoritatively(duplicate);
        DnsAuthoritativeResult malformedResult =
            service.AnswerAuthoritatively(malformed);

        Assert.Multiple(() =>
        {
            Assert.That(duplicateResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.FormatError));
            Assert.That(malformedResult.ResponseCode,
                Is.EqualTo(DnsResponseCode.FormatError));
            Assert.That(duplicateResult.Diagnostics, Is.Not.Empty);
            Assert.That(malformedResult.Diagnostics, Is.Not.Empty);
        });
    }

    static void AddOpt(
        DnsAuthoritativeRequest request,
        int payloadSize,
        int version,
        int flags)
    {
        DnsResourceRecord opt = MessageOperations.CreateOptRecord();
        opt.Class = (DnsClass)payloadSize;
        opt.ClassValue = payloadSize;
        opt.TimeToLive = ((uint)version << 16) | (uint)flags;
        opt.Data!.Edns!.UdpPayloadSize = payloadSize;
        opt.Data.Edns.Version = version;
        opt.Data.Edns.Flags = (DnsEdnsFlag)flags;
        opt.Data.Edns.FlagsValue = flags;
        request.Message.Additionals.Add(opt);
        request.Message.Header.AdditionalCount =
            request.Message.Additionals.Count;
    }

    static void AddCookie(
        DnsAuthoritativeRequest request,
        byte[] clientCookie,
        byte[] serverCookie)
    {
        AddOpt(request, 1232, 0, 0);
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = DnsEdnsOptionCode.Cookie;
        option.CodeValue = (int)DnsEdnsOptionCode.Cookie;
        option.Cookie = new DnsEdnsCookie();
        option.Cookie.ClientCookie = clientCookie.ToArray();
        option.Cookie.ServerCookie = serverCookie.ToArray();
        request.Message.Additionals[0].Data!.Edns!.Options.Add(option);
    }

    static void SetClient(
        DnsAuthoritativeRequest request,
        byte[] address,
        long unixTimeMilliseconds)
    {
        request.ClientEndpoint.Address = address;
        request.ClientEndpoint.HasAddress = true;
        request.ClientEndpoint.AddressFamily = 4;
        request.ReceivedAt.UnixTimeMilliseconds = unixTimeMilliseconds;
    }

    static byte[] ClientCookie()
    {
        return new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };
    }

    static DnsEdnsData ResponseEdns(DnsAuthoritativeResult result)
    {
        DnsResourceRecord? opt = MessageOperations.FindOptRecord(
            result.Response!);
        Assert.That(opt, Is.Not.Null);
        return opt!.Data!.Edns!;
    }

    static DnsEdnsCookie ResponseCookie(DnsAuthoritativeResult result)
    {
        return ResponseEdns(result).Options.Single(option =>
            option.CodeValue == (int)DnsEdnsOptionCode.Cookie).Cookie!;
    }

    static DnsEdnsExtendedError? ExtendedError(
        DnsAuthoritativeResult result)
    {
        return ResponseEdns(result).Options.FirstOrDefault(option =>
            option.CodeValue ==
                (int)DnsEdnsOptionCode.ExtendedDnsError)?.ExtendedError;
    }

    static DnsAuthoritativeService Service(DnsZone zone)
    {
        MemoryDnsZoneStore store = new MemoryDnsZoneStore();
        DnsZoneValidationResult added = store.AddZone(zone);
        Assert.That(added.Valid, Is.True,
            string.Join("; ", added.Diagnostics.Select(
                item => item.Reason)));
        return new DnsAuthoritativeService(store);
    }

    static DnsZone LoadZone(string records)
    {
        string source =
            "$ORIGIN example.\n" +
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
        foreach (string label in text.Split('.'))
        {
            if (label.Length > 0)
            {
                result.Labels.Add(Encoding.ASCII.GetBytes(label));
            }
        }

        return result;
    }
}
