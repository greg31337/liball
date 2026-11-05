namespace sip;

internal static class SipAuthorizationPolicyCore
{
    internal static SipAuthorizationDecision Authorize(
        SipAuthenticationRequest? request,
        SipAuthenticationResult? authentication,
        ISipAuthorizationPolicy? authorizationPolicy)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }
        if (authentication == null)
        {
            throw new System.ArgumentNullException("authentication");
        }
        if (authorizationPolicy == null)
        {
            throw new System.ArgumentNullException("authorizationPolicy");
        }
        if (request.Scope != SipCredentialScope.Origin
            && request.Scope != SipCredentialScope.Proxy)
        {
            return CreateDenial("An origin or proxy credential scope is required.",
                SipStatusCodes.ServerInternalError);
        }
        if (authentication.Status != SipAuthenticationStatus.Success)
        {
            return CreateDenial("Successful authentication is required.",
                request.Scope == SipCredentialScope.Proxy
                    ? SipStatusCodes.ProxyAuthenticationRequired
                    : SipStatusCodes.Unauthorized);
        }
        if (authentication.Identity == null
            || authentication.Identity.Length == 0)
        {
            return CreateDenial(
                "The successful authentication result has no identity.",
                SipStatusCodes.ServerInternalError);
        }
        if (request.CorrelationId != null
            && authentication.CorrelationId != null
            && request.CorrelationId != authentication.CorrelationId)
        {
            return CreateDenial(
                "The authentication result does not match the operation.",
                SipStatusCodes.Forbidden);
        }

        SipCertificateIdentity? certificate =
            SipCertificateIdentityCopy.Copy(authentication.CertificateIdentity);
        SipBearerToken? token = CopyBearerToken(authentication.BearerToken);
        SipAuthenticationRequest context = CreatePolicyContext(
            request, authentication.Identity, token);
        SipAuthorizationDecision? decision = authorizationPolicy
            .AuthorizeOperation(context,
                SipCertificateIdentityCopy.Copy(certificate),
                CopyBearerToken(token));
        if (decision == null)
        {
            return CreateDenial("The authorization policy returned no decision.",
                SipStatusCodes.ServerInternalError);
        }

        SipAuthorizationDecision result = CopyDecision(decision);
        if (result.Allowed)
        {
            if (result.Identity == null || result.Identity.Length == 0)
            {
                result.Identity = authentication.Identity;
            }
            result.StatusCode = 0;
        }
        else if (result.StatusCode == 0)
        {
            result.StatusCode = SipStatusCodes.Forbidden;
        }
        return result;
    }

    static SipAuthenticationRequest CreatePolicyContext(
        SipAuthenticationRequest source, string identity,
        SipBearerToken? token)
    {
        SipAuthenticationRequest result = new SipAuthenticationRequest();
        result.Message = CopySanitizedMessage(source.Message);
        SipUri? target = source.Target == null
            ? source.Message == null ? null : source.Message.RequestUri
            : source.Target;
        result.Target = target == null
            ? null : SipUriModelCopy.CreateNormalizedCopy(target);
        result.Body = CopyBytes(source.Body);
        result.Scope = source.Scope;
        result.Attempt = source.Attempt;
        result.CorrelationId = source.CorrelationId;
        result.BearerToken = CopyBearerToken(token);
        SipCredential credential = new SipCredential();
        credential.UserName = identity;
        credential.Realm = token == null ? ReadRealm(source) : token.Realm;
        credential.Scope = source.Scope;
        result.Credential = credential;
        return result;
    }

    internal static SipMessage? CopySanitizedMessage(SipMessage? source)
    {
        SipMessage? result = SipSnapshotCopy.CopyMessageModel(source);
        if (result == null)
        {
            return null;
        }
        System.Collections.Generic.List<SipHeader> headers =
            new System.Collections.Generic.List<SipHeader>();
        int index = 0;
        SipHeader[] sourceHeaders = result.Headers == null
            ? new SipHeader[0] : result.Headers;
        while (index < sourceHeaders.Length)
        {
            SipHeader? header = sourceHeaders[index];
            if (header != null && !SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.Authorization)
                && !SipAscii.EqualsIgnoreCase(
                    header.Name, SipHeaderNames.ProxyAuthorization))
            {
                headers.Add(header);
            }
            index = index + 1;
        }
        result.Headers = headers.ToArray();
        result.OriginalBytes = new byte[0];
        return result;
    }

    static string? ReadRealm(SipAuthenticationRequest request)
    {
        if (request.Credential != null)
        {
            return request.Credential.Realm;
        }
        if (request.Challenges != null && request.Challenges.Length > 0)
        {
            SipDigestChallenge? challenge = request.Challenges[0];
            return challenge == null ? null : challenge.Realm;
        }
        return null;
    }

    static SipBearerToken? CopyBearerToken(SipBearerToken? source)
    {
        if (source == null)
        {
            return null;
        }
        SipBearerToken result = new SipBearerToken();
        result.Token = null;
        result.Realm = source.Realm;
        result.AuthorizationServer = source.AuthorizationServer;
        result.Scopes = CopyStrings(source.Scopes);
        result.Subject = source.Subject;
        result.Error = source.Error;
        result.Parameters = CopyParameters(source.Parameters);
        return result;
    }

    static SipAuthorizationDecision CopyDecision(
        SipAuthorizationDecision source)
    {
        SipAuthorizationDecision result = new SipAuthorizationDecision();
        result.Allowed = source.Allowed;
        result.Identity = source.Identity;
        result.Reason = source.Reason;
        result.Scopes = CopyStrings(source.Scopes);
        result.StatusCode = source.StatusCode;
        return result;
    }

    static SipAuthorizationDecision CreateDenial(
        string reason, int statusCode)
    {
        SipAuthorizationDecision result = new SipAuthorizationDecision();
        result.Allowed = false;
        result.Reason = reason;
        result.StatusCode = statusCode;
        return result;
    }

    static byte[] CopyBytes(byte[]? source)
    {
        return source == null ? new byte[0] : (byte[])source.Clone();
    }

    static string[] CopyStrings(string[]? source)
    {
        return source == null ? new string[0] : (string[])source.Clone();
    }

    static SipUriParameter[] CopyParameters(SipUriParameter[]? source)
    {
        if (source == null)
        {
            return new SipUriParameter[0];
        }
        SipUriParameter[] result = new SipUriParameter[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            SipUriParameter parameter = new SipUriParameter();
            parameter.Name = source[index].Name;
            parameter.Value = source[index].Value;
            parameter.HasValue = source[index].HasValue;
            parameter.Quoted = source[index].Quoted;
            result[index] = parameter;
            index = index + 1;
        }
        return result;
    }
}
