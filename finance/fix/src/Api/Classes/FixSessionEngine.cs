using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace fix;

/// <summary>Owns and polls independently configured FIX sessions in deterministic insertion order.</summary>
public class FixSessionEngine : ISessionEngine
{
    internal List<FixSessionEngineEntry> Sessions = [];
    internal ISessionClock Clock;
    internal ISessionApplication Application;
    internal IDictionaryProvider DictionaryProvider;
    internal ISessionAuthenticator? CustomAuthenticator;
    internal FixMessageOperations MessageOperations = new FixMessageOperations();

    /// <summary>Creates an engine using shared clock, application, dictionary, and optional authentication services.</summary>
    /// <param name="clock">The deterministic clock shared by independently configured sessions.</param>
    /// <param name="application">The application receiving identity-bearing callbacks from all sessions.</param>
    /// <param name="dictionaryProvider">The dictionary provider used by all sessions.</param>
    /// <param name="customAuthenticator">An optional authentication policy applied after each session's configured credentials.</param>
    /// <exception cref="FixSessionException">Thrown when a required engine dependency is missing.</exception>
    public FixSessionEngine(ISessionClock clock, ISessionApplication application, IDictionaryProvider dictionaryProvider, ISessionAuthenticator? customAuthenticator)
    {
        if (clock == null || application == null || dictionaryProvider == null)
        {
            throw new FixSessionException("A FixSessionEngine dependency is missing.");
        }
        Clock = clock;
        Application = application;
        DictionaryProvider = dictionaryProvider;
        CustomAuthenticator = customAuthenticator;
    }

    /// <summary>Adds, starts, and retains one independently configured session.</summary>
    /// <param name="settings">The complete settings for the new session.</param>
    /// <returns>The detached initial lifecycle result.</returns>
    /// <exception cref="FixSessionException">Thrown when settings are invalid or the full session identity is already registered.</exception>
    public SessionPollResult AddSession(SessionSettings settings)
    {
        lock (this)
        {
            ValidateEngineSettings(settings);
            if (FindSessionIndex(settings.Id) >= 0)
            {
                throw new FixSessionException("A session with the same complete identity is already registered.");
            }
            IFixSession session = CreateConfiguredSession(settings);
            SessionPollResult started = session.StartSession();
            if (started == null)
            {
                throw new FixSessionException("The added session did not return a start result.");
            }
            if (started.State != SessionState.Stopped)
            {
                FixSessionEngineEntry entry = new FixSessionEngineEntry(CloneEngineId(settings.Id), session);
                Sessions.Add(entry);
            }
            return ClonePollResult(started);
        }
    }

    /// <summary>Stops and removes the session matching the complete supplied identity.</summary>
    /// <param name="id">The complete session identity.</param>
    /// <returns>The detached final lifecycle result.</returns>
    /// <exception cref="FixSessionException">Thrown when the identity is missing or not registered.</exception>
    public SessionPollResult RemoveSession(SessionId id)
    {
        lock (this)
        {
            ValidateEngineId(id);
            int index = FindSessionIndex(id);
            if (index < 0)
            {
                throw new FixSessionException("The requested session is not registered.");
            }
            SessionPollResult stopped = Sessions[index].Session.StopSession();
            if (stopped == null)
            {
                throw new FixSessionException("The removed session did not return a stop result.");
            }
            Sessions.RemoveAt(index);
            return ClonePollResult(stopped);
        }
    }

    /// <summary>Polls every registered session once and aggregates detached events in insertion order.</summary>
    /// <returns>The aggregated events, work indication, and snapshot of the last processed session.</returns>
    public SessionPollResult PollSessions()
    {
        lock (this)
        {
            SessionPollResult aggregate = CreateEmptyEngineResult();
            for (int index = 0; index < Sessions.Count; index++)
            {
                SessionPollResult polled = Sessions[index].Session.PollSession();
                if (polled == null)
                {
                    AppendMissingPollEvent(aggregate, Sessions[index].Id);
                    continue;
                }
                AppendPollResult(aggregate, polled);
            }
            return aggregate;
        }
    }

