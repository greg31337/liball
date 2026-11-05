using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>
/// Sends and correlates accounting events and session record sequences.
/// </summary>
public class DiameterAccountingClient :
    IDiameterAccountingClient,
    IDiameterAnswerHandler
{
    internal AccountingClientRuntime Runtime;

    /// <summary>Initializes an accounting client over the outbound request pipeline.</summary>
    /// <param name="originHost">The local Diameter host identity.</param>
    /// <param name="originRealm">The local Diameter realm.</param>
    /// <param name="avpFactory">The base AVP factory.</param>
    /// <param name="requestClient">The outbound request client.</param>
    /// <param name="store">The durable pending-record store.</param>
    /// <param name="clock">The UTC clock used for accounting state.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when an identity or dependency is invalid.
    /// </exception>
    public DiameterAccountingClient(
        DiameterIdentity? originHost,
        DiameterIdentity? originRealm,
        IDiameterBaseAvpFactory? avpFactory,
        IDiameterRequestClient? requestClient,
        IAccountingRecordStore? store,
        IDiameterClock? clock)
    {
        DiameterValueResult normalizedHost =
            ValueCodecs.NormalizeDiameterIdentity(originHost);
        DiameterValueResult normalizedRealm =
            ValueCodecs.NormalizeDiameterRealm(originRealm);
        if (normalizedHost.Status != DiameterCodecStatus.Success ||
            normalizedRealm.Status != DiameterCodecStatus.Success ||
            avpFactory == null || requestClient == null || store == null ||
            clock == null)
        {
            throw new ArgumentException(
                "Valid accounting identities and dependencies are required.");
        }

        DiameterIdentity host = new DiameterIdentity();
        host.Value = normalizedHost.Text;
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = normalizedRealm.Text;
        Runtime = new AccountingClientRuntime(
            host,
            realm,
            avpFactory,
            requestClient,
            store,
            clock);
        Runtime.RecoverAccountingClientRecords(this);
    }

    /// <summary>Sends one self-contained accounting event.</summary>
    /// <param name="request">The event accounting data and delivery policy.</param>
    /// <returns>The accounting operation outcome.</returns>
    public DiameterAccountingResult SendDiameterAccountingEvent(
        DiameterAccountingRequest? request)
    {
        return Runtime.SendAccountingClientRecord(
            request,
            DiameterAccountingRecordType.Event,
            this);
    }

    /// <summary>Sends the initial record for an accounting session.</summary>
    /// <param name="request">The start accounting data and delivery policy.</param>
    /// <returns>The accounting operation outcome.</returns>
    public DiameterAccountingResult SendDiameterAccountingStart(
        DiameterAccountingRequest? request)
    {
        return Runtime.SendAccountingClientRecord(
            request,
            DiameterAccountingRecordType.Start,
            this);
    }

    /// <summary>Sends cumulative interim data for an accounting session.</summary>
    /// <param name="request">The interim accounting data and delivery policy.</param>
    /// <returns>The accounting operation outcome.</returns>
    public DiameterAccountingResult SendDiameterAccountingInterim(
        DiameterAccountingRequest? request)
    {
        return Runtime.SendAccountingClientRecord(
            request,
            DiameterAccountingRecordType.Interim,
            this);
    }

    /// <summary>Sends the terminal record for an accounting session.</summary>
    /// <param name="request">The stop accounting data and delivery policy.</param>
    /// <returns>The accounting operation outcome.</returns>
    public DiameterAccountingResult SendDiameterAccountingStop(
        DiameterAccountingRequest? request)
    {
        return Runtime.SendAccountingClientRecord(
            request,
            DiameterAccountingRecordType.Stop,
            this);
    }

    /// <summary>Reads owned pending and retained accounting records.</summary>
    /// <returns>The accounting records in deterministic creation order.</returns>
    public List<DiameterAccountingRecord> ReadDiameterAccountingRecords()
    {
        return Runtime.ReadAccountingClientRecords();
    }

    /// <summary>Correlates one accounting answer or terminal request outcome.</summary>
    /// <param name="result">The owned outbound request outcome.</param>
    public void HandleDiameterAnswer(DiameterAnswerResult result)
    {
        Runtime.HandleAccountingClientAnswer(result);
    }
}
