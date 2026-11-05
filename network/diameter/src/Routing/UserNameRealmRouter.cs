using System;
using System.Collections.Generic;
using System.Text;

namespace diameter;

internal class UserNameRealmRoutingResult
{
    internal bool Accepted = true;
    internal bool Derived;
    internal bool Advanced;
    internal string Code = "user-name-routing-not-required";
    internal string Message = string.Empty;
}

internal class UserNameReadResult
{
    internal bool Valid = true;
    internal bool Present;
    internal string Value = string.Empty;
    internal string Code = string.Empty;
    internal string Message = string.Empty;
}

internal class UserNameRealmRouter
{
    internal string OriginRealm = string.Empty;
    internal List<string> LocalRealms = new List<string>();
    internal List<string> HomeRealms = new List<string>();
    internal bool EnableUserNameRouting;
    internal bool OriginRealmIsHome;
    internal bool RejectMalformedUserNames;

    internal UserNameRealmRouter(
        DiameterIdentity originRealm,
        DiameterRealmRoutingConfiguration configuration)
    {
        if (originRealm == null || configuration == null)
        {
            throw new ArgumentException(
                "User-Name realm routing requires realm policy.");
        }

        configuration.ValidateRealmRoutingConfiguration(originRealm);
        OriginRealm = ValueCodecs.NormalizeDiameterRealm(originRealm).Text;
        EnableUserNameRouting = configuration.EnableUserNameRouting;
        OriginRealmIsHome = configuration.OriginRealmIsHome;
        RejectMalformedUserNames = configuration.RejectMalformedUserNames;
        int localIndex = 0;
        while (localIndex < configuration.LocalRealms.Count)
        {
            LocalRealms.Add(ValueCodecs.NormalizeDiameterRealm(
                configuration.LocalRealms[localIndex]).Text);
            localIndex = localIndex + 1;
        }

        int homeIndex = 0;
        while (homeIndex < configuration.HomeRealms.Count)
        {
            HomeRealms.Add(ValueCodecs.NormalizeDiameterRealm(
                configuration.HomeRealms[homeIndex]).Text);
            homeIndex = homeIndex + 1;
        }
    }

    internal UserNameRealmRoutingResult ProcessUserNameRealmRoute(
        DiameterRouteRequest request)
    {
        UserNameRealmRoutingResult result =
            new UserNameRealmRoutingResult();
        if (!EnableUserNameRouting)
        {
            result.Code = "user-name-routing-disabled";
            return result;
        }

        UserNameReadResult userName = ReadRoutingUserName(request);
        if (!userName.Valid)
        {
            return HandleMalformedUserName(
                userName.Code,
                userName.Message);
        }

        if (!userName.Present)
        {
            return result;
        }

        request.UserName = userName.Value;
        int firstAt = userName.Value.IndexOf('@');
        int lastAt = userName.Value.LastIndexOf('@');
        int firstBang = userName.Value.IndexOf('!');
        if (request.DestinationRealm == null)
        {
            if (firstAt <= 0 || firstAt != lastAt ||
                firstAt == userName.Value.Length - 1)
            {
                return HandleMalformedUserName(
                    "user-name-realm-invalid",
                    "The User-Name does not contain one valid realm separator.");
            }

            string derivedRealm = userName.Value.Substring(firstAt + 1);
            if (!IsValidUserNameRealm(derivedRealm))
            {
                return HandleMalformedUserName(
                    "user-name-realm-invalid",
                    "The User-Name realm is not a valid Diameter realm.");
            }

            request.DestinationRealm = new DiameterIdentity();
            request.DestinationRealm.Value = derivedRealm;
            if (!WriteRoutingTextAvp(
                request.Message,
                DiameterAvpCodes.DestinationRealm,
                derivedRealm,
                true))
            {
                return HandleMalformedUserName(
                    "destination-realm-avp-invalid",
                    "The request contains conflicting Destination-Realm AVPs.");
            }

            result.Derived = true;
            result.Code = "user-name-realm-derived";
        }

        if (request.DestinationRealm == null ||
            !IsConfiguredLocalRealm(request.DestinationRealm) ||
            firstBang < 0)
        {
            return result;
        }

        if (firstAt <= firstBang || firstAt != lastAt || firstBang == 0 ||
            firstAt == userName.Value.Length - 1 ||
            !IsValidDecoratedUserName(userName.Value, firstAt))
        {
            return HandleMalformedUserName(
                "decorated-user-name-invalid",
                "The decorated User-Name contains an invalid realm path.");
        }

        string currentRealm = userName.Value.Substring(firstAt + 1);
        if (!IsValidUserNameRealm(currentRealm) ||
            !AreUserNameRealmsEqual(
                currentRealm,
                request.DestinationRealm.Value))
        {
            return HandleMalformedUserName(
                "user-name-destination-realm-mismatch",
                "The User-Name realm does not match Destination-Realm.");
        }

        string nextRealm = userName.Value.Substring(0, firstBang);
        string remainingUserName = userName.Value.Substring(
            firstBang + 1,
            firstAt - firstBang - 1);
        string updatedUserName = remainingUserName + "@" + nextRealm;
        request.UserName = updatedUserName;
        request.DestinationRealm.Value = nextRealm;
        if (!WriteRoutingTextAvp(
            request.Message,
            DiameterAvpCodes.UserName,
            updatedUserName,
            true) ||
            !WriteRoutingTextAvp(
                request.Message,
                DiameterAvpCodes.DestinationRealm,
                nextRealm,
                true))
        {
            return HandleMalformedUserName(
                "routing-avp-conflict",
                "The request contains conflicting realm-routing AVPs.");
        }

        result.Advanced = true;
        result.Code = "decorated-user-name-advanced";
        result.Message =
            "The decorated User-Name advanced to its next configured realm.";
        return result;
    }