    /// <summary>Resolves a detached snapshot for the session matching the complete supplied identity.</summary>
    /// <param name="id">The complete session identity.</param>
    /// <returns>The detached current session snapshot.</returns>
    /// <exception cref="FixSessionException">Thrown when the identity is missing or not registered.</exception>
    public SessionSnapshot ResolveSessionSnapshot(SessionId id)
    {
        lock (this)
        {
            ValidateEngineId(id);
            int index = FindSessionIndex(id);
            if (index < 0)
            {
                throw new FixSessionException("The requested session is not registered.");
            }
            SessionSnapshot snapshot = Sessions[index].Session.GetSessionSnapshot();
            if (snapshot == null)
            {
                throw new FixSessionException("The requested session did not return a snapshot.");
            }
            return CloneEngineSnapshot(snapshot);
        }
    }

    internal IFixSession CreateConfiguredSession(SessionSettings settings)
    {
        ISessionTransport transport = new ConfiguredSessionTransport(settings);
        IMessageStore store = CreateConfiguredStore(settings);
        IFixLog log = CreateConfiguredLog(settings);
        IMessageThrottle throttle = new TokenBucketMessageThrottle(Clock);
        ISessionAuthenticator authenticator = new BasicSessionAuthenticator(settings, CustomAuthenticator);
        return new FixSession(settings, transport, store, log, Clock, throttle, Application, new TagValueMessageFramer(), new TagValueMessageDecoder(), new TagValueMessageEncoder(), new FixMessageValidator(), DictionaryProvider, authenticator);
    }

    internal IMessageStore CreateConfiguredStore(SessionSettings settings)
    {
        if (string.Equals(settings.StoreType, "Memory", StringComparison.OrdinalIgnoreCase))
        {
            return new MemoryMessageStore(settings.StoreMaximumMessages);
        }
        if (string.Equals(settings.StoreType, "File", StringComparison.OrdinalIgnoreCase))
        {
            return new FileMessageStore(settings.StorePath, CreateStoreName(settings.Id), settings.StoreMaximumMessages);
        }
        throw new FixSessionException("The configured message store type is unsupported: " + settings.StoreType);
    }

    internal IFixLog CreateConfiguredLog(SessionSettings settings)
    {
        if (string.Equals(settings.LogType, "Null", StringComparison.OrdinalIgnoreCase))
        {
            return new NullFixLog();
        }
        if (string.Equals(settings.LogType, "File", StringComparison.OrdinalIgnoreCase))
        {
            return new FileFixLog(settings.LogPath, settings.RedactLogSensitiveFields);
        }
        throw new FixSessionException("The configured FIX log type is unsupported: " + settings.LogType);
    }

    internal int FindSessionIndex(SessionId id)
    {
        for (int index = 0; index < Sessions.Count; index++)
        {
            if (SessionIdsEqual(Sessions[index].Id, id)) return index;
        }
        return -1;
    }

    internal void AppendPollResult(SessionPollResult aggregate, SessionPollResult polled)
    {
        aggregate.State = polled.State;
        aggregate.WorkRemaining = aggregate.WorkRemaining || polled.WorkRemaining;
        aggregate.Snapshot = CloneEngineSnapshot(polled.Snapshot);
        for (int index = 0; index < polled.Events.Count; index++)
        {
            aggregate.Events.Add(CloneEngineEvent(polled.Events[index]));
        }
    }

    internal void AppendMissingPollEvent(SessionPollResult aggregate, SessionId id)
    {
        SessionEvent sessionEvent = new SessionEvent();
        sessionEvent.Id = CloneEngineId(id);
        sessionEvent.Kind = SessionEventKind.Error;
        sessionEvent.TimestampUtc = Clock.GetUtcNow();
        sessionEvent.Code = "missing-session-poll-result";
        sessionEvent.Text = "A registered session did not return a poll result.";
        aggregate.Events.Add(sessionEvent);
    }

    internal SessionPollResult ClonePollResult(SessionPollResult source)
    {
        SessionPollResult result = new SessionPollResult();
        result.State = source.State;
        result.WorkRemaining = source.WorkRemaining;
        result.Snapshot = CloneEngineSnapshot(source.Snapshot);
        for (int index = 0; index < source.Events.Count; index++)
        {
            result.Events.Add(CloneEngineEvent(source.Events[index]));
        }
        return result;
    }

