namespace whois;

internal static class ReferralValueParser
{
    internal static WhoisReferral ParseValue(string sourceField, int sourceLine, string advertisedValue)
    {
        InputGuard.RequireReference(sourceField, "sourceField");
        InputGuard.RequireReference(advertisedValue, "advertisedValue");

        EndpointParseResult parsedEndpoint = EndpointParser.ParseValue(advertisedValue);
        WhoisReferral referral = new WhoisReferral();
        referral.SourceField = sourceField;
        referral.SourceLine = sourceLine;
        referral.AdvertisedValue = advertisedValue;
        referral.Scheme = parsedEndpoint.Scheme;
        referral.Kind = InferKind(sourceField, parsedEndpoint.Scheme);
        referral.Endpoint = parsedEndpoint.Endpoint;
        referral.PathSuffix = parsedEndpoint.PathSuffix;
        referral.IsValid = parsedEndpoint.IsValid;
        referral.RejectionReason = parsedEndpoint.RejectionReason;
        referral.FollowPriority = GetFollowPriority(referral);
        return referral;
    }

    internal static int GetFollowPriority(WhoisReferral referral)
    {
        WhoisEndpoint? endpoint = referral.Endpoint;
        if (!referral.IsValid || endpoint == null || endpoint.Scheme != WhoisReferralScheme.Whois)
        {
            return -1;
        }
        if (referral.Kind == WhoisReferralKind.Iana)
        {
            return 0;
        }
        if (referral.Kind == WhoisReferralKind.Registrar)
        {
            return 1;
        }
        if (referral.Kind == WhoisReferralKind.Registry || referral.Kind == WhoisReferralKind.Generic)
        {
            return 2;
        }
        return -1;
    }

    internal static WhoisReferralKind InferKind(string sourceField, WhoisReferralScheme scheme)
    {
        if (scheme == WhoisReferralScheme.RWhois)
        {
            return WhoisReferralKind.RWhois;
        }
        if (scheme == WhoisReferralScheme.Http || scheme == WhoisReferralScheme.Https)
        {
            return WhoisReferralKind.Web;
        }

        string normalizedName = NormalizeSourceName(sourceField);
        if (normalizedName == "refer")
        {
            return WhoisReferralKind.Iana;
        }
        if (normalizedName == "whois")
        {
            return WhoisReferralKind.Registry;
        }
        if (normalizedName == "whois server" || normalizedName == "registrar whois server")
        {
            return WhoisReferralKind.Registrar;
        }
        if (normalizedName == "referralserver" || normalizedName == "referral server" || normalizedName == "%referral")
        {
            return WhoisReferralKind.Generic;
        }
        return WhoisReferralKind.Unknown;
    }

    internal static string NormalizeSourceName(string sourceField)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        bool pendingSpace = false;
        int index = 0;
        while (index < sourceField.Length)
        {
            char character = sourceField[index];
            if (character == ' ' || character == '\t')
            {
                if (builder.Length > 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }
                builder.Append(char.ToLowerInvariant(character));
            }
            index = index + 1;
        }
        return builder.ToString();
    }
}
