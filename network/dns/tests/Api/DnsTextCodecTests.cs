namespace dns.Tests.Api;

public class DnsTextCodecTests
{
    [Test]
    public void TimeToLiveTextSupportsDecimalAndCompoundUnits()
    {
        DnsTextCodec codec = new DnsTextCodec();

        Assert.Multiple(() =>
        {
            Assert.That(codec.ParseTimeToLive("3600"), Is.EqualTo(3600));
            Assert.That(codec.ParseTimeToLive("1w2D3h4M5s"),
                Is.EqualTo(788645));
            Assert.That(codec.ParseTimeToLive("4294967295"),
                Is.EqualTo(uint.MaxValue));
            Assert.That(codec.FormatTimeToLive(uint.MaxValue),
                Is.EqualTo("4294967295"));
            Assert.That(
                () => codec.ParseTimeToLive("1h30"),
                Throws.TypeOf<DnsFormatException>());
            Assert.That(
                () => codec.ParseTimeToLive("4294967296"),
                Throws.TypeOf<DnsFormatException>());
        });
    }

    [TestCase("0", 0)]
    [TestCase("1s", 1)]
    [TestCase("2m", 120)]
    [TestCase("3h", 10800)]
    [TestCase("4d", 345600)]
    [TestCase("5w", 3024000)]
    [TestCase("1W2d3H4m5S", 788645)]
    public void EveryTimeToLiveUnitFormParsesToSeconds(
        string text,
        long expected)
    {
        DnsTextCodec codec = new DnsTextCodec();

        uint parsed = codec.ParseTimeToLive(text);
        string formatted = codec.FormatTimeToLive(parsed);

        Assert.Multiple(() =>
        {
            Assert.That(parsed, Is.EqualTo((uint)expected));
            Assert.That(formatted, Is.EqualTo(expected.ToString()));
            Assert.That(codec.ParseTimeToLive(formatted), Is.EqualTo(parsed));
        });
    }

    [Test]
    public void NameFormattingAppliesOriginAndCanonicalPolicy()
    {
        DnsNameService names = new DnsNameService();
        DnsName origin = names.ParseName("Example.", null);
        DnsTextOptions options = new DnsTextOptions();
        options.Origin = origin;
        options.Canonical = true;

        Assert.Multiple(() =>
        {
            Assert.That(
                names.FormatName(names.ParseName("WWW.Example.", null), options),
                Is.EqualTo("www"));
            Assert.That(names.FormatName(origin, options), Is.EqualTo("@"));
            Assert.That(
                names.FormatName(names.ParseName("Elsewhere.", null), options),
                Is.EqualTo("elsewhere."));
        });
    }

    [Test]
    public void QuestionTextParsesMnemonicsAndGenericCodes()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();
        options.Origin = new DnsNameService().ParseName("example.", null);

        DnsTextResult mnemonic = codec.ParseQuestionText(
            "www IN AAAA", options);
        DnsTextResult generic = codec.ParseQuestionText(
            "@ CLASS65200 TYPE65400", options);