    internal SessionEvent CloneEngineEvent(SessionEvent source)
    {
        SessionEvent result = new SessionEvent();
        result.Id = CloneEngineId(source.Id);
        result.Kind = source.Kind;
        result.TimestampUtc = source.TimestampUtc;
        result.SequenceNumber = source.SequenceNumber;
        result.Code = source.Code;
        result.Text = source.Text;
        if (source.Message != null)
        {
            MessageEditResult cloned = MessageOperations.CloneMessage(source.Message);
            result.Message = cloned.Message;
        }
        return result;
    }

    internal static SessionSnapshot CloneEngineSnapshot(SessionSnapshot source)
    {
        SessionSnapshot result = new SessionSnapshot();
        result.Id = CloneEngineId(source.Id);
        result.State = source.State;
        result.TransportState = source.TransportState;
        result.IsLoggedOn = source.IsLoggedOn;
        result.NextSenderSeqNum = source.NextSenderSeqNum;
        result.NextTargetSeqNum = source.NextTargetSeqNum;
        result.InboundQueueCount = source.InboundQueueCount;
        result.OutboundQueueCount = source.OutboundQueueCount;
        result.HasLastInboundTime = source.HasLastInboundTime;
        result.LastInboundTimeUtc = source.LastInboundTimeUtc;
        result.HasLastOutboundTime = source.HasLastOutboundTime;
        result.LastOutboundTimeUtc = source.LastOutboundTimeUtc;
        return result;
    }

    internal static SessionId CloneEngineId(SessionId source)
    {
        SessionId result = new SessionId();
        result.BeginString = source.BeginString;
        result.SenderCompID = source.SenderCompID;
        result.SenderSubID = source.SenderSubID;
        result.SenderLocationID = source.SenderLocationID;
        result.TargetCompID = source.TargetCompID;
        result.TargetSubID = source.TargetSubID;
        result.TargetLocationID = source.TargetLocationID;
        result.Qualifier = source.Qualifier;
        return result;
    }

    internal static bool SessionIdsEqual(SessionId left, SessionId right)
    {
        return left.BeginString == right.BeginString && left.SenderCompID == right.SenderCompID && left.SenderSubID == right.SenderSubID && left.SenderLocationID == right.SenderLocationID && left.TargetCompID == right.TargetCompID && left.TargetSubID == right.TargetSubID && left.TargetLocationID == right.TargetLocationID && left.Qualifier == right.Qualifier;
    }

    internal static SessionPollResult CreateEmptyEngineResult()
    {
        SessionPollResult result = new SessionPollResult();
        result.State = SessionState.Stopped;
        result.Snapshot.State = SessionState.Stopped;
        result.Snapshot.TransportState = TransportState.Disconnected;
        return result;
    }

    internal static string CreateStoreName(SessionId id)
    {
        StringBuilder identity = new StringBuilder();
        AppendIdentityField(identity, id.BeginString);
        AppendIdentityField(identity, id.SenderCompID);
        AppendIdentityField(identity, id.SenderSubID);
        AppendIdentityField(identity, id.SenderLocationID);
        AppendIdentityField(identity, id.TargetCompID);
        AppendIdentityField(identity, id.TargetSubID);
        AppendIdentityField(identity, id.TargetLocationID);
        AppendIdentityField(identity, id.Qualifier);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity.ToString()));
        return "session-" + Convert.ToHexString(hash);
    }

    internal static void AppendIdentityField(StringBuilder destination, string value)
    {
        destination.Append(value.Length);
        destination.Append(':');
        destination.Append(value);
        destination.Append(';');
    }

    internal static void ValidateEngineSettings(SessionSettings settings)
    {
        if (settings == null || settings.Id == null)
        {
            throw new FixSessionException("The session engine settings are missing.");
        }
        ValidateEngineId(settings.Id);
        if (settings.StoreType == null || settings.LogType == null)
        {
            throw new FixSessionException("The session persistence configuration is missing.");
        }
    }

    internal static void ValidateEngineId(SessionId id)
    {
        if (id == null || id.BeginString == null || id.SenderCompID == null || id.SenderSubID == null || id.SenderLocationID == null || id.TargetCompID == null || id.TargetSubID == null || id.TargetLocationID == null || id.Qualifier == null)
        {
            throw new FixSessionException("The complete session identity is missing.");
        }
    }
}
