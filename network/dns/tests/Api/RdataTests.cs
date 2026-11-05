namespace dns.Tests.Api;

public class RdataTests
{
    [Test]
    public void SharedSchemasRetainExactOctetsAndOwnCollections()
    {
        DnsAddressData address = new DnsAddressData();
        address.Address = [192, 0, 2, 1];
        DnsCharacterStringsData firstStrings = new DnsCharacterStringsData();
        DnsCharacterStringsData secondStrings = new DnsCharacterStringsData();
        firstStrings.Strings.Add([0x00, 0xff]);
        DnsPreferenceNameData preference = new DnsPreferenceNameData();
        preference.Preference = 65535;
        preference.Name.IsAbsolute = true;

        Assert.Multiple(() =>
        {
            Assert.That(address.Address, Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(firstStrings.Strings[0], Is.EqualTo(new byte[] { 0x00, 0xff }));
            Assert.That(secondStrings.Strings, Is.Empty);
            Assert.That(preference.Preference, Is.EqualTo(65535));
            Assert.That(preference.Name.IsAbsolute, Is.True);
        });
    }

    [Test]
    public void ResourceRecordCanCarryTypedAndOriginalDataTogether()
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.RecordType = DnsRecordType.Soa;
        record.RecordTypeValue = 6;
        record.Data = new DnsRecordData();
        record.Data.Soa = new DnsSoaData();
        record.Data.Soa.Serial = uint.MaxValue;
        record.RawData = [1, 2, 3];

        Assert.Multiple(() =>
        {
            Assert.That(record.Data.Soa.Serial, Is.EqualTo(uint.MaxValue));
            Assert.That(record.RawData, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(record.UseRawData, Is.False);
        });
    }

    [Test]
    public void ClassicAndLegacySchemasRetainBoundaryValues()
    {
        DnsSoaData soa = new DnsSoaData();
        soa.Serial = uint.MaxValue;
        soa.Refresh = uint.MaxValue;
        soa.Retry = uint.MaxValue;
        soa.Expire = uint.MaxValue;
        soa.Minimum = uint.MaxValue;
        DnsA6Data a6 = new DnsA6Data();
        a6.PrefixLength = 128;
        a6.AddressSuffix = [];
        a6.PrefixName = new DnsName();
        DnsLocData loc = new DnsLocData();
        loc.Latitude = uint.MaxValue;
        loc.Longitude = uint.MaxValue;
        loc.Altitude = uint.MaxValue;

        Assert.Multiple(() =>
        {
            Assert.That(soa.Serial, Is.EqualTo(uint.MaxValue));
            Assert.That(soa.Minimum, Is.EqualTo(uint.MaxValue));
            Assert.That(a6.PrefixLength, Is.EqualTo(128));
            Assert.That(a6.PrefixName, Is.Not.Null);
            Assert.That(loc.Latitude, Is.EqualTo(uint.MaxValue));
            Assert.That(loc.Longitude, Is.EqualTo(uint.MaxValue));
            Assert.That(loc.Altitude, Is.EqualTo(uint.MaxValue));
        });
    }

    [Test]
    public void AddressPrefixListsOwnTypedItems()
    {
        DnsAplData first = new DnsAplData();
        DnsAplData second = new DnsAplData();
        DnsAplItem item = new DnsAplItem();
        item.AddressFamily = 1;
        item.PrefixLength = 24;
        item.Negated = true;
        item.Address = [192, 0, 2];
        first.Items.Add(item);

        Assert.Multiple(() =>
        {
            Assert.That(first.Items, Has.Count.EqualTo(1));
            Assert.That(first.Items[0].Negated, Is.True);
            Assert.That(first.Items[0].Address, Is.EqualTo(new byte[] { 192, 0, 2 }));
            Assert.That(second.Items, Is.Empty);
        });
    }

    [Test]
    public void SecuritySchemasPreserveRawRegistryCodes()
    {
        DnsSignatureData signature = new DnsSignatureData();
        signature.TypeCovered = (DnsRecordType)65000;
        signature.TypeCoveredValue = 65000;
        signature.Algorithm = (DnsDnssecAlgorithm)200;
        signature.AlgorithmValue = 200;
        signature.SignatureExpiration = uint.MaxValue;
        signature.SignatureInception = 0;
        DnsDsData ds = new DnsDsData();
        ds.Algorithm = DnsDnssecAlgorithm.Ed25519;
        ds.AlgorithmValue = 15;
        ds.DigestAlgorithm = (DnsDigestAlgorithm)200;
        ds.DigestAlgorithmValue = 200;

        Assert.Multiple(() =>
        {
            Assert.That((int)signature.TypeCovered, Is.EqualTo(65000));
            Assert.That(signature.TypeCoveredValue, Is.EqualTo(65000));
            Assert.That((int)signature.Algorithm, Is.EqualTo(200));
            Assert.That(signature.SignatureExpiration, Is.EqualTo(uint.MaxValue));
            Assert.That(ds.AlgorithmValue, Is.EqualTo(15));
            Assert.That((int)ds.DigestAlgorithm, Is.EqualTo(200));
            Assert.That(ds.DigestAlgorithmValue, Is.EqualTo(200));
        });
    }

    [Test]
    public void NsecAndHipCollectionsAreIndependent()
    {
        DnsNsec3Data firstNsec = new DnsNsec3Data();
        DnsNsec3Data secondNsec = new DnsNsec3Data();
        firstNsec.RecordTypeValues.Add(1);
        firstNsec.RecordTypeValues.Add(65535);
        DnsHipData firstHip = new DnsHipData();
        DnsHipData secondHip = new DnsHipData();
        firstHip.RendezvousServers.Add(new DnsName());

        Assert.Multiple(() =>
        {
            Assert.That(firstNsec.RecordTypeValues, Is.EqualTo(new[] { 1, 65535 }));
            Assert.That(secondNsec.RecordTypeValues, Is.Empty);
            Assert.That(firstHip.RendezvousServers, Has.Count.EqualTo(1));
            Assert.That(secondHip.RendezvousServers, Is.Empty);
        });
    }

    [Test]
    public void SvcbParametersRetainTypedAndOpaqueForms()
    {
        DnsSvcbParameter parameter = new DnsSvcbParameter();
        parameter.Key = DnsSvcbParameterKey.Alpn;
        parameter.KeyValue = 1;
        parameter.RawValue = [2, 0x68, 0x32];
        parameter.AlpnIdentifiers.Add([0x68, 0x32]);
        parameter.HasPort = true;
        parameter.Port = 443;
        parameter.UseRawValue = true;
        DnsSvcbData data = new DnsSvcbData();
        data.Priority = 1;
        data.Parameters.Add(parameter);

        Assert.Multiple(() =>
        {
            Assert.That(data.Parameters, Has.Count.EqualTo(1));
            Assert.That(parameter.KeyValue, Is.EqualTo(1));
            Assert.That(parameter.AlpnIdentifiers[0], Is.EqualTo(new byte[] { 0x68, 0x32 }));
            Assert.That(parameter.Port, Is.EqualTo(443));
            Assert.That(parameter.UseRawValue, Is.True);
        });
    }

    [Test]
    public void TransactionSecuritySchemasRetainWideTimeAndErrors()
    {
        DnsTkeyData tkey = new DnsTkeyData();
        tkey.Inception = uint.MaxValue;
        tkey.Expiration = uint.MaxValue;
        tkey.Key = [0xaa];
        DnsTsigData tsig = new DnsTsigData();
        tsig.TimeSigned = 0xffffffffffffUL;
        tsig.Error = DnsTsigError.BadTime;
        tsig.ErrorValue = 18;
        tsig.Mac = [0xbb];

        Assert.Multiple(() =>
        {
            Assert.That(tkey.Inception, Is.EqualTo(uint.MaxValue));
            Assert.That(tkey.Key, Is.EqualTo(new byte[] { 0xaa }));
            Assert.That(tsig.TimeSigned, Is.EqualTo(0xffffffffffffUL));
            Assert.That(tsig.ErrorValue, Is.EqualTo(18));
            Assert.That(tsig.Mac, Is.EqualTo(new byte[] { 0xbb }));
        });
    }

    [Test]
    public void EveryDeclaredRecordTypeHasAnExplicitDataModel()
    {
        DnsRecordType[] recordTypes = Enum.GetValues<DnsRecordType>();
        int index = 0;
        while (index < recordTypes.Length)
        {
            RecordDataModel model = RecordDataModelCatalog.ReadRecordDataModel(
                (int)recordTypes[index]);
            Assert.That(model, Is.Not.EqualTo(RecordDataModel.Unassigned),
                recordTypes[index].ToString());
            index = index + 1;
        }
    }

    [TestCase(DnsRecordType.A, 2)]
    [TestCase(DnsRecordType.Ns, 3)]
    [TestCase(DnsRecordType.Minfo, 4)]
    [TestCase(DnsRecordType.Mx, 5)]
    [TestCase(DnsRecordType.Txt, 6)]
    [TestCase(DnsRecordType.Eid, 7)]
    [TestCase(DnsRecordType.Nsap, 8)]
    [TestCase(DnsRecordType.Soa, 9)]
    [TestCase(DnsRecordType.Rrsig, 18)]
    [TestCase(DnsRecordType.Dnskey, 19)]
    [TestCase(DnsRecordType.Ds, 20)]
    [TestCase(DnsRecordType.Svcb, 31)]
    [TestCase(DnsRecordType.Tkey, 39)]
    [TestCase(DnsRecordType.Tsig, 40)]
    [TestCase(DnsRecordType.Opt, 41)]
    public void RepresentativeTypesSelectExpectedSchemas(
        DnsRecordType recordType,
        int expectedModel)
    {
        RecordDataModel model = RecordDataModelCatalog.ReadRecordDataModel(
            (int)recordType);

        Assert.That((int)model, Is.EqualTo(expectedModel));
    }

    [Test]
    public void UnassignedAndPrivateTypesUseFutureDispatch()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RecordDataModelCatalog.ReadRecordDataModel(54),
                Is.EqualTo(RecordDataModel.Unassigned));
            Assert.That(RecordDataModelCatalog.ReadRecordDataModel(65000),
                Is.EqualTo(RecordDataModel.Unassigned));
        });
    }
}