        Assert.Multiple(() =>
        {
            Assert.That(mnemonic.Success, Is.True);
            Assert.That(mnemonic.Question, Is.Not.Null);
            Assert.That(mnemonic.Question!.RecordTypeValue, Is.EqualTo(28));
            Assert.That(mnemonic.Question.ClassValue, Is.EqualTo(1));
            Assert.That(
                new DnsNameService().FormatName(
                    mnemonic.Question.Name, new DnsTextOptions()),
                Is.EqualTo("www.example."));
            Assert.That(generic.Success, Is.True);
            Assert.That(generic.Question!.ClassValue, Is.EqualTo(65200));
            Assert.That(generic.Question.RecordTypeValue, Is.EqualTo(65400));
        });
    }

    [TestCase("IN", 1, "IN")]
    [TestCase("CH", 3, "CH")]
    [TestCase("CHAOS", 3, "CH")]
    [TestCase("HS", 4, "HS")]
    [TestCase("HESIOD", 4, "HS")]
    [TestCase("NONE", 254, "NONE")]
    [TestCase("ANY", 255, "ANY")]
    [TestCase("CLASS0", 0, "CLASS0")]
    [TestCase("CLASS65535", 65535, "CLASS65535")]
    public void EveryClassFormParsesAndFormatsCanonically(
        string classText,
        int classValue,
        string canonicalText)
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextResult parsed = codec.ParseQuestionText(
            "example. " + classText + " A", new DnsTextOptions());
        DnsTextResult formatted = codec.FormatQuestionText(
            parsed.Question!, new DnsTextOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Question!.ClassValue, Is.EqualTo(classValue));
            Assert.That(formatted.Text,
                Is.EqualTo("example. " + canonicalText + " A"));
        });
    }

    [Test]
    public void EveryDeclaredRecordTypeMnemonicParsesAndFormats()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsRecordType[] recordTypes = Enum.GetValues<DnsRecordType>();

        foreach (DnsRecordType recordType in recordTypes)
        {
            string typeText = codec.FormatDnsRecordType(recordType);
            DnsTextResult parsed = codec.ParseQuestionText(
                "example. IN " + typeText, new DnsTextOptions());
            Assert.That(parsed.Success, Is.True, recordType.ToString());
            Assert.That(parsed.Question!.RecordTypeValue,
                Is.EqualTo((int)recordType), recordType.ToString());

            DnsTextResult formatted = codec.FormatQuestionText(
                parsed.Question, new DnsTextOptions());
            Assert.That(formatted.Text,
                Is.EqualTo("example. IN " + typeText), recordType.ToString());
        }
    }

    [Test]
    public void QuestionFormattingHonorsMnemonicAndOriginOptions()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsNameService names = new DnsNameService();
        DnsQuestion question = new DnsQuestion();
        question.Name = names.ParseName("WWW.Example.", null);
        question.Class = DnsClass.Internet;
        question.ClassValue = 1;
        question.RecordType = DnsRecordType.Aaaa;
        question.RecordTypeValue = 28;
        DnsTextOptions mnemonic = new DnsTextOptions();
        mnemonic.Origin = names.ParseName("example.", null);
        DnsTextOptions numeric = new DnsTextOptions();
        numeric.UseMnemonicCodes = false;
        DnsTextResult rejectedMnemonic = codec.ParseQuestionText(
            "example. IN A", numeric);
        DnsTextResult parsedNumeric = codec.ParseQuestionText(
            "example. CLASS1 TYPE1", numeric);

        Assert.Multiple(() =>
        {
            Assert.That(codec.FormatQuestionText(question, mnemonic).Text,
                Is.EqualTo("WWW IN AAAA"));
            Assert.That(codec.FormatQuestionText(question, numeric).Text,
                Is.EqualTo("WWW.Example. CLASS1 TYPE28"));
            Assert.That(rejectedMnemonic.Success, Is.False);
            Assert.That(parsedNumeric.Success, Is.True);
        });
    }

    [Test]
    public void Rfc3597RecordsRoundTripOpaqueDataAndFlexibleFieldOrder()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();
        options.Origin = new DnsNameService().ParseName("example.", null);
        DnsTextResult parsed = codec.ParseResourceRecordText(
            "host CLASS32 1h TYPE731 \\# 6 abcd EF01 2345",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Record, Is.Not.Null);
            Assert.That(parsed.Record!.ClassValue, Is.EqualTo(32));
            Assert.That(parsed.Record.RecordTypeValue, Is.EqualTo(731));
            Assert.That(parsed.Record.TimeToLive, Is.EqualTo(3600));
            Assert.That(parsed.Record.UseRawData, Is.True);
            Assert.That(parsed.Record.RawData,
                Is.EqualTo(new byte[] { 0xab, 0xcd, 0xef, 1, 0x23, 0x45 }));
            Assert.That(codec.FormatResourceRecordText(
                    parsed.Record, options).Text,
                Is.EqualTo("host 3600 CLASS32 TYPE731 \\# 6 ABCDEF012345"));
        });
    }

    [TestCase(".", ".", ".")]
    [TestCase("@", "@", "example.")]
    [TestCase("host", "host", "host.example.")]
    [TestCase("outside.test.", "outside.test.", "outside.test.")]
    [TestCase("a\\.b", "a\\.b", "a\\.b.example.")]
    public void ResourceRecordOwnerFormsRemainStableAcrossParseFormatParse(
        string ownerText,
        string formattedOwner,
        string absoluteOwner)
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsNameService names = new DnsNameService();
        DnsTextOptions options = new DnsTextOptions();
        options.Origin = names.ParseName("example.", null);
        string input = ownerText + " 1h IN A 192.0.2.1";

        DnsTextResult parsed = codec.ParseResourceRecordText(input, options);
        DnsTextResult formatted = codec.FormatResourceRecordText(
            parsed.Record!, options);
        DnsTextResult reparsed = codec.ParseResourceRecordText(
            formatted.Text, options);
        DnsTextResult reformatted = codec.FormatResourceRecordText(
            reparsed.Record!, options);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(formatted.Text,
                Is.EqualTo(formattedOwner + " 3600 IN A 192.0.2.1"));
            Assert.That(reparsed.Success, Is.True);
            Assert.That(names.FormatName(
                    reparsed.Record!.Name, new DnsTextOptions()),
                Is.EqualTo(absoluteOwner));
            Assert.That(reformatted.Text, Is.EqualTo(formatted.Text));
        });
    }

    [Test]
    public void Rfc3597SupportsKnownNumericTypesAndEmptyData()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();
        DnsTextResult known = codec.ParseResourceRecordText(
            "example. 60 IN TYPE1 \\# 4 C0000201", options);
        DnsTextResult empty = codec.ParseResourceRecordText(
            "example. 0 HS TYPE62347 \\# 0", options);

        Assert.Multiple(() =>
        {
            Assert.That(known.Success, Is.True);
            Assert.That(known.Record!.RecordType, Is.EqualTo(DnsRecordType.A));
            Assert.That(codec.FormatResourceRecordText(known.Record, options).Text,
                Is.EqualTo("example. 60 IN A \\# 4 C0000201"));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Record!.RawData, Is.Empty);
            Assert.That(codec.FormatResourceRecordText(empty.Record, options).Text,
                Is.EqualTo("example. 0 HS TYPE62347 \\# 0"));
        });
    }

    [Test]
    public void Rfc3597RejectsMalformedHexAndLengthMismatches()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();
        DnsTextResult odd = codec.ParseResourceRecordText(
            "example. 60 IN TYPE731 \\# 1 A", options);
        DnsTextResult invalid = codec.ParseResourceRecordText(
            "example. 60 IN TYPE731 \\# 1 GG", options);
        DnsTextResult mismatch = codec.ParseResourceRecordText(
            "example. 60 IN TYPE731 \\# 2 AA", options);

        Assert.Multiple(() =>
        {
            Assert.That(odd.Success, Is.False);
            Assert.That(odd.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidText));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidText));
            Assert.That(mismatch.Success, Is.False);
            Assert.That(mismatch.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLength));
        });
    }

    [Test]
    public void ClassTypeAndDiagnosticFormattingPreserveUnknownValues()
    {
        DnsTextCodec codec = new DnsTextCodec();

        Assert.Multiple(() =>
        {
            Assert.That(codec.FormatDnsClass(DnsClass.Internet),
                Is.EqualTo("IN"));
            Assert.That(codec.FormatDnsClass((DnsClass)65000),
                Is.EqualTo("CLASS65000"));
            Assert.That(codec.FormatDnsRecordType(DnsRecordType.NsapPointer),
                Is.EqualTo("NSAP-PTR"));
            Assert.That(codec.FormatDnsRecordType(
                    DnsRecordType.ResolverInformation),
                Is.EqualTo("RESINFO"));
            Assert.That(codec.FormatDnsRecordType((DnsRecordType)65000),
                Is.EqualTo("TYPE65000"));
            Assert.That(codec.FormatDnsDiagnosticCode(
                    DnsDiagnosticCode.InvalidText),
                Is.EqualTo("INVALIDTEXT"));
            Assert.That(codec.FormatDnsDiagnosticCode(
                    (DnsDiagnosticCode)65000),
                Is.EqualTo("DIAGNOSTIC65000"));
        });
    }

    [Test]
    public void PublicEnumFormattingMapsEveryEnumFamily()
    {
        IDnsTextCodec codec = new DnsTextCodec();

        Assert.Multiple(() =>
        {
            Assert.That(codec.FormatDnsOpcode(DnsOpcode.Notify),
                Is.EqualTo("NOTIFY"));
            Assert.That(codec.FormatDnsResponseCode(DnsResponseCode.NameError),
                Is.EqualTo("NAMEERROR"));
            Assert.That(codec.FormatDnsMessageSection(
                    DnsMessageSection.Additional),
                Is.EqualTo("ADDITIONAL"));
            Assert.That(codec.FormatDnsHeaderFlags(
                    DnsHeaderFlag.Response | DnsHeaderFlag.AuthoritativeAnswer),
                Is.EqualTo("FLAGS33792"));
            Assert.That(codec.FormatDnsEdnsFlags(DnsEdnsFlag.DnssecAnswerOk),
                Is.EqualTo("DNSSECANSWEROK"));
            Assert.That(codec.FormatDnsEdnsOptionCode(DnsEdnsOptionCode.Cookie),
                Is.EqualTo("COOKIE"));
            Assert.That(codec.FormatDnsSvcbParameterKey(
                    DnsSvcbParameterKey.Port),
                Is.EqualTo("PORT"));
            Assert.That(codec.FormatDnsDnssecAlgorithm(
                    DnsDnssecAlgorithm.RsaSha256),
                Is.EqualTo("RSASHA256"));
            Assert.That(codec.FormatDnsDigestAlgorithm(DnsDigestAlgorithm.Sha256),
                Is.EqualTo("SHA256"));
            Assert.That(codec.FormatDnsNsec3HashAlgorithm(
                    DnsNsec3HashAlgorithm.Sha1),
                Is.EqualTo("SHA1"));
            Assert.That(codec.FormatDnsTsigError(DnsTsigError.BadSignature),
                Is.EqualTo("BADSIGNATURE"));
            Assert.That(codec.FormatDnsExtendedErrorCode(
                    DnsExtendedErrorCode.Blocked),
                Is.EqualTo("BLOCKED"));
            Assert.That(codec.FormatDnsSecurityStatus(DnsSecurityStatus.Secure),
                Is.EqualTo("SECURE"));
            Assert.That(codec.FormatDnsAlgorithmPolicy(
                    DnsAlgorithmPolicy.Forbidden),
                Is.EqualTo("FORBIDDEN"));
            Assert.That(codec.FormatDnsTransportKind(DnsTransportKind.Quic),
                Is.EqualTo("QUIC"));
            Assert.That(codec.FormatDnsTransportStatus(
                    DnsTransportStatus.TimedOut),
                Is.EqualTo("TIMEDOUT"));
            Assert.That(codec.FormatDnsCacheEntryKind(
                    DnsCacheEntryKind.NegativeName),
                Is.EqualTo("NEGATIVENAME"));
            Assert.That(codec.FormatDnsResolveStatus(DnsResolveStatus.NoData),
                Is.EqualTo("NODATA"));
            Assert.That(codec.FormatDnsAuthoritativeAnswerKind(
                    DnsAuthoritativeAnswerKind.Referral),
                Is.EqualTo("REFERRAL"));
            Assert.That(codec.FormatDnsZoneChangeKind(
                    DnsZoneChangeKind.DeleteRecord),
                Is.EqualTo("DELETERECORD"));
            Assert.That(codec.FormatDnsTransferKind(DnsTransferKind.Ixfr),
                Is.EqualTo("IXFR"));
            Assert.That(codec.FormatDnsUpdatePrerequisiteKind(
                    DnsUpdatePrerequisiteKind.NameInUse),
                Is.EqualTo("NAMEINUSE"));
            Assert.That(codec.FormatDnsUpdateChangeKind(
                    DnsUpdateChangeKind.DeleteName),
                Is.EqualTo("DELETENAME"));
            Assert.That(codec.FormatDnsServerStatus(DnsServerStatus.Running),
                Is.EqualTo("RUNNING"));
            Assert.That(codec.FormatDnsOpcode((DnsOpcode)65000),
                Is.EqualTo("OPCODE65000"));
            Assert.That(codec.FormatDnsExtendedErrorCode(
                    (DnsExtendedErrorCode)65000),
                Is.EqualTo("EDE65000"));
        });
    }
}
