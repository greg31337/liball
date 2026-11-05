using System.Collections.Generic;

namespace diameter;

/// <summary>Executes session-based and event-based RFC 8506 credit control.</summary>
public interface IDiameterCreditControlClient
{
    /// <summary>Begins one stateful credit-control session.</summary>
    /// <param name="request">The initial credit-control request.</param>
    /// <returns>The initial operation outcome.</returns>
    DiameterCreditControlResult BeginDiameterCreditControlSession(
        DiameterCreditControlRequest? request);

    /// <summary>Requests updated quota for one active session.</summary>
    /// <param name="request">The update credit-control request.</param>
    /// <returns>The update operation outcome.</returns>
    DiameterCreditControlResult UpdateDiameterCreditControlSession(
        DiameterCreditControlRequest? request);

    /// <summary>Reports final usage and terminates one active session.</summary>
    /// <param name="request">The termination credit-control request.</param>
    /// <returns>The termination operation outcome.</returns>
    DiameterCreditControlResult TerminateDiameterCreditControlSession(
        DiameterCreditControlRequest? request);

    /// <summary>Executes one stateless direct-debit, refund, balance, or price event.</summary>
    /// <param name="request">The event credit-control request.</param>
    /// <returns>The event operation outcome.</returns>
    DiameterCreditControlResult ExecuteDiameterCreditControlEvent(
        DiameterCreditControlRequest? request);

    /// <summary>Finds one retained credit-control session.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <returns>An owned session snapshot, or <c>null</c> when absent.</returns>
    DiameterCreditControlSession? FindDiameterCreditControlSession(
        string? sessionId);

    /// <summary>Reads owned credit-control sessions in deterministic order.</summary>
    /// <returns>The retained credit-control session snapshots.</returns>
    List<DiameterCreditControlSession>
        ReadDiameterCreditControlSessions();
}

/// <summary>Hosts the server side of the RFC 8506 credit-control application.</summary>
public interface IDiameterCreditControlApplication
{
    /// <summary>Processes one inbound Credit-Control-Request.</summary>
    /// <param name="request">The raw Credit-Control-Request message.</param>
    /// <param name="peerIdentity">The transport peer identity, or <c>null</c>.</param>
    /// <returns>The credit-control decision and encoded answer.</returns>
    DiameterCreditControlResult ProcessDiameterCreditControlRequest(
        DiameterMessage? request,
        DiameterIdentity? peerIdentity);

    /// <summary>Reads the capability advertised by credit control.</summary>
    /// <returns>An owned authentication application identifier.</returns>
    DiameterApplicationId ReadDiameterCreditControlCapability();
}

/// <summary>Performs application-specific rating and credit decisions.</summary>
public interface ICreditControlRequestHandler
{
    /// <summary>Processes one parsed inbound Credit-Control-Request.</summary>
    /// <param name="request">The owned request supplied for rating.</param>
    /// <returns>The application credit-control answer.</returns>
    DiameterCreditControlAnswer HandleDiameterCreditControlRequest(
        DiameterCreditControlRequest? request);
}

/// <summary>Persists credit-control sessions with optimistic versions.</summary>
public interface ICreditControlStore
{
    /// <summary>Loads one credit-control session by Session-Id.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <returns>An owned session snapshot, or <c>null</c> when absent.</returns>
    DiameterCreditControlSession? LoadDiameterCreditControlSession(
        string? sessionId);

    /// <summary>Atomically creates or replaces one credit-control session.</summary>
    /// <param name="session">The complete owned session state.</param>
    /// <param name="expectedVersion">Zero for creation or the current version for replacement.</param>
    /// <returns>The persisted owned session with its new version.</returns>
    DiameterCreditControlSession SaveDiameterCreditControlSession(
        DiameterCreditControlSession? session,
        long expectedVersion);

    /// <summary>Atomically removes one matching credit-control session.</summary>
    /// <param name="sessionId">The globally unique Session-Id.</param>
    /// <param name="expectedVersion">The version that must still be current.</param>
    /// <returns><c>true</c> when the matching session was removed.</returns>
    bool RemoveDiameterCreditControlSession(
        string? sessionId,
        long expectedVersion);

    /// <summary>Reads owned sessions in deterministic storage order.</summary>
    /// <returns>All retained credit-control sessions.</returns>
    List<DiameterCreditControlSession>
        ReadStoredDiameterCreditControlSessions();
}

/// <summary>Receives owned quota, final-unit, and failure outcomes.</summary>
public interface ICreditControlEventHandler
{
    /// <summary>Handles one credit-control lifecycle or quota event.</summary>
    /// <param name="eventData">The owned credit-control outcome.</param>
    void HandleDiameterCreditControlEvent(
        DiameterCreditControlResult eventData);
}

/// <summary>Selects minimization or redaction for sensitive credit-control AVPs.</summary>
public interface ICreditControlPrivacyPolicy
{
    /// <summary>Selects handling for one classified credit-control AVP.</summary>
    /// <param name="privacy">The AVP classification and agent context.</param>
    /// <returns>The requested privacy action.</returns>
    DiameterCreditControlPrivacyAction SelectDiameterCreditControlPrivacyAction(
        DiameterCreditControlAvpPrivacy privacy);
}

/// <summary>Applies a credit-control privacy policy to an owned message copy.</summary>
public interface ICreditControlPrivacyProcessor
{
    /// <summary>Minimizes or redacts sensitive AVPs without changing the input.</summary>
    /// <param name="message">The credit-control request or answer.</param>
    /// <param name="policy">The policy invoked for each sensitive AVP.</param>
    /// <param name="agentRole">The role forwarding the message.</param>
    /// <returns>An owned message containing the permitted AVPs.</returns>
    DiameterMessage ApplyDiameterCreditControlPrivacyPolicy(
        DiameterMessage? message,
        ICreditControlPrivacyPolicy? policy,
        DiameterAgentRole agentRole);
}
