using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace jmap;

internal class PushValidator
{
    internal static readonly BigInteger P256Prime = new BigInteger(global::System.Convert.FromHexString("FFFFFFFF00000001000000000000000000000000FFFFFFFFFFFFFFFFFFFFFFFF"), true, true);
    internal static readonly BigInteger P256B = new BigInteger(global::System.Convert.FromHexString("5AC635D8AA3A93E7B3EBBD55769886BC651D06B0CC53B0F63BCE3C3E27D2604B"), true, true);
    internal ValueValidator Values = new ValueValidator();

    internal List<string> ValidateCreate(PushSubscription? subscription)
    {
        List<string> invalid = new List<string>();
        if (subscription == null)
        {
            invalid.Add("");
            return invalid;
        }

        if (subscription.Id != null)
        {
            invalid.Add("id");
        }

        if (!IsValidDeviceClientId(subscription.DeviceClientId))
        {
            invalid.Add("deviceClientId");
        }

        if (!IsHttpsUrl(subscription.Url))
        {
            invalid.Add("url");
        }

        if (subscription.Keys != null)
        {
            if (!IsValidP256dh(subscription.Keys.P256dh))
            {
                invalid.Add("keys/p256dh");
            }

            if (!IsValidAuth(subscription.Keys.Auth))
            {
                invalid.Add("keys/auth");
            }
        }

        if (subscription.VerificationCode != null)
        {
            invalid.Add("verificationCode");
        }

        if (subscription.Expires != null && !Values.IsUtcDate(subscription.Expires))
        {
            invalid.Add("expires");
        }

        ValidateTypes(subscription.Types, invalid);
        return invalid;
    }

    internal List<string> ValidateCreatePolicy(PushSubscription? subscription, JmapRequestContext? context, IJmapPushUrlPolicy? policy)
    {
        List<string> invalid = ValidateCreate(subscription);
        if (subscription != null && IsHttpsUrl(subscription.Url) && !IsPushUrlAllowed(subscription.Url, context, policy))
        {
            AddInvalidOnce("url", invalid);
        }

        return invalid;
    }

    internal List<string> ValidateWritablePatch(JmapPatchObject? patch)
    {
        List<string> invalid = new List<string>();
        if (patch == null)
        {
            invalid.Add("");
            return invalid;
        }

        foreach (string path in patch.Values.Keys)
        {
            if (path != "verificationCode" && path != "expires" && path != "types")
            {
                invalid.Add(path);
            }
        }

        return invalid;
    }

    internal List<string> ValidateWritablePatchValues(JmapPatchObject? patch)
    {
        List<string> invalid = ValidateWritablePatch(patch);
        if (patch == null)
        {
            return invalid;
        }

        foreach (KeyValuePair<string, JsonElement> entry in patch.Values)
        {
            if (entry.Key == "verificationCode" && entry.Value.ValueKind != JsonValueKind.String)
            {
                AddInvalidOnce(entry.Key, invalid);
            }

            if (entry.Key == "expires" && entry.Value.ValueKind != JsonValueKind.Null)
            {
                if (entry.Value.ValueKind != JsonValueKind.String || !Values.IsUtcDate(entry.Value.GetString()))
                {
                    AddInvalidOnce(entry.Key, invalid);
                }
            }

            if (entry.Key == "types" && entry.Value.ValueKind != JsonValueKind.Null)
            {
                ValidatePatchTypes(entry.Value, invalid);
            }
        }

        return invalid;
    }

    internal bool IsBase64Url(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool upper = character >= 'A' && character <= 'Z';
            bool lower = character >= 'a' && character <= 'z';
            bool digit = character >= '0' && character <= '9';
            if (!upper && !lower && !digit && character != '-' && character != '_')
            {
                return false;
            }

            index = index + 1;
        }

