using System;
using System.Collections.Generic;
using System.Net;

namespace diameter;

/// <summary>Contains a request to discover Diameter endpoints for a realm and application.</summary>
public class DiameterDiscoveryRequest
{
    /// <summary>The realm for which peers are requested.</summary>
    public DiameterIdentity Realm = new DiameterIdentity();

    /// <summary>The required authentication or accounting application.</summary>
    public DiameterApplicationId Application = new DiameterApplicationId();

    /// <summary>The preferred transport kind.</summary>
    public DiameterTransportKind Transport;

    /// <summary>The required transport security mode.</summary>
    public DiameterSecurityMode Security;

    /// <summary>Allows explicitly configured peers to satisfy the request.</summary>
    public bool AllowStaticPeers = true;

    /// <summary>Allows dynamic DNS discovery to satisfy the request.</summary>
    public bool AllowDnsDiscovery = true;

    /// <summary>Allows a valid cached discovery result to be reused.</summary>
    public bool AllowCachedResults = true;

    /// <summary>The maximum number of endpoints to return.</summary>
    public int MaximumResults = 32;

    internal void ValidateDiscoveryRequest()
    {
        if (Realm == null || Application == null ||
            ValueCodecs.NormalizeDiameterRealm(Realm).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid discovery realm and application are required.");
        }

        if (Transport != DiameterTransportKind.Tcp &&
            Transport != DiameterTransportKind.Sctp &&
            Transport != DiameterTransportKind.External)
        {
            throw new ArgumentOutOfRangeException("Transport");
        }

        if (Security != DiameterSecurityMode.None &&
            Security != DiameterSecurityMode.Tls &&
            Security != DiameterSecurityMode.Dtls &&
            Security != DiameterSecurityMode.Ipsec &&
            Security != DiameterSecurityMode.External)
        {
            throw new ArgumentOutOfRangeException("Security");
        }

        if ((Security == DiameterSecurityMode.Tls &&
            Transport != DiameterTransportKind.Tcp) ||
            (Security == DiameterSecurityMode.Dtls &&
            Transport != DiameterTransportKind.Sctp) ||
            (Security == DiameterSecurityMode.External &&
            Transport != DiameterTransportKind.External))
        {
            throw new ArgumentException("Discovery transport and security are incompatible.");
        }

        if (!AllowStaticPeers && !AllowDnsDiscovery)
        {
            throw new ArgumentException("At least one discovery source is required.");
        }

        if (MaximumResults <= 0 || MaximumResults > 1024)
        {
            throw new ArgumentOutOfRangeException("MaximumResults");
        }
    }
}

/// <summary>Contains one DNS Naming Authority Pointer record used for Diameter discovery.</summary>
public class DiameterNaptrRecord
{
    /// <summary>The DNS owner name.</summary>
    public string Name = string.Empty;

    /// <summary>The NAPTR order value.</summary>
    public int Order;

    /// <summary>The NAPTR preference value.</summary>
    public int Preference;

    /// <summary>The NAPTR flags text.</summary>
    public string Flags = string.Empty;

    /// <summary>The S-NAPTR service text.</summary>
    public string Service = string.Empty;

    /// <summary>The NAPTR regular-expression replacement text.</summary>
    public string RegularExpression = string.Empty;

    /// <summary>The replacement DNS name.</summary>
    public string Replacement = string.Empty;

    /// <summary>The DNS time-to-live value in seconds.</summary>
    public uint TimeToLiveSeconds;

    /// <summary>The absolute time at which the record expires.</summary>
    public DateTimeOffset ExpiresAt;
}

/// <summary>Contains one DNS Service Locator record used for Diameter discovery.</summary>
public class DiameterSrvRecord
{
    /// <summary>The DNS owner name.</summary>
    public string Name = string.Empty;

    /// <summary>The SRV priority value.</summary>
    public int Priority;

    /// <summary>The SRV selection weight.</summary>
    public int Weight;

    /// <summary>The service port.</summary>
    public int Port;