    internal UserNameReadResult ReadRoutingUserName(
        DiameterRouteRequest request)
    {
        UserNameReadResult result = new UserNameReadResult();
        string avpValue = string.Empty;
        bool avpPresent = false;
        int index = 0;
        while (index < request.Message.Avps.Count)
        {
            DiameterAvp avp = request.Message.Avps[index];
            if (avp.Code == DiameterAvpCodes.UserName && avp.VendorId == 0U)
            {
                if (avpPresent)
                {
                    result.Valid = false;
                    result.Code = "user-name-avp-duplicate";
                    result.Message =
                        "The request contains more than one User-Name AVP.";
                    return result;
                }

                DiameterValueResult decoded =
                    ValueCodecs.DecodeUtf8String(avp.Data);
                if (decoded.Status != DiameterCodecStatus.Success ||
                    decoded.Text.Length == 0)
                {
                    result.Valid = false;
                    result.Code = "user-name-avp-invalid";
                    result.Message =
                        "The request User-Name AVP is not valid UTF-8 text.";
                    return result;
                }

                avpValue = decoded.Text;
                avpPresent = true;
            }

            index = index + 1;
        }

        bool contextPresent = request.UserName.Length > 0;
        if (contextPresent && avpPresent && request.UserName != avpValue)
        {
            result.Valid = false;
            result.Code = "user-name-context-mismatch";
            result.Message =
                "The routing User-Name does not match the request AVP.";
            return result;
        }

        result.Present = contextPresent || avpPresent;
        result.Value = contextPresent ? request.UserName : avpValue;
        return result;
    }

    internal bool IsValidDecoratedUserName(string userName, int atIndex)
    {
        int segmentStart = 0;
        int bangIndex = userName.IndexOf('!');
        while (bangIndex >= 0 && bangIndex < atIndex)
        {
            if (bangIndex == segmentStart)
            {
                return false;
            }

            string realm = userName.Substring(
                segmentStart,
                bangIndex - segmentStart);
            if (!IsValidUserNameRealm(realm))
            {
                return false;
            }

            segmentStart = bangIndex + 1;
            bangIndex = userName.IndexOf('!', segmentStart);
        }

        return segmentStart < atIndex;
    }

    internal bool IsValidUserNameRealm(string value)
    {
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = value;
        return ValueCodecs.NormalizeDiameterRealm(realm).Status ==
            DiameterCodecStatus.Success;
    }

    internal bool IsConfiguredLocalRealm(DiameterIdentity realm)
    {
        string normalized = ValueCodecs.NormalizeDiameterRealm(realm).Text;
        if (normalized == OriginRealm || ContainsConfiguredRealm(HomeRealms, normalized))
        {
            return true;
        }

        return ContainsConfiguredRealm(LocalRealms, normalized);
    }

    internal bool IsConfiguredHomeRealm(DiameterIdentity realm)
    {
        string normalized = ValueCodecs.NormalizeDiameterRealm(realm).Text;
        if (OriginRealmIsHome && normalized == OriginRealm)
        {
            return true;
        }

        return ContainsConfiguredRealm(HomeRealms, normalized);
    }

    internal bool ContainsConfiguredRealm(
        List<string> realms,
        string realm)
    {
        int index = 0;
        while (index < realms.Count)
        {
            if (realms[index] == realm)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool AreUserNameRealmsEqual(string first, string second)
    {
        DiameterIdentity firstRealm = new DiameterIdentity();
        firstRealm.Value = first;
        DiameterIdentity secondRealm = new DiameterIdentity();
        secondRealm.Value = second;
        return ValueCodecs.NormalizeDiameterRealm(firstRealm).Text ==
            ValueCodecs.NormalizeDiameterRealm(secondRealm).Text;
    }

    internal bool WriteRoutingTextAvp(
        DiameterMessage message,
        uint code,
        string value,
        bool addWhenMissing)
    {
        DiameterAvp? found = null;
        int index = 0;
        while (index < message.Avps.Count)
        {
            DiameterAvp avp = message.Avps[index];
            if (avp.Code == code && avp.VendorId == 0U)
            {
                if (found != null)
                {
                    return false;
                }

                found = avp;
            }

            index = index + 1;
        }

        byte[] data = Encoding.UTF8.GetBytes(value);
        if (found != null)
        {
            found.Data = data;
            return true;
        }

        if (addWhenMissing)
        {
            DiameterAvp avp = new DiameterAvp();
            avp.Code = code;
            avp.Flags = DiameterAvpFlags.Mandatory;
            avp.Data = data;
            message.Avps.Add(avp);
        }

        return true;
    }

    internal UserNameRealmRoutingResult HandleMalformedUserName(
        string code,
        string message)
    {
        if (!RejectMalformedUserNames)
        {
            UserNameRealmRoutingResult ignored =
                new UserNameRealmRoutingResult();
            ignored.Code = "malformed-user-name-ignored";
            ignored.Message = message;
            return ignored;
        }

        return CreateUserNameRoutingFailure(code, message);
    }

    internal UserNameRealmRoutingResult CreateUserNameRoutingFailure(
        string code,
        string message)
    {
        UserNameRealmRoutingResult result =
            new UserNameRealmRoutingResult();
        result.Accepted = false;
        result.Code = code;
        result.Message = message;
        return result;
    }
}
