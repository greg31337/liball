namespace sip;

internal static class SipAuthenticationHeaderCodec
{
    internal static string? FormatBearerAuthorizationValue(
        SipBearerToken? token)
    {
        if (token == null || !IsBearerToken(token.Token))
        {
            return null;
        }
        return "Bearer " + token.Token;
    }

    internal static string? FormatBearerChallengeValue(
        SipBearerToken? token)
    {
        if (token == null || !IsHttpsAddress(token.AuthorizationServer)
            || !AreScopesValid(token.Scopes) || !IsBearerError(token.Error)
            || token.Parameters == null)
        {
            return null;
        }
        System.Text.StringBuilder builder =
            new System.Text.StringBuilder("Bearer ");
        int count = 0;
        if (token.Realm != null)
        {
            count = AppendPair(builder, count, "realm", token.Realm, true);
        }
        if (token.Scopes.Length > 0)
        {
            count = AppendPair(builder, count, "scope",
                System.String.Join(" ", token.Scopes), true);
        }
        count = AppendPair(builder, count, "authz_server",
            token.AuthorizationServer, true);
        if (token.Error != null)
        {
            count = AppendPair(builder, count, "error", token.Error, true);
        }
        AppendUnknownParameters(builder, count, token.Parameters);
        return builder.ToString();
    }

    internal static string? FormatDigestChallengeValue(
        SipDigestChallenge? challenge)
    {
        if (challenge == null)
        {
            return null;
        }
        return FormatChallenge(challenge);
    }

    internal static string? FormatDigestAuthorizationValue(
        SipDigestAuthorization? authorization)
    {
        if (authorization == null)
        {
            return null;
        }
        return FormatAuthorization(authorization);
    }

    internal static string? FormatAuthenticationInfoValue(
        SipDigestAuthorization? authorization)
    {
        if (authorization == null)
        {
            return null;
        }
        return FormatAuthenticationInfo(authorization);
    }