        return value.Length % 4 != 1;
    }

    internal void ValidateTypes(List<string>? types, List<string> invalid)
    {
        if (types == null)
        {
            return;
        }

        if (types.Count > SecurityLimits.MaximumEventSourceTypes)
        {
            invalid.Add("types");
            return;
        }

        int index = 0;
        HashSet<string> seen = new HashSet<string>(global::System.StringComparer.Ordinal);
        while (index < types.Count)
        {
            string type = types[index];
            if (type == null || type.Length == 0 || Encoding.UTF8.GetByteCount(type) > 255 || !Values.IsValidUnicode(type) || !seen.Add(type))
            {
                invalid.Add("types/" + index);
            }

            index = index + 1;
        }
    }

    internal bool IsPushUrlAllowed(string? url, JmapRequestContext? context, IJmapPushUrlPolicy? policy)
    {
        if (!IsHttpsUrl(url) || context == null)
        {
            return false;
        }

        if (policy == null)
        {
            return IsDefaultPushDestinationAllowed(url);
        }

        string callbackUrl = url == null ? "" : url;
        return policy.IsPushUrlAllowed(callbackUrl, context);
    }

    internal bool IsDefaultPushDestinationAllowed(string? url)
    {
        if (!IsHttpsUrl(url) || url == null)
        {
            return false;
        }

        global::System.Uri uri = new global::System.Uri(url);
        string host = uri.DnsSafeHost;
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        global::System.UriHostNameType hostType = global::System.Uri.CheckHostName(host);
        if (hostType != global::System.UriHostNameType.IPv4 &&
            hostType != global::System.UriHostNameType.IPv6)
        {
            return true;
        }

        global::System.Net.IPAddress address = global::System.Net.IPAddress.Parse(host);
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (global::System.Net.IPAddress.IsLoopback(address) ||
            address.Equals(global::System.Net.IPAddress.Any) ||
            address.Equals(global::System.Net.IPAddress.IPv6Any) ||
            address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
        {
            return false;
        }

        byte[] bytes = address.GetAddressBytes();
        if (bytes.Length == 4)
        {
            return IsPublicIpv4(bytes);
        }

        return bytes.Length == 16 && (bytes[0] & 254) != 252;
    }

    internal bool IsPublicIpv4(byte[] bytes)
    {
        if (bytes[0] == 0 || bytes[0] == 10 || bytes[0] == 127 || bytes[0] >= 224 ||
            bytes[0] == 169 && bytes[1] == 254 ||
            bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31 ||
            bytes[0] == 192 && bytes[1] == 168 ||
            bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127 ||
            bytes[0] == 198 && (bytes[1] == 18 || bytes[1] == 19))
        {
            return false;
        }

        return true;
    }

    internal void ApplyExpiryPolicy(PushSubscription subscription, JmapRequestContext context, JmapServerOptions options, IJmapClock? clock)
    {
        if (context.HasCredentialExpiresUtc)
        {
            return;
        }

        DateTimeOffset now = clock == null ? DateTimeOffset.UtcNow : clock.GetCurrentUtcDateTime();
        int maximumSeconds = options.MaximumPushSubscriptionLifetimeSeconds;
        if (maximumSeconds < JmapProtocolLimits.MinimumPushSubscriptionMaximumLifetimeSeconds)
        {
            maximumSeconds = JmapProtocolLimits.MinimumPushSubscriptionMaximumLifetimeSeconds;
        }

        DateTimeOffset maximum = now.AddSeconds(maximumSeconds);
        if (subscription.Expires == null || ParseUtcDate(subscription.Expires) > maximum)
        {
            subscription.Expires = FormatUtcDate(maximum);
        }
    }

    internal StateChange? FilterStateChange(StateChange? source, List<string>? types)
    {
        if (source == null)
        {
            return null;
        }

        StateChange filtered = new StateChange();
        filtered.Type = source.Type;
        foreach (KeyValuePair<string, JsonElement> unknown in source.UnknownProperties)
        {
            filtered.UnknownProperties.Add(unknown.Key, unknown.Value.Clone());
        }

        HashSet<string>? selected = null;
        if (types != null)
        {
            selected = new HashSet<string>(types, global::System.StringComparer.Ordinal);
        }

        foreach (KeyValuePair<string, Dictionary<string, string>> account in source.Changed)
        {
            Dictionary<string, string> states = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> state in account.Value)
            {
                if (selected == null || selected.Contains(state.Key))
                {
                    states.Add(state.Key, state.Value);
                }
            }

            if (states.Count > 0)
            {
                filtered.Changed.Add(account.Key, states);
            }
        }

        if (filtered.Changed.Count == 0)
        {
            return null;
        }

        return filtered;
    }

    internal byte[]? DecodeBase64Url(string? value)
    {
        if (!IsBase64Url(value))
        {
            return null;
        }

        string encoded = value == null ? "" : value.Replace('-', '+').Replace('_', '/');
        int remainder = encoded.Length % 4;
        if (remainder == 2)
        {
            encoded = encoded + "==";
        }

        if (remainder == 3)
        {
            encoded = encoded + "=";
        }

        return global::System.Convert.FromBase64String(encoded);
    }

    internal bool IsValidP256dh(string? value)
    {
        byte[]? key = DecodeBase64Url(value);
        if (key == null || key.Length != 65 || key[0] != 4)
        {
            return false;
        }

        byte[] xBytes = new byte[32];
        byte[] yBytes = new byte[32];
        global::System.Array.Copy(key, 1, xBytes, 0, 32);
        global::System.Array.Copy(key, 33, yBytes, 0, 32);
        BigInteger x = new BigInteger(xBytes, true, true);
        BigInteger y = new BigInteger(yBytes, true, true);
        if (x >= P256Prime || y >= P256Prime)
        {
            return false;
        }

        BigInteger left = BigInteger.ModPow(y, 2, P256Prime);
        BigInteger right = (BigInteger.ModPow(x, 3, P256Prime) - (3 * x) + P256B) % P256Prime;
        if (right < 0)
        {
            right = right + P256Prime;
        }

        return left == right;
    }

    internal bool IsValidAuth(string? value)
    {
        byte[]? auth = DecodeBase64Url(value);
        return auth != null && auth.Length == 16;
    }

    internal bool IsHttpsUrl(string? value)
    {
        if (value == null || value.Length > SecurityLimits.MaximumPushUrlCharacters ||
            !value.StartsWith("https://", global::System.StringComparison.OrdinalIgnoreCase) ||
            !global::System.Uri.IsWellFormedUriString(value, global::System.UriKind.Absolute))
        {
            return false;
        }

        global::System.Uri uri = new global::System.Uri(value);
        return uri.Scheme == global::System.Uri.UriSchemeHttps && uri.Host.Length > 0 && uri.UserInfo.Length == 0 && uri.Fragment.Length == 0;
    }

    internal bool IsValidDeviceClientId(string? value)
    {
        if (value == null || value.Length == 0 || Encoding.UTF8.GetByteCount(value) > 255 || !Values.IsValidUnicode(value))
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (char.IsControl(value[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void ValidatePatchTypes(JsonElement value, List<string> invalid)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            AddInvalidOnce("types", invalid);
            return;
        }

        HashSet<string> seen = new HashSet<string>(global::System.StringComparer.Ordinal);
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                invalid.Add("types/" + index);
            }
            else
            {
                string? type = item.GetString();
                if (type == null || type.Length == 0 || Encoding.UTF8.GetByteCount(type) > 255 || !Values.IsValidUnicode(type) || !seen.Add(type))
                {
                    invalid.Add("types/" + index);
                }
            }

            index = index + 1;
        }
    }

    internal void AddInvalidOnce(string path, List<string> invalid)
    {
        if (!invalid.Contains(path))
        {
            invalid.Add(path);
        }
    }

    internal DateTimeOffset ParseUtcDate(string value)
    {
        bool leapSecond = value.Length > 18 && value[17] == '6' && value[18] == '0';
        string parseable = leapSecond ? value.Substring(0, 17) + "59" + value.Substring(19) : value;
        DateTimeOffset parsed = DateTimeOffset.Parse(parseable, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        return leapSecond ? parsed.AddSeconds(1) : parsed;
    }

    internal string FormatUtcDate(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
    }
}
