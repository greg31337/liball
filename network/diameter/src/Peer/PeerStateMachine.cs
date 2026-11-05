using System;
using System.Collections.Generic;

namespace diameter;

internal class PeerStateMachine
{
    internal object StateLock = new object();
    internal DiameterPeerState State;

    internal PeerStateMachine(DiameterPeerState initialState)
    {
        ValidatePeerState(initialState);
        State = initialState;
    }

    internal DiameterPeerState ReadPeerState()
    {
        lock (StateLock)
        {
            return State;
        }
    }

    internal PeerStateTransition HandlePeerStateEvent(
        PeerStateMachineEvent peerEvent)
    {
        ValidatePeerEvent(peerEvent);
        lock (StateLock)
        {
            PeerStateTransition transition = ReadPeerStateTransition(
                State,
                peerEvent);
            if (transition.Defined)
            {
                State = transition.NextState;
            }

            return transition;
        }
    }

    internal PeerStateTransition ReadPeerStateTransition(
        DiameterPeerState state,
        PeerStateMachineEvent peerEvent)
    {
        if (state == DiameterPeerState.Closed)
        {
            return ReadClosedPeerTransition(peerEvent);
        }

        if (state == DiameterPeerState.WaitConnectionAcknowledgement)
        {
            return ReadWaitConnectionAcknowledgementPeerTransition(peerEvent);
        }

        if (state == DiameterPeerState.WaitInitiatorCapabilitiesAnswer)
        {
            return ReadWaitInitiatorCapabilitiesAnswerPeerTransition(peerEvent);
        }

        if (state == DiameterPeerState.WaitConnectionAcknowledgementElection)
        {
            return ReadWaitConnectionAcknowledgementElectionPeerTransition(peerEvent);
        }

        if (state == DiameterPeerState.WaitReturns)
        {
            return ReadWaitReturnsPeerTransition(peerEvent);
        }

        if (state == DiameterPeerState.ResponderOpen)
        {
            return ReadResponderOpenPeerTransition(peerEvent);
        }

        if (state == DiameterPeerState.InitiatorOpen)
        {
            return ReadInitiatorOpenPeerTransition(peerEvent);
        }

        if (state == DiameterPeerState.Closing)
        {
            return ReadClosingPeerTransition(peerEvent);
        }

        throw new ArgumentOutOfRangeException("state");
    }

