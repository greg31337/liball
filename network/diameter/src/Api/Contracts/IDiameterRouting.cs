using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Selects request routes and manages received redirect routes.</summary>
public interface IDiameterRouter
{
    /// <summary>Selects one local, agent, redirect, or translation route.</summary>
    /// <param name="request">The normalized request routing context.</param>
    /// <returns>The complete owned routing decision.</returns>
    DiameterRouteDecision SelectDiameterRoute(
        DiameterRouteRequest? request);

    /// <summary>Processes redirect routing information from an answer.</summary>
    /// <param name="request">The request that produced the redirect answer.</param>
    /// <param name="answer">The received redirect answer.</param>
    /// <param name="sourcePeer">The peer that supplied the answer.</param>
    /// <param name="receivedAt">The answer receive time.</param>
    /// <returns>The owned redirect entries accepted from the answer.</returns>
    List<DiameterRedirectEntry> ProcessDiameterRedirectAnswer(
        DiameterRouteRequest? request,
        DiameterMessage? answer,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt);

    /// <summary>Reads owned snapshots of the current route table.</summary>
    /// <returns>The route snapshots in deterministic order.</returns>
    List<DiameterRouteSnapshot> ReadDiameterRouteSnapshots();
}

/// <summary>Evaluates and scores one candidate during route selection.</summary>
public interface IRoutePolicy
{
    /// <summary>Applies policy to one owned route candidate.</summary>
    /// <param name="request">The request being routed.</param>
    /// <param name="candidate">The candidate to evaluate.</param>
    /// <returns>The owned evaluated candidate.</returns>
    DiameterRouteCandidate EvaluateDiameterRouteCandidate(
        DiameterRouteRequest? request,
        DiameterRouteCandidate? candidate);
}

/// <summary>Authorizes the complete Route-Record path before local delivery.</summary>
public interface IRoutePathPolicy
{
    /// <summary>Authorizes one owned request and its ordered traversed peer path.</summary>
    /// <param name="request">The request, local host, and ordered Route-Record values.</param>
    /// <returns>The path authorization decision.</returns>
    DiameterRoutePathAuthorizationResult AuthorizeDiameterRoutePath(
        DiameterRoutePathAuthorizationRequest? request);
}

/// <summary>Authorizes redirect targets before immediate or cached use.</summary>
public interface IRedirectHostPolicy
{
    /// <summary>Authorizes one owned redirect target and source context.</summary>
    /// <param name="request">The redirect target authorization request.</param>
    /// <returns>The redirect target authorization decision.</returns>
    DiameterRedirectHostAuthorizationResult AuthorizeDiameterRedirectHost(
        DiameterRedirectHostAuthorizationRequest? request);
}

/// <summary>Authorizes a discovered route produced by realm redirection.</summary>
public interface IRealmRedirectPolicy
{
    /// <summary>Authorizes one owned alternate realm and discovered route.</summary>
    /// <param name="request">The realm redirect authorization request.</param>
    /// <returns>The realm redirect authorization decision.</returns>
    DiameterRealmRedirectAuthorizationResult AuthorizeDiameterRealmRedirect(
        DiameterRealmRedirectAuthorizationRequest? request);
}

/// <summary>Translates request and answer messages between applications.</summary>
public interface ITranslationHandler
{
    /// <summary>Translates a routed request to a target application.</summary>
    /// <param name="request">The request routing context.</param>
    /// <param name="targetApplicationId">The target application identifier.</param>
    /// <returns>The owned request translation outcome.</returns>
    DiameterTranslationResult TranslateDiameterRequest(
        DiameterRouteRequest? request,
        uint targetApplicationId);

    /// <summary>Translates an answer using state from request translation.</summary>
    /// <param name="requestTranslation">The preceding request translation.</param>
    /// <param name="answer">The answer from the target application.</param>
    /// <returns>The owned answer translation outcome.</returns>
    DiameterTranslationResult TranslateDiameterAnswer(
        DiameterTranslationResult? requestTranslation,
        DiameterMessage? answer);
}
