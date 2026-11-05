namespace dns.Tests.Api;

public class EnumTests
{
    [TestCase(DnsRecordType.Reserved, 0)]
    [TestCase(DnsRecordType.A, 1)]
    [TestCase(DnsRecordType.Ns, 2)]
    [TestCase(DnsRecordType.Md, 3)]
    [TestCase(DnsRecordType.Mf, 4)]
    [TestCase(DnsRecordType.Cname, 5)]
    [TestCase(DnsRecordType.Soa, 6)]
    [TestCase(DnsRecordType.Mb, 7)]
    [TestCase(DnsRecordType.Mg, 8)]
    [TestCase(DnsRecordType.Mr, 9)]
    [TestCase(DnsRecordType.Null, 10)]
    [TestCase(DnsRecordType.Wks, 11)]
    [TestCase(DnsRecordType.Ptr, 12)]
    [TestCase(DnsRecordType.Hinfo, 13)]
    [TestCase(DnsRecordType.Minfo, 14)]
    [TestCase(DnsRecordType.Mx, 15)]
    [TestCase(DnsRecordType.Txt, 16)]
    [TestCase(DnsRecordType.Rp, 17)]
    [TestCase(DnsRecordType.Afsdb, 18)]
    [TestCase(DnsRecordType.X25, 19)]
    [TestCase(DnsRecordType.Isdn, 20)]
    [TestCase(DnsRecordType.Rt, 21)]
    [TestCase(DnsRecordType.Nsap, 22)]
    [TestCase(DnsRecordType.NsapPointer, 23)]
    [TestCase(DnsRecordType.Sig, 24)]
    [TestCase(DnsRecordType.Key, 25)]
    [TestCase(DnsRecordType.Px, 26)]
    [TestCase(DnsRecordType.Gpos, 27)]
    [TestCase(DnsRecordType.Aaaa, 28)]
    [TestCase(DnsRecordType.Loc, 29)]
    [TestCase(DnsRecordType.Nxt, 30)]
    [TestCase(DnsRecordType.Eid, 31)]
    [TestCase(DnsRecordType.Nimloc, 32)]
    [TestCase(DnsRecordType.Srv, 33)]
    [TestCase(DnsRecordType.Atma, 34)]
    [TestCase(DnsRecordType.Naptr, 35)]
    [TestCase(DnsRecordType.Kx, 36)]
    [TestCase(DnsRecordType.Cert, 37)]
    [TestCase(DnsRecordType.A6, 38)]
    [TestCase(DnsRecordType.Dname, 39)]
    [TestCase(DnsRecordType.Sink, 40)]
    [TestCase(DnsRecordType.Opt, 41)]
    [TestCase(DnsRecordType.Apl, 42)]
    [TestCase(DnsRecordType.Ds, 43)]
    [TestCase(DnsRecordType.Sshfp, 44)]
    [TestCase(DnsRecordType.IpsecKey, 45)]
    [TestCase(DnsRecordType.Rrsig, 46)]
    [TestCase(DnsRecordType.Nsec, 47)]
    [TestCase(DnsRecordType.Dnskey, 48)]
    [TestCase(DnsRecordType.Dhcid, 49)]
    [TestCase(DnsRecordType.Nsec3, 50)]
    [TestCase(DnsRecordType.Nsec3Param, 51)]
    [TestCase(DnsRecordType.Tlsa, 52)]
    [TestCase(DnsRecordType.Smimea, 53)]
    [TestCase(DnsRecordType.Hip, 55)]
    [TestCase(DnsRecordType.Ninfo, 56)]
    [TestCase(DnsRecordType.Rkey, 57)]
    [TestCase(DnsRecordType.Talink, 58)]
    [TestCase(DnsRecordType.Cds, 59)]
    [TestCase(DnsRecordType.Cdnskey, 60)]
    [TestCase(DnsRecordType.OpenPgpKey, 61)]
    [TestCase(DnsRecordType.Csync, 62)]
    [TestCase(DnsRecordType.Zonemd, 63)]
    [TestCase(DnsRecordType.Svcb, 64)]
    [TestCase(DnsRecordType.Https, 65)]
    [TestCase(DnsRecordType.Dsync, 66)]
    [TestCase(DnsRecordType.Hhit, 67)]
    [TestCase(DnsRecordType.Brid, 68)]
    [TestCase(DnsRecordType.Unece, 69)]
    [TestCase(DnsRecordType.Iso, 70)]
    [TestCase(DnsRecordType.Spf, 99)]
    [TestCase(DnsRecordType.Uinfo, 100)]
    [TestCase(DnsRecordType.Uid, 101)]
    [TestCase(DnsRecordType.Gid, 102)]
    [TestCase(DnsRecordType.Unspec, 103)]
    [TestCase(DnsRecordType.Nid, 104)]
    [TestCase(DnsRecordType.L32, 105)]
    [TestCase(DnsRecordType.L64, 106)]
    [TestCase(DnsRecordType.Lp, 107)]
    [TestCase(DnsRecordType.Eui48, 108)]
    [TestCase(DnsRecordType.Eui64, 109)]
    [TestCase(DnsRecordType.Nxname, 128)]
    [TestCase(DnsRecordType.Tkey, 249)]
    [TestCase(DnsRecordType.Tsig, 250)]
    [TestCase(DnsRecordType.Ixfr, 251)]
    [TestCase(DnsRecordType.Axfr, 252)]
    [TestCase(DnsRecordType.Mailb, 253)]
    [TestCase(DnsRecordType.Maila, 254)]
    [TestCase(DnsRecordType.Any, 255)]
    [TestCase(DnsRecordType.Uri, 256)]
    [TestCase(DnsRecordType.Caa, 257)]
    [TestCase(DnsRecordType.Avc, 258)]
    [TestCase(DnsRecordType.Doa, 259)]
    [TestCase(DnsRecordType.AmtRelay, 260)]
    [TestCase(DnsRecordType.ResolverInformation, 261)]
    [TestCase(DnsRecordType.Wallet, 262)]
    [TestCase(DnsRecordType.Cla, 263)]
    [TestCase(DnsRecordType.Ipn, 264)]
    [TestCase(DnsRecordType.Ta, 32768)]
    [TestCase(DnsRecordType.Dlv, 32769)]
    public void AssignedRecordTypesUseRegistryValues(
        DnsRecordType recordType,
        int expected)
    {
        Assert.That((int)recordType, Is.EqualTo(expected));
    }

