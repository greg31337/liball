using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>
/// Provides the client and server sides of the Diameter base accounting application.
/// </summary>
public class DiameterAccountingApplication :
    IDiameterAccountingClient,
    IDiameterAccountingApplication,
    IDiameterAnswerHandler
{
    internal AccountingClientRuntime ClientRuntime;
    internal AccountingServerStateMachine ServerRuntime;

    /// <summary>Initializes the base accounting application over the Diameter request pipeline.</summary>
    /// <param name="originHost">The local Diameter host identity.</param>
    /// <param name="originRealm">The local Diameter realm.</param>
    /// <param name="avpFactory">The base AVP factory.</param>
    /// <param name="commandFactory">The base command factory.</param>
    /// <param name="requestClient">The outbound request client.</param>
    /// <param name="duplicateStore">The inbound duplicate-request store.</param>
    /// <param name="handler">The inbound accounting application handler.</param>
    /// <param name="recordStore">The durable pending-record store.</param>
    /// <param name="clock">The UTC clock used for accounting state.</param>
    /// <exception cref="System.ArgumentException">
    /// Thrown when an identity or dependency is invalid.
    /// </exception>
    public DiameterAccountingApplication(
        DiameterIdentity? originHost,
        DiameterIdentity? originRealm,
        IDiameterBaseAvpFactory? avpFactory,
        IDiameterBaseCommandFactory? commandFactory,
        IDiameterRequestClient? requestClient,
        IDuplicateRequestStore? duplicateStore,
        IDiameterAccountingHandler? handler,
        IAccountingRecordStore? recordStore,
        IDiameterClock? clock)
    {
        DiameterValueResult normalizedHost =
            ValueCodecs.NormalizeDiameterIdentity(originHost);
        DiameterValueResult normalizedRealm =
            ValueCodecs.NormalizeDiameterRealm(originRealm);
        if (normalizedHost.Status != DiameterCodecStatus.Success ||
            normalizedRealm.Status != DiameterCodecStatus.Success ||
            avpFactory == null || commandFactory == null ||
            requestClient == null || duplicateStore == null ||
            handler == null || recordStore == null || clock == null)
        {
            throw new ArgumentException(
                "Valid accounting identities and dependencies are required.");
        }

        DiameterIdentity host = new DiameterIdentity();
        host.Value = normalizedHost.Text;
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = normalizedRealm.Text;
        ClientRuntime = new AccountingClientRuntime(
            host,
            realm,
            avpFactory,
            requestClient,
            recordStore,
            clock);
        ServerRuntime = new AccountingServerStateMachine(
            host,
            realm,
            commandFactory,
            duplicateStore,
            handler,
            clock);
        ClientRuntime.RecoverAccountingClientRecords(this);
    }

    /// <summary>Sends one self-contained accounting event.</summary>
    /// <param name="request">The event accounting data and delivery policy.</param>
    /// <returns>The accounting operation outcome.</returns>
    public DiameterAccountingResult SendDiameterAccountingEvent(
        DiameterAccountingRequest? request)
    {
        return ClientRuntime.SendAccountingClientRecord(
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
        return ClientRuntime.SendAccountingClientRecord(
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
        return ClientRuntime.SendAccountingClientRecord(
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
        return ClientRuntime.SendAccountingClientRecord(
            request,
            DiameterAccountingRecordType.Stop,
            this);
    }

    /// <summary>Reads owned pending and retained accounting records.</summary>
    /// <returns>The accounting records in deterministic creation order.</returns>
    public List<DiameterAccountingRecord> ReadDiameterAccountingRecords()
    {
        return ClientRuntime.ReadAccountingClientRecords();
    }

    /// <summary>Correlates one accounting answer or terminal request outcome.</summary>
    /// <param name="result">The owned outbound request outcome.</param>
    public void HandleDiameterAnswer(DiameterAnswerResult result)
    {
        ClientRuntime.HandleAccountingClientAnswer(result);
    }

    /// <summary>Processes one inbound Accounting-Request.</summary>
    /// <param name="request">The raw Accounting-Request message.</param>
    /// <param name="peerIdentity">The transport peer identity, or <c>null</c> when unavailable.</param>
    /// <returns>The accounting processing and answer outcome.</returns>
    public DiameterAccountingProcessingResult ProcessDiameterAccountingRequest(
        DiameterMessage? request,
        DiameterIdentity? peerIdentity)
    {
        AccountingServerResult processed =
            ServerRuntime.ProcessAccountingServerRequest(
                request,
                peerIdentity);
        return CreateAccountingProcessingResult(processed);
    }

    /// <summary>Reads the capability advertised by the base accounting application.</summary>
    /// <returns>An owned base accounting application identifier.</returns>
    public DiameterApplicationId ReadDiameterAccountingCapability()
    {
        DiameterApplicationId result = new DiameterApplicationId();
        result.Id = DiameterApplicationIds.BaseAccounting;
        result.VendorId = DiameterVendorIds.Ietf;
        result.IsAccounting = true;
        return result;
    }

    internal DiameterAccountingProcessingResult
        CreateAccountingProcessingResult(AccountingServerResult processed)
    {
        DiameterAccountingProcessingResult result =
            new DiameterAccountingProcessingResult();
        if (processed.Request != null)
        {
            result.Request =
                AccountingRecordCloner.CloneAccountingRequest(
                    processed.Request);
        }

        if (processed.AnswerData != null)
        {
            result.Answer =
                AccountingRecordCloner.CloneAccountingAnswer(
                    processed.AnswerData);
        }
        else if (processed.Answer != null)
        {
            DiameterAccountingAnswer answer =
                new DiameterAccountingAnswer();
            answer.SessionId = result.Request.SessionId;
            answer.RecordType = result.Request.RecordType;
            answer.RecordNumber = result.Request.RecordNumber;
            answer.ResultCode = processed.ResultCode;
            answer.Message =
                AccountingRecordCloner.CloneOptionalAccountingMessage(
                    processed.Answer);
            result.Answer = answer;
        }

        result.Accepted = processed.Accepted;
        result.Duplicate = processed.Duplicate;
        result.Replayed = processed.Replayed;
        result.ApplicationInvoked = processed.ApplicationInvoked;
        result.ResultCode = processed.ResultCode;
        result.Code = processed.Code;
        result.Message = processed.Message;
        return result;
    }
}
