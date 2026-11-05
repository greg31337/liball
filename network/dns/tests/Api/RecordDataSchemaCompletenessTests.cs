using System.Text;

namespace dns.Tests.Api;

public class RecordDataSchemaCompletenessTests
{
    const string ExpectedClassifications =
        "MetaQuery=Reserved,Nxname,Ixfr,Axfr,Mailb,Maila,Any\n" +
        "Address=A,Aaaa\n" +
        "Name=Ns,Md,Mf,Cname,Mb,Mg,Mr,Ptr,NsapPointer,Dname\n" +
        "TwoName=Minfo,Rp\n" +
        "PreferenceName=Mx,Afsdb,Rt,Kx,Lp\n" +
        "CharacterStrings=Hinfo,Txt,X25,Isdn,Gpos,Spf\n" +
        "Opaque=Null,Eid,Nimloc,Atma,Sink,Ninfo,Rkey,Talink,Hhit,Brid,Unece,Iso,Uinfo,Uid,Gid,Unspec,Avc,Doa,Wallet,Cla,Ipn\n" +
        "FixedBytes=Nsap,OpenPgpKey\n" +
        "Soa=Soa\n" +
        "Wks=Wks\n" +
        "A6=A6\n" +
        "Px=Px\n" +
        "Loc=Loc\n" +
        "Srv=Srv\n" +
        "Naptr=Naptr\n" +
        "Cert=Cert\n" +
        "Apl=Apl\n" +
        "Signature=Sig,Rrsig\n" +
        "Key=Key,Dnskey,Cdnskey\n" +
        "Ds=Ds,Cds,Ta,Dlv\n" +
        "Sshfp=Sshfp\n" +
        "IpsecKey=IpsecKey\n" +
        "Nsec=Nxt,Nsec\n" +
        "Nsec3=Nsec3\n" +
        "Nsec3Param=Nsec3Param\n" +
        "Dhcid=Dhcid\n" +
        "Tlsa=Tlsa,Smimea\n" +
        "Hip=Hip\n" +
        "Csync=Csync\n" +
        "Zonemd=Zonemd\n" +
        "Svcb=Svcb,Https\n" +
        "Dsync=Dsync\n" +
        "IlNp=Nid,L32,L64\n" +
        "Eui=Eui48,Eui64\n" +
        "Uri=Uri\n" +
        "Caa=Caa\n" +
        "AmtRelay=AmtRelay\n" +
        "ResolverInformation=ResolverInformation\n" +
        "Tkey=Tkey\n" +
        "Tsig=Tsig\n" +
        "Edns=Opt";

