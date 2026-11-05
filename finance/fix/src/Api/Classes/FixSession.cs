namespace fix;

/// <summary>Coordinates one caller-driven classic FIX or FIXT session.</summary>
public class FixSession : IFixSession
{
    internal FixSessionCore Core;

    /// <summary>Creates a session from explicit protocol, persistence, transport, and application dependencies.</summary>
    /// <param name="settings">The complete session settings.</param>
    /// <param name="transport">The synchronous session transport.</param>
    /// <param name="store">The durable message and sequence store.</param>
    /// <param name="log">The structured audit log.</param>
    /// <param name="clock">The deterministic session clock.</param>
    /// <param name="throttle">The application-message throttle.</param>
    /// <param name="application">The session application callbacks.</param>
    /// <param name="framer">The TagValue frame boundary reader.</param>
    /// <param name="decoder">The TagValue message decoder.</param>
    /// <param name="encoder">The TagValue message encoder.</param>
    /// <param name="validator">The dictionary and session message validator.</param>
    /// <param name="dictionaryProvider">The transport and application dictionary provider.</param>
    /// <param name="authenticator">The inbound Logon authenticator.</param>
    /// <exception cref="FixSessionException">Thrown when settings or dependencies are invalid.</exception>
    public FixSession(SessionSettings settings, ISessionTransport transport, IMessageStore store, IFixLog log, ISessionClock clock, IMessageThrottle throttle, ISessionApplication application, IMessageFramer framer, IMessageDecoder decoder, IMessageEncoder encoder, IMessageValidator validator, IDictionaryProvider dictionaryProvider, ISessionAuthenticator authenticator)
    {
        Core = new FixSessionCore(settings, transport, store, log, clock, throttle, application, framer, decoder, encoder, validator, dictionaryProvider, authenticator);
    }

    /// <summary>Starts persistence, transport, and Logon work without creating a worker thread.</summary>
    /// <returns>The resulting state, ordered events, and snapshot.</returns>
    public SessionPollResult StartSession()
    {
        return Core.StartSessionWork();
    }

    /// <summary>Performs at most one immediately available transport, message, or timer action.</summary>
    /// <returns>The resulting state, ordered events, and snapshot.</returns>
    public SessionPollResult PollSession()
    {
        return Core.PollSessionWork();
    }

    /// <summary>Validates and queues one application or administrative message.</summary>
    /// <param name="message">The message to submit.</param>
    /// <returns>The explicit validation, throttling, and queue outcome.</returns>
    public SessionSendResult SendMessage(FixMessage message)
    {
        return Core.SendSessionMessage(message);
    }

    /// <summary>Begins a graceful Logout exchange.</summary>
    /// <param name="text">The optional Logout text.</param>
    /// <returns>The resulting state, ordered events, and snapshot.</returns>
    public SessionPollResult LogoutSession(string text)
    {
        return Core.BeginSessionLogout(text);
    }

    /// <summary>Stops timers, closes the transport, and clears volatile queues.</summary>
    /// <returns>The resulting state, ordered events, and snapshot.</returns>
    public SessionPollResult StopSession()
    {
        return Core.StopSessionWork();
    }

    /// <summary>Returns a detached operational snapshot.</summary>
    /// <returns>The current session snapshot.</returns>
    public SessionSnapshot GetSessionSnapshot()
    {
        return Core.CreateSessionSnapshot();
    }
}