    /// <summary>The target DNS host name.</summary>
    public string Target = string.Empty;

    /// <summary>The DNS time-to-live value in seconds.</summary>
    public uint TimeToLiveSeconds;

    /// <summary>The absolute time at which the record expires.</summary>
    public DateTimeOffset ExpiresAt;
}

/// <summary>Contains one DNS address record used for Diameter discovery.</summary>
public class DiameterAddressRecord
{
    /// <summary>The DNS owner name.</summary>
    public string Name = string.Empty;

    /// <summary>The resolved IPv4 or IPv6 address, or <c>null</c> when absent.</summary>
    public IPAddress? Address;

    /// <summary>The DNS time-to-live value in seconds.</summary>
    public uint TimeToLiveSeconds;

    /// <summary>The absolute time at which the record expires.</summary>
    public DateTimeOffset ExpiresAt;
}

/// <summary>Contains resolved endpoints and source records from one discovery operation.</summary>
public class DiameterDiscoveryResult
{
    /// <summary>The discovery request represented by the result.</summary>
    public DiameterDiscoveryRequest Request = new DiameterDiscoveryRequest();

    /// <summary>The configured or admitted peers represented by the result.</summary>
    public List<DiameterPeerConfiguration> Peers =
        new List<DiameterPeerConfiguration>();

    /// <summary>The usable endpoints in selection order.</summary>
    public List<DiameterEndpoint> Endpoints = new List<DiameterEndpoint>();

    /// <summary>The NAPTR records considered by discovery.</summary>
    public List<DiameterNaptrRecord> NaptrRecords = new List<DiameterNaptrRecord>();

    /// <summary>The SRV records considered by discovery.</summary>
    public List<DiameterSrvRecord> SrvRecords = new List<DiameterSrvRecord>();

    /// <summary>The A and AAAA records considered by discovery.</summary>
    public List<DiameterAddressRecord> AddressRecords =
        new List<DiameterAddressRecord>();

    /// <summary>Indicates that at least one usable endpoint was resolved.</summary>
    public bool Resolved;

    /// <summary>Indicates that the result came from a cache.</summary>
    public bool FromCache;

    /// <summary>Indicates that this is a cacheable negative result.</summary>
    public bool Negative;

    /// <summary>The absolute expiry of the result.</summary>
    public DateTimeOffset ExpiresAt;

    /// <summary>The stable machine-readable discovery outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable discovery outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>Contains the records and DNS metadata returned by one DNS query.</summary>
public class DiameterDnsQueryResult
{
    /// <summary>The normalized DNS name that was queried.</summary>
    public string QueryName = string.Empty;

    /// <summary>The DNS resource-record type that was queried.</summary>
    public int RecordType;

    /// <summary>The DNS response code.</summary>
    public int ResponseCode;

    /// <summary>Indicates that the DNS response code represents success.</summary>
    public bool Successful;

    /// <summary>Indicates that the responding server was authoritative.</summary>
    public bool Authoritative;

    /// <summary>Indicates that recursive resolution is available from the server.</summary>
    public bool RecursionAvailable;

    /// <summary>Indicates that a truncated UDP response was retried over TCP.</summary>
    public bool UsedTcpFallback;

    /// <summary>The authoritative negative-cache lifetime in seconds, or zero when absent.</summary>
    public uint NegativeTimeToLiveSeconds;

    /// <summary>The decoded NAPTR records.</summary>
    public List<DiameterNaptrRecord> NaptrRecords = new List<DiameterNaptrRecord>();

    /// <summary>The decoded SRV records.</summary>
    public List<DiameterSrvRecord> SrvRecords = new List<DiameterSrvRecord>();

    /// <summary>The decoded IPv4 or IPv6 address records.</summary>
    public List<DiameterAddressRecord> AddressRecords =
        new List<DiameterAddressRecord>();

    /// <summary>The stable machine-readable query outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable query outcome description.</summary>
    public string Message = string.Empty;
}
