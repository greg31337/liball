namespace fix;

/// <summary>Receives ordered session lifecycle and message callbacks.</summary>
public interface ISessionApplication
{
    /// <summary>Handles session creation.</summary>
    /// <param name="id">The created session identity.</param>
    /// <returns>The application decision.</returns>
    ApplicationDecision OnSessionCreated(SessionId id);
    /// <summary>Handles successful logon.</summary>
    /// <param name="context">The logon message context.</param>
    /// <returns>The application decision.</returns>
    ApplicationDecision OnSessionLogon(ApplicationMessageContext context);
    /// <summary>Handles logout.</summary>
    /// <param name="context">The logout message context.</param>
    /// <returns>The application decision.</returns>
    ApplicationDecision OnSessionLogout(ApplicationMessageContext context);
    /// <summary>Handles an administrative message.</summary>
    /// <param name="context">The administrative message context.</param>
    /// <returns>The application decision.</returns>
    ApplicationDecision OnAdministrativeMessage(ApplicationMessageContext context);
    /// <summary>Handles an application message.</summary>
    /// <param name="context">The application message context.</param>
    /// <returns>The application decision.</returns>
    ApplicationDecision OnApplicationMessage(ApplicationMessageContext context);
    /// <summary>Reviews an application message before replay.</summary>
    /// <param name="context">The replay message context.</param>
    /// <returns>The application decision.</returns>
    ApplicationDecision OnReplayMessage(ApplicationMessageContext context);
}
