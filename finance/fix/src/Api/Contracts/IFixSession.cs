namespace fix;

/// <summary>Controls one caller-driven FIX session.</summary>
public interface IFixSession
{
    /// <summary>Starts the session.</summary>
    /// <returns>The resulting state and events.</returns>
    SessionPollResult StartSession();
    /// <summary>Performs one bounded unit of session work.</summary>
    /// <returns>The resulting state and events.</returns>
    SessionPollResult PollSession();
    /// <summary>Submits an application or administrative message.</summary>
    /// <param name="message">The message to submit.</param>
    /// <returns>The explicit send outcome.</returns>
    SessionSendResult SendMessage(FixMessage message);
    /// <summary>Initiates graceful logout.</summary>
    /// <param name="text">The logout text.</param>
    /// <returns>The resulting state and events.</returns>
    SessionPollResult LogoutSession(string text);
    /// <summary>Stops the session and closes its transport.</summary>
    /// <returns>The resulting state and events.</returns>
    SessionPollResult StopSession();
    /// <summary>Gets the latest operational snapshot.</summary>
    /// <returns>The session snapshot.</returns>
    SessionSnapshot GetSessionSnapshot();
}