    internal PeerStateTransition ReadClosedPeerTransition(
        PeerStateMachineEvent peerEvent)
    {
        if (peerEvent == PeerStateMachineEvent.Start)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.Closed,
                peerEvent,
                DiameterPeerState.WaitConnectionAcknowledgement,
                [PeerStateMachineAction.InitiatorSendConnectionRequest]);
        }

        if (peerEvent ==
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.Closed,
                peerEvent,
                DiameterPeerState.ResponderOpen,
                [
                    PeerStateMachineAction.ResponderAccept,
                    PeerStateMachineAction.ProcessCapabilitiesRequest,
                    PeerStateMachineAction.ResponderSendCapabilitiesAnswer
                ]);
        }

        return CreateUndefinedPeerStateTransition(
            DiameterPeerState.Closed,
            peerEvent);
    }

    internal PeerStateTransition ReadWaitConnectionAcknowledgementPeerTransition(
        PeerStateMachineEvent peerEvent)
    {
        if (peerEvent == PeerStateMachineEvent.InitiatorConnectionAcknowledged)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitConnectionAcknowledgement,
                peerEvent,
                DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
                [PeerStateMachineAction.InitiatorSendCapabilitiesRequest]);
        }

        if (peerEvent == PeerStateMachineEvent.InitiatorConnectionRejected)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitConnectionAcknowledgement,
                peerEvent,
                DiameterPeerState.Closed,
                [PeerStateMachineAction.Cleanup]);
        }

        if (peerEvent ==
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitConnectionAcknowledgement,
                peerEvent,
                DiameterPeerState.WaitConnectionAcknowledgementElection,
                [
                    PeerStateMachineAction.ResponderAccept,
                    PeerStateMachineAction.ProcessCapabilitiesRequest
                ]);
        }

        if (peerEvent == PeerStateMachineEvent.Timeout)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitConnectionAcknowledgement,
                peerEvent,
                DiameterPeerState.Closed,
                [PeerStateMachineAction.Error]);
        }

        return CreateUndefinedPeerStateTransition(
            DiameterPeerState.WaitConnectionAcknowledgement,
            peerEvent);
    }

    internal PeerStateTransition ReadWaitInitiatorCapabilitiesAnswerPeerTransition(
        PeerStateMachineEvent peerEvent)
    {
        if (peerEvent == PeerStateMachineEvent.InitiatorCapabilitiesAnswerReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
                peerEvent,
                DiameterPeerState.InitiatorOpen,
                [PeerStateMachineAction.ProcessCapabilitiesAnswer]);
        }

        if (peerEvent ==
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
                peerEvent,
                DiameterPeerState.WaitReturns,
                [
                    PeerStateMachineAction.ResponderAccept,
                    PeerStateMachineAction.ProcessCapabilitiesRequest,
                    PeerStateMachineAction.Elect
                ]);
        }

        if (peerEvent == PeerStateMachineEvent.InitiatorPeerDisconnected)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
                peerEvent,
                DiameterPeerState.Closed,
                [PeerStateMachineAction.InitiatorDisconnect]);
        }

        if (peerEvent == PeerStateMachineEvent.InitiatorNonCapabilitiesAnswerReceived ||
            peerEvent == PeerStateMachineEvent.Timeout)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
                peerEvent,
                DiameterPeerState.Closed,
                [PeerStateMachineAction.Error]);
        }

        return CreateUndefinedPeerStateTransition(
            DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
            peerEvent);
    }

    internal PeerStateTransition ReadWaitConnectionAcknowledgementElectionPeerTransition(
        PeerStateMachineEvent peerEvent)
    {
        if (peerEvent == PeerStateMachineEvent.InitiatorConnectionAcknowledged)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitConnectionAcknowledgementElection,
                peerEvent,
                DiameterPeerState.WaitReturns,
                [
                    PeerStateMachineAction.InitiatorSendCapabilitiesRequest,
                    PeerStateMachineAction.Elect
                ]);
        }

        if (peerEvent == PeerStateMachineEvent.InitiatorConnectionRejected)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitConnectionAcknowledgementElection,
                peerEvent,
                DiameterPeerState.ResponderOpen,
                [PeerStateMachineAction.ResponderSendCapabilitiesAnswer]);
        }

        if (peerEvent == PeerStateMachineEvent.ResponderPeerDisconnected)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitConnectionAcknowledgementElection,
                peerEvent,
                DiameterPeerState.WaitConnectionAcknowledgement,
                [PeerStateMachineAction.ResponderDisconnect]);
        }

        if (peerEvent ==
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitConnectionAcknowledgementElection,
                peerEvent,
                DiameterPeerState.WaitConnectionAcknowledgementElection,
                [PeerStateMachineAction.ResponderReject]);
        }

        if (peerEvent == PeerStateMachineEvent.Timeout)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitConnectionAcknowledgementElection,
                peerEvent,
                DiameterPeerState.Closed,
                [PeerStateMachineAction.Error]);
        }

        return CreateUndefinedPeerStateTransition(
            DiameterPeerState.WaitConnectionAcknowledgementElection,
            peerEvent);
    }

    internal PeerStateTransition ReadWaitReturnsPeerTransition(
        PeerStateMachineEvent peerEvent)
    {
        if (peerEvent == PeerStateMachineEvent.ElectionWon ||
            peerEvent == PeerStateMachineEvent.InitiatorPeerDisconnected)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitReturns,
                peerEvent,
                DiameterPeerState.ResponderOpen,
                [
                    PeerStateMachineAction.InitiatorDisconnect,
                    PeerStateMachineAction.ResponderSendCapabilitiesAnswer
                ]);
        }

        if (peerEvent == PeerStateMachineEvent.InitiatorCapabilitiesAnswerReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitReturns,
                peerEvent,
                DiameterPeerState.InitiatorOpen,
                [PeerStateMachineAction.ResponderDisconnect]);
        }

        if (peerEvent == PeerStateMachineEvent.ResponderPeerDisconnected)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitReturns,
                peerEvent,
                DiameterPeerState.WaitInitiatorCapabilitiesAnswer,
                [PeerStateMachineAction.ResponderDisconnect]);
        }

        if (peerEvent ==
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitReturns,
                peerEvent,
                DiameterPeerState.WaitReturns,
                [PeerStateMachineAction.ResponderReject]);
        }

        if (peerEvent == PeerStateMachineEvent.Timeout)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.WaitReturns,
                peerEvent,
                DiameterPeerState.Closed,
                [PeerStateMachineAction.Error]);
        }

        return CreateUndefinedPeerStateTransition(
            DiameterPeerState.WaitReturns,
            peerEvent);
    }

    internal PeerStateTransition ReadResponderOpenPeerTransition(
        PeerStateMachineEvent peerEvent)
    {
        if (peerEvent == PeerStateMachineEvent.SendMessage)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.ResponderOpen,
                peerEvent,
                DiameterPeerState.ResponderOpen,
                [PeerStateMachineAction.ResponderSendMessage]);
        }

        if (peerEvent == PeerStateMachineEvent.ResponderMessageReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.ResponderOpen,
                peerEvent,
                DiameterPeerState.ResponderOpen,
                [PeerStateMachineAction.ProcessMessage]);
        }

        if (peerEvent == PeerStateMachineEvent.ResponderWatchdogRequestReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.ResponderOpen,
                peerEvent,
                DiameterPeerState.ResponderOpen,
                [
                    PeerStateMachineAction.ProcessWatchdogRequest,
                    PeerStateMachineAction.ResponderSendWatchdogAnswer
                ]);
        }

        if (peerEvent == PeerStateMachineEvent.ResponderWatchdogAnswerReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.ResponderOpen,
                peerEvent,
                DiameterPeerState.ResponderOpen,
                [PeerStateMachineAction.ProcessWatchdogAnswer]);
        }

        if (peerEvent ==
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.ResponderOpen,
                peerEvent,
                DiameterPeerState.ResponderOpen,
                [PeerStateMachineAction.ResponderReject]);
        }

        if (peerEvent == PeerStateMachineEvent.Stop)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.ResponderOpen,
                peerEvent,
                DiameterPeerState.Closing,
                [PeerStateMachineAction.ResponderSendDisconnectRequest]);
        }

        if (peerEvent == PeerStateMachineEvent.ResponderDisconnectRequestReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.ResponderOpen,
                peerEvent,
                DiameterPeerState.Closing,
                [PeerStateMachineAction.ResponderSendDisconnectAnswer]);
        }

        if (peerEvent == PeerStateMachineEvent.ResponderPeerDisconnected)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.ResponderOpen,
                peerEvent,
                DiameterPeerState.Closed,
                [PeerStateMachineAction.ResponderDisconnect]);
        }

        return CreateUndefinedPeerStateTransition(
            DiameterPeerState.ResponderOpen,
            peerEvent);
    }

    internal PeerStateTransition ReadInitiatorOpenPeerTransition(
        PeerStateMachineEvent peerEvent)
    {
        if (peerEvent == PeerStateMachineEvent.SendMessage)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.InitiatorOpen,
                peerEvent,
                DiameterPeerState.InitiatorOpen,
                [PeerStateMachineAction.InitiatorSendMessage]);
        }

        if (peerEvent == PeerStateMachineEvent.InitiatorMessageReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.InitiatorOpen,
                peerEvent,
                DiameterPeerState.InitiatorOpen,
                [PeerStateMachineAction.ProcessMessage]);
        }

        if (peerEvent == PeerStateMachineEvent.InitiatorWatchdogRequestReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.InitiatorOpen,
                peerEvent,
                DiameterPeerState.InitiatorOpen,
                [
                    PeerStateMachineAction.ProcessWatchdogRequest,
                    PeerStateMachineAction.InitiatorSendWatchdogAnswer
                ]);
        }

        if (peerEvent == PeerStateMachineEvent.InitiatorWatchdogAnswerReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.InitiatorOpen,
                peerEvent,
                DiameterPeerState.InitiatorOpen,
                [PeerStateMachineAction.ProcessWatchdogAnswer]);
        }

        if (peerEvent ==
            PeerStateMachineEvent.ResponderConnectionCapabilitiesRequestReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.InitiatorOpen,
                peerEvent,
                DiameterPeerState.InitiatorOpen,
                [PeerStateMachineAction.ResponderReject]);
        }

        if (peerEvent == PeerStateMachineEvent.Stop)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.InitiatorOpen,
                peerEvent,
                DiameterPeerState.Closing,
                [PeerStateMachineAction.InitiatorSendDisconnectRequest]);
        }

        if (peerEvent == PeerStateMachineEvent.InitiatorDisconnectRequestReceived)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.InitiatorOpen,
                peerEvent,
                DiameterPeerState.Closing,
                [PeerStateMachineAction.InitiatorSendDisconnectAnswer]);
        }

        if (peerEvent == PeerStateMachineEvent.InitiatorPeerDisconnected)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.InitiatorOpen,
                peerEvent,
                DiameterPeerState.Closed,
                [PeerStateMachineAction.InitiatorDisconnect]);
        }

        return CreateUndefinedPeerStateTransition(
            DiameterPeerState.InitiatorOpen,
            peerEvent);
    }

    internal PeerStateTransition ReadClosingPeerTransition(
        PeerStateMachineEvent peerEvent)
    {
        if (peerEvent == PeerStateMachineEvent.InitiatorDisconnectAnswerReceived ||
            peerEvent == PeerStateMachineEvent.InitiatorPeerDisconnected)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.Closing,
                peerEvent,
                DiameterPeerState.Closed,
                [PeerStateMachineAction.InitiatorDisconnect]);
        }

        if (peerEvent == PeerStateMachineEvent.ResponderDisconnectAnswerReceived ||
            peerEvent == PeerStateMachineEvent.ResponderPeerDisconnected)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.Closing,
                peerEvent,
                DiameterPeerState.Closed,
                [PeerStateMachineAction.ResponderDisconnect]);
        }

        if (peerEvent == PeerStateMachineEvent.Timeout)
        {
            return CreatePeerStateTransition(
                DiameterPeerState.Closing,
                peerEvent,
                DiameterPeerState.Closed,
                [PeerStateMachineAction.Error]);
        }

        return CreateUndefinedPeerStateTransition(
            DiameterPeerState.Closing,
            peerEvent);
    }

    internal PeerStateTransition CreatePeerStateTransition(
        DiameterPeerState previousState,
        PeerStateMachineEvent peerEvent,
        DiameterPeerState nextState,
        List<PeerStateMachineAction> actions)
    {
        PeerStateTransition result = new PeerStateTransition();
        result.Defined = true;
        result.PreviousState = previousState;
        result.NextState = nextState;
        result.Event = peerEvent;
        result.Actions = actions;
        result.Code = "transition-defined";
        return result;
    }

    internal PeerStateTransition CreateUndefinedPeerStateTransition(
        DiameterPeerState state,
        PeerStateMachineEvent peerEvent)
    {
        PeerStateTransition result = new PeerStateTransition();
        result.PreviousState = state;
        result.NextState = state;
        result.Event = peerEvent;
        result.Code = "event-not-defined";
        return result;
    }

    internal void ValidatePeerState(DiameterPeerState state)
    {
        if (state != DiameterPeerState.Closed &&
            state != DiameterPeerState.WaitConnectionAcknowledgement &&
            state != DiameterPeerState.WaitInitiatorCapabilitiesAnswer &&
            state != DiameterPeerState.WaitConnectionAcknowledgementElection &&
            state != DiameterPeerState.WaitReturns &&
            state != DiameterPeerState.ResponderOpen &&
            state != DiameterPeerState.InitiatorOpen &&
            state != DiameterPeerState.Closing)
        {
            throw new ArgumentOutOfRangeException("state");
        }
    }

    internal void ValidatePeerEvent(PeerStateMachineEvent peerEvent)
    {
        if ((int)peerEvent < (int)PeerStateMachineEvent.Start ||
            (int)peerEvent >
                (int)PeerStateMachineEvent.ResponderDisconnectAnswerReceived)
        {
            throw new ArgumentOutOfRangeException("peerEvent");
        }
    }
}
