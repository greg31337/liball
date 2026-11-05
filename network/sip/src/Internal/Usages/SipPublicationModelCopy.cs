namespace sip;

internal static class SipPublicationModelCopy
{
    internal static SipPublication CopyPublication(SipPublication source)
    {
        SipPublication copy = new SipPublication();
        copy.PublicationId = source.PublicationId;
        copy.Target = source.Target == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Target);
        copy.Sender = source.Sender == null ? null
            : SipUriModelCopy.CreateNormalizedCopy(source.Sender);
        copy.EventPackage = source.EventPackage;
        copy.EntityTag = source.EntityTag;
        copy.Body = source.Body == null ? new byte[0] : (byte[])source.Body.Clone();
        copy.ContentType = SipReferCore.CopyMediaType(source.ContentType);
        copy.ExpiresAtMilliseconds = source.ExpiresAtMilliseconds;
        copy.UpdatedAtMilliseconds = source.UpdatedAtMilliseconds;
        copy.CorrelationId = source.CorrelationId;
        return copy;
    }

    internal static SipPublicationServerOptions CopyServerOptions(
        SipPublicationServerOptions? source)
    {
        SipPublicationServerOptions copy = new SipPublicationServerOptions();
        if (source != null)
        {
            copy.DefaultExpiresSeconds = source.DefaultExpiresSeconds;
            copy.MinExpiresSeconds = source.MinExpiresSeconds;
            copy.MaxExpiresSeconds = source.MaxExpiresSeconds;
            copy.MaxBodyBytes = source.MaxBodyBytes;
            copy.MaxPublications = source.MaxPublications;
            copy.MaxEventCount = source.MaxEventCount;
            copy.RequireAuthorization = source.RequireAuthorization;
            copy.ValidateEventBodies = source.ValidateEventBodies;
        }
        return copy;
    }

    internal static SipAuthorizationDecision? CopyAuthorization(
        SipAuthorizationDecision? source)
    {
        if (source == null) return null;
        SipAuthorizationDecision copy = new SipAuthorizationDecision();
        copy.Allowed = source.Allowed;
        copy.Identity = source.Identity;
        copy.Reason = source.Reason;
        copy.Scopes = source.Scopes == null
            ? new string[0] : (string[])source.Scopes.Clone();
        copy.StatusCode = source.StatusCode;
        return copy;
    }
}
