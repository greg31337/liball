using System.Collections.Generic;
using System.Net;

namespace diameter;

/// <summary>
/// Supplies node, command, and answer values used to construct Diameter base commands.
/// </summary>
public class DiameterBaseCommandOptions
{
    /// <summary>The identity of the node creating the command.</summary>
    public DiameterIdentity OriginHost = new DiameterIdentity();

    /// <summary>The realm of the node creating the command.</summary>
    public DiameterIdentity OriginRealm = new DiameterIdentity();

    /// <summary>The local addresses advertised by a capabilities command.</summary>
    public List<IPAddress> HostAddresses = new List<IPAddress>();

    /// <summary>The local implementation Vendor-Id advertised by a capabilities command.</summary>
    public uint VendorId;

    /// <summary>The local product name advertised by a capabilities command.</summary>
    public string ProductName = string.Empty;

    /// <summary>The local firmware revision advertised by a capabilities command.</summary>
    public uint FirmwareRevision;

    /// <summary>The supported vendor identifiers advertised by a capabilities command.</summary>
    public List<uint> SupportedVendorIds = new List<uint>();

    /// <summary>The authentication applications advertised by a capabilities command.</summary>
    public List<uint> AuthApplicationIds = new List<uint>();

    /// <summary>The accounting applications advertised by a capabilities command.</summary>
    public List<uint> AcctApplicationIds = new List<uint>();

    /// <summary>The vendor-specific applications advertised by a capabilities command.</summary>
    public List<DiameterApplicationId> VendorSpecificApplications =
        new List<DiameterApplicationId>();

    /// <summary>The in-band security mechanisms advertised by a capabilities command.</summary>
    public List<uint> InbandSecurityIds = new List<uint>();

    /// <summary>The destination host for a routed session command.</summary>
    public DiameterIdentity? DestinationHost;

    /// <summary>The destination realm for a routed session or accounting command.</summary>
    public DiameterIdentity? DestinationRealm;

    /// <summary>The Session-Id for a session or accounting command.</summary>
    public string SessionId = string.Empty;

    /// <summary>The authentication application identifier carried by a session command.</summary>
    public uint AuthApplicationId;

    /// <summary>The application identifier used in a Re-Auth command header, or zero for base Re-Auth.</summary>
    public uint CommandApplicationId;

    /// <summary>The cause carried by a Disconnect-Peer request.</summary>
    public DiameterDisconnectCause DisconnectCause;

    /// <summary>The operation requested by a Re-Auth request.</summary>
    public DiameterReAuthRequestType ReAuthRequestType;

    /// <summary>The credit-control sub-session selected by a Re-Auth command.</summary>
    public ulong CreditControlSubSessionId;

    /// <summary>Indicates that a credit-control sub-session selector is present.</summary>
    public bool CreditControlSubSessionIdPresent;

    /// <summary>The granted-unit pool selected by a Re-Auth command.</summary>
    public uint GsuPoolIdentifier;

    /// <summary>Indicates that a granted-unit pool selector is present.</summary>
    public bool GsuPoolIdentifierPresent;

    /// <summary>The service selected by a Re-Auth command.</summary>
    public uint ServiceIdentifier;

    /// <summary>Indicates that a service selector is present.</summary>
    public bool ServiceIdentifierPresent;

    /// <summary>The rating group selected by a Re-Auth command.</summary>
    public uint RatingGroup;

    /// <summary>Indicates that a rating-group selector is present.</summary>
    public bool RatingGroupPresent;

    /// <summary>The cause carried by a Session-Termination request.</summary>
    public DiameterTerminationCause TerminationCause = DiameterTerminationCause.Logout;

    /// <summary>The record type carried by an Accounting request.</summary>
    public DiameterAccountingRecordType AccountingRecordType = DiameterAccountingRecordType.Event;

    /// <summary>The sequence number carried by an Accounting request.</summary>
    public uint AccountingRecordNumber;

    /// <summary>The request for which an answer is being constructed.</summary>
    public DiameterMessage? Request;

    /// <summary>The Result-Code carried by an answer.</summary>
    public uint ResultCode = DiameterResultCodes.Success;
}
