namespace dns;

/// <summary>Identifies a DNS resource-record type or query type.</summary>
public enum DnsRecordType
{
    /// <summary>Identifies reserved record type zero.</summary>
    Reserved = 0,

    /// <summary>Identifies an IPv4 host address.</summary>
    A = 1,

    /// <summary>Identifies an authoritative name server.</summary>
    Ns = 2,

    /// <summary>Identifies the obsolete mail-destination record.</summary>
    Md = 3,

    /// <summary>Identifies the obsolete mail-forwarder record.</summary>
    Mf = 4,

    /// <summary>Identifies a canonical-name alias.</summary>
    Cname = 5,

    /// <summary>Identifies the start of a zone of authority.</summary>
    Soa = 6,

    /// <summary>Identifies an experimental mailbox domain name.</summary>
    Mb = 7,

    /// <summary>Identifies an experimental mail-group member.</summary>
    Mg = 8,

    /// <summary>Identifies an experimental mail-rename domain name.</summary>
    Mr = 9,

    /// <summary>Identifies an experimental null record.</summary>
    Null = 10,

    /// <summary>Identifies a well-known service description.</summary>
    Wks = 11,

    /// <summary>Identifies a domain-name pointer.</summary>
    Ptr = 12,

    /// <summary>Identifies host information.</summary>
    Hinfo = 13,

    /// <summary>Identifies mailbox or mail-list information.</summary>
    Minfo = 14,

    /// <summary>Identifies a mail exchange.</summary>
    Mx = 15,

    /// <summary>Identifies text strings.</summary>
    Txt = 16,

    /// <summary>Identifies a responsible person.</summary>
    Rp = 17,

    /// <summary>Identifies an AFS database location.</summary>
    Afsdb = 18,

    /// <summary>Identifies an X.25 PSDN address.</summary>
    X25 = 19,

    /// <summary>Identifies an ISDN address.</summary>
    Isdn = 20,

    /// <summary>Identifies a route-through host.</summary>
    Rt = 21,

    /// <summary>Identifies a deprecated NSAP address.</summary>
    Nsap = 22,

    /// <summary>Identifies a deprecated NSAP domain-name pointer.</summary>
    NsapPointer = 23,

    /// <summary>Identifies a legacy security signature.</summary>
    Sig = 24,

    /// <summary>Identifies a legacy security key.</summary>
    Key = 25,

    /// <summary>Identifies X.400 mail mapping information.</summary>
    Px = 26,

    /// <summary>Identifies a geographical position.</summary>
    Gpos = 27,

    /// <summary>Identifies an IPv6 host address.</summary>
    Aaaa = 28,

    /// <summary>Identifies geographic location information.</summary>
    Loc = 29,

    /// <summary>Identifies the obsolete next-domain record.</summary>
    Nxt = 30,

    /// <summary>Identifies an endpoint identifier.</summary>
    Eid = 31,

    /// <summary>Identifies a Nimrod locator.</summary>
    Nimloc = 32,

    /// <summary>Identifies server-selection data.</summary>
    Srv = 33,

    /// <summary>Identifies an ATM address.</summary>
    Atma = 34,

    /// <summary>Identifies naming-authority pointer data.</summary>
    Naptr = 35,

    /// <summary>Identifies a key exchanger.</summary>
    Kx = 36,

    /// <summary>Identifies a certificate record.</summary>
    Cert = 37,

    /// <summary>Identifies the obsolete A6 IPv6 address record.</summary>
    A6 = 38,

    /// <summary>Identifies a delegation-name alias.</summary>
    Dname = 39,

    /// <summary>Identifies the experimental SINK record.</summary>
    Sink = 40,

    /// <summary>Identifies an EDNS OPT pseudo-record.</summary>
    Opt = 41,

    /// <summary>Identifies an address-prefix list.</summary>
    Apl = 42,

    /// <summary>Identifies a delegation signer.</summary>
    Ds = 43,

    /// <summary>Identifies an SSH key fingerprint.</summary>
    Sshfp = 44,

    /// <summary>Identifies an IPsec public key.</summary>
    IpsecKey = 45,

    /// <summary>Identifies a DNSSEC resource-record signature.</summary>
    Rrsig = 46,

    /// <summary>Identifies an authenticated next-domain record.</summary>
    Nsec = 47,

    /// <summary>Identifies a DNSSEC public key.</summary>
    Dnskey = 48,

    /// <summary>Identifies a DHCP identifier.</summary>
    Dhcid = 49,

    /// <summary>Identifies a hashed authenticated denial record.</summary>
    Nsec3 = 50,

    /// <summary>Identifies NSEC3 parameters.</summary>
    Nsec3Param = 51,

    /// <summary>Identifies a TLS certificate association.</summary>
    Tlsa = 52,

    /// <summary>Identifies an S/MIME certificate association.</summary>
    Smimea = 53,

    /// <summary>Identifies Host Identity Protocol data.</summary>
    Hip = 55,

    /// <summary>Identifies the NINFO record.</summary>
    Ninfo = 56,

