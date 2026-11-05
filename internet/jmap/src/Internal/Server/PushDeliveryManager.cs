using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace jmap;

internal class PushDeliveryTarget
{
    internal string? Id;
    internal List<string>? Types;
    internal PushSubscriptionSecurityState? SecurityState;
    internal JmapRequestContext? Context;
    internal StateChange? Pending;
    internal long PendingVersion;
    internal DateTimeOffset BackoffUntilUtc;
    internal bool HasBackoff;
    internal int BackoffSeconds;
}

internal class PushDeliveryWork
{
    internal PushDeliveryTarget? Target;
    internal StateChange? StateChange;
    internal long Version;
}

internal class PushHttpResult
{
    internal int StatusCode;
    internal int RetryAfterSeconds;
}

internal class PushDeliveryManager : IPushVerificationSender
{
    internal HttpClient Client;
    internal JmapServerOptions Options;
    internal IJmapClock? Clock;
    internal PushSubscriptionSecurity Security = new PushSubscriptionSecurity();
    internal PushValidator Validator = new PushValidator();
    internal WebPushEncryptor Encryptor = new WebPushEncryptor();
    internal JmapJsonReader Reader = new JmapJsonReader(JmapProtocolLimits.SuggestedMinimumMaxSizeRequest, 64);
    internal BlobPushJsonCodec Codec;
    internal Dictionary<string, PushDeliveryTarget> Targets = new Dictionary<string, PushDeliveryTarget>(StringComparer.Ordinal);

    internal PushDeliveryManager(HttpClient client, JmapServerOptions options, IJmapClock? clock)
    {
        if (client == null)
        {
            throw new ArgumentNullException("client");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        Client = client;
        Options = options;
        Clock = clock;
        Codec = new BlobPushJsonCodec(Reader);
    }

    public Task SendPushVerificationAsync(
        PushSubscription subscription,
        PushVerification verification,
        JmapRequestContext context,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => SendPushVerification(subscription, verification, cancellationToken), cancellationToken);
    }

    internal void SendPushVerification(PushSubscription subscription, PushVerification verification, CancellationToken cancellationToken)
    {
        if (subscription == null || verification == null || !Validator.IsHttpsUrl(subscription.Url) || !ValuesAreVerification(verification))
        {
            throw new ArgumentException("A valid push subscription and verification are required.", "subscription");
        }

        byte[]? publicKey = null;
        byte[]? authenticationSecret = null;
        if (subscription.Keys != null)
        {
            publicKey = Validator.DecodeBase64Url(subscription.Keys.P256dh);
            authenticationSecret = Validator.DecodeBase64Url(subscription.Keys.Auth);
        }

        byte[] payload = EncodeVerification(verification);
        string id = verification.PushSubscriptionId == null ? "" : verification.PushSubscriptionId;
        string url = subscription.Url == null ? "" : subscription.Url;
        PostPayload(id, url, publicKey, authenticationSecret, payload, cancellationToken);
        CryptographicOperations.ZeroMemory(payload);
    }

    internal void RegisterTarget(string id, PushSubscriptionSecurityState state, JmapRequestContext context)
    {
        if (!ValuesId(id) || state == null || context == null || state.UrlBytes == null)
        {
            throw new ArgumentException("A valid credential-owned push delivery target is required.", "state");
        }

        PushDeliveryTarget target = new PushDeliveryTarget();
        target.Id = id;
        target.SecurityState = state;
        target.Context = context;
        if (state.Subscription != null && state.Subscription.Types != null)
        {
            target.Types = new List<string>(state.Subscription.Types);
        }

        lock (Targets)
        {
            if (Targets.ContainsKey(id))
            {
                PushDeliveryTarget previous = Targets[id];
                if (previous.SecurityState != null && previous.SecurityState != state)
                {
                    Security.Erase(previous.SecurityState);
                }

                Targets[id] = target;
            }
            else
            {
                Targets.Add(id, target);
            }
        }
    }

