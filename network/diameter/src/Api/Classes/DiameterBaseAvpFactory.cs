using System;
using System.Collections.Generic;
using System.Text;

namespace diameter;

/// <summary>
/// Constructs reusable RFC 6733 base-protocol AVPs with owned payload data.
/// </summary>
public class DiameterBaseAvpFactory : IDiameterBaseAvpFactory
{
    /// <summary>Initializes a base AVP factory.</summary>
    public DiameterBaseAvpFactory()
    {
    }

    /// <summary>Creates an Origin-Host AVP.</summary>
    /// <param name="identity">The originating node identity.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateOriginHostAvp(DiameterIdentity? identity)
    {
        return CreateIdentityAvp(DiameterAvpCodes.OriginHost, identity);
    }

    /// <summary>Creates an Origin-Realm AVP.</summary>
    /// <param name="realm">The originating node realm.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateOriginRealmAvp(DiameterIdentity? realm)
    {
        return CreateIdentityAvp(DiameterAvpCodes.OriginRealm, realm);
    }

    /// <summary>Creates an Origin-State-Id AVP.</summary>
    /// <param name="identifier">The originating node state identifier.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateOriginStateIdAvp(uint identifier)
    {
        return CreateUnsigned32Avp(DiameterAvpCodes.OriginStateId, identifier);
    }

    /// <summary>Creates a Destination-Host AVP.</summary>
    /// <param name="identity">The destination node identity.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateDestinationHostAvp(DiameterIdentity? identity)
    {
        return CreateIdentityAvp(DiameterAvpCodes.DestinationHost, identity);
    }

    /// <summary>Creates a Destination-Realm AVP.</summary>
    /// <param name="realm">The destination realm.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateDestinationRealmAvp(DiameterIdentity? realm)
    {
        return CreateIdentityAvp(DiameterAvpCodes.DestinationRealm, realm);
    }

    /// <summary>Creates an Auth-Application-Id AVP.</summary>
    /// <param name="applicationId">The authentication application identifier.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateAuthApplicationIdAvp(uint applicationId)
    {
        return CreateUnsigned32Avp(DiameterAvpCodes.AuthApplicationId, applicationId);
    }

    /// <summary>Creates an Acct-Application-Id AVP.</summary>
    /// <param name="applicationId">The accounting application identifier.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateAcctApplicationIdAvp(uint applicationId)
    {
        return CreateUnsigned32Avp(DiameterAvpCodes.AcctApplicationId, applicationId);
    }

    /// <summary>Creates a Vendor-Specific-Application-Id grouped AVP.</summary>
    /// <param name="application">The vendor and application identifier.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateVendorSpecificApplicationIdAvp(DiameterApplicationId? application)
    {
        if (application == null || application.VendorId == 0U)
        {
            throw new ArgumentException(
                "A vendor-specific application requires a nonzero Vendor-Id.");
        }

        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateUnsigned32Avp(DiameterAvpCodes.VendorId, application.VendorId));
        if (application.IsAccounting)
        {
            children.Add(CreateAcctApplicationIdAvp(application.Id));
        }
        else
        {
            children.Add(CreateAuthApplicationIdAvp(application.Id));
        }