    /// <summary>Identifies the RKEY record.</summary>
    Rkey = 57,

    /// <summary>Identifies a trust-anchor link.</summary>
    Talink = 58,

    /// <summary>Identifies a child delegation signer.</summary>
    Cds = 59,

    /// <summary>Identifies a child DNSKEY request.</summary>
    Cdnskey = 60,

    /// <summary>Identifies an OpenPGP public key.</summary>
    OpenPgpKey = 61,

    /// <summary>Identifies child-to-parent synchronization data.</summary>
    Csync = 62,

    /// <summary>Identifies a zone message digest.</summary>
    Zonemd = 63,

    /// <summary>Identifies a general-purpose service binding.</summary>
    Svcb = 64,

    /// <summary>Identifies an HTTPS service binding.</summary>
    Https = 65,

    /// <summary>Identifies a delegation-synchronization endpoint.</summary>
    Dsync = 66,

    /// <summary>Identifies a Hierarchical Host Identity Tag.</summary>
    Hhit = 67,

    /// <summary>Identifies UAS Broadcast Remote Identification data.</summary>
    Brid = 68,

    /// <summary>Identifies a value coded by a UNECE recommendation.</summary>
    Unece = 69,

    /// <summary>Identifies a value coded by an ISO standard.</summary>
    Iso = 70,

    /// <summary>Identifies the obsolete SPF record.</summary>
    Spf = 99,

    /// <summary>Identifies user information.</summary>
    Uinfo = 100,

    /// <summary>Identifies a user identifier.</summary>
    Uid = 101,

    /// <summary>Identifies a group identifier.</summary>
    Gid = 102,

    /// <summary>Identifies unspecified binary data.</summary>
    Unspec = 103,

    /// <summary>Identifies a node identifier.</summary>
    Nid = 104,

    /// <summary>Identifies a 32-bit locator.</summary>
    L32 = 105,

    /// <summary>Identifies a 64-bit locator.</summary>
    L64 = 106,

    /// <summary>Identifies a locator pointer.</summary>
    Lp = 107,

    /// <summary>Identifies an EUI-48 address.</summary>
    Eui48 = 108,

    /// <summary>Identifies an EUI-64 address.</summary>
    Eui64 = 109,

    /// <summary>Identifies the compact-denial nonexistent-name marker.</summary>
    Nxname = 128,

    /// <summary>Identifies a transaction key.</summary>
    Tkey = 249,

    /// <summary>Identifies a transaction signature.</summary>
    Tsig = 250,

    /// <summary>Identifies an incremental zone-transfer query.</summary>
    Ixfr = 251,

    /// <summary>Identifies a full zone-transfer query.</summary>
    Axfr = 252,

    /// <summary>Identifies the mailbox-record query type.</summary>
    Mailb = 253,

    /// <summary>Identifies the obsolete mail-agent query type.</summary>
    Maila = 254,

    /// <summary>Identifies the ANY query type.</summary>
    Any = 255,

    /// <summary>Identifies a URI record.</summary>
    Uri = 256,

    /// <summary>Identifies a certification-authority authorization record.</summary>
    Caa = 257,

    /// <summary>Identifies Application Visibility and Control data.</summary>
    Avc = 258,

    /// <summary>Identifies Digital Object Architecture data.</summary>
    Doa = 259,

    /// <summary>Identifies an Automatic Multicast Tunneling relay.</summary>
    AmtRelay = 260,

    /// <summary>Identifies resolver information key-value pairs.</summary>
    ResolverInformation = 261,

    /// <summary>Identifies a public wallet address.</summary>
    Wallet = 262,

    /// <summary>Identifies a Bundle Protocol convergence-layer adapter.</summary>
    Cla = 263,

    /// <summary>Identifies a Bundle Protocol node number.</summary>
    Ipn = 264,

    /// <summary>Identifies a DNSSEC trust authority.</summary>
    Ta = 32768,

    /// <summary>Identifies the obsolete DNSSEC lookaside-validation record.</summary>
    Dlv = 32769
}

/// <summary>Identifies a Service Binding parameter key.</summary>
public enum DnsSvcbParameterKey
{
    /// <summary>Identifies the list of mandatory parameter keys.</summary>
    Mandatory = 0,

    /// <summary>Identifies supported application-layer protocols.</summary>
    Alpn = 1,

    /// <summary>Disables use of a default application-layer protocol.</summary>
    NoDefaultAlpn = 2,

    /// <summary>Identifies an alternative endpoint port.</summary>
    Port = 3,

    /// <summary>Identifies IPv4 address hints.</summary>
    Ipv4Hint = 4,

    /// <summary>Identifies Encrypted ClientHello configuration.</summary>
    Ech = 5,

    /// <summary>Identifies IPv6 address hints.</summary>
    Ipv6Hint = 6,

    /// <summary>Identifies a DNS-over-HTTPS path template.</summary>
    DohPath = 7,

    /// <summary>Identifies Oblivious HTTP support.</summary>
    ObliviousHttp = 8
}