    [Test]
    public void ProtocolRegistriesUseWireValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)DnsClass.Internet, Is.EqualTo(1));
            Assert.That((int)DnsClass.Chaos, Is.EqualTo(3));
            Assert.That((int)DnsClass.Hesiod, Is.EqualTo(4));
            Assert.That((int)DnsClass.None, Is.EqualTo(254));
            Assert.That((int)DnsClass.Any, Is.EqualTo(255));
            Assert.That((int)DnsOpcode.Query, Is.EqualTo(0));
            Assert.That((int)DnsOpcode.Notify, Is.EqualTo(4));
            Assert.That((int)DnsOpcode.Update, Is.EqualTo(5));
            Assert.That((int)DnsOpcode.StatefulOperations, Is.EqualTo(6));
            Assert.That((int)DnsResponseCode.NameError, Is.EqualTo(3));
            Assert.That((int)DnsResponseCode.NotZone, Is.EqualTo(10));
            Assert.That((int)DnsResponseCode.BadVersion, Is.EqualTo(16));
            Assert.That((int)DnsResponseCode.BadCookie, Is.EqualTo(23));
            Assert.That((int)DnsTsigError.BadSignature, Is.EqualTo(16));
            Assert.That((int)DnsMessageSection.Additional, Is.EqualTo(3));
        });
    }

    [Test]
    public void HeaderAndEdnsFlagsComposeAsWireMasks()
    {
        DnsHeaderFlag headerFlags = DnsHeaderFlag.Response |
            DnsHeaderFlag.RecursionDesired |
            DnsHeaderFlag.CheckingDisabled;
        DnsEdnsFlag ednsFlags = DnsEdnsFlag.DnssecAnswerOk |
            DnsEdnsFlag.CompactAnswersOk;

        Assert.Multiple(() =>
        {
            Assert.That((int)headerFlags, Is.EqualTo(0x8110));
            Assert.That((int)DnsHeaderFlag.AuthoritativeAnswer, Is.EqualTo(0x0400));
            Assert.That((int)DnsHeaderFlag.Reserved, Is.EqualTo(0x0040));
            Assert.That((int)ednsFlags, Is.EqualTo(0xc000));
            Assert.That((int)DnsEdnsFlag.DelegationExtensions, Is.EqualTo(0x2000));
        });
    }

    [Test]
    public void EdnsOptionCodesUseRegistryValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)DnsEdnsOptionCode.UpdateLease, Is.EqualTo(2));
            Assert.That((int)DnsEdnsOptionCode.ClientSubnet, Is.EqualTo(8));
            Assert.That((int)DnsEdnsOptionCode.Cookie, Is.EqualTo(10));
            Assert.That((int)DnsEdnsOptionCode.ExtendedDnsError, Is.EqualTo(15));
            Assert.That((int)DnsEdnsOptionCode.ZoneVersion, Is.EqualTo(19));
            Assert.That((int)DnsEdnsOptionCode.StructuredDnsError, Is.EqualTo(26));
            Assert.That((int)DnsEdnsOptionCode.UmbrellaIdentifier, Is.EqualTo(20292));
            Assert.That((int)DnsEdnsOptionCode.DeviceIdentifier, Is.EqualTo(26946));
            Assert.That((int)DnsEdnsOptionCode.LocalExperimentalStart, Is.EqualTo(65001));
            Assert.That((int)DnsEdnsOptionCode.ReservedForFutureExpansion, Is.EqualTo(65535));
        });
    }

    [Test]
    public void RecordAndSecurityRegistriesUseAssignedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)DnsSvcbParameterKey.Mandatory, Is.EqualTo(0));
            Assert.That((int)DnsSvcbParameterKey.Ech, Is.EqualTo(5));
            Assert.That((int)DnsSvcbParameterKey.ObliviousHttp, Is.EqualTo(8));
            Assert.That((int)DnsDnssecAlgorithm.RsaSha256, Is.EqualTo(8));
            Assert.That((int)DnsDnssecAlgorithm.Ed25519, Is.EqualTo(15));
            Assert.That((int)DnsDnssecAlgorithm.MlDsa44, Is.EqualTo(18));
            Assert.That((int)DnsDnssecAlgorithm.EccGost12, Is.EqualTo(23));
            Assert.That((int)DnsDnssecAlgorithm.PrivateOid, Is.EqualTo(254));
            Assert.That((int)DnsDigestAlgorithm.Sha384, Is.EqualTo(4));
            Assert.That((int)DnsDigestAlgorithm.Sm3, Is.EqualTo(6));
            Assert.That((int)DnsNsec3HashAlgorithm.Sha1, Is.EqualTo(1));
            Assert.That((int)DnsTsigError.BadTruncation, Is.EqualTo(22));
            Assert.That((int)DnsExtendedErrorCode.UnsupportedDnskeyAlgorithm, Is.EqualTo(1));
            Assert.That((int)DnsExtendedErrorCode.BlockedByUpstream, Is.EqualTo(35));
        });
    }

    [Test]
    public void OperationStateEnumsHaveStableValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)DnsDiagnosticCode.ResponseMismatch, Is.EqualTo(25));
            Assert.That((int)DnsTransportKind.Quic, Is.EqualTo(4));
            Assert.That((int)DnsTransportStatus.Closed, Is.EqualTo(8));
            Assert.That((int)DnsCacheEntryKind.Failure, Is.EqualTo(3));
            Assert.That((int)DnsResolveStatus.Unsupported, Is.EqualTo(10));
            Assert.That((int)DnsAuthoritativeAnswerKind.ServerFailure, Is.EqualTo(7));
            Assert.That((int)DnsZoneChangeKind.DeleteName, Is.EqualTo(5));
            Assert.That((int)DnsTransferKind.Ixfr, Is.EqualTo(1));
            Assert.That((int)DnsUpdatePrerequisiteKind.RecordSetDoesNotExist, Is.EqualTo(4));
            Assert.That((int)DnsUpdateChangeKind.DeleteName, Is.EqualTo(3));
            Assert.That((int)DnsServerStatus.Failed, Is.EqualTo(3));
            Assert.That((int)DnsSecurityStatus.UnsupportedAlgorithm, Is.EqualTo(4));
            Assert.That((int)DnsAlgorithmPolicy.Forbidden, Is.EqualTo(3));
        });
    }

    [TestCase(2)]
    [TestCase(54)]
    [TestCase(65000)]
    public void UnassignedProtocolCodesRoundTripWithoutAnUnknownSentinel(int wireValue)
    {
        DnsRecordType recordType = (DnsRecordType)wireValue;

        Assert.That((int)recordType, Is.EqualTo(wireValue));
    }
}
