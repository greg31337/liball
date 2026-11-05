using System.Text;

namespace dns.Tests.Api;

public class TypedRecordDataTextTests
{
    [Test]
    public void AddressRecordsParseAndFormatHumanReadableAddresses()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();
        DnsTextResult ipv4 = codec.ParseResourceRecordText(
            "host.example. 300 IN A 192.0.2.15", options);
        DnsTextResult ipv6 = codec.ParseResourceRecordText(
            "host.example. 300 IN AAAA 2001:db8::15", options);

        Assert.Multiple(() =>
        {
            Assert.That(ipv4.Success, Is.True);
            Assert.That(ipv4.Record!.Data!.Address!.Address,
                Is.EqualTo(new byte[] { 192, 0, 2, 15 }));
            Assert.That(codec.FormatResourceRecordText(ipv4.Record, options).Text,
                Is.EqualTo("host.example. 300 IN A 192.0.2.15"));
            Assert.That(ipv6.Success, Is.True);
            Assert.That(ipv6.Record!.Data!.Address!.Address, Has.Length.EqualTo(16));
            Assert.That(codec.FormatResourceRecordText(ipv6.Record, options).Text,
                Is.EqualTo("host.example. 300 IN AAAA 2001:db8::15"));
        });
    }

    [Test]
    public void NamePreferenceAndSoaSchemasHonorOriginAndCanonicalNames()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsNameService names = new DnsNameService();
        DnsTextOptions options = new DnsTextOptions();
        options.Origin = names.ParseName("Example.", null);
        options.Canonical = true;
        DnsTextResult alias = codec.ParseResourceRecordText(
            "www 60 IN CNAME Target", options);
        DnsTextResult mail = codec.ParseResourceRecordText(
            "@ 60 IN MX 10 Mail", options);
        DnsTextResult soa = codec.ParseResourceRecordText(
            "@ 3600 IN SOA Ns Hostmaster 7 1h 10m 1d 5m", options);

        Assert.Multiple(() =>
        {
            Assert.That(codec.FormatResourceRecordText(alias.Record!, options).Text,
                Is.EqualTo("www 60 IN CNAME target"));
            Assert.That(codec.FormatResourceRecordText(mail.Record!, options).Text,
                Is.EqualTo("@ 60 IN MX 10 mail"));
            Assert.That(soa.Success, Is.True);
            Assert.That(soa.Record!.Data!.Soa!.Serial, Is.EqualTo(7));
            Assert.That(soa.Record.Data.Soa.Refresh, Is.EqualTo(3600));
            Assert.That(codec.FormatResourceRecordText(soa.Record, options).Text,
                Is.EqualTo("@ 3600 IN SOA ns hostmaster 7 3600 600 86400 300"));
        });
    }

    [Test]
    public void CharacterStringsRoundTripSpacesEscapesAndBinaryOctets()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();
        DnsTextResult parsed = codec.ParseResourceRecordText(
            "example. 30 IN TXT \"hello world\" \"a\\\"b\\000\"",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Record!.Data!.CharacterStrings!.Strings,
                Has.Count.EqualTo(2));
            Assert.That(parsed.Record.Data.CharacterStrings.Strings[0],
                Is.EqualTo(Encoding.ASCII.GetBytes("hello world")));
            Assert.That(parsed.Record.Data.CharacterStrings.Strings[1],
                Is.EqualTo(new byte[] { (byte)'a', (byte)'"', (byte)'b', 0 }));
            Assert.That(codec.FormatResourceRecordText(parsed.Record, options).Text,
                Is.EqualTo("example. 30 IN TXT \"hello world\" \"a\\\"b\\000\""));
        });
    }

    [Test]
    public void CharacterStringTextRoundTripsEveryPossibleOctet()
    {
        byte[] first = new byte[255];
        for (int value = 0; value < first.Length; value++)
        {
            first[value] = (byte)value;
        }

        DnsResourceRecord record = CreateRecord(DnsRecordType.Txt, 16);
        record.Data = new DnsRecordData();
        record.Data.CharacterStrings = new DnsCharacterStringsData();
        record.Data.CharacterStrings.Strings.Add(first);
        record.Data.CharacterStrings.Strings.Add([255]);
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();

        DnsTextResult formatted = codec.FormatResourceRecordText(
            record, options);
        DnsTextResult parsed = codec.ParseResourceRecordText(
            formatted.Text, options);
        DnsTextResult reformatted = codec.FormatResourceRecordText(
            parsed.Record!, options);

        Assert.Multiple(() =>
        {
            Assert.That(formatted.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Record!.Data!.CharacterStrings!.Strings[0],
                Is.EqualTo(first));
            Assert.That(parsed.Record.Data.CharacterStrings.Strings[1],
                Is.EqualTo(new byte[] { 255 }));
            Assert.That(reformatted.Text, Is.EqualTo(formatted.Text));
        });
    }

    [Test]
    public void SrvAndNaptrSchemasRoundTripStructuredFields()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();
        DnsTextResult srv = codec.ParseResourceRecordText(
            "_sip._tcp.example. 120 IN SRV 10 20 5060 sip.example.", options);
        DnsTextResult naptr = codec.ParseResourceRecordText(
            "example. 120 IN NAPTR 1 2 \"S\" \"SIP+D2U\" \"\" replacement.example.",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(srv.Success, Is.True);
            Assert.That(srv.Record!.Data!.Srv!.Port, Is.EqualTo(5060));
            Assert.That(codec.FormatResourceRecordText(srv.Record, options).Text,
                Is.EqualTo("_sip._tcp.example. 120 IN SRV 10 20 5060 sip.example."));
            Assert.That(naptr.Success, Is.True);
            Assert.That(naptr.Record!.Data!.Naptr!.Services,
                Is.EqualTo(Encoding.ASCII.GetBytes("SIP+D2U")));
        });
    }

    [Test]
    public void DnssecSchemasUseBase64HexBase32AndTypeMnemonics()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();
        DnsTextResult key = codec.ParseResourceRecordText(
            "example. 60 IN DNSKEY 257 3 8 AQIDBA==", options);
        DnsTextResult ds = codec.ParseResourceRecordText(
            "example. 60 IN DS 12345 8 2 A1B2C3D4", options);
        DnsTextResult nsec = codec.ParseResourceRecordText(
            "example. 60 IN NSEC next.example. A AAAA RRSIG TYPE65000", options);
        DnsTextResult nsec3 = codec.ParseResourceRecordText(
            "hash.example. 60 IN NSEC3 1 0 5 A1B2 04106105 A NS", options);
        DnsTextResult signature = codec.ParseResourceRecordText(
            "example. 60 IN RRSIG A 8 2 300 19700101000100 19700101000000 42 signer.example. AQI=",
            options);

        Assert.Multiple(() =>
        {
            Assert.That(key.Success, Is.True);
            Assert.That(key.Record!.Data!.Key!.PublicKey,
                Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(codec.FormatResourceRecordText(key.Record, options).Text,
                Is.EqualTo("example. 60 IN DNSKEY 257 3 8 AQIDBA=="));
            Assert.That(ds.Success, Is.True);
            Assert.That(codec.FormatResourceRecordText(ds.Record!, options).Text,
                Does.EndWith("12345 8 2 A1B2C3D4"));
            Assert.That(nsec.Success, Is.True);
            Assert.That(nsec.Record!.Data!.Nsec!.RecordTypeValues,
                Is.EqualTo(new[] { 1, 28, 46, 65000 }));
            Assert.That(nsec3.Success, Is.True);
            Assert.That(codec.FormatResourceRecordText(nsec3.Record!, options).Text,
                Does.Contain(" 1 0 5 A1B2 04106105 A NS"));
            Assert.That(signature.Success, Is.True);
            Assert.That(signature.Record!.Data!.Signature!.SignatureExpiration,
                Is.EqualTo(60));
            Assert.That(codec.FormatResourceRecordText(signature.Record, options).Text,
                Does.Contain("19700101000100 19700101000000"));
        });
    }

    [Test]
    public void ModernSchemasRoundTripUriCaaEuiAndDsyncForms()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();
        DnsTextResult uri = codec.ParseResourceRecordText(
            "example. 30 IN URI 10 20 \"https://example/path\"", options);
        DnsTextResult caa = codec.ParseResourceRecordText(
            "example. 30 IN CAA 0 \"issue\" \"ca.example\"", options);
        DnsTextResult eui = codec.ParseResourceRecordText(
            "example. 30 IN EUI48 00-11-22-33-44-55", options);
        DnsTextResult dsync = codec.ParseResourceRecordText(
            "_dns.example. 30 IN DSYNC CDS 1 5353 target.example.", options);

        Assert.Multiple(() =>
        {
            Assert.That(codec.FormatResourceRecordText(uri.Record!, options).Text,
                Is.EqualTo("example. 30 IN URI 10 20 \"https://example/path\""));
            Assert.That(codec.FormatResourceRecordText(caa.Record!, options).Text,
                Is.EqualTo("example. 30 IN CAA 0 \"issue\" \"ca.example\""));
            Assert.That(codec.FormatResourceRecordText(eui.Record!, options).Text,
                Is.EqualTo("example. 30 IN EUI48 00-11-22-33-44-55"));
            Assert.That(dsync.Success, Is.True);
            Assert.That(dsync.Record!.Data!.Dsync!.Port, Is.EqualTo(5353));
        });
    }

    [Test]
    public void HumanReadableFormattingCoversLocSvcbAndTransactionSchemas()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();
        DnsResourceRecord loc = CreateRecord(DnsRecordType.Loc, 29);
        loc.Data = new DnsRecordData();
        loc.Data.Loc = new DnsLocData();
        loc.Data.Loc.Latitude = 2147483648;
        loc.Data.Loc.Longitude = 2147483648;
        loc.Data.Loc.Altitude = 10000000;
        loc.Data.Loc.Size = 18;
        loc.Data.Loc.HorizontalPrecision = 22;
        loc.Data.Loc.VerticalPrecision = 19;
        DnsResourceRecord svcb = CreateRecord(DnsRecordType.Svcb, 64);
        svcb.Data = new DnsRecordData();
        svcb.Data.Svcb = new DnsSvcbData();
        svcb.Data.Svcb.Priority = 1;
        svcb.Data.Svcb.TargetName = new DnsNameService().ParseName("svc.example.", null);
        DnsSvcbParameter port = new DnsSvcbParameter();
        port.Key = DnsSvcbParameterKey.Port;
        port.KeyValue = 3;
        port.HasPort = true;
        port.Port = 8443;
        svcb.Data.Svcb.Parameters.Add(port);
        DnsTextResult formattedLoc = codec.FormatResourceRecordText(loc, options);
        DnsTextResult formattedSvcb = codec.FormatResourceRecordText(svcb, options);
        DnsTextResult parsedLoc = codec.ParseResourceRecordText(
            formattedLoc.Text, options);
        DnsTextResult parsedSvcb = codec.ParseResourceRecordText(
            formattedSvcb.Text, options);

        Assert.Multiple(() =>
        {
            Assert.That(formattedLoc.Text,
                Is.EqualTo("example. 60 IN LOC 0 0 0.000 N 0 0 0.000 E 0.00m 1.00m 10000.00m 10.00m"));
            Assert.That(formattedSvcb.Text,
                Is.EqualTo("example. 60 IN SVCB 1 svc.example. port=8443"));
            Assert.That(parsedLoc.Success, Is.True);
            Assert.That(parsedLoc.Record!.Data!.Loc!.Size, Is.EqualTo(18));
            Assert.That(parsedSvcb.Success, Is.True);
            Assert.That(parsedSvcb.Record!.Data!.Svcb!.Parameters[0].Port,
                Is.EqualTo(8443));
        });
    }

    [Test]
    public void TypedParsingReturnsDiagnosticsForMalformedFields()
    {
        DnsTextCodec codec = new DnsTextCodec();
        DnsTextOptions options = new DnsTextOptions();
        DnsTextResult badAddress = codec.ParseResourceRecordText(
            "example. 60 IN A 999.0.0.1", options);
        DnsTextResult badKey = codec.ParseResourceRecordText(
            "example. 60 IN DNSKEY 257 3 8 not-base64", options);
        DnsTextResult badSrv = codec.ParseResourceRecordText(
            "example. 60 IN SRV 0 0 70000 target.example.", options);

        Assert.Multiple(() =>
        {
            Assert.That(badAddress.Success, Is.False);
            Assert.That(badAddress.Diagnostics, Is.Not.Empty);
            Assert.That(badKey.Success, Is.False);
            Assert.That(badKey.Diagnostics, Is.Not.Empty);
            Assert.That(badSrv.Success, Is.False);
            Assert.That(badSrv.Diagnostics, Is.Not.Empty);
        });
    }

    static DnsResourceRecord CreateRecord(DnsRecordType type, int typeValue)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = new DnsNameService().ParseName("example.", null);
        record.Class = DnsClass.Internet;
        record.ClassValue = 1;
        record.RecordType = type;
        record.RecordTypeValue = typeValue;
        record.TimeToLive = 60;
        return record;
    }
}