    [Test]
    public void EveryAssignedRecordTypeHasItsExactDataModelClassification()
    {
        Dictionary<RecordDataModel, List<string>> groups =
            new Dictionary<RecordDataModel, List<string>>();
        DnsRecordType[] values = Enum.GetValues<DnsRecordType>();
        int index = 0;
        while (index < values.Length)
        {
            DnsRecordType value = values[index];
            RecordDataModel model = RecordDataModelCatalog.ReadRecordDataModel(
                (int)value);
            if (!groups.ContainsKey(model))
            {
                groups.Add(model, new List<string>());
            }

            groups[model].Add(value.ToString());
            index = index + 1;
        }

        StringBuilder actual = new StringBuilder();
        int modelValue = 1;
        while (modelValue <= (int)RecordDataModel.Edns)
        {
            RecordDataModel model = (RecordDataModel)modelValue;
            if (groups.ContainsKey(model))
            {
                if (actual.Length > 0)
                {
                    actual.Append('\n');
                }

                actual.Append(model).Append('=').Append(
                    string.Join(",", groups[model]));
            }

            modelValue = modelValue + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(groups.ContainsKey(RecordDataModel.Unassigned), Is.False);
            Assert.That(actual.ToString(), Is.EqualTo(ExpectedClassifications));
        });
    }

    [Test]
    public void FixturesCoverEveryTypedPresentationDataModelExactlyOnce()
    {
        List<SchemaFixture> fixtures = Fixtures();
        HashSet<RecordDataModel> actual = new HashSet<RecordDataModel>();
        int index = 0;
        while (index < fixtures.Count)
        {
            Assert.That(actual.Add(fixtures[index].Model), Is.True,
                fixtures[index].Model.ToString());
            index = index + 1;
        }

        int modelValue = (int)RecordDataModel.Address;
        while (modelValue <= (int)RecordDataModel.Tsig)
        {
            RecordDataModel model = (RecordDataModel)modelValue;
            if (model != RecordDataModel.Opaque)
            {
                Assert.That(actual.Contains(model), Is.True, model.ToString());
            }

            modelValue = modelValue + 1;
        }

        Assert.That(actual, Has.Count.EqualTo(38));
    }

    [Test]
    public void EveryTypedSchemaRoundTripsPresentationAndWireData()
    {
        DnsTextCodec textCodec = new DnsTextCodec();
        DnsRecordDataCodec wireCodec = new DnsRecordDataCodec();
        DnsTextOptions textOptions = new DnsTextOptions();
        DnsEncodeOptions encodeOptions = new DnsEncodeOptions();
        DnsDecodeOptions decodeOptions = new DnsDecodeOptions();
        List<SchemaFixture> fixtures = Fixtures();
        int index = 0;
        while (index < fixtures.Count)
        {
            SchemaFixture fixture = fixtures[index];
            string recordText = "owner.example. 60 IN " + fixture.TypeText +
                " " + fixture.DataText;
            DnsTextResult parsed = textCodec.ParseResourceRecordText(
                recordText, textOptions);
            Assert.That(parsed.Success, Is.True,
                fixture.Model + " parse: " + Diagnostic(parsed));
            DnsResourceRecord record = parsed.Record!;
            Assert.That(
                RecordDataModelCatalog.ReadRecordDataModel(
                    record.RecordTypeValue),
                Is.EqualTo(fixture.Model),
                fixture.Model + " classification");

            DnsTextResult formatted = textCodec.FormatResourceRecordText(
                record, textOptions);
            Assert.That(formatted.Success, Is.True,
                fixture.Model + " format: " + Diagnostic(formatted));
            DnsTextResult reparsed = textCodec.ParseResourceRecordText(
                formatted.Text, textOptions);
            Assert.That(reparsed.Success, Is.True,
                fixture.Model + " reparse: " + Diagnostic(reparsed));

            DnsEncodeResult encoded = wireCodec.TryEncodeRecordData(
                record.RecordType,
                record.RecordTypeValue,
                record.Data!,
                encodeOptions);
            Assert.That(encoded.Success, Is.True,
                fixture.Model + " wire encode: " + Diagnostic(encoded));
            DnsDecodeResult decoded = wireCodec.TryDecodeRecordData(
                record.RecordType,
                record.RecordTypeValue,
                encoded.Bytes,
                0,
                encoded.Bytes.Length,
                decodeOptions);
            Assert.That(decoded.Success, Is.True,
                fixture.Model + " wire decode: " + Diagnostic(decoded));

            DnsResourceRecord decodedRecord = CopyRecordShell(record);
            decodedRecord.Data = decoded.RecordData;
            DnsTextResult decodedText = textCodec.FormatResourceRecordText(
                decodedRecord, textOptions);
            Assert.That(decodedText.Success, Is.True,
                fixture.Model + " decoded format: " + Diagnostic(decodedText));
            Assert.That(decodedText.Text, Is.EqualTo(formatted.Text),
                fixture.Model + " text stability");

            DnsEncodeResult reencoded = wireCodec.TryEncodeRecordData(
                reparsed.Record!.RecordType,
                reparsed.Record.RecordTypeValue,
                reparsed.Record.Data!,
                encodeOptions);
            Assert.That(reencoded.Success, Is.True,
                fixture.Model + " re-encode: " + Diagnostic(reencoded));
            Assert.That(reencoded.Bytes, Is.EqualTo(encoded.Bytes),
                fixture.Model + " wire stability");
            index = index + 1;
        }
    }

    [Test]
    public void EveryTypedSchemaHandlesRandomizedContainersAndMalformedBounds()
    {
        Random random = new Random(162);
        DnsTextCodec textCodec = new DnsTextCodec();
        DnsRecordDataCodec wireCodec = new DnsRecordDataCodec();
        List<SchemaFixture> fixtures = Fixtures();
        int index = 0;
        while (index < fixtures.Count)
        {
            SchemaFixture fixture = fixtures[index];
            DnsTextResult parsed = textCodec.ParseResourceRecordText(
                "owner.example. 60 IN " + fixture.TypeText + " " +
                    fixture.DataText,
                new DnsTextOptions());
            Assert.That(parsed.Success, Is.True, fixture.Model.ToString());
            DnsResourceRecord record = parsed.Record!;
            DnsEncodeResult encoded = wireCodec.TryEncodeRecordData(
                record.RecordType,
                record.RecordTypeValue,
                record.Data!,
                new DnsEncodeOptions());
            Assert.That(encoded.Success, Is.True, fixture.Model.ToString());
            int suffixLength = random.Next(1, 17);
            byte[] container = new byte[
                encoded.Bytes.Length + suffixLength];
            random.NextBytes(container);
            Array.Copy(
                encoded.Bytes,
                0,
                container,
                0,
                encoded.Bytes.Length);

            DnsDecodeResult decoded = wireCodec.TryDecodeRecordData(
                record.RecordType,
                record.RecordTypeValue,
                container,
                0,
                encoded.Bytes.Length,
                new DnsDecodeOptions());
            Assert.That(decoded.Success, Is.True, fixture.Model.ToString());
            DnsEncodeResult reencoded = wireCodec.TryEncodeRecordData(
                record.RecordType,
                record.RecordTypeValue,
                decoded.RecordData!,
                new DnsEncodeOptions());
            Assert.That(reencoded.Bytes, Is.EqualTo(encoded.Bytes),
                fixture.Model.ToString());
            DnsDecodeResult malformed = wireCodec.TryDecodeRecordData(
                record.RecordType,
                record.RecordTypeValue,
                container,
                container.Length + 1,
                0,
                new DnsDecodeOptions());
            Assert.That(malformed.Success, Is.False,
                fixture.Model.ToString());
            index = index + 1;
        }
    }

    static List<SchemaFixture> Fixtures()
    {
        string zonemdDigest = "A1B2C3D4E5F60708090A0B0C";
        return new List<SchemaFixture>
        {
            Fixture(RecordDataModel.Address, "A", "192.0.2.1"),
            Fixture(RecordDataModel.Name, "CNAME", "target.example."),
            Fixture(RecordDataModel.TwoName, "MINFO",
                "one.example. two.example."),
            Fixture(RecordDataModel.PreferenceName, "MX",
                "10 mail.example."),
            Fixture(RecordDataModel.CharacterStrings, "TXT",
                "\"one\" \"two\""),
            Fixture(RecordDataModel.FixedBytes, "OPENPGPKEY", "AQID"),
            Fixture(RecordDataModel.Soa, "SOA",
                "ns.example. hostmaster.example. 1 2 3 4 5"),
            Fixture(RecordDataModel.Wks, "WKS", "192.0.2.1 6 53 80"),
            Fixture(RecordDataModel.A6, "A6", "0 2001:db8::1"),
            Fixture(RecordDataModel.Px, "PX",
                "10 one.example. two.example."),
            Fixture(RecordDataModel.Loc, "LOC",
                "0 0 0.000 N 0 0 0.000 E 0.00m 1.00m 10000.00m 10.00m"),
            Fixture(RecordDataModel.Srv, "SRV",
                "1 2 53 target.example."),
            Fixture(RecordDataModel.Naptr, "NAPTR",
                "1 2 \"S\" \"SIP+D2U\" \"\" replacement.example."),
            Fixture(RecordDataModel.Cert, "CERT", "1 2 200 AQID"),
            Fixture(RecordDataModel.Apl, "APL", "1:192.0.2.0/24"),
            Fixture(RecordDataModel.Signature, "RRSIG",
                "A 200 2 300 19700101000100 19700101000000 42 signer.example. AQI="),
            Fixture(RecordDataModel.Key, "DNSKEY", "257 3 200 AQID"),
            Fixture(RecordDataModel.Ds, "DS", "12345 200 200 A1B2C3D4"),
            Fixture(RecordDataModel.Sshfp, "SSHFP", "200 200 A1B2"),
            Fixture(RecordDataModel.IpsecKey, "IPSECKEY",
                "1 0 2 . AQI="),
            Fixture(RecordDataModel.Nsec, "NSEC", "next.example. A AAAA"),
            Fixture(RecordDataModel.Nsec3, "NSEC3", "250 0 1 - 0410 A"),
            Fixture(RecordDataModel.Nsec3Param, "NSEC3PARAM", "250 0 1 -"),
            Fixture(RecordDataModel.Dhcid, "DHCID", "AAEAqg=="),
            Fixture(RecordDataModel.Tlsa, "TLSA", "3 1 200 A1B2"),
            Fixture(RecordDataModel.Hip, "HIP",
                "200 A1B2 AQID rendezvous.example."),
            Fixture(RecordDataModel.Csync, "CSYNC", "1 0 A AAAA"),
            Fixture(RecordDataModel.Zonemd, "ZONEMD",
                "1 250 250 " + zonemdDigest),
            Fixture(RecordDataModel.Svcb, "SVCB",
                "1 svc.example. port=8443"),
            Fixture(RecordDataModel.Dsync, "DSYNC",
                "CDS 1 5353 target.example."),
            Fixture(RecordDataModel.IlNp, "NID",
                "10 0011:2233:4455:6677"),
            Fixture(RecordDataModel.Eui, "EUI48", "00-11-22-33-44-55"),
            Fixture(RecordDataModel.Uri, "URI",
                "10 20 \"https://example/path\""),
            Fixture(RecordDataModel.Caa, "CAA",
                "0 \"issue\" \"ca.example\""),
            Fixture(RecordDataModel.AmtRelay, "AMTRELAY",
                "10 1 1 192.0.2.1"),
            Fixture(RecordDataModel.ResolverInformation, "RESINFO",
                "\"key=value\""),
            Fixture(RecordDataModel.Tkey, "TKEY",
                "alg.example. 1 2 3 0 AQI= AwQ="),
            Fixture(RecordDataModel.Tsig, "TSIG",
                "hmac.example. 1 300 AQI= 4660 0 AwQ=")
        };
    }

    static SchemaFixture Fixture(
        RecordDataModel model,
        string typeText,
        string dataText)
    {
        SchemaFixture fixture = new SchemaFixture();
        fixture.Model = model;
        fixture.TypeText = typeText;
        fixture.DataText = dataText;
        return fixture;
    }

    static DnsResourceRecord CopyRecordShell(DnsResourceRecord source)
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = source.Name;
        record.RecordType = source.RecordType;
        record.RecordTypeValue = source.RecordTypeValue;
        record.Class = source.Class;
        record.ClassValue = source.ClassValue;
        record.TimeToLive = source.TimeToLive;
        return record;
    }

    static string Diagnostic(DnsTextResult result)
    {
        if (result.Diagnostics.Count == 0)
        {
            return "no diagnostic";
        }

        return result.Diagnostics[0].Field + ": " +
            result.Diagnostics[0].Reason;
    }

    static string Diagnostic(DnsEncodeResult result)
    {
        if (result.Diagnostics.Count == 0)
        {
            return "no diagnostic";
        }

        return result.Diagnostics[0].Field + ": " +
            result.Diagnostics[0].Reason;
    }

    static string Diagnostic(DnsDecodeResult result)
    {
        if (result.Diagnostics.Count == 0)
        {
            return "no diagnostic";
        }

        return result.Diagnostics[0].Field + ": " +
            result.Diagnostics[0].Reason;
    }

    class SchemaFixture
    {
        internal RecordDataModel Model;
        internal string TypeText = string.Empty;
        internal string DataText = string.Empty;
    }
}
