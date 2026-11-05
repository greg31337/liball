namespace dns.Tests.Internal.Messages;

public class DiagnosticEdnsMessageTests
{
    [Test]
    public void RejectsResponseOnlyOptionsInRequests()
    {
        DnsEdnsOption error = Error("failure");
        DnsEdnsOption report = Option(DnsEdnsOptionCode.ReportChannel);
        report.ReportChannel = Parse("agent.example.");

        DnsEncodeResult errorResult = Encode(
            Message(false, error));
        DnsEncodeResult reportResult = Encode(
            Message(false, report));

        Assert.Multiple(() =>
        {
            Assert.That(errorResult.Success, Is.False);
            Assert.That(errorResult.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.extendedError"));
            Assert.That(reportResult.Success, Is.False);
            Assert.That(reportResult.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.reportChannel"));
        });
    }

    [Test]
    public void DecodeEnforcesRequestAndResponseOptionRoles()
    {
        DnsEncodeResult errorResponse = Encode(
            Message(true, Error("failure")));
        DnsEncodeResult populatedVersionResponse = Encode(
            Message(true, ZoneVersion(
                true, 2, 0, [0, 0, 0, 1])));
        DnsEncodeResult emptyVersionRequest = Encode(
            Message(false, ZoneVersion(false, 0, 0, [])));
        byte[] errorRequestWire = errorResponse.Bytes.ToArray();
        errorRequestWire[2] = (byte)(errorRequestWire[2] & 127);
        byte[] populatedRequestWire =
            populatedVersionResponse.Bytes.ToArray();
        populatedRequestWire[2] =
            (byte)(populatedRequestWire[2] & 127);
        byte[] emptyResponseWire = emptyVersionRequest.Bytes.ToArray();
        emptyResponseWire[2] = (byte)(emptyResponseWire[2] | 128);

        DnsDecodeResult errorRequest = Decode(errorRequestWire);
        DnsDecodeResult populatedRequest = Decode(populatedRequestWire);
        DnsDecodeResult emptyResponse = Decode(emptyResponseWire);

        Assert.Multiple(() =>
        {
            Assert.That(errorRequest.Success, Is.False);
            Assert.That(errorRequest.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.extendedError"));
            Assert.That(errorRequest.Diagnostics[0].OptionIndex,
                Is.Zero);
            Assert.That(populatedRequest.Success, Is.False);
            Assert.That(populatedRequest.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.zoneVersion.length"));
            Assert.That(emptyResponse.Success, Is.False);
            Assert.That(emptyResponse.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.zoneVersion.length"));
        });
    }

    [Test]
    public void RequestZoneVersionMustBeEmptyAndUnique()
    {
        DnsEdnsOption populated = ZoneVersion(
            true, 2, 0, [0, 0, 0, 1]);
        DnsEdnsOption first = ZoneVersion(false, 0, 0, []);
        DnsEdnsOption second = ZoneVersion(false, 0, 0, []);

        DnsEncodeResult content = Encode(Message(false, populated));
        DnsEncodeResult duplicate = Encode(
            Message(false, first, second));

        Assert.Multiple(() =>
        {
            Assert.That(content.Success, Is.False);
            Assert.That(content.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.zoneVersion.length"));
            Assert.That(duplicate.Success, Is.False);
            Assert.That(duplicate.Diagnostics[0].Reason,
                Does.Contain("at most one"));
        });
    }

    [Test]
    public void ResponseZoneVersionRequiresContentAndQuestionBoundedLabels()
    {
        DnsEdnsOption empty = ZoneVersion(false, 0, 0, []);
        DnsEdnsOption excessive = ZoneVersion(
            true, 4, 0, [0, 0, 0, 1]);

        DnsEncodeResult emptyResult = Encode(Message(true, empty));
        DnsEncodeResult labelResult = Encode(Message(true, excessive));

        Assert.Multiple(() =>
        {
            Assert.That(emptyResult.Success, Is.False);
            Assert.That(emptyResult.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.zoneVersion.length"));
            Assert.That(labelResult.Success, Is.False);
            Assert.That(labelResult.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.zoneVersion.labelCount"));
        });
    }

    [Test]
    public void ResponseRejectsDuplicateZonePairAndReportChannel()
    {
        DnsEdnsOption first = ZoneVersion(
            true, 2, 0, [0, 0, 0, 1]);
        DnsEdnsOption second = ZoneVersion(
            true, 2, 0, [0, 0, 0, 2]);
        DnsEdnsOption reportOne = Report("one.example.");
        DnsEdnsOption reportTwo = Report("two.example.");

        DnsEncodeResult zoneResult = Encode(
            Message(true, first, second));
        DnsEncodeResult reportResult = Encode(
            Message(true, reportOne, reportTwo));

        Assert.Multiple(() =>
        {
            Assert.That(zoneResult.Success, Is.False);
            Assert.That(zoneResult.Diagnostics[0].Reason,
                Does.Contain("cannot repeat"));
            Assert.That(reportResult.Success, Is.False);
            Assert.That(reportResult.Diagnostics[0].Reason,
                Does.Contain("at most one"));
        });
    }

    [Test]
    public void ResponseRoundTripsMultipleErrorsReportAndDistinctVersions()
    {
        DnsEdnsOption firstError = Error("stale");
        DnsEdnsOption secondError = Error("upstream");
        secondError.ExtendedError!.Code =
            DnsExtendedErrorCode.NoReachableAuthority;
        secondError.ExtendedError.CodeValue = 22;
        DnsEdnsOption report = Report("agent.example.");
        DnsEdnsOption soa = ZoneVersion(
            true, 2, 0, [120, 149, 164, 233]);
        DnsEdnsOption custom = ZoneVersion(true, 2, 246, [9, 8, 7]);
        DnsMessage message = Message(
            true, firstError, secondError, report, soa, custom);

        DnsEncodeResult encoded = Encode(message);
        DnsDecodeResult decoded = new DnsMessageCodec().TryDecodeMessage(
            encoded.Bytes, new DnsDecodeOptions());
        DnsEdnsData edns =
            decoded.Message!.Additionals[0].Data!.Edns!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(edns.Options, Has.Count.EqualTo(5));
            Assert.That(edns.Options[0].ExtendedError!.ExtraText,
                Is.EqualTo("stale"));
            Assert.That(edns.Options[1].ExtendedError!.Code,
                Is.EqualTo(DnsExtendedErrorCode.NoReachableAuthority));
            Assert.That(Format(edns.Options[2].ReportChannel!),
                Is.EqualTo("agent.example."));
            Assert.That(edns.Options[3].ZoneVersion!.Version,
                Is.EqualTo(new byte[] { 120, 149, 164, 233 }));
            Assert.That(edns.Options[4].ZoneVersion!.Type,
                Is.EqualTo(246));
        });
    }

    static DnsEdnsOption Error(string text)
    {
        DnsEdnsOption option = Option(DnsEdnsOptionCode.ExtendedDnsError);
        option.ExtendedError = new DnsEdnsExtendedError();
        option.ExtendedError.Code = DnsExtendedErrorCode.StaleAnswer;
        option.ExtendedError.CodeValue = 3;
        option.ExtendedError.ExtraText = text;
        return option;
    }

    static DnsEdnsOption Report(string name)
    {
        DnsEdnsOption option = Option(DnsEdnsOptionCode.ReportChannel);
        option.ReportChannel = Parse(name);
        return option;
    }

    static DnsEdnsOption ZoneVersion(
        bool hasVersion,
        int labelCount,
        int type,
        byte[] versionBytes)
    {
        DnsEdnsOption option = Option(DnsEdnsOptionCode.ZoneVersion);
        option.ZoneVersion = new DnsEdnsZoneVersion();
        option.ZoneVersion.HasVersion = hasVersion;
        option.ZoneVersion.LabelCount = labelCount;
        option.ZoneVersion.Type = type;
        option.ZoneVersion.Version = versionBytes;
        return option;
    }

    static DnsEdnsOption Option(DnsEdnsOptionCode code)
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = code;
        option.CodeValue = (int)code;
        return option;
    }

    static DnsMessage Message(
        bool response,
        params DnsEdnsOption[] options)
    {
        DnsMessage message = new DnsMessage();
        if (response) message.Header.Flags = DnsHeaderFlag.Response;
        DnsQuestion question = new DnsQuestion();
        question.Name = Parse("a.b.example.");
        question.RecordType = DnsRecordType.A;
        question.RecordTypeValue = 1;
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        message.Questions.Add(question);
        DnsEdnsData edns = new DnsEdnsData();
        edns.Options.AddRange(options);
        DnsRecordData data = new DnsRecordData();
        data.Edns = edns;
        DnsResourceRecord opt = new DnsResourceRecord();
        opt.Name = Parse(".");
        opt.RecordType = DnsRecordType.Opt;
        opt.RecordTypeValue = 41;
        opt.Data = data;
        message.Additionals.Add(opt);
        return message;
    }

    static DnsEncodeResult Encode(DnsMessage message)
    {
        return new DnsMessageCodec().TryEncodeMessage(
            message, new DnsEncodeOptions());
    }

    static DnsDecodeResult Decode(byte[] bytes)
    {
        return new DnsMessageCodec().TryDecodeMessage(
            bytes, new DnsDecodeOptions());
    }

    static DnsName Parse(string text)
    {
        return new DnsNameService().ParseName(text, null);
    }

    static string Format(DnsName name)
    {
        return new DnsNameService().FormatName(
            name, new DnsTextOptions());
    }
}
