using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Publishes, processes, and applies Diameter overload and load information.</summary>
public interface IDiameterOverloadManager
{
    /// <summary>Builds and publishes a local overload report for a request scope.</summary>
    /// <param name="request">The request scope used to obtain local overload state.</param>
    /// <returns>The owned local overload report.</returns>
    DiameterOverloadReport PublishDiameterOverloadReport(
        DiameterAdmissionRequest? request);

    /// <summary>Processes one overload report received from a Diameter peer.</summary>
    /// <param name="report">The received overload report.</param>
    /// <param name="sourcePeer">The peer that supplied the report.</param>
    /// <param name="receivedAt">The time at which the report was received.</param>
    /// <returns>The accepted owned report snapshot.</returns>
    DiameterOverloadReport ProcessDiameterOverloadReport(
        DiameterOverloadReport? report,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt);

    /// <summary>Builds and publishes a local host or peer load report.</summary>
    /// <param name="source">The local load source represented by the report.</param>
    /// <returns>The owned local load report.</returns>
    DiameterLoadReport PublishDiameterLoadReport(
        DiameterLoadSource source);

    /// <summary>Processes one load report received from a Diameter peer.</summary>
    /// <param name="report">The received load report.</param>
    /// <param name="sourcePeer">The peer that supplied the report.</param>
    /// <param name="receivedAt">The time at which the report was received.</param>
    /// <returns>The accepted owned load report snapshot.</returns>
    DiameterLoadReport ProcessDiameterLoadReport(
        DiameterLoadReport? report,
        DiameterIdentity? sourcePeer,
        DateTimeOffset receivedAt);

    /// <summary>Evaluates overload admission for one routed request.</summary>
    /// <param name="request">The request and current route candidate.</param>
    /// <returns>The owned admission decision.</returns>
    DiameterAdmissionDecision EvaluateDiameterAdmission(
        DiameterAdmissionRequest? request);

    /// <summary>Reads owned active overload report snapshots.</summary>
    /// <returns>The reports in deterministic cache order.</returns>
    List<DiameterOverloadReport> ReadDiameterOverloadReports();

    /// <summary>Reads owned active load report snapshots.</summary>
    /// <returns>The reports in deterministic cache order.</returns>
    List<DiameterLoadReport> ReadDiameterLoadReports();
}

/// <summary>Supplies local reporting-node overload and load state.</summary>
public interface IOverloadSource
{
    /// <summary>Reads local overload state for one request scope.</summary>
    /// <param name="request">The owned request scope being reported.</param>
    /// <returns>The local overload report.</returns>
    DiameterOverloadReport ReadLocalDiameterOverloadReport(
        DiameterAdmissionRequest? request);

    /// <summary>Reads local host or peer load state.</summary>
    /// <param name="source">The load source represented by the report.</param>
    /// <returns>The local load report.</returns>
    DiameterLoadReport ReadLocalDiameterLoadReport(
        DiameterLoadSource source);
}
