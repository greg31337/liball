using System.Text.Json;

namespace jmap.Tests;

public class PushSubscriptionSecurityTests
{
    [Test]
    public void CreateValidationChecksDeviceUrlKeysAndUniqueExtensibleTypes()
    {
        PushValidator validator = new PushValidator();
        PushSubscription valid = ValidSubscription();
        valid.Types = new List<string> { JmapDataTypes.Email, "https://vendor.example/Todo" };
        byte[] invalidPoint = new byte[65];
        invalidPoint[0] = 4;
        PushSubscription invalid = new PushSubscription
        {
            DeviceClientId = "device\nidentifier",
            Url = "https://user@push.example.test/events#fragment",
            Keys = new PushSubscriptionKeys
            {
                P256dh = Base64Url(invalidPoint),
                Auth = Base64Url(new byte[15])
            },
            Types = new List<string> { JmapDataTypes.Email, JmapDataTypes.Email, "" }
        };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.ValidateCreate(valid), Is.Empty);
            Assert.That(
                validator.ValidateCreate(invalid),
                Is.EqualTo(new[] { "deviceClientId", "url", "keys/p256dh", "keys/auth", "types/1", "types/2" }));
            Assert.That(validator.IsBase64Url("a"), Is.False);
            Assert.That(validator.IsBase64Url("YWJjZA"), Is.True);
        }
    }

    [Test]
    public void CreateValidationConsultsPushUrlPolicyAfterHttpsValidation()
    {
        PushValidator validator = new PushValidator();
        PushSubscription subscription = ValidSubscription();
        JmapRequestContext context = new JmapRequestContext { CredentialId = "credential-1" };
        RecordingUrlPolicy policy = new RecordingUrlPolicy();
        policy.Allowed = false;

        List<string> blocked = validator.ValidateCreatePolicy(subscription, context, policy);
        subscription.Url = "http://push.example.test/events";
        List<string> insecure = validator.ValidateCreatePolicy(subscription, context, policy);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(blocked, Is.EqualTo(new[] { "url" }));
            Assert.That(policy.Calls, Is.EqualTo(1));
            Assert.That(policy.LastContext, Is.SameAs(context));
            Assert.That(insecure, Is.EqualTo(new[] { "url" }));
        }
    }

    [Test]
    public void ExpiryPolicyDefaultsAndCapsOnlyNonExpiringCredentials()
    {
        PushValidator validator = new PushValidator();
        FixedClock clock = new FixedClock();
        JmapServerOptions options = new JmapServerOptions();
        JmapRequestContext durable = new JmapRequestContext();
        PushSubscription missing = ValidSubscription();
        PushSubscription excessive = ValidSubscription();
        excessive.Expires = "2030-01-01T00:00:00Z";
        PushSubscription shortLived = ValidSubscription();
        shortLived.Expires = "2026-09-11T12:00:00Z";
        JmapRequestContext timed = new JmapRequestContext
        {
            HasCredentialExpiresUtc = true,
            CredentialExpiresUtc = clock.Now.AddHours(1)
        };
        PushSubscription timedSubscription = ValidSubscription();

        validator.ApplyExpiryPolicy(missing, durable, options, clock);
        validator.ApplyExpiryPolicy(excessive, durable, options, clock);
        validator.ApplyExpiryPolicy(shortLived, durable, options, clock);
        validator.ApplyExpiryPolicy(timedSubscription, timed, options, clock);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missing.Expires, Is.EqualTo("2026-09-17T12:00:00Z"));
            Assert.That(excessive.Expires, Is.EqualTo("2026-09-17T12:00:00Z"));
            Assert.That(shortLived.Expires, Is.EqualTo("2026-09-11T12:00:00Z"));
            Assert.That(timedSubscription.Expires, Is.Null);
        }
    }

    [Test]
    public void WritablePatchValidationRejectsImmutableAndMalformedValues()
    {
        PushValidator validator = new PushValidator();
        JmapPatchObject patch = Patch(
            ("url", "\"https://other.example\""),
            ("keys/auth", "\"secret\""),
            ("deviceClientId", "\"other\""),
            ("expires", "\"tomorrow\""),
            ("verificationCode", "null"),
            ("types", "[\"Email\",\"Email\",3]"));

        Assert.That(
            validator.ValidateWritablePatchValues(patch),
            Is.EqualTo(new[] { "url", "keys/auth", "deviceClientId", "expires", "verificationCode", "types/1", "types/2" }));
    }

    [Test]
    public void TypeFilteringPreservesOnlyExactSelectedStatesAndMatchingAccounts()
    {
        PushValidator validator = new PushValidator();
        StateChange change = new StateChange();
        change.Changed["account-1"] = new Dictionary<string, string>
        {
            [JmapDataTypes.Email] = "email-state",
            [JmapDataTypes.Mailbox] = "mailbox-state"
        };
        change.Changed["account-2"] = new Dictionary<string, string>
        {
            [JmapDataTypes.Thread] = "thread-state"
        };

        StateChange? filtered = validator.FilterStateChange(change, new List<string> { JmapDataTypes.Email, "https://vendor.example/Todo" });
        StateChange? unmatched = validator.FilterStateChange(change, new List<string> { "email" });
        StateChange? all = validator.FilterStateChange(change, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(filtered, Is.Not.Null);
            Assert.That(filtered!.Changed.Keys, Is.EqualTo(new[] { "account-1" }));
            Assert.That(filtered.Changed["account-1"], Is.EqualTo(new Dictionary<string, string> { [JmapDataTypes.Email] = "email-state" }));
            Assert.That(unmatched, Is.Null);
            Assert.That(all, Is.Not.SameAs(change));
            Assert.That(all!.Changed.Count, Is.EqualTo(2));
            Assert.That(change.Changed["account-1"].Count, Is.EqualTo(2));
        }
    }

    [Test]
    public void VerificationCodesHaveHighEntropyAndUseFixedTimeValidation()
    {
        PushSubscriptionSecurity security = new PushSubscriptionSecurity();
        string first = security.GenerateVerificationCode();
        string second = security.GenerateVerificationCode();
        PushSubscription subscription = ValidSubscription();
        JmapRequestContext context = new JmapRequestContext { CredentialId = "credential-1" };
        PushSubscriptionSecurityState state = security.Capture(subscription, context, first);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Has.Length.EqualTo(43));
            Assert.That(first, Does.Match("^[A-Za-z0-9_-]+$"));
            Assert.That(Convert.FromBase64String(PadBase64(first.Replace('-', '+').Replace('_', '/'))), Has.Length.EqualTo(32));
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(security.VerifyCode(state, first), Is.True);
            Assert.That(security.VerifyCode(state, second), Is.False);
            Assert.That(security.VerifyCode(state, "invalid="), Is.False);
        }
    }

    [Test]
    public void CredentialOwnershipRevocationAndExpiryControlAccess()
    {
        PushSubscriptionSecurity security = new PushSubscriptionSecurity();
        FixedClock clock = new FixedClock();
        JmapRequestContext owner = new JmapRequestContext
        {
            CredentialId = "credential-1",
            HasCredentialExpiresUtc = true,
            CredentialExpiresUtc = clock.Now.AddMinutes(5)
        };
        PushSubscriptionSecurityState state = security.Capture(ValidSubscription(), owner, security.GenerateVerificationCode());
        JmapRequestContext other = new JmapRequestContext { CredentialId = "credential-2" };

        bool ownerBeforeExpiry = security.IsAccessible(state, owner, clock);
        bool otherCredential = security.IsAccessible(state, other, clock);
        clock.Now = clock.Now.AddMinutes(6);
        bool ownerAfterExpiry = security.IsAccessible(state, owner, clock);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ownerBeforeExpiry, Is.True);
            Assert.That(otherCredential, Is.False);
            Assert.That(ownerAfterExpiry, Is.False);
            Assert.That(state.CredentialRevoked, Is.True);
            Assert.That(state.Subscription, Is.Null);
        }
    }

    [Test]
    public void RevocationSecurelyErasesCapturedSecretsAndPrivateModelFields()
    {
        PushSubscriptionSecurity security = new PushSubscriptionSecurity();
        PushSubscription subscription = ValidSubscription();
        PushSubscriptionKeys keys = subscription.Keys!;
        JmapRequestContext context = new JmapRequestContext { CredentialId = "credential-1" };
        PushSubscriptionSecurityState state = security.Capture(subscription, context, security.GenerateVerificationCode());
        byte[] url = state.UrlBytes!;
        byte[] p256dh = state.P256dh!;
        byte[] auth = state.Auth!;
        byte[] code = state.VerificationCode!;

        bool wrongCredential = security.RevokeCredential(state, "credential-2");
        bool ownerCredential = security.RevokeCredential(state, "credential-1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(wrongCredential, Is.False);
            Assert.That(ownerCredential, Is.True);
            Assert.That(url, Has.All.EqualTo(0));
            Assert.That(p256dh, Has.All.EqualTo(0));
            Assert.That(auth, Has.All.EqualTo(0));
            Assert.That(code, Has.All.EqualTo(0));
            Assert.That(subscription.Url, Is.Null);
            Assert.That(subscription.Keys, Is.Null);
            Assert.That(keys.P256dh, Is.Null);
            Assert.That(keys.Auth, Is.Null);
            Assert.That(state.CredentialRevoked, Is.True);
            Assert.That(state.CredentialId, Is.Null);
        }
    }

    private static PushSubscription ValidSubscription()
    {
        return new PushSubscription
        {
            DeviceClientId = "app.example/device-hash",
            Url = "https://push.example.test/events",
            Keys = new PushSubscriptionKeys
            {
                P256dh = Base64Url(Convert.FromHexString("046B17D1F2E12C4247F8BCE6E563A440F277037D812DEB33A0F4A13945D898C2964FE342E2FE1A7F9B8EE7EB4A7C0F9E162BCE33576B315ECECBB6406837BF51F5")),
                Auth = Base64Url(new byte[16])
            }
        };
    }

    private static JmapPatchObject Patch(params (string Path, string Json)[] values)
    {
        JmapPatchObject patch = new JmapPatchObject();
        foreach ((string path, string json) in values)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            patch.Values[path] = document.RootElement.Clone();
        }

        return patch;
    }

    private static string Base64Url(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string PadBase64(string value)
    {
        int remainder = value.Length % 4;
        if (remainder == 2)
        {
            return value + "==";
        }

        if (remainder == 3)
        {
            return value + "=";
        }

        return value;
    }

    private class FixedClock : IJmapClock
    {
        public DateTimeOffset Now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

        public DateTimeOffset GetCurrentUtcDateTime()
        {
            return Now;
        }
    }

    private class RecordingUrlPolicy : IJmapPushUrlPolicy
    {
        public bool Allowed;
        public int Calls;
        public JmapRequestContext? LastContext;

        public bool IsPushUrlAllowed(string url, JmapRequestContext context)
        {
            Calls = Calls + 1;
            LastContext = context;
            return Allowed;
        }
    }
}
