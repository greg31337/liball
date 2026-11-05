using System.Collections.Generic;

namespace diameter;

internal enum PeerStateMachineEvent
{
    Start = 0,
    ResponderConnectionCapabilitiesRequestReceived = 1,
    InitiatorConnectionAcknowledged = 2,
    InitiatorConnectionRejected = 3,
    Timeout = 4,
    InitiatorCapabilitiesAnswerReceived = 5,
    InitiatorNonCapabilitiesAnswerReceived = 6,
    InitiatorPeerDisconnected = 7,
    ResponderPeerDisconnected = 8,
    ElectionWon = 9,
    SendMessage = 10,
    ResponderMessageReceived = 11,
    ResponderWatchdogRequestReceived = 12,
    ResponderWatchdogAnswerReceived = 13,
    Stop = 14,
    ResponderDisconnectRequestReceived = 15,
    InitiatorMessageReceived = 16,
    InitiatorWatchdogRequestReceived = 17,
    InitiatorWatchdogAnswerReceived = 18,
    InitiatorDisconnectRequestReceived = 19,
    InitiatorDisconnectAnswerReceived = 20,
    ResponderDisconnectAnswerReceived = 21
}

internal enum PeerStateMachineAction
{
    InitiatorSendConnectionRequest = 0,
    ResponderAccept = 1,
    ProcessCapabilitiesRequest = 2,
    ResponderSendCapabilitiesAnswer = 3,
    InitiatorSendCapabilitiesRequest = 4,
    ProcessCapabilitiesAnswer = 5,
    Cleanup = 6,
    Error = 7,
    Elect = 8,
    InitiatorDisconnect = 9,
    ResponderDisconnect = 10,
    ResponderReject = 11,
    ResponderSendMessage = 12,
    ProcessMessage = 13,
    ProcessWatchdogRequest = 14,
    ResponderSendWatchdogAnswer = 15,
    ProcessWatchdogAnswer = 16,
    ResponderSendDisconnectRequest = 17,
    ResponderSendDisconnectAnswer = 18,
    InitiatorSendMessage = 19,
    InitiatorSendWatchdogAnswer = 20,
    InitiatorSendDisconnectRequest = 21,
    InitiatorSendDisconnectAnswer = 22
}

internal class PeerStateTransition
{
    internal bool Defined;
    internal DiameterPeerState PreviousState;
    internal DiameterPeerState NextState;
    internal PeerStateMachineEvent Event;
    internal List<PeerStateMachineAction> Actions =
        new List<PeerStateMachineAction>();
    internal string Code = string.Empty;
}
