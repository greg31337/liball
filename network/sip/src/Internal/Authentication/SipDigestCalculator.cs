namespace sip;

internal static class SipDigestCalculator
{
    internal static SipDigestCalculationResult Calculate(
        SipDigestCalculation? calculation)
    {
        Validate(calculation);
        SipDigestCalculation input = calculation == null
            ? new SipDigestCalculation() : calculation;
        string userName = input.UserName == null ? "" : input.UserName;
        string realm = input.Realm == null ? "" : input.Realm;
        byte[] secret = input.Secret == null ? new byte[0] : input.Secret;
        string nonce = input.Nonce == null ? "" : input.Nonce;
        string clientNonce = input.ClientNonce == null ? "" : input.ClientNonce;
        string method = input.Method == null ? "" : input.Method;
        string digestUri = input.DigestUri == null ? "" : input.DigestUri;
        byte[] requestBody = input.RequestBody == null
            ? new byte[0] : input.RequestBody;
        byte[] responseBody = input.ResponseBody == null
            ? new byte[0] : input.ResponseBody;

        byte[] userNameBytes = EncodeCredentialText(
            userName, input.Utf8, true);
        byte[] realmBytes = EncodeCredentialText(realm, input.Utf8, false);
        byte[] secretBytes = EncodeSecret(secret, input.Utf8);
        byte[] colon = new byte[] { 58 };
        byte[] credentialBytes = Concatenate(new byte[][]
        {
            userNameBytes, colon, realmBytes, colon, secretBytes
        });
        string credentialHash = SipDigestHash.HashHex(
            input.Algorithm, credentialBytes);
        string sessionHash = credentialHash;
        if (SipDigestHash.UsesSessionKey(input.Algorithm))
        {
            sessionHash = SipDigestHash.HashTextHex(input.Algorithm,
                credentialHash + ":" + nonce + ":" + clientNonce);
        }

        byte[] userHashBytes = Concatenate(new byte[][]
        {
            userNameBytes, colon, realmBytes
        });
        string userHash = SipDigestHash.HashHex(
            input.Algorithm, userHashBytes);
        string requestBodyHash = "";
        string responseBodyHash = "";
        if (input.Quality == SipDigestQuality.AuthInt)
        {
            requestBodyHash = SipDigestHash.HashHex(
                input.Algorithm, requestBody);
            responseBodyHash = SipDigestHash.HashHex(
                input.Algorithm, responseBody);
        }

        string requestA2 = method + ":" + digestUri;
        string responseA2 = ":" + digestUri;
        if (input.Quality == SipDigestQuality.AuthInt)
        {
            requestA2 = requestA2 + ":" + requestBodyHash;
            responseA2 = responseA2 + ":" + responseBodyHash;
        }
        string requestA2Hash = SipDigestHash.HashTextHex(
            input.Algorithm, requestA2);
        string responseA2Hash = SipDigestHash.HashTextHex(
            input.Algorithm, responseA2);

        string? nonceCountText = null;
        string requestData = nonce + ":" + requestA2Hash;
        string responseData = nonce + ":" + responseA2Hash;
        if (input.Quality != SipDigestQuality.None)
        {
            nonceCountText = input.NonceCount.ToString(
                "x8", System.Globalization.CultureInfo.InvariantCulture);
            string quality = input.Quality == SipDigestQuality.Auth
                ? "auth" : "auth-int";
            string prefix = nonce + ":" + nonceCountText + ":"
                + clientNonce + ":" + quality + ":";
            requestData = prefix + requestA2Hash;
            responseData = prefix + responseA2Hash;
        }

        SipDigestCalculationResult result = new SipDigestCalculationResult();
        result.Response = SipDigestHash.HashTextHex(input.Algorithm,
            sessionHash + ":" + requestData);
        result.ResponseAuthentication = SipDigestHash.HashTextHex(
            input.Algorithm, sessionHash + ":" + responseData);
        result.UserHash = userHash;
        result.NonceCountText = nonceCountText;

        System.Security.Cryptography.CryptographicOperations.ZeroMemory(
            userNameBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(realmBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(secretBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(credentialBytes);
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(userHashBytes);
        return result;
    }

    static void Validate(SipDigestCalculation? calculation)
    {
        if (calculation == null)
        {
            throw new System.ArgumentNullException("calculation");
        }
        SipDigestHash.HashByteCount(calculation.Algorithm);
        if (calculation.Quality != SipDigestQuality.None
            && calculation.Quality != SipDigestQuality.Auth
            && calculation.Quality != SipDigestQuality.AuthInt)
        {
            throw new System.ArgumentOutOfRangeException("Quality");
        }
        if (calculation.UserName == null)
        {
            throw new System.ArgumentNullException("UserName");
        }
        if (calculation.UserName.Length == 0
            || calculation.UserName.IndexOf(':') >= 0)
        {
            throw new System.ArgumentException(
                "A Digest user name must be non-empty and cannot contain a colon.",
                "UserName");
        }
        if (calculation.Realm == null)
        {
            throw new System.ArgumentNullException("Realm");
        }
        if (calculation.Secret == null)
        {
            throw new System.ArgumentNullException("Secret");
        }
        if (calculation.Nonce == null)
        {
            throw new System.ArgumentNullException("Nonce");
        }
        if (calculation.Nonce.Length == 0)
        {
            throw new System.ArgumentException(
                "A Digest nonce must be non-empty.", "Nonce");
        }
        ValidateAscii(calculation.Nonce, "Nonce");
        if (!SipAscii.IsToken(calculation.Method))
        {
            throw new System.ArgumentException(
                "A Digest method must be a SIP token.", "Method");
        }
        if (calculation.DigestUri == null)
        {
            throw new System.ArgumentNullException("DigestUri");
        }
        if (calculation.DigestUri.Length == 0)
        {
            throw new System.ArgumentException(
                "A Digest URI must be non-empty.", "DigestUri");
        }
        ValidateAscii(calculation.DigestUri, "DigestUri");

        bool session = SipDigestHash.UsesSessionKey(calculation.Algorithm);
        bool clientNonceRequired = session
            || calculation.Quality != SipDigestQuality.None;
        if (clientNonceRequired && (calculation.ClientNonce == null
            || calculation.ClientNonce.Length == 0))
        {
            throw new System.ArgumentException(
                "The selected Digest mode requires a client nonce.",
                "ClientNonce");
        }
        if (calculation.ClientNonce != null)
        {
            ValidateAscii(calculation.ClientNonce, "ClientNonce");
        }
        if (calculation.Quality == SipDigestQuality.None)
        {
            if (calculation.NonceCount != -1)
            {
                throw new System.ArgumentOutOfRangeException("NonceCount");
            }
        }
        else if (calculation.NonceCount < 1
            || calculation.NonceCount > 4294967295L)
        {
            throw new System.ArgumentOutOfRangeException("NonceCount");
        }
        if (calculation.Quality == SipDigestQuality.AuthInt
            && calculation.RequestBody == null)
        {
            throw new System.ArgumentNullException("RequestBody");
        }
        if (calculation.Quality == SipDigestQuality.AuthInt
            && calculation.ResponseBody == null)
        {
            throw new System.ArgumentNullException("ResponseBody");
        }
        ValidateCredentialText(calculation.UserName, calculation.Utf8,
            "UserName");
        ValidateCredentialText(calculation.Realm, calculation.Utf8, "Realm");
        if (calculation.Utf8)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding(
                false, true);
            string password = encoding.GetString(calculation.Secret);
            password.Normalize(System.Text.NormalizationForm.FormC);
        }
    }

    static void ValidateAscii(string value, string parameterName)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                throw new System.ArgumentException(
                    "The Digest value must contain only ASCII characters.",
                    parameterName);
            }
            index = index + 1;
        }
    }

    static void ValidateCredentialText(
        string value, bool utf8, string parameterName)
    {
        if (utf8)
        {
            value.Normalize(System.Text.NormalizationForm.FormC);
            return;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 255)
            {
                throw new System.ArgumentException(
                    "A legacy Digest credential must use Latin-1 characters.",
                    parameterName);
            }
            index = index + 1;
        }
    }

    static byte[] EncodeCredentialText(
        string value, bool utf8, bool normalize)
    {
        if (utf8)
        {
            string text = normalize
                ? value.Normalize(System.Text.NormalizationForm.FormC) : value;
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding(
                false, true);
            return encoding.GetBytes(text);
        }
        return System.Text.Encoding.Latin1.GetBytes(value);
    }

    static byte[] EncodeSecret(byte[] secret, bool utf8)
    {
        if (!utf8)
        {
            return (byte[])secret.Clone();
        }
        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding(
            false, true);
        string password = encoding.GetString(secret);
        string normalized = password.Normalize(
            System.Text.NormalizationForm.FormC);
        return encoding.GetBytes(normalized);
    }

    static byte[] Concatenate(byte[][] values)
    {
        int length = 0;
        int index = 0;
        while (index < values.Length)
        {
            length = length + values[index].Length;
            index = index + 1;
        }
        byte[] result = new byte[length];
        int offset = 0;
        index = 0;
        while (index < values.Length)
        {
            System.Buffer.BlockCopy(values[index], 0, result, offset,
                values[index].Length);
            offset = offset + values[index].Length;
            index = index + 1;
        }
        return result;
    }
}
