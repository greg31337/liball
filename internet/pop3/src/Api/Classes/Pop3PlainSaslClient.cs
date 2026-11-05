namespace pop3;

/// <summary>Provides a single-use RFC 4616 SASL PLAIN client exchange.</summary>
public class Pop3PlainSaslClient : IPop3SaslClient
{
    internal byte[] AuthorizationIdentityBytes;
    internal byte[] AuthenticationIdentityBytes;
    internal byte[] PasswordBytes;
    byte[]? ActiveResponse;
    string AuthenticationIdentity;
    bool IsUsed;

    /// <summary>Creates a PLAIN exchange from credentials and an explicit transport-security policy.</summary>
    /// <param name="credentials">The authentication, authorization, and password values to encode.</param>
    /// <param name="isTransportProtected">Whether the transport already provides adequate confidentiality.</param>
    /// <param name="allowUnprotectedCredentials">Whether clear credentials are explicitly permitted without transport protection.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>credentials</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when a credential field is empty where prohibited, contains NUL, or is not valid Unicode.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when an unprotected transport is used without explicit permission.</exception>
    public Pop3PlainSaslClient(
        Pop3Credentials credentials,
        bool isTransportProtected,
        bool allowUnprotectedCredentials)
    {
        if (credentials == null)
        {
            throw new System.ArgumentNullException("credentials");
        }
        if (!isTransportProtected && !allowUnprotectedCredentials)
        {
            throw new System.InvalidOperationException(
                "SASL PLAIN credentials require a protected transport unless unprotected use is explicitly permitted.");
        }
        ValidateRequiredCredential(credentials.Username, "Username");
        ValidateRequiredCredential(credentials.Password, "Password");
        ValidateOptionalCredential(credentials.AuthorizationIdentity, "AuthorizationIdentity");

        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding(false, true);
        if (credentials.AuthorizationIdentity == null || credentials.AuthorizationIdentity.Length == 0)
        {
            AuthorizationIdentityBytes = new byte[0];
        }
        else
        {
            AuthorizationIdentityBytes = encoding.GetBytes(credentials.AuthorizationIdentity);
        }
        AuthenticationIdentityBytes = encoding.GetBytes(credentials.Username);
        PasswordBytes = encoding.GetBytes(credentials.Password);
        AuthenticationIdentity = credentials.Username;
    }

    /// <summary>Returns the registered PLAIN mechanism name.</summary>
    /// <returns><c>PLAIN</c>.</returns>
    public string GetMechanism()
    {
        return "PLAIN";
    }

    /// <summary>Builds the single RFC 4616 initial response.</summary>
    /// <returns>A successful client step carrying the UTF-8 PLAIN credential message.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when this single-use exchange was already started or cleared.</exception>
    public Pop3SaslStep BeginAuthentication()
    {
        if (IsUsed)
        {
            throw new System.InvalidOperationException("A SASL PLAIN client exchange can only be started once.");
        }

        byte[] response = new byte[
            AuthorizationIdentityBytes.Length +
            AuthenticationIdentityBytes.Length +
            PasswordBytes.Length +
            2];
        int writeIndex = 0;
        System.Array.Copy(
            AuthorizationIdentityBytes,
            0,
            response,
            writeIndex,
            AuthorizationIdentityBytes.Length);
        writeIndex += AuthorizationIdentityBytes.Length;
        response[writeIndex] = 0;
        writeIndex++;
        System.Array.Copy(
            AuthenticationIdentityBytes,
            0,
            response,
            writeIndex,
            AuthenticationIdentityBytes.Length);
        writeIndex += AuthenticationIdentityBytes.Length;
        response[writeIndex] = 0;
        writeIndex++;
        System.Array.Copy(PasswordBytes, 0, response, writeIndex, PasswordBytes.Length);

        ClearCredentialBytes();
        ActiveResponse = response;
        IsUsed = true;

        Pop3SaslStep step = new Pop3SaslStep();
        step.Status = Pop3SaslStepStatus.Success;
        step.Data = response;
        step.HasData = true;
        step.AuthenticatedIdentity = AuthenticationIdentity;
        return step;
    }

    /// <summary>Rejects an unexpected challenge because PLAIN has only one client message.</summary>
    /// <param name="challenge">The unexpected decoded server challenge.</param>
    /// <returns>A rejected step with no response data.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>challenge</c> is <c>null</c>.</exception>
    public Pop3SaslStep ContinueAuthentication(byte[] challenge)
    {
        if (challenge == null)
        {
            throw new System.ArgumentNullException("challenge");
        }

        ClearSecrets();
        IsUsed = true;
        Pop3SaslStep step = new Pop3SaslStep();
        step.Status = Pop3SaslStepStatus.Rejected;
        return step;
    }

    /// <summary>Cancels the single-use exchange and clears mutable credential buffers.</summary>
    public void CancelAuthentication()
    {
        ClearSecrets();
        IsUsed = true;
    }

    /// <summary>Returns the unchanged transport because PLAIN negotiates no security layer.</summary>
    /// <param name="stream">The existing transport stream.</param>
    /// <returns>The supplied <c>stream</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>stream</c> is <c>null</c>.</exception>
    public System.IO.Stream ApplySecurityLayer(System.IO.Stream stream)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        ClearSecrets();
        return stream;
    }

    internal void ClearSecrets()
    {
        ClearCredentialBytes();
        if (ActiveResponse != null)
        {
            System.Array.Clear(ActiveResponse, 0, ActiveResponse.Length);
            ActiveResponse = null;
        }
    }

    void ClearCredentialBytes()
    {
        System.Array.Clear(AuthorizationIdentityBytes, 0, AuthorizationIdentityBytes.Length);
        System.Array.Clear(AuthenticationIdentityBytes, 0, AuthenticationIdentityBytes.Length);
        System.Array.Clear(PasswordBytes, 0, PasswordBytes.Length);
        AuthorizationIdentityBytes = new byte[0];
        AuthenticationIdentityBytes = new byte[0];
        PasswordBytes = new byte[0];
    }

    void ValidateRequiredCredential(string value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentException("A SASL PLAIN " + fieldName + " value cannot be null.", "credentials");
        }
        if (value.Length == 0)
        {
            throw new System.ArgumentException("A SASL PLAIN " + fieldName + " value cannot be empty.", "credentials");
        }
        ValidateCredentialText(value, fieldName);
    }

    void ValidateOptionalCredential(string? value, string fieldName)
    {
        if (value == null || value.Length == 0)
        {
            return;
        }
        ValidateCredentialText(value, fieldName);
    }

    void ValidateCredentialText(string value, string fieldName)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == 0)
            {
                throw new System.ArgumentException(
                    "A SASL PLAIN " + fieldName + " value cannot contain NUL.",
                    "credentials");
            }
            if (character >= 55296 && character <= 56319)
            {
                if (index + 1 >= value.Length || value[index + 1] < 56320 || value[index + 1] > 57343)
                {
                    throw new System.ArgumentException(
                        "A SASL PLAIN " + fieldName + " value must contain valid Unicode.",
                        "credentials");
                }
                index += 2;
            }
            else if (character >= 56320 && character <= 57343)
            {
                throw new System.ArgumentException(
                    "A SASL PLAIN " + fieldName + " value must contain valid Unicode.",
                    "credentials");
            }
            else
            {
                index++;
            }
        }
    }
}
