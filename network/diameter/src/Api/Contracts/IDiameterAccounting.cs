using System.Collections.Generic;

namespace diameter;

/// <summary>Sends EVENT, START, INTERIM, and STOP accounting records.</summary>
public interface IDiameterAccountingClient
{
    /// <summary>Sends one self-contained accounting event.</summary>
    /// <param name="request">The event accounting data and delivery policy.</param>
    /// <returns>The accounting operation outcome.</returns>
    DiameterAccountingResult SendDiameterAccountingEvent(
        DiameterAccountingRequest? request);

    /// <summary>Sends the initial record for an accounting session.</summary>
    /// <param name="request">The start accounting data and delivery policy.</param>
    /// <returns>The accounting operation outcome.</returns>
    DiameterAccountingResult SendDiameterAccountingStart(
        DiameterAccountingRequest? request);

    /// <summary>Sends cumulative interim data for an accounting session.</summary>
    /// <param name="request">The interim accounting data and delivery policy.</param>
    /// <returns>The accounting operation outcome.</returns>
    DiameterAccountingResult SendDiameterAccountingInterim(
        DiameterAccountingRequest? request);

    /// <summary>Sends the terminal record for an accounting session.</summary>
    /// <param name="request">The stop accounting data and delivery policy.</param>
    /// <returns>The accounting operation outcome.</returns>
    DiameterAccountingResult SendDiameterAccountingStop(
        DiameterAccountingRequest? request);

    /// <summary>Reads owned pending and retained accounting records.</summary>
    /// <returns>The accounting records in deterministic storage order.</returns>
    List<DiameterAccountingRecord> ReadDiameterAccountingRecords();
}

/// <summary>Processes one parsed inbound Accounting-Request.</summary>
public interface IDiameterAccountingHandler
{
    /// <summary>Processes accounting data and supplies the answer policy.</summary>
    /// <param name="request">The owned parsed accounting request.</param>
    /// <returns>The application accounting answer.</returns>
    DiameterAccountingAnswer HandleDiameterAccountingRequest(
        DiameterAccountingRequest? request);
}

/// <summary>Hosts the server side of the base accounting application.</summary>
public interface IDiameterAccountingApplication
{
    /// <summary>Processes one inbound Accounting-Request.</summary>
    /// <param name="request">The raw Accounting-Request message.</param>
    /// <param name="peerIdentity">The transport peer identity, or <c>null</c> when unavailable.</param>
    /// <returns>The accounting processing and answer outcome.</returns>
    DiameterAccountingProcessingResult ProcessDiameterAccountingRequest(
        DiameterMessage? request,
        DiameterIdentity? peerIdentity);

    /// <summary>Reads the capability advertised by the base accounting application.</summary>
    /// <returns>An owned base accounting application identifier.</returns>
    DiameterApplicationId ReadDiameterAccountingCapability();
}

/// <summary>Persists pending accounting records with optimistic versions.</summary>
public interface IAccountingRecordStore
{
    /// <summary>Loads one accounting record by its local identifier.</summary>
    /// <param name="recordIdentifier">The positive local record identifier.</param>
    /// <returns>The found or missing store outcome.</returns>
    DiameterAccountingStoreResult LoadDiameterAccountingRecord(
        long recordIdentifier);

    /// <summary>Atomically creates or replaces one accounting record.</summary>
    /// <param name="record">The complete owned accounting record.</param>
    /// <param name="expectedVersion">Zero for creation or the current version for replacement.</param>
    /// <returns>The save or conflict outcome.</returns>
    DiameterAccountingStoreResult SaveDiameterAccountingRecord(
        DiameterAccountingRecord? record,
        long expectedVersion);

    /// <summary>Atomically removes one matching accounting record.</summary>
    /// <param name="recordIdentifier">The positive local record identifier.</param>
    /// <param name="expectedVersion">The version that must still be current.</param>
    /// <returns>The removal, missing, or conflict outcome.</returns>
    DiameterAccountingStoreResult RemoveDiameterAccountingRecord(
        long recordIdentifier,
        long expectedVersion);

    /// <summary>Reads owned accounting records in deterministic storage order.</summary>
    /// <returns>All retained accounting records.</returns>
    List<DiameterAccountingRecord> ReadStoredDiameterAccountingRecords();
}