    internal static bool IsAuthenticationHeaderName(string? name)
    {
        return IsChallengeName(name) || IsAuthorizationName(name)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.AuthenticationInfo);
    }

    internal static SipAuthenticationHeaderResult ParseAuthenticationHeaderValue(string? name, string? text,
        SipParseOptions? options)
    {
        if (name == null || text == null || options == null)
        {
            return CreateError(0, "An authentication header name, value, and parse options are required.");
        }

        if (IsChallengeName(name))
        {
            return ParseChallenge(text, options);
        }

        if (IsAuthorizationName(name))
        {
            return ParseAuthorization(text, options);
        }

        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.AuthenticationInfo))
        {
            return ParseAuthenticationInfo(text, options);
        }

        return CreateError(0, "The header field is not a supported authentication header.");
    }

    internal static SipWriteResult FormatAuthenticationHeaderValue(string? name,
        SipAuthenticationHeaderResult? parsed, SipWriteOptions? options)
    {
        if (name == null || parsed == null || options == null)
        {
            return CreateWriteError("An authentication header value and write options are required.");
        }

        if (!parsed.Success)
        {
            return CreateWriteError(parsed.ErrorMessage == null ? "The authentication header value is invalid."
                : parsed.ErrorMessage);
        }

        string? value = null;
        if (IsChallengeName(name) && parsed.BearerToken != null)
        {
            value = FormatBearerChallengeValue(parsed.BearerToken);
        }
        else if (IsAuthorizationName(name) && parsed.BearerToken != null)
        {
            value = FormatBearerAuthorizationValue(parsed.BearerToken);
        }
        else if (IsChallengeName(name) && parsed.Challenge != null)
        {
            value = FormatChallenge(parsed.Challenge);
        }
        else if (IsAuthorizationName(name) && parsed.Authorization != null)
        {
            value = FormatAuthorization(parsed.Authorization);
        }
        else if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.AuthenticationInfo)
            && parsed.Authorization != null)
        {
            value = FormatAuthenticationInfo(parsed.Authorization);
        }

        if (value == null)
        {
            return CreateWriteError("The authentication header model is incomplete.");
        }

        string headerName = GetFormattedHeaderName(name);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(headerName + ": " + value + "\r\n");
        if (bytes.Length > options.MaxHeaderLineBytes)
        {
            SipWriteResult limited = CreateWriteError("The formatted header field exceeds the configured output limit.");
            limited.RequiredBytes = bytes.Length;
            return limited;
        }

        SipWriteResult result = new SipWriteResult();
        result.Success = true;
        result.Bytes = bytes;
        result.Written = bytes.Length;
        result.RequiredBytes = bytes.Length;
        return result;
    }

    static SipAuthenticationHeaderResult ParseChallenge(string text, SipParseOptions options)
    {
        SipHeaderPrimitiveResult scheme = ParseScheme(text);
        if (!scheme.Success || scheme.Text == null)
        {
            return CreateError(scheme.ErrorOffset, scheme.ErrorMessage);
        }

        string schemeName = scheme.Text;
        string schemeData = text.Substring(scheme.Number);
        SipDigestChallenge challenge = new SipDigestChallenge();
        challenge.Scheme = schemeName;
        challenge.SchemeData = schemeData;
        if (options.PreserveOriginal)
        {
            challenge.OriginalText = text;
        }

        if (!SipAscii.EqualsIgnoreCase(schemeName, "Digest"))
        {
            if (SipAscii.EqualsIgnoreCase(schemeName, "Bearer"))
            {
                SipAuthenticationHeaderResult? bearer =
                    ParseBearerChallenge(schemeData, options);
                if (bearer != null)
                {
                    bearer.Challenge = challenge;
                    return bearer;
                }
            }
            SipHeaderPrimitiveResult unknown = ParseParameterList(schemeData, options.MaxParameterCount);
            if (unknown.Success)
            {
                challenge.Parameters = unknown.Parameters;
                ClearParameterOriginals(challenge.Parameters, options.PreserveOriginal);
            }

            SipAuthenticationHeaderResult unknownResult = CreateSuccess();
            unknownResult.Challenge = challenge;
            return unknownResult;
        }

        SipHeaderPrimitiveResult parameters = ParseParameterList(schemeData, options.MaxParameterCount);
        if (!parameters.Success)
        {
            return CreateError(scheme.Number + parameters.ErrorOffset, parameters.ErrorMessage);
        }

        challenge.Algorithm = SipDigestAlgorithm.Md5;
        bool realm = false;
        bool nonce = false;
        bool domain = false;
        bool opaque = false;
        bool stale = false;
        bool algorithm = false;
        bool qop = false;
        bool charset = false;
        bool userhash = false;
        System.Collections.Generic.List<SipUriParameter> unknownParameters =
            new System.Collections.Generic.List<SipUriParameter>();
        int index = 0;
        while (index < parameters.Parameters.Length)
        {
            SipUriParameter parameter = parameters.Parameters[index];
            string? issue = null;
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "realm"))
            {
                issue = ValidateKnownParameter(parameter, realm, true, "realm");
                realm = true;
                challenge.Realm = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "nonce"))
            {
                issue = ValidateKnownParameter(parameter, nonce, true, "nonce");
                nonce = true;
                challenge.Nonce = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "domain"))
            {
                issue = ValidateKnownParameter(parameter, domain, true, "domain");
                domain = true;
                challenge.Domain = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "opaque"))
            {
                issue = ValidateKnownParameter(parameter, opaque, true, "opaque");
                opaque = true;
                challenge.Opaque = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "stale"))
            {
                issue = ValidateBooleanParameter(parameter, stale, "stale");
                stale = true;
                challenge.HasStale = true;
                challenge.Stale = SipAscii.EqualsIgnoreCase(parameter.Value, "true");
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "algorithm"))
            {
                issue = ValidateKnownParameter(parameter, algorithm, false, "algorithm");
                algorithm = true;
                challenge.AlgorithmToken = parameter.Value;
                challenge.Algorithm = GetAlgorithm(parameter.Value);
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "qop"))
            {
                issue = ValidateKnownParameter(parameter, qop, true, "qop");
                qop = true;
                if (issue == null)
                {
                    issue = ApplyChallengeQualities(challenge, parameter.Value, options.MaxListValueCount);
                }
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "charset"))
            {
                issue = ValidateKnownParameter(parameter, charset, false, "charset");
                charset = true;
                if (issue == null && !SipAscii.EqualsIgnoreCase(parameter.Value, "UTF-8"))
                {
                    issue = "The Digest charset parameter must be UTF-8.";
                }

                challenge.Utf8 = issue == null;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "userhash"))
            {
                issue = ValidateBooleanParameter(parameter, userhash, "userhash");
                userhash = true;
                challenge.HasUserHash = true;
                challenge.UserHash = SipAscii.EqualsIgnoreCase(parameter.Value, "true");
            }
            else
            {
                unknownParameters.Add(parameter);
            }

            if (issue != null)
            {
                return CreateError(scheme.Number, issue);
            }

            index = index + 1;
        }

        if (!realm || !nonce)
        {
            return CreateError(scheme.Number, "A Digest challenge requires realm and nonce parameters.");
        }

        challenge.Parameters = unknownParameters.ToArray();
        ClearParameterOriginals(challenge.Parameters, options.PreserveOriginal);
        SipAuthenticationHeaderResult result = CreateSuccess();
        result.Challenge = challenge;
        return result;
    }

    static SipAuthenticationHeaderResult ParseAuthorization(string text, SipParseOptions options)
    {
        SipHeaderPrimitiveResult scheme = ParseScheme(text);
        if (!scheme.Success || scheme.Text == null)
        {
            return CreateError(scheme.ErrorOffset, scheme.ErrorMessage);
        }

        string schemeName = scheme.Text;
        string schemeData = text.Substring(scheme.Number);
        SipDigestAuthorization authorization = new SipDigestAuthorization();
        authorization.Scheme = schemeName;
        authorization.SchemeData = schemeData;
        if (options.PreserveOriginal)
        {
            authorization.OriginalText = text;
        }

        if (!SipAscii.EqualsIgnoreCase(schemeName, "Digest"))
        {
            if (SipAscii.EqualsIgnoreCase(schemeName, "Bearer"))
            {
                if (!IsBearerToken(schemeData))
                {
                    return CreateError(scheme.Number,
                        "The Bearer access token is malformed.");
                }
                SipBearerToken bearerToken = new SipBearerToken();
                bearerToken.Token = schemeData;
                SipAuthenticationHeaderResult bearerResult = CreateSuccess();
                bearerResult.Authorization = authorization;
                bearerResult.BearerToken = bearerToken;
                return bearerResult;
            }
            SipHeaderPrimitiveResult unknown = ParseParameterList(schemeData, options.MaxParameterCount);
            if (unknown.Success)
            {
                authorization.Parameters = unknown.Parameters;
                ClearParameterOriginals(authorization.Parameters, options.PreserveOriginal);
            }

            SipAuthenticationHeaderResult unknownResult = CreateSuccess();
            unknownResult.Authorization = authorization;
            return unknownResult;
        }

        SipHeaderPrimitiveResult parameters = ParseParameterList(schemeData, options.MaxParameterCount);
        if (!parameters.Success)
        {
            return CreateError(scheme.Number + parameters.ErrorOffset, parameters.ErrorMessage);
        }

        authorization.Algorithm = SipDigestAlgorithm.Md5;
        bool username = false;
        bool usernameExtended = false;
        bool realm = false;
        bool nonce = false;
        bool uri = false;
        bool response = false;
        bool algorithm = false;
        bool qop = false;
        bool nc = false;
        bool cnonce = false;
        bool opaque = false;
        bool userhash = false;
        System.Collections.Generic.List<SipUriParameter> unknownParameters =
            new System.Collections.Generic.List<SipUriParameter>();
        int index = 0;
        while (index < parameters.Parameters.Length)
        {
            SipUriParameter parameter = parameters.Parameters[index];
            string? issue = null;
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "username"))
            {
                issue = ValidateKnownParameter(parameter, username || usernameExtended, true, "username");
                username = true;
                authorization.UserName = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "username*"))
            {
                issue = ValidateKnownParameter(parameter, username || usernameExtended, false, "username*");
                usernameExtended = true;
                authorization.UserNameExtended = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "realm"))
            {
                issue = ValidateKnownParameter(parameter, realm, true, "realm");
                realm = true;
                authorization.Realm = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "nonce"))
            {
                issue = ValidateKnownParameter(parameter, nonce, true, "nonce");
                nonce = true;
                authorization.Nonce = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "uri"))
            {
                issue = ValidateKnownParameter(parameter, uri, true, "uri");
                uri = true;
                if (issue == null && !IsDigestUri(parameter.Value, options))
                {
                    issue = "The Digest uri parameter must contain a valid request URI.";
                }

                authorization.DigestUri = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "response"))
            {
                issue = ValidateKnownParameter(parameter, response, true, "response");
                response = true;
                if (issue == null && !IsDigestHex(parameter.Value))
                {
                    issue = "The Digest response parameter must be empty or contain at least 32 lowercase hex digits.";
                }

                authorization.Response = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "algorithm"))
            {
                issue = ValidateKnownParameter(parameter, algorithm, false, "algorithm");
                algorithm = true;
                authorization.AlgorithmToken = parameter.Value;
                authorization.Algorithm = GetAlgorithm(parameter.Value);
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "qop"))
            {
                issue = ValidateKnownParameter(parameter, qop, false, "qop");
                qop = true;
                authorization.QualityToken = parameter.Value;
                authorization.Quality = GetQuality(parameter.Value);
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "nc"))
            {
                issue = ValidateKnownParameter(parameter, nc, false, "nc");
                nc = true;
                long count = ParseNonceCount(parameter.Value);
                if (issue == null && count < 0)
                {
                    issue = "The Digest nc parameter must contain exactly eight lowercase hex digits.";
                }

                authorization.NonceCount = count;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "cnonce"))
            {
                issue = ValidateKnownParameter(parameter, cnonce, true, "cnonce");
                cnonce = true;
                authorization.ClientNonce = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "opaque"))
            {
                issue = ValidateKnownParameter(parameter, opaque, true, "opaque");
                opaque = true;
                authorization.Opaque = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "userhash"))
            {
                issue = ValidateBooleanParameter(parameter, userhash, "userhash");
                userhash = true;
                authorization.HasUserHash = true;
                authorization.UserHash = SipAscii.EqualsIgnoreCase(parameter.Value, "true");
            }
            else
            {
                unknownParameters.Add(parameter);
            }

            if (issue != null)
            {
                return CreateError(scheme.Number, issue);
            }

            index = index + 1;
        }

        if ((!username && !usernameExtended) || !realm || !nonce || !uri || !response)
        {
            return CreateError(scheme.Number,
                "Digest credentials require username, realm, nonce, uri, and response parameters.");
        }

        if (qop && (!cnonce || !nc))
        {
            return CreateError(scheme.Number, "Digest credentials with qop require cnonce and nc parameters.");
        }

        if (!IsResponseLengthValid(authorization.Response, authorization.Algorithm))
        {
            return CreateError(scheme.Number,
                "The Digest response length does not match the selected digest algorithm.");
        }

        authorization.Parameters = unknownParameters.ToArray();
        ClearParameterOriginals(authorization.Parameters, options.PreserveOriginal);
        SipAuthenticationHeaderResult result = CreateSuccess();
        result.Authorization = authorization;
        return result;
    }

    static SipAuthenticationHeaderResult ParseAuthenticationInfo(string text, SipParseOptions options)
    {
        SipHeaderPrimitiveResult parameters = ParseParameterList(text, options.MaxParameterCount);
        if (!parameters.Success)
        {
            return CreateError(parameters.ErrorOffset, parameters.ErrorMessage);
        }

        SipDigestAuthorization authorization = new SipDigestAuthorization();
        if (options.PreserveOriginal)
        {
            authorization.OriginalText = text;
        }

        bool nextnonce = false;
        bool response = false;
        bool qop = false;
        bool cnonce = false;
        bool nc = false;
        System.Collections.Generic.List<SipUriParameter> unknownParameters =
            new System.Collections.Generic.List<SipUriParameter>();
        int index = 0;
        while (index < parameters.Parameters.Length)
        {
            SipUriParameter parameter = parameters.Parameters[index];
            string? issue = null;
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "nextnonce"))
            {
                issue = ValidateKnownParameter(parameter, nextnonce, true, "nextnonce");
                nextnonce = true;
                authorization.NextNonce = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "rspauth"))
            {
                issue = ValidateKnownParameter(parameter, response, true, "rspauth");
                response = true;
                if (issue == null && !IsDigestHex(parameter.Value))
                {
                    issue = "The rspauth parameter must be empty or contain at least 32 lowercase hex digits.";
                }

                authorization.Response = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "qop"))
            {
                issue = ValidateKnownParameter(parameter, qop, false, "qop");
                qop = true;
                authorization.QualityToken = parameter.Value;
                authorization.Quality = GetQuality(parameter.Value);
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "cnonce"))
            {
                issue = ValidateKnownParameter(parameter, cnonce, true, "cnonce");
                cnonce = true;
                authorization.ClientNonce = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "nc"))
            {
                issue = ValidateKnownParameter(parameter, nc, false, "nc");
                nc = true;
                long count = ParseNonceCount(parameter.Value);
                if (issue == null && count < 0)
                {
                    issue = "The nc parameter must contain exactly eight lowercase hex digits.";
                }

                authorization.NonceCount = count;
            }
            else
            {
                unknownParameters.Add(parameter);
            }

            if (issue != null)
            {
                return CreateError(0, issue);
            }

            index = index + 1;
        }

        if (qop && (!response || !cnonce || !nc))
        {
            return CreateError(0, "Authentication-Info with qop requires rspauth, cnonce, and nc parameters.");
        }

        authorization.Parameters = unknownParameters.ToArray();
        ClearParameterOriginals(authorization.Parameters, options.PreserveOriginal);
        SipAuthenticationHeaderResult result = CreateSuccess();
        result.Authorization = authorization;
        return result;
    }

    static SipHeaderPrimitiveResult ParseScheme(string text)
    {
        int separator = 0;
        while (separator < text.Length && text[separator] != ' ' && text[separator] != '\t')
        {
            separator = separator + 1;
        }

        string scheme = text.Substring(0, separator);
        if (!SipAscii.IsToken(scheme) || separator == text.Length)
        {
            return CreatePrimitiveError(0, "An authentication scheme and value separated by whitespace are required.");
        }

        int dataStart = SkipWhitespace(text, separator);
        if (dataStart == text.Length)
        {
            return CreatePrimitiveError(dataStart, "The authentication scheme value is empty.");
        }

        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.Text = scheme;
        result.Number = dataStart;
        return result;
    }

    static SipHeaderPrimitiveResult ParseParameterList(string text, int maximumCount)
    {
        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, maximumCount);
        if (!split.Success)
        {
            return split;
        }

        SipUriParameter[] parameters = new SipUriParameter[split.TextValues.Length];
        int index = 0;
        int offset = 0;
        while (index < split.TextValues.Length)
        {
            string item = split.TextValues[index];
            int itemOffset = FindItemOffset(text, item, offset);
            SipHeaderPrimitiveResult parameter = SipHeaderPrimitives.ParseParameterText(item);
            if (!parameter.Success || parameter.Parameter == null || !parameter.Parameter.HasValue)
            {
                return CreatePrimitiveError(itemOffset + parameter.ErrorOffset,
                    "Each authentication parameter requires a name and value.");
            }

            parameters[index] = parameter.Parameter;
            offset = itemOffset + item.Length;
            index = index + 1;
        }

        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.Success = true;
        result.Parameters = parameters;
        return result;
    }

    static string? ApplyChallengeQualities(SipDigestChallenge challenge, string? text, int maximumCount)
    {
        if (text == null)
        {
            return "The Digest qop parameter is empty.";
        }

        SipHeaderPrimitiveResult split = SipHeaderPrimitives.SplitCommaListText(text, maximumCount);
        if (!split.Success)
        {
            return split.ErrorMessage;
        }

        SipDigestQuality[] qualities = new SipDigestQuality[split.TextValues.Length];
        int index = 0;
        while (index < split.TextValues.Length)
        {
            if (!SipAscii.IsToken(split.TextValues[index]))
            {
                return "Every Digest qop value must be a token.";
            }

            qualities[index] = GetQuality(split.TextValues[index]);
            index = index + 1;
        }

        challenge.QualityTokens = split.TextValues;
        challenge.Qualities = qualities;
        return null;
    }

    static string? ValidateKnownParameter(SipUriParameter parameter, bool duplicate, bool quoted,
        string parameterName)
    {
        if (duplicate)
        {
            return "The Digest " + parameterName + " parameter cannot be repeated.";
        }

        if (!parameter.HasValue || parameter.Value == null || IsQuotedParameter(parameter) != quoted)
        {
            return "The Digest " + parameterName + " parameter uses the wrong quoted-string form.";
        }

        if (!quoted && !SipAscii.IsToken(parameter.Value))
        {
            return "The Digest " + parameterName + " parameter must be a token.";
        }

        return null;
    }

    static string? ValidateBooleanParameter(SipUriParameter parameter, bool duplicate, string parameterName)
    {
        string? issue = ValidateKnownParameter(parameter, duplicate, false, parameterName);
        if (issue == null && !SipAscii.EqualsIgnoreCase(parameter.Value, "true")
            && !SipAscii.EqualsIgnoreCase(parameter.Value, "false"))
        {
            return "The Digest " + parameterName + " parameter must be true or false.";
        }

        return issue;
    }

    static SipDigestAlgorithm GetAlgorithm(string? token)
    {
        if (SipAscii.EqualsIgnoreCase(token, "MD5")) return SipDigestAlgorithm.Md5;
        if (SipAscii.EqualsIgnoreCase(token, "MD5-sess")) return SipDigestAlgorithm.Md5Sess;
        if (SipAscii.EqualsIgnoreCase(token, "SHA-256")) return SipDigestAlgorithm.Sha256;
        if (SipAscii.EqualsIgnoreCase(token, "SHA-256-sess")) return SipDigestAlgorithm.Sha256Sess;
        if (SipAscii.EqualsIgnoreCase(token, "SHA-512-256")) return SipDigestAlgorithm.Sha512256;
        if (SipAscii.EqualsIgnoreCase(token, "SHA-512-256-sess")) return SipDigestAlgorithm.Sha512256Sess;
        return SipDigestAlgorithm.Unknown;
    }

    static SipDigestQuality GetQuality(string? token)
    {
        if (SipAscii.EqualsIgnoreCase(token, "auth")) return SipDigestQuality.Auth;
        if (SipAscii.EqualsIgnoreCase(token, "auth-int")) return SipDigestQuality.AuthInt;
        return SipDigestQuality.None;
    }

    static bool IsDigestHex(string? text)
    {
        if (text == null || text.Length > 0 && text.Length < 32)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!IsLowerHex(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsResponseLengthValid(string? text, SipDigestAlgorithm algorithm)
    {
        if (text == null)
        {
            return false;
        }

        if (text.Length == 0)
        {
            return true;
        }

        if (algorithm == SipDigestAlgorithm.Md5 || algorithm == SipDigestAlgorithm.Md5Sess)
        {
            return text.Length == 32;
        }

        if (algorithm == SipDigestAlgorithm.Sha256 || algorithm == SipDigestAlgorithm.Sha256Sess
            || algorithm == SipDigestAlgorithm.Sha512256 || algorithm == SipDigestAlgorithm.Sha512256Sess)
        {
            return text.Length == 64;
        }

        return text.Length >= 32;
    }

    static bool IsDigestUri(string? text, SipParseOptions options)
    {
        if (text == null || text.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] > 127)
            {
                return false;
            }

            index = index + 1;
        }

        SipParseResult parsed = SipUriParser.ParseUriBytes(System.Text.Encoding.ASCII.GetBytes(text), options);
        return parsed.Status == SipParseStatus.Complete;
    }

    static long ParseNonceCount(string? text)
    {
        if (text == null || text.Length != 8)
        {
            return -1;
        }

        long value = 0;
        int index = 0;
        while (index < text.Length)
        {
            int digit = GetLowerHexValue(text[index]);
            if (digit < 0)
            {
                return -1;
            }

            value = value * 16 + digit;
            index = index + 1;
        }

        return value;
    }

    static SipAuthenticationHeaderResult? ParseBearerChallenge(
        string text, SipParseOptions options)
    {
        SipHeaderPrimitiveResult parameters = ParseParameterList(
            text, options.MaxParameterCount);
        if (!parameters.Success)
        {
            return null;
        }
        SipBearerToken token = new SipBearerToken();
        bool realm = false;
        bool scope = false;
        bool authorizationServer = false;
        bool error = false;
        bool recognized = false;
        System.Collections.Generic.List<SipUriParameter> unknownParameters =
            new System.Collections.Generic.List<SipUriParameter>();
        int index = 0;
        while (index < parameters.Parameters.Length)
        {
            SipUriParameter parameter = parameters.Parameters[index];
            string? issue = null;
            if (SipAscii.EqualsIgnoreCase(parameter.Name, "realm"))
            {
                recognized = true;
                issue = ValidateKnownParameter(parameter, realm, true, "realm");
                realm = true;
                token.Realm = parameter.Value;
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "scope"))
            {
                recognized = true;
                issue = ValidateKnownParameter(parameter, scope, true, "scope");
                scope = true;
                if (issue == null)
                {
                    token.Scopes = ParseBearerScopes(parameter.Value);
                    if (token.Scopes.Length == 0)
                    {
                        issue = "The Bearer scope parameter is malformed.";
                    }
                }
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "authz_server"))
            {
                recognized = true;
                issue = ValidateKnownParameter(parameter,
                    authorizationServer, true, "authz_server");
                authorizationServer = true;
                token.AuthorizationServer = parameter.Value;
                if (issue == null
                    && !IsHttpsAddress(token.AuthorizationServer))
                {
                    issue = "The Bearer authz_server parameter must be an HTTPS URI.";
                }
            }
            else if (SipAscii.EqualsIgnoreCase(parameter.Name, "error"))
            {
                recognized = true;
                issue = ValidateKnownParameter(parameter, error, true, "error");
                error = true;
                token.Error = parameter.Value;
                if (issue == null && !IsBearerError(token.Error))
                {
                    issue = "The Bearer error parameter is unsupported.";
                }
            }
            else
            {
                unknownParameters.Add(parameter);
            }
            if (issue != null)
            {
                return CreateError(0, issue);
            }
            index = index + 1;
        }
        if (!recognized)
        {
            return null;
        }
        if (!authorizationServer)
        {
            return CreateError(0,
                "A Bearer challenge requires an authz_server parameter.");
        }
        token.Parameters = unknownParameters.ToArray();
        ClearParameterOriginals(token.Parameters, options.PreserveOriginal);
        SipAuthenticationHeaderResult result = CreateSuccess();
        result.BearerToken = token;
        return result;
    }

    static string[] ParseBearerScopes(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return new string[0];
        }
        string[] scopes = value.Split(' ');
        return AreScopesValid(scopes) ? scopes : new string[0];
    }

    static bool AreScopesValid(string[]? scopes)
    {
        if (scopes == null)
        {
            return false;
        }
        int index = 0;
        while (index < scopes.Length)
        {
            string? scope = scopes[index];
            if (scope == null || scope.Length == 0)
            {
                return false;
            }
            int characterIndex = 0;
            while (characterIndex < scope.Length)
            {
                int character = scope[characterIndex];
                if (character < 33 || character > 126
                    || character == 34 || character == 92)
                {
                    return false;
                }
                characterIndex = characterIndex + 1;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsBearerToken(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }
        bool padding = false;
        bool content = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '=')
            {
                padding = true;
            }
            else
            {
                if (padding || !(character >= 'A' && character <= 'Z'
                    || character >= 'a' && character <= 'z'
                    || character >= '0' && character <= '9'
                    || character == '-' || character == '.'
                    || character == '_' || character == '~'
                    || character == '+' || character == '/'))
                {
                    return false;
                }
                content = true;
            }
            index = index + 1;
        }
        return content;
    }

    static bool IsHttpsAddress(string? value)
    {
        if (value == null || value.Length <= 8
            || !SipAscii.EqualsIgnoreCase(value.Substring(0, 8), "https://")
            || !System.Uri.IsWellFormedUriString(
                value, System.UriKind.Absolute))
        {
            return false;
        }
        int index = 8;
        while (index < value.Length)
        {
            char character = value[index];
            if (character <= 32 || character >= 127
                || character == '"' || character == '\\')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool IsBearerError(string? value)
    {
        return value == null || value == "invalid_token"
            || value == "invalid_scope";
    }

    static string FormatChallenge(SipDigestChallenge challenge)
    {
        if (!SipAscii.EqualsIgnoreCase(challenge.Scheme, "Digest"))
        {
            return FormatUnknownScheme(challenge.Scheme, challenge.SchemeData);
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder("Digest ");
        int count = 0;
        count = AppendPair(builder, count, "realm", challenge.Realm, true);
        if (challenge.Domain != null) count = AppendPair(builder, count, "domain", challenge.Domain, true);
        count = AppendPair(builder, count, "nonce", challenge.Nonce, true);
        if (challenge.Opaque != null) count = AppendPair(builder, count, "opaque", challenge.Opaque, true);
        if (challenge.HasStale) count = AppendPair(builder, count, "stale", challenge.Stale ? "true" : "false", false);
        if (challenge.AlgorithmToken != null)
        {
            count = AppendPair(builder, count, "algorithm", GetAlgorithmToken(challenge), false);
        }

        if (challenge.QualityTokens.Length > 0)
        {
            count = AppendPair(builder, count, "qop", SipHeaderPrimitives.JoinCommaListValues(
                challenge.QualityTokens), true);
        }

        if (challenge.Utf8) count = AppendPair(builder, count, "charset", "UTF-8", false);
        if (challenge.HasUserHash)
        {
            count = AppendPair(builder, count, "userhash", challenge.UserHash ? "true" : "false", false);
        }

        AppendUnknownParameters(builder, count, challenge.Parameters);
        return builder.ToString();
    }

    static string FormatAuthorization(SipDigestAuthorization authorization)
    {
        if (!SipAscii.EqualsIgnoreCase(authorization.Scheme, "Digest"))
        {
            return FormatUnknownScheme(authorization.Scheme, authorization.SchemeData);
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder("Digest ");
        int count = 0;
        if (authorization.UserNameExtended != null)
        {
            count = AppendPair(builder, count, "username*", authorization.UserNameExtended, false);
        }
        else
        {
            count = AppendPair(builder, count, "username", authorization.UserName, true);
        }
        count = AppendPair(builder, count, "realm", authorization.Realm, true);
        count = AppendPair(builder, count, "nonce", authorization.Nonce, true);
        count = AppendPair(builder, count, "uri", authorization.DigestUri, true);
        count = AppendPair(builder, count, "response", authorization.Response, true);
        if (authorization.AlgorithmToken != null)
        {
            count = AppendPair(builder, count, "algorithm", GetAuthorizationAlgorithmToken(authorization), false);
        }

        if (authorization.QualityToken != null)
        {
            count = AppendPair(builder, count, "qop", authorization.QualityToken, false);
        }

        if (authorization.NonceCount >= 0)
        {
            count = AppendPair(builder, count, "nc", authorization.NonceCount.ToString(
                "x8", System.Globalization.CultureInfo.InvariantCulture), false);
        }

        if (authorization.ClientNonce != null)
        {
            count = AppendPair(builder, count, "cnonce", authorization.ClientNonce, true);
        }

        if (authorization.Opaque != null) count = AppendPair(builder, count, "opaque", authorization.Opaque, true);
        if (authorization.HasUserHash)
        {
            count = AppendPair(builder, count, "userhash", authorization.UserHash ? "true" : "false", false);
        }

        AppendUnknownParameters(builder, count, authorization.Parameters);
        return builder.ToString();
    }

    static string FormatAuthenticationInfo(SipDigestAuthorization authorization)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int count = 0;
        if (authorization.NextNonce != null)
        {
            count = AppendPair(builder, count, "nextnonce", authorization.NextNonce, true);
        }

        if (authorization.QualityToken != null)
        {
            count = AppendPair(builder, count, "qop", authorization.QualityToken, false);
        }

        if (authorization.Response != null)
        {
            count = AppendPair(builder, count, "rspauth", authorization.Response, true);
        }

        if (authorization.ClientNonce != null)
        {
            count = AppendPair(builder, count, "cnonce", authorization.ClientNonce, true);
        }

        if (authorization.NonceCount >= 0)
        {
            count = AppendPair(builder, count, "nc", authorization.NonceCount.ToString(
                "x8", System.Globalization.CultureInfo.InvariantCulture), false);
        }

        AppendUnknownParameters(builder, count, authorization.Parameters);
        return builder.ToString();
    }

    static int AppendPair(System.Text.StringBuilder builder, int count, string name, string? value, bool quoted)
    {
        if (count > 0)
        {
            builder.Append(", ");
        }

        builder.Append(name);
        builder.Append('=');
        builder.Append(quoted ? SipHeaderPrimitives.FormatQuotedString(value) : value);
        return count + 1;
    }

    static void AppendUnknownParameters(System.Text.StringBuilder builder, int count,
        SipUriParameter[] parameters)
    {
        int index = 0;
        while (index < parameters.Length)
        {
            if (count > 0)
            {
                builder.Append(", ");
            }

            SipUriParameter parameter = parameters[index];
            builder.Append(parameter.Name == null ? "" : parameter.Name.ToLowerInvariant());
            builder.Append('=');
            if (parameter.Value != null && SipAscii.IsToken(parameter.Value))
            {
                builder.Append(parameter.Value);
            }
            else
            {
                builder.Append(SipHeaderPrimitives.FormatQuotedString(parameter.Value));
            }

            count = count + 1;
            index = index + 1;
        }
    }

    static string FormatUnknownScheme(string? scheme, string? schemeData)
    {
        return (scheme == null ? "" : scheme) + " " + (schemeData == null ? "" : schemeData);
    }

    static string GetAlgorithmToken(SipDigestChallenge challenge)
    {
        return GetCanonicalAlgorithmToken(challenge.Algorithm, challenge.AlgorithmToken);
    }

    static string GetAuthorizationAlgorithmToken(SipDigestAuthorization authorization)
    {
        return GetCanonicalAlgorithmToken(authorization.Algorithm, authorization.AlgorithmToken);
    }

    static string GetCanonicalAlgorithmToken(SipDigestAlgorithm algorithm, string? original)
    {
        switch (algorithm)
        {
            case SipDigestAlgorithm.Md5: return "MD5";
            case SipDigestAlgorithm.Md5Sess: return "MD5-sess";
            case SipDigestAlgorithm.Sha256: return "SHA-256";
            case SipDigestAlgorithm.Sha256Sess: return "SHA-256-sess";
            case SipDigestAlgorithm.Sha512256: return "SHA-512-256";
            case SipDigestAlgorithm.Sha512256Sess: return "SHA-512-256-sess";
            default: return original == null ? "" : original;
        }
    }

    static void ClearParameterOriginals(SipUriParameter[] parameters, bool preserveOriginal)
    {
        if (preserveOriginal)
        {
            return;
        }

        int index = 0;
        while (index < parameters.Length)
        {
            parameters[index].OriginalText = null;
            index = index + 1;
        }
    }

    static bool IsQuotedParameter(SipUriParameter parameter)
    {
        if (parameter.Quoted)
        {
            return true;
        }

        if (parameter.OriginalText == null)
        {
            return false;
        }

        int equals = parameter.OriginalText.IndexOf('=');
        int position = equals < 0 ? parameter.OriginalText.Length : SkipWhitespace(parameter.OriginalText, equals + 1);
        return position < parameter.OriginalText.Length && parameter.OriginalText[position] == '"';
    }

    static bool IsLowerHex(int character)
    {
        return character >= '0' && character <= '9' || character >= 'a' && character <= 'f';
    }

    static int GetLowerHexValue(int character)
    {
        if (character >= '0' && character <= '9') return character - '0';
        if (character >= 'a' && character <= 'f') return character - 'a' + 10;
        return -1;
    }

    static bool IsChallengeName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.WwwAuthenticate)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ProxyAuthenticate);
    }

    static bool IsAuthorizationName(string? name)
    {
        return SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Authorization)
            || SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ProxyAuthorization);
    }

    static string GetFormattedHeaderName(string name)
    {
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.WwwAuthenticate)) return SipHeaderNames.WwwAuthenticate;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ProxyAuthenticate)) return SipHeaderNames.ProxyAuthenticate;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.Authorization)) return SipHeaderNames.Authorization;
        if (SipAscii.EqualsIgnoreCase(name, SipHeaderNames.ProxyAuthorization)) return SipHeaderNames.ProxyAuthorization;
        return SipHeaderNames.AuthenticationInfo;
    }

    static int FindItemOffset(string text, string item, int start)
    {
        int offset = text.IndexOf(item, start, System.StringComparison.Ordinal);
        return offset < 0 ? start : offset;
    }

    static int SkipWhitespace(string text, int start)
    {
        int index = start;
        while (index < text.Length && (text[index] == ' ' || text[index] == '\t'))
        {
            index = index + 1;
        }

        return index;
    }

    static SipAuthenticationHeaderResult CreateSuccess()
    {
        SipAuthenticationHeaderResult result = new SipAuthenticationHeaderResult();
        result.Success = true;
        return result;
    }

    static SipAuthenticationHeaderResult CreateError(int offset, string? message)
    {
        SipAuthenticationHeaderResult result = new SipAuthenticationHeaderResult();
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        return result;
    }

    static SipHeaderPrimitiveResult CreatePrimitiveError(int offset, string? message)
    {
        SipHeaderPrimitiveResult result = new SipHeaderPrimitiveResult();
        result.ErrorOffset = offset;
        result.ErrorMessage = message;
        return result;
    }

    static SipWriteResult CreateWriteError(string message)
    {
        SipWriteResult result = new SipWriteResult();
        result.ErrorMessage = message;
        return result;
    }
}
