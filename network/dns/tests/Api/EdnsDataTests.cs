namespace dns.Tests.Api;

public class EdnsDataTests
{
    [Test]
    public void EdnsDataStartsWithPathSafeIndependentOptions()
    {
        DnsEdnsData first = new DnsEdnsData();
        DnsEdnsData second = new DnsEdnsData();
        first.Flags = DnsEdnsFlag.DnssecAnswerOk;
        first.FlagsValue = 0x8000;
        first.ExtendedResponseCode = 1;
        first.Version = 0;
        first.Options.Add(new DnsEdnsOption());

        Assert.Multiple(() =>
        {
            Assert.That(first.UdpPayloadSize,
                Is.EqualTo(DnsProtocolLimits.DefaultEdnsUdpPayloadSize));
            Assert.That(first.FlagsValue, Is.EqualTo(0x8000));
            Assert.That(first.ExtendedResponseCode, Is.EqualTo(1));
            Assert.That(first.Options, Has.Count.EqualTo(1));
            Assert.That(second.Options, Is.Empty);
        });
    }

    [Test]
    public void UnknownOptionRetainsRawCodeAndOpaqueBytes()
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = (DnsEdnsOptionCode)65000;
        option.CodeValue = 65000;
        option.RawData = [0x00, 0xff, 0x80];
        option.UseRawData = true;

