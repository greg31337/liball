using System;
using System.Security.Cryptography;
using System.Text;

namespace jmap;

internal class PushSubscriptionSecurityState
{
    internal PushSubscription? Subscription;
    internal string? CredentialId;
    internal DateTimeOffset CredentialExpiresUtc;
    internal bool HasCredentialExpiresUtc;
    internal bool CredentialRevoked;
    internal bool IsVerified;
    internal byte[]? UrlBytes;
    internal byte[]? P256dh;
    internal byte[]? Auth;
    internal byte[]? VerificationCode;
}

internal class PushSubscriptionSecurity
{
    internal PushValidator Validator = new PushValidator();

    internal string GenerateVerificationCode()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        string verificationCode = EncodeBase64Url(bytes);
        CryptographicOperations.ZeroMemory(bytes);
        return verificationCode;
    }

    internal PushSubscriptionSecurityState Capture(PushSubscription subscription, JmapRequestContext context, string verificationCode)
    {
        PushSubscriptionSecurityState state = new PushSubscriptionSecurityState();
        state.Subscription = subscription;
        state.CredentialId = context.CredentialId;
        state.CredentialExpiresUtc = context.CredentialExpiresUtc;
        state.HasCredentialExpiresUtc = context.HasCredentialExpiresUtc;
        state.CredentialRevoked = context.CredentialIsRevoked;
        if (subscription.Url != null)
        {
            state.UrlBytes = Encoding.UTF8.GetBytes(subscription.Url);
        }

        if (subscription.Keys != null)
        {
            state.P256dh = Validator.DecodeBase64Url(subscription.Keys.P256dh);
            state.Auth = Validator.DecodeBase64Url(subscription.Keys.Auth);
        }

        state.VerificationCode = Validator.DecodeBase64Url(verificationCode);
        return state;
    }

    internal bool IsAccessible(PushSubscriptionSecurityState? state, JmapRequestContext? context, IJmapClock? clock)
    {
        if (state == null || context == null || state.CredentialId == null || context.CredentialId == null || state.CredentialId != context.CredentialId)
        {
            return false;
        }

        if (state.CredentialRevoked || context.CredentialIsRevoked)
        {
            Erase(state);
            return false;
        }

        DateTimeOffset now = clock == null ? DateTimeOffset.UtcNow : clock.GetCurrentUtcDateTime();
        if (state.HasCredentialExpiresUtc && state.CredentialExpiresUtc <= now)
        {
            Erase(state);
            return false;
        }

        if (context.HasCredentialExpiresUtc && context.CredentialExpiresUtc <= now)
        {
            Erase(state);
            return false;
        }

        return true;
    }

    internal bool VerifyCode(PushSubscriptionSecurityState? state, string? verificationCode)
    {
        if (state == null || state.VerificationCode == null)
        {
            return false;
        }

        byte[]? supplied = Validator.DecodeBase64Url(verificationCode);
        if (supplied == null)
        {
            return false;
        }

        if (supplied.Length != state.VerificationCode.Length)
        {
            CryptographicOperations.ZeroMemory(supplied);
            return false;
        }

        bool matches = CryptographicOperations.FixedTimeEquals(supplied, state.VerificationCode);
        CryptographicOperations.ZeroMemory(supplied);
        return matches;
    }

    internal bool ConfirmVerification(PushSubscriptionSecurityState? state, string? verificationCode)
    {
        if (!VerifyCode(state, verificationCode) || state == null)
        {
            return false;
        }

        state.IsVerified = true;
        return true;
    }

    internal bool CanReceiveNotification(PushSubscriptionSecurityState? state, JmapRequestContext? context, IJmapClock? clock)
    {
        return state != null && state.IsVerified && IsAccessible(state, context, clock);
    }

    internal bool RevokeCredential(PushSubscriptionSecurityState? state, string? credentialId)
    {
        if (state == null || state.CredentialId == null || credentialId == null || state.CredentialId != credentialId)
        {
            return false;
        }

        Erase(state);
        return true;
    }

    internal void Erase(PushSubscriptionSecurityState state)
    {
        ZeroBytes(state.UrlBytes);
        ZeroBytes(state.P256dh);
        ZeroBytes(state.Auth);
        ZeroBytes(state.VerificationCode);
        state.UrlBytes = null;
        state.P256dh = null;
        state.Auth = null;
        state.VerificationCode = null;
        state.CredentialId = null;
        state.CredentialRevoked = true;
        state.IsVerified = false;
        if (state.Subscription != null)
        {
            state.Subscription.Url = null;
            state.Subscription.VerificationCode = null;
            if (state.Subscription.Keys != null)
            {
                state.Subscription.Keys.P256dh = null;
                state.Subscription.Keys.Auth = null;
                state.Subscription.Keys = null;
            }
        }

        state.Subscription = null;
    }

    internal string EncodeBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    internal void ZeroBytes(byte[]? value)
    {
        if (value != null)
        {
            CryptographicOperations.ZeroMemory(value);
        }
    }
}
