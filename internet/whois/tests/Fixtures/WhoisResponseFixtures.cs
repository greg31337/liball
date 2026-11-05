namespace whois.Tests;

internal static class WhoisResponseFixtures
{
    internal const string Iana =
        "% IANA WHOIS server\r\n" +
        "% for more information on IANA, visit http://www.iana.org\r\n" +
        "domain:       COM\r\n" +
        "organisation: VeriSign Global Registry Services\r\n" +
        "address:      12061 Bluemont Way\r\n" +
        "address:      Reston Virginia 20190\r\n" +
        "refer:        whois.verisign-grs.com\r\n" +
        "nserver:      A.GTLD-SERVERS.NET 192.5.6.30 2001:503:a83e::2:30\r\n" +
        "status:       ACTIVE\r\n" +
        "created:      1985-01-01\r\n" +
        "changed:      2025-01-01\r\n" +
        "source:       IANA\r\n";

    internal const string Gtld =
        "Domain Name: EXAMPLE.COM\r\n" +
        "Registry Domain ID: 2336799_DOMAIN_COM-VRSN\r\n" +
        "Registrar WHOIS Server: whois.example-registrar.test\r\n" +
        "Registrar URL: https://www.example-registrar.test\r\n" +
        "Updated Date: 2025-01-01T00:00:00Z\r\n" +
        "Creation Date: 1995-08-14T04:00:00Z\r\n" +
        "Registry Expiry Date: 2030-08-13T04:00:00Z\r\n" +
        "Domain Status: clientDeleteProhibited https://icann.org/epp#clientDeleteProhibited\r\n" +
        "Domain Status: clientTransferProhibited https://icann.org/epp#clientTransferProhibited\r\n" +
        "Name Server: A.IANA-SERVERS.NET\r\n" +
        "Name Server: B.IANA-SERVERS.NET\r\n" +
        "DNSSEC: signedDelegation\r\n" +
        ">>> Last update of whois database: 2025-01-01T00:00:00Z <<<\r\n";

    internal const string Arin =
        "# ARIN WHOIS data and services are subject to the Terms of Use\r\n" +
        "NetRange:       192.0.2.0 - 192.0.2.255\r\n" +
        "CIDR:           192.0.2.0/24\r\n" +
        "NetName:        TEST-NET-1\r\n" +
        "NetHandle:      NET-192-0-2-0-1\r\n" +
        "Parent:         NET192 (NET-192-0-0-0-0)\r\n" +
        "NetType:        IANA Special Use\r\n" +
        "OriginAS:       AS64496\r\n" +
        "Organization:   Example Network Documentation\r\n" +
        "ReferralServer: whois://whois.example.test:43\r\n" +
        "RegDate:        2009-08-31\r\n" +
        "Updated:        2025-01-01\r\n";

    internal const string Ripe =
        "% This is the RIPE Database query service.\n" +
        "% The objects are in RPSL format.\n" +
        "inetnum:        192.0.2.0 - 192.0.2.255\n" +
        "netname:        TEST-NET-RIPE\n" +
        "descr:          Documentation network\n" +
        "country:        ZZ\n" +
        "admin-c:        TEST1-RIPE\n" +
        "tech-c:         TEST1-RIPE\n" +
        "status:         ASSIGNED PA\n" +
        "mnt-by:         TEST-MNT\n" +
        "created:        2020-01-01T00:00:00Z\n" +
        "last-modified:  2025-01-01T00:00:00Z\n" +
        "source:         RIPE\n";

    internal const string Apnic =
        "% Information related to '2001:db8::/32'\r" +
        "inet6num:       2001:db8::/32\r" +
        "netname:        DOCUMENTATION-V6\r" +
        "descr:          IPv6 documentation prefix\r" +
        "country:        ZZ\r" +
        "admin-c:        TEST1-AP\r" +
        "tech-c:         TEST1-AP\r" +
        "status:         ALLOCATED PORTABLE\r" +
        "mnt-by:         APNIC-HM\r" +
        "last-modified:  2025-01-01T00:00:00Z\r" +
        "source:         APNIC\r";

    internal const string ProseAndDisclaimer =
        "This response is provided for information purposes only.\r\n" +
        "Use of this service is subject to acceptable-use restrictions.\r\n" +
        "Automated high-volume access is prohibited.\r\n" +
        "\r\n" +
        "% End of disclaimer\r\n";

    internal const string RepeatedFields =
        "Domain Status: active\r\n" +
        "Domain Status: clientTransferProhibited\r\n" +
        "Domain Status: clientUpdateProhibited\r\n" +
        "Name Server: NS1.EXAMPLE.TEST\r\n" +
        "Name Server: NS2.EXAMPLE.TEST\r\n";

    internal const string Continuations =
        "remarks: first line with trailing space \r\n" +
        "  second line of the same value\r\n" +
        "\tthird line with a tab\r\n" +
        "contact: Example Registry\r\n";

    internal const string Malformed =
        "colonless text\r\n" +
        ": missing field name\n" +
        "Field:: double separator\r" +
        "  orphaned continuation\r\n" +
        "Nul: embedded\0value\r\n" +
        "Final: unterminated";

    internal static readonly byte[] MalformedUtf8 = new byte[]
    {
        68, 111, 109, 97, 105, 110, 58, 32, 195, 40, 13, 10,
        83, 116, 97, 116, 117, 115, 58, 32, 255
    };
}