        Assert.Multiple(() =>
        {
            Assert.That((int)option.Code, Is.EqualTo(65000));
            Assert.That(option.CodeValue, Is.EqualTo(65000));
            Assert.That(option.RawData, Is.EqualTo(new byte[] { 0x00, 0xff, 0x80 }));
            Assert.That(option.UseRawData, Is.True);
        });
    }

    [Test]
    public void AlgorithmOptionsOwnIndependentRawCodeLists()
    {
        DnsEdnsOption first = new DnsEdnsOption();
        DnsEdnsOption second = new DnsEdnsOption();
        first.DnssecAlgorithmValues.Add(15);
        first.DnssecAlgorithmValues.Add(200);
        first.DigestAlgorithmValues.Add(4);
        first.Nsec3HashAlgorithmValues.Add(1);
        first.KeyTagValues.Add(65535);

        Assert.Multiple(() =>
        {
            Assert.That(first.DnssecAlgorithmValues, Is.EqualTo(new[] { 15, 200 }));
            Assert.That(first.DigestAlgorithmValues, Is.EqualTo(new[] { 4 }));
            Assert.That(first.Nsec3HashAlgorithmValues, Is.EqualTo(new[] { 1 }));
            Assert.That(first.KeyTagValues, Is.EqualTo(new[] { 65535 }));
            Assert.That(second.DnssecAlgorithmValues, Is.Empty);
            Assert.That(second.KeyTagValues, Is.Empty);
        });
    }

    [Test]
    public void ClientSubnetRetainsMaskedPrefixOctets()
    {
        DnsEdnsClientSubnet subnet = new DnsEdnsClientSubnet();
        subnet.AddressFamily = 2;
        subnet.SourcePrefixLength = 56;
        subnet.ScopePrefixLength = 48;
        subnet.Address = [0x20, 0x01, 0x0d, 0xb8, 0xab, 0xcd, 0xef];

        Assert.Multiple(() =>
        {
            Assert.That(subnet.AddressFamily, Is.EqualTo(2));
            Assert.That(subnet.SourcePrefixLength, Is.EqualTo(56));
            Assert.That(subnet.ScopePrefixLength, Is.EqualTo(48));
            Assert.That(subnet.Address, Has.Length.EqualTo(7));
        });
    }

    [Test]
    public void CookieAndKeepaliveRepresentRequestAndResponseForms()
    {
        DnsEdnsCookie cookie = new DnsEdnsCookie();
        cookie.ClientCookie = [0, 1, 2, 3, 4, 5, 6, 7];
        cookie.ServerCookie = new byte[32];
        DnsEdnsKeepalive requestKeepalive = new DnsEdnsKeepalive();
        DnsEdnsKeepalive responseKeepalive = new DnsEdnsKeepalive();
        responseKeepalive.HasTimeout = true;
        responseKeepalive.TimeoutUnits = 300;

        Assert.Multiple(() =>
        {
            Assert.That(cookie.ClientCookie, Has.Length.EqualTo(8));
            Assert.That(cookie.ServerCookie, Has.Length.EqualTo(32));
            Assert.That(requestKeepalive.HasTimeout, Is.False);
            Assert.That(responseKeepalive.HasTimeout, Is.True);
            Assert.That(responseKeepalive.TimeoutUnits, Is.EqualTo(300));
        });
    }

    [Test]
    public void LeaseExpireAndNameOptionsRetainPresenceAndBoundaries()
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.HasUpdateLease = true;
        option.UpdateLeaseSeconds = uint.MaxValue;
        option.HasExpire = true;
        option.ExpireSeconds = uint.MaxValue;
        option.ChainName = new DnsName();
        option.ChainName.IsAbsolute = true;
        option.ReportChannel = new DnsName();
        option.ReportChannel.IsAbsolute = true;

        Assert.Multiple(() =>
        {
            Assert.That(option.HasUpdateLease, Is.True);
            Assert.That(option.UpdateLeaseSeconds, Is.EqualTo(uint.MaxValue));
            Assert.That(option.HasExpire, Is.True);
            Assert.That(option.ExpireSeconds, Is.EqualTo(uint.MaxValue));
            Assert.That(option.ChainName.IsAbsolute, Is.True);
            Assert.That(option.ReportChannel.IsAbsolute, Is.True);
        });
    }

    [Test]
    public void ExtendedErrorRetainsKnownAndUnknownInformationCodes()
    {
        DnsEdnsExtendedError known = new DnsEdnsExtendedError();
        known.Code = DnsExtendedErrorCode.Blocked;
        known.CodeValue = 15;
        known.ExtraText = "Policy block";
        DnsEdnsExtendedError unknown = new DnsEdnsExtendedError();
        unknown.Code = (DnsExtendedErrorCode)65000;
        unknown.CodeValue = 65000;

        Assert.Multiple(() =>
        {
            Assert.That(known.Code, Is.EqualTo(DnsExtendedErrorCode.Blocked));
            Assert.That(known.CodeValue, Is.EqualTo(15));
            Assert.That(known.ExtraText, Is.EqualTo("Policy block"));
            Assert.That((int)unknown.Code, Is.EqualTo(65000));
            Assert.That(unknown.CodeValue, Is.EqualTo(65000));
            Assert.That(unknown.ExtraText, Is.Null);
        });
    }

    [Test]
    public void ZoneVersionDistinguishesEmptyQueryFromResponseData()
    {
        DnsEdnsZoneVersion query = new DnsEdnsZoneVersion();
        DnsEdnsZoneVersion response = new DnsEdnsZoneVersion();
        response.HasVersion = true;
        response.LabelCount = 2;
        response.Type = 0;
        response.Version = [0x78, 0x95, 0xa4, 0xe9];

        Assert.Multiple(() =>
        {
            Assert.That(query.HasVersion, Is.False);
            Assert.That(query.Version, Is.Empty);
            Assert.That(response.HasVersion, Is.True);
            Assert.That(response.LabelCount, Is.EqualTo(2));
            Assert.That(response.Type, Is.Zero);
            Assert.That(response.Version,
                Is.EqualTo(new byte[] { 0x78, 0x95, 0xa4, 0xe9 }));
        });
    }

    [Test]
    public void EdnsOptionResultsOwnBuffersAndDiagnostics()
    {
        DnsEdnsOptionResult first = new DnsEdnsOptionResult();
        DnsEdnsOptionResult second = new DnsEdnsOptionResult();
        first.Success = true;
        first.BytesProcessed = 7;
        first.Option = new DnsEdnsOption();
        first.Bytes = [0x00, 0x0f, 0x00, 0x03, 0x00, 0x0f, 0x41];
        first.Diagnostics.Add(new DnsDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.BytesProcessed, Is.EqualTo(7));
            Assert.That(first.Option, Is.Not.Null);
            Assert.That(first.Bytes, Has.Length.EqualTo(7));
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.Bytes, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void OptRecordUsesTypedEdnsContainer()
    {
        DnsResourceRecord record = new DnsResourceRecord();
        record.RecordType = DnsRecordType.Opt;
        record.RecordTypeValue = 41;
        record.Data = new DnsRecordData();
        record.Data.Edns = new DnsEdnsData();
        record.Data.Edns.Options.Add(new DnsEdnsOption());

        Assert.Multiple(() =>
        {
            Assert.That(record.Data.Edns.UdpPayloadSize, Is.EqualTo(1232));
            Assert.That(record.Data.Edns.Options, Has.Count.EqualTo(1));
        });
    }
}
