namespace dns;

/// <summary>Defines well-known DNS presentation names and algorithm mnemonics.</summary>
public static class DnsWellKnownNames
{
    /// <summary>The absolute presentation name of the DNS root.</summary>
    public static readonly string Root = ".";

    /// <summary>The absolute suffix used for IPv4 reverse mapping.</summary>
    public static readonly string Ipv4ReverseSuffix = "in-addr.arpa.";

    /// <summary>The absolute suffix used for IPv6 reverse mapping.</summary>
    public static readonly string Ipv6ReverseSuffix = "ip6.arpa.";

    /// <summary>The DNSSEC deletion-marker mnemonic.</summary>
    public static readonly string DnssecDelete = "DELETE";

    /// <summary>The deprecated RSA with MD5 DNSSEC mnemonic.</summary>
    public static readonly string DnssecRsaMd5 = "RSAMD5";

    /// <summary>The Diffie-Hellman DNS security mnemonic.</summary>
    public static readonly string DnssecDiffieHellman = "DH";

    /// <summary>The DSA with SHA-1 DNSSEC mnemonic.</summary>
    public static readonly string DnssecDsaSha1 = "DSA";

    /// <summary>The RSA with SHA-1 DNSSEC mnemonic.</summary>
    public static readonly string DnssecRsaSha1 = "RSASHA1";

    /// <summary>The DSA NSEC3 with SHA-1 DNSSEC mnemonic.</summary>
    public static readonly string DnssecDsaNsec3Sha1 = "DSA-NSEC3-SHA1";

    /// <summary>The RSA NSEC3 with SHA-1 DNSSEC mnemonic.</summary>
    public static readonly string DnssecRsaNsec3Sha1 = "RSASHA1-NSEC3-SHA1";

    /// <summary>The RSA with SHA-256 DNSSEC mnemonic.</summary>
    public static readonly string DnssecRsaSha256 = "RSASHA256";

    /// <summary>The RSA with SHA-512 DNSSEC mnemonic.</summary>
    public static readonly string DnssecRsaSha512 = "RSASHA512";

    /// <summary>The deprecated GOST R 34.10-2001 DNSSEC mnemonic.</summary>
    public static readonly string DnssecEccGost = "ECC-GOST";

    /// <summary>The ECDSA P-256 with SHA-256 DNSSEC mnemonic.</summary>
    public static readonly string DnssecEcdsaP256Sha256 = "ECDSAP256SHA256";

    /// <summary>The ECDSA P-384 with SHA-384 DNSSEC mnemonic.</summary>
    public static readonly string DnssecEcdsaP384Sha384 = "ECDSAP384SHA384";

    /// <summary>The Ed25519 DNSSEC mnemonic.</summary>
    public static readonly string DnssecEd25519 = "ED25519";

    /// <summary>The Ed448 DNSSEC mnemonic.</summary>
    public static readonly string DnssecEd448 = "ED448";

    /// <summary>The SM2 with SM3 DNSSEC mnemonic.</summary>
    public static readonly string DnssecSm2Sm3 = "SM2SM3";

    /// <summary>The ML-DSA-44 DNSSEC mnemonic.</summary>
    public static readonly string DnssecMlDsa44 = "MLDSA44";

    /// <summary>The GOST R 34.10-2012 DNSSEC mnemonic.</summary>
    public static readonly string DnssecEccGost12 = "ECC-GOST12";

    /// <summary>The indirect-key DNS security mnemonic.</summary>
    public static readonly string DnssecIndirect = "INDIRECT";

    /// <summary>The private-domain-name DNS security mnemonic.</summary>
    public static readonly string DnssecPrivateDns = "PRIVATEDNS";

    /// <summary>The private-object-identifier DNS security mnemonic.</summary>
    public static readonly string DnssecPrivateOid = "PRIVATEOID";

    /// <summary>The legacy HMAC-MD5 TSIG algorithm name.</summary>
    public static readonly string TsigHmacMd5 = "hmac-md5.sig-alg.reg.int.";

    /// <summary>The GSS-TSIG algorithm name.</summary>
    public static readonly string TsigGss = "gss-tsig.";

    /// <summary>The HMAC-SHA-1 TSIG algorithm name.</summary>
    public static readonly string TsigHmacSha1 = "hmac-sha1.";

    /// <summary>The HMAC-SHA-224 TSIG algorithm name.</summary>
    public static readonly string TsigHmacSha224 = "hmac-sha224.";

    /// <summary>The HMAC-SHA-256 TSIG algorithm name.</summary>
    public static readonly string TsigHmacSha256 = "hmac-sha256.";

    /// <summary>The 128-bit truncated HMAC-SHA-256 TSIG algorithm name.</summary>
    public static readonly string TsigHmacSha256Truncated128 = "hmac-sha256-128.";

    /// <summary>The HMAC-SHA-384 TSIG algorithm name.</summary>
    public static readonly string TsigHmacSha384 = "hmac-sha384.";

    /// <summary>The 192-bit truncated HMAC-SHA-384 TSIG algorithm name.</summary>
    public static readonly string TsigHmacSha384Truncated192 = "hmac-sha384-192.";

    /// <summary>The HMAC-SHA-512 TSIG algorithm name.</summary>
    public static readonly string TsigHmacSha512 = "hmac-sha512.";

    /// <summary>The 256-bit truncated HMAC-SHA-512 TSIG algorithm name.</summary>
    public static readonly string TsigHmacSha512Truncated256 = "hmac-sha512-256.";
}