        return CreateGroupedAvp(DiameterAvpCodes.VendorSpecificApplicationId, children);
    }

    /// <summary>Creates a Session-Id AVP.</summary>
    /// <param name="sessionId">The globally unique session identifier.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateSessionIdAvp(string? sessionId)
    {
        return CreateTextAvp(
            DiameterAvpCodes.SessionId,
            ValidateText(sessionId, "A valid Session-Id is required."));
    }

    /// <summary>Creates a Route-Record AVP.</summary>
    /// <param name="identity">The traversed node identity.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateRouteRecordAvp(DiameterIdentity? identity)
    {
        return CreateIdentityAvp(DiameterAvpCodes.RouteRecord, identity);
    }

    /// <summary>Creates a Proxy-Info grouped AVP.</summary>
    /// <param name="information">The proxy identity, state, and extensions.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateProxyInfoAvp(DiameterProxyInformation? information)
    {
        if (information == null || information.ProxyState == null ||
            information.ExtensionAvps == null)
        {
            throw new ArgumentException("Complete Proxy-Info values are required.");
        }

        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateIdentityAvp(DiameterAvpCodes.ProxyHost, information.ProxyHost));
        children.Add(CreateOctetStringAvp(DiameterAvpCodes.ProxyState, information.ProxyState));
        int index = 0;
        while (index < information.ExtensionAvps.Count)
        {
            DiameterAvp extension = information.ExtensionAvps[index];
            if (extension == null ||
                (extension.VendorId == DiameterVendorIds.Ietf &&
                (extension.Code == DiameterAvpCodes.ProxyHost ||
                extension.Code == DiameterAvpCodes.ProxyState)))
            {
                throw new ArgumentException(
                    "Proxy-Info extensions cannot replace required proxy values.");
            }

            children.Add(CloneAvp(extension));
            index = index + 1;
        }

        return CreateGroupedAvp(DiameterAvpCodes.ProxyInfo, children);
    }

    /// <summary>Creates a Redirect-Host AVP.</summary>
    /// <param name="uri">The redirect target URI.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateRedirectHostAvp(DiameterUri? uri)
    {
        DiameterValueResult normalized = ValueCodecs.NormalizeDiameterUri(uri);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid Diameter redirect URI is required.");
        }

        return CreateTextAvp(DiameterAvpCodes.RedirectHost, normalized.Text);
    }

    /// <summary>Creates a Redirect-Host-Usage AVP.</summary>
    /// <param name="usage">The scope of the redirect route.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateRedirectHostUsageAvp(DiameterRedirectHostUsage usage)
    {
        int value = (int)usage;
        if (value < 0 || value > 6)
        {
            throw new ArgumentException("A valid Redirect-Host-Usage is required.");
        }

        return CreateUnsigned32Avp(DiameterAvpCodes.RedirectHostUsage, (uint)value);
    }

    /// <summary>Creates a Redirect-Max-Cache-Time AVP.</summary>
    /// <param name="seconds">The maximum cache lifetime in seconds.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateRedirectMaxCacheTimeAvp(uint seconds)
    {
        return CreateUnsigned32Avp(DiameterAvpCodes.RedirectMaxCacheTime, seconds);
    }

    /// <summary>Creates an Experimental-Result grouped AVP.</summary>
    /// <param name="result">The vendor and vendor-assigned result code.</param>
    /// <returns>The constructed AVP.</returns>
    public DiameterAvp CreateExperimentalResultAvp(DiameterExperimentalResult? result)
    {
        if (result == null || result.VendorId == 0U)
        {
            throw new ArgumentException("An Experimental-Result requires a nonzero Vendor-Id.");
        }

        List<DiameterAvp> children = new List<DiameterAvp>();
        children.Add(CreateUnsigned32Avp(DiameterAvpCodes.VendorId, result.VendorId));
        children.Add(CreateUnsigned32Avp(
            DiameterAvpCodes.ExperimentalResultCode,
            result.ResultCode));
        return CreateGroupedAvp(DiameterAvpCodes.ExperimentalResult, children);
    }

    internal DiameterAvp CreateIdentityAvp(uint code, DiameterIdentity? identity)
    {
        DiameterValueResult normalized = ValueCodecs.NormalizeDiameterIdentity(identity);
        if (normalized.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException("A valid DiameterIdentity is required.");
        }

        return CreateTextAvp(code, normalized.Text);
    }

    internal string ValidateText(string? value, string failureMessage)
    {
        if (value == null || value.Length == 0 || ValueCodecs.FindInvalidUtf16Index(value) >= 0)
        {
            throw new ArgumentException(failureMessage);
        }

        return value;
    }

    internal DiameterAvp CreateTextAvp(uint code, string value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = Encoding.UTF8.GetBytes(value);
        return avp;
    }

    internal DiameterAvp CreateUnsigned32Avp(uint code, uint value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(avp.Data, value);
        return avp;
    }

    internal DiameterAvp CreateOctetStringAvp(uint code, byte[] value)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = new byte[value.Length];
        Array.Copy(value, avp.Data, value.Length);
        return avp;
    }

    internal DiameterAvp CreateGroupedAvp(uint code, List<DiameterAvp> children)
    {
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        DiameterValueResult measurement = GroupedValueCodec.MeasureGroupedValue(
            children,
            null,
            limits);
        if (measurement.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException("The Grouped AVP value is invalid or exceeds limits.");
        }

        byte[] data = new byte[measurement.RequiredCount];
        DiameterValueResult encoding = GroupedValueCodec.EncodeGroupedValue(
            data,
            children,
            null,
            limits);
        if (encoding.Status != DiameterCodecStatus.Success)
        {
            throw new ArgumentException("The Grouped AVP value could not be encoded.");
        }

        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = data;
        return avp;
    }

    internal DiameterAvp CloneAvp(DiameterAvp source)
    {
        if (source.Data == null)
        {
            throw new ArgumentException("A Proxy-Info extension requires owned AVP data.");
        }

        DiameterAvp clone = new DiameterAvp();
        clone.Code = source.Code;
        clone.Flags = source.Flags;
        clone.VendorId = source.VendorId;
        clone.Data = new byte[source.Data.Length];
        Array.Copy(source.Data, clone.Data, source.Data.Length);
        return clone;
    }
}