    internal bool RemoveTarget(string? id)
    {
        if (id == null)
        {
            return false;
        }

        lock (Targets)
        {
            if (!Targets.ContainsKey(id))
            {
                return false;
            }

            PushDeliveryTarget target = Targets[id];
            if (target.SecurityState != null)
            {
                Security.Erase(target.SecurityState);
            }

            target.SecurityState = null;
            target.Context = null;
            target.Types = null;
            target.Pending = null;
            Targets.Remove(id);
            return true;
        }
    }

    internal void QueuePublication(JmapStatePublication? publication)
    {
        ValidatePublication(publication);
        if (publication == null)
        {
            return;
        }

        lock (Targets)
        {
            foreach (PushDeliveryTarget target in Targets.Values)
            {
                if (target.SecurityState != null && Security.CanReceiveNotification(target.SecurityState, target.Context, Clock))
                {
                    List<string>? targetTypes = CurrentTypes(target);
                    StateChange? filtered = FilterPublication(publication, targetTypes);
                    if (filtered != null)
                    {
                        Coalesce(target, filtered);
                    }
                }
            }
        }
    }

    internal Task FlushAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => Flush(cancellationToken), cancellationToken);
    }

    internal void Flush(CancellationToken cancellationToken)
    {
        List<PushDeliveryWork> works = BuildWork();
        foreach (PushDeliveryWork work in works)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PushHttpResult result = DeliverWork(work, cancellationToken);
            CompleteWork(work, result);
        }
    }

    internal List<PushDeliveryWork> BuildWork()
    {
        List<PushDeliveryWork> works = new List<PushDeliveryWork>();
        DateTimeOffset now = CurrentTime();
        lock (Targets)
        {
            foreach (PushDeliveryTarget target in Targets.Values)
            {
                if (target.Pending != null && target.SecurityState != null && Security.CanReceiveNotification(target.SecurityState, target.Context, Clock))
                {
                    if (!target.HasBackoff || target.BackoffUntilUtc <= now)
                    {
                        PushDeliveryWork work = new PushDeliveryWork();
                        work.Target = target;
                        work.StateChange = CloneStateChange(target.Pending);
                        work.Version = target.PendingVersion;
                        works.Add(work);
                    }
                }
            }
        }

        return works;
    }

    internal PushHttpResult DeliverWork(PushDeliveryWork work, CancellationToken cancellationToken)
    {
        if (work.Target == null || work.StateChange == null || work.Target.Id == null || work.Target.SecurityState == null)
        {
            throw new InvalidOperationException("A complete push delivery work item is required.");
        }

        if (!Security.CanReceiveNotification(work.Target.SecurityState, work.Target.Context, Clock))
        {
            PushHttpResult skipped = new PushHttpResult();
            skipped.StatusCode = 204;
            return skipped;
        }

        if (work.Target.SecurityState.UrlBytes == null)
        {
            throw new InvalidOperationException("A push delivery target URL is required.");
        }

        byte[] payload = EncodeStateChange(work.StateChange);
        string url = Encoding.UTF8.GetString(work.Target.SecurityState.UrlBytes);
        PushHttpResult result = PostPayload(
            work.Target.Id,
            url,
            work.Target.SecurityState.P256dh,
            work.Target.SecurityState.Auth,
            payload,
            cancellationToken);
        CryptographicOperations.ZeroMemory(payload);
        return result;
    }

    internal void CompleteWork(PushDeliveryWork work, PushHttpResult result)
    {
        if (work.Target == null)
        {
            return;
        }

        lock (Targets)
        {
            PushDeliveryTarget target = work.Target;
            if (result.StatusCode >= 200 && result.StatusCode <= 299)
            {
                target.HasBackoff = false;
                target.BackoffSeconds = 0;
                if (target.PendingVersion == work.Version)
                {
                    target.Pending = null;
                }
            }
            else
            {
                int delay = result.StatusCode == 429 ? result.RetryAfterSeconds : 0;
                if (delay <= 0)
                {
                    delay = target.BackoffSeconds <= 0 ? 60 : target.BackoffSeconds * 2;
                }

                if (delay > 3600)
                {
                    delay = 3600;
                }

                target.BackoffSeconds = delay;
                target.BackoffUntilUtc = CurrentTime().AddSeconds(delay);
                target.HasBackoff = true;
            }
        }
    }

    internal PushHttpResult PostPayload(
        string id,
        string url,
        byte[]? publicKey,
        byte[]? authenticationSecret,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        byte[] body = payload;
        bool encrypted = publicKey != null || authenticationSecret != null;
        if (encrypted)
        {
            if (publicKey == null || authenticationSecret == null)
            {
                throw new CryptographicException("Both Web Push encryption keys are required.");
            }

            body = Encryptor.Encrypt(payload, publicKey, authenticationSecret);
        }

        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("TTL", EffectiveTimeToLive().ToString(CultureInfo.InvariantCulture));
        request.Headers.TryAddWithoutValidation("Urgency", "normal");
        request.Headers.TryAddWithoutValidation("Topic", CreateTopic(id));
        ByteArrayContent content = new ByteArrayContent(body);
        MediaTypeHeaderValue contentType = new MediaTypeHeaderValue(JmapMediaTypes.Json);
        contentType.CharSet = "utf-8";
        content.Headers.ContentType = contentType;
        if (encrypted)
        {
            content.Headers.ContentEncoding.Add("aes128gcm");
        }

        request.Content = content;
        using HttpResponseMessage response = Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).GetAwaiter().GetResult();
        PushHttpResult result = new PushHttpResult();
        result.StatusCode = (int)response.StatusCode;
        result.RetryAfterSeconds = ReadRetryAfter(response);
        if (encrypted)
        {
            CryptographicOperations.ZeroMemory(body);
        }

        return result;
    }

    internal int ReadRetryAfter(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter == null)
        {
            return 0;
        }

        if (response.Headers.RetryAfter.Delta.HasValue)
        {
            double seconds = response.Headers.RetryAfter.Delta.GetValueOrDefault().TotalSeconds;
            if (seconds <= 0)
            {
                return 1;
            }

            if (seconds > 3600)
            {
                return 3600;
            }

            return (int)seconds;
        }

        if (response.Headers.RetryAfter.Date.HasValue)
        {
            double seconds = (response.Headers.RetryAfter.Date.GetValueOrDefault() - CurrentTime()).TotalSeconds;
            if (seconds <= 0)
            {
                return 1;
            }

            if (seconds > 3600)
            {
                return 3600;
            }

            return (int)seconds;
        }

        return 0;
    }

    internal StateChange? FilterPublication(JmapStatePublication publication, List<string>? targetTypes)
    {
        StateChange filtered = new StateChange();
        filtered.Type = publication.StateChange.Type;
        foreach (KeyValuePair<string, JsonElement> unknown in publication.StateChange.UnknownProperties)
        {
            filtered.UnknownProperties.Add(unknown.Key, unknown.Value.Clone());
        }

        foreach (KeyValuePair<string, Dictionary<string, string>> account in publication.StateChange.Changed)
        {
            Dictionary<string, string> states = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> state in account.Value)
            {
                bool include = publication.HasNewEmailDelivery || state.Key != JmapDataTypes.EmailDelivery;
                if (include && publication.Types != null && !publication.Types.Contains(state.Key))
                {
                    include = false;
                }

                if (include && targetTypes != null && !targetTypes.Contains(state.Key))
                {
                    include = false;
                }

                if (include)
                {
                    states.Add(state.Key, state.Value);
                }
            }

            if (states.Count > 0)
            {
                filtered.Changed.Add(account.Key, states);
            }
        }

        return filtered.Changed.Count == 0 ? null : filtered;
    }

    internal void Coalesce(PushDeliveryTarget target, StateChange change)
    {
        if (target.Pending == null)
        {
            target.Pending = CloneStateChange(change);
        }
        else
        {
            target.Pending.Type = change.Type;
            foreach (KeyValuePair<string, JsonElement> unknown in change.UnknownProperties)
            {
                target.Pending.UnknownProperties[unknown.Key] = unknown.Value.Clone();
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> account in change.Changed)
            {
                if (!target.Pending.Changed.ContainsKey(account.Key))
                {
                    target.Pending.Changed.Add(account.Key, new Dictionary<string, string>());
                }

                foreach (KeyValuePair<string, string> state in account.Value)
                {
                    target.Pending.Changed[account.Key][state.Key] = state.Value;
                }
            }
        }

        target.PendingVersion = target.PendingVersion + 1;
    }

    internal StateChange CloneStateChange(StateChange source)
    {
        StateChange clone = new StateChange();
        clone.Type = source.Type;
        foreach (KeyValuePair<string, Dictionary<string, string>> account in source.Changed)
        {
            clone.Changed.Add(account.Key, new Dictionary<string, string>(account.Value, StringComparer.Ordinal));
        }

        foreach (KeyValuePair<string, JsonElement> unknown in source.UnknownProperties)
        {
            clone.UnknownProperties.Add(unknown.Key, unknown.Value.Clone());
        }

        return clone;
    }

    internal List<string>? CurrentTypes(PushDeliveryTarget target)
    {
        if (target.SecurityState != null && target.SecurityState.Subscription != null)
        {
            return target.SecurityState.Subscription.Types;
        }

        return target.Types;
    }

    internal void ValidatePublication(JmapStatePublication? publication)
    {
        if (publication == null || publication.StateChange == null || publication.StateChange.Type != "StateChange")
        {
            throw new ArgumentException("A StateChange publication is required.", "publication");
        }

        if (publication.Types != null)
        {
            List<string> invalidTypes = new List<string>();
            Validator.ValidateTypes(publication.Types, invalidTypes);
            if (invalidTypes.Count > 0)
            {
                throw new ArgumentException("Publication types must be valid and unique.", "publication");
            }
        }

        foreach (KeyValuePair<string, Dictionary<string, string>> account in publication.StateChange.Changed)
        {
            if (!ValuesId(account.Key) || account.Value == null)
            {
                throw new ArgumentException("A StateChange account entry is invalid.", "publication");
            }

            foreach (KeyValuePair<string, string> state in account.Value)
            {
                if (state.Key == null || state.Key.Length == 0 || state.Value == null || state.Value.Length == 0 || !Validator.Values.IsValidUnicode(state.Key) || !Validator.Values.IsValidUnicode(state.Value))
                {
                    throw new ArgumentException("A StateChange type state is invalid.", "publication");
                }
            }
        }
    }

    internal bool ValuesAreVerification(PushVerification verification)
    {
        return verification.Type == "PushVerification" && ValuesId(verification.PushSubscriptionId) && verification.VerificationCode != null && verification.VerificationCode.Length > 0 && Validator.Values.IsValidUnicode(verification.VerificationCode);
    }

    internal bool ValuesId(string? value)
    {
        return Validator.Values.IsId(value);
    }

    internal byte[] EncodeStateChange(StateChange value)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodeStateChange(value, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    internal byte[] EncodeVerification(PushVerification value)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec.EncodePushVerification(value, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    internal string CreateTopic(string id)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(id));
        byte[] topicBytes = new byte[24];
        Buffer.BlockCopy(digest, 0, topicBytes, 0, topicBytes.Length);
        string topic = Convert.ToBase64String(topicBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        CryptographicOperations.ZeroMemory(digest);
        return topic;
    }

    internal int EffectiveTimeToLive()
    {
        return Options.PushTimeToLiveSeconds < 0 ? 0 : Options.PushTimeToLiveSeconds;
    }

    internal DateTimeOffset CurrentTime()
    {
        return Clock == null ? DateTimeOffset.UtcNow : Clock.GetCurrentUtcDateTime();
    }
}
