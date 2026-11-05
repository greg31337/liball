using System.Text;

namespace dns.Tests.Api;

public class DnsMessageTextTests
{
    [Test]
    public void CompleteMessageTextIncludesHeaderFlagsAndAllSections()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = 4660;
        message.Header.Flags = DnsHeaderFlag.RecursionDesired;
        message.Questions.Add(CreateQuestion("example.", DnsRecordType.A, 1));
        message.Answers.Add(CreateAddressRecord(
            "example.", DnsRecordType.A, 1, new byte[] { 192, 0, 2, 1 }));
        message.Authorities.Add(CreateNameRecord(
            "example.", DnsRecordType.Ns, 2, "ns.example."));
        message.Additionals.Add(CreateTextRecord(
            "note.example.", "diagnostic"));

        DnsTextResult result = codec.FormatMessageText(
            message, new DnsTextOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.SameAs(message));
        Assert.That(result.Text, Is.EqualTo(
            ";; ->>HEADER<<- opcode: QUERY, status: NOERROR, id: 4660\n" +
            ";; flags: rd; QUERY: 1, ANSWER: 1, AUTHORITY: 1, ADDITIONAL: 1\n\n" +
            ";; QUESTION SECTION:\n" +
            ";example. IN A\n\n" +
            ";; ANSWER SECTION:\n" +
            "example. 300 IN A 192.0.2.1\n\n" +
            ";; AUTHORITY SECTION:\n" +
            "example. 300 IN NS ns.example.\n\n" +
            ";; ADDITIONAL SECTION:\n" +
            "note.example. 300 IN TXT \"diagnostic\""));
    }

    [Test]
    public void HeaderAndSectionNamesCanBeSuppressedIndependently()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsMessage message = new DnsMessage();
        message.Questions.Add(CreateQuestion("example.", DnsRecordType.Aaaa, 28));
        message.Answers.Add(CreateAddressRecord(
            "example.", DnsRecordType.Aaaa, 28,
            new byte[] { 32, 1, 13, 184, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 1 }));
        DnsTextOptions options = new DnsTextOptions();
        options.IncludeHeader = false;
        options.IncludeSectionNames = false;

        DnsTextResult result = codec.FormatMessageText(message, options);

        Assert.That(result.Text, Is.EqualTo(
            ";example. IN AAAA\n\nexample. 300 IN AAAA 2001:db8::1"));
    }

    [Test]
    public void CanonicalMessageTextSortsEntriesWithoutMutatingMessage()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsMessage message = new DnsMessage();
        message.Answers.Add(CreateAddressRecord(
            "z.Example.", DnsRecordType.A, 1,
            new byte[] { 192, 0, 2, 2 }));
        message.Answers.Add(CreateAddressRecord(
            "A.Example.", DnsRecordType.A, 1,
            new byte[] { 192, 0, 2, 1 }));
        DnsTextOptions options = new DnsTextOptions();
        options.Canonical = true;
        DnsTextResult result = codec.FormatMessageText(message, options);

        int first = result.Text.IndexOf(
            "a.example. 300 IN A 192.0.2.1",
            StringComparison.Ordinal);
        int second = result.Text.IndexOf(
            "z.example. 300 IN A 192.0.2.2",
            StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.GreaterThanOrEqualTo(0));
            Assert.That(second, Is.GreaterThan(first));
            Assert.That(FormatName(message.Answers[0].Name),
                Is.EqualTo("z.Example."));
            Assert.That(FormatName(message.Answers[1].Name),
                Is.EqualTo("A.Example."));
        });
    }

    [Test]
    public void EdnsPseudoSectionIncludesFlagsExtendedStatusAndTypedOptions()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsMessage message = new DnsMessage();
        message.Header.Identifier = 7;
        message.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.RecursionAvailable;
        DnsResourceRecord opt = CreateOptRecord();
        opt.Data!.Edns!.ExtendedResponseCode = 1;
        opt.Data.Edns.Version = 0;
        opt.Data.Edns.Flags = DnsEdnsFlag.DnssecAnswerOk |
            DnsEdnsFlag.CompactAnswersOk;
        DnsEdnsOption error = new DnsEdnsOption();
        error.Code = DnsEdnsOptionCode.ExtendedDnsError;
        error.CodeValue = 15;
        error.ExtendedError = new DnsEdnsExtendedError();
        error.ExtendedError.Code = DnsExtendedErrorCode.Blocked;
        error.ExtendedError.CodeValue = 15;
        error.ExtendedError.ExtraText = "policy";
        DnsEdnsOption cookie = new DnsEdnsOption();
        cookie.Code = DnsEdnsOptionCode.Cookie;
        cookie.CodeValue = 10;
        cookie.Cookie = new DnsEdnsCookie();
        cookie.Cookie.ClientCookie = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        cookie.Cookie.ServerCookie = new byte[] { 0xaa, 0xbb };
        opt.Data.Edns.Options.Add(error);
        opt.Data.Edns.Options.Add(cookie);
        message.Additionals.Add(opt);
        DnsTextOptions options = new DnsTextOptions();
        options.Canonical = true;

        DnsTextResult result = codec.FormatMessageText(message, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text,
                Does.Contain("status: BADVERSION, id: 7"));
            Assert.That(result.Text,
                Does.Contain("; EDNS: version: 0, flags: do co; udp: 1232; ext-rcode: 1"));
            Assert.That(result.Text,
                Does.Contain("; COOKIE: 0102030405060708AABB"));
            Assert.That(result.Text,
                Does.Contain("; EXTENDEDDNSERROR: BLOCKED: policy"));
            Assert.That(result.Text.IndexOf("; COOKIE", StringComparison.Ordinal),
                Is.LessThan(result.Text.IndexOf(
                    "; EXTENDEDDNSERROR", StringComparison.Ordinal)));
            Assert.That(result.Text,
                Does.Not.Contain("example. 0 CLASS1232 OPT"));
        });
    }

    [Test]
    public void EdnsClientSubnetNamesAndOpaqueOptionsHaveStableDetails()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsMessage message = new DnsMessage();
        DnsResourceRecord opt = CreateOptRecord();
        DnsEdnsOption subnet = new DnsEdnsOption();
        subnet.Code = DnsEdnsOptionCode.ClientSubnet;
        subnet.CodeValue = 8;
        subnet.ClientSubnet = new DnsEdnsClientSubnet();
        subnet.ClientSubnet.AddressFamily = 1;
        subnet.ClientSubnet.SourcePrefixLength = 24;
        subnet.ClientSubnet.ScopePrefixLength = 0;
        subnet.ClientSubnet.Address = new byte[] { 192, 0, 2 };
        DnsEdnsOption chain = new DnsEdnsOption();
        chain.Code = DnsEdnsOptionCode.Chain;
        chain.CodeValue = 13;
        chain.ChainName = ParseName("trust.example.");
        DnsEdnsOption opaque = new DnsEdnsOption();
        opaque.Code = (DnsEdnsOptionCode)65000;
        opaque.CodeValue = 65000;
        opaque.UseRawData = true;
        opaque.RawData = new byte[] { 0xde, 0xad };
        opt.Data!.Edns!.Options.Add(subnet);
        opt.Data.Edns.Options.Add(chain);
        opt.Data.Edns.Options.Add(opaque);
        message.Additionals.Add(opt);

        DnsTextResult result = codec.FormatMessageText(
            message, new DnsTextOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Text,
                Does.Contain("; CLIENTSUBNET: 1 192.0.2.0/24 scope/0"));
            Assert.That(result.Text,
                Does.Contain("; CHAIN: trust.example."));
            Assert.That(result.Text,
                Does.Contain("; OPTION65000: DEAD"));
        });
    }

    [Test]
    public void RawOptDataIsRenderedWithoutTypedEdnsState()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsMessage message = new DnsMessage();
        DnsResourceRecord opt = new DnsResourceRecord();
        opt.RecordType = DnsRecordType.Opt;
        opt.RecordTypeValue = 41;
        opt.Class = (DnsClass)1400;
        opt.ClassValue = 1400;
        opt.TimeToLive = 32768;
        opt.RawData = new byte[] { 0, 1, 2 };
        opt.UseRawData = true;
        message.Additionals.Add(opt);

        DnsTextResult result = codec.FormatMessageText(
            message, new DnsTextOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text,
                Does.Contain("; EDNS: version: 0, flags: do; udp: 1400"));
            Assert.That(result.Text, Does.Contain("; OPT-RDATA: 000102"));
        });
    }

    [Test]
    public void HeaderUsesActualSectionCountsAndPreservesUnknownCodes()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsMessage message = new DnsMessage();
        message.Header.Opcode = (DnsOpcode)15;
        message.Header.OpcodeValue = 15;
        message.Header.ResponseCode = (DnsResponseCode)14;
        message.Header.ResponseCodeValue = 14;
        message.Header.QuestionCount = 99;
        message.Questions.Add(CreateQuestion("example.", DnsRecordType.A, 1));

        DnsTextResult result = codec.FormatMessageText(
            message, new DnsTextOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Text,
                Does.Contain("opcode: OPCODE15, status: RCODE14"));
            Assert.That(result.Text,
                Does.Contain("QUERY: 1, ANSWER: 0, AUTHORITY: 0, ADDITIONAL: 0"));
            Assert.That(result.Text, Does.Not.Contain("QUERY: 99"));
        });
    }

    [Test]
    public void InvalidNestedRecordReturnsStructuredDiagnostics()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsMessage message = new DnsMessage();
        DnsResourceRecord invalid = new DnsResourceRecord();
        invalid.Name = ParseName("example.");
        invalid.Class = DnsClass.Internet;
        invalid.ClassValue = 1;
        invalid.RecordType = DnsRecordType.A;
        invalid.RecordTypeValue = 1;
        invalid.Data = new DnsRecordData();
        message.Answers.Add(invalid);

        DnsTextResult result = codec.FormatMessageText(
            message, new DnsTextOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Is.Not.Empty);
            Assert.That(result.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
        });
    }

    static DnsQuestion CreateQuestion(
        string name,
        DnsRecordType type,
        int typeValue)
    {
        DnsQuestion question = new DnsQuestion();
        question.Name = ParseName(name);
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        question.RecordType = type;
        question.RecordTypeValue = typeValue;
        return question;
    }

    static DnsResourceRecord CreateAddressRecord(
        string name,
        DnsRecordType type,
        int typeValue,
        byte[] address)
    {
        DnsResourceRecord record = CreateRecord(name, type, typeValue);
        record.Data = new DnsRecordData();
        record.Data.Address = new DnsAddressData();
        record.Data.Address.Address = address;
        return record;
    }

    static DnsResourceRecord CreateNameRecord(
        string name,
        DnsRecordType type,
        int typeValue,
        string target)
    {
        DnsResourceRecord record = CreateRecord(name, type, typeValue);
        record.Data = new DnsRecordData();
        record.Data.Name = new DnsNameData();
        record.Data.Name.Name = ParseName(target);
        return record;
    }

    static DnsResourceRecord CreateTextRecord(string name, string text)
    {
        DnsResourceRecord record = CreateRecord(name, DnsRecordType.Txt, 16);
        record.Data = new DnsRecordData();
        record.Data.CharacterStrings = new DnsCharacterStringsData();
        record.Data.CharacterStrings.Strings.Add(
            Encoding.ASCII.GetBytes(text));
        return record;
    }

    static DnsResourceRecord CreateRecord(
        string name,
        DnsRecordType type,
        int typeValue)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = ParseName(name);
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.RecordType = type;
        record.RecordTypeValue = typeValue;
        record.TimeToLive = 300;
        return record;
    }

    static DnsResourceRecord CreateOptRecord()
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = ParseName(".");
        record.RecordType = DnsRecordType.Opt;
        record.RecordTypeValue = 41;
        record.Class = (DnsClass)1232;
        record.ClassValue = 1232;
        record.Data = new DnsRecordData();
        record.Data.Edns = new DnsEdnsData();
        return record;
    }

    static DnsName ParseName(string text)
    {
        return new DnsNameService().ParseName(text, null);
    }

    static string FormatName(DnsName name)
    {
        return new DnsNameService().FormatName(
            name, new DnsTextOptions());
    }
}
